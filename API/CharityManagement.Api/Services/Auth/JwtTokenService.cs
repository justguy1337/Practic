using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CharityManagement.Api.Models;
using CharityManagement.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CharityManagement.Api.Services.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly byte[] _signingKeyBytes;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
        _signingKeyBytes = Encoding.UTF8.GetBytes(_settings.SigningKey);
    }

    public JwtTokenResult GenerateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_settings.SigningKey) || _signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be configured and at least 32 bytes length.");
        }

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenLifetimeMinutes <= 0 ? 60 : _settings.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role.Name),
            new(JwtRegisteredClaimNames.Email, user.Email)
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(_signingKeyBytes), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.WriteToken(token);

        return new JwtTokenResult(accessToken, expires);
    }
}
