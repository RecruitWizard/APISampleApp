using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecruitWizard.AuthSample.Services;

/// <summary>
/// Token payload returned by <c>POST /api/connect/token</c>. Properties match
/// the JSON keys exactly (snake_case), so we can round-trip the raw response.
/// </summary>
public sealed class TokenPayload
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("access_token_expiry")]
    public string? AccessTokenExpiry { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("refresh_token_expiry")]
    public string? RefreshTokenExpiry { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class StoredTokens
{
    public string AccessToken { get; init; } = "";
    public string? AccessTokenExpiry { get; init; }
    public string? RefreshToken { get; init; }
    public string? RefreshTokenExpiry { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
}

public sealed class LastTokenResponse
{
    public TokenPayload Payload { get; init; } = new();
    public DateTimeOffset At { get; init; }
}

public sealed class LastApiResult
{
    public string Method { get; init; } = "";
    public string Url { get; init; } = "";
    public int Status { get; init; }
    public string StatusText { get; init; } = "";
    public string? Body { get; init; }
    public bool BodyIsJson { get; init; }
    public DateTimeOffset At { get; init; }
}

/// <summary>
/// Very simple in-memory token store. Single user, single process — equivalent
/// to <c>nodejs/src/tokenStore.js</c>. In a real app, persist tokens per-user
/// (encrypted) in a database.
/// </summary>
public sealed class TokenStore
{
    private readonly Lock _lock = new();
    private StoredTokens? _tokens;
    private LastApiResult? _lastApiResult;
    private LastTokenResponse? _lastTokenResponse;

    public StoredTokens SetTokens(TokenPayload payload)
    {
        lock (_lock)
        {
            var receivedAt = DateTimeOffset.UtcNow;
            _tokens = new StoredTokens
            {
                AccessToken = payload.AccessToken,
                AccessTokenExpiry = payload.AccessTokenExpiry,
                RefreshToken = payload.RefreshToken,
                RefreshTokenExpiry = payload.RefreshTokenExpiry,
                ReceivedAt = receivedAt,
            };
            _lastTokenResponse = new LastTokenResponse
            {
                Payload = payload,
                At = receivedAt,
            };
            return _tokens;
        }
    }

    public StoredTokens? GetTokens()
    {
        lock (_lock) return _tokens;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _tokens = null;
            _lastApiResult = null;
            _lastTokenResponse = null;
        }
    }

    public void SetLastApiResult(LastApiResult result)
    {
        lock (_lock) _lastApiResult = result;
    }

    public LastApiResult? GetLastApiResult()
    {
        lock (_lock) return _lastApiResult;
    }

    public LastTokenResponse? GetLastTokenResponse()
    {
        lock (_lock) return _lastTokenResponse;
    }
}
