namespace CharityManagement.Api.Services.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Role { get; }
    bool IsInRole(string role);
}
