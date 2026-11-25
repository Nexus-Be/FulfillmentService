namespace Fulfillment.Api.Tests.Filters;

using System.Collections.Generic;
using System.Threading.Tasks;
using Fulfillment.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Xunit;

public class GhnWebhookSecurityFilterTests
{
    private static AuthorizationFilterContext CreateContext(
        string? headerToken = null,
        string? headerSecret = null,
        string? queryToken = null,
        string? querySecret = null)
    {
        var httpContext = new DefaultHttpContext();
        if (headerToken != null)
            httpContext.Request.Headers["X-Webhook-Token"] = headerToken;
        if (headerSecret != null)
            httpContext.Request.Headers["X-Webhook-Secret"] = headerSecret;

        var query = new List<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>();
        if (queryToken != null)
            query.Add(new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("token", queryToken));
        if (querySecret != null)
            query.Add(new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("secret", querySecret));

        if (query.Count > 0)
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(query));

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Theory]
    [InlineData("X-Webhook-Token", "valid-secret-123")]
    [InlineData("X-Webhook-Secret", "valid-secret-123")]
    [InlineData("token", "valid-secret-123")]
    [InlineData("secret", "valid-secret-123")]
    public async Task Filter_AcceptsAllConfiguredSecretLocations(string location, string value)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Carriers:GHN:WebhookSecret"] = "valid-secret-123"
            })
            .Build();

        var filter = new GhnWebhookSecretFilter(config);
        var context = location switch
        {
            "X-Webhook-Token" => CreateContext(headerToken: value),
            "X-Webhook-Secret" => CreateContext(headerSecret: value),
            "token" => CreateContext(queryToken: value),
            "secret" => CreateContext(querySecret: value),
            _ => CreateContext()
        };

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Filter_RejectsWhenSecretIsInvalid()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Carriers:GHN:WebhookSecret"] = "valid-secret-123"
            })
            .Build();

        var filter = new GhnWebhookSecretFilter(config);
        var context = CreateContext(headerSecret: "wrong-secret");

        await filter.OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }
}
