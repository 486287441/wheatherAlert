import sys
import unittest
from datetime import date, datetime
from pathlib import Path
from zoneinfo import ZoneInfo


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from weather_alert import HourlyForecast, notice_text, summarize


TZ = ZoneInfo("Asia/Shanghai")


def hour(value: str, mm: float, probability: int, text: str) -> HourlyForecast:
    return HourlyForecast(datetime.fromisoformat(value), mm, probability, text)


class WeatherAlertTests(unittest.TestCase):
    def test_empty_day_has_no_rain(self):
        self.assertFalse(summarize([], date(2026, 7, 19)).has_rain)

    def test_contiguous_rain_is_merged_and_gaps_are_split(self):
        values = [
            hour("2026-07-19T02:00+08:00", 2.5, 60, "中雨"),
            hour("2026-07-19T03:00+08:00", 1.0, 60, "小雨"),
            hour("2026-07-19T05:00+08:00", 1.0, 60, "小雨"),
        ]
        summary = summarize(values, date(2026, 7, 19))
        self.assertTrue(summary.has_rain)
        self.assertEqual("moderate", summary.intensity)
        self.assertEqual(2, len(summary.ranges))
        self.assertEqual(2, summary.ranges[0][0].astimezone(TZ).hour)
        self.assertEqual(4, summary.ranges[0][1].astimezone(TZ).hour)

    def test_probability_requires_rain_condition_when_mm_is_zero(self):
        cloudy = [hour("2026-07-19T02:00+08:00", 0, 90, "多云")]
        rainy = [hour("2026-07-19T02:00+08:00", 0, 90, "雷阵雨")]
        self.assertFalse(summarize(cloudy, date(2026, 7, 19)).has_rain)
        self.assertEqual("heavy", summarize(rainy, date(2026, 7, 19)).intensity)

    def test_notice_text_matches_desktop_format(self):
        summary = summarize(
            [hour("2026-07-19T08:00+08:00", 1, 60, "小雨")],
            date(2026, 7, 19),
        )
        title, body, digest = notice_text(summary, True)
        self.assertEqual("今天有雨，记得带伞", title)
        self.assertEqual("08:00–09:00 有降雨（中雨）", body)
        self.assertEqual(64, len(digest))


if __name__ == "__main__":
    unittest.main()
