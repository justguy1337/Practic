namespace CharityManagement.Api.Models;

public class ProjectVolunteer
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid VolunteerId { get; set; }
    public Volunteer Volunteer { get; set; } = null!;
    public string Role { get; set; } = "Member";
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}
