using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Security;
using System.Net.Security;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Authentication;
using System.Security.Cryptography;
using backend.Controllers.example.logging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
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
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365); 
});
builder.Services.AddLogging(loggingBuilder => { loggingBuilder.AddSerilog(Log.Logger); });
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.AddServerHeader = false;

    var portArgumentIndex = args.ToList().IndexOf("-port");
    var portNumber = 5000; 
    if (portArgumentIndex >= 0 && args.Length > portArgumentIndex + 1)
    {
        var port = args[portArgumentIndex + 1];
        if (int.TryParse(port, out var userSpecifiedPortNumber))
        {
            portNumber = userSpecifiedPortNumber;
            Log.Information($"Listening on port: {portNumber}");
        }
        else
        {
            Log.Error("Invalid port number provided");
        }
    }

    var cert = new X509Certificate2("certs/cert.crt");

    RsaPrivateCrtKeyParameters privateKeyParameters;
    using (var reader = File.OpenText("certs/key.key"))
    {
        privateKeyParameters = (RsaPrivateCrtKeyParameters)new PemReader(reader).ReadObject();
    }

    var rsaParams = DotNetUtilities.ToRSAParameters(privateKeyParameters);
    using (var rsa = RSA.Create())
    {
        rsa.ImportParameters(rsaParams);

        var certWithKey = cert.CopyWithPrivateKey(rsa);

        serverOptions.ListenAnyIP(portNumber, listenOptions =>
        {
            listenOptions.UseHttps(certWithKey, options =>
            {
                options.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                options.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                options.OnAuthenticate = (context, sslOptions) =>
                {
                    sslOptions.CipherSuitesPolicy = new CipherSuitesPolicy(new[]
                    {
                        // TLS 1.3 Ciphers
                        TlsCipherSuite.TLS_AES_128_GCM_SHA256,
                        TlsCipherSuite.TLS_AES_256_GCM_SHA384,
                        TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,

                        // TLS 1.2 Ciphers
                        TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
                        TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
                        TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
                        TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                        TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                        TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256
                    });
                };
            });
        });
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
app.UseHsts();
app.UseHttpsRedirection();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();


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


//app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

Log.Information("Application is running!");

app.Run();