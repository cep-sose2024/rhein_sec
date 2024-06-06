using Serilog;

/// <summary>
/// Middleware for logging the client's IP address for each request.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The context for the current HTTP request.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Get the client's IP address from the HttpContext
        var clientIP = context.Connection.RemoteIpAddress?.ToString();

        // Log the client's IP address using Serilog
        Log.Information("Request from IP: {ClientIP}", clientIP);

        // Continue processing the request
        await _next(context);
    }
}
