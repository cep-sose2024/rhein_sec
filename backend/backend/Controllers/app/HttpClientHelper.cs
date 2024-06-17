namespace backend.Controllers.app;

public static class HttpClientHelper
{
    public static readonly HttpClientHandler Handler;

    static HttpClientHelper()
    {
        Handler = new HttpClientHandler();
        var insecure = Environment
            .GetCommandLineArgs()
            .Any(arg => arg.Equals("-insecure", StringComparison.OrdinalIgnoreCase));

        if (insecure)
            Handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    public static HttpClient CreateClient()
    {
        return new HttpClient(Handler);
    }
}