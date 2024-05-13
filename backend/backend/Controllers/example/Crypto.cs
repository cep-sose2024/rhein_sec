using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

public class Crypto
{
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

    private static (string, string) GenerateRsaKeyPair(int keySize = 2048)
    {
        var generator = new RsaKeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
        var keyPair = generator.GenerateKeyPair();

        var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
        var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);

        var privateKey = Convert.ToBase64String(privateKeyInfo.GetDerEncoded());
        var publicKey = Convert.ToBase64String(publicKeyInfo.GetDerEncoded());

        return (privateKey, publicKey);
    }

    public static JObject GetxX25519KeyPair(string name, int keySize = 256)
    {
        var (privateKey, publicKey) = GenerateX25519KeyPair(keySize);
        return MakeKeyJson(name, "ecdh", "Curve25519", publicKey, privateKey, keySize.ToString());
    }
    
    public static JObject GetEd25519KeyPair(string name, int keySize = 256)
    {
        var (privateKey, publicKey) = GenerateEd25519KeyPair(keySize);
        return MakeKeyJson(name, "ecdsa", "Curve25519", publicKey, privateKey, keySize.ToString());
    }

    public static JObject GetRsaKeyPair(string name, int keySize = 2048)
    {
        var (privateKey, publicKey) = GenerateRsaKeyPair(keySize);
        return MakeKeyJson(name, "RSA", null, publicKey, privateKey, keySize.ToString());
    }

    private static JObject MakeKeyJson(string name, string alg, string curve, string publicKey, string privateKey,
        string length)
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