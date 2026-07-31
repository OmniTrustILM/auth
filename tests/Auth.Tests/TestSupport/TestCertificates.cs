using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Auth.Tests.TestSupport;

/// <summary>
/// Self-signed certificate generated once per test run. Self-signed means chain building fails against any machine's
/// trust store, which is what the authentication tests need; the identify tests only need the fingerprint, because that
/// path does not verify the chain.
/// </summary>
public static class TestCertificates
{
    private static readonly Lazy<X509Certificate2> Generated = new(() =>
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=auth-test-user", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    });

    public static X509Certificate2 Certificate => Generated.Value;

    public static string Base64 => Convert.ToBase64String(Certificate.Export(X509ContentType.Cert));

    /// <summary>Lowercase hex SHA-256 thumbprint, in the form <c>UserService</c> stores and looks users up by.</summary>
    public static string Sha256Fingerprint => Convert.ToHexString(Certificate.GetCertHash(HashAlgorithmName.SHA256)).ToLower();
}
