using System.Net;
using System.Text.Json;
using GrainMarket.Application.Common.Exceptions;
using Serilog;

namespace GrainMarket.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private static async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, errors) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            ForbiddenAccessException => (HttpStatusCode.Forbidden, exception.Message, null),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message, null),
            InvalidCalculationException => (HttpStatusCode.BadRequest, exception.Message, null),
            ValidationAppException validationEx => (HttpStatusCode.BadRequest, "Validation failed.", (object?)validationEx.Errors),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            Log.Error(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            Log.Warning("{StatusCode} on {Method} {Path}: {Message}", (int)status, context.Request.Method, context.Request.Path, exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;

        var payload = new { title, status = (int)status, errors };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
