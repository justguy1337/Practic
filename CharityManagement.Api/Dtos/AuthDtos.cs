namespace CharityManagement.Api.Dtos;

public record LoginRequestDto(string UserNameOrEmail, string Password, string? TwoFactorCode);

public record LoginResponseDto(string? AccessToken, DateTimeOffset? ExpiresAt, string Role, bool RequiresTwoFactor);

public record TwoFactorInitializeResponseDto(string Secret, string ProvisioningUri);

public record TwoFactorVerifyRequestDto(string Code);

public record TwoFactorDisableRequestDto(string Password);

public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);
