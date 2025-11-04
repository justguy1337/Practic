using CharityManagement.Api.Dtos;
using CharityManagement.Api.Models;

namespace CharityManagement.Api.Extensions;

public static class MappingExtensions
{
    public static ProjectSummaryDto ToSummaryDto(this Project project) =>
        new(
            project.Id,
            project.Code,
            project.Name,
            project.Status,
            project.GoalAmount,
            project.CollectedAmount,
            project.StartDate,
            project.EndDate,
            project.IsArchived);

    public static ProjectDetailsDto ToDetailsDto(this Project project) =>
        new(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            project.Status,
            project.GoalAmount,
            project.CollectedAmount,
            project.StartDate,
            project.EndDate,
            project.IsArchived,
            project.Volunteers
                .Select(x => new ProjectVolunteerDto(
                    x.VolunteerId,
                    $"{x.Volunteer.FirstName} {x.Volunteer.LastName}".Trim(),
                    x.Role,
                    x.AssignedAt))
                .ToList(),
            project.Donations
                .OrderByDescending(x => x.DonatedAt)
                .Select(x => x.ToDto())
                .ToList(),
            project.Reports
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.ToDto())
                .ToList());

    public static DonationDto ToDto(this Donation donation) =>
        new(
            donation.Id,
            donation.ProjectId,
            donation.VolunteerId,
            donation.Amount,
            donation.Method,
            donation.DonorName,
            donation.DonorEmail,
            donation.DonorPhone,
            donation.PaymentReference,
            donation.DonatedAt);

    public static VolunteerSummaryDto ToSummaryDto(this Volunteer volunteer) =>
        new(
            volunteer.Id,
            volunteer.FirstName,
            volunteer.LastName,
            volunteer.Email,
            volunteer.PhoneNumber,
            volunteer.TwoFactorEnabled,
            volunteer.IsActive);

    public static VolunteerDetailsDto ToDetailsDto(this Volunteer volunteer) =>
        new(
            volunteer.Id,
            volunteer.FirstName,
            volunteer.LastName,
            volunteer.Email,
            volunteer.PhoneNumber,
            volunteer.TwoFactorEnabled,
            volunteer.IsActive,
            volunteer.JoinedAt,
            volunteer.Projects
                .Select(x => x.Project.ToSummaryDto())
                .ToList());

    public static ReportDto ToDto(this Report report) =>
        new(
            report.Id,
            report.ProjectId,
            report.CreatedById,
            report.Title,
            report.Content,
            report.CreatedAt,
            report.PublishedAt,
            report.IsPublic);

    public static NotificationDto ToDto(this Notification notification) =>
        new(
            notification.Id,
            notification.Channel,
            notification.Title,
            notification.Message,
            notification.IsSent,
            notification.CreatedAt,
            notification.SentAt,
            notification.ProjectId,
            notification.VolunteerId,
            notification.DonationId);
}
