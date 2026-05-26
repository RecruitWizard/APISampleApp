using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace RecruitWizard.AuthSample.Services;

/// <summary>
/// Wraps the Recruit Wizard OAuth endpoints — equivalent to
/// <c>nodejs/src/auth.js</c>:
/// <list type="bullet">
///   <item>Build the authorize URL (step 1).</item>
///   <item>Exchange an authorization code for tokens (step 3).</item>
///   <item>Refresh an access token (step 4).</item>
///   <item>Decode a JWT payload for display (no signature verification).</item>
/// </list>
/// </summary>
public sealed class AuthService
{
    private readonly HttpClient _http;
    private readonly RecruitWizardOptions _options;
    private readonly ILogger<AuthService> _logger;

    public AuthService(HttpClient http, IOptions<RecruitWizardOptions> options, ILogger<AuthService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string BuildAuthorizeUrl(string state)
    {
        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["state"] = state,
        };
        var qs = string.Join("&", query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? "")}"));
        return $"{_options.AuthorizeUrl}?{qs}";
    }

    public Task<TokenPayload> ExchangeCodeForTokenAsync(string code, CancellationToken ct = default) =>
        PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
        }, ct);

    public Task<TokenPayload> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default) =>
        PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, ct);

    private async Task<TokenPayload> PostTokenAsync(IDictionary<string, string> form, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(form);
        using var req = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
        {
            Content = content,
        };
        req.Headers.Accept.ParseAdd("application/json");

        using var res = await _http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            throw new TokenEndpointException(
                $"Token endpoint returned {(int)res.StatusCode} {res.ReasonPhrase}: " +
                (string.IsNullOrEmpty(text) ? "(empty body)" : text),
                res.StatusCode,
                text);
        }

        try
        {
            return JsonSerializer.Deserialize<TokenPayload>(text)
                ?? throw new TokenEndpointException("Token endpoint returned null payload.", res.StatusCode, text);
        }
        catch (JsonException ex)
        {
            throw new TokenEndpointException(
                $"Token endpoint returned non-JSON body: {text}", res.StatusCode, text, ex);
        }
    }

    /// <summary>
    /// Decode the JWT access token payload (without verifying the signature).
    /// Used purely for display + extracting the UserID claim for demo calls.
    /// </summary>
    public static Dictionary<string, JsonElement>? DecodeJwt(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return null;
        var parts = accessToken.Split('.');
        if (parts.Length != 3) return null;

        try
        {
            var payloadJson = Encoding.UTF8.GetString(FromBase64Url(parts[1]));
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] FromBase64Url(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}

public sealed class TokenEndpointException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string RawBody { get; }

    public TokenEndpointException(string message, HttpStatusCode status, string body, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = status;
        RawBody = body;
    }
}
