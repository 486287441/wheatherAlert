import { invoke } from "@tauri-apps/api/core";
import type { AppSettings, City, DashboardData, HistoryEntry } from "../types";

const isTauri = () => "__TAURI_INTERNALS__" in window;

export async function getDashboard(): Promise<DashboardData> {
  if (!isTauri()) return demoDashboard;
  return invoke("get_dashboard");
}

export async function refreshWeather(): Promise<DashboardData> {
  if (!isTauri()) return { ...demoDashboard, updatedAt: new Date().toISOString() };
  return invoke("refresh_weather");
}

export async function getHistory(): Promise<HistoryEntry[]> {
  if (!isTauri()) return demoHistory;
  return invoke("get_history");
}

export async function clearHistory(): Promise<void> {
  if (!isTauri()) return;
  await invoke("clear_history");
}

export async function searchCities(keyword: string): Promise<City[]> {
  if (!isTauri()) return keyword ? demoCities : [];
  return invoke("search_cities", { keyword });
}

export async function locateCity(longitude: number, latitude: number): Promise<City> {
  if (!isTauri()) return demoCities[0];
  return invoke("locate_city", { longitude, latitude });
}

export async function selectCity(city: City): Promise<DashboardData> {
  if (!isTauri()) return { ...demoDashboard, cityCode: city.id, cityName: city.name };
  return invoke("select_city", { city });
}

export async function getSettings(): Promise<AppSettings> {
  if (!isTauri()) return demoSettings;
  return invoke("get_settings");
}

export async function saveSettings(settings: AppSettings): Promise<AppSettings> {
  if (!isTauri()) return settings;
  return invoke("save_settings", { settings });
}

export async function askWeatherAi(question: string): Promise<string> {
  if (!isTauri()) return `## 出行建议\n\n建议优先选择 **晴天或多云** 的日期：\n\n- 降水量较低\n- 气温更舒适\n- 出发前请再次刷新天气\n\n> 你问的是：“${question}”`;
  return invoke("ask_weather_ai", { question });
}

const at = (hours: number) => new Date(Date.now() + hours * 3_600_000).toISOString();
const atDay = (days: number) => {
  const value = new Date();
  value.setHours(12, 0, 0, 0);
  value.setDate(value.getDate() + days);
  return value.toISOString().slice(0, 10);
};
const conditions = ["晴", "多云", "多云", "小雨", "中雨", "阵雨", "阴", "多云"];
const icons = ["100", "101", "101", "305", "306", "300", "104", "101"];

const demoDashboard: DashboardData = {
  cityCode: "101020100", cityName: "上海", updatedAt: new Date().toISOString(), isCached: false,
  today: { date: new Date().toISOString(), hasRain: true, intensity: "moderate", ranges: [{ start: at(3), end: at(7) }] },
  tomorrow: { date: at(24), hasRain: false, intensity: "none", ranges: [] },
  hourly: Array.from({ length: 24 }, (_, i) => ({
    forecastTime: at(i), temperature: 27 - Math.round(Math.sin(i / 5) * 4),
    precipitationMm: i >= 3 && i <= 6 ? 2.4 : 0,
    precipitationProbability: i >= 3 && i <= 6 ? 75 : 12,
    conditionText: conditions[Math.floor(i / 3) % conditions.length], icon: icons[Math.floor(i / 3) % icons.length],
    windDirection: "东南风", windScale: "2", humidity: 68,
  })),
  daily: Array.from({ length: 30 }, (_, i) => {
    const condition = conditions[i % conditions.length];
    const rainy = /雨/.test(condition);
    return {
      forecastDate: atDay(i), temperatureMax: 31 - Math.round(Math.sin(i / 4) * 4),
      temperatureMin: 23 - Math.round(Math.sin(i / 4) * 3),
      conditionDay: condition, conditionNight: i % 3 === 0 ? "多云" : condition,
      iconDay: icons[i % icons.length], iconNight: icons[i % icons.length],
      precipitationMm: rainy ? 3.2 : 0, humidity: rainy ? 82 : 65,
      windDirectionDay: "东南风", windScaleDay: "2-3",
    };
  }),
};

const demoHistory: HistoryEntry[] = [
  { id: 1, createdAt: at(-2), type: "Rain", cityCode: "101020100", title: "今天有雨", body: "14:00–18:00 有降雨（中雨）" },
  { id: 2, createdAt: at(-26), type: "Rain", cityCode: "101020100", title: "明天有雨", body: "08:00–10:00 有降雨（小雨）" },
];
const demoCities: City[] = [
  { id: "101020100", name: "上海", province: "上海市", prefecture: "上海市" },
  { id: "101010100", name: "北京", province: "北京市", prefecture: "北京市" },
  { id: "101280601", name: "深圳", province: "广东省", prefecture: "深圳市" },
];
const demoSettings: AppSettings = {
  apiKey: "", apiBaseUrl: "https://your-api-host.qweatherapi.com", pollingMinutes: 60,
  autostart: false, notifications: true, currentCityCode: "101020100", currentCityName: "上海",
  aiApiKey: "",
};
