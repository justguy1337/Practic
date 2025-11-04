using CharityManagement.Api.Models.Enums;

namespace CharityManagement.Api.Dtos;

public record DonationDto(
    Guid Id,
    Guid ProjectId,
    Guid? VolunteerId,
    decimal Amount,
    DonationMethod Method,
    string? DonorName,
    string? DonorEmail,
    string? DonorPhone,
    string? PaymentReference,
    DateTimeOffset DonatedAt);

public record CreateDonationRequest(
    Guid ProjectId,
    Guid? VolunteerId,
    decimal Amount,
    DonationMethod Method,
    string? DonorName,
    string? DonorEmail,
    string? DonorPhone,
    string? PaymentReference);
