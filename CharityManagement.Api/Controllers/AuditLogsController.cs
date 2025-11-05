using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Administrator)]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 10, 500);

        IQueryable<AuditLog> query = _dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var normalized = entityName.Trim().ToLowerInvariant();
            query = query.Where(x => x.EntityName.ToLower() == normalized);
        }

        if (entityId is Guid entity)
        {
            query = query.Where(x => x.EntityId == entity);
        }

        if (from is not null)
        {
            query = query.Where(x => x.CreatedAt >= from);
        }

        if (to is not null)
        {
            query = query.Where(x => x.CreatedAt <= to);
        }

        var logs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto(
                x.Id,
                x.EntityName,
                x.EntityId,
                x.Action,
                x.Changes,
                x.PerformedBy,
                x.UserId,
                x.CreatedAt))
            .ToListAsync();

        return Ok(logs);
    }
}
