using System.Security.Cryptography;
using System.Text;

namespace ApexPost.Domain.ValueObjects;

public sealed record IdentityDocument
{
    private IdentityDocument(string documentType, string maskedNumber, string fingerprint)
    {
        DocumentType = documentType;
        MaskedNumber = maskedNumber;
        Fingerprint = fingerprint;
    }

    public string DocumentType { get; }
    public string MaskedNumber { get; }
    public string Fingerprint { get; }

    public static IdentityDocument Create(string documentType, string documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            throw new ArgumentException("Document type is required.", nameof(documentType));
        }

        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            throw new ArgumentException("Document number is required.", nameof(documentNumber));
        }

        var normalized = documentNumber.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var fingerprint = Convert.ToHexString(bytes);
        var visibleSuffix = normalized.Length <= 4 ? normalized : normalized[^4..];
        var maskedNumber = $"***{visibleSuffix}";

        return new IdentityDocument(documentType.Trim(), maskedNumber, fingerprint);
    }
}
