export type View = "weather" | "ai" | "history" | "settings";

export interface HourlyForecast {
  forecastTime: string;
  temperature: number;
  precipitationMm: number;
  precipitationProbability: number;
  conditionText: string;
  icon: string;
  windDirection: string;
  windScale: string;
  humidity: number;
}

export interface DailyForecast {
  forecastDate: string;
  temperatureMax: number;
  temperatureMin: number;
  conditionDay: string;
  conditionNight: string;
  iconDay: string;
  iconNight: string;
  precipitationMm: number;
  humidity: number;
  windDirectionDay: string;
  windScaleDay: string;
}

export interface RainRange { start: string; end: string }

export interface DaySummary {
  date: string;
  hasRain: boolean;
  intensity: "none" | "light" | "moderate" | "heavy";
  ranges: RainRange[];
}

export interface DashboardData {
  cityCode: string;
  cityName: string;
  updatedAt: string | null;
  isCached: boolean;
  today: DaySummary;
  tomorrow: DaySummary;
  hourly: HourlyForecast[];
  daily: DailyForecast[];
}

export interface HistoryEntry {
  id: number;
  createdAt: string;
  type: "Rain" | "Error" | string;
  cityCode: string;
  title: string;
  body: string;
}

export interface City { id: string; name: string; province: string; prefecture: string }

export interface AppSettings {
  apiKey: string;
  apiBaseUrl: string;
  pollingMinutes: number;
  autostart: boolean;
  notifications: boolean;
  currentCityCode: string;
  currentCityName: string;
  aiApiKey: string;
}
