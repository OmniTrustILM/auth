using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Auth.Tests.TestSupport;

/// <summary>Fresh public test chains without AIA/CRL URLs or host trust-store writes.</summary>
public sealed class TestCertificateChains : IDisposable
{
    private readonly List<X509Certificate2> _owned = [];
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "auth-chain-" + Guid.NewGuid());
    public X509Certificate2 RootA { get; }
    public X509Certificate2 RootB { get; }
    public X509Certificate2 WrongKeyA { get; }
    public X509Certificate2 IntermediateA { get; }
    public X509Certificate2 IntermediateB { get; }
    public X509Certificate2 LeafA { get; }
    public X509Certificate2 LeafB { get; }
    public X509Certificate2 Pin { get; }

    public TestCertificateChains()
    {
        Directory.CreateDirectory(DirectoryPath);
        // Unique names keep a previous fixture's ambient intermediate cache from completing a missing path.
        var id = Guid.NewGuid();
        RootA = Root("CN=Root A " + id);
        RootB = Root("CN=Root B " + id);
        WrongKeyA = Root(RootA.Subject);
        using var intermediateKey = RSA.Create(2048);
        IntermediateA = Issue("CN=Intermediate " + id, intermediateKey, RootA, true);
        IntermediateB = Issue("CN=Intermediate " + id, intermediateKey, RootB, true);
        LeafA = Leaf(IntermediateA);
        LeafB = Leaf(IntermediateB);
        Pin = Root("CN=Infrastructure pin " + id, false);
    }

    private X509Certificate2 Own(X509Certificate2 certificate)
    {
        _owned.Add(certificate);
        return certificate;
    }

    private static CertificateRequest Request(string subject, RSA key, bool ca, int padding = 0)
    {
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(ca, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(ca
            ? X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign : X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        if (padding > 0) request.CertificateExtensions.Add(new X509Extension("1.3.6.1.4.1.55555.1", new byte[padding], false));
        return request;
    }

    private X509Certificate2 Root(string subject, bool ca = true)
    {
        using var key = RSA.Create(2048);
        return Own(Request(subject, key, ca).CreateSelfSigned(DateTimeOffset.UtcNow.AddYears(-5), DateTimeOffset.UtcNow.AddYears(5)));
    }

    public X509Certificate2 Issue(string subject, RSA key, X509Certificate2 issuer, bool ca,
        DateTimeOffset? before = null, DateTimeOffset? after = null, int padding = 0)
    {
        var serial = RandomNumberGenerator.GetBytes(16);
        // A positive, nonzero leading byte gives exact-size fixtures a stable DER INTEGER length.
        serial[0] = 1;
        using var publicCertificate = Request(subject, key, ca, padding).Create(issuer,
            before ?? DateTimeOffset.UtcNow.AddYears(-1), after ?? DateTimeOffset.UtcNow.AddYears(1), serial);
        return Own(publicCertificate.CopyWithPrivateKey(key));
    }

    public X509Certificate2 Leaf(X509Certificate2 issuer, DateTimeOffset? before = null, DateTimeOffset? after = null)
    {
        using var key = RSA.Create(2048);
        return Issue("CN=Client " + Guid.NewGuid(), key, issuer, false,
            before ?? DateTimeOffset.UtcNow.AddDays(-1), after ?? DateTimeOffset.UtcNow.AddDays(1));
    }

    public X509Certificate2 LargeLeaf(int size)
    {
        using var key = RSA.Create(2048);
        var padding = size - 900;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var certificate = Issue("CN=Large client", key, RootA, false, padding: padding);
            if (certificate.RawData.Length == size) return certificate;
            padding += size - certificate.RawData.Length;
        }
        throw new InvalidOperationException("Unable to construct exact-sized public fixture.");
    }

    public static string Base64(X509Certificate2 certificate) => Convert.ToBase64String(certificate.RawData);
    public static string Fingerprint(X509Certificate2 certificate) => Convert.ToHexString(certificate.GetCertHash(HashAlgorithmName.SHA256)).ToLowerInvariant();
    public string Bundle(params X509Certificate2[] certificates)
    {
        var path = Path.Combine(DirectoryPath, Guid.NewGuid() + ".pem");
        File.WriteAllText(path, string.Join('\n', certificates.Select(c => c.ExportCertificatePem())));
        return path;
    }

    public void Dispose()
    {
        foreach (var certificate in _owned) certificate.Dispose();
        Directory.Delete(DirectoryPath, true);
    }
}
