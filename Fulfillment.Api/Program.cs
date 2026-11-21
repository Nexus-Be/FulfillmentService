using System;
using Fulfillment.Api.Common;
using Fulfillment.Api.Extensions;
using Fulfillment.Api.Workers;
using Fulfillment.Application;
using Fulfillment.Infrastructure;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDefaults(builder.Configuration, "fulfillment-service");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

builder.Services.AddSingleton(new Fulfillment.Application.Services.Shipping.ShippingEstimateOptions(
    Enabled: builder.Configuration.GetValue("Shipping:EstimatedFee:Enabled", false),
    BaseFee: builder.Configuration.GetValue("Shipping:EstimatedFee:BaseFee", 30000m),
    PerExtra500Grams: builder.Configuration.GetValue("Shipping:EstimatedFee:PerExtra500Grams", 5000m)));
builder.Services.AddSingleton(new Fulfillment.Application.Services.Shipment.ShippingBehaviorOptions(
    EmitReturningStatus: builder.Configuration.GetValue("Shipping:EmitReturningStatus", false),
    AutoReturnAfterFailedAttempts: builder.Configuration.GetValue("Shipping:AutoReturnAfterFailedAttempts", false)));
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<ReservationAutoExpireWorker>();
builder.Services.AddHostedService<Fulfillment.Api.Workers.CommerceDeliveryNotificationWorker>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ApiValidationProblemFactory.Create;
    });

var app = builder.Build();

app.UseServiceDefaults();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Database migration deferred on startup: {Message}", ex.Message);
    }
}

app.MapControllers();
// Readiness, not just "the process is up": the database must answer, otherwise 503 so the central health view can tell.
app.MapGet("/health", async (Fulfillment.Infrastructure.Persistence.AppDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "Healthy", service = "fulfillment-service" })
            : Results.Json(new { status = "Unhealthy", service = "fulfillment-service", message = "The database is not reachable." }, statusCode: 503);
    }
    catch (Exception)
    {
        return Results.Json(new { status = "Unhealthy", service = "fulfillment-service", message = "The database check failed." }, statusCode: 503);
    }
});

app.Run();

public partial class Program { }
