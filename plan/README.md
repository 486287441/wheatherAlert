# WeatherAlert.TrayPopup 重写计划

在 `rewrite/WeatherAlert.TrayPopup` 内用 **C# + WPF** 重做托盘飞层：自绘圆角卡片、毛玻璃（WindowChrome + DWM BlurBehind）、点击托盘图标在右下角弹出、鼠标移开自动收起。

## 模块顺序

| 模块 | 文档 | 依赖 |
|------|------|------|
| M01 | [M01-骨架与测试管线.md](M01-骨架与测试管线.md) | — |
| M02 | [M02-屏幕定位.md](M02-屏幕定位.md) | M01 |
| M03 | [M03-飞层可见性状态机.md](M03-飞层可见性状态机.md) | M02 |
| M04 | [M04-毛玻璃圆角窗口.md](M04-毛玻璃圆角窗口.md) | M03 |
| M05 | [M05-托盘宿主与集成.md](M05-托盘宿主与集成.md) | M04 |
| M06 | [M06-业务迁移.md](M06-业务迁移.md) | M05 |

每模块完成前：**`dotnet test` 全绿** 且满足该模块验收标准，才进入下一模块。记录见 [验收记录.md](验收记录.md)。

## 一键验证

```powershell
cd rewrite/WeatherAlert.TrayPopup
dotnet test
powershell -ExecutionPolicy Bypass -File scripts/verify-all-modules.ps1
```
