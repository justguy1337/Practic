using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CharityManagement.Api.Services.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId => ResolveGuidClaim(ClaimTypes.NameIdentifier) ?? ResolveGuidClaim(JwtRegisteredClaimNames.Sub);

    public string? UserName => ResolveStringClaim(ClaimTypes.Name) ?? ResolveStringClaim(JwtRegisteredClaimNames.Email);

    public string? Role => ResolveStringClaim(ClaimTypes.Role);

    public bool IsInRole(string role) =>
        Role is not null && string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);

    private Guid? ResolveGuidClaim(string claimType)
    {
        var value = ResolveStringClaim(claimType);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private string? ResolveStringClaim(string claimType)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.Claims?.FirstOrDefault(x => x.Type == claimType)?.Value;
    }
}
