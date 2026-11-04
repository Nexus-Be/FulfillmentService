namespace Fulfillment.Api.Tests.Infrastructure;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Fulfillment.Api.Middleware;
using Fulfillment.Infrastructure.Services.Common;
using Xunit;

public sealed class IdentityPropagationInfrastructureTests
{
    [Fact]
    public async Task UserContextMiddleware_populates_ClaimsPrincipal_when_headers_present()
    {
        var context = new DefaultHttpContext();
        var expectedUserId = Guid.NewGuid();
        context.Request.Headers["x-user-id"] = expectedUserId.ToString();
        context.Request.Headers["x-user-roles"] = "staff,admin";
        context.Request.Headers["x-user-email"] = "fulfillment@nexus.local";
        context.Request.Headers["x-user-permissions"] = "SHIPMENTS_READ,SHIPMENTS_WRITE";

        var nextInvoked = false;
        var middleware = new UserContextMiddleware(nextContext =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.NotNull(context.User);
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal("NexusGatewayAuth", context.User.Identity?.AuthenticationType);
        Assert.Equal(expectedUserId.ToString(), context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("fulfillment@nexus.local", context.User.FindFirst(ClaimTypes.Email)?.Value);
        Assert.True(context.User.IsInRole("staff"));
        Assert.True(context.User.IsInRole("admin"));
        Assert.False(context.User.IsInRole("buyer"));
        Assert.Contains(context.User.FindAll("permission"), c => c.Value == "SHIPMENTS_READ");
        Assert.Contains(context.User.FindAll("permission"), c => c.Value == "SHIPMENTS_WRITE");
    }

    [Fact]
    public async Task UserContextMiddleware_does_not_modify_user_when_userId_header_missing()
    {
        var context = new DefaultHttpContext();
        var nextInvoked = false;
        var middleware = new UserContextMiddleware(nextContext =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task UserContextMiddleware_does_not_modify_user_when_userId_header_invalid_guid()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-user-id"] = "not-a-valid-guid";

        var nextInvoked = false;
        var middleware = new UserContextMiddleware(nextContext =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }

    private sealed class TestCapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task IdentityPropagationHandler_injects_headers_from_ambient_HttpContext()
    {
        var expectedUserId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new(ClaimTypes.Role, "staff"),
            new(ClaimTypes.Role, "admin"),
            new(ClaimTypes.Email, "ambient-fulfillment@nexus.local"),
            new("permission", "WAREHOUSES_READ"),
            new("permission", "WAREHOUSES_WRITE")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "NexusGatewayAuth"));
        context.TraceIdentifier = "trace-ambient-fulfillment-12345";
        context.Request.Headers["x-client-ip"] = "192.168.1.200";

        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var capturingHandler = new TestCapturingHandler();
        var handler = new IdentityPropagationHandler(httpContextAccessor, NullLogger<IdentityPropagationHandler>.Instance)
        {
            InnerHandler = capturingHandler
        };

        using var client = new HttpClient(handler);
        var response = await client.GetAsync("http://internal-downstream.local/api/v1/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var captured = capturingHandler.LastRequest;
        Assert.NotNull(captured);

        Assert.True(captured.Headers.Contains("x-user-id"));
        Assert.Equal(expectedUserId.ToString(), string.Join(",", captured.Headers.GetValues("x-user-id")));

        Assert.True(captured.Headers.Contains("x-user-roles"));
        var roles = string.Join(",", captured.Headers.GetValues("x-user-roles"));
        Assert.Contains("staff", roles);
        Assert.Contains("admin", roles);

        Assert.True(captured.Headers.Contains("x-user-email"));
        Assert.Equal("ambient-fulfillment@nexus.local", string.Join(",", captured.Headers.GetValues("x-user-email")));

        Assert.True(captured.Headers.Contains("x-user-permissions"));
        var perms = string.Join(",", captured.Headers.GetValues("x-user-permissions"));
        Assert.Contains("WAREHOUSES_READ", perms);
        Assert.Contains("WAREHOUSES_WRITE", perms);

        Assert.True(captured.Headers.Contains("x-client-ip"));
        Assert.Equal("192.168.1.200", string.Join(",", captured.Headers.GetValues("x-client-ip")));

        Assert.True(captured.Headers.Contains("x-trace-id"));
        Assert.Equal("trace-ambient-fulfillment-12345", string.Join(",", captured.Headers.GetValues("x-trace-id")));
    }

    [Fact]
    public async Task IdentityPropagationHandler_falls_back_to_raw_headers_if_User_is_empty()
    {
        var expectedUserId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers["x-user-id"] = expectedUserId.ToString();
        context.Request.Headers["x-user-roles"] = "staff";
        context.Request.Headers["x-user-email"] = "fallback@nexus.local";
        context.Request.Headers["x-user-permissions"] = "SHIPMENTS_READ";

        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var capturingHandler = new TestCapturingHandler();
        var handler = new IdentityPropagationHandler(httpContextAccessor, NullLogger<IdentityPropagationHandler>.Instance)
        {
            InnerHandler = capturingHandler
        };

        using var client = new HttpClient(handler);
        await client.GetAsync("http://internal-downstream.local/api/v1/ping");

        var captured = capturingHandler.LastRequest;
        Assert.NotNull(captured);
        Assert.Equal(expectedUserId.ToString(), string.Join(",", captured.Headers.GetValues("x-user-id")));
        Assert.Equal("staff", string.Join(",", captured.Headers.GetValues("x-user-roles")));
        Assert.Equal("fallback@nexus.local", string.Join(",", captured.Headers.GetValues("x-user-email")));
        Assert.Equal("SHIPMENTS_READ", string.Join(",", captured.Headers.GetValues("x-user-permissions")));
    }

    [Fact]
    public async Task IdentityPropagationHandler_passes_through_gracefully_when_HttpContext_is_null()
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = null };
        var capturingHandler = new TestCapturingHandler();
        var handler = new IdentityPropagationHandler(httpContextAccessor, NullLogger<IdentityPropagationHandler>.Instance)
        {
            InnerHandler = capturingHandler
        };

        using var client = new HttpClient(handler);
        var response = await client.GetAsync("http://internal-downstream.local/api/v1/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var captured = capturingHandler.LastRequest;
        Assert.NotNull(captured);
        Assert.False(captured.Headers.Contains("x-user-id"));
        Assert.False(captured.Headers.Contains("x-user-roles"));
    }

    [Fact]
    public async Task IdentityPropagationHandler_does_not_overwrite_manually_set_headers()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "staff")
        }, "NexusGatewayAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var capturingHandler = new TestCapturingHandler();
        var handler = new IdentityPropagationHandler(httpContextAccessor, NullLogger<IdentityPropagationHandler>.Instance)
        {
            InnerHandler = capturingHandler
        };

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://internal-downstream.local/api/v1/ping");
        var customUserId = "custom-user-123";
        request.Headers.Add("x-user-id", customUserId);

        await client.SendAsync(request);

        var captured = capturingHandler.LastRequest;
        Assert.NotNull(captured);
        Assert.Equal(customUserId, string.Join(",", captured.Headers.GetValues("x-user-id")));
    }
}
