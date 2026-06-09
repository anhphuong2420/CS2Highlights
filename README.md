# CS2Highlights

A personal Windows desktop app that scans your CS2 demo files, detects highlight and lowlight moments, and renders them as video clips using HLAE and FFmpeg.

## What it does

1. Points at your local demos folder — scans all `.dem` files
2. You select a demo → a popup shows all 10 players → you pick who to analyze
3. The app parses the demo and detects moments like multi-kills, clutches, entry frags, death streaks, team flashes, etc.
4. You browse the detected highlights and pick which ones to render
5. The app launches CS2 via HLAE, plays back the demo at the right tick range, and encodes the output to `.mp4` via FFmpeg

Duplicate prevention is built in — the same demo + player combination is only ever parsed and rendered once.

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| Windows 10/11 | — | WinForms app, Windows only |
| .NET 10 SDK | 10.x | [Download](https://dotnet.microsoft.com/download) |
| CS2 | — | Must be installed via Steam |
| HLAE | Latest | [Download](https://www.advancedfx.org/download/) — install to `E:\Works\HLAE` or update path in settings |
| FFmpeg | 8.x+ | Install via `winget install Gyan.FFmpeg` |

---

## Setup

**1. Clone the repo**
```
git clone <repo-url>
cd CS2Highlights
```

**2. Copy the settings template**
```
copy appsettings.template.json CS2Highlights.WinForms\appsettings.json
```
Then open `appsettings.json` and update the paths to match your machine.

**3. Build**
```
dotnet build
```

**4. Run migrations** (creates the SQLite database)
```
dotnet ef migrations add InitialCreate --project CS2Highlights.Database --startup-project CS2Highlights.WinForms
dotnet ef database update --project CS2Highlights.Database --startup-project CS2Highlights.WinForms
```
> Skip the first command if the `Migrations/` folder already exists — just run `database update`.

**5. Run**
```
dotnet run --project CS2Highlights.WinForms
```

---

## Project structure

```
CS2Highlights.sln
├── CS2Highlights.Core          # Models, enums, interfaces — no dependencies
├── CS2Highlights.Database      # EF Core + SQLite — entities, migrations, settings repo
├── CS2Highlights.DemoScanner   # Folder scanner + lightweight demo header reader
├── CS2Highlights.Parser        # Full demo parser (DemoFile library)
├── CS2Highlights.Renderer      # HLAE launcher + FFmpeg encoder + CFG builder
├── CS2Highlights.WinForms      # Desktop UI — entry point
└── CS2Highlights.Tests         # NUnit tests
```

**Dependency flow:**
```
WinForms → DemoScanner, Parser, Renderer, Database, Core
Parser   → Core
Renderer → Core
Database → Core
DemoScanner → Core
Tests    → DemoScanner, Parser, Core
```

---

## Demo folder

Put your `.dem` files anywhere, then point the app at that folder in Settings. The app scans one level deep (no subfolders).

CS2 stores downloaded replays at:
```
%USERPROFILE%\Documents\CS2\replays\
```
Or for Steam library installs:
```
<SteamLibrary>\steamapps\common\Counter-Strike Global Offensive\game\csgo\replays\
```

---

## How match identity works

Demo filenames can be renamed without affecting the app. Match identity is based on a SHA-256 hash of the first 64 KB of the demo file content — stable, rename-proof, computed in milliseconds.

The database uses `(MatchId, SelectedPlayerSteamId)` as a unique key, so the same demo parsed for two different players creates two separate rows cleanly.

---

## Detected moments

| Type | Category |
|---|---|
| Triple / Quad / Ace kill | Highlight |
| Clutch win (1vX) | Highlight |
| Entry frag | Highlight |
| Clutch loss (1v1, enemy low HP) | Lowlight |
| Death streak | Lowlight |
| Team flash | Lowlight |
| Wasted grenade | Lowlight |
| Bomb drop death | Lowlight |
| Friendly fire | Lowlight |

All detection thresholds are configurable per-rule in the Settings panel.

---

## Tech stack

- .NET 10 / C# 13
- WinForms (desktop UI)
- EF Core 10 + SQLite (local database)
- [DemoFile.Game.Cs](https://www.nuget.org/packages/DemoFile.Game.Cs) v0.44.1 (demo parsing)
- HLAE (CS2 injection + frame capture)
- FFmpeg h264_nvenc (video encoding — requires NVIDIA GPU)
- Serilog (file logging)

---

## Build status

| Step | Status |
|---|---|
| Solution scaffold | Done |
| Core models & interfaces | Done |
| Database schema & migrations | Done |
| Demo folder scanner + lightweight reader | Done |
| WinForms shell + settings panel | — |
| Demo parser (full) | — |
| Highlight detectors | — |
| Render pipeline | — |
