use std::fs;
use std::time::Instant;
use anyhow::Result;
use reqwest::Error as ReqwestError;
use serde_json::{ Value};
use serde_json::json;
use reqwest::Error;

use std::fs::File;
use std::io::prelude::*;
use std::collections::HashMap;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    crud_test().await?;
    //benchmark().await.expect("error when running benchmark");
    Ok(())
}

async fn get_token(benchmark: bool) -> Result<String, Box<dyn std::error::Error>> {
    let response: Value = reqwest::Client::new()
        .get("http://localhost:5272/apidemo/getToken")
        .header("accept", "*/*")
        .send()
        .await?
        .json()
        .await?;

    if let Some(user_token) = response.get("token") {
        if let Some(user_token_str) = user_token.as_str() {
            println!("{}", user_token_str);
            if !benchmark {
                let token_data = json!({
                    "usertoken": user_token_str
                });
                fs::write("token.json", token_data.to_string())?;
            }
            return Ok(user_token_str.to_string());
        }
    }
    println!("The response does not contain a 'token' field");
    Ok(String::new())
}


async fn get_secrets(token: &str) -> Result<String, Box<dyn std::error::Error>> {
    let client = reqwest::Client::new();
    let body = json!({
        "token": token
    });

    let response: Value = client.post("http://localhost:5272/apidemo/getSecrets")
        .header("accept", "*/*")
        .header("Content-Type", "application/json-patch+json")
        .json(&body)
        .send()
        .await?
        .json()
        .await?;

    //let response_json = response.json().await?;

    let response_text= response.to_string();

    //save new token
    if let Some(user_token) = response.get("newToken") {
        if let Some(user_token_str) = user_token.as_str() {
            let token_data = json!({
                "usertoken": user_token_str
            });
            fs::write("token.json", token_data.to_string())?;
        }
    }

    if response_text.is_empty() {
        println!("Received empty response from server");
        Ok(String::new())
    } else {
        let response: Value = serde_json::from_str(&response_text)?;
        let pretty_response = serde_json::to_string_pretty(&response).unwrap_or_else(|_| String::from("Error formatting JSON"));
        Ok(pretty_response)
    }
}


async fn add_secrets(token: &str, data: Value) -> Result<String, Box<dyn std::error::Error>> {
    let client = reqwest::Client::new();
    let body = json!({
        "token": token,
        "data": data
    });

    let response: Value = client.post("http://localhost:5272/apidemo/addSecrets")
        .header("accept", "*/*")
        .header("Content-Type", "application/json-patch+json")
        .json(&body)
        .send()
        .await?
        .json()
        .await?;

    //save new token
    if let Some(user_token) = response.get("newToken") {
        if let Some(user_token_str) = user_token.as_str() {
            let token_data = json!({
                "usertoken": user_token_str
            });
            fs::write("token.json", token_data.to_string())?;
        }
    }

    let pretty_response = serde_json::to_string_pretty(&response).unwrap_or_else(|_| String::from("Error formatting JSON"));
    println!("{}", pretty_response);

    Ok((pretty_response))
}
async fn delete_secrets(token: &str) -> Result<(), Error> {
    let client = reqwest::Client::new();
    let body = json!({
        "token": token
    });

    let response: Value = client.delete("http://localhost:5272/apidemo/deleteSecrets")
        .header("accept", "*/*")
        .header("Content-Type", "application/json-patch+json")
        .json(&body)
        .send()
        .await?
        .json()
        .await?;

    //save new token
    if let Some(user_token) = response.get("newToken") {
        if let Some(user_token_str) = user_token.as_str() {
            let token_data = json!({
                "usertoken": user_token_str
            });
            fs::write("token.json", token_data.to_string());
        }
    }

    let pretty_response = serde_json::to_string_pretty(&response).unwrap_or_else(|_| String::from("Error formatting JSON"));
    println!("{}", pretty_response);

    Ok(())
}

fn get_usertoken_from_file() -> Option<String> {
    let mut file = File::open("token.json").ok()?;
    let mut contents = String::new();
    file.read_to_string(&mut contents).ok()?;

    let json: Value = serde_json::from_str(&contents).ok()?;

    if let Some(usertoken) = json["usertoken"].as_str() {
        return Some(usertoken.to_string());
    } else {
        println!("usertoken not found or invalid format.");
        return None;
    }
}

async fn benchmark() -> Result<(), Box<dyn std::error::Error>> {
    let start = Instant::now();

    let mut tokens = Vec::new();
    for _ in 0..100 {
        let token = get_token(true).await?;
        println!("{}",token);
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
        add_secrets(&token, data).await?;
        let retrieved_data = get_secrets(&token).await?;
        data_map.insert(token, retrieved_data);
    }

    let duration = start.elapsed();
    println!("Time elapsed is: {:?}", duration);

    Ok(())
}
async fn crud_test() -> Result<(), Box<dyn std::error::Error>> {
    let start = Instant::now();
    println!("Getting a key from Vault:");
    let mut token = get_token(false).await?;
    let data = json!({
        "key1": "secret1",
        "key2": "secret2",
        "key3": "secret3",
        "key4": "secret4"
    });
    println!("Storing secrets:");
    add_secrets(&token, data).await?;
    token = get_usertoken_from_file().unwrap();
    println!("Retrieving secrets:");
    let mut secret = get_secrets(&token).await?;
    println!("{}", secret);
    token = get_usertoken_from_file().unwrap();
    println!("Deleting secrets:");
    delete_secrets(&token).await?;
    token = get_usertoken_from_file().unwrap();
    println!("Retrieving secrets again:");
    secret = get_secrets(&token).await?;
    println!("{}",secret);
    let duration = start.elapsed();
    println!("Time elapsed is: {:?}", duration);

    Ok(())
}

