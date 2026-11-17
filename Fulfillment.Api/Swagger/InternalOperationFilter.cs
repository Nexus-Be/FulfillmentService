namespace Fulfillment.Api.Swagger;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class InternalOperationFilter : IOperationFilter
{
    private readonly string _serviceKey;

    public InternalOperationFilter(IConfiguration configuration)
    {
        _serviceKey = Environment.GetEnvironmentVariable("INTERNAL_SERVICE_SECRET")
            ?? configuration["INTERNAL_SERVICE_SECRET"]
            ?? configuration["InternalSecurity:ServiceKey"]
            ?? "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e";
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath is not { } path ||
            !path.StartsWith("api/v1/internal", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();
        if (!operation.Parameters.Any(p => string.Equals(p.Name, "X-Internal-Service-Key", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Internal-Service-Key",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Internal service secret key (Pre-filled: eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e).",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString(_serviceKey)
                }
            });
        }
    }
}
