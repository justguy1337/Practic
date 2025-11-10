namespace CharityManagement.Api.Dtos;

public record ReportDto(
    Guid Id,
    Guid ProjectId,
    Guid CreatedById,
    string Title,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    bool IsPublic);

public record CreateReportRequest(
    Guid ProjectId,
    Guid CreatedById,
    string Title,
    string Content,
    bool IsPublic);

public record UpdateReportRequest(
    string Title,
    string Content,
    bool IsPublic,
    DateTimeOffset? PublishedAt);
