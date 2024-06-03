using Newtonsoft.Json;

namespace backend.Controllers.example.logging;

/// <summary>
/// Middleware for handling exceptions and providing a custom response.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="httpContext">The context for the current HTTP request.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
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

    /// <summary>
    /// Handles the exception by setting the HTTP response status code to 500 and writing a custom error message to the response.
    /// </summary>
    /// <param name="context">The context for the current HTTP request.</param>
    private static void HandleException(HttpContext context)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var response = new { message = "Internal Server Error" };
        var payload = JsonConvert.SerializeObject(response);
        context.Response.WriteAsync(payload);
    }
}
