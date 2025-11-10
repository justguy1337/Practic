using CharityManagement.Api.Options;
using Microsoft.Extensions.Options;
using OtpNet;

namespace CharityManagement.Api.Services.Auth;

public class TwoFactorService
{
    private readonly TwoFactorSettings _settings;

    public TwoFactorService(IOptions<TwoFactorSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);
        return totp.VerifyTotp(code.Trim(), out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    public string BuildProvisioningUri(string secret, string userName)
    {
        var issuer = Uri.EscapeDataString(_settings.Issuer);
        var label = Uri.EscapeDataString($"{_settings.Issuer}:{userName}");
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}";
    }
}
