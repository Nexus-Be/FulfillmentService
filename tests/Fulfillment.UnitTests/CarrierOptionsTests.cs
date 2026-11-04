namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using FluentAssertions;
using Fulfillment.Infrastructure.Carriers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public class CarrierOptionsTests
{
    [Fact]
    public void AddCarrierServices_WhenDefaultCarrierCodeIsMissing_ShouldThrowOptionsValidationExceptionOnValueAccess()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddCarrierServices(configuration);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<CarrierOptions>>();

        Action act = () => _ = options.Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*DefaultCarrierCode*");
    }

    [Fact]
    public void AddCarrierServices_WhenDefaultCarrierCodeIsProvided_ShouldBindCorrectly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Carriers:DefaultCarrierCode"] = "GHN_TEST"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCarrierServices(configuration);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<CarrierOptions>>();

        options.Value.DefaultCarrierCode.Should().Be("GHN_TEST");
    }

    [Fact]
    public void AddCarrierServices_WhenDefaultCarrierCodeIsMissing_StartupValidatorShouldFailWithoutReadingValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddCarrierServices(configuration);

        var sp = services.BuildServiceProvider();
        var validator = sp.GetRequiredService<IStartupValidator>();

        Action act = () => validator.Validate();

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*DefaultCarrierCode*");
    }
}
