param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "artifacts/publish"
)

$ErrorActionPreference = "Stop"
$project = "src/WeatherAlert.App/WeatherAlert.App.csproj"

dotnet publish $project `
  -c $Configuration `
  -r $Runtime `
  --self-contained false `
  -o $Output

Write-Host "Publish completed: $Output"
