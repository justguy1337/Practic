using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Extensions;
using CharityManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VolunteersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public VolunteersController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VolunteerSummaryDto>>> GetVolunteers()
    {
        var volunteers = await _dbContext.Volunteers
            .AsNoTracking()
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync();

        return Ok(volunteers.Select(x => x.ToSummaryDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VolunteerDetailsDto>> GetVolunteer(Guid id)
    {
        var volunteer = await _dbContext.Volunteers
            .Include(x => x.Projects)
                .ThenInclude(x => x.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return volunteer is null
            ? NotFound()
            : Ok(volunteer.ToDetailsDto());
    }

    [HttpPost]
    public async Task<ActionResult<VolunteerDetailsDto>> CreateVolunteer([FromBody] CreateVolunteerRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await _dbContext.Volunteers.AnyAsync(x => x.Email.ToLower() == normalizedEmail);
        if (exists)
        {
            return Conflict("Волонтер с таким email уже существует.");
        }

        var volunteer = new Volunteer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber?.Trim(),
            TwoFactorEnabled = request.TwoFactorEnabled,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Volunteers.Add(volunteer);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVolunteer), new { id = volunteer.Id }, volunteer.ToDetailsDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VolunteerDetailsDto>> UpdateVolunteer(Guid id, [FromBody] UpdateVolunteerRequest request)
    {
        var volunteer = await _dbContext.Volunteers
            .Include(x => x.Projects)
                .ThenInclude(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (volunteer is null)
        {
            return NotFound();
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await _dbContext.Volunteers
            .AnyAsync(x => x.Id != id && x.Email.ToLower() == normalizedEmail);

        if (emailTaken)
        {
            return Conflict("Указанный email уже используется.");
        }

        volunteer.FirstName = request.FirstName.Trim();
        volunteer.LastName = request.LastName.Trim();
        volunteer.Email = normalizedEmail;
        volunteer.PhoneNumber = request.PhoneNumber?.Trim();
        volunteer.TwoFactorEnabled = request.TwoFactorEnabled;
        volunteer.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return Ok(volunteer.ToDetailsDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteVolunteer(Guid id)
    {
        var volunteer = await _dbContext.Volunteers
            .Include(x => x.Donations)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (volunteer is null)
        {
            return NotFound();
        }

        if (volunteer.Donations.Any())
        {
            volunteer.IsActive = false;
            await _dbContext.SaveChangesAsync();
            return Ok("Волонтер имеет пожертвования, поэтому он деактивирован вместо удаления.");
        }

        _dbContext.Volunteers.Remove(volunteer);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

