using CharityManagement.Api.Models.Enums;

namespace CharityManagement.Api.Dtos;

public record ProjectSummaryDto(
    Guid Id,
    string Code,
    string Name,
    ProjectStatus Status,
    string StatusColor,
    decimal GoalAmount,
    decimal CollectedAmount,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    bool IsArchived);

public record ProjectDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    ProjectStatus Status,
    string StatusColor,
    decimal GoalAmount,
    decimal CollectedAmount,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    bool IsArchived,
    IReadOnlyCollection<ProjectMemberDto> Members,
    IReadOnlyCollection<DonationDto> Donations,
    IReadOnlyCollection<ReportDto> Reports);

public record ProjectMemberDto(Guid UserId, string FullName, string AssignmentRole, DateTimeOffset AssignedAt);

public record CreateProjectRequest(
    string Code,
    string Name,
    string Description,
    decimal GoalAmount,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate);

public record UpdateProjectRequest(
    string Name,
    string Description,
    decimal GoalAmount,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    ProjectStatus Status,
    bool IsArchived);

public record AssignUserRequest(Guid UserId, string AssignmentRole);

public record ProjectFilterRequest(
    ProjectStatus? Status,
    Guid? UserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    string? SortBy,
    bool SortDescending);
