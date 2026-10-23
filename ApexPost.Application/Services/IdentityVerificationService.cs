using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;

namespace ApexPost.Application.Services;

public sealed class IdentityVerificationService : IIdentityVerificationService
{
    public async Task<IdentityVerificationResult> VerifyAsync(IdentityVerificationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.ExpectedFingerprint))
        {
            throw new ArgumentException("Expected fingerprint is required.", nameof(request));
        }

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        var verified = string.Equals(request.Document.Fingerprint, request.ExpectedFingerprint.Trim(), StringComparison.OrdinalIgnoreCase);
        return new IdentityVerificationResult(verified, request.Document.MaskedNumber, request.Document.DocumentType);
    }
}
