use crate::{
    error::{AppError, Result},
    models::*,
};
use chrono::{DateTime, Duration, Local, NaiveDate, Timelike};
use reqwest::Client;
use sha2::{Digest, Sha256};
use url::Url;

pub async fn fetch(client: &Client, settings: &AppSettings) -> Result<Vec<HourlyForecast>> {
    if settings.api_key.trim().is_empty() {
        return Err(AppError::MissingApiKey);
    }
    let mut url = Url::parse(&settings.api_base_url)
        .map_err(|_| AppError::InvalidHost(settings.api_base_url.clone()))?;
    url.set_path("/v7/weather/72h");
    url.query_pairs_mut()
        .append_pair("location", &settings.current_city_code)
        .append_pair("key", &settings.api_key)
        .append_pair("lang", "zh");
    let response = client
        .get(url)
        .send()
        .await
        .map_err(|e| AppError::Network(e.to_string()))?;
    if !response.status().is_success() {
        return Err(AppError::WeatherApi(format!("HTTP {}", response.status())));
    }
    let payload: WeatherResponse = response
        .json()
        .await
        .map_err(|e| AppError::WeatherApi(e.to_string()))?;
    if payload.code != "200" {
        return Err(AppError::WeatherApi(format!("业务代码 {}", payload.code)));
    }
    payload.hourly.into_iter().map(map_hour).collect()
}

pub async fn fetch_daily(client: &Client, settings: &AppSettings) -> Result<Vec<DailyForecast>> {
    if settings.api_key.trim().is_empty() {
        return Err(AppError::MissingApiKey);
    }
    let mut url = Url::parse(&settings.api_base_url)
        .map_err(|_| AppError::InvalidHost(settings.api_base_url.clone()))?;
    url.set_path("/v7/weather/30d");
    url.query_pairs_mut()
        .append_pair("location", &settings.current_city_code)
        .append_pair("key", &settings.api_key)
        .append_pair("lang", "zh");
    let response = client
        .get(url)
        .send()
        .await
        .map_err(|e| AppError::Network(e.to_string()))?;
    if !response.status().is_success() {
        return Err(AppError::WeatherApi(format!("HTTP {}", response.status())));
    }
    let payload: DailyWeatherResponse = response
        .json()
        .await
        .map_err(|e| AppError::WeatherApi(e.to_string()))?;
    if payload.code != "200" {
        return Err(AppError::WeatherApi(format!("业务代码 {}", payload.code)));
    }
    payload
        .daily
        .into_iter()
        .map(|x| {
            Ok(DailyForecast {
                forecast_date: NaiveDate::parse_from_str(&x.fx_date, "%Y-%m-%d").map_err(|e| {
                    AppError::WeatherApi(format!("无效的预报日期 {}：{e}", x.fx_date))
                })?,
                temperature_max: x.temp_max.parse().unwrap_or_default(),
                temperature_min: x.temp_min.parse().unwrap_or_default(),
                condition_day: x.text_day,
                condition_night: x.text_night,
                icon_day: x.icon_day,
                icon_night: x.icon_night,
                precipitation_mm: x.precip.parse().unwrap_or_default(),
                humidity: x.humidity.parse().unwrap_or_default(),
                wind_direction_day: x.wind_dir_day,
                wind_scale_day: x.wind_scale_day,
            })
        })
        .collect()
}

fn map_hour(x: HourlyDto) -> Result<HourlyForecast> {
    Ok(HourlyForecast {
        forecast_time: parse_forecast_time(&x.fx_time)?,
        temperature: x.temp.parse().unwrap_or_default(),
        precipitation_mm: x.precip.parse().unwrap_or_default(),
        precipitation_probability: x.pop.parse().unwrap_or_default(),
        condition_text: x.text,
        icon: x.icon,
        wind_direction: x.wind_dir,
        wind_scale: x.wind_scale,
        humidity: x.humidity.parse().unwrap_or_default(),
    })
}

fn parse_forecast_time(value: &str) -> Result<DateTime<chrono::FixedOffset>> {
    DateTime::parse_from_rfc3339(value)
        .or_else(|_| DateTime::parse_from_str(value, "%Y-%m-%dT%H:%M%:z"))
        .map_err(|e| AppError::WeatherApi(format!("无效的预报时间 {value}：{e}")))
}

pub async fn search(client: &Client, settings: &AppSettings, keyword: &str) -> Result<Vec<City>> {
    if keyword.trim().is_empty() {
        return Ok(vec![]);
    }
    if settings.api_key.trim().is_empty() {
        return Err(AppError::MissingApiKey);
    }
    let mut url = Url::parse(&settings.api_base_url)
        .map_err(|_| AppError::InvalidHost(settings.api_base_url.clone()))?;
    url.set_path("/geo/v2/city/lookup");
    url.query_pairs_mut()
        .append_pair("location", keyword.trim())
        .append_pair("key", &settings.api_key)
        .append_pair("range", "cn")
        .append_pair("number", "20")
        .append_pair("lang", "zh");
    let response = client
        .get(url)
        .send()
        .await
        .map_err(|e| AppError::Network(e.to_string()))?;
    let payload: GeoResponse = response
        .json()
        .await
        .map_err(|e| AppError::WeatherApi(e.to_string()))?;
    if payload.code != "200" {
        return Err(AppError::WeatherApi(format!(
            "城市搜索代码 {}",
            payload.code
        )));
    }
    Ok(payload
        .locations
        .into_iter()
        .map(|x| City {
            id: x.id,
            name: x.name,
            province: x.adm1,
            prefecture: x.adm2,
        })
        .collect())
}

pub async fn locate(
    client: &Client,
    settings: &AppSettings,
    longitude: f64,
    latitude: f64,
) -> Result<City> {
    let keyword = format!("{longitude:.2},{latitude:.2}");
    search(client, settings, &keyword)
        .await?
        .into_iter()
        .next()
        .ok_or_else(|| AppError::WeatherApi("未能将当前位置匹配到中国城市".into()))
}

pub fn summarize(values: &[HourlyForecast], date: NaiveDate) -> DaySummary {
    let mut rainy: Vec<&HourlyForecast> = values
        .iter()
        .filter(|x| x.forecast_time.with_timezone(&Local).date_naive() == date && is_rain(x))
        .collect();
    rainy.sort_by_key(|x| x.forecast_time);
    if rainy.is_empty() {
        return DaySummary {
            date,
            has_rain: false,
            intensity: "none".into(),
            ranges: vec![],
        };
    }
    let max_mm = rainy.iter().map(|x| x.precipitation_mm).fold(0.0, f64::max);
    let max_pop = rainy
        .iter()
        .map(|x| x.precipitation_probability)
        .max()
        .unwrap_or(0);
    let intensity = if max_mm >= 10.0 || max_pop >= 80 {
        "heavy"
    } else if max_mm >= 2.0 || max_pop >= 40 {
        "moderate"
    } else {
        "light"
    };
    let mut ranges = vec![];
    let mut start = rainy[0].forecast_time;
    let mut end = start + Duration::hours(1);
    for item in rainy.iter().skip(1) {
        if item.forecast_time <= end + Duration::minutes(5) {
            end = item.forecast_time + Duration::hours(1);
        } else {
            ranges.push(RainRange { start, end });
            start = item.forecast_time;
            end = start + Duration::hours(1);
        }
    }
    ranges.push(RainRange { start, end });
    DaySummary {
        date,
        has_rain: true,
        intensity: intensity.into(),
        ranges,
    }
}

fn is_rain(x: &HourlyForecast) -> bool {
    x.precipitation_mm > 0.0
        || (x.precipitation_probability >= 40
            && (x.condition_text.contains('雨')
                || x.condition_text.to_lowercase().contains("rain")))
}

pub fn build_dashboard(
    settings: &AppSettings,
    hourly: Vec<HourlyForecast>,
    daily: Vec<DailyForecast>,
    updated_at: Option<chrono::DateTime<chrono::Utc>>,
    cached: bool,
) -> DashboardData {
    let today = Local::now().date_naive();
    let tomorrow = today.succ_opt().unwrap_or(today);
    DashboardData {
        city_code: settings.current_city_code.clone(),
        city_name: settings.current_city_name.clone(),
        updated_at,
        is_cached: cached,
        today: summarize(&hourly, today),
        tomorrow: summarize(&hourly, tomorrow),
        hourly,
        daily,
    }
}

pub fn notice_text(summary: &DaySummary, today: bool) -> (String, String, String) {
    let label = if today { "今天" } else { "明天" };
    let intensity = match summary.intensity.as_str() {
        "heavy" => "大雨",
        "moderate" => "中雨",
        "light" => "小雨",
        _ => "降雨",
    };
    let ranges = summary
        .ranges
        .iter()
        .map(|r| format!("{:02}:00–{:02}:00", r.start.hour(), r.end.hour()))
        .collect::<Vec<_>>()
        .join("、");
    let title = format!("{label}有雨，记得带伞");
    let body = format!("{ranges} 有降雨（{intensity}）");
    let mut sha = Sha256::new();
    sha.update(format!("{}|{}|{}", summary.date, label, body));
    (title, body, format!("{:x}", sha.finalize()))
}

#[cfg(test)]
mod tests {
    use super::*;

    fn hour(value: &str, precipitation_mm: f64, probability: i32, text: &str) -> HourlyForecast {
        HourlyForecast {
            forecast_time: parse_forecast_time(value).expect("forecast time"),
            temperature: 24,
            precipitation_mm,
            precipitation_probability: probability,
            condition_text: text.into(),
            icon: "305".into(),
            wind_direction: "东风".into(),
            wind_scale: "1-3".into(),
            humidity: 70,
        }
    }
    #[test]
    fn empty_day_has_no_rain() {
        let d = Local::now().date_naive();
        assert!(!summarize(&[], d).has_rain);
    }

    #[test]
    fn qweather_timestamp_without_seconds_is_supported() {
        let value = parse_forecast_time("2026-07-18T20:00+08:00").expect("QWeather timestamp");
        assert_eq!(value.hour(), 20);
    }

    #[test]
    fn contiguous_rain_is_merged_and_gaps_are_split() {
        let values = vec![
            hour("2026-07-19T02:00+08:00", 2.5, 60, "中雨"),
            hour("2026-07-19T03:00+08:00", 1.0, 60, "小雨"),
            hour("2026-07-19T05:00+08:00", 1.0, 60, "小雨"),
        ];
        let summary = summarize(
            &values,
            chrono::NaiveDate::from_ymd_opt(2026, 7, 19).unwrap(),
        );
        assert!(summary.has_rain);
        assert_eq!(summary.intensity, "moderate");
        assert_eq!(summary.ranges.len(), 2);
        assert_eq!(summary.ranges[0].start.hour(), 2);
        assert_eq!(summary.ranges[0].end.hour(), 4);
    }

    #[test]
    fn probability_requires_rain_condition_when_precipitation_is_zero() {
        let date = chrono::NaiveDate::from_ymd_opt(2026, 7, 19).unwrap();
        let cloudy = vec![hour("2026-07-19T02:00+08:00", 0.0, 90, "多云")];
        let rainy = vec![hour("2026-07-19T02:00+08:00", 0.0, 90, "雷阵雨")];
        assert!(!summarize(&cloudy, date).has_rain);
        assert_eq!(summarize(&rainy, date).intensity, "heavy");
    }
}
