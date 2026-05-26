using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using RecruitWizard.AuthSample.Services;

namespace RecruitWizard.AuthSample.Pages;

public sealed class IndexModel : PageModel
{
    private readonly TokenStore _store;
    private readonly RecruitWizardOptions _options;

    public IndexModel(TokenStore store, IOptions<RecruitWizardOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public string Host => _options.NormalisedHost;
    public string ClientId => _options.ClientId;
    public string RedirectUri => _options.RedirectUri;

    public StoredTokens? Tokens { get; private set; }
    public Dictionary<string, JsonElement>? JwtClaims { get; private set; }
    public LastApiResult? LastApiResult { get; private set; }
    public LastTokenResponse? LastTokenResponse { get; private set; }

    public string? Message { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string PrefillCode { get; private set; } = "";

    public void OnGet(string? message, string? error, string? code)
    {
        Tokens = _store.GetTokens();
        JwtClaims = Tokens is null ? null : AuthService.DecodeJwt(Tokens.AccessToken);
        LastApiResult = _store.GetLastApiResult();
        LastTokenResponse = _store.GetLastTokenResponse();
        Message = message;
        ErrorMessage = error;
        PrefillCode = code ?? "";
    }
}
