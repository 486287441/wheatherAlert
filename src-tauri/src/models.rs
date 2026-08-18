use chrono::{DateTime, Local, NaiveDate, Utc};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AppSettings {
    pub api_key: String,
    pub api_base_url: String,
    pub polling_minutes: u64,
    pub autostart: bool,
    pub notifications: bool,
    pub current_city_code: String,
    pub current_city_name: String,
    #[serde(default)]
    pub ai_api_key: String,
}

impl Default for AppSettings {
    fn default() -> Self {
        Self {
            api_key: String::new(),
            api_base_url: "https://your-api-host.qweatherapi.com".into(),
            polling_minutes: 60,
            autostart: false,
            notifications: true,
            current_city_code: "101010100".into(),
            current_city_name: "北京".into(),
            ai_api_key: String::new(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct HourlyForecast {
    pub forecast_time: DateTime<chrono::FixedOffset>,
    pub temperature: i32,
    pub precipitation_mm: f64,
    pub precipitation_probability: i32,
    pub condition_text: String,
    pub icon: String,
    pub wind_direction: String,
    pub wind_scale: String,
    pub humidity: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DailyForecast {
    pub forecast_date: NaiveDate,
    pub temperature_max: i32,
    pub temperature_min: i32,
    pub condition_day: String,
    pub condition_night: String,
    pub icon_day: String,
    pub icon_night: String,
    pub precipitation_mm: f64,
    pub humidity: i32,
    pub wind_direction_day: String,
    pub wind_scale_day: String,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct RainRange {
    pub start: DateTime<chrono::FixedOffset>,
    pub end: DateTime<chrono::FixedOffset>,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct DaySummary {
    pub date: NaiveDate,
    pub has_rain: bool,
    pub intensity: String,
    pub ranges: Vec<RainRange>,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct DashboardData {
    pub city_code: String,
    pub city_name: String,
    pub updated_at: Option<DateTime<Utc>>,
    pub is_cached: bool,
    pub today: DaySummary,
    pub tomorrow: DaySummary,
    pub hourly: Vec<HourlyForecast>,
    pub daily: Vec<DailyForecast>,
}

impl DashboardData {
    pub fn empty(settings: &AppSettings) -> Self {
        let today = Local::now().date_naive();
        Self {
            city_code: settings.current_city_code.clone(),
            city_name: settings.current_city_name.clone(),
            updated_at: None,
            is_cached: true,
            today: DaySummary {
                date: today,
                has_rain: false,
                intensity: "none".into(),
                ranges: vec![],
            },
            tomorrow: DaySummary {
                date: today.succ_opt().unwrap_or(today),
                has_rain: false,
                intensity: "none".into(),
                ranges: vec![],
            },
            hourly: vec![],
            daily: vec![],
        }
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct HistoryEntry {
    pub id: i64,
    pub created_at: String,
    #[serde(rename = "type")]
    pub kind: String,
    pub city_code: String,
    pub title: String,
    pub body: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct City {
    pub id: String,
    pub name: String,
    pub province: String,
    pub prefecture: String,
}

#[derive(Debug, Deserialize)]
pub struct WeatherResponse {
    pub code: String,
    #[serde(default)]
    pub hourly: Vec<HourlyDto>,
}

#[derive(Debug, Deserialize)]
pub struct DailyWeatherResponse {
    pub code: String,
    #[serde(default)]
    pub daily: Vec<DailyDto>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DailyDto {
    pub fx_date: String,
    #[serde(default)]
    pub temp_max: String,
    #[serde(default)]
    pub temp_min: String,
    #[serde(default)]
    pub text_day: String,
    #[serde(default)]
    pub text_night: String,
    #[serde(default)]
    pub icon_day: String,
    #[serde(default)]
    pub icon_night: String,
    #[serde(default)]
    pub precip: String,
    #[serde(default)]
    pub humidity: String,
    #[serde(default)]
    pub wind_dir_day: String,
    #[serde(default)]
    pub wind_scale_day: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct HourlyDto {
    pub fx_time: String,
    #[serde(default)]
    pub temp: String,
    #[serde(default)]
    pub precip: String,
    #[serde(default)]
    pub pop: String,
    #[serde(default)]
    pub text: String,
    #[serde(default)]
    pub icon: String,
    #[serde(default)]
    pub wind_dir: String,
    #[serde(default)]
    pub wind_scale: String,
    #[serde(default)]
    pub humidity: String,
}

#[derive(Debug, Deserialize)]
pub struct GeoResponse {
    pub code: String,
    #[serde(default, rename = "location")]
    pub locations: Vec<GeoDto>,
}

#[derive(Debug, Deserialize)]
pub struct GeoDto {
    pub id: String,
    pub name: String,
    #[serde(default)]
    pub adm1: String,
    #[serde(default)]
    pub adm2: String,
}
