using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RecruitWizard.AuthSample.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<RecruitWizardOptions>()
    .Bind(builder.Configuration.GetSection(RecruitWizardOptions.SectionName))
    .Validate(o =>
    {
        try { o.Validate(); return true; }
        catch { return false; }
    }, "RecruitWizard configuration is incomplete. See README for required values.");

builder.Services.AddHttpClient<AuthService>();
builder.Services.AddHttpClient("RecruitWizardApi");
builder.Services.AddSingleton<TokenStore>();
builder.Services.AddSingleton<StateStore>();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();

app.MapGet("/auth/login", (AuthService auth, StateStore states, ILogger<Program> log) =>
{
    var state = states.Generate();
    var url = auth.BuildAuthorizeUrl(state);
    log.LogInformation("[auth] Redirecting to authorize URL with state={State}", state);
    return Results.Redirect(url);
});

app.MapPost("/auth/exchange",
    async ([FromForm] string? code, AuthService auth, TokenStore store, ILogger<Program> log) =>
{
    var trimmed = code?.Trim() ?? "";
    if (string.IsNullOrEmpty(trimmed))
        return Results.Redirect("/?error=" + Uri.EscapeDataString("No code supplied to exchange."));

    try
    {
        log.LogInformation("[auth] Exchanging code for token (code={Prefix}...)",
            trimmed[..Math.Min(8, trimmed.Length)]);
        var payload = await auth.ExchangeCodeForTokenAsync(trimmed);
        store.SetTokens(payload);
        return Results.Redirect("/?message=" + Uri.EscapeDataString("Access token retrieved successfully."));
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[auth] Token exchange failed");
        return Results.Redirect(
            "/?error=" + Uri.EscapeDataString(ex.Message) +
            "&code=" + Uri.EscapeDataString(trimmed));
    }
}).DisableAntiforgery();

app.MapPost("/auth/refresh", async (TokenStore store, AuthService auth, ILogger<Program> log) =>
{
    var tokens = store.GetTokens();
    if (tokens is null || string.IsNullOrEmpty(tokens.RefreshToken))
        return Results.Redirect("/?error=" + Uri.EscapeDataString("No refresh token available."));

    try
    {
        log.LogInformation("[auth] Refreshing access token...");
        var payload = await auth.RefreshAccessTokenAsync(tokens.RefreshToken);
        store.SetTokens(payload);
        return Results.Redirect("/?message=" + Uri.EscapeDataString("Access token refreshed."));
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[auth] Refresh failed");
        return Results.Redirect("/?error=" + Uri.EscapeDataString(ex.Message));
    }
}).DisableAntiforgery();

app.MapPost("/auth/logout", (TokenStore store) =>
{
    store.Clear();
    return Results.Redirect("/?message=" + Uri.EscapeDataString("Tokens cleared."));
}).DisableAntiforgery();

app.MapPost("/api/server-status",
    (TokenStore store, IHttpClientFactory factory, IOptions<RecruitWizardOptions> opts, ILogger<Program> log) =>
        CallApiAsync(store, factory, opts.Value, log, HttpMethod.Get, "/api/ServerStatus"))
   .DisableAntiforgery();

app.MapPost("/api/me",
    (TokenStore store, IHttpClientFactory factory, IOptions<RecruitWizardOptions> opts, ILogger<Program> log) =>
{
    var tokens = store.GetTokens();
    if (tokens is null)
        return Task.FromResult(Results.Redirect("/?error=" + Uri.EscapeDataString("Not authenticated.")));

    var claims = AuthService.DecodeJwt(tokens.AccessToken);
    var userId = ClaimValue(claims, "UserID")
              ?? ClaimValue(claims, "userId")
              ?? ClaimValue(claims, "sub");

    if (string.IsNullOrWhiteSpace(userId))
        return Task.FromResult(Results.Redirect(
            "/?error=" + Uri.EscapeDataString("Could not find UserID claim on the access token.")));

    return CallApiAsync(store, factory, opts.Value, log,
        HttpMethod.Get, $"/api/Users/{Uri.EscapeDataString(userId)}");
}).DisableAntiforgery();

var options = app.Services.GetRequiredService<IOptions<RecruitWizardOptions>>().Value;
app.Logger.LogInformation("\nRecruit Wizard auth sample (.NET) running");
app.Logger.LogInformation("  HOST          = {Host}", options.NormalisedHost);
app.Logger.LogInformation("  CLIENT_ID     = {ClientId}", options.ClientId);
app.Logger.LogInformation("  REDIRECT_URI  = {RedirectUri}", options.RedirectUri);
app.Logger.LogInformation("  authorize URL = {AuthorizeUrl}", options.AuthorizeUrl);
app.Logger.LogInformation("  token URL     = {TokenUrl}\n", options.TokenUrl);

app.Run();

static async Task<IResult> CallApiAsync(
    TokenStore store,
    IHttpClientFactory factory,
    RecruitWizardOptions options,
    ILogger logger,
    HttpMethod method,
    string apiPath)
{
    var tokens = store.GetTokens();
    if (tokens is null)
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Not authenticated."));

    var url = $"{options.NormalisedHost}{apiPath}";
    logger.LogInformation("[api] {Method} {Url}", method.Method, url);

    try
    {
        var http = factory.CreateClient("RecruitWizardApi");
        using var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        req.Headers.Accept.ParseAdd("application/json");

        using var res = await http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        var (pretty, isJson) = TryPrettyPrintJson(body);

        store.SetLastApiResult(new LastApiResult
        {
            Method = method.Method,
            Url = url,
            Status = (int)res.StatusCode,
            StatusText = res.ReasonPhrase ?? "",
            Body = pretty,
            BodyIsJson = isJson,
            At = DateTimeOffset.UtcNow,
        });
        return Results.Redirect("/");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[api] Request failed");
        return Results.Redirect("/?error=" + Uri.EscapeDataString(ex.Message));
    }
}

static (string Body, bool IsJson) TryPrettyPrintJson(string raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return (raw, false);
    try
    {
        using var doc = JsonDocument.Parse(raw);
        var pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        return (pretty, true);
    }
    catch
    {
        return (raw, false);
    }
}

static string? ClaimValue(IDictionary<string, JsonElement>? claims, string key)
{
    if (claims is null) return null;
    if (!claims.TryGetValue(key, out var element)) return null;
    return element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.ToString(),
        _ => element.ToString(),
    };
}

public partial class Program;
