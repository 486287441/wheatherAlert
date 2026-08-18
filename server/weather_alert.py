#!/usr/bin/env python3
"""Independent QWeather-to-Bark rain notifier for the cloud server."""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import os
import sqlite3
import sys
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from datetime import date, datetime, timedelta
from pathlib import Path
from typing import Any
from zoneinfo import ZoneInfo


SHANGHAI = ZoneInfo("Asia/Shanghai")


@dataclass(frozen=True)
class Config:
    qweather_api_host: str
    qweather_api_key: str
    location: str
    city_name: str
    polling_minutes: int
    bark_server: str
    bark_device_key: str
    bark_icon_url: str
    state_db: Path

    @classmethod
    def from_env(cls) -> "Config":
        required = {
            "QWEATHER_API_HOST": os.getenv("QWEATHER_API_HOST", "").rstrip("/"),
            "QWEATHER_API_KEY": os.getenv("QWEATHER_API_KEY", ""),
            "BARK_DEVICE_KEY": os.getenv("BARK_DEVICE_KEY", ""),
        }
        missing = [name for name, value in required.items() if not value]
        if missing:
            raise ValueError(f"missing environment variables: {', '.join(missing)}")
        polling_minutes = int(os.getenv("POLLING_MINUTES", "60"))
        if not 15 <= polling_minutes <= 1440:
            raise ValueError("POLLING_MINUTES must be between 15 and 1440")
        return cls(
            qweather_api_host=required["QWEATHER_API_HOST"],
            qweather_api_key=required["QWEATHER_API_KEY"],
            location=os.getenv("QWEATHER_LOCATION", "101280704"),
            city_name=os.getenv("CITY_NAME", "香洲"),
            polling_minutes=polling_minutes,
            bark_server=os.getenv("BARK_SERVER", "https://api.day.app").rstrip("/"),
            bark_device_key=required["BARK_DEVICE_KEY"],
            bark_icon_url=os.getenv("BARK_ICON_URL", ""),
            state_db=Path(os.getenv("STATE_DB", "/opt/weather-alert-server/state.db")),
        )


@dataclass(frozen=True)
class HourlyForecast:
    forecast_time: datetime
    precipitation_mm: float
    precipitation_probability: int
    condition_text: str


@dataclass(frozen=True)
class RainSummary:
    target_date: date
    has_rain: bool
    intensity: str
    ranges: tuple[tuple[datetime, datetime], ...]


def log(event: str, **fields: Any) -> None:
    print(
        json.dumps(
            {"time": datetime.now(SHANGHAI).isoformat(), "event": event, **fields},
            ensure_ascii=False,
            separators=(",", ":"),
        ),
        flush=True,
    )


def request_json(
    url: str,
    *,
    method: str = "GET",
    payload: dict[str, Any] | None = None,
    timeout: int = 15,
) -> dict[str, Any]:
    body = None
    headers = {"User-Agent": "WeatherAlert-Server/1.0"}
    if payload is not None:
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        headers["Content-Type"] = "application/json; charset=utf-8"
    request = urllib.request.Request(url, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            response_body = response.read()
            if response.headers.get("Content-Encoding", "").lower() == "gzip":
                response_body = gzip.decompress(response_body)
            return json.loads(response_body.decode("utf-8"))
    except urllib.error.HTTPError as error:
        detail = error.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"HTTP {error.code}: {detail}") from error
    except (urllib.error.URLError, TimeoutError) as error:
        raise RuntimeError(f"network error: {error}") from error


def qweather_url(config: Config, path: str) -> str:
    query = urllib.parse.urlencode(
        {"location": config.location, "key": config.qweather_api_key, "lang": "zh"}
    )
    return f"{config.qweather_api_host}{path}?{query}"


def fetch_forecasts(config: Config) -> list[HourlyForecast]:
    hourly_payload = request_json(qweather_url(config, "/v7/weather/72h"))
    if hourly_payload.get("code") != "200":
        raise RuntimeError(f"hourly weather API code {hourly_payload.get('code')}")

    # The desktop app requires both endpoints to succeed before processing alerts.
    daily_payload = request_json(qweather_url(config, "/v7/weather/30d"))
    if daily_payload.get("code") != "200":
        raise RuntimeError(f"daily weather API code {daily_payload.get('code')}")

    forecasts: list[HourlyForecast] = []
    for item in hourly_payload.get("hourly", []):
        forecasts.append(
            HourlyForecast(
                forecast_time=datetime.fromisoformat(item["fxTime"]),
                precipitation_mm=float(item.get("precip") or 0),
                precipitation_probability=int(item.get("pop") or 0),
                condition_text=item.get("text") or "",
            )
        )
    return forecasts


def is_rain(item: HourlyForecast) -> bool:
    return item.precipitation_mm > 0 or (
        item.precipitation_probability >= 40
        and ("雨" in item.condition_text or "rain" in item.condition_text.lower())
    )


def summarize(values: list[HourlyForecast], target_date: date) -> RainSummary:
    rainy = sorted(
        (
            item
            for item in values
            if item.forecast_time.astimezone(SHANGHAI).date() == target_date
            and is_rain(item)
        ),
        key=lambda item: item.forecast_time,
    )
    if not rainy:
        return RainSummary(target_date, False, "none", ())

    max_mm = max(item.precipitation_mm for item in rainy)
    max_pop = max(item.precipitation_probability for item in rainy)
    if max_mm >= 10 or max_pop >= 80:
        intensity = "heavy"
    elif max_mm >= 2 or max_pop >= 40:
        intensity = "moderate"
    else:
        intensity = "light"

    ranges: list[tuple[datetime, datetime]] = []
    start = rainy[0].forecast_time
    end = start + timedelta(hours=1)
    for item in rainy[1:]:
        if item.forecast_time <= end + timedelta(minutes=5):
            end = item.forecast_time + timedelta(hours=1)
        else:
            ranges.append((start, end))
            start = item.forecast_time
            end = start + timedelta(hours=1)
    ranges.append((start, end))
    return RainSummary(target_date, True, intensity, tuple(ranges))


def notice_text(summary: RainSummary, today: bool) -> tuple[str, str, str]:
    label = "今天" if today else "明天"
    intensity = {
        "heavy": "大雨",
        "moderate": "中雨",
        "light": "小雨",
    }.get(summary.intensity, "降雨")
    ranges = "、".join(
        f"{start.astimezone(SHANGHAI):%H}:00–{end.astimezone(SHANGHAI):%H}:00"
        for start, end in summary.ranges
    )
    title = f"{label}有雨，记得带伞"
    body = f"{ranges} 有降雨（{intensity}）"
    digest = hashlib.sha256(
        f"{summary.target_date}|{label}|{body}".encode("utf-8")
    ).hexdigest()
    return title, body, digest


class NoticeState:
    def __init__(self, path: Path):
        path.parent.mkdir(parents=True, exist_ok=True)
        self.connection = sqlite3.connect(path)
        self.connection.execute(
            """
            CREATE TABLE IF NOT EXISTS rain_notification_state (
                city_code TEXT NOT NULL,
                target_date TEXT NOT NULL,
                perspective TEXT NOT NULL,
                notified_at TEXT NOT NULL,
                message_hash TEXT NOT NULL,
                PRIMARY KEY (city_code, target_date, perspective)
            )
            """
        )
        self.connection.commit()

    def has_notice(self, city: str, target_date: str, perspective: str) -> bool:
        row = self.connection.execute(
            """
            SELECT EXISTS(
                SELECT 1 FROM rain_notification_state
                WHERE city_code=? AND target_date=? AND perspective=?
            )
            """,
            (city, target_date, perspective),
        ).fetchone()
        return bool(row and row[0])

    def record(
        self,
        city: str,
        target_date: str,
        perspective: str,
        message_hash: str,
    ) -> None:
        self.connection.execute(
            """
            INSERT OR IGNORE INTO rain_notification_state
            (city_code, target_date, perspective, notified_at, message_hash)
            VALUES (?, ?, ?, ?, ?)
            """,
            (
                city,
                target_date,
                perspective,
                datetime.now(SHANGHAI).isoformat(),
                message_hash,
            ),
        )
        self.connection.commit()


def send_bark(config: Config, title: str, body: str) -> None:
    payload: dict[str, Any] = {
        "device_key": config.bark_device_key,
        "title": title,
        "body": body,
        "group": "weather-alert",
        "level": "active",
    }
    if config.bark_icon_url:
        payload["icon"] = config.bark_icon_url
    response = request_json(f"{config.bark_server}/push", method="POST", payload=payload)
    if response.get("code") != 200:
        raise RuntimeError(f"Bark API error: {response}")


def run(config: Config, *, dry_run: bool = False) -> int:
    forecasts = fetch_forecasts(config)
    now = datetime.now(SHANGHAI)
    summaries = (
        ("today", summarize(forecasts, now.date()), True),
        ("tomorrow", summarize(forecasts, now.date() + timedelta(days=1)), False),
    )
    state = NoticeState(config.state_db)
    sent = 0
    for perspective, summary, today in summaries:
        log(
            "rain_summary",
            city=config.city_name,
            perspective=perspective,
            date=str(summary.target_date),
            has_rain=summary.has_rain,
            intensity=summary.intensity,
            range_count=len(summary.ranges),
        )
        if not summary.has_rain:
            continue
        if state.has_notice(config.location, str(summary.target_date), perspective):
            log("notice_skipped_duplicate", perspective=perspective)
            continue
        title, body, digest = notice_text(summary, today)
        if dry_run:
            log("notice_dry_run", title=title, body=body)
            continue
        send_bark(config, title, body)
        state.record(config.location, str(summary.target_date), perspective, digest)
        sent += 1
        log("notice_sent", title=title, body=body)
    log("check_complete", forecast_count=len(forecasts), sent=sent, dry_run=dry_run)
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--dry-run", action="store_true", help="check weather without sending or deduplicating"
    )
    parser.add_argument(
        "--test-push", action="store_true", help="send one Bark connectivity test"
    )
    args = parser.parse_args()
    try:
        config = Config.from_env()
        if args.test_push:
            send_bark(
                config,
                "WeatherAlert 云端测试",
                f"{config.city_name}天气检测已部署，服务器推送链路正常。",
            )
            log("test_push_sent", city=config.city_name)
            return 0
        return run(config, dry_run=args.dry_run)
    except Exception as error:
        log("check_failed", error=str(error))
        return 1


if __name__ == "__main__":
    sys.exit(main())
