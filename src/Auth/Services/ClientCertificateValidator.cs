using Auth.Common.Exceptions;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Auth.Services;

/// <summary>Validates against existing System trust; forwarded material is never an anchor.</summary>
public sealed class ClientCertificateValidator : IClientCertificateValidator
{
    private const int RequestCharacterLimit = 262144;
    private const int CertificateByteLimit = 65536;
    private const int MaterialLimit = 16;
    private const int CanonicalCertificateCharacterLimit = 87384;

    public string GetFingerprint(string certificateContent)
    {
        using var certificate = LoadCertificate(certificateContent, strictDer: false);
        return Fingerprint(certificate);
    }

    public string Validate(string certificateContent, IReadOnlyList<string>? certificateChain)
    {
        CheckRequestBounds(certificateContent, certificateChain);

        using var leaf = LoadCertificate(certificateContent, certificateChain != null);
        var candidates = new List<X509Certificate2>();
        try
        {
            ReadCandidates(certificateChain, candidates);
            using var chain = new X509Chain();
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            chain.ChainPolicy.ExtraStore.AddRange(candidates.ToArray());
            if (!chain.Build(leaf)) throw InvalidTrust();

            return Fingerprint(leaf);
        }
        catch (CryptographicException)
        {
            throw InvalidTrust();
        }
        finally
        {
            foreach (var certificate in candidates) certificate.Dispose();
        }
    }

    private static void ReadCandidates(IReadOnlyList<string>? encodedChain, List<X509Certificate2> candidates)
    {
        if (encodedChain == null) return;
        foreach (var encoded in encodedChain)
            AddDistinct(candidates, LoadCertificate(encoded, strictDer: true, canonicalBase64: true));
    }

    private static void CheckRequestBounds(string content, IReadOnlyList<string>? chain)
    {
        if (string.IsNullOrEmpty(content) || content.Length > RequestCharacterLimit || chain?.Count >= MaterialLimit)
            throw InvalidCertificateFormat();
        long total = content.Length;
        if (chain == null) return;
        foreach (var candidate in chain)
        {
            if (string.IsNullOrEmpty(candidate) || candidate.Length > CanonicalCertificateCharacterLimit)
                throw InvalidCertificateFormat();
            total += candidate.Length;
            if (total > RequestCharacterLimit) throw InvalidCertificateFormat();
        }
    }

    private static X509Certificate2 LoadCertificate(string content, bool strictDer, bool canonicalBase64 = false)
    {
        if (string.IsNullOrEmpty(content) || content.Length > RequestCharacterLimit) throw InvalidCertificateFormat();
        try
        {
            var bytes = Convert.FromBase64String(content);
            if (canonicalBase64 && !string.Equals(Convert.ToBase64String(bytes), content, StringComparison.Ordinal))
                throw InvalidCertificateFormat();
            if (strictDer)
            {
                if (bytes.Length > CertificateByteLimit) throw InvalidCertificateFormat();
                var reader = new AsnReader(bytes, AsnEncodingRules.DER);
                ValidateDerContents(reader.ReadSequence());
                reader.ThrowIfNotEmpty();
            }

            return LoadValidatedCertificate(bytes, strictDer);
        }
        catch (FormatException)
        {
            // Keep the legacy domain/inner-exception contract without retaining submitted material.
            throw new InvalidFormatException("Wrong format of user authentication certificate.", new FormatException("Invalid certificate encoding."));
        }
        catch (Exception exception) when (exception is CryptographicException or AsnContentException or DecoderFallbackException)
        {
            throw InvalidCertificateFormat();
        }
    }

    private static X509Certificate2 LoadValidatedCertificate(byte[] bytes, bool strictDer)
    {
        // The legacy loader also accepts Base64-wrapped PEM and its historical first-certificate behavior.
        var certificate = X509CertificateLoader.LoadCertificate(bytes);
        try
        {
            if (certificate.RawData.Length > CertificateByteLimit || (strictDer && !certificate.RawData.AsSpan().SequenceEqual(bytes)))
                throw InvalidCertificateFormat();
            if (strictDer)
            {
                // X.509 extnValue contains one DER value; other OCTET STRING payloads remain opaque.
                foreach (var extension in certificate.Extensions)
                {
                    var reader = new AsnReader(extension.RawData, AsnEncodingRules.DER);
                    var value = reader.ReadEncodedValue();
                    reader.ThrowIfNotEmpty();
                    ValidateDerContents(new AsnReader(value, AsnEncodingRules.DER));
                }
            }
            return certificate;
        }
        catch
        {
            certificate.Dispose();
            throw;
        }
    }

    private static void ValidateDerContents(AsnReader contents)
    {
        // Each TLV consumes bounded input. An explicit stack also bounds adversarial nesting.
        var readers = new Stack<AsnReader>();
        readers.Push(contents);
        while (readers.Count > 0)
        {
            var reader = readers.Peek();
            if (!reader.HasData)
            {
                readers.Pop();
                continue;
            }
            var tag = reader.PeekTag();
            if (tag.IsConstructed)
            {
                if (readers.Count >= 32) throw InvalidCertificateFormat();
                readers.Push(ReadDerConstructed(reader, tag));
            }
            else if (tag.TagClass == TagClass.Universal)
            {
                ValidateDerPrimitive(reader, (UniversalTagNumber)tag.TagValue);
            }
            else reader.ReadEncodedValue();
        }
    }

    private static AsnReader ReadDerConstructed(AsnReader reader, Asn1Tag tag)
    {
        if (tag.HasSameClassAndValue(Asn1Tag.SetOf)) return reader.ReadSetOf();
        return reader.ReadSequence(tag.TagClass == TagClass.Universal ? null : tag);
    }

    private static void ValidateDerPrimitive(AsnReader reader, UniversalTagNumber tag)
    {
        // Typed DER readers enforce canonical content as well as tag and length encodings.
        switch (tag)
        {
            case UniversalTagNumber.Boolean: reader.ReadBoolean(); break;
            case UniversalTagNumber.Integer: reader.ReadIntegerBytes(); break;
            case UniversalTagNumber.BitString: reader.ReadBitString(out _); break;
            case UniversalTagNumber.OctetString: reader.ReadOctetString(); break;
            case UniversalTagNumber.Null: reader.ReadNull(); break;
            case UniversalTagNumber.ObjectIdentifier: reader.ReadObjectIdentifier(); break;
            case UniversalTagNumber.Enumerated: reader.ReadEnumeratedBytes(); break;
            case UniversalTagNumber.UtcTime: reader.ReadUtcTime(); break;
            case UniversalTagNumber.GeneralizedTime: reader.ReadGeneralizedTime(); break;
            case UniversalTagNumber.UTF8String:
            case UniversalTagNumber.NumericString:
            case UniversalTagNumber.PrintableString:
            case UniversalTagNumber.T61String:
            case UniversalTagNumber.IA5String:
            case UniversalTagNumber.VisibleString:
            case UniversalTagNumber.BMPString:
                reader.ReadCharacterString(tag);
                break;
            case UniversalTagNumber.UniversalString:
                // DirectoryString permits UCS-4, which AsnReader does not decode directly.
                new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true)
                    .GetCharCount(reader.PeekContentBytes().Span);
                reader.ReadEncodedValue();
                break;
            default: throw InvalidCertificateFormat();
        }
    }

    private static void AddDistinct(List<X509Certificate2> certificates, X509Certificate2 candidate)
    {
        if (certificates.Any(certificate => certificate.RawData.AsSpan().SequenceEqual(candidate.RawData))) candidate.Dispose();
        else certificates.Add(candidate);
    }

    private static string Fingerprint(X509Certificate2 certificate)
        => Convert.ToHexString(certificate.GetCertHash(HashAlgorithmName.SHA256)).ToLowerInvariant();

    private static InvalidFormatException InvalidCertificateFormat() => new("Wrong format of user authentication certificate.");
    private static UnauthorizedException InvalidTrust() => new("User client certificate is invalid.", new Exception("Certificate trust validation failed."));
}
