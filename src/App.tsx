import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import {
  Bell, Bot, Check, ChevronRight, CloudRain, Droplets, History, LoaderCircle,
  LocateFixed, MapPin, Navigation, RefreshCw, Search, Settings, Sparkles,
  Send, Trash2, Umbrella, Wind, X,
} from "lucide-react";
import * as api from "./lib/bridge";
import type { AppSettings, City, DailyForecast, DashboardData, HistoryEntry, HourlyForecast, View } from "./types";

const cn = (...values: Array<string | false | null | undefined>) => values.filter(Boolean).join(" ");
const time = (value: string) => new Intl.DateTimeFormat("zh-CN", { hour: "2-digit", minute: "2-digit", hour12: false }).format(new Date(value));
const day = (value: string) => new Intl.DateTimeFormat("zh-CN", { month: "long", day: "numeric", weekday: "short" }).format(new Date(value));
const relative = (value: string | null) => {
  if (!value) return "尚未更新";
  const mins = Math.max(0, Math.floor((Date.now() - new Date(value).getTime()) / 60_000));
  return mins < 1 ? "刚刚更新" : mins < 60 ? `${mins} 分钟前更新` : `${Math.floor(mins / 60)} 小时前更新`;
};

function WeatherGlyph({ item, size = "large" }: { item?: Pick<HourlyForecast, "conditionText">; size?: "small" | "large" }) {
  const text = item?.conditionText ?? "多云";
  const rainy = /雨|rain/i.test(text);
  const sunny = /晴|sun|clear/i.test(text);
  return (
    <span className={cn("weather-glyph", `weather-glyph--${size}`, rainy && "is-rain", sunny && "is-sun")} aria-label={text}>
      <span className="glyph-sun" />
      <span className="glyph-cloud" />
      {rainy && <span className="glyph-rain"><i /><i /><i /></span>}
    </span>
  );
}

function dailyLabel(value: string, index: number) {
  if (index === 0) return "今天";
  if (index === 1) return "明天";
  return new Intl.DateTimeFormat("zh-CN", { weekday: "short" }).format(new Date(`${value}T12:00:00`));
}

function dailyDate(value: string) {
  return new Intl.DateTimeFormat("zh-CN", { month: "numeric", day: "numeric" }).format(new Date(`${value}T12:00:00`));
}

function HorizontalScroller({ className, children }: { className: string; children: React.ReactNode }) {
  const ref = useRef<HTMLDivElement>(null);
  const drag = useRef({ active: false, startX: 0, startScrollLeft: 0 });
  const [dragging, setDragging] = useState(false);

  const onWheel = (event: React.WheelEvent<HTMLDivElement>) => {
    const element = ref.current;
    if (!element || element.scrollWidth <= element.clientWidth) return;
    const delta = Math.abs(event.deltaY) >= Math.abs(event.deltaX) ? event.deltaY : event.deltaX;
    const max = element.scrollWidth - element.clientWidth;
    const canMove = (delta < 0 && element.scrollLeft > 0) || (delta > 0 && element.scrollLeft < max);
    if (!canMove) return;
    element.scrollLeft += delta;
    event.preventDefault();
  };

  const onPointerDown = (event: React.PointerEvent<HTMLDivElement>) => {
    if (event.button !== 0) return;
    const element = ref.current;
    if (!element || element.scrollWidth <= element.clientWidth) return;
    drag.current = { active: true, startX: event.clientX, startScrollLeft: element.scrollLeft };
    element.setPointerCapture(event.pointerId);
    setDragging(true);
    event.preventDefault();
  };

  const onPointerMove = (event: React.PointerEvent<HTMLDivElement>) => {
    const element = ref.current;
    if (!element || !drag.current.active) return;
    element.scrollLeft = drag.current.startScrollLeft - (event.clientX - drag.current.startX);
    event.preventDefault();
  };

  const stopDragging = (event: React.PointerEvent<HTMLDivElement>) => {
    if (!drag.current.active) return;
    drag.current.active = false;
    if (ref.current?.hasPointerCapture(event.pointerId)) ref.current.releasePointerCapture(event.pointerId);
    setDragging(false);
  };

  return (
    <div
      ref={ref}
      className={cn(className, "horizontal-scroller", dragging && "is-dragging")}
      onWheel={onWheel}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={stopDragging}
      onPointerCancel={stopDragging}
      onLostPointerCapture={() => { drag.current.active = false; setDragging(false); }}
    >
      {children}
    </div>
  );
}

function Sidebar({ view, onChange }: { view: View; onChange: (view: View) => void }) {
  return (
    <aside className="sidebar">
      <div className="brand"><span className="brand-mark"><CloudRain size={20} /></span><span>Weather<span>Alert</span></span></div>
      <nav>
        <button className={cn(view === "weather" && "active")} onClick={() => onChange("weather")}><Sparkles size={19} /><span>天气概览</span></button>
        <button className={cn(view === "ai" && "active")} onClick={() => onChange("ai")}><Bot size={19} /><span>问 AI</span></button>
        <button className={cn(view === "history" && "active")} onClick={() => onChange("history")}><History size={19} /><span>提醒记录</span></button>
        <button className={cn(view === "settings" && "active")} onClick={() => onChange("settings")}><Settings size={19} /><span>偏好设置</span></button>
      </nav>
      <div className="sidebar-tip"><Umbrella size={19} /><div><strong>出门带伞</strong><span>让每一场雨都有准备</span></div></div>
    </aside>
  );
}

function Topbar({ data, refreshing, onRefresh, onCity }: { data: DashboardData | null; refreshing: boolean; onRefresh: () => void; onCity: () => void }) {
  return (
    <header className="topbar" data-tauri-drag-region>
      <button className="location-pill" onClick={onCity}><MapPin size={15} /><span>{data?.cityName ?? "选择城市"}</span><ChevronRight size={14} /></button>
      <div className="top-actions">
        <span className="updated"><i className="live-dot" />{data?.isCached ? "正在显示缓存" : relative(data?.updatedAt ?? null)}</span>
        <button className="icon-button" onClick={onRefresh} disabled={refreshing} title="立即更新"><RefreshCw size={17} className={cn(refreshing && "spin")} /></button>
      </div>
    </header>
  );
}

function HeroCard({ data }: { data: DashboardData }) {
  const current = data.hourly[0];
  const rain = data.today.hasRain ? data.today : data.tomorrow;
  const label = data.today.hasRain ? "今天" : data.tomorrow.hasRain ? "明天" : "未来两天";
  const ranges = rain.ranges.map(r => `${time(r.start)}–${time(r.end)}`).join("、");
  return (
    <section className="hero-card">
      <div className="aurora aurora-one" /><div className="aurora aurora-two" />
      <div className="hero-copy">
        <span className="eyebrow"><i /> {day(new Date().toISOString())}</span>
        <div className="temperature"><strong>{current?.temperature ?? "--"}</strong><sup>°</sup><span>{current?.conditionText ?? "等待更新"}</span></div>
        <div className={cn("rain-callout", rain.hasRain ? "warning" : "clear")}>
          {rain.hasRain ? <Umbrella size={22} /> : <Check size={22} />}
          <div><strong>{label}{rain.hasRain ? "有雨，记得带伞" : "暂无降雨"}</strong><span>{rain.hasRain ? `${ranges} · 降雨概率最高 ${Math.max(...data.hourly.map(x => x.precipitationProbability))}%` : "适合安排户外活动"}</span></div>
        </div>
      </div>
      <div className="hero-visual"><div className="weather-orbit" /><WeatherGlyph item={current} /><span className="visual-label">{data.cityName} · 实时天气</span></div>
      <div className="weather-stats">
        <div><Droplets size={18} /><span>湿度<strong>{current?.humidity ?? "--"}%</strong></span></div>
        <div><Wind size={18} /><span>{current?.windDirection || "风向"}<strong>{current?.windScale ? `${current.windScale} 级` : "--"}</strong></span></div>
        <div><CloudRain size={18} /><span>降雨概率<strong>{current?.precipitationProbability ?? 0}%</strong></span></div>
      </div>
    </section>
  );
}

function HourlyTimeline({ hourly }: { hourly: HourlyForecast[] }) {
  const items = hourly.slice(0, 16);
  const maxPop = Math.max(1, ...items.map(x => x.precipitationProbability));
  return (
    <section className="panel hourly-panel">
      <div className="section-heading"><div><span>逐小时预报</span><h2>接下来 16 小时</h2></div><span className="section-note">左右滚动查看更多</span></div>
      <HorizontalScroller className="hourly-scroll">
        {items.map((item, index) => (
          <article className={cn("hour-item", index === 0 && "now", item.precipitationProbability >= 40 && "rainy")} key={item.forecastTime}>
            <span className="hour-time">{index === 0 ? "现在" : time(item.forecastTime)}</span>
            <WeatherGlyph item={item} size="small" />
            <strong className="hour-temp">{item.temperature}°</strong>
            <span className="hour-condition">{item.conditionText}</span>
            <span className="pop"><Droplets size={11} />{item.precipitationProbability}%</span>
            <span className="rain-bar"><i style={{ height: `${Math.max(5, item.precipitationProbability / maxPop * 100)}%` }} /></span>
          </article>
        ))}
      </HorizontalScroller>
    </section>
  );
}

function DailyTimeline({ daily }: { daily: DailyForecast[] }) {
  return (
    <section className="panel daily-panel">
      <div className="section-heading">
        <div><span>逐日预报</span><h2>未来 30 天天气</h2></div>
        <span className="section-note">左右滚动查看每日天气</span>
      </div>
      <HorizontalScroller className="daily-scroll">
        {daily.map((item, index) => (
          <article className={cn("daily-item", index === 0 && "today", item.precipitationMm > 0 && "rainy")} key={item.forecastDate}>
            <span className="daily-weekday">{dailyLabel(item.forecastDate, index)}</span>
            <span className="daily-date">{dailyDate(item.forecastDate)}</span>
            <WeatherGlyph item={{ conditionText: item.conditionDay }} size="small" />
            <strong className="daily-temp"><b>{item.temperatureMax}°</b><span>{item.temperatureMin}°</span></strong>
            <span className="daily-condition" title={`${item.conditionDay} / ${item.conditionNight}`}>{item.conditionDay}</span>
            <span className={cn("daily-rain", item.precipitationMm > 0 && "has-rain")}><Droplets size={11} />{item.precipitationMm.toFixed(1)} mm</span>
          </article>
        ))}
      </HorizontalScroller>
    </section>
  );
}

function DayCard({ title, summary }: { title: string; summary: DashboardData["today"] }) {
  return (
    <article className={cn("day-card", summary.hasRain && "has-rain")}>
      <div><span>{title}</span><strong>{day(summary.date)}</strong></div>
      <span className="day-status">{summary.hasRain ? <><CloudRain size={18} />有降雨</> : <><Check size={18} />天气安稳</>}</span>
      <p>{summary.hasRain ? summary.ranges.map(r => `${time(r.start)}–${time(r.end)}`).join("、") : "暂未检测到明显降雨信号"}</p>
    </article>
  );
}

function WeatherView({ data }: { data: DashboardData }) {
  return <><HeroCard data={data} /><HourlyTimeline hourly={data.hourly} /><DailyTimeline daily={data.daily} /><section className="day-grid"><DayCard title="今天" summary={data.today} /><DayCard title="明天" summary={data.tomorrow} /></section></>;
}

function HistoryView({ entries, onClear }: { entries: HistoryEntry[]; onClear: () => void }) {
  return (
    <section className="page-section">
      <div className="page-title"><div><span className="eyebrow">NOTIFICATION LOG</span><h1>提醒记录</h1><p>回顾每一次天气变化，不错过重要信息。</p></div><button className="ghost-button" onClick={onClear} disabled={!entries.length}><Trash2 size={16} />清空记录</button></div>
      <div className="history-list">
        {!entries.length && <EmptyState icon={<Bell />} title="还没有提醒" detail="检测到降雨后，记录会出现在这里。" />}
        {entries.map((entry, i) => (
          <article className="history-row" key={entry.id} style={{ animationDelay: `${i * 40}ms` }}>
            <span className={cn("history-icon", entry.type === "Error" && "error")}>{entry.type === "Error" ? <X /> : <Umbrella />}</span>
            <div><span className="history-date">{new Intl.DateTimeFormat("zh-CN", { month: "long", day: "numeric", hour: "2-digit", minute: "2-digit" }).format(new Date(entry.createdAt))}</span><h3>{entry.title}</h3><p>{entry.body}</p></div>
            <span className="history-tag">{entry.type === "Error" ? "异常" : "降雨"}</span>
          </article>
        ))}
      </div>
    </section>
  );
}

function AiView({ cityName }: { cityName: string }) {
  const [messages, setMessages] = useState<Array<{ role: "user" | "assistant"; content: string }>>([
    { role: "assistant", content: `你好，我可以结合 ${cityName} 的逐小时和未来 30 天天气，帮你安排出游、穿衣或避雨。` },
  ]);
  const [question, setQuestion] = useState("");
  const [asking, setAsking] = useState(false);
  const suggestions = ["最近哪天最适合出去玩？", "这周哪天最适合户外运动？", "未来几天需要带伞吗？"];
  const send = async (value = question) => {
    const text = value.trim();
    if (!text || asking) return;
    setQuestion("");
    setMessages(current => [...current, { role: "user", content: text }]);
    setAsking(true);
    try {
      const answer = await api.askWeatherAi(text);
      setMessages(current => [...current, { role: "assistant", content: answer }]);
    } catch (error) {
      setMessages(current => [...current, { role: "assistant", content: `暂时无法回答：${String(error)}` }]);
    } finally {
      setAsking(false);
    }
  };
  return (
    <section className="ai-page">
      <div className="page-title"><div><span className="eyebrow">WEATHER ASSISTANT</span><h1>问 AI</h1><p>DeepSeek V4 Flash · 基于 {cityName} 当前天气数据回答</p></div></div>
      <div className="ai-shell">
        <div className="ai-messages">
          {messages.map((message, index) => (
            <article className={cn("ai-message", message.role)} key={`${message.role}-${index}`}>
              <span>{message.role === "assistant" ? <Bot size={17} /> : "你"}</span>
              {message.role === "assistant" ? (
                <div className="ai-markdown">
                  <ReactMarkdown
                    remarkPlugins={[remarkGfm]}
                    components={{ a: props => <a {...props} target="_blank" rel="noreferrer" /> }}
                  >
                    {message.content}
                  </ReactMarkdown>
                </div>
              ) : <p>{message.content}</p>}
            </article>
          ))}
          {asking && <article className="ai-message assistant"><span><Bot size={17} /></span><p className="ai-thinking"><i /><i /><i /></p></article>}
        </div>
        <div className="ai-suggestions">
          {suggestions.map(item => <button onClick={() => void send(item)} disabled={asking} key={item}>{item}</button>)}
        </div>
        <div className="ai-composer">
          <textarea value={question} onChange={event => setQuestion(event.target.value)} onKeyDown={event => {
            if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); void send(); }
          }} placeholder="问问未来天气，例如：周末适合去公园吗？" rows={2} />
          <button onClick={() => void send()} disabled={!question.trim() || asking} title="发送"><Send size={18} /></button>
        </div>
        <span className="ai-disclaimer">AI 建议仅供参考，临近出行请再次刷新天气。</span>
      </div>
    </section>
  );
}

function SettingsView({ settings, onSave }: { settings: AppSettings; onSave: (settings: AppSettings) => Promise<void> }) {
  const [draft, setDraft] = useState(settings);
  const [saving, setSaving] = useState(false);
  useEffect(() => setDraft(settings), [settings]);
  const save = async () => { setSaving(true); try { await onSave(draft); } finally { setSaving(false); } };
  return (
    <section className="page-section settings-page">
      <div className="page-title"><div><span className="eyebrow">PREFERENCES</span><h1>偏好设置</h1><p>调整数据源、检查频率和系统行为。</p></div></div>
      <div className="settings-grid">
        <section className="settings-card"><div className="settings-card-title"><span><Navigation size={18} /></span><div><h2>天气数据</h2><p>和风天气开发服务配置</p></div></div>
          <label><span>API Host</span><input value={draft.apiBaseUrl} onChange={e => setDraft({ ...draft, apiBaseUrl: e.target.value })} placeholder="https://xxx.qweatherapi.com" /></label>
          <label><span>API Key</span><input type="password" value={draft.apiKey} onChange={e => setDraft({ ...draft, apiKey: e.target.value })} placeholder="输入你的 API Key" /></label>
          <label><span>检查频率</span><select value={draft.pollingMinutes} onChange={e => setDraft({ ...draft, pollingMinutes: Number(e.target.value) })}><option value={15}>每 15 分钟</option><option value={30}>每 30 分钟</option><option value={60}>每小时</option><option value={120}>每 2 小时</option></select></label>
        </section>
        <section className="settings-card"><div className="settings-card-title"><span><Settings size={18} /></span><div><h2>系统行为</h2><p>WeatherAlert 如何在后台工作</p></div></div>
          <label><span>DeepSeek API Key</span><input type="password" value={draft.aiApiKey} onChange={e => setDraft({ ...draft, aiApiKey: e.target.value })} placeholder="用于“问 AI”功能" /></label>
          <Toggle label="开机自动启动" detail="登录 Windows 后静默驻留托盘" value={draft.autostart} onChange={autostart => setDraft({ ...draft, autostart })} />
          <Toggle label="降雨系统通知" detail="发现新的降雨时发送桌面通知" value={draft.notifications} onChange={notifications => setDraft({ ...draft, notifications })} />
        </section>
      </div>
      <div className="save-row"><span>配置保存在本机，不会上传到其他服务。</span><button className="primary-button" onClick={save} disabled={saving}>{saving ? <LoaderCircle className="spin" size={17} /> : <Check size={17} />}保存设置</button></div>
    </section>
  );
}

function Toggle({ label, detail, value, onChange }: { label: string; detail: string; value: boolean; onChange: (v: boolean) => void }) {
  return <button className="toggle-row" onClick={() => onChange(!value)}><span><strong>{label}</strong><small>{detail}</small></span><i className={cn("toggle", value && "on")}><b /></i></button>;
}

function EmptyState({ icon, title, detail }: { icon: React.ReactNode; title: string; detail: string }) {
  return <div className="empty-state"><span>{icon}</span><h3>{title}</h3><p>{detail}</p></div>;
}

function CityModal({ open, current, onClose, onSelect }: { open: boolean; current?: string; onClose: () => void; onSelect: (city: City) => void }) {
  const [query, setQuery] = useState(""); const [cities, setCities] = useState<City[]>([]); const [loading, setLoading] = useState(false); const [locating, setLocating] = useState(false); const [locationError, setLocationError] = useState("");
  useEffect(() => { if (!open) { setQuery(""); setCities([]); } }, [open]);
  useEffect(() => {
    if (query.trim().length < 1) return setCities([]);
    const timer = setTimeout(async () => { setLoading(true); try { setCities(await api.searchCities(query)); } finally { setLoading(false); } }, 280);
    return () => clearTimeout(timer);
  }, [query]);
  const locate = () => {
    setLocationError(""); setLocating(true);
    if (!navigator.geolocation) { setLocationError("当前系统不支持位置服务"); setLocating(false); return; }
    navigator.geolocation.getCurrentPosition(async position => {
      try { onSelect(await api.locateCity(position.coords.longitude, position.coords.latitude)); }
      catch (e) { setLocationError(String(e)); }
      finally { setLocating(false); }
    }, () => { setLocationError("无法获取位置，请检查 Windows 位置权限"); setLocating(false); }, { enableHighAccuracy: false, timeout: 10_000 });
  };
  if (!open) return null;
  return <div className="modal-backdrop" onMouseDown={onClose}><section className="city-modal" onMouseDown={e => e.stopPropagation()}><div className="modal-head"><div><span className="eyebrow">CHANGE LOCATION</span><h2>选择城市</h2></div><button className="icon-button" onClick={onClose}><X /></button></div><div className="search-box"><Search size={18} /><input autoFocus value={query} onChange={e => setQuery(e.target.value)} placeholder="搜索城市，例如：杭州" />{loading && <LoaderCircle className="spin" size={17} />}</div><button className="locate-button" onClick={locate} disabled={locating}>{locating ? <LoaderCircle className="spin" size={16} /> : <LocateFixed size={16} />}使用当前位置</button>{locationError && <p className="location-error">{locationError}</p>}<div className="city-results">{!query && <EmptyState icon={<LocateFixed />} title="搜索你的城市" detail="支持中国大陆城市名称查询" />}{query && !loading && !cities.length && <EmptyState icon={<MapPin />} title="没有找到" detail="换一个城市名称试试" />}{cities.map(city => <button onClick={() => onSelect(city)} key={city.id}><span className="city-pin"><MapPin size={17} /></span><span><strong>{city.name}</strong><small>{[city.prefecture, city.province].filter(Boolean).join(" · ")}</small></span>{city.id === current ? <Check className="selected-check" size={18} /> : <ChevronRight size={17} />}</button>)}</div></section></div>;
}

export default function App() {
  const [view, setView] = useState<View>("weather"); const [data, setData] = useState<DashboardData | null>(null);
  const [history, setHistory] = useState<HistoryEntry[]>([]); const [settings, setSettings] = useState<AppSettings | null>(null);
  const [refreshing, setRefreshing] = useState(false); const [cityOpen, setCityOpen] = useState(false); const [error, setError] = useState<string | null>(null);
  const load = useCallback(async () => { try { const [d, h, s] = await Promise.all([api.getDashboard(), api.getHistory(), api.getSettings()]); setData(d); setHistory(h); setSettings(s); } catch (e) { setError(String(e)); } }, []);
  useEffect(() => { void load(); }, [load]);
  const refresh = async () => { setRefreshing(true); setError(null); try { setData(await api.refreshWeather()); setHistory(await api.getHistory()); } catch (e) { setError(String(e)); } finally { setRefreshing(false); } };
  const select = async (city: City) => { setCityOpen(false); setRefreshing(true); try { setData(await api.selectCity(city)); setSettings(await api.getSettings()); } catch (e) { setError(String(e)); } finally { setRefreshing(false); } };
  const clear = async () => { await api.clearHistory(); setHistory([]); };
  const save = async (value: AppSettings) => { setSettings(await api.saveSettings(value)); };
  const content = useMemo(() => {
    if (!data || !settings) return <div className="loading-screen"><span className="brand-mark"><CloudRain /></span><LoaderCircle className="spin" /><p>正在唤醒天气服务…</p></div>;
    if (view === "history") return <HistoryView entries={history} onClear={clear} />;
    if (view === "ai") return <AiView cityName={data.cityName} />;
    if (view === "settings") return <SettingsView settings={settings} onSave={save} />;
    return <WeatherView data={data} />;
  }, [data, history, settings, view]);
  return <div className="app-shell"><Sidebar view={view} onChange={setView} /><main><Topbar data={data} refreshing={refreshing} onRefresh={refresh} onCity={() => setCityOpen(true)} />{error && <div className="error-banner"><X size={16} /><span>{error}</span><button onClick={() => setError(null)}>知道了</button></div>}<div className="content">{content}</div></main><CityModal open={cityOpen} current={data?.cityCode} onClose={() => setCityOpen(false)} onSelect={select} /></div>;
}
