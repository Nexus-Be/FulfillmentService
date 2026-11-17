namespace Fulfillment.Api.Tests.Infrastructure;

using System;
using Xunit;

/// <summary>
/// A test that calls the real GHN sandbox. It runs only when a GHN token is set through the environment
/// (Carriers__GHN__Token), and is skipped otherwise so a machine without carrier credentials still gets a green run.
/// </summary>
public sealed class GhnSandboxFactAttribute : FactAttribute
{
    public GhnSandboxFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Carriers__GHN__Token")))
            Skip = "Requires a GHN sandbox token in the Carriers__GHN__Token environment variable.";
    }
}
