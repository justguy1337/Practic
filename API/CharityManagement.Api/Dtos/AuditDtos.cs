namespace CharityManagement.Api.Dtos;

public record AuditLogDto(
    Guid Id,
    string EntityName,
    Guid EntityId,
    string Action,
    string Changes,
    string PerformedBy,
    Guid? UserId,
    DateTimeOffset CreatedAt);

public record AuditLogFilterDto(
    string? EntityName,
    Guid? EntityId,
    DateTimeOffset? From,
    DateTimeOffset? To);
