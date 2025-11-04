using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Extensions;
using CharityManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ReportsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetReports([FromQuery] Guid? projectId)
    {
        IQueryable<Report> query = _dbContext.Reports.AsNoTracking();

        if (projectId is not null && projectId != Guid.Empty)
        {
            query = query.Where(x => x.ProjectId == projectId);
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
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return report is null
            ? NotFound()
            : Ok(report.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<ReportDto>> CreateReport([FromBody] CreateReportRequest request)
    {
        var projectExists = await _dbContext.Projects.AnyAsync(x => x.Id == request.ProjectId);
        if (!projectExists)
        {
            return NotFound("Проект не найден.");
        }

        var volunteerExists = await _dbContext.Volunteers.AnyAsync(x => x.Id == request.CreatedById);
        if (!volunteerExists)
        {
            return NotFound("Автор отчета не найден.");
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            CreatedById = request.CreatedById,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            IsPublic = request.IsPublic
        };

        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReportDto>> UpdateReport(Guid id, [FromBody] UpdateReportRequest request)
    {
        var report = await _dbContext.Reports.FirstOrDefaultAsync(x => x.Id == id);
        if (report is null)
        {
            return NotFound();
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
        var report = await _dbContext.Reports.FirstOrDefaultAsync(x => x.Id == id);
        if (report is null)
        {
            return NotFound();
        }

        _dbContext.Reports.Remove(report);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

