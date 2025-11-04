namespace CharityManagement.Api.Models;

public class Volunteer
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ProjectVolunteer> Projects { get; set; } = new List<ProjectVolunteer>();
    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
