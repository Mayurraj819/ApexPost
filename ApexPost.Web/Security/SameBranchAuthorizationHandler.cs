using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ApexPost.Web.Security;

public sealed class SameBranchAuthorizationHandler : AuthorizationHandler<SameBranchRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SameBranchRequirement requirement)
    {
        if (context.User.IsInRole("Administrator"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var branchClaim = context.User.FindFirstValue(WebClaimTypes.BranchId);
        if (context.Resource is HttpContext httpContext &&
            Guid.TryParse(branchClaim, out var userBranchId) &&
            Guid.TryParse(httpContext.Request.RouteValues["branchId"]?.ToString(), out var routeBranchId) &&
            userBranchId == routeBranchId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
