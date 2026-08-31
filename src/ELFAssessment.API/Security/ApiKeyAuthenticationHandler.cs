using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using ELFAssessment.API.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ELFAssessment.API.Security;

/// <summary>
/// Custom authentication handler that validates the X-Api-Key header.
/// Uses CryptographicOperations.FixedTimeEquals to prevent timing attacks.
/// If no API key is configured, authentication is skipped (NoResult).
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApiKeyOptions _apiKeyOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiKeyOptions> apiKeyOptions)
        : base(options, logger, encoder)
    {
        _apiKeyOptions = apiKeyOptions.Value;
    }

    /// <summary>Validates the API key from the request header against the configured value.</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(_apiKeyOptions.Value))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Request.Headers.TryGetValue(_apiKeyOptions.HeaderName, out var providedKey))
            return Task.FromResult(AuthenticateResult.Fail("Missing API key header"));

        var expected = Encoding.UTF8.GetBytes(_apiKeyOptions.Value);
        var actual = Encoding.UTF8.GetBytes(providedKey.ToString());

        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
