using CharityManagement.Api.Models.Enums;

namespace CharityManagement.Api.Models;

public class Notification
{
    public Guid Id { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? DonationId { get; set; }
    public Donation? Donation { get; set; }
}
