mod ai;
mod error;
mod models;
mod storage;
mod weather;

use std::sync::Arc;
use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    AppHandle, Manager, State,
};
use tauri_plugin_autostart::{MacosLauncher, ManagerExt as AutostartExt};
use tauri_plugin_notification::NotificationExt;
use tokio::sync::RwLock;

use error::{AppError, Result};
use models::{AppSettings, City, DashboardData, HistoryEntry};
use storage::Storage;

fn migrate_legacy_settings(storage: &Storage) -> Option<AppSettings> {
    if storage.has_settings() {
        return None;
    }
    let path = std::env::current_dir()
        .ok()?
        .join("src/WeatherAlert.TrayPopup.App/appsettings.Local.json");
    let value: serde_json::Value =
        serde_json::from_str(&std::fs::read_to_string(path).ok()?).ok()?;
    let weather = value.get("Weather")?;
    let mut settings = AppSettings::default();
    settings.api_key = weather
        .get("ApiKey")
        .and_then(|x| x.as_str())
        .unwrap_or_default()
        .to_string();
    settings.api_base_url = weather
        .get("ApiBaseUrl")
        .and_then(|x| x.as_str())
        .unwrap_or(&settings.api_base_url)
        .trim_end_matches('/')
        .to_string();
    settings.current_city_code = weather
        .get("DefaultCityCode")
        .and_then(|x| x.as_str())
        .unwrap_or(&settings.current_city_code)
        .to_string();
    settings.polling_minutes = weather
        .get("PollingMinutes")
        .and_then(|x| x.as_u64())
        .unwrap_or(settings.polling_minutes)
        .clamp(15, 1440);
    if settings.current_city_code == "101020100" {
        settings.current_city_name = "上海".into();
    }
    if !settings.api_key.is_empty() {
        let _ = storage.save_settings(&settings);
    }
    Some(settings)
}

#[derive(Clone)]
struct SharedState(Arc<StateInner>);

struct StateInner {
    settings: RwLock<AppSettings>,
    storage: Storage,
    client: reqwest::Client,
}

impl SharedState {
    fn new(storage: Storage, settings: AppSettings) -> Self {
        Self(Arc::new(StateInner {
            settings: RwLock::new(settings),
            storage,
            client: reqwest::Client::builder()
                .timeout(std::time::Duration::from_secs(15))
                .build()
                .expect("HTTP client"),
        }))
    }
}

#[tauri::command]
async fn get_dashboard(state: State<'_, SharedState>) -> Result<DashboardData> {
    dashboard_from_cache(state.inner()).await
}

async fn dashboard_from_cache(state: &SharedState) -> Result<DashboardData> {
    let settings = state.0.settings.read().await.clone();
    let (hourly, updated) = state.0.storage.load_forecast(&settings.current_city_code)?;
    let daily = state
        .0
        .storage
        .load_daily_forecast(&settings.current_city_code)?;
    if hourly.is_empty() {
        Ok(DashboardData::empty(&settings))
    } else {
        Ok(weather::build_dashboard(
            &settings, hourly, daily, updated, true,
        ))
    }
}

#[tauri::command]
async fn refresh_weather(app: AppHandle, state: State<'_, SharedState>) -> Result<DashboardData> {
    refresh_internal(&app, state.inner(), false).await
}

#[tauri::command]
async fn ask_weather_ai(question: String, state: State<'_, SharedState>) -> Result<String> {
    let settings = state.0.settings.read().await.clone();
    let dashboard = dashboard_from_cache(state.inner()).await?;
    ai::ask(&settings, &dashboard, &question).await
}

async fn refresh_internal(
    app: &AppHandle,
    state: &SharedState,
    background: bool,
) -> Result<DashboardData> {
    let settings = state.0.settings.read().await.clone();
    let hourly = match weather::fetch(&state.0.client, &settings).await {
        Ok(values) => values,
        Err(error) if background => {
            eprintln!("Weather refresh skipped: {error}");
            return dashboard_from_cache(state).await;
        }
        Err(error) => return Err(error),
    };
    let daily = match weather::fetch_daily(&state.0.client, &settings).await {
        Ok(values) => values,
        Err(error) if background => {
            eprintln!("Daily weather refresh skipped: {error}");
            return dashboard_from_cache(state).await;
        }
        Err(error) => return Err(error),
    };
    state
        .0
        .storage
        .cache_forecast(&settings.current_city_code, &hourly)?;
    state
        .0
        .storage
        .cache_daily_forecast(&settings.current_city_code, &daily)?;
    let dashboard =
        weather::build_dashboard(&settings, hourly, daily, Some(chrono::Utc::now()), false);
    if let Some(tray) = app.tray_by_id("main-tray") {
        let status = if dashboard.today.has_rain || dashboard.tomorrow.has_rain {
            "未来两天有雨"
        } else {
            "未来两天暂无降雨"
        };
        let _ = tray.set_tooltip(Some(format!("WeatherAlert · {status}")));
    }
    if settings.notifications {
        notify_rain(app, state, &settings, &dashboard).await?;
    }
    Ok(dashboard)
}

async fn notify_rain(
    app: &AppHandle,
    state: &SharedState,
    settings: &AppSettings,
    dashboard: &DashboardData,
) -> Result<()> {
    for (perspective, summary, today) in [
        ("today", &dashboard.today, true),
        ("tomorrow", &dashboard.tomorrow, false),
    ] {
        if !summary.has_rain {
            continue;
        }
        let date = summary.date.to_string();
        if state
            .0
            .storage
            .has_rain_notice(&settings.current_city_code, &date, perspective)?
        {
            continue;
        }
        let (title, body, hash) = weather::notice_text(summary, today);
        state.0.storage.record_rain_notice(
            &settings.current_city_code,
            &date,
            perspective,
            &title,
            &body,
            &hash,
        )?;
        app.notification()
            .builder()
            .title(&title)
            .body(&body)
            .show()
            .map_err(|e| AppError::System(e.to_string()))?;
    }
    Ok(())
}

#[tauri::command]
fn get_history(state: State<'_, SharedState>) -> Result<Vec<HistoryEntry>> {
    state.0.storage.history()
}

#[tauri::command]
fn clear_history(state: State<'_, SharedState>) -> Result<()> {
    state.0.storage.clear_history()
}

#[tauri::command]
async fn search_cities(keyword: String, state: State<'_, SharedState>) -> Result<Vec<City>> {
    let settings = state.0.settings.read().await.clone();
    weather::search(&state.0.client, &settings, &keyword).await
}

#[tauri::command]
async fn locate_city(longitude: f64, latitude: f64, state: State<'_, SharedState>) -> Result<City> {
    if !longitude.is_finite()
        || !latitude.is_finite()
        || longitude.abs() > 180.0
        || latitude.abs() > 90.0
    {
        return Err(AppError::WeatherApi("无效的经纬度".into()));
    }
    let settings = state.0.settings.read().await.clone();
    weather::locate(&state.0.client, &settings, longitude, latitude).await
}

#[tauri::command]
async fn select_city(
    app: AppHandle,
    city: City,
    state: State<'_, SharedState>,
) -> Result<DashboardData> {
    {
        let mut settings = state.0.settings.write().await;
        settings.current_city_code = city.id;
        settings.current_city_name = city.name;
        state.0.storage.save_settings(&settings)?;
    }
    refresh_internal(&app, state.inner(), false).await
}

#[tauri::command]
async fn get_settings(state: State<'_, SharedState>) -> Result<AppSettings> {
    Ok(state.0.settings.read().await.clone())
}

#[tauri::command]
async fn save_settings(
    app: AppHandle,
    mut settings: AppSettings,
    state: State<'_, SharedState>,
) -> Result<AppSettings> {
    settings.polling_minutes = settings.polling_minutes.clamp(15, 24 * 60);
    settings.api_base_url = settings.api_base_url.trim_end_matches('/').to_string();
    url::Url::parse(&settings.api_base_url)
        .map_err(|_| AppError::InvalidHost(settings.api_base_url.clone()))?;
    if settings.autostart {
        app.autolaunch().enable()
    } else {
        app.autolaunch().disable()
    }
    .map_err(|e| AppError::System(e.to_string()))?;
    state.0.storage.save_settings(&settings)?;
    *state.0.settings.write().await = settings.clone();
    Ok(settings)
}

fn show_main(app: &AppHandle) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.show();
        let _ = window.unminimize();
        let _ = window.set_focus();
    }
}

fn setup_tray(app: &mut tauri::App) -> std::result::Result<(), Box<dyn std::error::Error>> {
    let open = MenuItem::with_id(app, "open", "打开 WeatherAlert", true, None::<&str>)?;
    let check = MenuItem::with_id(app, "check", "立即检查天气", true, None::<&str>)?;
    let quit = MenuItem::with_id(app, "quit", "退出", true, None::<&str>)?;
    let menu = Menu::with_items(app, &[&open, &check, &quit])?;
    let mut builder = TrayIconBuilder::with_id("main-tray")
        .tooltip("WeatherAlert · 降雨提醒")
        .menu(&menu)
        .show_menu_on_left_click(false);
    if let Some(icon) = app.default_window_icon() {
        builder = builder.icon(icon.clone());
    }
    builder
        .on_menu_event(|app, event| match event.id.as_ref() {
            "open" => show_main(app),
            "check" => {
                let handle = app.clone();
                let state = app.state::<SharedState>().inner().clone();
                tauri::async_runtime::spawn(async move {
                    let _ = refresh_internal(&handle, &state, false).await;
                });
            }
            "quit" => app.exit(0),
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                show_main(tray.app_handle());
            }
        })
        .build(app)?;
    Ok(())
}

fn start_worker(app: AppHandle, state: SharedState) {
    tauri::async_runtime::spawn(async move {
        loop {
            let configured = !state.0.settings.read().await.api_key.trim().is_empty();
            if configured {
                let _ = refresh_internal(&app, &state, true).await;
            }
            let minutes = state.0.settings.read().await.polling_minutes.max(15);
            tokio::time::sleep(std::time::Duration::from_secs(minutes * 60)).await;
        }
    });
}

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_autostart::init(
            MacosLauncher::LaunchAgent,
            Some(vec!["--autostart"]),
        ))
        .setup(|app| {
            let app_dir = app.path().app_data_dir()?;
            let storage = Storage::new(app_dir)?;
            eprintln!("WeatherAlert database: {}", storage.path().display());
            let mut settings =
                migrate_legacy_settings(&storage).unwrap_or(storage.load_settings()?);
            settings.autostart = app.autolaunch().is_enabled().unwrap_or(settings.autostart);
            let state = SharedState::new(storage, settings);
            app.manage(state.clone());
            setup_tray(app)?;
            if std::env::args().any(|arg| arg == "--autostart") {
                if let Some(window) = app.get_webview_window("main") {
                    let _ = window.hide();
                }
            }
            start_worker(app.handle().clone(), state);
            Ok(())
        })
        .on_window_event(|window, event| {
            if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                api.prevent_close();
                let _ = window.hide();
            }
        })
        .invoke_handler(tauri::generate_handler![
            get_dashboard,
            refresh_weather,
            get_history,
            clear_history,
            search_cities,
            locate_city,
            select_city,
            get_settings,
            save_settings,
            ask_weather_ai
        ])
        .run(tauri::generate_context!())
        .expect("error while running WeatherAlert");
}
