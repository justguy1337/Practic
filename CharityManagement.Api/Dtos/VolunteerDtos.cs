namespace CharityManagement.Api.Dtos;

public record VolunteerSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool TwoFactorEnabled,
    bool IsActive);

public record VolunteerDetailsDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool TwoFactorEnabled,
    bool IsActive,
    DateTimeOffset JoinedAt,
    IReadOnlyCollection<ProjectSummaryDto> Projects);

public record CreateVolunteerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool TwoFactorEnabled);

public record UpdateVolunteerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool TwoFactorEnabled,
    bool IsActive);
