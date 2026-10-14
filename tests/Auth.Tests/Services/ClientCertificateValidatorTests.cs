using Auth.Common.Exceptions;
using Auth.Services;
using Auth.Tests.TestSupport;
using System.Text;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Auth.Tests.Services;

public sealed class ClientCertificateValidatorTests : IDisposable
{
    private readonly TestCertificateChains _certificates = new();
    private static ClientCertificateValidator Validator() => new();
    private string Leaf => TestCertificateChains.Base64(_certificates.LeafA);
    private string Intermediate => TestCertificateChains.Base64(_certificates.IntermediateA);

    [Fact]
    public async Task SystemTrustWithForwardedIntermediatesUsesAnIsolatedLinuxProcess()
    {
        var directory = Environment.GetEnvironmentVariable("AUTH_SYSTEM_TRUST_FIXTURE_DIRECTORY");
        if (directory != null)
        {
            var leaf = File.ReadAllText(Path.Combine(directory, "leaf.txt"));
            var extendedLeaf = File.ReadAllText(Path.Combine(directory, "general-string-leaf.txt"));
            var intermediate = File.ReadAllText(Path.Combine(directory, "intermediate.txt"));
            var root = File.ReadAllText(Path.Combine(directory, "root.txt"));
            Assert.Throws<UnauthorizedException>(() => Validator().Validate(leaf, null));
            if (Environment.GetEnvironmentVariable("AUTH_SYSTEM_TRUST_EXPECTED_ROOT") == Path.Combine(directory, "trusted-root.pem"))
            {
                Assert.Equal(File.ReadAllText(Path.Combine(directory, "leaf-fingerprint.txt")), Validator().Validate(leaf, [intermediate]));
                Assert.Equal(File.ReadAllText(Path.Combine(directory, "general-string-fingerprint.txt")), Validator().Validate(extendedLeaf, [intermediate]));
            }
            else
            {
                Assert.Throws<UnauthorizedException>(() => Validator().Validate(leaf, [intermediate, root]));
            }
            return;
        }
        if (!OperatingSystem.IsLinux())
        {
            // The isolated System-root success control uses Linux/OpenSSL's process-local CA paths.
            Assert.Throws<UnauthorizedException>(() => Validator().Validate(Leaf, [Intermediate]));
            return;
        }

        var extended = _certificates.Leaf(_certificates.IntermediateA, extension: Convert.FromHexString("1B0161"));
        var fixtures = new Dictionary<string, string>
        {
            ["leaf.txt"] = Leaf,
            ["leaf-fingerprint.txt"] = TestCertificateChains.Fingerprint(_certificates.LeafA),
            ["general-string-leaf.txt"] = TestCertificateChains.Base64(extended),
            ["general-string-fingerprint.txt"] = TestCertificateChains.Fingerprint(extended),
            ["intermediate.txt"] = Intermediate,
            ["root.txt"] = TestCertificateChains.Base64(_certificates.RootA),
            ["trusted-root.pem"] = _certificates.RootA.ExportCertificatePem(),
            ["unrelated-root.pem"] = _certificates.RootB.ExportCertificatePem()
        };
        foreach (var fixture in fixtures) File.WriteAllText(Path.Combine(_certificates.DirectoryPath, fixture.Key), fixture.Value);
        var testName = typeof(ClientCertificateValidatorTests).FullName + "." + nameof(SystemTrustWithForwardedIntermediatesUsesAnIsolatedLinuxProcess);
        await _certificates.RunSystemTrustProbe(Path.Combine(_certificates.DirectoryPath, "trusted-root.pem"), testName);
        await _certificates.RunSystemTrustProbe(Path.Combine(_certificates.DirectoryPath, "unrelated-root.pem"), testName);
    }

    [Fact]
    public void ForwardedRootNeverAuthorizesAnUntrustedLeaf()
        => Assert.Throws<UnauthorizedException>(() => Validator().Validate(Leaf,
            [Intermediate, TestCertificateChains.Base64(_certificates.RootA)]));

    [Fact]
    public void MissingIntermediateFailsWithoutAia()
        => Assert.Throws<UnauthorizedException>(() => Validator().Validate(Leaf, null));

    [Fact]
    public void SelfSignedLeafCannotAuthorizeItself()
        => Assert.Throws<UnauthorizedException>(() => Validator().Validate(TestCertificateChains.Base64(_certificates.Pin), null));

    [Fact]
    public void CrossSignedCandidatesCannotSupplyTheirOwnAnchors()
        => Assert.Throws<UnauthorizedException>(() => Validator().Validate(Leaf,
            [Intermediate, TestCertificateChains.Base64(_certificates.IntermediateB),
             TestCertificateChains.Base64(_certificates.RootA), TestCertificateChains.Base64(_certificates.RootB)]));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonminimalNestedLengthIsRejectedBeforeTrust(bool malformedLeaf)
    {
        var original = _certificates.LeafA.RawData;
        // The outer length stays canonical; only TBSCertificate gains a redundant length octet.
        var certificate = new AsnReader(original, AsnEncodingRules.DER).ReadSequence();
        var tbs = certificate.ReadEncodedValue().ToArray();
        Assert.Equal(0x82, tbs[1]);
        var malformedTbs = new byte[] { 0x30, 0x83, 0 }.Concat(tbs.Skip(2)).ToArray();
        var body = malformedTbs.Concat(certificate.ReadEncodedValue().ToArray())
            .Concat(certificate.ReadEncodedValue().ToArray()).ToArray();
        var malformed = new byte[] { 0x30, 0x82, (byte)(body.Length >> 8), (byte)body.Length }.Concat(body).ToArray();
        var encoded = Convert.ToBase64String(malformed);
        Assert.Throws<InvalidFormatException>(() => malformedLeaf
            ? Validator().Validate(encoded, [Intermediate])
            : Validator().Validate(Leaf, [Intermediate, encoded]));
    }

    [Theory]
    [InlineData("1000")] // SEQUENCE must be constructed.
    [InlineData("010101")] // DER TRUE must be FF.
    [InlineData("02020001")] // Redundant positive INTEGER octet.
    [InlineData("03020101")] // Nonzero unused BIT STRING bit.
    [InlineData("050100")] // NULL has content.
    [InlineData("06028001")] // Nonminimal OID component.
    [InlineData("3106020102020101")] // Unsorted SET OF.
    [InlineData("1C0400110000")] // Invalid UCS-4 scalar.
    [InlineData("1C03000061")] // Truncated UCS-4 character.
    [InlineData("24800401000000")] // Constructed, indefinite OCTET STRING.
    public void NoncanonicalAlgorithmParametersInUnusedCandidateFailBeforeTrust(string parameters)
    {
        var candidate = WithSignatureParameters(Convert.FromHexString(parameters));
        Assert.Throws<InvalidFormatException>(() => Validator().Validate(Leaf, [Intermediate, candidate]));
    }

    [Theory]
    [InlineData("070161")] // ObjectDescriptor.
    [InlineData("0900")] // REAL zero.
    [InlineData("0D012A")] // RELATIVE-OID.
    [InlineData("0E08313938352D313032")] // TIME: 1985-102 (ordinal date).
    [InlineData("1F1F083139383530343132")] // DATE: 1985-04-12.
    [InlineData("1F2006313532373436")] // TIME-OF-DAY: 15:27:46.
    [InlineData("1F210E3139383530343132313031353330")] // DATE-TIME: 1985-04-12T10:15:30.
    [InlineData("1F220454373248")] // DURATION: PT72H.
    [InlineData("1F230F2F54656C6562696F6D657472696373")] // OID-IRI: /Telebiometrics.
    [InlineData("1F2403544D4D")] // RELATIVE-OID-IRI: TMM.
    [InlineData("150161")] // VideotexString.
    [InlineData("190161")] // GraphicString.
    [InlineData("1B0161")] // GeneralString.
    [InlineData("280706012AA0020500")] // EXTERNAL.
    [InlineData("2B0BA00606012A06012A040100")] // EMBEDDED PDV.
    [InlineData("3D0BA00606012A06012A040100")] // CHARACTER STRING.
    [InlineData("3108A003020101810100")] // SET fields ordered by tag number, not encoded octets.
    [InlineData("3107800100A0020500")] // SET OF equal context-specific tags, with different P/C bits.
    [InlineData("3109020101020102040100")] // SET OF CHOICE values sorted across every element.
    [InlineData("800101")]
    [InlineData("A003020101")]
    [InlineData("0101FF")]
    [InlineData("020101")]
    [InlineData("03020080")]
    [InlineData("040100")]
    [InlineData("0500")]
    [InlineData("06032A0304")]
    [InlineData("0A0101")]
    [InlineData("0C0161")]
    [InlineData("130161")]
    [InlineData("140161")]
    [InlineData("160161")]
    [InlineData("1E020061")]
    [InlineData("1C0400000061")]
    [InlineData("170D3236303130313030303030305A")]
    [InlineData("180F32303530303130313030303030305A")]
    [InlineData("3106020101020102")]
    public void CanonicalExtensionValuesSurviveParsing(string parameters)
        => Assert.Throws<UnauthorizedException>(() => Validator().Validate(Leaf,
            [Intermediate, WithExtension(Convert.FromHexString(parameters))]));

    [Theory]
    [InlineData("3003010101", false)] // Noncanonical TRUE in BasicConstraints.
    [InlineData("3081030101FF", false)] // Nonminimal BasicConstraints length.
    [InlineData("3003010101", true)]
    [InlineData("3081030101FF", true)]
    [InlineData("30003000", false)] // More than one DER value.
    [InlineData("", false)] // Missing DER value.
    [InlineData("0000", false)] // End-of-contents is not a DER value.
    [InlineData("0F00", false)] // Unassigned universal tag.
    [InlineData("1F2500", false)] // Unassigned high universal tag.
    [InlineData("0800", false)] // EXTERNAL must be constructed.
    [InlineData("0B00", false)] // EMBEDDED PDV must be constructed.
    [InlineData("1D00", false)] // CHARACTER STRING must be constructed.
    [InlineData("21030101FF", false)] // BOOLEAN must be primitive.
    [InlineData("3B031B0161", false)] // DER GeneralString must be primitive.
    [InlineData("1B810161", false)] // GeneralString has a nonminimal length.
    [InlineData("1F1B0161", false)] // GeneralString has a nonminimal tag.
    [InlineData("2808A0063081030101FF", false)] // Nonminimal nested length inside EXTERNAL.
    public void ExtensionValuesMustBeExactlyOneCanonicalDerValue(string extension, bool malformedLeaf)
    {
        var encoded = WithExtension(Convert.FromHexString(extension), "2.5.29.19");
        Assert.Throws<InvalidFormatException>(() => malformedLeaf
            ? Validator().Validate(encoded, [])
            : Validator().Validate(Leaf, [Intermediate, encoded]));
    }

    [Theory]
    [InlineData("3106020102020101", false)]
    [InlineData("3106020102020101", true)]
    [InlineData("3107A0020500800100", false)]
    [InlineData("3107A0020500800100", true)]
    [InlineData("3109020101040100020102", false)] // Repeated INTEGER pair is sorted; the full SET OF is not.
    public void UnsortedSetOfInPrivateExtensionFailsBeforeTrust(string extension, bool malformedLeaf)
    {
        var encoded = WithExtension(Convert.FromHexString(extension));
        Assert.Throws<InvalidFormatException>(() => malformedLeaf
            ? Validator().Validate(encoded, [])
            : Validator().Validate(Leaf, [Intermediate, encoded]));
    }

    private static string WithExtension(byte[] rawData, string oid = "1.3.6.1.4.1.55555.2")
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Extension candidate", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509Extension(oid, rawData, false));
        using var candidate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return TestCertificateChains.Base64(candidate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExcessiveNestingIsRejectedWithoutRecursiveParsing(bool extensionValue)
    {
        byte[] parameters = [0x05, 0x00];
        for (var i = 0; i < 64; i++)
            parameters = parameters.Length < 128
                ? new byte[] { 0x30, (byte)parameters.Length }.Concat(parameters).ToArray()
                : new byte[] { 0x30, 0x81, (byte)parameters.Length }.Concat(parameters).ToArray();
        Assert.Throws<InvalidFormatException>(() => Validator().Validate(Leaf,
            [Intermediate, extensionValue ? WithExtension(parameters) : WithSignatureParameters(parameters)]));
    }

    private string WithSignatureParameters(byte[] parameters)
    {
        var certificate = new AsnReader(_certificates.LeafA.RawData, AsnEncodingRules.DER).ReadSequence();
        var tbs = certificate.ReadEncodedValue();
        var algorithm = certificate.ReadSequence();
        var oid = algorithm.ReadEncodedValue();
        // BER permits deliberately noncanonical test parameters; surrounding fields retain canonical lengths.
        var writer = new AsnWriter(AsnEncodingRules.BER);
        using (writer.PushSequence())
        {
            writer.WriteEncodedValue(tbs.Span);
            using (writer.PushSequence())
            {
                writer.WriteEncodedValue(oid.Span);
                writer.WriteEncodedValue(parameters);
            }
            writer.WriteEncodedValue(certificate.ReadEncodedValue().Span);
        }
        return Convert.ToBase64String(writer.Encode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("!")]
    [InlineData("AQ==")]
    public void MalformedUnusedCandidateFailsBeforeTrust(string? candidate)
        => Assert.Throws<InvalidFormatException>(() => Validator().Validate(Leaf, [Intermediate, candidate!]));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("!")]
    [InlineData("AQ==")]
    public void MalformedLeafFailsBeforeTrust(string? leaf)
        => Assert.Throws<InvalidFormatException>(() => Validator().Validate(leaf!, null));

    [Fact]
    public void CandidatesRequireCanonicalBase64AndCompleteDer()
    {
        var validator = Validator();
        Assert.Throws<InvalidFormatException>(() => validator.Validate(Leaf, [Intermediate + "\n"]));
        var wrappedPem = Convert.ToBase64String(Encoding.ASCII.GetBytes(_certificates.IntermediateA.ExportCertificatePem()));
        Assert.Throws<InvalidFormatException>(() => validator.Validate(Leaf, [wrappedPem]));
        var appended = Convert.ToBase64String(_certificates.IntermediateA.RawData.Concat(_certificates.RootA.RawData).ToArray());
        Assert.Throws<InvalidFormatException>(() => validator.Validate(Leaf, [appended]));
        Assert.Throws<InvalidFormatException>(() => validator.Validate(appended, [Intermediate]));
    }

    [Fact]
    public void NewChainRequiresDerWhileLegacyWrappedPemKeepsLoaderBehavior()
    {
        var pem = Convert.ToBase64String(Encoding.ASCII.GetBytes(_certificates.LeafA.ExportCertificatePem()));
        Assert.Equal(TestCertificateChains.Fingerprint(_certificates.LeafA), Validator().GetFingerprint(pem));
        Assert.Throws<UnauthorizedException>(() => Validator().Validate(pem, null));
        Assert.Throws<InvalidFormatException>(() => Validator().Validate(pem, [Intermediate]));
        Assert.Equal(TestCertificateChains.Fingerprint(_certificates.LeafA), Validator().GetFingerprint(Leaf + "\n"));
    }

    [Fact]
    public void CountsCandidatesBeforeDeduplication()
    {
        Assert.Throws<UnauthorizedException>(() => Validator().Validate(Leaf, Enumerable.Repeat(Intermediate, 15).ToArray()));
        Assert.Throws<InvalidFormatException>(() => Validator().Validate(Leaf, Enumerable.Repeat(Intermediate, 16).ToArray()));
    }

    [Fact]
    public void BoundsTotalEncodedCharactersBeforeDecoding()
    {
        var exact = Leaf.PadRight(262144, ' ');
        Assert.Equal(TestCertificateChains.Fingerprint(_certificates.LeafA), Validator().GetFingerprint(exact));
        Assert.Throws<InvalidFormatException>(() => Validator().GetFingerprint(exact + " "));
        Assert.Throws<InvalidFormatException>(() => Validator().Validate(exact, [Intermediate]));
        Assert.Throws<UnauthorizedException>(() => Validator().Validate(Leaf.PadRight(262144 - Intermediate.Length, ' '), [Intermediate]));
    }

    [Fact]
    public void BoundsIndividualEncodedCandidatesBeforeDecoding()
        => Assert.Throws<InvalidFormatException>(() => Validator().Validate(Leaf, [new string('A', 87385)]));

    [Fact]
    public void BoundsRealCertificateDerAtExactly64KiB()
    {
        var exact = _certificates.LargeLeaf(65536);
        var over = _certificates.LargeLeaf(65537);
        Assert.Equal(65536, exact.RawData.Length);
        Assert.Equal(TestCertificateChains.Fingerprint(exact), Validator().GetFingerprint(TestCertificateChains.Base64(exact)));
        Assert.Throws<UnauthorizedException>(() => Validator().Validate(TestCertificateChains.Base64(exact), null));
        Assert.Throws<InvalidFormatException>(() => Validator().Validate(TestCertificateChains.Base64(over), null));
        Assert.Throws<InvalidFormatException>(() => Validator().GetFingerprint(TestCertificateChains.Base64(over)));
        Assert.Throws<InvalidFormatException>(() => Validator().Validate(Leaf, [TestCertificateChains.Base64(over)]));
    }

    public void Dispose() => _certificates.Dispose();
}
