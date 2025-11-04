namespace CharityManagement.Api.Dtos;

public record DashboardSummaryDto(
    int TotalProjects,
    int ActiveProjects,
    int CompletedProjects,
    int CancelledProjects,
    decimal TotalRaised,
    decimal MonthlyRaised,
    decimal SuccessRate,
    IReadOnlyCollection<MonthlyDonationPoint> DonationSeries);

public record MonthlyDonationPoint(int Year, int Month, decimal Amount);
