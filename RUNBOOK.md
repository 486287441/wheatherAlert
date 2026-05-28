# WeatherAlert 运行与回滚

## 运行

1. 修改 `src/WeatherAlert.App/appsettings.json`，填写 `Weather.ApiKey`。
2. 执行：
   - `dotnet run --project src/WeatherAlert.App`
3. 程序会最小化到托盘，菜单包含：
   - 立即检查
   - 查看历史通知
   - 切换城市

## 发布

执行：

- `powershell -ExecutionPolicy Bypass -File scripts/publish.ps1`

产物输出到 `artifacts/publish`。

## 回滚

1. 停止当前运行中的 `WeatherAlert.App` 进程。
2. 用上一个可用发布目录覆盖当前目录（保留配置与 `data/` 数据库）。
3. 重新启动可执行文件。
