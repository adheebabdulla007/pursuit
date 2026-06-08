namespace Pursuit.API.Middleware;

public sealed class ErrorResponse
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
}