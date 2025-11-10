using System;
using System.Net.Http;
using System.Net.Http.Headers;
using CharityManagement.Client.Models;

namespace CharityManagement.Client.Services;

public sealed class ApiClient : IDisposable
{
    private HttpClient _httpClient;
    private string? _currentBaseAddress;
    private string? _currentBearerToken;

    public ApiClient(AppConfiguration configuration)
    {
        _httpClient = CreateHttpClient();

        if (!TrySetBaseAddress(configuration.ApiBaseUrl, out var errorMessage))
        {
            throw new InvalidOperationException(errorMessage ?? $"Неверный адрес API: {configuration.ApiBaseUrl}");
        }
    }

    public HttpClient HttpClient => _httpClient;

    public string? BaseAddress => _currentBaseAddress;

    public bool TrySetBaseAddress(string baseUrl, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            errorMessage = "Укажите адрес API.";
            return false;
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            errorMessage = "Некорректный адрес API.";
            return false;
        }

        if (!string.Equals(baseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Адрес API должен начинаться с http:// или https://.";
            return false;
        }

        if (_currentBaseAddress is not null)
        {
            var currentUri = new Uri(_currentBaseAddress, UriKind.Absolute);
            if (Uri.Compare(currentUri, baseUri, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            _httpClient.Dispose();
            _httpClient = CreateHttpClient();

            if (!string.IsNullOrWhiteSpace(_currentBearerToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _currentBearerToken);
            }
        }

        _httpClient.BaseAddress = baseUri;
        _currentBaseAddress = baseUri.ToString();
        return true;
    }

    public void SetBearerToken(string? token)
    {
        _currentBearerToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();

        if (_currentBearerToken is null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _currentBearerToken);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
