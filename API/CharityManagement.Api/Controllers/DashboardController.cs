using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Models;
using CharityManagement.Api.Models.Enums;
using CharityManagement.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(ApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = now.ToUniversalTime();
        var monthStart = new DateTimeOffset(new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        var monthsBack = utcNow.AddMonths(-5);
        var seriesStart = new DateTimeOffset(new DateTime(monthsBack.Year, monthsBack.Month, 1, 0, 0, 0, DateTimeKind.Utc));

        var projectQuery = _dbContext.Projects.AsQueryable();
        var donationQuery = _dbContext.Donations.AsQueryable();

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase) && _currentUser.UserId is Guid currentUserId)
        {
            projectQuery = projectQuery.Where(p => p.Members.Any(m => m.UserId == currentUserId));
            donationQuery = donationQuery.Where(d => d.Project.Members.Any(m => m.UserId == currentUserId));
        }

        var totalProjects = await projectQuery.CountAsync();
        var activeProjects = await projectQuery.CountAsync(x => x.Status == ProjectStatus.Active);
        var completedProjects = await projectQuery.CountAsync(x => x.Status == ProjectStatus.Completed);
        var cancelledProjects = await projectQuery.CountAsync(x => x.Status == ProjectStatus.Cancelled);

        var totalRaised = await donationQuery.SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var monthlyRaised = await donationQuery
            .Where(x => x.DonatedAt >= monthStart)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var successRate = totalProjects == 0
            ? 0m
            : Math.Round((decimal)completedProjects / totalProjects * 100, 2, MidpointRounding.AwayFromZero);

        var series = await donationQuery
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
