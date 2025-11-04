using CharityManagement.Api.Models.Enums;

namespace CharityManagement.Api.Models;

public class Donation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid? VolunteerId { get; set; }
    public Volunteer? Volunteer { get; set; }
    public decimal Amount { get; set; }
    public DonationMethod Method { get; set; } = DonationMethod.Unknown;
    public string? DonorName { get; set; }
    public string? DonorEmail { get; set; }
    public string? DonorPhone { get; set; }
    public string? PaymentReference { get; set; }
    public DateTimeOffset DonatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Notification? Notification { get; set; }
}
