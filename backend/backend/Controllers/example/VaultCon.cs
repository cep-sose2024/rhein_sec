using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace backend.Controllers.example;

public class VaultCon
{
    private static readonly HttpClient client = new();

    private static string? _token = ReadToken();

    public readonly string _defpolicyname = "user";

    /*public static async Task Main(string[] args)
    {

        var stopwatch = Stopwatch.StartNew();
        string policyName = "UserPolicy";
        string actuaname = await CreateUserPolicy(policyName, PolicyOptions.CRUDPolicy);
        string token = await CreateUserToken(actuaname);
        await CreateSecret(token);
        stopwatch.Stop();
        Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");
    }*/
    public VaultCon()
    {
        CreateUserPolicy(_defpolicyname, PolicyOptions.CRUDPolicy);
    }

    public async Task<string> CreateUserPolicy(string policyName, string policyPermissions)
    {
        var url = $"https://localhost:8200/v1/sys/policies/acl/{policyName}";
        var policy = $"path \"cubbyhole/secrets\" {{ {policyPermissions} }}";

        var json = new JObject
        {
            ["policy"] = policy
        };
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", _token);

        var response = await client.PostAsync(url, content);

        return policyName;
    }

    public async Task<string> CreateUserToken(string policy)
    {
        var url = "https://localhost:8200/v1/auth/token/create";

        var json = new JObject
        {
            ["display_name"] = "user1_token13",
            ["explicit_max_ttl"] = "0s",
            ["meta"] = new JObject(),
            ["no_default_policy"] = true,
            ["no_parent"] = true,
            ["num_uses"] = 0,
            ["policies"] = new JArray { policy },
            ["renewable"] = true,
            ["ttl"] = "8760h",
            ["type"] = "service"
        };
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", _token);

        var response = await client.PostAsync(url, content);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JObject.Parse(responseContent);
        var token = responseJson["auth"]["client_token"].ToString();
        return new JObject { ["token"] = token }.ToString();
    }

    public async Task<int> CreateSecret(string token, JObject newSecret)
    {
        var url = "https://localhost:8200/v1/cubbyhole/secrets";

        var content = new StringContent(newSecret.ToString(), Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.PostAsync(url, content);

        return (int)response.StatusCode;
    }


    public async Task<object> GetSecrets(string token)
    {
        var url = "https://localhost:8200/v1/cubbyhole/secrets";

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.GetAsync(url);
        var responseBody = await response.Content.ReadAsStringAsync();

        var responseDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
        object data;
        if (responseDict != null && responseDict.ContainsKey("data"))
        {
            var jsonData = JsonConvert.SerializeObject(responseDict["data"]);
            var jTokenData = JToken.Parse(jsonData);
            data = new JObject { ["data"] = jTokenData }.ToString();
        }
        else
        {
            data = new JObject();
        }
        return data;
    }


    public async Task<int> DeleteSecrets(string token)
    {
        var url = "https://localhost:8200/v1/cubbyhole/secrets";

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.DeleteAsync(url);
        return (int)response.StatusCode;
    }

    public static string? ReadToken()
    {
        var jsonFilePath = "tokens.env";
        var jsonText = File.ReadAllText(jsonFilePath);
        var jsonObject = JObject.Parse(jsonText);
        var vaultToken = (string)jsonObject["VaultToken"]!;
        return vaultToken;
    }
}