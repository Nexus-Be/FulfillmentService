namespace Fulfillment.UnitTests;

using System;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Carriers.Ghn;

/// <summary>
/// T27 and T16: failed-delivery attempts are told apart by the carrier event time, "returning" is no longer "returned",
/// and a returning parcel can only end as returned or cancelled.
/// </summary>
public class ReturningFlowTests
{
    [Theory]
    [InlineData("returning", ShipmentStatus.Returning)]
    [InlineData("return", ShipmentStatus.Returning)]
    [InlineData("returned", ShipmentStatus.Returned)]
    [InlineData("return_to_origin", ShipmentStatus.Returned)]
    [InlineData("delivery_fail", ShipmentStatus.DeliveryFailed)]
    public void GhnStatusMapper_SeparatesReturningFromReturned(string ghnStatus, ShipmentStatus expected)
    {
        GhnStatusMapper.Map(ghnStatus).Should().Be(expected);
    }

    [Theory]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Returning, true)]
    [InlineData(ShipmentStatus.HandedOver, ShipmentStatus.Returning, true)]
    [InlineData(ShipmentStatus.DeliveryFailed, ShipmentStatus.Returning, true)]
    [InlineData(ShipmentStatus.Returning, ShipmentStatus.Returned, true)]
    [InlineData(ShipmentStatus.Returning, ShipmentStatus.Cancelled, true)]
    [InlineData(ShipmentStatus.Returning, ShipmentStatus.InTransit, false)]
    [InlineData(ShipmentStatus.Returning, ShipmentStatus.Delivered, false)]
    [InlineData(ShipmentStatus.Returning, ShipmentStatus.DeliveryFailed, false)]
    [InlineData(ShipmentStatus.Returning, ShipmentStatus.Returning, false)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Returning, false)]
    [InlineData(ShipmentStatus.Picking, ShipmentStatus.Returning, false)]
    [InlineData(ShipmentStatus.Returned, ShipmentStatus.Returning, false)]
    [InlineData(ShipmentStatus.Cancelled, ShipmentStatus.Returning, false)]
    // the existing lifecycle is unchanged
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.DeliveryFailed, true)]
    [InlineData(ShipmentStatus.DeliveryFailed, ShipmentStatus.InTransit, true)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Returned, true)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.InTransit, false)]
    public void IsForward_HandlesReturning(ShipmentStatus current, ShipmentStatus next, bool expected)
    {
        ShipmentStatusTransitions.IsForward(current, next).Should().Be(expected);
    }

    [Fact]
    public void ASecondFailedAttemptIsNotAStepForward_ButIsRecognisedAsARepeatedAttempt()
    {
        ShipmentStatusTransitions.IsForward(ShipmentStatus.DeliveryFailed, ShipmentStatus.DeliveryFailed).Should().BeFalse();
        ShipmentStatusTransitions.IsRepeatedFailedAttempt(ShipmentStatus.DeliveryFailed, ShipmentStatus.DeliveryFailed).Should().BeTrue();
        ShipmentStatusTransitions.IsRepeatedFailedAttempt(ShipmentStatus.InTransit, ShipmentStatus.DeliveryFailed).Should().BeFalse();
        ShipmentStatusTransitions.IsRepeatedFailedAttempt(ShipmentStatus.DeliveryFailed, ShipmentStatus.InTransit).Should().BeFalse();
    }

    [Fact]
    public void EventTimeKey_IsTheSameForTheSameInstantWhateverTheFormat()
    {
        var iso = GhnEventTime.Key("2026-11-20T10:11:12Z");
        var offset = GhnEventTime.Key("2026-11-20T17:11:12+07:00");
        var seconds = GhnEventTime.Key(new DateTimeOffset(2026, 11, 20, 10, 11, 12, TimeSpan.Zero).ToUnixTimeSeconds());
        var millis = GhnEventTime.Key(new DateTimeOffset(2026, 11, 20, 10, 11, 12, TimeSpan.Zero).ToUnixTimeMilliseconds());
        var text = GhnEventTime.Key(new DateTimeOffset(2026, 11, 20, 10, 11, 12, TimeSpan.Zero).ToUnixTimeSeconds().ToString());

        iso.Should().Be("2026-11-20T10:11:12.000Z");
        new[] { offset, seconds, millis, text }.Should().AllBeEquivalentTo(iso);
    }

    [Fact]
    public void EventTimeKey_DiffersForDifferentTimes()
    {
        GhnEventTime.Key("2026-11-20T10:11:12Z").Should().NotBe(GhnEventTime.Key("2026-11-20T10:41:12Z"));
    }

    [Fact]
    public void EventTimeKey_ReadsAJsonElementLikeTheWebhookBodyDelivers()
    {
        using var doc = JsonDocument.Parse("{\"a\":\"2026-11-20T10:11:12Z\",\"b\":1795255872}");
        GhnEventTime.Key(doc.RootElement.GetProperty("a")).Should().Be("2026-11-20T10:11:12.000Z");
        GhnEventTime.Key(doc.RootElement.GetProperty("b")).Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a time")]
    [InlineData(0L)]
    public void EventTimeKey_IsNullWhenThereIsNoUsableTime(object? value)
    {
        GhnEventTime.Key(value).Should().BeNull();
    }
}
