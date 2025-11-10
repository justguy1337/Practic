namespace CharityManagement.Api.Services.Security;

public sealed class NullCurrentUserService : ICurrentUserService
{
    public static readonly NullCurrentUserService Instance = new();

    private NullCurrentUserService()
    {
    }

    public Guid? UserId => null;
    public string? UserName => null;
    public string? Role => null;
    public bool IsInRole(string role) => false;
}
