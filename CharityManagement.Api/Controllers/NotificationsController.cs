using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Extensions;
using CharityManagement.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
        [FromQuery] bool? sent,
        [FromQuery] NotificationChannel? channel,
        [FromQuery] Guid? projectId)
    {
        var query = _dbContext.Notifications.AsNoTracking().AsQueryable();

        if (sent is not null)
        {
            query = query.Where(x => x.IsSent == sent);
        }

        if (channel is not null)
        {
            query = query.Where(x => x.Channel == channel);
        }

        if (projectId is not null && projectId != Guid.Empty)
        {
            query = query.Where(x => x.ProjectId == projectId);
        }

        var notifications = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(notifications.Select(x => x.ToDto()));
    }

    [HttpPatch("{id:guid}/send")]
    public async Task<IActionResult> MarkAsSent(Guid id)
    {
        var notification = await _dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == id);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsSent = true;
        notification.SentAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

