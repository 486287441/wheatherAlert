#[derive(Debug, thiserror::Error)]
pub enum AppError {
    #[error("请先在设置中填写和风天气 API Key")]
    MissingApiKey,
    #[error("请先在设置中填写 DeepSeek API Key")]
    MissingAiApiKey,
    #[error("API Host 配置无效：{0}")]
    InvalidHost(String),
    #[error("天气服务请求失败：{0}")]
    Network(String),
    #[error("天气服务返回异常：{0}")]
    WeatherApi(String),
    #[error("AI 服务请求失败：{0}")]
    AiApi(String),
    #[error("本地数据访问失败：{0}")]
    Database(#[from] rusqlite::Error),
    #[error("本地配置访问失败：{0}")]
    Io(#[from] std::io::Error),
    #[error("配置格式错误：{0}")]
    Json(#[from] serde_json::Error),
    #[error("系统集成失败：{0}")]
    System(String),
}

impl serde::Serialize for AppError {
    fn serialize<S>(&self, serializer: S) -> std::result::Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}

pub type Result<T> = std::result::Result<T, AppError>;
