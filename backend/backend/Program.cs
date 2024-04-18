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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();