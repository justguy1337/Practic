using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Extensions;
using CharityManagement.Api.Models;
using CharityManagement.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DonationsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public DonationsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DonationDto>>> GetDonations([FromQuery] Guid? projectId)
    {
        IQueryable<Donation> query = _dbContext.Donations.AsNoTracking();

        if (projectId is not null && projectId != Guid.Empty)
        {
            query = query.Where(x => x.ProjectId == projectId);
        }

        var donations = await query
            .OrderByDescending(x => x.DonatedAt)
            .ToListAsync();

        return Ok(donations.Select(x => x.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DonationDto>> GetDonation(Guid id)
    {
        var donation = await _dbContext.Donations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return donation is null
            ? NotFound()
            : Ok(donation.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<DonationDto>> CreateDonation([FromBody] CreateDonationRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Сумма пожертвования должна быть больше нуля.");
        }

        var project = await _dbContext.Projects
            .Include(x => x.Notifications)
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId);

        if (project is null)
        {
            return NotFound("Проект не найден.");
        }

        Volunteer? volunteer = null;
        if (request.VolunteerId is { } volunteerId && volunteerId != Guid.Empty)
        {
            volunteer = await _dbContext.Volunteers.FindAsync(volunteerId);
            if (volunteer is null)
            {
                return NotFound("Указанный волонтер не найден.");
            }
        }

        var donation = new Donation
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VolunteerId = volunteer?.Id,
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Method = request.Method,
            DonorName = request.DonorName?.Trim(),
            DonorEmail = request.DonorEmail?.Trim(),
            DonorPhone = request.DonorPhone?.Trim(),
            PaymentReference = request.PaymentReference?.Trim(),
            DonatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Donations.Add(donation);

        project.CollectedAmount += donation.Amount;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VolunteerId = volunteer?.Id,
            DonationId = donation.Id,
            Channel = NotificationChannel.Email,
            Title = $"Новое пожертвование в проект {project.Name}",
            Message = $"Получено пожертвование {donation.Amount:C} от {donation.DonorName ?? "анонимного жертвователя"}.",
            CreatedAt = DateTimeOffset.UtcNow,
            IsSent = false
        };

        _dbContext.Notifications.Add(notification);

        await _dbContext.SaveChangesAsync();

        var created = await _dbContext.Donations.AsNoTracking().FirstAsync(x => x.Id == donation.Id);

        return CreatedAtAction(nameof(GetDonation), new { id = donation.Id }, created.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDonation(Guid id)
    {
        var donation = await _dbContext.Donations.FirstOrDefaultAsync(x => x.Id == id);

        if (donation is null)
        {
            return NotFound();
        }

        var project = await _dbContext.Projects.FindAsync(donation.ProjectId);
        if (project is not null)
        {
            project.CollectedAmount = Math.Max(0, project.CollectedAmount - donation.Amount);
        }

        _dbContext.Donations.Remove(donation);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

