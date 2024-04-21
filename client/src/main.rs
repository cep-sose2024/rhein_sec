use std::fs;
use std::time::Instant;
use anyhow::Result;
use reqwest::Error as ReqwestError;
use serde_json::{ Value};
use serde_json::json;
use reqwest::Error;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let start = Instant::now();

    let token = get_token().await?;
    let data = json!({
        "key1": "secret1",
        "key2": "secret2",
        "key3": "secret3",
        "key4": "secret4"
    });
    add_secrets(&token, data).await?;
    get_secrets(&token).await?;
    delete_secrets(&token).await?;
    get_secrets(&token).await?;

    let duration = start.elapsed();
    println!("Time elapsed in expensive_function() is: {:?}", duration);

    Ok(())
}

async fn get_token() -> Result<String, Box<dyn std::error::Error>> {
    match fs::read_to_string("token.json") {
        Ok(contents) => {
            let token_data: Value = serde_json::from_str(&contents)?;
            if let Some(user_token) = token_data.get("usertoken") {
                if let Some(user_token_str) = user_token.as_str() {
                    println!("{}", user_token_str);
                    return Ok(user_token_str.to_string());
                }
            }
            println!("The file does not contain a 'usertoken' field");
            Ok(String::new())
        }
        Err(_) => {
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
                    let token_data = json!({
                        "usertoken": user_token_str
                    });
                    fs::write("token.json", token_data.to_string())?;
                    return Ok(user_token_str.to_string());
                }
            }
            println!("The response does not contain a 'token' field");
            Ok(String::new())
        }
    }
}

async fn get_secrets(token: &str) -> Result<()> {
    let client = reqwest::Client::new();
    let body = json!({
        "token": token
    });

    let response_text = client.post("http://localhost:5272/apidemo/getSecrets")
        .header("accept", "*/*")
        .header("Content-Type", "application/json-patch+json")
        .json(&body)
        .send()
        .await?
        .text()
        .await?;
    println!("{}", "TESTTTT");

    if response_text.is_empty() {
        println!("Received empty response from server");
        // Handle the empty response here, e.g., by returning an error or a default value
    } else {
        let response: Value = serde_json::from_str(&response_text)?;
        let pretty_response = serde_json::to_string_pretty(&response).unwrap_or_else(|_| String::from("Error formatting JSON"));
        println!("{}", pretty_response);
    }

    Ok(())
}

async fn add_secrets(token: &str, data: Value) -> Result<()> {
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

    let pretty_response = serde_json::to_string_pretty(&response).unwrap_or_else(|_| String::from("Error formatting JSON"));
    println!("{}", pretty_response);

    Ok(())
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

    println!("{:?}", response);

    Ok(())
}