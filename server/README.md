# WeatherAlert Server

An independent, standard-library-only Python service that mirrors the desktop
rain detection and notification deduplication rules. It checks QWeather every
60 minutes and sends today/tomorrow rain notices to Bark.

The Windows desktop application does not call or depend on this service.

## Local verification

```powershell
python -m unittest discover server/tests -v
```

## Runtime configuration

Copy `.env.example` to `/etc/weather-alert-server.env`, fill the secrets, and
restrict it to root:

```sh
chmod 600 /etc/weather-alert-server.env
```

Run a weather check without notifications:

```sh
set -a
. /etc/weather-alert-server.env
set +a
python3 /opt/weather-alert-server/weather_alert.py --dry-run
```

The production deployment uses `weather-alert.timer` and stores deduplication
state in `/opt/weather-alert-server/state.db`.
