using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace backend.Controllers.example;

public class VaultCon
{
    private static readonly HttpClient client = new();

    public readonly string _defpolicyname = "user";
    public List<string> _addresses = new();
    public List<string> _tokens = new();

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
        ReadConfig();
        for (var i = 0; i < _addresses.Count; i++)
            CreateUserPolicy(_defpolicyname, PolicyOptions.CRUDPolicy, _addresses[i], _tokens[i]);
    }


    public async Task<string> CreateUserPolicy(string policyName, string policyPermissions, string address,
        string token)
    {
        var url = $"{address}/v1/sys/policies/acl/{policyName}";
        var policy = $"path \"cubbyhole/secrets\" {{ {policyPermissions} }}";

        var json = new JObject
        {
            ["policy"] = policy
        };
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.PostAsync(url, content);

        return policyName;
    }

    public async Task<string> CreateUserToken(string policy, string address, string token, string userToken)
    {
        var url = $"{address}/v1/auth/token/create";

        var json = new JObject
        {
            ["id"] = userToken,
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
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JObject.Parse(responseContent);
        var user_token = responseJson["auth"]["client_token"].ToString();
        return new JObject { ["token"] = user_token }.ToString();
    }

    public async Task<int> CreateSecret(string token, JObject newSecret, string address)
    {
        var url = $"{address}/v1/cubbyhole/secrets";

        var content = new StringContent(newSecret.ToString(), Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.PostAsync(url, content);

        return (int)response.StatusCode;
    }


    public async Task<object> GetSecrets(string token, string address)
    {
        var url = $"{address}/v1/cubbyhole/secrets";

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


    public async Task<int> DeleteSecrets(string token, string address)
    {
            var url = $"{address}/v1/cubbyhole/secrets";

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.DeleteAsync(url);
        return (int)response.StatusCode;
    }

    public void ReadConfig()
    {
        var jsonFilePath = "nksconfig.json";
        var jsonText = File.ReadAllText(jsonFilePath);
        var jsonArray = JArray.Parse(jsonText);

        foreach (var jsonObject in jsonArray)
        {
            var address = (string)jsonObject["address"];
            if (address.EndsWith("/"))
            {
                address = address.Remove(address.Length - 1);
            }
            var token = (string)jsonObject["token"];
            _addresses.Add(address);
            _tokens.Add(token);
        }

    }

    public static string GenerateToken(int length)
    {
        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890*-_";
        var res = new StringBuilder();
        using (var rng = new RNGCryptoServiceProvider())
        {
            var uintBuffer = new byte[4];
            while (length-- > 0)
            {
                rng.GetBytes(uintBuffer);
                var num = BitConverter.ToUInt32(uintBuffer, 0);
                res.Append(valid[(int)(num % (uint)valid.Length)]);
            }
        }

        return res.ToString();
    }

    public async Task<int> DeleteToken(string xvToken, string address, string tokenToDelete)
    {
        var url = $"{address}/v1/auth/token/revoke";

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", xvToken);

        var requestBody = $"{{\"token\": \"{tokenToDelete}\"}}";

        var response = await client.PostAsync(url, new StringContent(requestBody));

        return (int)response.StatusCode;
    }

    public async Task<bool> TokenExists(string rootToken, string userToken, string address)
    {
        var url = $"{address}/v1/auth/token/lookup";

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", rootToken);

        var requestBody = $"{{\"token\": \"{userToken}\"}}";
        var response = await client.PostAsync(url, new StringContent(requestBody));
        var responseBody = await response.Content.ReadAsStringAsync();
        bool tokenExists;
        if (responseBody.Contains("errors") && responseBody.Contains("bad token"))
            tokenExists = false;
        else
            tokenExists = true;
        return tokenExists;
    }

    public async Task<bool> checkToken(string userToken)
    {
        for (var i = 0; i < _addresses.Count(); i++)
            if (!await TokenExists(_tokens[i], userToken, _addresses[i]))
                return false;

        return true;
    }


    public async Task<string> RotateUserToken(string policy, string address, string token, string userToken,
        string newToken)
    {
        //store secret in variable
        var secretsObject = await GetSecrets(userToken, address);
        var secretsString = secretsObject.ToString();
        var secretsDeserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(secretsString);
        JObject secrets;
        if (secretsDeserialized != null && secretsDeserialized.ContainsKey("data"))
        {
            var jsonData = JsonConvert.SerializeObject(secretsDeserialized["data"]);
            secrets = JObject.Parse(jsonData);
        }
        else
        {
            secrets = new JObject();
        }

        //create new token
        await CreateUserToken(policy, address, token, newToken);
        //store secret from variable with new token
        await CreateSecret(newToken, secrets, address);
        //delete old secret in old token
        await DeleteToken(token, address, userToken);
        //return new token
        return newToken;
    }
}