# CS2Highlights — Build Timeline
> One step at a time. Complete a step → stop → wait for user review → move to next.
> Status: `[ ]` Not started | `[~]` In progress | `[x]` Done | `[!]` Blocked

---

## Step 0 — Project Prerequisites
**Status:** `[x]`

Set up everything needed before writing a single line of app code.

- [x] Create `.gitignore` (exclude `appsettings.json`, `bin/`, `obj/`, `*.user`, SQLite db file)
- [x] Create `appsettings.template.json` with placeholder values (HLAE path, FFmpeg path, Steam keys, demo/clip/cfg folders)
- [x] Confirm FFmpeg is installed — `C:\Users\Newgear\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe` (winget, v8.1.1)
- [x] Confirm HLAE path is `E:\Works\HLAE\HLAE.exe`
- [x] Update DESIGN.md paths from `C:\Program Files\HLAE` → `E:\Works\HLAE`

---

## Step 1 — Solution Scaffold
**Status:** `[x]`

Create the blank solution and all 6 projects with correct references. No logic yet — just structure.

Projects to create:
- `CS2Highlights.Core` — Class Library (net10.0)
- `CS2Highlights.Parser` — Class Library (net10.0) → refs Core
- `CS2Highlights.Renderer` — Class Library (net10.0) → refs Core
- `CS2Highlights.Steam` — Class Library (net10.0) → refs Core
- `CS2Highlights.Database` — Class Library (net10.0) → refs Core
- `CS2Highlights.WinForms` — Windows Forms App (net10.0-windows) → refs all above
- `CS2Highlights.Tests` — NUnit Test Project (net10.0) → refs Parser, Core

NuGet packages to install per project:
- Core: _(none)_
- Parser: `DemoFile`, `DemoFile.Game.Cs`
- Steam: `SteamKit2`
- Database: `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Tools`
- Renderer: `Serilog`, `Serilog.Sinks.File`
- WinForms: `Microsoft.Extensions.DependencyInjection`, `Serilog`, `Serilog.Sinks.File`
- Tests: `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`

**Acceptance criteria:** Solution builds with 0 errors. All projects appear in Solution Explorer.

---

## Step 2 — Core Models, Enums & Interfaces
**Status:** `[x]`

Populate `CS2Highlights.Core` with all data models, enums, and service interfaces. No implementation — only contracts.

Files to create in Core:
- `Models/Match.cs`
- `Models/Round.cs`
- `Models/PlayerEvent.cs` (base)
- `Models/KillEvent.cs`
- `Models/GrenadeEvent.cs`
- `Models/ClutchEvent.cs`
- `Models/DeathEvent.cs`
- `Models/Highlight.cs`
- `Models/ParsedMatch.cs` (returned by parser — holds all raw events)
- `Models/RenderJob.cs`
- `Models/RenderSettings.cs`
- `Models/RenderProgress.cs` (used by IProgress<T> reporting)
- `Models/DetectionOptions.cs` (user's toggle + threshold settings)
- `Enums/HighlightType.cs`
- `Enums/LowlightType.cs`
- `Enums/GrenadeType.cs`
- `Enums/ClutchResult.cs`
- `Enums/RenderStatus.cs`
- `Interfaces/IDemoParser.cs`
- `Interfaces/IHighlightDetector.cs`
- `Interfaces/IClipRenderer.cs`
- `Interfaces/ISteamService.cs`

**Acceptance criteria:** Solution builds with 0 errors. All interfaces are defined. No business logic yet.

---

## Step 3 — Database Schema & Migrations
**Status:** `[x]`

Set up `CS2Highlights.Database` with EF Core, define all tables, run the initial migration.

- [x] Create `AppDbContext.cs` with DbSets for all tables
- [x] Create entity classes (mapped to DB tables): `MatchEntity`, `RoundEntity`, `KillEventEntity`, `GrenadeEventEntity`, `HighlightEntity`, `RenderJobEntity`, `UserSettingEntity`
- [x] Add initial EF Core migration
- [x] Create `SettingsRepository.cs` — get/set key-value pairs from `UserSettings` table
- [ ] Verify SQLite file is created on first run (pending first app launch)

Tables (from DESIGN.md §4.5):
`Matches`, `Rounds`, `KillEvents`, `GrenadeEvents`, `Highlights`, `RenderJobs`, `UserSettings`

**Acceptance criteria:** Running the app (even as a stub) creates `cs2highlights.db` with all tables present. Verified in DB Browser for SQLite.

---

## Step 4 — Demo Scanner
**Status:** `[x]`

Implement `CS2Highlights.DemoScanner` — scan the demos folder and read lightweight info from demo headers.

- [x] `DemoFolderScanner.cs` — implements `IDemoScanner`, calls `Directory.GetFiles(folder, "*.dem")`, returns `List<DemoFileInfo>`
- [x] `LightweightDemoReader.cs` — opens a `.dem`, reads header only to extract: Match ID, map name, match date, all 10 players (SteamId + name). Stops reading after signon state via `OnCommandFinish` callback.
- [x] Added `DemoHeaderInfo` model and `ILightweightDemoReader` interface to Core
- Note: DB duplicate check (MatchId + SelectedPlayerSteamId) handled in Step 6 (Parser), not here

**Acceptance criteria:** Drop a `.dem` into the demos folder → app lists it → selecting it shows a player picker with all 10 players populated.

---

## Step 5 — WinForms Shell + Settings Panel
**Status:** `[x]`

Build the app skeleton — launch it, see a window, configure paths.

- [x] `Program.cs` — DI container setup, configure Serilog, launch `MainForm`
- [x] `MainForm.cs` — `TabControl` with tabs: Dashboard, Matches, Render, Clips, Settings
- [x] `SettingsPanel.cs` — form fields for: HLAE path, FFmpeg path, Demos folder, Clips folder, CFG folder
- [x] `PlayerPickerDialog.cs` — modal dialog showing a list of 10 players, returns selected `PlayerInfo`
- [x] Save/load all settings to `UserSettings` table via `SettingsRepository`
- [x] "Browse" buttons for file/folder path fields (`FolderBrowserDialog` / `OpenFileDialog`)
- [x] Updated `SettingsRepository` to use `IDbContextFactory` (no long-lived DbContext)

**Acceptance criteria:** App launches. You can fill in path settings and click Save. Values persist after restarting the app. Player picker dialog opens and returns a selection.

---

## Step 6 — Demo Parser
**Status:** `[x]`

Implement `CS2Highlights.Parser` — two-phase parsing via `IDemoParser`.

- [x] `DemoParser.cs` implements both interface methods:
  - `ReadPlayersAsync(demoPath)` — lightweight, reads header only, returns player list. Used to populate `PlayerPickerDialog`.
  - `ParseAsync(demoPath, selectedPlayer)` — full parse for the chosen player. Subscribes to game events, emits typed C# objects.
- [x] Full parse captures: kills (all players), deaths (selected player), grenade throws + damage (selected player), round start/end
- [x] Returns `ParsedMatch` with all events
- [x] Saves raw events to SQLite (`Matches`, `Rounds`, `KillEvents`, `GrenadeEvents` tables)
- [x] Duplicate guard: if (MatchId + SelectedPlayerSteamId) already in DB → skip, return existing data
- [x] `DemoParser` registered as singleton in DI container
- [x] 11 integration tests — all pass

Notes:
- `AllPlayers` is populated on live parse; returns `[]` on cache-hit (AllPlayers not stored in DB)
- Grenade damage for molotov/incendiary spans ticks — aggregated until round_end
- Flash grenade blinds: only counted if BlindDuration ≥ 0.5s (near-misses excluded)
- ClutchEvents left empty — computed by ClutchDetector in Step 7

**Acceptance criteria:** Select a demo → pick a player → full parse runs → DB populated with kills, rounds, grenade events. Picking the same demo + player again skips parsing. Verified in DB Browser.

---

## Step 7 — Highlight Detectors (Core Set)
**Status:** `[ ]`

Implement the two highest-value detectors first.

- [ ] `MultiKillDetector.cs` — group kills by (round, attacker), flag at threshold (3/4/5)
- [ ] `ClutchDetector.cs` — track alive counts per team per tick, detect 1vX situations, flag win/loss

Each detector:
- Takes `ParsedMatch` + `DetectionOptions`
- Returns `List<Highlight>`
- Saves results to `Highlights` table

**Acceptance criteria:** Running detectors on a parsed match produces at least one highlight entry in the DB (assuming the demo has any multi-kills or clutches).

---

## Step 8 — Matches Panel + Match Detail Panel
**Status:** `[ ]`

Build the UI to browse demos and see their detected highlights.

- [ ] `DashboardPanel.cs` — scans demos folder on load, lists `.dem` files in a `DataGridView` (filename, size, date). "Parse" button triggers lightweight scan → `PlayerPickerDialog` → full parse.
- [ ] `MatchesPanel.cs` — `DataGridView` listing all parsed demos from DB (map, date, selected player, parse status).
- [ ] `MatchDetailPanel.cs` — opens when a parsed demo is selected. Shows list of detected highlights/lowlights with type, round, description. "Add to Render Queue" button per highlight.

**Acceptance criteria:** Drop a demo → parse it → open match detail → see highlights listed. Parsing the same demo + player again skips to showing existing highlights.

---

## Step 9 — Remaining Detectors
**Status:** `[ ]`

Implement the rest of the highlight and lowlight detectors.

- [ ] `EntryFragDetector.cs` — first kill of round within N seconds of round start
- [ ] `DeathStreakDetector.cs` — N consecutive rounds where player died first
- [ ] `FriendlyFireDetector.cs` — teammate damage >= threshold in a single round
- [ ] `GrenadeDetector.cs` — TeamFlash, TeamMolotov, WastedGrenade
- [ ] `FailedClutchDetector.cs` — last alive 1v1, enemy HP < 50, round lost
- [ ] `BombDropDetector.cs` — carrying bomb, died before plant, team lost

**Acceptance criteria:** All detectors produce entries in the `Highlights` table. No crashes on edge cases (e.g. rounds with no kills).

---

## Step 10 — CFG Script Builder
**Status:** `[ ]`

Implement `CfgScriptBuilder.cs` in `CS2Highlights.Renderer`.

- [ ] Takes a `RenderJob` (demo path, tick start, tick end, player SteamId, `RenderSettings`)
- [ ] Generates a `.cfg` file with `mirv_streams` commands (see DESIGN.md §4.4 for example output)
- [ ] Saves `.cfg` to the configured CFG folder
- [ ] Handles pre/post buffer ticks (convert seconds → ticks at 64 tick rate)

**Acceptance criteria:** Given a `RenderJob`, produces a valid `.cfg` file in the CFG folder. Contents match the expected `mirv_streams` command structure.

---

## Step 11 — HLAE Renderer + FFmpeg Encoder
**Status:** `[ ]`

Implement `HlaeRenderer.cs` and `FfmpegEncoder.cs`.

- [ ] `HlaeRenderer.cs` — builds HLAE CLI arguments (`-csgo -steam -autoConfig ... -noGui -mmcfgEnabled true -mmcfg ...`), launches via `Process.Start`, waits for CS2 to exit, verifies output `.mp4` exists
- [ ] `FfmpegEncoder.cs` — configures `h264_nvenc` NVENC pipe args (`-c:v h264_nvenc -preset p4 -b:v 20M -pix_fmt yuv420p -vf scale=1920:1080`)
- [ ] Create `mirv_base.cfg` template (base HLAE config, written to CFG folder on first run)

**Acceptance criteria:** Triggering a render on a known highlight launches CS2+HLAE, plays back the demo, and produces an `.mp4` file in the clips folder.

---

## Step 12 — Render Queue + Render Panel
**Status:** `[ ]`

Wire up the render pipeline to the UI.

- [ ] `RenderQueue.cs` — holds list of `RenderJob`s, processes sequentially, accepts `IProgress<RenderProgress>`, updates `RenderJobs` table
- [ ] `RenderPanel.cs` — lists queued/active/done jobs, `ProgressBar` per job, log `ListBox` streaming HLAE stdout, "Add to Queue" button from match detail, "Cancel" button
- [ ] "Render" button on `MatchDetailPanel` → creates `RenderJob` → adds to queue

**Acceptance criteria:** Queue a render from match detail → watch progress bar fill → `.mp4` appears in clips folder.

---

## Step 13 — Clip Gallery Panel
**Status:** `[ ]`

- [ ] `ClipGalleryPanel.cs` — `ListView` of all `.mp4` files in clips folder (filename, size, date)
- [ ] "Open" button — opens the selected clip in the default media player
- [ ] "Open Folder" button — opens the clips folder in Windows Explorer
- [ ] Refresh button — rescans clips folder

**Acceptance criteria:** Finished clips appear in the gallery. Double-clicking opens the video.

---

## Step 14 — Detection Settings UI
**Status:** `[ ]`

Add per-rule toggle + threshold controls to `SettingsPanel`.

- [ ] One row per highlight/lowlight type (from DESIGN.md §5)
- [ ] Each row: `CheckBox` (enable/disable) + `TrackBar` or `NumericUpDown` (threshold, where applicable)
- [ ] All values saved to `UserSettings` table on change
- [ ] `DetectionOptions` loaded from DB and passed to detectors at parse time

**Acceptance criteria:** Toggle a rule off → re-parse → that highlight type no longer appears in results.

---

## Step 15 — Unit Tests
**Status:** `[ ]`

Write unit tests for all detector classes in `CS2Highlights.Tests`.

- [ ] `MultiKillDetectorTests.cs`
- [ ] `ClutchDetectorTests.cs`
- [ ] `EntryFragDetectorTests.cs`
- [ ] `DeathStreakDetectorTests.cs`
- [ ] `FriendlyFireDetectorTests.cs`
- [ ] `GrenadeDetectorTests.cs`
- [ ] `FailedClutchDetectorTests.cs`
- [ ] `BombDropDetectorTests.cs`

Each test class covers: normal detection, below-threshold (no highlight), edge cases (empty round, single player, etc.).

**Acceptance criteria:** All tests pass. `dotnet test` exits 0.

---

## Step 16 — Error Handling & Polish
**Status:** `[ ]`

Final hardening pass.

- [ ] HLAE crash recovery — detect non-zero exit code, mark `RenderJob` as Failed, log error, allow retry
- [ ] Demo download retry logic (network failure)
- [ ] Graceful handling of corrupted/incomplete `.dem` files
- [ ] User-facing error messages in WinForms (no raw exceptions shown to user)
- [ ] Serilog file logging wired up everywhere
- [ ] Final review of DESIGN.md + TIMELINE.md for accuracy

**Acceptance criteria:** App handles a simulated HLAE crash without freezing. Logs written to file. No unhandled exceptions reach the user.

---

*Last updated: Step 6 — done. Awaiting review before Step 7.*
