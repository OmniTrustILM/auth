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
    public void ExtensionValuesMustBeExactlyOneCanonicalDerValue(string extension, bool malformedLeaf)
    {
        var encoded = WithExtension(Convert.FromHexString(extension), "2.5.29.19");
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

    [Fact]
    public void ExcessiveNestedAlgorithmParametersAreRejectedWithoutRecursiveParsing()
    {
        byte[] parameters = [0x05, 0x00];
        for (var i = 0; i < 64; i++)
            parameters = parameters.Length < 128
                ? new byte[] { 0x30, (byte)parameters.Length }.Concat(parameters).ToArray()
                : new byte[] { 0x30, 0x81, (byte)parameters.Length }.Concat(parameters).ToArray();
        Assert.Throws<InvalidFormatException>(() => Validator().Validate(Leaf, [Intermediate, WithSignatureParameters(parameters)]));
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
