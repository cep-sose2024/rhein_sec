using Newtonsoft.Json;

namespace backend.Controllers.example.logging;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch
        {
            HandleException(httpContext);
        }
    }

    private static void HandleException(HttpContext context)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var response = new { message = "Internal Server Error" };
        var payload = JsonConvert.SerializeObject(response);
        context.Response.WriteAsync(payload);
    }
}