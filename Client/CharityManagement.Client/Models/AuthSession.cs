using System;

namespace CharityManagement.Client.Models;

public sealed record AuthSession(string AccessToken, DateTimeOffset? ExpiresAt, string Role)
{
    public bool HasValidToken => !string.IsNullOrWhiteSpace(AccessToken);
}
