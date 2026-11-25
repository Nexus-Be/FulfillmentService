namespace Fulfillment.Api.Tests.Controllers;

using System;
using System.Collections.Generic;
using Fulfillment.Api.Extensions;
using Fulfillment.Application.Services.Shipping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>The body of a location list stays a plain array; the origin and the time the carrier gave it travel in two headers.</summary>
public class LocationResponseTests
{
    private sealed class Probe : ControllerBase
    {
        public Probe() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }; }
    }

    [Fact]
    public void The_body_is_a_plain_array_and_the_origin_is_in_two_headers()
    {
        var probe = new Probe();
        var observed = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

        var result = probe.OkLocations(new LocationList<ProvinceDto>(new[] { new ProvinceDto(202, "Ho Chi Minh") }, LocationSources.GhnCache, observed));

        Assert.IsAssignableFrom<IReadOnlyList<ProvinceDto>>(result.Value);
        Assert.Equal("ghn-cache", probe.Response.Headers[LocationResponseExtensions.SourceHeader].ToString());
        Assert.StartsWith("2026-11-24T10:00:00", probe.Response.Headers[LocationResponseExtensions.ObservedAtHeader].ToString());
    }
}
