using System.Security.Claims;
using System.Text.Encodings.Web;
using ELFAssessment.API.Configuration;
using ELFAssessment.API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ELFAssessment.Tests;

public class ApiKeyAuthenticationHandlerTests
{
    private async Task<AuthenticateResult> RunAuthAsync(string? providedKey, string configuredKey)
    {
        var apiKeyOptions = Options.Create(new ApiKeyOptions
        {
            HeaderName = "X-Api-Key",
            Value = configuredKey
        });

        var schemeOptions = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        schemeOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

        var handler = new ApiKeyAuthenticationHandler(
            schemeOptions.Object,
            loggerFactory.Object,
            UrlEncoder.Default,
            apiKeyOptions);

        var scheme = new AuthenticationScheme("ApiKey", null, typeof(ApiKeyAuthenticationHandler));
        var context = new DefaultHttpContext();

        if (providedKey is not null)
            context.Request.Headers["X-Api-Key"] = providedKey;

        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task Authenticate_ValidKey_Succeeds()
    {
        var result = await RunAuthAsync("my-secret-key", "my-secret-key");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
    }

    [Fact]
    public async Task Authenticate_InvalidKey_Fails()
    {
        var result = await RunAuthAsync("wrong-key", "correct-key");

        Assert.True(result.None || !result.Succeeded);
    }

    [Fact]
    public async Task Authenticate_MissingHeader_Fails()
    {
        var result = await RunAuthAsync(null, "my-key");

        Assert.True(result.None || !result.Succeeded);
    }

    [Fact]
    public async Task Authenticate_EmptyConfiguredKey_SkipsAuth()
    {
        var result = await RunAuthAsync(null, "");

        // When no API key is configured, auth should be skipped (NoResult)
        Assert.True(result.None);
    }
}
