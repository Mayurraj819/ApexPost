using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Services;

public interface IIdentityVerificationService
{
    Task<IdentityVerificationResult> VerifyAsync(IdentityVerificationRequest request, CancellationToken cancellationToken = default);
}
