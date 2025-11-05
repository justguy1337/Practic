using CharityManagement.Api.Models;

namespace CharityManagement.Api.Dtos;

public record UserSummaryDto(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool TwoFactorEnabled,
    bool IsActive);

public record UserDetailsDto(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool TwoFactorEnabled,
    bool IsActive,
    DateTimeOffset JoinedAt,
    IReadOnlyCollection<ProjectSummaryDto> Projects);

public record CreateUserRequest(
    string UserName,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    bool TwoFactorEnabled);

public record UpdateUserRequest(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    bool TwoFactorEnabled,
    bool IsActive,
    string? NewPassword);
