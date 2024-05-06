mod crypto;
mod api;

use std::fs;
use std::time::Instant;
use anyhow::Result;
use reqwest::{Error as ReqwestError, Response};
use serde_json::{Value};
use serde_json::json;
use reqwest::Error;
use std::fs::File;
use std::io::prelude::*;
use std::collections::HashMap;
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
    let enc_data = encrypt(b"Hello World", decode_base64_public_key("afEWKMdxXarhkRbCUB37deol7TyTi4OeffNEDV/P6CY="));
    println!("{:?}", enc_data);
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

    let data = json!({
    "Keys": keys,
    "Signatures": []
});
    println!("Storing secrets:");
    api::add_secrets(&token, data).await?;
    token = api::get_usertoken_from_file().unwrap();
    println!("Retrieving secrets:");
    let mut secret = api::get_secrets(&token).await?;
    println!("{}", secret);
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
