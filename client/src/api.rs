use std::fs;
use std::fs::File;
use std::io::Read;
use reqwest::Error;
use serde_json::{json, Value};

pub(crate) async fn get_token(benchmark: bool) -> anyhow::Result<String, Box<dyn std::error::Error>> {
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


pub(crate) async fn get_secrets(token: &str) -> anyhow::Result<String, Box<dyn std::error::Error>> {
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

    let response_text = response.to_string();

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


pub(crate) async fn add_secrets(token: &str, data: Value) -> anyhow::Result<String, Box<dyn std::error::Error>> {
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

pub(crate) async fn delete_secrets(token: &str) -> anyhow::Result<(), Error> {
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

pub(crate) fn get_usertoken_from_file() -> Option<String> {
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


pub(crate) async fn get_and_save_key_pair(token: &str, key_name: &str, key_type: &str) -> std::result::Result<String, Box<dyn std::error::Error>> {
    let client = reqwest::Client::new();
    let request_body = json!(
        {
        "token": token,
        "name": key_name,
        "type": key_type
        }
    );
    println!("body: {}",request_body);

    let response = client
        .post("http://localhost:5272/apidemo/generateAndSaveKeyPair")
        .header("accept", "*/*")
        .header("Content-Type", "application/json-patch+json")
        .json(&request_body)
        .send()
        .await?;

    let status = response.status(); // Clone the status here
    let response_text = response.text().await?;
    if !status.is_success() {
        println!("Error response:\n{}", response_text);
        return Err(format!("Server returned status code: {}", status).into());
    }

    println!("Success response:\n{}", response_text);
    let response_json: Value = serde_json::from_str(&response_text)?;

    if let Some(user_token) = response_json.get("newToken") {
        if let Some(user_token_str) = user_token.as_str() {
            let token_data = json!({
                "usertoken": user_token_str
            });
            fs::write("token.json", token_data.to_string())?;
        }
    }
    let pretty_response = serde_json::to_string_pretty(&response_json)
        .unwrap_or_else(|_| String::from("Error formatting JSON"));

    Ok(pretty_response)
}