using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Api.Common.Middleware;

/// <summary>
/// Catches every unhandled exception from any request and converts it into
/// a consistent ProblemDetails JSON response, instead of leaking raw stack
/// traces or returning the wrong status code (e.g. 500 for bad credentials).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);

            var (statusCode, title) = MapException(ex);

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }

    private static (int StatusCode, string Title) MapException(Exception ex) => ex switch
    {
        UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Unauthorized"),
        ArgumentException => ((int)HttpStatusCode.BadRequest, "Bad request"),
        KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Not found"),
        _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred")
    };
}