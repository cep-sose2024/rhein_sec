using System.Text.Json;
using System.Text.Json.Nodes;
using backend.Controllers.app;
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
public class Apidemo : ControllerBase
{
    private VaultCon _vaultCon = new();
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Apidemo"/> class.
    /// </summary>
    /// <param name="logger">The logger to be used by the instance.</param>
    public Apidemo(ILogger<Apidemo> logger)
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
    public async Task<IActionResult> GetToken()
    {
        var token = "";
        var userToken = VaultCon.GenerateToken(80);
        var tokenExists = true;
        while (tokenExists)
        {
            if (tokenExists)
                userToken = VaultCon.GenerateToken(80);
            tokenExists = await _vaultCon.CheckToken(userToken);
        }

        for (var i = 0; i < _vaultCon._tokens.Count; i++)
            token = await _vaultCon.CreateUserToken(
                _vaultCon._defpolicyname,
                _vaultCon._addresses[i],
                _vaultCon._tokens[i],
                userToken
            );

        var dataToSave = new JsonObject { ["keys"] = new JsonArray() };
        dataToSave.Add("timestamp", ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds());
        var jsonString = dataToSave.ToString();

        await PutSecret(userToken, JsonDocument.Parse(jsonString).RootElement);

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
    public async Task<IActionResult> AddSecrets([FromBody] SecretModel secretModel)
    {
        var properties = secretModel.GetType().GetProperties().Select(p => p.Name).ToList();
        if (!properties.SequenceEqual(new List<string> { "Token", "Data" }))
            return BadRequest("Invalid request format.");

        if (string.IsNullOrWhiteSpace(secretModel.Token))
            return BadRequest("Token and Data are required.");

        var oldToken = secretModel.Token;
        var jsonData = secretModel.Data;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var jsonString = JsonSerializer.Serialize(jsonData, options);
        var jsonElement = JsonDocument.Parse(jsonString).RootElement;

        var tokenExists = await _vaultCon.CheckToken(oldToken);
        if (tokenExists)
        {
            var validationResult = ValidateJsonData(jsonElement);
            if (validationResult != null)
                return validationResult;

            var ret = await PutSecret(oldToken, jsonElement);

            if (ret > 199 && ret < 300)
            {
                var newToken = await RotateToken(oldToken, secretModel.Data.timestamp);
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
    public async Task<IActionResult> GetSecrets([FromBody] TokenModel tokenModel)
    {
        var oldToken = tokenModel.Token;
        var tokenExists = await _vaultCon.CheckToken(oldToken);
        object ret = null!;
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
            var retJObject = JObject.Parse(ret.ToString() ?? throw new InvalidOperationException());
            var data = retJObject.GetValue("data");
            var newToken = await RotateToken(oldToken, (data!["timestamp"] ?? throw new InvalidOperationException()).Value<long>());
            var timestamp = Checktimestamp(oldToken, newToken, (data["timestamp"] ?? throw new InvalidOperationException()).Value<long>());
            data["timestamp"] = timestamp;
            var returnObject = new { data, newToken };
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
    public async Task<IActionResult> DeleteSecrets([FromBody] TokenModel tokenModel)
    {
        var oldToken = tokenModel.Token;
        var tokenExists = await _vaultCon.CheckToken(oldToken);
        var ret = 0;
        if (tokenExists)
        {
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
                ret = await _vaultCon.DeleteSecrets(oldToken, _vaultCon._addresses[i]);

            var dataToSave = new JsonObject { ["keys"] = new JsonArray() };
            dataToSave.Add("timestamp", ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds());

            var jsonString = dataToSave.ToString();
            await PutSecret(oldToken, JsonDocument.Parse(jsonString).RootElement);

            var newToken = await RotateToken(oldToken, 0);
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
        var tokenExists = await _vaultCon.CheckToken(oldToken);
        object ret = null!;

        if (tokenExists)
        {
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
            {
                var secret = await _vaultCon.GetSecrets(oldToken, _vaultCon._addresses[i]);
                if (ret == null)
                {
                    ret = secret;
                }
                if (
                    ret is JObject
                    && secret is JObject
                    && JToken.DeepEquals((JObject)ret, (JObject)secret)
                ) { }
                else if (!ret.Equals(secret))
                {
                    return StatusCode(500, "Internal server Error");
                }
            }

            var retJObject = JObject.Parse(ret.ToString() ?? throw new InvalidOperationException());
            var keysArray = (JArray)retJObject["data"]?["keys"]!;

            var existingKey = (keysArray ?? throw new InvalidOperationException()).FirstOrDefault(obj =>
                obj is JObject jObj
                && jObj["id"] != null
                && (jObj["id"] ?? throw new InvalidOperationException()).Value<string>()?.ToLower() == keyPairModel.Name.ToLower()
            );
            string newToken;
            if (existingKey != null)
            {
                var errorResponse = new
                {
                    message = $"Key with ID {keyPairModel.Name} already exists."
                };
                return BadRequest(errorResponse);
            }

            JObject keyPair;

            if (keyPairModel.Type.ToLower().Equals("ecdh"))
            {
                keyPair = Crypto.GetxX25519KeyPair(keyPairModel.Name);
            }
            else if (keyPairModel.Type.ToLower().Equals("ecdsa"))
            {
                keyPair = Crypto.GetEd25519KeyPair(keyPairModel.Name);
            }
            else if (keyPairModel.Type.ToLower().Equals("aes"))
            {
                if (Enum.GetNames(typeof(Crypto.SymmetricModes)).All(mode => mode.ToLower() != keyPairModel.CipherType.ToLower()) || !Enum.TryParse(
                        keyPairModel.CipherType,
                        true,
                        out Crypto.SymmetricModes symmetricMode
                    )
                )
                {
                    var availableModes = string.Join(
                        ", ",
                        Enum.GetNames(typeof(Crypto.SymmetricModes))
                    );
                    return BadRequest(
                        $"Invalid symmetric mode. Available modes are: {availableModes}."
                    );
                }

                if (
                    !keyPairModel.Length.HasValue
                    || !Enum.IsDefined(typeof(Crypto.AesKeyLength), keyPairModel.Length.Value)
                )
                {
                    var availableLengths = string.Join(
                        ", ",
                        Enum.GetValues(typeof(Crypto.AesKeyLength)).Cast<int>()
                    );
                    return BadRequest(
                        $"Invalid key length. Supported lengths are: {availableLengths}."
                    );
                }

                keyPair = Crypto.GetAesKey(
                    keyPairModel.Name,
                    symmetricMode,
                    keyPairModel.Length.Value
                );
            }
            else if (keyPairModel.Type.ToLower().Equals("rsa"))
            {
                if (
                    keyPairModel.Length.HasValue
                    && !Enum.GetValues(typeof(Crypto.RsaKeyLengths))
                        .Cast<int>()
                        .Contains(keyPairModel.Length.Value)
                )
                    return BadRequest(
                        new
                        {
                            message = $"Invalid key length. Supported lengths are {string.Join(", ", Enum.GetValues(typeof(Crypto.RsaKeyLengths)).Cast<int>())}."
                        }
                    );

                var keySize = keyPairModel.Length.HasValue
                    ? keyPairModel.Length.Value
                    : (int)Crypto.RsaKeyLengths.L2048;
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
                var data = new JObject { { "keys", new JArray { keyPair } } };
                retJObject.Add("data", data);
            }
            newToken = await RotateToken(oldToken, (retJObject["data"]?["timestamp"] ?? throw new InvalidOperationException()).Value<long>());
            var timestamp = Checktimestamp(
                oldToken,
                newToken,
                (retJObject["data"]?["timestamp"] ?? throw new InvalidOperationException()).Value<long>()
            );
            retJObject["data"]!["timestamp"] = timestamp;
            var putRetCode = await PutSecret(
                oldToken,
                JsonDocument.Parse(retJObject["data"]?.ToString() ?? throw new InvalidOperationException()).RootElement
            );
            if (putRetCode >= 400 && putRetCode < 600)
                return StatusCode(
                    putRetCode,
                    new { message = "Error occurred while putting the secret", newToken }
                );

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
        var tokenExists = await _vaultCon.CheckToken(token);
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
    /// <param name="timestamp">The timestamp of the token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the new user token.</returns>
    /// <remarks>
    /// This method first checks if the difference between the current timestamp and the provided timestamp is less than the token refresh time. If it is, the method returns the original user token.
    /// If the difference is greater, the method generates a new token and rotates the user token at all Vault addresses. The new token is then returned.
    /// </remarks>
    private async Task<string> RotateToken(string userToken, long timestamp)
    {
        var currentTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        if (currentTimestamp - timestamp < _vaultCon._token_refresh)
            return userToken;
        var newToken = VaultCon.GenerateToken(80);
        for (var i = 0; i < _vaultCon._addresses.Count; i++)
            newToken = await _vaultCon.RotateUserToken(
                _vaultCon._defpolicyname,
                _vaultCon._addresses[i],
                _vaultCon._tokens[i],
                userToken,
                newToken,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );

        return newToken;
    }

    /// <summary>
    /// Checks the timestamp of a token.
    /// </summary>
    /// <param name="oldToken">The original token.</param>
    /// <param name="newToken">The new token.</param>
    /// <param name="timestamp">The timestamp of the token.</param>
    /// <returns>The timestamp of the token. If the old token is equal to the new token, the method returns the provided timestamp. Otherwise, it returns the current timestamp.</returns>
    public static long Checktimestamp(string oldToken, string newToken, long timestamp)
    {
        if (oldToken.Equals(newToken))
            return timestamp;
        var currentTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        return currentTimestamp;
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

        var lowerCaseData = ConvertKeysToLower(data);

        if (
            !lowerCaseData.TryGetValue("keys", out var keysElement)
            || keysElement.ValueKind != JsonValueKind.Array
        )
            return BadRequest("Keys field is missing or is not an array.");
        if (!lowerCaseData.TryGetValue("timestamp", out var timestampElement))
            return BadRequest("Timestamp field is missing.");

        if (timestampElement.ValueKind != JsonValueKind.Number)
            return BadRequest("Timestamp field is not a number.");

        var timestamp = timestampElement.GetInt64();
        var currentTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

        if (currentTimestamp - timestamp > 300)
            return BadRequest("Timestamp is too far in the past.");
        if (timestamp - currentTimestamp > 300)
            return BadRequest("Timestamp is too far in the future.");
        var ids = new HashSet<string>();

        foreach (var keyElement in keysElement.EnumerateArray())
        {
            var key = keyElement.Deserialize<Dictionary<string, JsonElement>>(options);
            if (key == null)
                continue;

            var id = key.TryGetValue("id", out var idElement) ? idElement.GetString() : null;
            var type = key.TryGetValue("type", out var typeElement)
                ? typeElement.GetString()?.ToLower()
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
            var cipherType = key.TryGetValue("ciphertype", out var cipherTypeElement)
                ? cipherTypeElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
                return BadRequest("Key id or type is missing or invalid.");

            if (!ids.Add(id))
                return BadRequest($"Duplicate key id found: {id}");

            if (type != "ecdh" && type != "ecdsa" && type != "rsa" && type != "aes")
                return BadRequest(
                    $"Invalid key type: {type}. Supported types are ecdh, ecdsa, rsa, aes."
                );

            if ((type == "ecdh" || type == "ecdsa") && string.IsNullOrWhiteSpace(curve))
                return BadRequest("Curve is required for key type: {type}");

            if (type == "rsa")
            {
                if (
                    length == null
                    || !Enum.GetValues(typeof(Crypto.RsaKeyLengths))
                        .Cast<int>()
                        .Contains(length.Value)
                )
                    return BadRequest(
                        new
                        {
                            message = $"Invalid key length. Supported lengths are {string.Join(", ", Enum.GetValues(typeof(Crypto.RsaKeyLengths)).Cast<int>())}."
                        }
                    );
                if (
                    (privateKey != null && privateKey.Length > 684)
                    || (publicKey != null && publicKey.Length > 684)
                )
                    return BadRequest("The RSA key length is too long.");
            }

            if (type == "aes")
            {
                if (
                    length == null
                    || !Enum.GetValues(typeof(Crypto.AesKeyLength))
                        .Cast<int>()
                        .Contains(length.Value)
                )
                    return BadRequest(
                        new
                        {
                            message = $"Invalid key length. Supported lengths are {string.Join(", ", Enum.GetValues(typeof(Crypto.AesKeyLength)).Cast<int>())}."
                        }
                    );
                if (!Enum.TryParse<Crypto.SymmetricModes>(cipherType, true, out _))
                    return BadRequest(
                        new
                        {
                            message = $"Invalid cipher type. Supported types are {string.Join(", ", Enum.GetNames(typeof(Crypto.SymmetricModes)))}."
                        }
                    );
            }
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
