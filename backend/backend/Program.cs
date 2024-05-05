using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(serverOptions => { serverOptions.AddServerHeader = false; });
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
            Console.WriteLine($"Swagger UI started on: {swaggerUrl}");
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();