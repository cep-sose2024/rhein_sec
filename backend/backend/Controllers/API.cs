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

        var token = secretModel.Token;
        var jsonData = secretModel.Data;
        var ret = 0;
        for (var i = 0; i < _vaultCon._addresses.Count; i++)
            ret = await _vaultCon.CreateSecret(token, jsonData, _vaultCon._addresses[i]);
        if (ret > 199 && ret < 300)
            return Ok(ret);
        else
            return BadRequest($"Internal server returned code {ret}");
    }
    [HttpPost("getSecrets/")]
    public async Task<IActionResult> getSecrets([FromBody] TokenModel tokenModel)
    {
        var token = tokenModel.Token;
        object ret = null;

        for (var i = 0; i < _vaultCon._addresses.Count; i++)
        {
            var secret = await _vaultCon.GetSecrets(token, _vaultCon._addresses[i]);
            if (ret == null)
            {
                ret = secret;
            }
            else if (ret is JObject && secret is JObject && JObject.DeepEquals((JObject)ret, (JObject)secret))
            {
                continue;
            }
            else if (!ret.Equals(secret))
            {
                return StatusCode(500, "Internal server Error");
            }
        }

        if (ret is JObject obj && !obj.HasValues)
        {
            return Ok(new {});
        }

        return Ok(ret);
    }



    [HttpDelete("deleteSecrets/")]
    public async Task<IActionResult> deleteSecrets([FromBody] TokenModel tokenModel)
    {
        var token = tokenModel.Token;
        var ret = 0;
        for (var i = 0; i < _vaultCon._addresses.Count; i++)
            ret = await _vaultCon.DeleteSecrets(token, _vaultCon._addresses[i]);

        return Ok(ret);
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