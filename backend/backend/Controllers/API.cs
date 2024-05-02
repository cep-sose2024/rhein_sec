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
            ret = await putSecret(newToken, jsonData);
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

            var keyPair = new JObject();
            if (keyPairModel.Type.ToLower().Equals("ecc"))
            {
                keyPair = Crypto.GetxX25519KeyPair(keyPairModel.Name);
            }
            else if (keyPairModel.Type.ToLower().Equals("rsa"))
            {
                keyPair = Crypto.GetRsaKeyPair(keyPairModel.Name);
            }
            var retJObject = JObject.Parse(ret.ToString());
            //TODO: what if no data exists yet?
            retJObject["data"]["keys"].Last.AddAfterSelf(keyPair);
            var putRetCode = putSecret(newToken, retJObject);
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
        {
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
                ret = await _vaultCon.CreateSecret(token, data, _vaultCon._addresses[i]);
        }

        return ret;
    }
    

    private async Task<string> rotateToken(string userToken)
    {
        var newToken = VaultCon.GenerateToken(80);
        for (var i = 0; i < _vaultCon._addresses.Count; i++)
            newToken = await _vaultCon.RotateUserToken(_vaultCon._defpolicyname, _vaultCon._addresses[i],
                _vaultCon._tokens[i], userToken, newToken);

        return newToken;
    }

    public class TokenModel
    {
        public string Token { get; set; }
    }
    public class KeyPairModel
    {
        public string Token { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class SecretModel
    {
        public string Token { get; set; }
        public JObject Data { get; set; }
    }
}