using CharityManagement.Api.Data;
using CharityManagement.Api.Dtos;
using CharityManagement.Api.Models;
using CharityManagement.Api.Services.Auth;
using CharityManagement.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly TwoFactorService _twoFactorService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IPasswordHasher<User> passwordHasher,
        TwoFactorService twoFactorService,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _twoFactorService = twoFactorService;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Необходимо указать логин/email и пароль.");
        }

        var lookup = request.UserNameOrEmail.Trim().ToUpperInvariant();

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.NormalizedUserName == lookup || x.NormalizedEmail == lookup);

        if (user is null || !user.IsActive)
        {
            return Unauthorized("Неверные учетные данные.");
        }

        var passwordCheck = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordCheck == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Неверные учетные данные.");
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode) || string.IsNullOrWhiteSpace(user.TwoFactorSecret) ||
                !_twoFactorService.ValidateCode(user.TwoFactorSecret, request.TwoFactorCode))
            {
                return Unauthorized(new LoginResponseDto(null, null, user.Role.Name, true));
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(new LoginResponseDto(token.AccessToken, token.ExpiresAt, user.Role.Name, false));
    }

    [HttpPost("two-factor/initialize")]
    [Authorize]
    [ProducesResponseType(typeof(TwoFactorInitializeResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorInitializeResponseDto>> InitializeTwoFactor()
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return NotFound("Пользователь не найден.");
        }

        var secret = _twoFactorService.GenerateSecret();
        user.TwoFactorSecret = secret;
        user.TwoFactorEnabled = false;

        await _dbContext.SaveChangesAsync();

        var provisioningUri = _twoFactorService.BuildProvisioningUri(secret, user.Email);
        return Ok(new TwoFactorInitializeResponseDto(secret, provisioningUri));
    }

    [HttpPost("two-factor/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerifyRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest("Необходимо указать код подтверждения.");
        }

        var user = await LoadCurrentUserAsync();
        if (user is null || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            return NotFound("Настройки двухфакторной аутентификации не найдены.");
        }

        if (!_twoFactorService.ValidateCode(user.TwoFactorSecret, request.Code))
        {
            return Unauthorized("Неверный код подтверждения.");
        }

        user.TwoFactorEnabled = true;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("two-factor")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorDisableRequestDto request)
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return NotFound("Пользователь не найден.");
        }

        var passwordCheck = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordCheck == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Неверный пароль.");
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("Текущий и новый пароли обязательны.");
        }

        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return NotFound("Пользователь не найден.");
        }

        var passwordCheck = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (passwordCheck == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Неверный текущий пароль.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private async Task<User?> LoadCurrentUserAsync()
    {
        if (_currentUser.UserId is null)
        {
            return null;
        }

        return await _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == _currentUser.UserId);
    }
}
