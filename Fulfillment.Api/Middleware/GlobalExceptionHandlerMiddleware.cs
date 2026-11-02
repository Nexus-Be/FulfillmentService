namespace Fulfillment.Api.Middleware;

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fulfillment.Domain.Exceptions;

public sealed class GlobalExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        if (exception is InsufficientInventoryBatchException batchEx)
        {
            const int statusCode = StatusCodes.Status400BadRequest;
            const string errorCode = "INSUFFICIENT_INVENTORY";
            const string title = "Insufficient inventory";
            var detail = batchEx.Message;

            _logger.LogWarning("[WARN {ErrorCode}] {Title}: {Detail}. Failures count: {Count}. Trace: {TraceId}",
                errorCode, title, detail, batchEx.Failures.Count, traceId);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var batchProblemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title,
                status = statusCode,
                errorCode,
                detail,
                traceId,
                items = batchEx.Failures,
                timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(batchProblemDetails, JsonOptions));
            return;
        }

        if (exception is ReservationAlreadyCommittedException committedEx)
        {
            const int statusCode = StatusCodes.Status409Conflict;
            const string errorCode = "RESERVATION_ALREADY_COMMITTED";
            const string title = "Reservation already committed";
            var detail = committedEx.Message;

            _logger.LogWarning("[WARN {ErrorCode}] {Title}: {Detail}. ReferenceId: {ReferenceId}. Trace: {TraceId}",
                errorCode, title, detail, committedEx.ReferenceId, traceId);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var conflictProblemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                title,
                status = statusCode,
                errorCode,
                detail,
                traceId,
                referenceId = committedEx.ReferenceId,
                committedAt = committedEx.CommittedAt,
                reservation = committedEx.Reservation,
                timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(conflictProblemDetails, JsonOptions));
            return;
        }

        var (status, code, errTitle, errDetail) = exception switch
        {
            InsufficientInventoryException iie => (
                StatusCodes.Status400BadRequest,
                "INSUFFICIENT_STOCK",
                "Insufficient stock",
                iie.Message),
            BusinessRuleValidationException bve => (
                bve.StatusCode != 0 ? bve.StatusCode : StatusCodes.Status400BadRequest,
                bve.ErrorCode ?? "BUSINESS_RULE_VIOLATION",
                GetStatusTitle(bve.StatusCode != 0 ? bve.StatusCode : StatusCodes.Status400BadRequest, "Business rule violation"),
                bve.Message),
            EntityNotFoundException nfe => (
                nfe.StatusCode != 0 ? nfe.StatusCode : StatusCodes.Status404NotFound,
                nfe.ErrorCode ?? "NOT_FOUND",
                GetStatusTitle(nfe.StatusCode != 0 ? nfe.StatusCode : StatusCodes.Status404NotFound, "Resource not found"),
                nfe.Message),
            DomainException dex => (
                dex.StatusCode != 0 ? dex.StatusCode : StatusCodes.Status400BadRequest,
                dex.ErrorCode ?? "DOMAIN_ERROR",
                GetStatusTitle(dex.StatusCode != 0 ? dex.StatusCode : StatusCodes.Status400BadRequest, "Domain error"),
                dex.Message),
            Microsoft.EntityFrameworkCore.DbUpdateException dbEx when dbEx.InnerException is Npgsql.PostgresException { SqlState: "23505" } => (
                StatusCodes.Status400BadRequest,
                "BUSINESS_RULE_VIOLATION",
                "Duplicate key violation",
                "A record with the same unique identifier or code already exists."),
            ArgumentException aex => (
                StatusCodes.Status400BadRequest,
                "VAL_INVALID_ARGUMENT",
                "Invalid argument",
                aex.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "An unexpected error occurred",
                "An internal server error occurred. Please try again later.")
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "[ERROR {ErrorCode}] {Title}: {Detail}. Trace: {TraceId}", code, errTitle, errDetail, traceId);
        }
        else
        {
            _logger.LogWarning("[WARN {ErrorCode}] {Title}: {Detail}. Trace: {TraceId}", code, errTitle, errDetail, traceId);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        var problemDetails = new
        {
            type = GetRfcType(status),
            title = errTitle,
            status,
            errorCode = code,
            detail = errDetail,
            traceId,
            timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }

    private static string GetRfcType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.3",
        StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        StatusCodes.Status429TooManyRequests => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };

    private static string GetStatusTitle(int statusCode, string fallback) => statusCode switch
    {
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status423Locked => "Account locked",
        StatusCodes.Status429TooManyRequests => "Too many requests",
        _ => fallback
    };
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}
