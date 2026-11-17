namespace Fulfillment.Api.Swagger;

using System;
using System.Collections.Generic;
using System.Linq;
using Fulfillment.Api.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class GhnWebhookOperationFilter : IOperationFilter
{
    private readonly string _webhookSecret;

    public GhnWebhookOperationFilter(IConfiguration configuration)
    {
        _webhookSecret = configuration["Carriers:GHN:WebhookSecret"]
            ?? configuration["GHN_WEBHOOK_SECRET"]
            ?? "ghn-webhook-secret-key";
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAttribute = context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(RequireGhnWebhookSecretAttribute), true).Any() == true
            || context.MethodInfo.GetCustomAttributes(typeof(RequireGhnWebhookSecretAttribute), true).Any();

        var isWebhookPath = context.ApiDescription.RelativePath is { } path &&
            path.Contains("webhook", StringComparison.OrdinalIgnoreCase);

        if (!hasAttribute && !isWebhookPath)
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();

        if (!operation.Parameters.Any(p => string.Equals(p.Name, "X-Webhook-Token", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Webhook-Token",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Carrier webhook secret token in header (Pre-filled: ghn-webhook-secret-key).",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString(_webhookSecret)
                }
            });
        }

        if (!operation.Parameters.Any(p => string.Equals(p.Name, "token", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "token",
                In = ParameterLocation.Query,
                Required = false,
                Description = "Carrier webhook secret token in query string (Alternative to header).",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString(_webhookSecret)
                }
            });
        }
    }
}
