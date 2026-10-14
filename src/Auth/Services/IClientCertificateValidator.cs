namespace Auth.Services;

public interface IClientCertificateValidator
{
    string GetFingerprint(string certificateContent);
    string Validate(string certificateContent, IReadOnlyList<string>? certificateChain);
}
