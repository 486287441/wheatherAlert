use crate::{
    error::Result,
    models::{AppSettings, DailyForecast, HistoryEntry, HourlyForecast},
};
use chrono::Utc;
use rusqlite::{params, Connection};
use std::{
    fs,
    path::{Path, PathBuf},
};

pub struct Storage {
    db_path: PathBuf,
    settings_path: PathBuf,
}

impl Storage {
    pub fn new(app_dir: PathBuf) -> Result<Self> {
        fs::create_dir_all(&app_dir)?;
        let storage = Self {
            db_path: app_dir.join("weather-alert.db"),
            settings_path: app_dir.join("settings.json"),
        };
        storage.initialize()?;
        Ok(storage)
    }

    fn connection(&self) -> Result<Connection> {
        Ok(Connection::open(&self.db_path)?)
    }

    fn initialize(&self) -> Result<()> {
        self.connection()?.execute_batch(r#"
            CREATE TABLE IF NOT EXISTS notification_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT, created_at TEXT NOT NULL, type TEXT NOT NULL,
                city_code TEXT NOT NULL, title TEXT NOT NULL, body TEXT NOT NULL, meta_json TEXT NOT NULL DEFAULT '{}'
            );
            CREATE TABLE IF NOT EXISTS rain_notification_state (
                city_code TEXT NOT NULL, target_date TEXT NOT NULL, perspective TEXT NOT NULL DEFAULT 'day',
                notified_at TEXT NOT NULL, message_hash TEXT NOT NULL,
                PRIMARY KEY (city_code, target_date, perspective)
            );
            CREATE TABLE IF NOT EXISTS hourly_forecast_v2 (
                city_code TEXT NOT NULL, forecast_time TEXT NOT NULL, temperature INTEGER NOT NULL,
                precipitation_mm REAL NOT NULL, precipitation_probability INTEGER NOT NULL,
                condition_text TEXT NOT NULL, icon TEXT NOT NULL, wind_direction TEXT NOT NULL,
                wind_scale TEXT NOT NULL, humidity INTEGER NOT NULL, captured_at TEXT NOT NULL,
                PRIMARY KEY (city_code, forecast_time)
            );
            CREATE TABLE IF NOT EXISTS daily_forecast_v1 (
                city_code TEXT NOT NULL, forecast_date TEXT NOT NULL,
                temperature_max INTEGER NOT NULL, temperature_min INTEGER NOT NULL,
                condition_day TEXT NOT NULL, condition_night TEXT NOT NULL,
                icon_day TEXT NOT NULL, icon_night TEXT NOT NULL,
                precipitation_mm REAL NOT NULL, humidity INTEGER NOT NULL,
                wind_direction_day TEXT NOT NULL, wind_scale_day TEXT NOT NULL,
                captured_at TEXT NOT NULL,
                PRIMARY KEY (city_code, forecast_date)
            );
        "#)?;
        Ok(())
    }

    pub fn load_settings(&self) -> Result<AppSettings> {
        if !self.settings_path.exists() {
            return Ok(AppSettings::default());
        }
        Ok(serde_json::from_str(&fs::read_to_string(
            &self.settings_path,
        )?)?)
    }

    pub fn has_settings(&self) -> bool {
        self.settings_path.exists()
    }

    pub fn save_settings(&self, value: &AppSettings) -> Result<()> {
        let temp = self.settings_path.with_extension("json.tmp");
        fs::write(&temp, serde_json::to_vec_pretty(value)?)?;
        if self.settings_path.exists() {
            fs::remove_file(&self.settings_path)?;
        }
        fs::rename(temp, &self.settings_path)?;
        Ok(())
    }

    pub fn cache_forecast(&self, city: &str, values: &[HourlyForecast]) -> Result<()> {
        let mut conn = self.connection()?;
        let tx = conn.transaction()?;
        tx.execute(
            "DELETE FROM hourly_forecast_v2 WHERE city_code = ?1",
            [city],
        )?;
        let captured = Utc::now().to_rfc3339();
        for x in values {
            tx.execute(r#"INSERT INTO hourly_forecast_v2
                (city_code,forecast_time,temperature,precipitation_mm,precipitation_probability,condition_text,icon,wind_direction,wind_scale,humidity,captured_at)
                VALUES (?1,?2,?3,?4,?5,?6,?7,?8,?9,?10,?11)"#,
                params![city, x.forecast_time.to_rfc3339(), x.temperature, x.precipitation_mm, x.precipitation_probability,
                    x.condition_text, x.icon, x.wind_direction, x.wind_scale, x.humidity, captured])?;
        }
        tx.commit()?;
        Ok(())
    }

    pub fn load_forecast(
        &self,
        city: &str,
    ) -> Result<(Vec<HourlyForecast>, Option<chrono::DateTime<Utc>>)> {
        let conn = self.connection()?;
        let mut stmt = conn.prepare(r#"SELECT forecast_time,temperature,precipitation_mm,precipitation_probability,
            condition_text,icon,wind_direction,wind_scale,humidity,captured_at FROM hourly_forecast_v2
            WHERE city_code=?1 AND forecast_time >= datetime('now','-2 hours') ORDER BY forecast_time LIMIT 72"#)?;
        let mut updated = None;
        let rows = stmt.query_map([city], |row| {
            let forecast_time: String = row.get(0)?;
            let captured: String = row.get(9)?;
            if updated.is_none() {
                updated = chrono::DateTime::parse_from_rfc3339(&captured)
                    .ok()
                    .map(|x| x.with_timezone(&Utc));
            }
            Ok(HourlyForecast {
                forecast_time: chrono::DateTime::parse_from_rfc3339(&forecast_time).map_err(
                    |e| {
                        rusqlite::Error::FromSqlConversionFailure(
                            0,
                            rusqlite::types::Type::Text,
                            Box::new(e),
                        )
                    },
                )?,
                temperature: row.get(1)?,
                precipitation_mm: row.get(2)?,
                precipitation_probability: row.get(3)?,
                condition_text: row.get(4)?,
                icon: row.get(5)?,
                wind_direction: row.get(6)?,
                wind_scale: row.get(7)?,
                humidity: row.get(8)?,
            })
        })?;
        Ok((rows.collect::<std::result::Result<Vec<_>, _>>()?, updated))
    }

    pub fn cache_daily_forecast(&self, city: &str, values: &[DailyForecast]) -> Result<()> {
        let mut conn = self.connection()?;
        let tx = conn.transaction()?;
        tx.execute("DELETE FROM daily_forecast_v1 WHERE city_code = ?1", [city])?;
        let captured = Utc::now().to_rfc3339();
        for x in values {
            tx.execute(
                r#"INSERT INTO daily_forecast_v1
                (city_code,forecast_date,temperature_max,temperature_min,condition_day,condition_night,
                 icon_day,icon_night,precipitation_mm,humidity,wind_direction_day,wind_scale_day,captured_at)
                VALUES (?1,?2,?3,?4,?5,?6,?7,?8,?9,?10,?11,?12,?13)"#,
                params![
                    city,
                    x.forecast_date.to_string(),
                    x.temperature_max,
                    x.temperature_min,
                    x.condition_day,
                    x.condition_night,
                    x.icon_day,
                    x.icon_night,
                    x.precipitation_mm,
                    x.humidity,
                    x.wind_direction_day,
                    x.wind_scale_day,
                    captured
                ],
            )?;
        }
        tx.commit()?;
        Ok(())
    }

    pub fn load_daily_forecast(&self, city: &str) -> Result<Vec<DailyForecast>> {
        let conn = self.connection()?;
        let mut stmt = conn.prepare(
            r#"SELECT forecast_date,temperature_max,temperature_min,condition_day,condition_night,
               icon_day,icon_night,precipitation_mm,humidity,wind_direction_day,wind_scale_day
               FROM daily_forecast_v1
               WHERE city_code=?1 AND forecast_date >= date('now','localtime')
               ORDER BY forecast_date LIMIT 30"#,
        )?;
        let rows = stmt.query_map([city], |row| {
            let forecast_date: String = row.get(0)?;
            Ok(DailyForecast {
                forecast_date: chrono::NaiveDate::parse_from_str(&forecast_date, "%Y-%m-%d")
                    .map_err(|e| {
                        rusqlite::Error::FromSqlConversionFailure(
                            0,
                            rusqlite::types::Type::Text,
                            Box::new(e),
                        )
                    })?,
                temperature_max: row.get(1)?,
                temperature_min: row.get(2)?,
                condition_day: row.get(3)?,
                condition_night: row.get(4)?,
                icon_day: row.get(5)?,
                icon_night: row.get(6)?,
                precipitation_mm: row.get(7)?,
                humidity: row.get(8)?,
                wind_direction_day: row.get(9)?,
                wind_scale_day: row.get(10)?,
            })
        })?;
        Ok(rows.collect::<std::result::Result<Vec<_>, _>>()?)
    }

    pub fn history(&self) -> Result<Vec<HistoryEntry>> {
        let conn = self.connection()?;
        let mut stmt = conn.prepare("SELECT id,created_at,type,city_code,title,body FROM notification_history ORDER BY id DESC LIMIT 200")?;
        let rows = stmt.query_map([], |r| {
            Ok(HistoryEntry {
                id: r.get(0)?,
                created_at: r.get(1)?,
                kind: r.get(2)?,
                city_code: r.get(3)?,
                title: r.get(4)?,
                body: r.get(5)?,
            })
        })?;
        Ok(rows.collect::<std::result::Result<Vec<_>, _>>()?)
    }

    pub fn clear_history(&self) -> Result<()> {
        self.connection()?
            .execute("DELETE FROM notification_history", [])?;
        Ok(())
    }

    pub fn has_rain_notice(&self, city: &str, date: &str, perspective: &str) -> Result<bool> {
        Ok(self.connection()?.query_row("SELECT EXISTS(SELECT 1 FROM rain_notification_state WHERE city_code=?1 AND target_date=?2 AND perspective=?3)", params![city,date,perspective], |r| r.get(0))?)
    }

    pub fn record_rain_notice(
        &self,
        city: &str,
        date: &str,
        perspective: &str,
        title: &str,
        body: &str,
        hash: &str,
    ) -> Result<()> {
        let mut conn = self.connection()?;
        let tx = conn.transaction()?;
        let now = Utc::now().to_rfc3339();
        tx.execute("INSERT OR IGNORE INTO rain_notification_state(city_code,target_date,perspective,notified_at,message_hash) VALUES(?1,?2,?3,?4,?5)", params![city,date,perspective,now,hash])?;
        tx.execute("INSERT INTO notification_history(created_at,type,city_code,title,body,meta_json) VALUES(?1,'Rain',?2,?3,?4,'{}')", params![now,city,title,body])?;
        tx.commit()?;
        Ok(())
    }

    pub fn path(&self) -> &Path {
        &self.db_path
    }
}
