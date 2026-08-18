use crate::{
    error::{AppError, Result},
    models::{AppSettings, DashboardData},
};
use serde::Deserialize;
use serde_json::json;
use std::time::Duration;

#[derive(Deserialize)]
struct ChatResponse {
    choices: Vec<ChatChoice>,
}

#[derive(Deserialize)]
struct ChatChoice {
    message: ChatMessage,
}

#[derive(Deserialize)]
struct ChatMessage {
    content: String,
}

pub async fn ask(
    settings: &AppSettings,
    dashboard: &DashboardData,
    question: &str,
) -> Result<String> {
    if settings.ai_api_key.trim().is_empty() {
        return Err(AppError::MissingAiApiKey);
    }
    if question.trim().is_empty() {
        return Err(AppError::AiApi("问题不能为空".into()));
    }

    let weather_context = json!({
        "city": dashboard.city_name,
        "updatedAt": dashboard.updated_at,
        "isCached": dashboard.is_cached,
        "hourly": dashboard.hourly,
        "daily": dashboard.daily,
    });
    let request = json!({
        "model": "deepseek-v4-flash",
        "messages": [
            {
                "role": "system",
                "content": "你是 WeatherAlert 的天气顾问。只根据用户提供的当前天气数据回答，使用简洁自然的中文。涉及出游日期时给出明确推荐、理由和备选日期，并提醒天气预报越远不确定性越高。不要编造天气数据；数据不足时直接说明。"
            },
            {
                "role": "user",
                "content": format!("天气数据：\n{}\n\n用户问题：{}", weather_context, question.trim())
            }
        ],
        "stream": false,
        "thinking": { "type": "disabled" },
        "temperature": 0.3,
        "max_tokens": 900
    });
    let client = reqwest::Client::builder()
        .timeout(Duration::from_secs(90))
        .build()
        .map_err(|e| AppError::AiApi(e.to_string()))?;
    let response = client
        .post("https://api.deepseek.com/v1/chat/completions")
        .bearer_auth(settings.ai_api_key.trim())
        .json(&request)
        .send()
        .await
        .map_err(|e| AppError::AiApi(e.to_string()))?;
    let status = response.status();
    if !status.is_success() {
        let detail = response.text().await.unwrap_or_default();
        return Err(AppError::AiApi(format!("HTTP {status}：{detail}")));
    }
    let payload: ChatResponse = response
        .json()
        .await
        .map_err(|e| AppError::AiApi(e.to_string()))?;
    payload
        .choices
        .into_iter()
        .next()
        .map(|choice| choice.message.content.trim().to_string())
        .filter(|content| !content.is_empty())
        .ok_or_else(|| AppError::AiApi("DeepSeek 未返回回答".into()))
}
