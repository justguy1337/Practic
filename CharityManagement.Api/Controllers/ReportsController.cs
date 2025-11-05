using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Extensions;
using CharityManagement.Api.Models;
using CharityManagement.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(ApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetReports([FromQuery] Guid? projectId)
    {
        IQueryable<Report> query = _dbContext.Reports
            .Include(x => x.Project)
                .ThenInclude(x => x.Members)
            .AsNoTracking();

        if (projectId is not null && projectId != Guid.Empty)
        {
            query = query.Where(x => x.ProjectId == projectId);
        }

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase) && _currentUser.UserId is Guid currentUserId)
        {
            query = query.Where(x => x.Project.Members.Any(m => m.UserId == currentUserId));
        }

        var reports = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(reports.Select(x => x.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportDto>> GetReport(Guid id)
    {
        var report = await _dbContext.Reports
            .Include(x => x.Project)
                .ThenInclude(x => x.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (report is null)
        {
            return NotFound();
        }

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase))
        {
            if (_currentUser.UserId is not Guid currentUserId ||
                report.Project.Members.All(m => m.UserId != currentUserId))
            {
                return Forbid();
            }
        }

        return Ok(report.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<ReportDto>> CreateReport([FromBody] CreateReportRequest request)
    {
        var project = await _dbContext.Projects
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId);

        if (project is null)
        {
            return NotFound("Проект не найден.");
        }

        Guid authorId = request.CreatedById;

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase))
        {
            if (_currentUser.UserId is not Guid currentUserId)
            {
                return Forbid();
            }

            if (currentUserId != request.CreatedById)
            {
                return BadRequest("Пользователь может создавать отчёты только от своего имени.");
            }

            var assigned = await _dbContext.ProjectMembers
                .AnyAsync(x => x.ProjectId == project.Id && x.UserId == currentUserId);

            if (!assigned)
            {
                return Forbid();
            }

            authorId = currentUserId;
        }
        else
        {
            var authorExists = await _dbContext.Users.AnyAsync(x => x.Id == request.CreatedById);
            if (!authorExists)
            {
                return NotFound("Автор отчёта не найден.");
            }
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedById = authorId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            IsPublic = request.IsPublic
        };

        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync();

        var created = await _dbContext.Reports
            .AsNoTracking()
            .FirstAsync(x => x.Id == report.Id);

        return CreatedAtAction(nameof(GetReport), new { id = created.Id }, created.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReportDto>> UpdateReport(Guid id, [FromBody] UpdateReportRequest request)
    {
        var report = await _dbContext.Reports
            .Include(x => x.Project)
                .ThenInclude(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (report is null)
        {
            return NotFound();
        }

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase))
        {
            if (_currentUser.UserId is not Guid currentUserId ||
                report.CreatedById != currentUserId ||
                report.Project.Members.All(m => m.UserId != currentUserId))
            {
                return Forbid();
            }
        }

        report.Title = request.Title.Trim();
        report.Content = request.Content.Trim();
        report.IsPublic = request.IsPublic;
        report.PublishedAt = request.PublishedAt;

        await _dbContext.SaveChangesAsync();

        return Ok(report.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReport(Guid id)
    {
        var report = await _dbContext.Reports
            .Include(x => x.Project)
                .ThenInclude(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (report is null)
        {
            return NotFound();
        }

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase))
        {
            if (_currentUser.UserId is not Guid currentUserId ||
                report.CreatedById != currentUserId ||
                report.Project.Members.All(m => m.UserId != currentUserId))
            {
                return Forbid();
            }
        }

        _dbContext.Reports.Remove(report);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
