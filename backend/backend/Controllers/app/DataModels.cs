using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace backend.Controllers.example;

/// <summary>
/// Represents a model for a token.
/// </summary>
public class TokenModel
{
    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    public string Token { get; set; }
}

/// <summary>
/// Represents a model for a key pair.
/// </summary>
public class KeyPairModel
{
    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    public string Token { get; set; }
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Gets or sets the length.
    /// </summary>
    public int? Length { get; set; }
}

/// <summary>
/// Represents a model for a secret.
/// </summary>
public class SecretModel
{
    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    public string Token { get; set; }
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public DataModel Data { get; set; }
}

/// <summary>
/// Represents a model for data.
/// </summary>
public class DataModel
{
    /// <summary>
    /// Gets or sets the keys.
    /// </summary>
    [JsonProperty("keys")] public List<KeyModel> Keys { get; set; }

    /// <summary>
    /// Gets or sets the signatures.
    /// </summary>
    [JsonProperty("signatures")] public List<SignatureModel> Signatures { get; set; }

    /// <summary>
    /// Converts the data model to a JObject.
    /// </summary>
    /// <returns>A JObject that represents the data model.</returns>
    public JObject ToJObject()
    {
        var data = new JObject();
        data["keys"] = JArray.FromObject(Keys);
        data["signatures"] = JArray.FromObject(Signatures);
        return data;
    }
}

/// <summary>
/// Represents a model for a key.
/// </summary>
public class KeyModel
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    [JsonProperty("id")] public string Id { get; set; }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    [JsonProperty("type")] public string Type { get; set; }

    /// <summary>
    /// Gets or sets the public key.
    /// </summary>
    [JsonProperty("publickey")] public string PublicKey { get; set; }

    /// <summary>
    /// Gets or sets the private key.
    /// </summary>
    [JsonProperty("privatekey")] public string PrivateKey { get; set; }

    /// <summary>
    /// Gets or sets the curve.
    /// </summary>
    [JsonProperty("curve")] public string Curve { get; set; }

    /// <summary>
    /// Gets or sets the length.
    /// </summary>
    [JsonProperty("length")] public int Length { get; set; }
}

// <summary>
/// Represents a model for a signature.
/// </summary>
public class SignatureModel
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Gets or sets the key id.
    /// </summary>
    public string KeyId { get; set; }
    
    /// <summary>
    /// Gets or sets the hashing algorithm.
    /// </summary>
    public string HashingAlg { get; set; }
    
    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    public string Signature { get; set; }
}