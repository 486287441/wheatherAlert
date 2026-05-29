# WeatherAlert（托盘降雨提醒）

Windows 托盘应用：定时查询和风天气，今天或明天有雨时切换托盘图标并记录通知历史。

## 功能

- **托盘右键**：立即检查 / 历史记录 / 切换城市 / 退出
- **托盘左键**：无操作
- **图标**：今天或明天有雨 → 雨伞；否则 → 铃铛
- **城市**：北京、上海、深圳、广州（可在菜单中切换）

## 环境要求

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## 配置 API 密钥

仓库中的 `appsettings.json` 仅含占位符，**请勿把真实 Key 提交到 Git**。

任选一种方式配置和风 API：

1. **本地文件（推荐）**  
   复制 `src/WeatherAlert.TrayPopup.App/appsettings.Local.json.example` 为同目录下的 `appsettings.Local.json`，填入 `ApiKey` 与专属 `ApiBaseUrl`。该文件已在 `.gitignore` 中忽略。

2. **环境变量**  
   ```powershell
   $env:WEATHER_ALERT_Weather__ApiKey = "你的密钥"
   ```

3. **直接改本地 `appsettings.json`**  
   仅用于本机调试；提交前请恢复为 `REPLACE_WITH_YOUR_API_KEY`。

在 [和风天气控制台](https://console.qweather.com/) 创建项目并获取 Key 与 API Host。

## 运行

**一键启动（推荐）**

| 方式 | 命令 |
|------|------|
| 资源管理器 | 双击 `start.bat` |
| PowerShell | `.\start.bat` 或 `.\start.ps1` |
| CMD | `start.bat` |

PowerShell 下必须加 `.\` 前缀，否则会报“无法识别 start.bat”。首次运行会自动编译 Release 版本。

立即执行一次检查：

```powershell
.\start.bat --check-now
# 或
.\start.ps1 --check-now
```

**开发调试（dotnet run）**

```powershell
cd d:\code\wheatherAlert
dotnet run --project src/WeatherAlert.TrayPopup.App
```

```powershell
dotnet run --project src/WeatherAlert.TrayPopup.App -- --check-now
```

## 测试

```powershell
dotnet test WeatherAlert.TrayPopup.sln
```

或运行仓库脚本：

```powershell
.\scripts\verify-all-modules.ps1
```

## 项目结构

| 路径 | 说明 |
|------|------|
| `src/WeatherAlert.TrayPopup.App` | 宿主：托盘、后台轮询、配置 |
| `src/WeatherAlert.TrayPopup.Core` | 领域模型与接口 |
| `src/WeatherAlert.TrayPopup.Infrastructure` | SQLite、和风 API 客户端 |
| `src/WeatherAlert.TrayPopup.Wpf` | 历史/城市选择等 WPF 窗口 |
| `tests/WeatherAlert.TrayPopup.Tests` | 单元测试 |
| `plan/` | 模块拆分与验收记录 |

## 日志与数据

- 日志：`logs/`（已忽略，不提交）
- 数据库：`data/weather-alert.db`（已忽略，不提交）
