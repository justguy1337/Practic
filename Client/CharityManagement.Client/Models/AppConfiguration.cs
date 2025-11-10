using System;
using System.IO;
using System.Text.Json;

namespace CharityManagement.Client.Models;

public sealed record AppConfiguration
{
    private const string DefaultApiUrl = "http://localhost:5009";
    private const string EnvironmentVariableName = "CHARITY_API_BASE_URL";

    public string ApiBaseUrl { get; init; } = DefaultApiUrl;

    public static AppConfiguration Load(string? basePath = null)
    {
        var configuration = new AppConfiguration();

        var environmentValue = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            configuration = configuration with { ApiBaseUrl = environmentValue.Trim() };
        }

        var resolvedBasePath = basePath ?? AppContext.BaseDirectory;
        var settingsPath = Path.Combine(resolvedBasePath, "appsettings.json");

        if (!File.Exists(settingsPath))
        {
            return configuration;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            var fileConfig = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!string.IsNullOrWhiteSpace(fileConfig?.ApiBaseUrl))
            {
                configuration = configuration with { ApiBaseUrl = fileConfig.ApiBaseUrl.Trim() };
            }
        }
        catch (Exception)
        {
            // Ignore malformed configuration and fall back to defaults/environment overrides.
        }

        return configuration;
    }
}
