using ApexPost.Application.Abstractions.Services;
using System.Security.Claims;

namespace ApexPost.Api.Services;
/// <summary>Supplies audit identity to the reused infrastructure; JWT claims are added in Part D.</summary>
public sealed class ApiCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    /// <summary>The authenticated courier or subject, otherwise the explicit unauthenticated API identity.</summary>
    public string Id => accessor.HttpContext?.User.FindFirstValue("courierId") ?? accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous-api";
}
