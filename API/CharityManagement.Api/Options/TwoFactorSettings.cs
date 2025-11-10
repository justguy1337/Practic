namespace CharityManagement.Api.Options;

public class TwoFactorSettings
{
    public const string SectionName = "TwoFactor";

    public string Issuer { get; set; } = "CharityManagement";
}
