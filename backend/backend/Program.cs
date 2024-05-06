using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Newtonsoft.Json;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddLogging(loggingBuilder => { loggingBuilder.AddSerilog(Log.Logger); });
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.AddServerHeader = false;

    var portArgumentIndex = args.ToList().IndexOf("-port");
    if (portArgumentIndex >= 0 && args.Length > portArgumentIndex + 1)
    {
        var port = args[portArgumentIndex + 1];
        if (int.TryParse(port, out var portNumber))
        {
            serverOptions.ListenAnyIP(portNumber);
            Log.Information($"Listening on port: {portNumber}");
        }
        else
        {
            Log.Error("Invalid port number provided");
        }
    }
});

builder.WebHost.ConfigureKestrel(serverOptions => { serverOptions.AddServerHeader = false; });
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var loggerConfiguration = new LoggerConfiguration().WriteTo.Console();

var outputFileArgumentIndex = args.ToList().IndexOf("-o");
if (outputFileArgumentIndex >= 0 && args.Length > outputFileArgumentIndex + 1)
{
    Console.WriteLine("Started Logging to " + args[outputFileArgumentIndex + 1]);
    var logFilePath = args[outputFileArgumentIndex + 1];
    loggerConfiguration.WriteTo.File(logFilePath);
}

Log.Logger = loggerConfiguration.CreateLogger();


var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment() || args.Contains("--UseSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "NKS_BACKEND V0.1"); });

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStarted.Register(() =>
    {
        var server = app.Services.GetService(typeof(IServer)) as IServer;
        var addresses = server.Features.Get<IServerAddressesFeature>().Addresses;
        foreach (var address in addresses)
        {
            var swaggerUrl = $"{address}/swagger";
            Log.Information($"Swagger UI started on: {swaggerUrl}");
        }
    });
}

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

Log.Information("Application is running!");

app.Run();