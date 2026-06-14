# CS2 Highlights — Automated Highlight & Lowlight Recorder
### Technical Design Document | v1.0

---

## Purpose

A personal local tool that automatically fetches CS2 match demos via Steam, parses them to detect highlights and lowlights, and renders video clips using HLAE + FFmpeg — with zero manual input.

**Developer machine:** Intel i5-13400F | 32GB DDR4-3200 (dual channel) | RTX 2060 6GB | Windows 11

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Required Software](#2-required-software)
3. [Solution Architecture](#3-solution-architecture)
4. [Project Details](#4-project-details)
5. [Highlight & Lowlight Detection Rules](#5-highlight--lowlight-detection-rules)
6. [Render Options Panel](#6-render-options-panel)
7. [Recommended Build Order](#7-recommended-build-order)
8. [VAC Safety & Hardware Notes](#8-vac-safety--hardware-notes)
9. [Glossary](#9-glossary)

---

## 1. Project Overview

This is a **personal home-use application** — not intended for public deployment. It runs entirely on one Windows machine where CS2 is already installed. The user never touches HLAE manually; the app automates every step from demo download to finished `.mp4` clip.

### 1.1 End-to-End User Flow

```
1. Launch the WinForms desktop app
2. App scans the configured demos folder — lists all .dem files found
3. Select a demo from the list
4. App does a lightweight scan — reads match ID + player list from demo header
5. Player picker popup — choose which player to analyse (any of the 10 players)
6. App runs full parse for the selected player → saves events to SQLite
7. App shows detected highlights and lowlights with round details
8. Configure render options (resolution, FPS, buffers)
9. Click Render — CS2 opens silently, renders clips, closes
10. Browse finished .mp4 clips in the Clip Gallery
```

Steps 9 and 10 are fully automatic. The user just waits ~2 minutes.

> **How to get demo files:** In CS2, go to Watch → Your Matches → Download. Drop the `.dem` file into the configured demos folder.

### 1.2 Key Design Principles

- **Zero manual HLAE interaction** — fully automated via CLI arguments and `.cfg` scripts
- **Parse once, render many** — demo parsed once into SQLite; re-render with different settings anytime without re-parsing
- **Personal machine only** — no cloud, no auth server, no deployment concerns
- **User-configurable detection** — every highlight/lowlight rule has toggles and thresholds in the UI

---

## 2. Required Software

### 2.1 Development Tools

| Software | Version | Purpose | Where |
|---|---|---|---|
| Visual Studio 2026 Community | Latest | C# IDE | visualstudio.microsoft.com |
| .NET 10 SDK | 10.x | App runtime (bundled with VS) | dotnet.microsoft.com |
| Git | Latest | Version control | git-scm.com |
| DB Browser for SQLite | Latest | Inspect local database | sqlitebrowser.org |

### 2.2 Runtime Tools

| Software | Purpose | Notes |
|---|---|---|
| CS2 (Steam) | Plays back demo files for rendering | Already installed |
| HLAE | Injects into CS2, controls frame capture | advancedfx.org — free |
| FFmpeg | Encodes frames to .mp4 | Installed via `winget install Gyan.FFmpeg` |

### 2.3 NuGet Packages

| Package | Project | Purpose |
|---|---|---|
| `DemoFile` + `DemoFile.Game.Cs` | Parser | Parses CS2 `.dem` files — kills, ticks, rounds, player events |
| `Microsoft.Data.Sqlite` | Database | SQLite access |
| `Microsoft.EntityFrameworkCore.Sqlite` | Database | EF Core ORM for SQLite |
| `Microsoft.EntityFrameworkCore.Tools` | Database | EF Core CLI tools for running migrations |
| `Serilog` + `Serilog.Sinks.File` | Renderer, WinForms | Logging — critical for debugging HLAE/CS2 launch issues |
| `Microsoft.Extensions.DependencyInjection` | WinForms | DI container wired up in `Program.cs` — injects services into forms |

### 2.4 FFmpeg Setup

FFmpeg is installed via winget and is already on the system PATH:

```
winget install Gyan.FFmpeg

Installed path: C:\Users\Newgear\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe

Verify: open PowerShell and run:
  ffmpeg -version
```

The app reads the FFmpeg path from `appsettings.json` (`Paths.FfmpegExe`) and passes it explicitly to HLAE via the generated `.cfg` script — no manual PATH configuration needed.

---

## 3. Solution Architecture

### 3.1 Project Structure

```
CS2Highlights.slnx
├── CS2Highlights.Core          Business logic, models, interfaces — no UI dependencies
├── CS2Highlights.Parser        .dem file parsing + highlight/lowlight detection
├── CS2Highlights.Renderer      HLAE + FFmpeg orchestration
├── CS2Highlights.DemoScanner   Scans demos folder, reads lightweight demo info
├── CS2Highlights.Database      SQLite via EF Core
├── CS2Highlights.WinForms      .NET 10 Windows Forms desktop app
└── CS2Highlights.Tests         Unit tests for parsers and detectors
```

### 3.2 Project Dependency Diagram

```mermaid
graph TD
    Core["CS2Highlights.Core\n(models, enums, interfaces)"]

    Parser["CS2Highlights.Parser\n(DemoFile, DemoFile.Game.Cs)"]
    Renderer["CS2Highlights.Renderer\n(HLAE, FFmpeg)"]
    DemoScanner["CS2Highlights.DemoScanner\n(folder scan + demo header read)"]
    Database["CS2Highlights.Database\n(EF Core + SQLite)"]

    WinForms["CS2Highlights.WinForms\n(UI entry point)"]
    Tests["CS2Highlights.Tests\n(NUnit)"]

    Parser --> Core
    Renderer --> Core
    DemoScanner --> Core
    Database --> Core

    WinForms --> Core
    WinForms --> Parser
    WinForms --> Renderer
    WinForms --> DemoScanner
    WinForms --> Database

    Tests --> Core
    Tests --> Parser
```

### 3.3 Data Flow

```
[Demos Folder]  ←  user drops .dem files here manually
    │
    ▼
[DemoScanner]     →  lists .dem files  →  MatchesPanel (filename, size, date)
    │
    ▼  (user selects a demo)
[DemoParser.ReadPlayersAsync]  →  lightweight header read  →  player picker popup
    │
    ▼  (user picks a player)
[DemoParser.ParseAsync]   →  full parse  →  SQLite: Matches, Rounds, KillEvents, GrenadeEvents
    │  (checks MatchId + PlayerSteamId — skips if already parsed)
    ▼
[HighlightDetector]  →  applies rules  →  SQLite: Highlights (type, tick range)
    │
    ▼
[RenderOptionsPanel]  ←  user selects highlights + render settings
    │  (checks RenderJobs table — warns if clip already exists)
    ▼
[RenderQueue]     →  one RenderJob per clip  →  SQLite: RenderJobs (status tracking)
    │
    ▼
[CfgScriptBuilder]  →  generates .cfg script per clip
    │
    ▼
[HlaeRenderer]    →  launches CS2+HLAE  →  frames  →  FFmpeg  →  .mp4
    │  (updates RenderJob status to Done + saves clip path)
    ▼
[ClipGallery]     →  user browses finished .mp4 clips
```

---

## 4. Project Details

### 4.1 CS2Highlights.Core

#### Models

```
Match.cs          MatchId, SteamId, Map, Date, Score, DemoPath, ParsedAt
Round.cs          RoundNumber, TickStart, TickEnd, WinnerSide
PlayerEvent.cs    Base: Tick, SteamId, RoundNumber
KillEvent.cs      : PlayerEvent + Weapon, IsHeadshot, IsWallbang, IsNoscope, VictimSteamId
GrenadeEvent.cs   : PlayerEvent + GrenadeType, DamageToEnemies, DamageToTeam,
                    EnemiesBlinded, TeammatesBlinded
ClutchEvent.cs    : PlayerEvent + EnemyCount, Result (Win/Loss), TickResolved
DeathEvent.cs     : PlayerEvent + KillerSteamId, TimeIntoRound (seconds)
Highlight.cs      HighlightId, MatchId, Type, TickStart, TickEnd,
                  RoundNumber, Description, ClipPath (nullable)
```

#### Enums

```
HighlightType:   MultiKill3, MultiKill4, MultiKill5 (Ace), Clutch, EntryFrag,
                 Wallbang, HeadshotStreak, OuunumberedWin
LowlightType:    DeathStreak, FriendlyFire, FailedClutch, FirstBloodAgainst,
                 BombDropDeath, TeamFlash, TeamMolotov, WastedGrenade
GrenadeType:     HE, Molotov, Incendiary, Flash, Smoke, Decoy
ClutchResult:    Win, Loss
RenderStatus:    Queued, Rendering, Done, Failed
```

#### Interfaces

```csharp
IDemoParser          ParseAsync(string demoPath, string steamId) → ParsedMatch
IHighlightDetector   DetectAsync(ParsedMatch, DetectionOptions) → List<Highlight>
IClipRenderer        RenderAsync(RenderJob) → string clipPath
ISteamService        GetMatchesAsync(string steamId) → List<MatchInfo>
                     DownloadDemoAsync(MatchInfo) → string demoPath
```

---

### 4.2 CS2Highlights.Parser

#### DemoParser.cs

Wraps `DemoFile.Game.Cs`. Reads the `.dem` binary, subscribes to game events, and emits typed C# objects. Returns a `ParsedMatch` containing all raw events sorted by tick.

#### Detectors (one class per rule)

| Detector | Input | Logic | Output |
|---|---|---|---|
| `MultiKillDetector` | KillEvents per round | Group kills by (round, attacker). Count >= threshold. | MultiKill3/4/5 Highlight |
| `ClutchDetector` | Kill/Death events + alive counts | Track alive per team per tick. Flag when player is last alive 1vX. | Clutch Win or Loss Highlight |
| `EntryFragDetector` | KillEvents | First kill of round within N seconds of round start tick. | EntryFrag Highlight |
| `DeathStreakDetector` | DeathEvents | Count consecutive rounds where player died first (< 8s). Flag at N >= threshold. | DeathStreak Lowlight |
| `FriendlyFireDetector` | GrenadeEvents + DamageEvents | Teammate damage >= threshold in a single round. | FriendlyFire Lowlight |
| `GrenadeDetector` | GrenadeEvents | Flash blinded only teammates. Molotov damaged only team. Low dmg HE/molotov (user threshold). | TeamFlash / TeamMolotov / WastedGrenade Lowlight |
| `FailedClutchDetector` | ClutchEvents | Was last alive 1v1. Enemy HP < 50. Round lost. | FailedClutch Lowlight |

---

### 4.3 CS2Highlights.DemoScanner

No external API, no credentials. Reads the local filesystem and the demo file header only.

#### DemoFolderScanner.cs

Implements `IDemoScanner`. Calls `Directory.GetFiles(folder, "*.dem")` and returns a list of `DemoFileInfo` objects (file path, name, size, last modified). Used to populate the demo list in the UI.

#### LightweightDemoReader.cs

Opens a `.dem` file, reads just the header and the first few packets to extract:
- **Match ID** — unique identifier baked into the demo by Valve. Used as the primary key in the `Matches` table. Rename-proof.
- **Player list** — all 10 players with their SteamId and in-game name. Shown in the player picker popup.
- **Map name** and **match date**.

Closes the file without doing a full parse. Completes in under 2 seconds for any demo size.

---

### 4.4 CS2Highlights.Renderer

#### CfgScriptBuilder.cs

Generates a CS2 console script (`.cfg`) for each clip. Example output for one highlight:

```
// cs2highlights auto-generated — do not edit
mirv_streams record screen enabled 1
mirv_streams record fps 300
mirv_streams record name "C:\CS2Highlights\clips\clip_r5_multikill"

mirv_streams settings add ffmpeg cs2hl_enc "-c:v h264_nvenc -preset p4 -b:v 20M -pix_fmt yuv420p {QUOTE}{AFX_STREAM_PATH}\clip.mp4{QUOTE}"
mirv_streams settings edit afxDefault settings cs2hl_enc

mirv_cmd addAtTick 44480 "spec_lock_to_accountid 107535193; mirv_streams record start"
mirv_cmd addAtTick 46692 "mirv_streams record end; quit"

playdemo "C:\demos\match_abc123.dem"
demo_gototick 44380
```

Notes:
- `mirv_streams record name` sets the output folder; `{AFX_STREAM_PATH}` in the FFmpeg args expands to it
- `mirv_cmd addAtTick` schedules commands to fire when the demo reaches that tick (not immediately)
- `spec_lock_to_accountid` takes the 32-bit Steam AccountID (`SteamID64 - 76561197960265728`)
- `demo_gototick` at the end seeks to just before the clip start so the demo is ready when the first `mirv_cmd` fires
- FFmpeg is configured via `E:\Works\HLAE\ffmpeg\ffmpeg.ini` (Option B — path reference to winget FFmpeg)

#### HlaeRenderer.cs

Builds HLAE CLI arguments and launches the process via `Process.Start`. Waits for CS2 to exit. Checks output file exists before reporting success.

```
E:\Works\HLAE\HLAE.exe
  -csgo
  -steam
  -autoConfig "C:\CS2Highlights\cfg\job_001.cfg"
  -noGui
```

#### FfmpegEncoder.cs

Called by HLAE via `mirv_streams` pipe. Configured to use `h264_nvenc` (RTX 2060 NVENC) for fast encoding without competing with CS2's GPU rendering load.

```
Recommended FFmpeg args for RTX 2060:
  -c:v h264_nvenc -preset p4 -b:v 20M -pix_fmt yuv420p -vf scale=1920:1080

CS2 playback resolution : 1280x720  (saves VRAM during render)
Output resolution       : 1920x1080 (upscaled by FFmpeg after capture)
host_framerate          : 300
Expected VRAM usage     : ~3.5GB / 6GB
```

#### RenderQueue.cs

Holds a list of `RenderJob` objects. Processes them sequentially — one CS2 instance at a time. Each job contains: demo path, tick start, tick end, player SteamId, and `RenderSettings`. Reports progress back to the WinForms UI via `IProgress<RenderProgress>` on a `BackgroundWorker`.

---

### 4.5 CS2Highlights.Database

#### Tables

```
Matches          Id, MatchId*, DemoPath, DemoFileName, Map, Date,
                 SelectedPlayerSteamId, SelectedPlayerName, ParsedAt
                 * unique per (MatchId + SelectedPlayerSteamId) — same demo parsed
                   for different players creates separate rows

Rounds           Id, MatchId(FK), RoundNumber, TickStart, TickEnd, WinnerSide

KillEvents       Id, MatchId(FK), RoundId(FK), Tick, KillerSteamId, VictimSteamId,
                 Weapon, IsHeadshot, IsWallbang, IsNoscope

GrenadeEvents    Id, MatchId(FK), RoundId(FK), Tick, ThrowerSteamId, GrenadeType,
                 DmgToEnemies, DmgToTeam, EnemiesBlinded, TeammatesBlinded

Highlights       Id, MatchId(FK), RoundId(FK), HighlightType, LowlightType,
                 TickStart, TickEnd, Description, ClipPath, RenderStatus

RenderJobs       Id, HighlightId(FK), QueuedAt, StartedAt, FinishedAt,
                 Status, ClipPath, ErrorMessage
                 * used to prevent duplicate renders — if Status=Done and
                   ClipPath exists on disk, app warns before re-rendering

UserSettings     Key, Value   (HLAE path, FFmpeg path, folder paths, detection thresholds)
```

#### Entity Relationship Diagram

```mermaid
erDiagram
    Matches {
        int Id PK
        string MatchId "unique per player"
        string DemoPath
        string DemoFileName
        string Map
        datetime Date
        string SelectedPlayerSteamId "unique per player"
        string SelectedPlayerName
        datetime ParsedAt "nullable"
    }

    Rounds {
        int Id PK
        int MatchId FK
        int RoundNumber
        int TickStart
        int TickEnd
        string WinnerSide "CT or T"
    }

    KillEvents {
        int Id PK
        int MatchId FK
        int RoundId FK
        int Tick
        string KillerSteamId
        string VictimSteamId
        string Weapon
        bool IsHeadshot
        bool IsWallbang
        bool IsNoscope
    }

    GrenadeEvents {
        int Id PK
        int MatchId FK
        int RoundId FK
        int Tick
        string ThrowerSteamId
        string GrenadeType
        int DmgToEnemies
        int DmgToTeam
        int EnemiesBlinded
        int TeammatesBlinded
    }

    Highlights {
        int Id PK
        int MatchId FK
        int RoundId FK "nullable"
        string HighlightType "nullable"
        string LowlightType "nullable"
        int TickStart
        int TickEnd
        string Description
        string ClipPath "nullable"
        string RenderStatus
    }

    RenderJobs {
        int Id PK
        int HighlightId FK
        datetime QueuedAt
        datetime StartedAt "nullable"
        datetime FinishedAt "nullable"
        string Status
        string ClipPath "nullable"
        string ErrorMessage "nullable"
    }

    UserSettings {
        string Key PK
        string Value
    }

    Matches ||--o{ Rounds : "has"
    Matches ||--o{ KillEvents : "has"
    Matches ||--o{ GrenadeEvents : "has"
    Matches ||--o{ Highlights : "has"
    Rounds ||--o{ KillEvents : "belongs to"
    Rounds ||--o{ GrenadeEvents : "belongs to"
    Rounds |o--o{ Highlights : "belongs to"
    Highlights ||--o{ RenderJobs : "rendered by"
```

---

### 4.6 CS2Highlights.WinForms

Single executable desktop app. No web server, no browser required. All forms share a singleton `AppDbContext` and call Core/Steam/Renderer services directly.

#### Forms / Panels

```
MainForm          Tab container hosting all panels below
├── DashboardPanel    Demo folder list (DataGridView) + pending render jobs
├── MatchesPanel      Parsed demos — map, date, selected player, parse status
├── MatchDetailPanel  Highlights/lowlights list for one parsed demo
├── RenderPanel       Render options + live queue progress (ProgressBar + log ListBox)
├── ClipGalleryPanel  Finished .mp4 files — list view + "Open in Explorer" button
└── SettingsPanel     HLAE path, FFmpeg path, folder paths, detection thresholds

PlayerPickerDialog  Modal popup shown after lightweight scan — lists all 10 players
                    in the demo, user picks one, full parse begins
```

#### Detection Settings (SettingsPanel)

Each highlight/lowlight type from Section 5 gets one row: a `CheckBox` toggle and a `TrackBar` (or `NumericUpDown`) for its threshold. Values are persisted to the `UserSettings` table on change.

#### Render Progress

`RenderQueue` accepts an `IProgress<RenderProgress>` instance. The WinForms `RenderPanel` creates a `Progress<T>` that marshals updates back to the UI thread — no SignalR needed. A `ProgressBar` shows per-job completion; a `ListBox` streams log lines from HLAE stdout.

#### appsettings.json (copy from `appsettings.template.json` on first run)

No secrets — contains only local paths. Safe to keep private but nothing sensitive.

```json
{
  "Paths": {
    "HlaeExe": "E:\\Works\\HLAE\\HLAE.exe",
    "FfmpegExe": "C:\\Users\\Newgear\\AppData\\Local\\Microsoft\\WinGet\\Links\\ffmpeg.exe",
    "DemosFolder": "C:\\CS2Highlights\\demos",
    "ClipsFolder": "C:\\CS2Highlights\\clips",
    "CfgFolder":   "C:\\CS2Highlights\\cfg"
  },
  "Database": {
    "Path": "C:\\CS2Highlights\\cs2highlights.db"
  },
  "Logging": {
    "LogFolder": "C:\\CS2Highlights\\logs"
  }
}
```

---

## 5. Highlight & Lowlight Detection Rules

### 5.1 Highlights

| Type | Trigger Condition | Default Threshold | User Adjustable? |
|---|---|---|---|
| Multi-Kill (3K/4K/5K) | N kills by same player in one round | N >= 3 | Yes — minimum kills slider |
| Ace | 5 kills by same player in one round | Always 5 | No (fixed) |
| True Clutch | Last player alive on team + 1+ enemies + round WIN | Any 1vX | No |
| Outnumbered Win | At any point 1v2 or more during round + round WIN | 1v2 minimum | Yes |
| Entry Frag | First kill of round within N seconds of round start | < 8 seconds | Yes — time slider |
| Wallbang Kill | Kill with penetration flag set in demo | Any | Toggle on/off |
| Headshot Streak | N consecutive HS kills across rounds | 3 consecutive | Yes — count slider |

### 5.2 Lowlights

| Type | Trigger Condition | Default Threshold | User Adjustable? |
|---|---|---|---|
| Death Streak | Player died first in round N consecutive times | N >= 3 | Yes — count slider |
| Friendly Fire | Damage dealt to teammates in one round >= threshold | >= 40 HP | Yes — HP slider |
| Failed 1v1 Clutch | Last alive, 1v1, enemy HP < 50, round LOST | Enemy < 50 HP | Yes — HP slider |
| First Blood Against | Player died first < 8 sec into round, team lost round | < 8 seconds | Yes — time slider |
| Bomb Drop Death | Player carrying bomb, died before plant, team lost round | Any | Toggle on/off |
| Team Flash | Flash grenade blinded 2+ teammates, 0 enemies blinded | 2+ teammates | Yes — count slider |
| Team Molotov | Molotov/incendiary dealt damage only to teammates | Any amount | Toggle on/off |
| Wasted Grenade | HE/Molotov + 0 enemy damage + round lost + player died | Combined signal | Toggle on/off |
| Low Damage Grenade | HE or Molotov dealt < N damage to enemies | < 20 HP **(off by default)** | Yes — threshold + toggle |

### 5.3 What Cannot Be Detected

The following are **not detectable** from demo data and will not be implemented:

- Bad positioning (no positional intent data)
- Wrong utility usage timing (can see grenade thrown, not if it was correct)
- Passive holding vs. passive cowardice (indistinguishable)
- Poor communication (not in demo)
- Wrong weapon choice (subjective)

> The demo only records what happened — not intent or strategy.

---

## 6. Render Options Panel

All settings are saved to the `UserSettings` table in SQLite and persist between sessions.

### 6.1 Clip Settings

| Setting | Default | Range / Options |
|---|---|---|
| Buffer before event | 5 seconds | 1–15 seconds |
| Buffer after event | 3 seconds | 1–10 seconds |
| Output resolution | 1080p | 1080p / 720p |
| Output FPS | 60 | 60 / 120 |
| Camera perspective | First Person POV | First Person POV (Phase 1 only) |
| CS2 render resolution | 1280x720 | Internal — not user-facing |
| FFmpeg encoder | h264_nvenc | Auto-detected from GPU |

### 6.2 Toggle Reference

Every highlight and lowlight type in Section 5 has an individual toggle + threshold control in the UI. All default ON except **Low Damage Grenade** (default OFF — too many false positives without context).

---

## 7. Recommended Build Order

### Phase 1 — Foundation
- Core models, enums, interfaces
- Database schema + EF Core migrations
- `DemoFolderScanner` + `LightweightDemoReader` (scan folder, read header, player list)
- WinForms shell: `MainForm` with tab layout + `SettingsPanel` (paths only) + `PlayerPickerDialog`

### Phase 2 — The Brain
- `DemoParser` wrapping `DemoFile.Game.Cs` (full parse for selected player)
- `MultiKillDetector` + `ClutchDetector` (highest value, cleanest signals)
- `MatchesPanel` + `MatchDetailPanel` showing detected highlights in a `DataGridView`
- `EntryFragDetector` + `DeathStreakDetector` + `FriendlyFireDetector`

### Phase 3 — Rendering
- `CfgScriptBuilder` — generate `.cfg` scripts from highlight tick ranges
- `HlaeRenderer` — launch HLAE via `Process.Start`, wait for completion
- `FfmpegEncoder` — configure NVENC pipe
- `RenderQueue` + `IProgress<RenderProgress>` reporting to `RenderPanel` (`ProgressBar` + log `ListBox`)
- `ClipGalleryPanel` — lists finished `.mp4` files, "Open in Explorer" button

### Phase 4 — Polish
- Detection settings in `SettingsPanel`: `CheckBox` + `TrackBar` per rule, all toggles and thresholds
- `GrenadeDetector` (team flash, molotov, wasted grenade)
- `FailedClutchDetector` + `BombDropDetector`
- Unit tests for all detectors
- Error handling: HLAE crash recovery, retry logic

---

## 8. VAC Safety & Hardware Notes

### 8.1 VAC Ban Risk

**This app is safe.** VAC only scans processes when connecting to a VAC-protected server. This app never connects to a live server — it only plays back `.dem` files offline. HLAE injects only when launched through `HLAE.exe`. Launching CS2 normally through Steam means HLAE is completely dormant.

- Playing CS2 normally → launch via Steam → HLAE not involved at all ✅
- Rendering demos → app launches CS2 via HLAE → never connects to matchmaking ✅
- HLAE has been used for demo playback for 15+ years with Valve's knowledge ✅

### 8.2 Hardware Workload

| Component | Load During Render | Notes |
|---|---|---|
| GPU (RTX 2060) | 70–85% | CS2 rendering frames. NVENC on separate silicon — doesn't compete. |
| CPU (i5-13400F) | 1 P-core maxed (~4.6GHz) | HLAE single-thread RGB→BGR bottleneck. Other cores mostly idle. |
| RAM | 8–12 GB used | Frame buffer GPU→HLAE→FFmpeg. DDR4-3200 dual channel is ideal. |
| VRAM | ~3.5GB / 6GB | CS2 at 720p medium settings. Do NOT render at Ultra — VRAM overflow. |
| Thermals | Low — short bursts | 30-second clip renders in ~15 seconds. Not sustained load. |

### 8.3 Recommended Render Config

```
CS2 playback resolution : 1280x720
CS2 graphics settings   : Medium
host_framerate          : 300
FFmpeg encoder          : h264_nvenc  (NOT libx264 — use GPU not CPU)
Output resolution       : 1920x1080 (upscale post-capture)
Output bitrate          : 20 Mbps
Expected render speed   : ~1.5–2x realtime at 1080p/60fps
```

---

## 9. Glossary

| Term | Definition |
|---|---|
| `.dem file` | CS2 demo file — binary recording of a match containing all network events, player positions, kills, damage, grenades encoded as protobuf. |
| `HLAE` | Half-Life Advanced Effects. Free tool that injects into CS2 and exposes `mirv_streams` commands for programmatic frame capture. advancedfx.org |
| `mirv_streams` | HLAE command that controls the frame capture pipeline. Pipes raw frames directly to FFmpeg without saving to disk first. |
| `FFmpeg` | Open-source video encoder. Receives raw frames from HLAE via pipe, encodes to `.mp4` using `h264_nvenc` (GPU) or `libx264` (CPU). |
| `h264_nvenc` | NVIDIA GPU hardware video encoder. Runs on dedicated NVENC silicon — does not compete with CS2 rendering on CUDA cores. |
| `host_framerate` | CS2 console command that overrides game clock speed. Setting to 300 makes CS2 simulate time ~5x faster than real-time. |
| `Tick` | CS2 game unit of time. Matchmaking runs at 64 ticks/sec. Demo files record all events at tick resolution. |
| `Share Code` | Valve's encoded string (`CSGO-XXXXX-XXXXX-XXXXX-XXXXX-XXXXX`) that identifies a specific match and contains enough info to download its demo. |
| `DemoFile` / `DemoFile.Game.Cs` | Open-source C# library for parsing CS2 `.dem` files. Exposes typed C# events for kills, rounds, grenades, etc. |
| `SteamKit2` | Open-source C# library for communicating with Steam services including the Game Coordinator. |
| `Game Coordinator` | Valve's internal service that holds match metadata and demo download URLs. Accessed via SteamKit2. |
| `NVENC` | NVIDIA video encoding hardware built into the GPU. On RTX 2060 it runs independently of the 3D render pipeline. |
| `Clutch` | Being the last player alive on your team with 1+ enemies remaining. A true clutch = you win the round. |
| `ParsedMatch` | Internal C# object returned by DemoParser containing all raw events from one `.dem` file, sorted by tick. |
| `RenderJob` | One clip to render: demo path + tick start + tick end + player SteamId + RenderSettings. |
| `DetectionOptions` | User's toggle and threshold settings passed to HighlightDetector to filter which events become highlights. |

---

*CS2Highlights Design Document — Personal Use Only — v1.0*
