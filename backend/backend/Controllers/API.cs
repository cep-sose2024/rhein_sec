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
        bool tokenExists = await _vaultCon.TokenExists(_vaultCon._tokens[0], oldToken, _vaultCon._addresses[0]);
        var ret = 0;
        if (tokenExists)
        {
            newToken = await rotateToken(oldToken);
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
                ret = await _vaultCon.CreateSecret(newToken, jsonData, _vaultCon._addresses[i]);
        }
        
        var returnObject = new
        {
            tokenExists = tokenExists,
            returnCode = ret,
            newToken = newToken
        };
        
        if (ret > 199 && ret < 300)
            return Ok(returnObject);
        else
            return BadRequest(returnObject);
    }
    
    [HttpPost("getSecrets/")]
    public async Task<IActionResult> getSecrets([FromBody] TokenModel tokenModel)
    {
        var oldToken = tokenModel.Token;
        var newToken = "";
        bool tokenExists = await _vaultCon.TokenExists(_vaultCon._tokens[0], oldToken, _vaultCon._addresses[0]);
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
                else if (ret is JObject && secret is JObject && JObject.DeepEquals((JObject)ret, (JObject)secret))
                {
                }
                else if (!ret.Equals(secret))
                {
                    return StatusCode(500, "Internal server Error");
                }
            }
        }
        if ((ret is JObject obj && !obj.HasValues) || ret is null)
        {
            ret = new {};
        }

        var retJObject = JObject.Parse(ret.ToString());
        var returnObject = new
        {
            tokenExists = tokenExists,
            data = retJObject.GetValue("data"),
            newToken = newToken
        };
        return Ok(returnObject);
    }
    
    private async Task<string> rotateToken(string userToken)
    {
        var newToken = VaultCon.GenerateToken(80);
        for (var i = 0; i < _vaultCon._addresses.Count; i++)
            newToken = await _vaultCon.RotateUserToken(_vaultCon._defpolicyname, _vaultCon._addresses[i], _vaultCon._tokens[i], userToken, newToken);

        return newToken;
    }
    
    [HttpDelete("deleteSecrets/")]
    public async Task<IActionResult> deleteSecrets([FromBody] TokenModel tokenModel)
    {
        var oldToken = tokenModel.Token;
        var newToken = "";
        bool tokenExists = await _vaultCon.TokenExists(_vaultCon._tokens[0], oldToken, _vaultCon._addresses[0]);
        var ret = 0;
        if (tokenExists)
        {
            newToken = await rotateToken(oldToken);
            for (var i = 0; i < _vaultCon._addresses.Count; i++)
                ret = await _vaultCon.DeleteSecrets(newToken, _vaultCon._addresses[i]);
        }
        var returnObject = new
        {
            tokenExists = tokenExists,
            returnCode = ret,
            newToken = newToken
        };
        return Ok(returnObject);
    }

    public class TokenModel
    {
        public string Token { get; set; }
    }

    public class SecretModel
    {
        public string Token { get; set; }
        public JObject Data { get; set; }
    }
}