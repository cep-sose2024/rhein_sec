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
        var token = await _vaultCon.CreateUserToken("user");
        return Ok(token);
    }

    [HttpPost("addSecrets/")]
    public async Task<IActionResult> addSecrets([FromBody] SecretModel secretModel)
    {
        var token = secretModel.Token;
        var jsonData = secretModel.Data; 
        var ret = await _vaultCon.CreateSecret(token, jsonData);
        Console.WriteLine(ret);
        if (ret > 199 && ret < 300)
            return Ok(ret);
        else
            return BadRequest($"Internal server returned code {ret}");
    }

    [HttpPost("getSecrets/")]
    public async Task<IActionResult> getSecrets([FromBody] TokenModel tokenModel)
    {
        var token = tokenModel.Token;

        var ret = await _vaultCon.GetSecrets(token);
        Console.WriteLine("getsecrets returned: " + ret);

        return Ok(ret);
    }
    
    [HttpDelete("deleteSecrets/")]
    public async Task<IActionResult> deleteSecrets([FromBody] TokenModel tokenModel)
    {
        var token = tokenModel.Token;

        var ret = await _vaultCon.DeleteSecrets(token);
        Console.WriteLine("getsecrets returned: " + ret);

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