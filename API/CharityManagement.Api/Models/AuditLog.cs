namespace CharityManagement.Api.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = "system";
    public Guid? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
