using CharityManagement.Api.Models.Enums;

namespace CharityManagement.Api.Dtos;

public record ProjectSummaryDto(
    Guid Id,
    string Code,
    string Name,
    ProjectStatus Status,
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
    decimal GoalAmount,
    decimal CollectedAmount,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    bool IsArchived,
    IReadOnlyCollection<ProjectVolunteerDto> Volunteers,
    IReadOnlyCollection<DonationDto> Donations,
    IReadOnlyCollection<ReportDto> Reports);

public record ProjectVolunteerDto(Guid VolunteerId, string FullName, string Role, DateTimeOffset AssignedAt);

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

public record AssignVolunteerRequest(Guid VolunteerId, string Role);

public record ProjectFilterRequest(
    ProjectStatus? Status,
    Guid? VolunteerId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    string? SortBy,
    bool SortDescending);
