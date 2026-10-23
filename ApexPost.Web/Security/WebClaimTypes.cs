namespace ApexPost.Web.Security;

public static class WebClaimTypes
{
    public const string StaffId = "apexpost:staff_id";
    public const string BranchId = "apexpost:branch_id";
}

public static class WebPolicies
{
    public const string SameBranch = "same-branch";
    public const string BranchManager = "branch-manager";
    public const string Administrator = "administrator";
}
