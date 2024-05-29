using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

/// <summary>
/// Provides methods for generating and managing cryptographic keys.
/// </summary>
/// <remarks>
/// This class includes methods for generating X25519, Ed25519, and RSA key pairs, 
/// as well as methods for converting these keys to JSON objects and PEM formatted strings.
/// </remarks>
public class Crypto
{
    /// <summary>
    /// Generates a X25519 key pair.
    /// </summary>
    /// <param name="keySize">The size of the key in bits. Default is 256.</param>
    /// <returns>A tuple where the first item is the private key and the second item is the public key. Both keys are Base64 encoded strings.</returns>
    private static (string, string) GenerateX25519KeyPair(int keySize = 256)
    {
        var generator2 = new Ed25519KeyPairGenerator();
        var generator = new X25519KeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
        var keyPair = generator.GenerateKeyPair();

        var privateKey = Convert.ToBase64String(((X25519PrivateKeyParameters)keyPair.Private).GetEncoded());
        var publicKey = Convert.ToBase64String(((X25519PublicKeyParameters)keyPair.Public).GetEncoded());

        return (privateKey, publicKey);
    }

    /// <summary>
    /// Generates an Ed25519 key pair.
    /// </summary>
    /// <param name="keySize">The size of the key in bits. Default is 256.</param>
    /// <returns>A tuple where the first item is the private key and the second item is the public key. Both keys are Base64 encoded strings.</returns>
    private static (string, string) GenerateEd25519KeyPair(int keySize = 256)
    {
        var generator2 = new Ed25519KeyPairGenerator();
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
        var keyPair = generator.GenerateKeyPair();

        var privateKey = Convert.ToBase64String(((Ed25519PrivateKeyParameters)keyPair.Private).GetEncoded());
        var publicKey = Convert.ToBase64String(((Ed25519PublicKeyParameters)keyPair.Public).GetEncoded());

        return (privateKey, publicKey);
    }

    /// <summary>
    /// Generates an RSA key pair.
    /// </summary>
    /// <param name="keySize">The size of the key in bits. Default is 2048.</param>
    /// <returns>A tuple where the first item is the private key and the second item is the public key. Both keys are PEM encoded strings.</returns>
    private static (string, string) GenerateRsaKeyPair(int keySize = 2048)
    {
        var generator = new RsaKeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
        var keyPair = generator.GenerateKeyPair();

        var privateKey = KeyToPem(keyPair.Private);
        var publicKey = KeyToPem(keyPair.Public);

        return (privateKey, publicKey);
    }

    /// <summary>
    /// Converts an asymmetric key to a PEM formatted string.
    /// </summary>
    /// <param name="key">The asymmetric key to convert.</param>
    /// <returns>A PEM formatted string representation of the key.</returns>
    private static string KeyToPem(AsymmetricKeyParameter key)
    {
        var stringWriter = new StringWriter();
        var pemWriter = new PemWriter(stringWriter);
        pemWriter.WriteObject(key);
        return stringWriter.ToString();
    }

    /// <summary>
    /// Generates a X25519 key pair and returns it in a JSON object.
    /// </summary>
    /// <param name="name">The name to be associated with the key pair.</param>
    /// <param name="keySize">The size of the key in bits. Default is 256.</param>
    /// <returns>A JSON object containing the key pair and associated information.</returns>
    public static JObject GetxX25519KeyPair(string name, int keySize = 256)
    {
        var (privateKey, publicKey) = GenerateX25519KeyPair(keySize);
        return MakeKeyJson(name, "ecdh", "Curve25519", publicKey, privateKey, keySize);
    }

    /// <summary>
    /// Generates an Ed25519 key pair and returns it in a JSON object.
    /// </summary>
    /// <param name="name">The name to be associated with the key pair.</param>
    /// <param name="keySize">The size of the key in bits. Default is 256.</param>
    /// <returns>A JSON object containing the key pair and associated information.</returns>
    public static JObject GetEd25519KeyPair(string name, int keySize = 256)
    {
        var (privateKey, publicKey) = GenerateEd25519KeyPair(keySize);
        return MakeKeyJson(name, "ecdsa", "Curve25519", publicKey, privateKey, keySize);
    }

    /// <summary>
    /// Generates an RSA key pair and returns it in a JSON object.
    /// </summary>
    /// <param name="name">The name to be associated with the key pair.</param>
    /// <param name="keySize">The size of the key in bits. Default is 2048.</param>
    /// <returns>A JSON object containing the key pair and associated information.</returns>
    public static JObject GetRsaKeyPair(string name, int keySize = 2048)
    {
        var (privateKey, publicKey) = GenerateRsaKeyPair(keySize);
        return MakeKeyJson(name, "RSA", null, publicKey, privateKey, keySize);
    }

    /// <summary>
    /// Creates a JSON object representing a key pair.
    /// </summary>
    /// <param name="name">The name to be associated with the key pair.</param>
    /// <param name="alg">The algorithm used to generate the key pair.</param>
    /// <param name="curve">The curve used for the key pair. This is applicable for ECDH and ECDSA algorithms.</param>
    /// <param name="publicKey">The public key of the key pair.</param>
    /// <param name="privateKey">The private key of the key pair.</param>
    /// <param name="length">The size of the key in bits.</param>
    /// <returns>A JSON object containing the key pair and associated information.</returns>
    private static JObject MakeKeyJson(string name, string alg, string curve, string publicKey, string privateKey,
        int length)
    {
        var keyJson = new JObject();

        keyJson["id"] = name;
        keyJson["type"] = alg;
        keyJson["publicKey"] = publicKey;
        keyJson["privateKey"] = privateKey;
        keyJson["length"] = length;
        keyJson["curve"] = null;

        if (alg.ToLower() == "ecdh" || alg.ToLower() == "ecdsa") keyJson["curve"] = curve;

        return keyJson;
    }
}