using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace backend.Controllers.example;

public class TokenModel
{
    public string Token { get; set; }
}

public class KeyPairModel
{
    public string Token { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int? Length { get; set; }
}

public class SecretModel
{
    public string Token { get; set; }
    public DataModel Data { get; set; }
}

public class DataModel
{
    [JsonProperty("keys")] public List<KeyModel> Keys { get; set; }

    [JsonProperty("signatures")] public List<SignatureModel> Signatures { get; set; }

    public JObject ToJObject()
    {
        var data = new JObject();
        data["keys"] = JArray.FromObject(Keys);
        data["signatures"] = JArray.FromObject(Signatures);
        return data;
    }
}

public class KeyModel
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("publickey")] public string PublicKey { get; set; }

    [JsonProperty("privatekey")] public string PrivateKey { get; set; }

    [JsonProperty("curve")] public string Curve { get; set; }

    [JsonProperty("length")] public int Length { get; set; }
}

public class SignatureModel
{
    public string Id { get; set; }
    public string KeyId { get; set; }
    public string HashingAlg { get; set; }
    public string Signature { get; set; }
}