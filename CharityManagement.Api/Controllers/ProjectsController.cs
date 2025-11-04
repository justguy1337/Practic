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
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ProjectsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectSummaryDto>>> GetProjects(
        [FromQuery] ProjectStatus? status,
        [FromQuery] Guid? volunteerId,
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

        if (volunteerId is not null && volunteerId != Guid.Empty)
        {
            query = query.Where(x => x.Volunteers.Any(v => v.VolunteerId == volunteerId));
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
            .Include(x => x.Volunteers)
                .ThenInclude(x => x.Volunteer)
            .Include(x => x.Donations)
            .Include(x => x.Reports)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return project is null
            ? NotFound()
            : Ok(project.ToDetailsDto());
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDetailsDto>> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (request.GoalAmount <= 0)
        {
            return BadRequest("Целевая сумма должна быть больше нуля.");
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
    public async Task<ActionResult<ProjectDetailsDto>> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        var project = await _dbContext.Projects
            .Include(x => x.Donations)
            .Include(x => x.Volunteers)
                .ThenInclude(x => x.Volunteer)
            .Include(x => x.Reports)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        if (project.Status == ProjectStatus.Active &&
            (project.GoalAmount != request.GoalAmount || project.EndDate != request.EndDate))
        {
            return BadRequest("Нельзя изменять целевую сумму или дату окончания активного проекта.");
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

    [HttpPost("{id:guid}/volunteers")]
    public async Task<ActionResult<ProjectDetailsDto>> AssignVolunteer(Guid id, [FromBody] AssignVolunteerRequest request)
    {
        var project = await _dbContext.Projects
            .Include(x => x.Volunteers)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound("Проект не найден.");
        }

        var volunteer = await _dbContext.Volunteers.FindAsync(request.VolunteerId);
        if (volunteer is null)
        {
            return NotFound("Волонтер не найден.");
        }

        var alreadyAssigned = project.Volunteers.Any(x => x.VolunteerId == volunteer.Id);
        if (alreadyAssigned)
        {
            return Conflict("Волонтер уже прикреплен к проекту.");
        }

        project.Volunteers.Add(new ProjectVolunteer
        {
            ProjectId = project.Id,
            VolunteerId = volunteer.Id,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Member" : request.Role.Trim(),
            AssignedAt = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        var result = await _dbContext.Projects
            .Include(x => x.Volunteers).ThenInclude(x => x.Volunteer)
            .Include(x => x.Donations)
            .Include(x => x.Reports)
            .AsNoTracking()
            .FirstAsync(x => x.Id == id);

        return Ok(result.ToDetailsDto());
    }

    [HttpDelete("{projectId:guid}/volunteers/{volunteerId:guid}")]
    public async Task<IActionResult> RemoveVolunteer(Guid projectId, Guid volunteerId)
    {
        var link = await _dbContext.ProjectVolunteers
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.VolunteerId == volunteerId);

        if (link is null)
        {
            return NotFound();
        }

        _dbContext.ProjectVolunteers.Remove(link);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
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
            return BadRequest("Нельзя удалить проект с пожертвованиями. Вместо этого архивируйте его.");
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

