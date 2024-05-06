using Serilog;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

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