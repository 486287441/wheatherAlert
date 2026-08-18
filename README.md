# WeatherAlert

一款使用 **Tauri 2 + React + Rust** 构建的 Windows 桌面降雨提醒。界面采用纯白液态玻璃风格，应用可以驻留系统托盘，定时查询和风天气，并在今天或明天有雨时发送系统通知。

## 功能

- 未来 72 小时天气与 16 小时可视化时间轴
- 今天/明天降雨识别、强度判断和连续时段合并
- 中国城市搜索与 Windows 当前位置识别
- SQLite 天气缓存、通知历史和降雨通知去重
- 后台定时轮询、系统托盘和登录后静默自启
- 本地 API 配置；密钥不会进入前端代码
- 无网络时继续显示最近一次缓存

## 环境要求

- Windows 10/11，系统包含 WebView2
- Node.js 20 或更高版本
- Rust stable（MSVC toolchain）
- Visual Studio C++ Build Tools

## 开发运行

```powershell
npm install
npm run tauri:dev
```

也可以双击 `start.bat`。只预览 React 界面时运行：

```powershell
npm run dev
```

浏览器预览使用演示数据；Tauri 窗口使用真实 Rust 后端。

## 配置天气服务

首次启动后进入 **偏好设置 → 天气数据**，填写和风天气控制台提供的：

- API Host，例如 `https://abcxyz.qweatherapi.com`
- API Key

保存后点击右上角刷新按钮。配置保存在当前用户的应用数据目录，不会写入仓库。开发模式首次启动时，如果检测到旧版 `src/WeatherAlert.TrayPopup.App/appsettings.Local.json`，会安全迁移其中的 API 配置。

## 开机自启

在应用的 **偏好设置 → 系统行为 → 开机自动启动** 中开启。登录 Windows 后应用会静默驻留托盘，不弹出主窗口，也不需要管理员权限。

仓库根目录的 `install-autostart.bat` / `uninstall-autostart.bat` 作为脚本备用入口。

## 构建安装包

```powershell
npm run tauri:build
```

产物位于 `src-tauri/target/release/bundle/`。Windows 默认生成 MSI 和 NSIS 安装包。

## 验证

```powershell
npm run build
cd src-tauri
cargo fmt --check
cargo clippy --all-targets -- -D warnings
cargo test
```

## 项目结构

| 路径 | 说明 |
|---|---|
| `src/` | React/TypeScript 界面 |
| `src-tauri/src/` | Rust 后端、天气业务、SQLite 与系统集成 |
| `src-tauri/icons/` | 桌面应用和安装包图标 |
| `src/WeatherAlert.*` | 旧版 C# 实现，迁移验证期保留 |
| `tests/` | 旧版 C# 回归测试 |

## 本地数据

Tauri 版数据保存在 Windows 当前用户应用数据目录下的 `com.weatheralert.desktop`：

- `settings.json`：API、城市和偏好设置
- `weather-alert.db`：天气缓存、通知记录和去重状态

真实密钥、数据库、构建产物均已由 `.gitignore` 排除。
