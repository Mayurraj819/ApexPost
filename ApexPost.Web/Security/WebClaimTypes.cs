namespace ApexPost.Web.Security;

public static class WebClaimTypes
{
    public const string StaffId = "apexpost:staff_id";
    public const string BranchId = "apexpost:branch_id";
    public const string BranchCode = "apexpost:branch_code";
    public const string Role = "apexpost:role";
}

public static class WebPolicies
{
    public const string SameBranch = "same-branch";
    public const string BranchManager = "branch-manager";
    public const string BranchManagerOnly = "branch-manager-only";
    public const string Administrator = "administrator";
}
