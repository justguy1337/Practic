namespace CharityManagement.Api.Options;

public class SeedSettings
{
    public const string SectionName = "Seed";

    public string AdminUserName { get; set; } = "admin";
    public string AdminEmail { get; set; } = "admin@charity.local";
    public string AdminPassword { get; set; } = "ChangeMe123!";
}
