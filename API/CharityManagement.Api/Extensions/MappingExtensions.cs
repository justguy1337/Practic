using CharityManagement.Api.Dtos;
using CharityManagement.Api.Models.Enums;
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
            ResolveStatusColor(project.Status),
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
            ResolveStatusColor(project.Status),
            project.GoalAmount,
            project.CollectedAmount,
            project.StartDate,
            project.EndDate,
            project.IsArchived,
            project.Members
                .Select(x => new ProjectMemberDto(
                    x.UserId,
                    $"{x.User.FirstName} {x.User.LastName}".Trim(),
                    x.AssignmentRole,
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
            donation.UserId,
            donation.Amount,
            donation.Method,
            donation.DonorName,
            donation.DonorEmail,
            donation.DonorPhone,
            donation.PaymentReference,
            donation.DonatedAt);

    public static UserSummaryDto ToSummaryDto(this User user) =>
        new(
            user.Id,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Role.Name,
            user.TwoFactorEnabled,
            user.IsActive);

    public static UserDetailsDto ToDetailsDto(this User user) =>
        new(
            user.Id,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Role.Name,
            user.TwoFactorEnabled,
            user.IsActive,
            user.JoinedAt,
            user.Projects
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
            notification.UserId,
            notification.DonationId);

    private static string ResolveStatusColor(ProjectStatus status) =>
        status switch
        {
            ProjectStatus.Active => "#22C55E",
            ProjectStatus.Completed => "#9CA3AF",
            ProjectStatus.Cancelled => "#EF4444",
            ProjectStatus.Draft => "#FACC15",
            _ => "#6B7280"
        };
}
