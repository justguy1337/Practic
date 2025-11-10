using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CharityManagement.Client.Models;

namespace CharityManagement.Client.Services;

public sealed class AuthService
{
    private readonly ApiClient _apiClient;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthSession? CurrentSession { get; private set; }
    public string? BaseAddress => _apiClient.BaseAddress;

    public AuthService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<LoginResult> LoginAsync(string userNameOrEmail, string password, string baseUrl, CancellationToken cancellationToken = default)
    {
        if (!_apiClient.TrySetBaseAddress(baseUrl, out var addressError))
        {
            return LoginResult.Failure(addressError ?? "Ошибка конфигурации API.");
        }

        var request = new LoginRequestDto(userNameOrEmail, password);

        try
        {
            using var response = await _apiClient.HttpClient.PostAsJsonAsync("api/auth/login", request, _serializerOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<LoginResponseDto>(_serializerOptions, cancellationToken);
                if (payload is null)
                {
                    return LoginResult.Failure("Не удалось обработать ответ сервера.");
                }

                if (payload.RequiresTwoFactor)
                {
                    return LoginResult.Failure("Для этой учетной записи требуется двухфакторная авторизация. Поддержка появится позже.");
                }

                if (string.IsNullOrWhiteSpace(payload.AccessToken))
                {
                    return LoginResult.Failure("Сервер не вернул токен доступа.");
                }

                var session = new AuthSession(payload.AccessToken, payload.ExpiresAt, payload.Role);
                CurrentSession = session;
                _apiClient.SetBearerToken(session.AccessToken);

                return LoginResult.Success(session);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var loginResponse = await TryReadLoginResponseAsync(response, cancellationToken);
                if (loginResponse?.RequiresTwoFactor == true)
                {
                    return LoginResult.Failure("Для этой учетной записи включена двухфакторная проверка.");
                }
            }

            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(message))
            {
                message = $"Ошибка авторизации: {(int)response.StatusCode} {response.StatusCode}";
            }

            return LoginResult.Failure(message.Trim());
        }
        catch (HttpRequestException httpEx)
        {
            return LoginResult.Failure($"Ошибка соединения: {httpEx.Message}");
        }
        catch (TaskCanceledException)
        {
            return LoginResult.Failure("Превышено время ожидания ответа от API. Проверьте, что сервис запущен и адрес указан верно.");
        }
        catch (Exception ex)
        {
            return LoginResult.Failure($"Неожиданная ошибка: {ex.Message}");
        }
    }

    public void Logout()
    {
        CurrentSession = null;
        _apiClient.SetBearerToken(null);
    }

    private async Task<LoginResponseDto?> TryReadLoginResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<LoginResponseDto>(_serializerOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private sealed record LoginRequestDto(string UserNameOrEmail, string Password, string? TwoFactorCode = null);

    private sealed record LoginResponseDto(string? AccessToken, DateTimeOffset? ExpiresAt, string Role, bool RequiresTwoFactor);

    public sealed record LoginResult(bool IsSuccess, string? Error, AuthSession? Session)
    {
        public static LoginResult Success(AuthSession session) => new(true, null, session);
        public static LoginResult Failure(string error) => new(false, error, null);
    }
}
