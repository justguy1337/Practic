using CharityManagement.Api.Models.Enums;

namespace CharityManagement.Api.Dtos;

public record NotificationDto(
    Guid Id,
    NotificationChannel Channel,
    string Title,
    string Message,
    bool IsSent,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    Guid? ProjectId,
    Guid? VolunteerId,
    Guid? DonationId);
