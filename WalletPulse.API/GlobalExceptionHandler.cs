using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WalletPulse;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var method = httpContext.Request.Method.Replace("\r", "").Replace("\n", "");
        var path = httpContext.Request.Path.ToString().Replace("\r", "").Replace("\n", "");
        var sanitizedMessage = exception.Message.Replace("\r", "").Replace("\n", "");
        var sanitizedTrace = exception.StackTrace?.Replace("\r", "").Replace("\n", "");

        _logger.LogError("Unhandled exception for {Method} {Path}: {Message} {Trace}", method, path, sanitizedMessage, sanitizedTrace);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 500,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        }, cancellationToken);
        return true;
    }
}
