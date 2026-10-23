using Microsoft.AspNetCore.Authorization;

namespace ApexPost.Web.Security;

public sealed class SameBranchRequirement : IAuthorizationRequirement
{
}
