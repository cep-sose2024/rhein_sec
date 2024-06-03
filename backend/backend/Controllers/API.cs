using System.Text.Json;
using System.Text.Json.Nodes;
using backend.Controllers.example;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace backend.Controllers;

/// <summary>
/// Provides API endpoints for managing tokens and secrets in Vault.
/// </summary>
/// <remarks>
/// This class includes endpoints for generating a new token, adding secrets, retrieving secrets, deleting secrets, and generating and saving key pairs.
/// It inherits from the ControllerBase class, which provides the base class for an MVC controller without view support.
/// </remarks>
[ApiController]
//[Route("[controller]")]
public class apidemo : ControllerBase
{
    private VaultCon _vaultCon = new();
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="apidemo"/> class.
    /// </summary>
    /// <param name="logger">The logger to be used by the instance.</param>
    public apidemo(ILogger<apidemo> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Asynchronously generates a new user token and stores an empty set of keys and signatures associated with it.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult that can be one of the following:
    /// OkObjectResult (200) - The operation was successful. The result value is the new user token.
    /// </returns>
    [HttpGet("getToken/")]
    public async Task<IActionResult> getToken()
    {
        var token = "";
        var user_token = VaultCon.GenerateToken(80);
        var tokenExists = true;
        while (tokenExists)
        {
            if (tokenExists)
                user_token = VaultCon.GenerateToken(80);
            tokenExists = await _vaultCon.checkToken(user_token);
        }

        for (var i = 0; i < _vaultCon._tokens.Count; i++)
            token = await _vaultCon.CreateUserToken(
                _vaultCon._defpolicyname,
                _vaultCon._addresses[i],
                _vaultCon._tokens[i],
                user_token
            );

        var dataToSave = new JsonObject { ["keys"] = new JsonArray() };

        var jsonString = dataToSave.ToString();

        await PutSecret(user_token, JsonDocument.Parse(jsonString).RootElement);

        return Ok(token);
    }

    /// <summary>
    /// Asynchronously adds secrets to the vault.
    /// </summary>
    /// <param name="secretModel">The model containing the token and data to be added as a secret.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult that can be one of the following:
    /// OkObjectResult (200) - The operation was successful. The result value is an object containing the return code and the new token.
    /// BadRequestObjectResult (400) - The operation failed due to an invalid request format, missing token, internal server error, or unknown user token.
    /// </returns>
    [HttpPost("addSecrets/")]
    public async Task<IActionResult> addSecrets([FromBody] SecretModel secretModel)
    {
        var properties = secretModel.GetType().GetProperties().Select(p => p.Name).ToList();
        if (!properties.SequenceEqual(new List<string> { "Token", "Data" }))
            return BadRequest("Invalid request format.");

        if (string.IsNullOrWhiteSpace(secretModel.Token))
            return BadRequest("Token and Data are required.");

        var oldToken = secretModel.Token;
        var newToken = "";
        var jsonData = secretModel.Data;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var jsonString = JsonSerializer.Serialize(jsonData, options);
        var jsonElement = JsonDocument.Parse(jsonString).RootElement;

        var tokenExists = await _vaultCon.checkToken(oldToken);
        var ret = 0;
        if (tokenExists)
        {
            var validationResult = ValidateJsonData(jsonElement);
            if (validationResult != null)
                return validationResult;

            ret = await PutSecret(oldToken, jsonElement);

            if (ret > 199 && ret < 300)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                newToken = await RotateToken(oldToken);
                stopwatch.Stop();
                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"Time taken: {elapsedMilliseconds} ms");
                var returnObject = new { returnCode = ret, newToken };
                return Ok(returnObject);
            }
            else
            {
                return BadRequest("Internal server Error");
            }
        }

        return BadRequest("Unknown User Token");
    }

    /// <summary>
    /// Asynchronously retrieves secrets associated with a given token.
    /// </summary>
    /// <param name="tokenModel">The model containing the token used to retrieve secrets.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult that can be one of the following:
    /// OkObjectResult (200) - The operation was successful. The result value is an object containing the retrieved data and the new token.
    /// BadRequestObjectResult (400) - The operation failed due to an unknown user token.
    /// StatusCodeResult (500) - The operation failed due to an internal server error.
    /// </returns>
    [HttpPost("getSecrets/")]
    public async Task<IActionResult> getSecrets([FromBody] TokenModel tokenModel)
    {
        var oldToken = tokenModel.Token;
        var newToken = "";
        var tokenExists = await _vaultCon.checkToken(oldToken);
        object ret = null;
        if (tokenExists)
        {
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
            {
                var secret = await _vaultCon.GetSecrets(oldToken, _vaultCon._addresses[i]);
                if (ret == null)
                {
                    ret = secret;
                }
                else if (
                    ret is JObject
                    && secret is JObject
                    && JToken.DeepEquals((JObject)ret, (JObject)secret)
                ) { }
                else if (!ret.Equals(secret))
                {
                    return StatusCode(500, "Internal server Error");
                }
            }

            var retJObject = JObject.Parse(ret.ToString());
            newToken = await RotateToken(oldToken);
            var returnObject = new { data = retJObject.GetValue("data"), newToken };
            return Ok(returnObject);
        }

        return BadRequest("Unknown User Token");
    }

    /// <summary>
    /// Asynchronously deletes secrets associated with a given token.
    /// </summary>
    /// <param name="tokenModel">The model containing the token used to delete secrets.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult that can be one of the following:
    /// OkObjectResult (200) - The operation was successful. The result value is an object containing the token existence status, return code, and the new token.
    /// BadRequestObjectResult (400) - The operation failed due to an unknown user token.
    /// </returns>
    [HttpDelete("deleteSecrets/")]
    public async Task<IActionResult> deleteSecrets([FromBody] TokenModel tokenModel)
    {
        var oldToken = tokenModel.Token;
        var newToken = "";
        var tokenExists = await _vaultCon.checkToken(oldToken);
        var ret = 0;
        if (tokenExists)
        {
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
                ret = await _vaultCon.DeleteSecrets(oldToken, _vaultCon._addresses[i]);

            var dataToSave = new JsonObject { ["keys"] = new JsonArray(), };

            var jsonString = dataToSave.ToString();
            await PutSecret(oldToken, JsonDocument.Parse(jsonString).RootElement);

            newToken = await RotateToken(oldToken);

            var returnObject = new
            {
                tokenExists,
                returnCode = ret,
                newToken
            };
            return Ok(returnObject);
        }

        return BadRequest("Unknown User Token");
    }

    /// <summary>
    /// Asynchronously generates a new key pair and saves it to the vault.
    /// </summary>
    /// <param name="keyPairModel">The model containing the token and information about the key pair to be generated.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult that can be one of the following:
    /// OkObjectResult (200) - The operation was successful. The result value is an object containing the data and the new token.
    /// BadRequestObjectResult (400) - The operation failed due to an unknown user token, existing key ID, invalid key length, or invalid key type.
    /// StatusCodeResult (500) - The operation failed due to an internal server error.
    /// </returns>
    [HttpPost("generateAndSaveKeyPair/")]
    public async Task<IActionResult> GenerateAndSaveKeyPair([FromBody] KeyPairModel keyPairModel)
    {
        var oldToken = keyPairModel.Token;
        var tokenExists = await _vaultCon.checkToken(oldToken);
        object ret = null;

        if (tokenExists)
        {
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
            {
                var secret = await _vaultCon.GetSecrets(oldToken, _vaultCon._addresses[i]);
                if (ret == null)
                {
                    ret = secret;
                }
                else if (
                    ret is JObject
                    && secret is JObject
                    && JToken.DeepEquals((JObject)ret, (JObject)secret)
                ) { }
                else if (!ret.Equals(secret))
                {
                    return StatusCode(500, "Internal server Error");
                }
            }

            Console.WriteLine(ret.ToString());
            var retJObject = JObject.Parse(ret.ToString());
            var keysArray = (JArray)retJObject["data"]["keys"];
            var existingKey = keysArray.FirstOrDefault(obj =>
                obj["id"] != null
                && obj["id"].Value<string>().ToLower() == keyPairModel.Name.ToLower()
            );
            var newToken = "";
            if (existingKey != null)
            {
                //    newToken = await RotateToken(oldToken);
                var errorResponse = new
                {
                    message = $"Key with ID {keyPairModel.Name} already exists."
                };
                return BadRequest(errorResponse);
            }

            var keyPair = new JObject();
            var validKeyLengths = new List<int>
            {
                128,
                192,
                256,
                512,
                1024,
                2048,
                3072,
                4096,
                8192
            };
            if (
                keyPairModel.Length.HasValue && !validKeyLengths.Contains(keyPairModel.Length.Value)
            )
            {
                return BadRequest(
                    new
                    {
                        message = $"Invalid key length. Supported lengths are {string.Join(", ", validKeyLengths)}."
                    }
                );
            }
            if (keyPairModel.Type.ToLower().Equals("ecdh"))
            {
                keyPair = Crypto.GetxX25519KeyPair(keyPairModel.Name);
            }
            else if (keyPairModel.Type.ToLower().Equals("ecdsa"))
            {
                keyPair = Crypto.GetEd25519KeyPair(keyPairModel.Name);
            }
            else if (keyPairModel.Type.ToLower().Equals("rsa"))
            {
                int keySize = keyPairModel.Length.HasValue ? keyPairModel.Length.Value : 2048;
                keyPair = Crypto.GetRsaKeyPair(keyPairModel.Name, keySize);
            }
            else
            {
                var errorResponse = new
                {
                    message = "Invalid key type. Supported types are ecdh, ecdsa, rsa, please provide a valid key type in the request body via the 'type' field."
                };
                return BadRequest(errorResponse);
            }

            if (SecretHasKeys(retJObject))
            {
                keysArray.Add(keyPair);
            }
            else
            {
                var data = new JObject();
                data.Add("keys", new JArray { keyPair });
                retJObject.Add("data", data);
            }

            var putRetCode = await PutSecret(
                oldToken,
                JsonDocument.Parse(retJObject["data"].ToString()).RootElement
            );
            Console.WriteLine("CODE ::: " + putRetCode);
            newToken = await RotateToken(oldToken);
            var returnObject = new { data = retJObject.GetValue("data"), newToken };

            return Ok(returnObject);
        }

        return BadRequest("Unknown User Token");
    }

    /// <summary>
    /// Asynchronously stores a secret in the vault for a given token.
    /// </summary>
    /// <param name="token">The token associated with the secret.</param>
    /// <param name="data">The secret data to be stored in the vault, represented as a JsonElement.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an integer that represents the status of the operation.
    /// If the token does not exist, the method returns 0. If the token exists, the method returns the result of the CreateSecret operation.</returns>
    private async Task<int> PutSecret(string token, JsonElement data)
    {
        var tokenExists = await _vaultCon.checkToken(token);
        var ret = 0;
        if (tokenExists)
        {
            // Serialize the JsonElement data to a JSON string
            var jsonString = JsonSerializer.Serialize(data);
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
                ret = await _vaultCon.CreateSecret(token, jsonString, _vaultCon._addresses[i]);
        }

        return ret;
    }

    /// <summary>
    /// Checks if a given secret contains keys.
    /// </summary>
    /// <param name="secret">The secret to be checked, represented as a JObject.</param>
    /// <returns>A boolean value that indicates whether the secret contains keys. Returns true if the secret contains keys, otherwise returns false.</returns>
    private static bool SecretHasKeys(JObject secret)
    {
        var containsDataField = secret.ContainsKey("data");
        if (containsDataField)
        {
            var dataObject = secret["data"] as JObject;
            var containsKeysField = dataObject?.ContainsKey("keys") ?? false;
            if (containsKeysField)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Asynchronously rotates a user token.
    /// </summary>
    /// <param name="userToken">The user token to be rotated.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the new user token.</returns>
    private async Task<string> RotateToken(string userToken)
    {
        var newToken = VaultCon.GenerateToken(80);
        for (var i = 0; i < _vaultCon._addresses.Count; i++)
            newToken = await _vaultCon.RotateUserToken(
                _vaultCon._defpolicyname,
                _vaultCon._addresses[i],
                _vaultCon._tokens[i],
                userToken,
                newToken
            );

        return newToken;
    }

    /// <summary>
    /// Validates the JSON data for the keys.
    /// </summary>
    /// <param name="jsonData">The JSON data to be validated, represented as a JsonElement.</param>
    /// <returns>An IActionResult that represents the result of the validation.
    /// If the validation is successful, the method returns null.
    /// If the validation fails, the method returns a BadRequestObjectResult with an error message.</returns>
    private IActionResult? ValidateJsonData(JsonElement jsonData)
    {
        if (
            jsonData.ValueKind == JsonValueKind.Undefined
            || jsonData.ValueKind == JsonValueKind.Null
        )
            return BadRequest("Data field is missing.");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            jsonData.GetRawText(),
            options
        );
        if (data == null)
            return BadRequest("Invalid JSON data.");

        // Convert all keys to lower case
        var lowerCaseData = ConvertKeysToLower(data);

        Console.WriteLine("Lower case data: " + JsonSerializer.Serialize(lowerCaseData));

        if (
            !lowerCaseData.TryGetValue("keys", out var keysElement)
            || keysElement.ValueKind != JsonValueKind.Array
        )
            return BadRequest("Keys field is missing or is not an array.");

        var ids = new HashSet<string>();
        var validLengths = new HashSet<int> { 1024, 2048, 3072, 4096 };

        foreach (var keyElement in keysElement.EnumerateArray())
        {
            var key = keyElement.Deserialize<Dictionary<string, JsonElement>>(options);
            if (key == null)
                continue;

            var id = key.TryGetValue("id", out var idElement) ? idElement.GetString() : null;
            var type = key.TryGetValue("type", out var typeElement)
                ? typeElement.GetString()
                : null;
            var curve = key.TryGetValue("curve", out var curveElement)
                ? curveElement.GetString()
                : null;
            var length = key.TryGetValue("length", out var lengthElement)
                ? lengthElement.GetInt32()
                : (int?)null;
            var publicKey = key.TryGetValue("publickey", out var publicKeyElement)
                ? publicKeyElement.GetString()
                : null;
            var privateKey = key.TryGetValue("privatekey", out var privateKeyElement)
                ? privateKeyElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
                return BadRequest("Public key or private key is missing or invalid.");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
                return BadRequest("Key id or type is missing or invalid.");

            if (!ids.Add(id))
                return BadRequest($"Duplicate key id found: {id}");

            if (type != "ecdh" && type != "ecdsa" && type != "rsa")
                return BadRequest(
                    $"Invalid key type: {type}. Supported types are ecdh, ecdsa, rsa."
                );

            if ((type == "ecdh" || type == "ecdsa") && string.IsNullOrWhiteSpace(curve))
                return BadRequest("Curve is required for key type: {type}");

            if (type == "rsa" && (length == null || !validLengths.Contains(length.Value)))
                return BadRequest(
                    "Invalid length for RSA key type. Supported lengths are 1024, 2048, 3072, 4096."
                );
        }

        return null;
    }

    /// <summary>
    /// Converts all keys in a JSON object to lower case.
    /// </summary>
    /// <param name="data">The JSON object to be processed, represented as a dictionary of string keys and JsonElement values.</param>
    /// <returns>A new dictionary where all keys in the JSON object, including nested objects and arrays, are converted to lower case.</returns>
    private Dictionary<string, JsonElement> ConvertKeysToLower(Dictionary<string, JsonElement> data)
    {
        var lowerCaseData = new Dictionary<string, JsonElement>();
        foreach (var pair in data)
            if (pair.Value.ValueKind == JsonValueKind.Object)
            {
                var lowerCaseSubData = ConvertKeysToLower(
                    pair.Value.EnumerateObject().ToDictionary(kvp => kvp.Name, kvp => kvp.Value)
                );
                lowerCaseData.Add(
                    pair.Key.ToLower(),
                    JsonDocument.Parse(JsonSerializer.Serialize(lowerCaseSubData)).RootElement
                );
            }
            else if (pair.Value.ValueKind == JsonValueKind.Array)
            {
                var lowerCaseArray = new List<JsonElement>();
                foreach (var item in pair.Value.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var lowerCaseSubData = ConvertKeysToLower(
                            item.EnumerateObject().ToDictionary(kvp => kvp.Name, kvp => kvp.Value)
                        );
                        lowerCaseArray.Add(
                            JsonDocument
                                .Parse(JsonSerializer.Serialize(lowerCaseSubData))
                                .RootElement
                        );
                    }
                    else
                    {
                        lowerCaseArray.Add(item);
                    }

                lowerCaseData.Add(
                    pair.Key.ToLower(),
                    JsonDocument.Parse(JsonSerializer.Serialize(lowerCaseArray)).RootElement
                );
            }
            else
            {
                lowerCaseData.Add(pair.Key.ToLower(), pair.Value);
            }

        return lowerCaseData;
    }
}
