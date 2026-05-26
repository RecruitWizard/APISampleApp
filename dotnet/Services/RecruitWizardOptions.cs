namespace RecruitWizard.AuthSample.Services;

/// <summary>
/// Strongly typed configuration for the Recruit Wizard OAuth client.
/// Bound from the <c>RecruitWizard</c> section of configuration
/// (appsettings.json, environment variables, user secrets, etc.).
/// </summary>
public sealed class RecruitWizardOptions
{
    public const string SectionName = "RecruitWizard";

    /// <summary>Base URL of the Recruit Wizard API, e.g. https://sandbox.recruitwizard.site.</summary>
    public string Host { get; set; } = "";

    /// <summary>OAuth client ID issued by Recruit Wizard.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>OAuth client secret issued by Recruit Wizard.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>
    /// Callback URL — must exactly match a redirect URI that Recruit Wizard
    /// support has white-listed on the client. For testing, prefer a
    /// <c>https://webhook.site/&lt;uuid&gt;</c> URL over a localhost callback
    /// so you don't need ngrok or another tunnel.
    /// </summary>
    public string RedirectUri { get; set; } = "https://webhook.site/your-unique-id-here";

    public string NormalisedHost => Host.TrimEnd('/');
    public string AuthorizeUrl => $"{NormalisedHost}/api/connect/authorize";
    public string TokenUrl => $"{NormalisedHost}/api/connect/token";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException(
                "RecruitWizard:Host is required. Set it via appsettings.json, " +
                "environment variable RecruitWizard__Host, or user secrets.");
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("RecruitWizard:ClientId is required.");
        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new InvalidOperationException("RecruitWizard:ClientSecret is required.");
        if (string.IsNullOrWhiteSpace(RedirectUri))
            throw new InvalidOperationException("RecruitWizard:RedirectUri is required.");
    }
}
