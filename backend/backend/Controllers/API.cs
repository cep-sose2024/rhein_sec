using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers.example;
using Newtonsoft.Json.Linq;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class apidemo : ControllerBase
{
    private VaultCon _vaultCon = new();

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
        
        var dataToSave = new JObject
        {
            ["keys"] = new JArray(),
            ["signatures"] = new JArray()
        };
        await putSecret(user_token, dataToSave);

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
        var tokenExists = await _vaultCon.checkToken(oldToken);
        var ret = 0;
        if (tokenExists)
        {
            newToken = await rotateToken(oldToken);
            ret = await putSecret(newToken, jsonData.ToJObject());
            var returnObject = new
            {
                returnCode = ret,
                newToken
            };

            if (ret > 199 && ret < 300)
                return Ok(returnObject);
            else
                return BadRequest(returnObject);
        }
        else
        {
            return BadRequest("Unknown User Token");
        }
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
            newToken = await rotateToken(oldToken);
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
            {
                var secret = await _vaultCon.GetSecrets(newToken, _vaultCon._addresses[i]);
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
            newToken = await rotateToken(oldToken);
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
                ret = await _vaultCon.DeleteSecrets(newToken, _vaultCon._addresses[i]);
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
        var newToken = "";
        var tokenExists = await _vaultCon.checkToken(oldToken);
        object ret = null;
        if (tokenExists)
        {
            newToken = await rotateToken(oldToken);
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
            {
                var secret = await _vaultCon.GetSecrets(newToken, _vaultCon._addresses[i]);
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
            var keysArray = (JArray)retJObject["data"]["keys"];
            var existingKey = keysArray.FirstOrDefault(obj => obj["id"].Value<string>() == keyPairModel.Name);

            if (existingKey != null)
            {
                var errorResponse = new
                {
                    message = $"Key with ID {keyPairModel.Name} already exists.",
                    newToken
                };
                return BadRequest(errorResponse);
            }

            var keyPair = new JObject();
            if (keyPairModel.Type.ToLower().Equals("ecc"))
                keyPair = Crypto.GetxX25519KeyPair(keyPairModel.Name);
            else if (keyPairModel.Type.ToLower().Equals("rsa")) keyPair = Crypto.GetRsaKeyPair(keyPairModel.Name);

            if (secretHasKeys(retJObject))
            {
                keysArray.Add(keyPair);
            }
            else
            {
                var data = new JObject();
                data.Add("keys", new JArray { keyPair });
                retJObject.Add("data", data);
            }

            var putRetCode = putSecret(newToken, retJObject["data"] as JObject);
            var returnObject = new
            {
                data = retJObject.GetValue("data"),
                newToken
            };

            return Ok(returnObject);
        }

        return BadRequest("Unknown User Token");
    }


    private async Task<int> putSecret(string token, JObject data)
    {
        var tokenExists = await _vaultCon.checkToken(token);
        var ret = 0;
        if (tokenExists)
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
                ret = await _vaultCon.CreateSecret(token, data, _vaultCon._addresses[i]);

        return ret;
    }

    private bool secretHasKeys(JObject secret)
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

    private async Task<string> rotateToken(string userToken)
    {
        var newToken = VaultCon.GenerateToken(80);
        for (var i = 0; i < _vaultCon._addresses.Count; i++)
            newToken = await _vaultCon.RotateUserToken(_vaultCon._defpolicyname, _vaultCon._addresses[i],
                _vaultCon._tokens[i], userToken, newToken);

        return newToken;
    }
}