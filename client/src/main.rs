mod crypto;
mod api;

use io::stdin;
use std::{fs, io};
use std::time::Instant;
use anyhow::Result;
use reqwest::{Error as ReqwestError, Response};
use serde_json::{Value};
use serde_json::json;
use reqwest::Error;
use std::fs::File;
use std::io::prelude::*;
use std::collections::HashMap;
use crate::api::{get_secrets, search_signature_from_api};
use crate::api::get_keys_from_api;
use crate::api::get_keys;
use crate::api::search_key_from_api;
use crate::crypto::decode_base64_private_key;
use crate::crypto::decode_base64_public_key;
use crate::crypto::encrypt;
use crate::crypto::decrypt;
use crate::crypto::sign;


#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    crud_test().await?;
    test_crypto();
    //benchmark().await.expect("error when running benchmark");
    Ok(())
}

fn test_crypto(){
    let (encrypted_message, ephemeral_public, nonce) = encrypt(b"Hello World", decode_base64_public_key("afEWKMdxXarhkRbCUB37deol7TyTi4OeffNEDV/P6CY=")).expect("Encryption failed");
    match decrypt(&encrypted_message, &ephemeral_public, decode_base64_private_key("6BCIEufBjTrfeprQi3a3jA3khSPm6NzeAidXWlVYYkA="), &nonce) {
        Ok(decrypted_message) => {
            println!("Encrypted message: {:?}", encrypted_message);
            println!("Decrypted message: {:?}", String::from_utf8(decrypted_message).expect("Invalid UTF-8"));
        }
        Err(_) => {
            println!("Failed to decrypt the message");
        }
    }
}


async fn benchmark() -> Result<(), Box<dyn std::error::Error>> {//NOT UP TO DATE
    let start = Instant::now();

    let mut tokens = Vec::new();
    for _ in 0..100 {
        let token = api::get_token(true).await?;
        println!("{}", token);
        tokens.push(token);
    }
    let mut file = File::create("tokens.txt")?;
    file.write_all(tokens.join("\n").as_bytes())?;

    let mut data_map = HashMap::new();
    for token in &tokens {
        let data = json!({
            "key1": "secret1",
            "key2": "secret2",
            "key3": "secret3",
            "key4": "secret4"
        });
        api::add_secrets(&token, data).await?;
        let retrieved_data = api::get_secrets(&token).await?;
        data_map.insert(token, retrieved_data);
    }

    let duration = start.elapsed();
    println!("Time elapsed is: {:?}", duration);

    Ok(())
}

async fn crud_test() -> Result<(), Box<dyn std::error::Error>> {
    let start = Instant::now();
    println!("Getting a key from Vault:");
    let mut token = api::get_token(false).await?;
    let keys = json!([
    {
        "Id": "key1",
        "Type": "RSA",
        "PublicKey": "---BEGIN PUB KEY---...",
        "PrivateKey": "---BEGIN RSA PRIVATE KEY---...",
        "Curve": "null",
        "Length": 2048
    },
    {
        "Id": "key2",
        "Type": "ecc",
        "PublicKey": "...",
        "PrivateKey": "...",
        "Curve": "Curve25519",
    }
]);
    let signatures = json!([
    {
        "id": "signature1",
        "hashingAlg" : "SHA384",
        "signature": "SignatureText1",
     },
     {
        "id": "signature2",
        "hashingAlg" : "SHA384",
        "signature": "SignatureText2",
    }
]);

    let data = json!({
    "Keys": keys,
    "Signatures": signatures
});
    println!("Storing secrets:");
    api::add_secrets(&token, data).await?;
    token = api::get_usertoken_from_file().unwrap();
    println!("Retrieving secrets:");
    let mut secret = api::get_secrets(&token).await?;
    println!("{}", secret);

    token = api::get_usertoken_from_file().unwrap();

    println!("");
    println!("-----------------------------------------");
    let key_id = "key2"; // enter Key to be searched
    println!("Searching Key: {}", key_id);
    let response = get_secrets(&token).await?;
    match search_key_from_api(&response, &key_id).await? {
        Some((public_key, private_key, key_type, length, curve)) => {
            println!("Public Key for key '{}': {}", key_id, public_key);
            println!("Private Key for key '{}': {}", key_id, private_key);
            println!("Type for key '{}': {}", key_id, key_type);
            println!("Length for key '{}': {}", key_id, length);
            match curve {
                Some(curve) => println!("Curve for key '{}': {}", key_id, curve),
                None => println!("Curve for key '{}': None", key_id),
            }
        }
        None => println!("No keys found for ID '{}'", key_id),
    }
    println!("-----------------------------------------");
    println!("");

    //token = api::get_usertoken_from_file().unwrap();

    // println!("");
    // println!("-----------------------------------------");
    // let signature_id = "signature1";
    // println!("Searching signature: {}", signature_id);
    // let response = get_secrets(&token).await?;
    //println!("Searching signature: {}", response);
    // token = api::get_usertoken_from_file().unwrap();
    // match search_signature_from_api(&token, &signature_id).await? {
    //    Some((hashing_alg, signature_text)) => {
    //       println!("Hashing Algorithm for signature '{}': {}", signature_id, hashing_alg);
    //       println!("Signature for signature '{}': {}", signature_id, signature_text);
    //    }
    //    None => println!("No signatures found for ID '{}'", signature_id),
    //}
    //println!("-----------------------------------------");
    //println!("");

    token = api::get_usertoken_from_file().unwrap();
    println!("Deleting secrets:");
    api::delete_secrets(&token).await?;
    token = api::get_usertoken_from_file().unwrap();
    println!("Retrieving secrets again:");
    secret = api::get_secrets(&token).await?;
    println!("get Secrets 1 {}", secret);

    token = api::get_usertoken_from_file().unwrap();
    let keys;
    println!("GetKeyPair:");
    match api::get_and_save_key_pair(&token, "testKey", "ecc").await {
        Ok(response) => {
            println!("Response:\n{}", response);
            keys = response;
        },
        Err(e) => eprintln!("Error: {}", e),
    }


    let duration = start.elapsed();
    println!("Time elapsed is: {:?}", duration);

    Ok(())
}
