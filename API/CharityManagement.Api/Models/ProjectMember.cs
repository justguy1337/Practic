namespace CharityManagement.Api.Models;

public class ProjectMember
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string AssignmentRole { get; set; } = "Member";
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}
