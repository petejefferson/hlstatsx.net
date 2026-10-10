# HLStatsX.NET

A .NET 10 rewrite of [HLStatsX Community Edition](https://github.com/NomisCZ/hlstatsx-community-edition) — a real-time player and clan statistics system for Half-Life engine games (Counter-Strike, Day of Defeat: Source, Team Fortress 2, and others).

## Project Status

| Component | Status | Notes |
|---|---|---|
| **HLStatsX.NET.Web** | ~97% feature parity | Production-ready. Full stats site with all public pages and admin panel. |
| **HLStatsX.NET.Awards** | 100% complete | Replaces `hlstats-awards.pl`. Nightly awards, ribbons, GeoIP, pruning. Production-ready. |
| **HLStatsX.NET.Daemon** | ~96% complete | Replaces `hlstats.pl`. UDP log processor and skill engine. **Work in progress — not yet hardened for production.** |

### Web Frontend (~97%)

All major public stat pages are implemented: player leaderboards (total, weekly, monthly, daily), player profiles, clan rankings, clan profiles, weapon stats, map stats, server detail with load graphs, awards, ribbons, ranks, country leaderboards, actions, roles, search, chat log, bans, and all 19 in-game pages.

The admin panel (~98%) covers full CRUD for all entity types (games, servers, weapons, actions, teams, roles, ranks, ribbons, awards, clans, clan tags, host groups, users), site options, player/clan edit tools, DB reset/optimize/copy, IP stats, VoiceComm server management, and daemon control.

> **Not implemented:** TeamSpeak/Ventrilo live user listings (legacy platforms) and VAC master sync (master servers are defunct).

### Daemon — Work in Progress

The daemon (`HLStatsX.NET.Daemon`) processes UDP game server log packets in real time — handling kills, deaths, connects, chat, skill calculation, clan tag matching, and GeoIP. It is functionally complete at ~96% but has not been through extended production soak testing.

The awards worker (`HLStatsX.NET.Awards`) is production-ready. It runs nightly: calculating daily and global award winners, assigning ribbons, marking inactive players, pruning old events, and updating GeoIP data.

If you are deploying today, you can run the web frontend and awards worker against an existing HLStatsX database that is still being fed by the original Perl daemon. The .NET daemon is optional — swap it in when you are ready.

---

## Requirements

- Docker and Docker Compose
- A MySQL / MariaDB database populated by an existing HLStatsX installation
- A reverse proxy (Nginx recommended) for TLS termination

---

## Deploying HLStatsX.NET.Web

### 1. Create a working directory and pull the config files

```bash
mkdir -p /opt/hlstatsx-net-web
cd /opt/hlstatsx-net-web
```

Copy `deploy/opt/hlstatsx-net-web/docker-compose.yml` from this repo into that directory.

### 2. Create a `.env` file

```bash
cp .env.example .env   # or create from scratch
```

Edit `.env` with your values:

```env
# MySQL connection string — must point at your existing HLStatsX database
HLSTATSX_CONNECTION_STRING=Server=localhost;Port=3306;Database=hlstatsx;User=hlstatsx;Password=changeme;CharSet=utf8mb4;

# The game code that the site defaults to when none is specified (e.g. dods, cstrike, tf)
HLSTATSX_DEFAULT_GAME=dods

# Display name shown in the site header and title bar
HLSTATSX_SITE_NAME=HLStatsX.NET

# URL path base — set this if the site is hosted under a sub-path (e.g. /stats)
# Leave blank or omit if serving from the root (/).
HLSTATSX_PATH_BASE=/stats
```

> If your MySQL server is running on the Docker host, use `host.docker.internal` as the server name — the compose file already adds the `extra_hosts` entry that maps it to the host gateway.

### 3. Start the container

```bash
docker compose up -d
```

The container listens on `127.0.0.1:8086` (host-bound, not public). Traffic reaches it via your reverse proxy.

### 4. Configure Nginx

Add a `location` block to your existing Nginx server config. A ready-to-use snippet is provided at `deploy/nginx/stats-location.conf`:

```nginx
location /stats/ {
    proxy_pass         http://127.0.0.1:8086;
    proxy_http_version 1.1;
    proxy_set_header   Host              $host;
    proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Proto $scheme;
    proxy_cache_bypass $http_upgrade;
}

# Static assets served without the /stats/ prefix
location /hlstatsimg/ { proxy_pass http://127.0.0.1:8086; ... }
location /css/        { proxy_pass http://127.0.0.1:8086; ... }
location /js/         { proxy_pass http://127.0.0.1:8086; ... }
```

See the full file at `deploy/nginx/stats-location.conf` for the complete block.

### 5. (Optional) Install as a systemd service

To have Docker Compose manage the container across reboots:

```bash
cp deploy/hlstatsx-net-web.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now hlstatsx-net-web
```

The service pulls the latest image on start and on `systemctl reload hlstatsx-net-web`.

---

## Configuration Reference

All settings can be passed as environment variables (using `__` as the section separator) or via `appsettings.json`.

| Setting | Env var | Default | Description |
|---|---|---|---|
| Connection string | `ConnectionStrings__HLStats` | — | MySQL connection string |
| `HLStatsX:DefaultGame` | `HLStatsX__DefaultGame` | `dods` | Default game code |
| `HLStatsX:SiteName` | `HLStatsX__SiteName` | `HLStatsX.NET` | Site display name |
| `HLStatsX:DefaultPageSize` | `HLStatsX__DefaultPageSize` | `50` | Rows per page on list pages |
| `HLStatsX:CommandTimeout` | `HLStatsX__CommandTimeout` | `120` | DB command timeout (seconds) |
| `HLStatsX:HideBotPlayers` | `HLStatsX__HideBotPlayers` | `false` | Exclude bots from leaderboards |
| `HLStatsX:PreviewMode` | `HLStatsX__PreviewMode` | `false` | Hides some sensitive data (for public demos) |
| `HLStatsX:Geo:GoogleMapsApiKey` | `HLStatsX__Geo__GoogleMapsApiKey` | *(empty)* | Google Maps API key — enables satellite map overlays on clan/country profiles (Leaflet/OSM used when absent) |
| `HLStatsX:Steam:ApiKey` | `HLStatsX__Steam__ApiKey` | *(empty)* | [Steam Web API key](https://steamcommunity.com/dev/apikey) — used for player avatars. Strongly recommended in production: without it the app scrapes steamcommunity.com, which rate-limits (HTTP 429) shared server IPs and leaves many avatars missing |
| `PathBase` | `PathBase` | *(empty)* | URL path base, e.g. `/stats` |

---

## Building from Source

```bash
git clone https://github.com/petejefferson/hlstatsx.net.git
cd hlstatsx.net

dotnet build
dotnet run --project src/HLStatsX.NET.Web

# Run tests (exclude DB-dependent repository tests)
dotnet test --filter "FullyQualifiedName!~RepositoryTests"
```

The app defaults to `https://localhost:5017` when run locally via `dotnet run` or Visual Studio.

---

## Project Structure

```
src/
  HLStatsX.NET.Core/           # Domain entities and interfaces
  HLStatsX.NET.Infrastructure/ # EF Core + MySQL repositories
  HLStatsX.NET.Web/            # ASP.NET Core MVC — the stats site
  HLStatsX.NET.Awards/         # Worker service: nightly awards, GeoIP, pruning
  HLStatsX.NET.Daemon/         # Worker service: real-time UDP log processor      [WIP]
tests/
  HLStatsX.NET.Tests/          # xUnit unit + integration tests (641 tests)
legacy/                        # NOT tracked - see "Legacy Reference Source" below
  php/                         # Original PHP source (authoritative reference)
  perl/scripts/                # Original Perl daemon (authoritative reference)
docker/                        # Dockerfile and local docker-compose
deploy/                        # Production deployment files (systemd, Nginx, compose)
```

---

## PHP Source Compatibility

HLStatsX.NET connects directly to an existing HLStatsX database — no schema changes required. It is a drop-in replacement for the PHP frontend. You can run it alongside the original PHP site against the same database while evaluating.

---

## Legacy Reference Source

The original PHP and Perl code is the authoritative spec for this rewrite, but it is **not committed to this repository** (`legacy/` is gitignored). To follow the "read the PHP first" workflow, fetch it from the upstream project (GPL-2.0):

```bash
git clone https://github.com/NomisCZ/hlstatsx-community-edition.git /tmp/hlstatsx-ce
mkdir -p legacy/perl
cp -r /tmp/hlstatsx-ce/web     legacy/php
cp -r /tmp/hlstatsx-ce/scripts legacy/perl/scripts
```

This gives you `legacy/php/pages/` (PHP pages) and `legacy/perl/scripts/` (Perl daemon). Nothing under `legacy/` should be committed.

---

## License


GPL-2.0-or-later — see [LICENSE](LICENSE).

Based on HLStatsX Community Edition and its predecessors (ELstatsNEO, HLstatsX, HLstats). See LICENSE for the full copyright chain.
