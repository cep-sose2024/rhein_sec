using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace backend.Controllers.example;

/// <summary>
/// The VaultCon class is responsible for managing interactions with the Vault service.
/// This includes creating user policies, generating user tokens, managing secrets, and more.
/// </summary>
public class VaultCon
{
    private static readonly HttpClient client = new();

    public readonly string _defpolicyname = "user";
    public List<string> _addresses = new();
    public List<string> _tokens = new();

    /// <summary>
    /// Initializes a new instance of the VaultCon class.
    /// Reads the configuration and creates user policies for each address and token pair.
    /// </summary>
    public VaultCon()
    {
        ReadConfig();
        for (var i = 0; i < _addresses.Count; i++)
            CreateUserPolicy(_defpolicyname, PolicyOptions.CRUDPolicy, _addresses[i], _tokens[i]);
    }

    /// <summary>
    /// Asynchronously creates a user policy with the specified name and permissions at the given Vault address.
    /// </summary>
    /// <param name="policyName">The name of the policy to be created.</param>
    /// <param name="policyPermissions">The permissions to be assigned to the policy. These permissions should be in HCL (HashiCorp Configuration Language) format.</param>
    /// <param name="address">The Vault server address where the policy will be created.</param>
    /// <param name="token">The Vault token used for authentication.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the name of the created policy.</returns>
    public async Task<string> CreateUserPolicy(
        string policyName,
        string policyPermissions,
        string address,
        string token
    )
    {
        var url = $"{address}/v1/sys/policies/acl/{policyName}";
        var policy = $"path \"cubbyhole/secrets\" {{ {policyPermissions} }}";

        var json = new JObject { ["policy"] = policy };
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.PostAsync(url, content);

        return policyName;
    }

    /// <summary>
    /// Asynchronously creates a user token with the specified policy at the given Vault address.
    /// </summary>
    /// <param name="policy">The policy to be assigned to the token.</param>
    /// <param name="address">The Vault server address where the token will be created.</param>
    /// <param name="token">The Vault token used for authentication.</param>
    /// <param name="userToken">The user token to be created.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created user token.</returns>
    public async Task<string> CreateUserToken(
        string policy,
        string address,
        string token,
        string userToken
    )
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

    /// <summary>
    /// Asynchronously creates a secret at the given Vault address.
    /// </summary>
    /// <param name="token">The Vault token used for authentication.</param>
    /// <param name="newSecretJson">The JSON string that represents the new secret to be created.</param>
    /// <param name="address">The Vault server address where the secret will be created.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP status code of the response.</returns>
    public async Task<int> CreateSecret(string token, string newSecretJson, string address)
    {
        var url = $"{address}/v1/cubbyhole/secrets";

        // Create a StringContent using the JSON string directly
        var content = new StringContent(newSecretJson, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.PostAsync(url, content);

        return (int)response.StatusCode;
    }

    /// <summary>
    /// Asynchronously retrieves the secrets from the given Vault address.
    /// </summary>
    /// <param name="token">The Vault token used for authentication.</param>
    /// <param name="address">The Vault server address from where the secrets will be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved secrets in JSON format.</returns>
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

    /// <summary>
    /// Asynchronously deletes the secrets from the given Vault address.
    /// </summary>
    /// <param name="token">The Vault token used for authentication.</param>
    /// <param name="address">The Vault server address from where the secrets will be deleted.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP status code of the response.</returns>
    public async Task<int> DeleteSecrets(string token, string address)
    {
        var url = $"{address}/v1/cubbyhole/secrets";

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        var response = await client.DeleteAsync(url);
        return (int)response.StatusCode;
    }

    /// <summary>
    /// Reads the configuration from a JSON file and populates the addresses and tokens lists.
    /// </summary>
    /// <remarks>
    /// The configuration file is expected to be in the format of a JSON array with each element being a JSON object containing an "address" and a "token".
    /// If the address ends with a "/", it is removed.
    /// </remarks>
    public void ReadConfig()
    {
        var jsonFilePath = "nksconfig.json";
        var jsonText = File.ReadAllText(jsonFilePath);
        var jsonArray = JArray.Parse(jsonText);

        foreach (var jsonObject in jsonArray)
        {
            var address = (string)jsonObject["address"];
            if (address.EndsWith("/"))
                address = address.Remove(address.Length - 1);
            var token = (string)jsonObject["token"];
            _addresses.Add(address);
            _tokens.Add(token);
        }
    }

    /// <summary>
    /// Generates a random token of the specified length.
    /// </summary>
    /// <param name="length">The length of the token to be generated.</param>
    /// <returns>A string that represents the generated token.</returns>
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

    /// <summary>
    /// Asynchronously revokes a token at the given Vault address.
    /// </summary>
    /// <param name="xvToken">The Vault token used for authentication.</param>
    /// <param name="address">The Vault server address where the token will be revoked.</param>
    /// <param name="tokenToDelete">The token to be revoked.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP status code of the response.</returns>
    public async Task<int> DeleteToken(string xvToken, string address, string tokenToDelete)
    {
        var url = $"{address}/v1/auth/token/revoke";

        client.DefaultRequestHeaders.Remove("X-Vault-Token");
        client.DefaultRequestHeaders.Add("X-Vault-Token", xvToken);

        var requestBody = $"{{\"token\": \"{tokenToDelete}\"}}";

        var response = await client.PostAsync(url, new StringContent(requestBody));

        return (int)response.StatusCode;
    }

    /// <summary>
    /// Asynchronously checks if a token exists at the given Vault address.
    /// </summary>
    /// <param name="rootToken">The Vault root token used for authentication.</param>
    /// <param name="userToken">The user token to be checked.</param>
    /// <param name="address">The Vault server address where the token will be checked.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the token exists.</returns>
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

    /// <summary>
    /// Asynchronously checks if a user token exists at all Vault addresses.
    /// </summary>
    /// <param name="userToken">The user token to be checked.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the token exists at all addresses.</returns>
    public async Task<bool> checkToken(string userToken)
    {
        for (var i = 0; i < _addresses.Count(); i++)
            if (!await TokenExists(_tokens[i], userToken, _addresses[i]))
                return false;

        return true;
    }

    /// <summary>
    /// Asynchronously rotates a user token at the given Vault address.
    /// </summary>
    /// <param name="policy">The policy to be assigned to the new token.</param>
    /// <param name="address">The Vault server address where the token will be rotated.</param>
    /// <param name="token">The Vault token used for authentication.</param>
    /// <param name="userToken">The user token to be rotated.</param>
    /// <param name="newToken">The new token to be created.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the new user token.</returns>
    public async Task<string> RotateUserToken(
        string policy,
        string address,
        string token,
        string userToken,
        string newToken
    )
    {
        //store secret in variable
        var secretsObject = await GetSecrets(userToken, address);
        var secretsString = secretsObject.ToString();
        var secretsDeserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            secretsString
        );
        JsonElement secrets;
        if (secretsDeserialized != null && secretsDeserialized.ContainsKey("data"))
        {
            var jsonData = JsonConvert.SerializeObject(secretsDeserialized["data"]);
            secrets = JsonDocument.Parse(jsonData).RootElement;
        }
        else
        {
            secrets = JsonDocument.Parse("{}").RootElement;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        //create new token
        await CreateUserToken(policy, address, token, newToken);
        //store secret from variable with new token
        await CreateSecret(newToken, secrets.ToString(), address);
        //delete old secret in old token
        await DeleteToken(token, address, userToken);
        //return new token
        stopwatch.Stop();
        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        Console.WriteLine($"Time taken222: {elapsedMilliseconds} ms");
        return newToken;
    }
}
