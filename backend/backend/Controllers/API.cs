using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers.example;
using Newtonsoft.Json.Linq;


namespace backend.Controllers;

[ApiController]
//[Route("[controller]")]
public class apidemo : ControllerBase
{
    private VaultCon _vaultCon = new();
    private readonly ILogger _logger;

    public apidemo(ILogger<apidemo> logger)
    {
        _logger = logger;
    }

    [HttpGet("getToken/")]
    public async Task<IActionResult> getToken()
    {
        var token = "";
        var user_token = VaultCon.GenerateToken(80);
        var tokenExists = true;
        while (tokenExists)
        {
            if (tokenExists) user_token = VaultCon.GenerateToken(80);
            tokenExists = await _vaultCon.checkToken(user_token);
        }

        for (var i = 0; i < _vaultCon._tokens.Count; i++)
            token = await _vaultCon.CreateUserToken(_vaultCon._defpolicyname, _vaultCon._addresses[i],
                _vaultCon._tokens[i], user_token);

        // Create a new JsonObject instead of JObject
        var dataToSave = new JsonObject
        {
            ["keys"] = new JsonArray(), // Use JsonArray instead of JArray
            ["signatures"] = new JsonArray()
        };

        // Serialize the JsonObject to a string
        var jsonString = dataToSave.ToString();

        // Assuming PutSecret has been refactored to accept a string or JsonElement
        await PutSecret(user_token, JsonDocument.Parse(jsonString).RootElement);

        return Ok(token);
    }


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

        // Serialize the data to a JSON string and parse it to a JsonElement
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var jsonString = JsonSerializer.Serialize(jsonData, options);
        var jsonElement = JsonDocument.Parse(jsonString).RootElement;

        var tokenExists = await _vaultCon.checkToken(oldToken);
        var ret = 0;
        if (tokenExists)
        {

            var validationResult = ValidateJsonData(jsonElement);
            if (validationResult != null) return validationResult;


            ret = await PutSecret(oldToken, jsonElement); // Assuming PutSecret accepts a JsonElement

            if (ret > 199 && ret < 300)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                newToken = await RotateToken(oldToken);
                stopwatch.Stop();
                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"Time taken: {elapsedMilliseconds} ms");
                var returnObject = new
                {
                    returnCode = ret,
                    newToken
                };
                return Ok(returnObject);
            }
            else
                return BadRequest("Internal server Error");
        }

        return BadRequest("Unknown User Token");
    }

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
                else if (ret is JObject && secret is JObject && JToken.DeepEquals((JObject)ret, (JObject)secret))
                {
                }
                else if (!ret.Equals(secret))
                {
                    return StatusCode(500, "Internal server Error");
                }
            }

            var retJObject = JObject.Parse(ret.ToString());
            newToken = await RotateToken(oldToken);
            var returnObject = new
            {
                data = retJObject.GetValue("data"),
                newToken
            };
            return Ok(returnObject);
        }

        return BadRequest("Unknown User Token");
    }


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

            var dataToSave = new JsonObject
            {
                ["keys"] = new JsonArray(),
                ["signatures"] = new JsonArray()
            };

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
                else if (ret is JObject && secret is JObject && JToken.DeepEquals((JObject)ret, (JObject)secret))
                {
                }
                else if (!ret.Equals(secret))
                {
                    return StatusCode(500, "Internal server Error");
                }
            }

            Console.WriteLine(ret.ToString());
            var retJObject = JObject.Parse(ret.ToString());
            var keysArray = (JArray)retJObject["data"]["keys"];
            var existingKey = keysArray.FirstOrDefault(obj =>
                obj["id"] != null && obj["id"].Value<string>().ToLower() == keyPairModel.Name.ToLower());
            var newToken = "";
            if (existingKey != null)
            {
            //    newToken = await RotateToken(oldToken);
                var errorResponse = new
                {
                    message = $"Key with ID {keyPairModel.Name} already exists."};
                return BadRequest(errorResponse);
            }

            var keyPair = new JObject();
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
                keyPair = Crypto.GetRsaKeyPair(keyPairModel.Name);
            }
            else
            {
                var errorResponse = new
                {
                    message =
                        "Invalid key type. Supported types are ecdh, ecdsa, rsa, please provide a valid key type in the request body via the 'type' field."
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

            var putRetCode = await PutSecret(oldToken, JsonDocument.Parse(retJObject["data"].ToString()).RootElement);            
            Console.WriteLine("CODE ::: "+putRetCode);
            newToken = await RotateToken(oldToken);
            var returnObject = new
            {
                data = retJObject.GetValue("data"),
                newToken
            };

            return Ok(returnObject);
        }

        return BadRequest("Unknown User Token");
    }


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


    private static bool SecretHasKeys(JObject secret)
    {
        var containsDataField = secret.ContainsKey("data");
        if (containsDataField)
        {
            var dataObject = secret["data"] as JObject;
            var containsKeysField = dataObject?.ContainsKey("keys") ?? false;
            if (containsKeysField) return true;
        }

        return false;
    }

    private async Task<string> RotateToken(string userToken)
    {
        var newToken = VaultCon.GenerateToken(80);
        for (var i = 0; i < _vaultCon._addresses.Count; i++)
            newToken = await _vaultCon.RotateUserToken(_vaultCon._defpolicyname, _vaultCon._addresses[i],
                _vaultCon._tokens[i], userToken, newToken);

        return newToken;
    }

    private IActionResult? ValidateJsonData(JsonElement jsonData)
    {
        if (jsonData.ValueKind == JsonValueKind.Undefined || jsonData.ValueKind == JsonValueKind.Null)
            return BadRequest("Data field is missing.");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonData.GetRawText(), options);
        if (data == null)
            return BadRequest("Invalid JSON data.");

        // Convert all keys to lower case
        var lowerCaseData = ConvertKeysToLower(data);

        Console.WriteLine("Lower case data: " + JsonSerializer.Serialize(lowerCaseData));

        if (!lowerCaseData.TryGetValue("keys", out JsonElement keysElement) || keysElement.ValueKind != JsonValueKind.Array)
            return BadRequest("Keys field is missing or is not an array.");


    var ids = new HashSet<string>();
    var validLengths = new HashSet<int> { 1024, 2048, 3072, 4096 };

    foreach (var keyElement in keysElement.EnumerateArray())
    {
        var key = keyElement.Deserialize<Dictionary<string, JsonElement>>(options);
        if (key == null)
            continue;

        var id = key.TryGetValue("id", out JsonElement idElement) ? idElement.GetString() : null;
        var type = key.TryGetValue("type", out JsonElement typeElement) ? typeElement.GetString() : null;
        var curve = key.TryGetValue("curve", out JsonElement curveElement) ? curveElement.GetString() : null;
        var length = key.TryGetValue("length", out JsonElement lengthElement) ? lengthElement.GetInt32() : (int?)null;
        var publicKey = key.TryGetValue("publickey", out JsonElement publicKeyElement) ? publicKeyElement.GetString() : null;
        var privateKey = key.TryGetValue("privatekey", out JsonElement privateKeyElement) ? privateKeyElement.GetString() : null;

        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
            return BadRequest("Public key or private key is missing or invalid.");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
            return BadRequest("Key id or type is missing or invalid.");

        if (!ids.Add(id))
            return BadRequest($"Duplicate key id found: {id}");

        if (type != "ecdh" && type != "ecdsa" && type != "rsa")
            return BadRequest($"Invalid key type: {type}. Supported types are ecdh, ecdsa, rsa.");

        if ((type == "ecdh" || type == "ecdsa") && string.IsNullOrWhiteSpace(curve))
            return BadRequest("Curve is required for key type: {type}");

        if (type == "rsa" && (length == null || !validLengths.Contains(length.Value)))
            return BadRequest("Invalid length for RSA key type. Supported lengths are 1024, 2048, 3072, 4096.");
    }
    return null;
}
    private Dictionary<string, JsonElement> ConvertKeysToLower(Dictionary<string, JsonElement> data)
    {
        var lowerCaseData = new Dictionary<string, JsonElement>();
        foreach (var pair in data)
        {
            if (pair.Value.ValueKind == JsonValueKind.Object)
            {
                var lowerCaseSubData = ConvertKeysToLower(pair.Value.EnumerateObject().ToDictionary(kvp => kvp.Name, kvp => kvp.Value));
                lowerCaseData.Add(pair.Key.ToLower(), JsonDocument.Parse(JsonSerializer.Serialize(lowerCaseSubData)).RootElement);
            }
            else if (pair.Value.ValueKind == JsonValueKind.Array)
            {
                var lowerCaseArray = new List<JsonElement>();
                foreach (var item in pair.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var lowerCaseSubData = ConvertKeysToLower(item.EnumerateObject().ToDictionary(kvp => kvp.Name, kvp => kvp.Value));
                        lowerCaseArray.Add(JsonDocument.Parse(JsonSerializer.Serialize(lowerCaseSubData)).RootElement);
                    }
                    else
                    {
                        lowerCaseArray.Add(item);
                    }
                }
                lowerCaseData.Add(pair.Key.ToLower(), JsonDocument.Parse(JsonSerializer.Serialize(lowerCaseArray)).RootElement);
            }
            else
            {
                lowerCaseData.Add(pair.Key.ToLower(), pair.Value);
            }
        }
        return lowerCaseData;
    }


}