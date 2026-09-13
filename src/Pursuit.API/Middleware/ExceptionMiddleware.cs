using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Pursuit.Domain.Exceptions;

namespace Pursuit.API.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
        context.Response.ContentType = "application/json";

        var (statusCode, message, logLevel) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message, LogLevel.Warning),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message, LogLevel.Warning),
            ForbiddenAccessException => (HttpStatusCode.Forbidden, exception.Message, LogLevel.Warning),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message, LogLevel.Warning),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message, LogLevel.Warning),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", LogLevel.Error)
        };

        if (logLevel == LogLevel.Error)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            _logger.Log(logLevel, "Handled {ExceptionType} ({StatusCode}) for {Method} {Path}: {Message}",
                exception.GetType().Name,
                (int)statusCode,
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }

        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Details = _env.IsDevelopment() ? exception.StackTrace : null
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}