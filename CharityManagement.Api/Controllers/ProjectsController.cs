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
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ProjectsController(ApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectSummaryDto>>> GetProjects(
        [FromQuery] ProjectStatus? status,
        [FromQuery] Guid? userId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool desc = false)
    {
        IQueryable<Project> query = _dbContext.Projects.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (userId is not null && userId != Guid.Empty)
        {
            query = query.Where(x => x.Members.Any(m => m.UserId == userId));
        }

        if (from is not null)
        {
            query = query.Where(x => x.StartDate >= from);
        }

        if (to is not null)
        {
            query = query.Where(x => x.EndDate <= to || x.EndDate == null);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.Code.ToLower().Contains(term) ||
                x.Description.ToLower().Contains(term));
        }

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase) && _currentUser.UserId is Guid currentUserId)
        {
            query = query.Where(x => x.Members.Any(m => m.UserId == currentUserId));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "goal" => desc ? query.OrderByDescending(x => x.GoalAmount) : query.OrderBy(x => x.GoalAmount),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "collected" => desc ? query.OrderByDescending(x => x.CollectedAmount) : query.OrderBy(x => x.CollectedAmount),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };

        var projects = await query
            .Select(x => x.ToSummaryDto())
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailsDto>> GetProject(Guid id)
    {
        var project = await _dbContext.Projects
            .Include(x => x.Members)
                .ThenInclude(x => x.User)
            .Include(x => x.Donations)
            .Include(x => x.Reports)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        if (string.Equals(_currentUser.Role, RoleNames.Volunteer, StringComparison.OrdinalIgnoreCase))
        {
            if (_currentUser.UserId is not Guid currentUserId || project.Members.All(x => x.UserId != currentUserId))
            {
                return Forbid();
            }
        }

        return Ok(project.ToDetailsDto());
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<ActionResult<ProjectDetailsDto>> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (request.GoalAmount <= 0)
        {
            return BadRequest("Сумма сбора должна быть положительной.");
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _dbContext.Projects.AnyAsync(x => x.Code == normalizedCode);
        if (exists)
        {
            return Conflict($"Проект с кодом {normalizedCode} уже существует.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            GoalAmount = decimal.Round(request.GoalAmount, 2, MidpointRounding.AwayFromZero),
            CollectedAmount = 0,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = ProjectStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project.ToDetailsDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<ActionResult<ProjectDetailsDto>> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        var project = await _dbContext.Projects
            .Include(x => x.Donations)
            .Include(x => x.Members)
                .ThenInclude(x => x.User)
            .Include(x => x.Reports)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        if (project.Status == ProjectStatus.Active &&
            (project.GoalAmount != request.GoalAmount || project.EndDate != request.EndDate))
        {
            return BadRequest("Нельзя изменить целевую сумму или дату окончания активного проекта.");
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description.Trim();
        project.StartDate = request.StartDate;
        project.Status = request.Status;
        project.IsArchived = request.IsArchived;

        if (project.Status != ProjectStatus.Active)
        {
            project.GoalAmount = decimal.Round(request.GoalAmount, 2, MidpointRounding.AwayFromZero);
            project.EndDate = request.EndDate;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(project.ToDetailsDto());
    }

    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<ActionResult<ProjectDetailsDto>> AssignUser(Guid id, [FromBody] AssignUserRequest request)
    {
        var project = await _dbContext.Projects
            .Include(x => x.Members)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound("Проект не найден.");
        }

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == request.UserId);
        if (user is null)
        {
            return NotFound("Пользователь не найден.");
        }

        var alreadyAssigned = project.Members.Any(x => x.UserId == user.Id);
        if (alreadyAssigned)
        {
            return Conflict("Пользователь уже привязан к проекту.");
        }

        project.Members.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = user.Id,
            AssignmentRole = string.IsNullOrWhiteSpace(request.AssignmentRole) ? "Member" : request.AssignmentRole.Trim(),
            AssignedAt = DateTimeOffset.UtcNow,
            User = user
        });

        await _dbContext.SaveChangesAsync();

        var result = await _dbContext.Projects
            .Include(x => x.Members).ThenInclude(x => x.User)
            .Include(x => x.Donations)
            .Include(x => x.Reports)
            .AsNoTracking()
            .FirstAsync(x => x.Id == id);

        return Ok(result.ToDetailsDto());
    }

    [HttpDelete("{projectId:guid}/members/{userId:guid}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> RemoveUser(Guid projectId, Guid userId)
    {
        var link = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.UserId == userId);

        if (link is null)
        {
            return NotFound();
        }

        _dbContext.ProjectMembers.Remove(link);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var project = await _dbContext.Projects
            .Include(x => x.Donations)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        if (project.Donations.Any())
        {
            return BadRequest("Проект нельзя удалить: есть связанные пожертвования.");
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
