using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using backend.Controllers.app.logging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

ServicePointManager.ServerCertificateValidationCallback = delegate
{
    return true;
};

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Host.UseSerilog(
    (context, _, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
);
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
var useHttps = true;
if (!CheckCerts("certs"))
{
    Log.Error("\n\nWARNING Missing certificates! Defaulting to HTTP.\n\n");
    useHttps = false;
    if (!args.Contains("-http"))
    {
        Log.Error("Missing -http argument ");
        return;
    }
}
else if (args.Contains("-http"))
{
    Log.Warning("Certificates are present, ignoring -http argument and defaulting to HTTPS.");
}
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddSerilog(Log.Logger);
});
builder.WebHost.ConfigureKestrel(
    (_, serverOptions) =>
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

        if (!useHttps)
        {
            serverOptions.ListenAnyIP(portNumber);
        }
        else
        {
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

                serverOptions.ListenAnyIP(
                    portNumber,
                    listenOptions =>
                    {
                        listenOptions.UseHttps(
                            certWithKey,
                            options =>
                            {
                                options.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                                options.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                                options.OnAuthenticate = (_, sslOptions) =>
                                {
#pragma warning disable CA1416
                                    sslOptions.CipherSuitesPolicy = new CipherSuitesPolicy(
                                        new[]
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
                                        }
                                    );
#pragma warning restore CA1416
                                };
                            }
                        );
                    }
                );
            }
        }
    }
);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});
builder
    .Services.AddControllers()
    .AddNewtonsoftJson(options =>
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

if (useHttps)
{
    app.Use(
        async (context, next) =>
        {
            context.Response.Headers.Append(
                "Strict-Transport-Security",
                "max-age=31536000; includeSubDomains"
            );
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            await next();
        }
    );

    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment() || args.Contains("--UseSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NKS_BACKEND V0.1");
    });

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStarted.Register(() =>
    {
        var server = app.Services.GetService(typeof(IServer)) as IServer;
        var addresses = server!.Features.Get<IServerAddressesFeature>()!.Addresses;
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
        diagnosticContext.Set(
            "RemoteIpAddress",
            httpContext.Connection.RemoteIpAddress?.ToString()
        );
    };
});

//app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

Log.Information("Application is running!");

app.Run();

static bool CheckCerts(string path)
{
    string privateKeyPath = Path.Combine(path, "key.key");
    string certificatePath = Path.Combine(path, "cert.crt");

    return File.Exists(privateKeyPath) && File.Exists(certificatePath);
}