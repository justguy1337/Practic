using CharityManagement.Api.Models;

namespace CharityManagement.Api.Services.Auth;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(User user);
}

public record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);
