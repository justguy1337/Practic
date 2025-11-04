using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = now.ToUniversalTime();
        var monthStart = new DateTimeOffset(new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        var monthsBack = utcNow.AddMonths(-5);
        var seriesStart = new DateTimeOffset(new DateTime(monthsBack.Year, monthsBack.Month, 1, 0, 0, 0, DateTimeKind.Utc));

        var totalProjects = await _dbContext.Projects.CountAsync();
        var activeProjects = await _dbContext.Projects.CountAsync(x => x.Status == ProjectStatus.Active);
        var completedProjects = await _dbContext.Projects.CountAsync(x => x.Status == ProjectStatus.Completed);
        var cancelledProjects = await _dbContext.Projects.CountAsync(x => x.Status == ProjectStatus.Cancelled);

        var totalRaised = await _dbContext.Donations.SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var monthlyRaised = await _dbContext.Donations
            .Where(x => x.DonatedAt >= monthStart)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var successRate = totalProjects == 0
            ? 0m
            : Math.Round((decimal)completedProjects / totalProjects * 100, 2, MidpointRounding.AwayFromZero);

        var series = await _dbContext.Donations
            .Where(x => x.DonatedAt >= seriesStart)
            .GroupBy(x => new { x.DonatedAt.Year, x.DonatedAt.Month })
            .Select(g => new MonthlyDonationPoint(g.Key.Year, g.Key.Month, g.Sum(x => x.Amount)))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var result = new DashboardSummaryDto(
            totalProjects,
            activeProjects,
            completedProjects,
            cancelledProjects,
            totalRaised,
            monthlyRaised,
            successRate,
            series);

        return Ok(result);
    }
}

