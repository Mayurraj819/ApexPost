using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;

namespace ApexPost.Web.Services;
public sealed class CurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    public string Id => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
}
