use arrayref::array_ref;
use x25519_dalek::{PublicKey as X25519PublicKey, PublicKey, StaticSecret as X25519StaticSecret, StaticSecret};
use chacha20poly1305::{
    ChaCha20Poly1305,
    Key,
    Nonce,
    aead::{Aead, KeyInit},
};
use rand::rngs::OsRng;
use base64::prelude::*;
use ed25519_dalek::{Verifier, SigningKey, VerifyingKey, Signature, Signer};

fn main() {

    let x25519_keys = decode_base64("afEWKMdxXarhkRbCUB37deol7TyTi4OeffNEDV/P6CY=", "6BCIEufBjTrfeprQi3a3jA3khSPm6NzeAidXWlVYYkA=");
    let message = b"hello world.";
    }

fn decode_base64(_public_key_base64: &str, _private_key_base64: &str ) -> (PublicKey, StaticSecret) {
    let public_key_base64 = _public_key_base64;// example public key
    let private_key_base64 = _private_key_base64; // example private key

    let public_key_bytes = BASE64_STANDARD.decode(public_key_base64.as_bytes()).expect("Invalid public key base64");
    let private_key_bytes = BASE64_STANDARD.decode(private_key_base64.as_bytes()).expect("Invalid private key base64");

    let x25519_public_key = X25519PublicKey::from(*array_ref![public_key_bytes, 0, 32]);
    let x25519_private_key = X25519StaticSecret::from(*array_ref![private_key_bytes, 0, 32]);

    return(x25519_public_key, x25519_private_key);
}


fn sign(x25519_private_key: StaticSecret, _message: &[u8]) -> Signature {
    let ed25519_secret_key = SigningKey::from_bytes(&x25519_private_key.to_bytes());
    let ed25519_public_key = VerifyingKey::from(&ed25519_secret_key);

    let signature: Signature = ed25519_secret_key.sign(_message);

    assert!(ed25519_public_key.verify(_message, &signature).is_ok());
    return signature;
}

fn encrypt(message: &[u8], public_key: &PublicKey) -> Result<(Vec<u8>, PublicKey, Nonce), ()> {
    let mut rng = OsRng;
    let ephemeral_secret = StaticSecret::random_from_rng(&mut rng);
    let ephemeral_public = PublicKey::from(&ephemeral_secret);

    let shared_secret = ephemeral_secret.diffie_hellman(public_key);

    let symmetric_key = Key::from_slice(shared_secret.as_bytes());
    let cipher = ChaCha20Poly1305::new(&symmetric_key);

    let random_bytes = rand::random::<[u8; 12]>();
    let nonce = Nonce::from_slice(&random_bytes);
    println!("{:?}",nonce);
    cipher.encrypt(&nonce, message)
        .map(|encrypted_message| (encrypted_message, ephemeral_public, *nonce))
        .map_err(|_| ())
}

fn decrypt(encrypted_message: &[u8], ephemeral_public: &X25519PublicKey, private_key: &X25519StaticSecret, nonce: &Nonce) -> Result<Vec<u8>, ()> {
    let shared_secret = private_key.diffie_hellman(ephemeral_public);

    let symmetric_key = Key::from_slice(shared_secret.as_bytes());
    let cipher = ChaCha20Poly1305::new(&symmetric_key);
    println!("{:?}",nonce);

    cipher.decrypt(nonce, encrypted_message).map_err(|_| ())
}
