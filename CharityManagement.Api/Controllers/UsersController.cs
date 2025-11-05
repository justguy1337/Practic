using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Extensions;
using CharityManagement.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Administrator)]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsers()
    {
        var users = await _dbContext.Users
            .Include(x => x.Role)
            .AsNoTracking()
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync();

        return Ok(users.Select(x => x.ToSummaryDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailsDto>> GetUser(Guid id)
    {
        var user = await _dbContext.Users
            .Include(x => x.Role)
            .Include(x => x.Projects)
                .ThenInclude(x => x.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return user is null
            ? NotFound()
            : Ok(user.ToDetailsDto());
    }

    [HttpPost]
    public async Task<ActionResult<UserDetailsDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Логин и пароль обязательны.");
        }

        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var usernameExists = await _dbContext.Users
            .AnyAsync(x => x.NormalizedUserName == normalizedUserName);
        if (usernameExists)
        {
            return Conflict("Указанный логин уже используется.");
        }

        var emailExists = await _dbContext.Users
            .AnyAsync(x => x.NormalizedEmail == normalizedEmail);
        if (emailExists)
        {
            return Conflict("Указанный email уже используется.");
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId);
        if (role is null)
        {
            return NotFound("Роль не найдена.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName.Trim(),
            NormalizedUserName = normalizedUserName,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            TwoFactorEnabled = request.TwoFactorEnabled,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var created = await _dbContext.Users
            .Include(x => x.Role)
            .Include(x => x.Projects)
                .ThenInclude(x => x.Project)
            .AsNoTracking()
            .FirstAsync(x => x.Id == user.Id);

        return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created.ToDetailsDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDetailsDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _dbContext.Users
            .Include(x => x.Role)
            .Include(x => x.Projects)
                .ThenInclude(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var emailTaken = await _dbContext.Users
            .AnyAsync(x => x.Id != id && x.NormalizedEmail == normalizedEmail);
        if (emailTaken)
        {
            return Conflict("Указанный email уже используется.");
        }

        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();
        var userNameTaken = await _dbContext.Users
            .AnyAsync(x => x.Id != id && x.NormalizedUserName == normalizedUserName);
        if (userNameTaken)
        {
            return Conflict("Указанный логин уже используется.");
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId);
        if (role is null)
        {
            return NotFound("Роль не найдена.");
        }

        user.UserName = request.UserName.Trim();
        user.NormalizedUserName = normalizedUserName;
        user.Email = request.Email.Trim();
        user.NormalizedEmail = normalizedEmail;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.TwoFactorEnabled = request.TwoFactorEnabled;
        user.IsActive = request.IsActive;
        user.RoleId = role.Id;
        user.Role = role;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        }

        await _dbContext.SaveChangesAsync();

        var updated = await _dbContext.Users
            .Include(x => x.Role)
            .Include(x => x.Projects)
                .ThenInclude(x => x.Project)
            .AsNoTracking()
            .FirstAsync(x => x.Id == id);

        return Ok(updated.ToDetailsDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _dbContext.Users
            .Include(x => x.Donations)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        if (user.Donations.Any())
        {
            user.IsActive = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
            return Ok("У пользователя есть связанные пожертвования, профиль переведен в неактивное состояние.");
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
