namespace ApexPost.Web.Security;

public sealed class DemoAccountOptions
{
    public List<DemoAccount> Accounts { get; init; } = [];
}

public sealed class DemoAccount
{
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string BranchId { get; init; } = string.Empty;
    public string StaffId { get; init; } = string.Empty;
    public string PasswordEnvironmentVariable { get; init; } = string.Empty;
}
