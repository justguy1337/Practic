using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Extensions;
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
public class DonationsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public DonationsController(ApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DonationDto>>> GetDonations([FromQuery] Guid? projectId)
    {
        IQueryable<Donation> query = _dbContext.Donations
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

        var donations = await query
            .OrderByDescending(x => x.DonatedAt)
            .ToListAsync();

        return Ok(donations.Select(x => x.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DonationDto>> GetDonation(Guid id)
    {
        var donation = await _dbContext.Donations
            .Include(x => x.Project)
                .ThenInclude(x => x.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (donation is null)
        {
            return NotFound();
        }

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase))
        {
            if (_currentUser.UserId is not Guid currentUserId ||
                donation.Project.Members.All(m => m.UserId != currentUserId))
            {
                return Forbid();
            }
        }

        return Ok(donation.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<DonationDto>> CreateDonation([FromBody] CreateDonationRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Сумма пожертвования должна быть положительной.");
        }

        var project = await _dbContext.Projects
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId);

        if (project is null)
        {
            return NotFound("Проект не найден.");
        }

        Guid? userId = request.UserId;

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase))
        {
            if (_currentUser.UserId is not Guid currentUserId)
            {
                return Forbid();
            }

            userId = currentUserId;
        }

        User? user = null;
        if (userId is Guid resolvedUserId)
        {
            user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == resolvedUserId);

            if (user is null)
            {
                return NotFound("Пользователь не найден.");
            }

            var assigned = await _dbContext.ProjectMembers
                .AnyAsync(x => x.ProjectId == project.Id && x.UserId == resolvedUserId);

            if (!assigned)
            {
                return Forbid();
            }
        }

        var donation = new Donation
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = user?.Id,
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

        var notifications = BuildDonationNotifications(project, user, donation).ToList();
        if (notifications.Count > 0)
        {
            _dbContext.Notifications.AddRange(notifications);
        }

        await _dbContext.SaveChangesAsync();

        var created = await _dbContext.Donations
            .AsNoTracking()
            .FirstAsync(x => x.Id == donation.Id);

        return CreatedAtAction(nameof(GetDonation), new { id = donation.Id }, created.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.Administrator)]
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

        var relatedNotifications = await _dbContext.Notifications
            .Where(x => x.DonationId == donation.Id)
            .ToListAsync();

        if (relatedNotifications.Count > 0)
        {
            _dbContext.Notifications.RemoveRange(relatedNotifications);
        }

        _dbContext.Donations.Remove(donation);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static IEnumerable<Notification> BuildDonationNotifications(Project project, User? user, Donation donation)
    {
        var title = $"Новое пожертвование в проект {project.Name}";
        var donorName = string.IsNullOrWhiteSpace(donation.DonorName) ? "Аноним" : donation.DonorName;
        var message = $"Зарегистрировано пожертвование {donation.Amount:C} от {donorName}.";

        yield return new Notification
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = user?.Id,
            DonationId = donation.Id,
            Channel = NotificationChannel.Email,
            Title = title,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow,
            IsSent = false
        };

        yield return new Notification
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = user?.Id,
            DonationId = donation.Id,
            Channel = NotificationChannel.Sms,
            Title = title,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow,
            IsSent = false
        };
    }
}
