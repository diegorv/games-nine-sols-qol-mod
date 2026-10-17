# Nine Sols QoL: handoff for a new session

Paste this whole file as the first message of a new session. It describes who I am, how I want to work, the
current state of the mod, what was verified in the game code, and the next goal.

---

## Who I am and how to work with me

- I'm Diego, a programmer with 10+ years of experience, but new to modding and to Unity.
- **Reply to me in Brazilian Portuguese.** Code, comments, docs and commit messages stay in English.
- Explain what changed and why, briefly.
- **Don't guess.** Before writing or changing a Harmony patch, read the real game code. Use sub-agents (Explore) to
  read the decompiled code in parallel, one focused question set each. They must cite file:line, quote the code,
  and mark as UNCONFIRMED anything that lives in scene, prefab or animation data. Then confirm yourself the few
  facts the design depends on.
- **One hypothesis at a time.** Change one thing, I test it in the game, you read the log.
- **Instrument before fixing.** Logs, the F9 dump and the post-respawn watchdog solved in minutes what days of
  guessing did not.
- I test in the game. You read `LogOutput.log` directly from the profile (path below).
- Don't commit unless I ask. I commit myself.

---

## The process that worked: repeat it for every feature

The previous session built the respawn feature in four stages (skeleton, ordinary deaths, boss logic in log-only
mode, boss reload) and every stage passed in one test round. Do the same discovery and delivery for each new
feature.

1. **Set up the session.** Decompile the game into the scratchpad (command below). Download the original mod's
   source into its own empty folder in the scratchpad (`curl` the raw files) and only read it.
2. **Discovery fan-out.** Launch 2 to 4 Explore sub-agents in parallel, in the background, each with a focused,
   numbered question list about one area (e.g. the patch targets of the original mod; the game flow around them;
   resets and lifecycle; UI/config). Use the template below. While they run, do only work that does not overlap
   (check the profile, the log, the original mod's code). If the scope changes, send the running agent a message
   instead of starting over.
3. **Synthesize for me** in Portuguese: what is confirmed (with file names), what is UNCONFIRMED (scene/prefab
   data), and what changes the design. Point out bugs or risks found in the original mod.
4. **Verify yourself** every signature, accessibility (public/private/protected), caller and field the design
   depends on (a quick grep in the decompiled code) before writing code.
5. **Propose the plan** (files, patches, config, test steps) and wait for my OK.
6. **Implement in stages, one hypothesis each**, building after each one (the build hot-reloads the mod):
   - first instrumentation and anything that does not change behaviour (config, dump lines, or patches that only
     log what they would do);
   - then the behaviour change;
   - after every build, check the log for the hot reload (`unloaded` → `Registered ... in RCGLifeCycle` →
     `loaded`) and for exceptions before telling me to test.
7. **When the answer lives in game data** (prefabs, scenes, animations), add a diagnostic (an F9 dump section, a
   watchdog field) and ask me for one concrete action ("die once and press F9 standing on the bag"). Then read the
   log and decide. Remove one-off diagnostics once they answered their question.
8. **Tell me exactly what to test**, in a short numbered list, and what the log should show.
9. Keep the README (features table and credits) up to date and summarize what changed in a few lines.

Sub-agent prompt template (this structure produced reports that were precise enough to design from):

```
You are investigating the decompiled C# source of the Unity game Nine Sols (Mono) to support a BepInEx 5 +
Harmony mod. Thoroughness: very thorough.

RULES
- Do not guess. Every claim must cite file path + line numbers and quote the relevant lines.
- If something cannot be confirmed in code (scene, prefab, animation or asset data), mark it UNCONFIRMED and say why.
- Read-only: do not modify anything. Dumps only under <scratchpad>\agent-<topic>\.
- Final report: answer each numbered question in order, concisely, then list surprises relevant to the mod.

WHERE THE CODE IS
- <scratchpad>\decomp\Assembly-CSharp\, \rcg.rcgmakercore.Runtime\, \RCG_General\ (one file per type; grep the whole folder)
- Game DLLs: J:\SteamLibrary\steamapps\common\Nine Sols\NineSols_Data\Managed
- For compiler-generated code: dnSpy.Console.exe --no-tokens --no-color --show-all -t TypeName <dll> (or -l IL)

CONTEXT
<what the feature must do, what the original mod patches, what is already known and needs no re-proof>

QUESTIONS
1. ...
```

---

## Environment (Windows)

| Item | Path / value |
|---|---|
| Game | `J:\SteamLibrary\steamapps\common\Nine Sols` |
| Game DLLs | `J:\SteamLibrary\steamapps\common\Nine Sols\NineSols_Data\Managed` |
| Repo | `C:\c\dev\NineSols-ExampleMod` (folder name is still the template's; rename later, outside VS Code) |
| Project | `NineSolsQoL.sln`, `Source\NineSolsQoL.csproj` |
| Mod manager | r2modman, profile **Default** |
| Profile | `%AppData%\r2modmanPlus-local\NineSols\profiles\Default` |
| BepInEx log | `<profile>\BepInEx\LogOutput.log` |
| Our config | `<profile>\BepInEx\config\diegorv.NineSolsQoL.cfg` (an old `diegorv.RespawnNearDeath.cfg` may still be there; harmless) |
| Tools | .NET SDK 10, Git, VS Code + C# Dev Kit, `C:\Users\diegorv\Downloads\dnSpy-net-win64\dnSpy.Console.exe` |
| Build | `dotnet build Source/NineSolsQoL.csproj` (Ctrl+Shift+B in VS Code) |
| CI | `.github/workflows/build.yml` builds against `nine-sols-modding/libs-stripped` (it includes `UniTask.dll`) |

Installed in the profile: BepInExPack 5.4.23, NineSolsAPI, ScriptEngine, ConfigurationManager, UnityExplorer, and
other mods I toggle in r2modman (HPNumbers, XPNumber, JinMultiplier, FarawayPickup, NoDeathPenalty; Ixrec's
TeleportFromAnywhere is installed but disabled).

### Dev loop

- The build copies the DLL to `<profile>\BepInEx\scripts` (`CopyDir` in the csproj).
- ScriptEngine config: `LoadOnStart = false` (loading at startup fails in Nine Sols: ScriptEngine adds the
  component a frame later and the object is already destroyed), `EnableFileSystemWatcher = true`,
  `AutoReloadDelay = 1`. I start the game from r2modman, load a save and press **F6 once**; after that every build
  hot-reloads the mod (`OnDestroy` calls `UnpatchSelf`).
- Never have the DLL in both `scripts` and `plugins`, or it loads twice.
- If you rename or delete files that may be open in VS Code, tell me to close those tabs without saving: an old tab
  once overwrote a new file.
- **F8 opens one of the game's own debug screens.** Our dump key is F9.
- My BepInEx console is on (`[Logging.Console] Enabled = true`) and the game itself spams the warning
  `Adding null Transform to TransformAccessArray` (thousands of lines, vanilla). With chatty mods this is the prime
  suspect for stutters.

### Decompiling the game (do this first, the output is per session)

Export the game assemblies as C# into the session scratchpad (took about 14 s):

```sh
dnSpy.Console.exe --no-sln --no-resources --no-tokens --no-color --spaces 4 --asm-path "<Managed>" -o "<scratchpad>/decomp" "<Managed>/Assembly-CSharp.dll" "<Managed>/rcg.rcgmakercore.Runtime.dll" "<Managed>/RCG_General.dll"
```

One file per type, flat folders (namespaced types in subfolders). Async lambdas and other compiler-generated code
only show up with `--show-all` or `-l IL`; `-t TypeName` prints one type to stdout. Line numbers below come from
this output.

---

## Current state of the code (NineSolsQoL 0.1.0)

The mod started as the template's ExampleMod, became RespawnNearDeath and was just renamed to **NineSolsQoL**
(GUID `diegorv.NineSolsQoL`). The rename and the move into `Core/` + `Features/Respawn/` compile, and the game
hot-loaded it fine, but **it still needs an in-game smoke test** (one ordinary death and one Yingzhao death) before
adding features.

```
Source/
  Plugin.cs                  entry point: each feature's Init, RCGLifeCycle registration, Harmony
  Log.cs                     logging (Log.Debug only when DebugLogging is on, written at Info level)
  Core/
    GameAccess.cs            safe access to the game singletons, boss fight detection
    GamePrivates.cs          the private game fields the mod reads or writes (lazy FieldRefAccess)
    UnityExtensions.cs       HierarchyPath, Bounds.Describe
    DebugTools.cs            [Debug] config, F9 dump (+ AddDumpSection), watchdog, [hitch] frame logger
  Features/
    Respawn/
      RespawnFeature.cs      [Respawn] config and dump section
      RespawnPatches.cs      every Harmony patch of the feature; they only forward
      DeathSnapshot.cs       what the mod needs to know about a death, recorded when it happens
      RespawnController.cs   decides: the game's normal respawn or the mod's
      Respawner.cs           quick revive in the same scene / scene reload for bosses
      SafeTrail.cs           level-relative trail of the player's safe ground in the current scene
      ArenaTracker.cs        fight entrances and boss areas, respawn point for a boss fight
```

Conventions (keep them):

- A feature lives in `Features/<Name>/`: `<Name>Feature.Init(ConfigFile)` binds its config section and registers
  its dump lines with `DebugTools.AddDumpSection`; `<Name>Patches` holds only Harmony patches that forward to the
  feature's code. `Plugin.Awake` calls each `Init`.
- Private game fields only through `Core/GamePrivates`, looked up lazily: a missing field is logged once and only
  that feature stops. (An exception in a static initializer used by a patch would break the game's own code.)
- Never read `X.Instance` of a `SingletonBehaviour` without `X.IsAvailable()` first: `Instance` creates a new
  empty object when there is none (title screen, loading).
- Every method called from a patch catches its exceptions. An exception in a prefix aborts the game's method
  (death handling, fight start...).
- The Harmony analyzer treats every static method of a `[HarmonyPatch]` class as a patch (warning Harmony003):
  keep helpers out of patch classes.
- `DebugLogging` defaults to true while developing; `Log.Debug` for diagnostics, no per-event `Log.Info` spam.

Config today: `[Respawn] Enabled, BossRespawnOutsideArena`; `[Debug] DebugLogging, DumpKey = F9`.

---

## Respawn feature: done and validated in the game

Goals: ordinary deaths respawn on the last safe ground before the death, in the same scene. Boss deaths reload the
scene (so the boss really resets) and put the player just outside the arena, before the fight entrance, free to
re-enter or leave. The death penalty stays vanilla (no-penalty mods compose with it).

How it works:

1. **Prefix `PlayerDeadState.OnStateEnter`** → `DeathSnapshot.Record()`: position, `lastSafeGroundPosition`, level,
   and whether a boss fight is on (`monsterHpUI.CurrentBossHP != null` or
   `MonsterManager.QueryEngagingMonsters(m => m.CompareTag("Boss"))`, the game's own check). It must be a prefix:
   `NormalDeadHandle` clears the boss bars and the engaged monsters right after.
2. **Prefix on the private async `PlayerDeadState.Revive`** (patched by name) → `RespawnController.TryRespawn`;
   returning false skips the game's revive. The game's respawn still runs when: config off,
   `DebugSetting.IsDebugMode`, maps with native `CanQuickRevive()`, VR challenge, memory mode, leaderboard
   challenge, a pending tutorial respawn, core state not Playing, or the level changed since the death.
3. **Ordinary death** → `Respawner.QuickReviveAt`: the vanilla quick revive branch (`PlayerDeadState.cs` 104-115)
   **without `FetchReviveData`**:
   - `if (player.IsNeedDeathPenalty) playerGameData.PlayerDeathPenalty()` first. Vanilla quick revive never
     applies the penalty (only the next scene load does), which duplicates Jin and then wipes everything carried.
     Calling the game's own method keeps no-penalty mods (which transpile it) working.
   - `FadeToBlack(0.2)` → `ResetLevel(false)` (monsters and level back; `GameLevel.EnterLevelReset` →
     `CheckDeadRecord` spawns the bag; `SafeTrail` paused) → `RestoreEverything` → `ChangeState(Normal)` →
     `SetPosition(pos, true)` → `ForceOnGround` → **`SafeGroundRecorder.ForceSave(pos)`** → `IsJustDead = false`
     → `UpdateCameraBounds` + `CameraManager.ResetCamera2DDockerToPlayer` → BGM back → `FadeOutBlack`.
   - `ForceSave` matters: the player is under the ResetManager object (`GameCore(Clone)/RCG LifeCycle`, verified in
     game), so `ResetLevel` sets the last safe ground to the death spot; without it the next death respawns in the
     same pit (observed and fixed).
   - On exception it falls back to `RespawnAtSavePoint`.
4. **Boss death** → `Respawner.ReloadSceneAt`, like `GameCore.ReloadScene`: `ChangeSceneData { sceneName =
   current, playerSpawnPosition = level-relative point, ChangedDoneEvent = AllFull, panData NoPan }`, clear
   `GameCore._lastTeleportPoint`, `ChangeScene(...).Forget()`. The scene load applies the penalty and spawns the bag.
   - Respawn point: `SafeTrail` records every write to the player's `SafeGroundRecorder.LastSafeGroundPosition`
     (level-relative, restarted on each scene load, `StartsAtEntrance`). When `PlayerWalkToTrigger.Triggered`
     fires for the first time (`_triggered` is private), `ArenaTracker` picks the newest trail point outside the
     trigger's colliders plus a 64-unit margin. Fallbacks: newest point outside active `BossArea` bounds, then the
     scene entrance, else the game's respawn.
   - Validated on Yingzhao (`A2_S5_BossHorseman_Final`, `Boss_SpearHorse_Logic`): trigger x -4690..-4590, walkTo
     -4568, respawn point ≈ -4764. Six reloads in a row, deaths on both sides of the arena, walking away, leaving the
     scene and coming back: no errors, player visible and controllable every time. (Surviving a trip back to the
     title screen was verified separately, with the plugin registered in `RCGLifeCycle`.)
5. **Why not patch `CanQuickRevive`/`FetchReviveData`** (the first attempt did): `FetchReviveData` sets
   `GameCore._lastTeleportPoint`. At the end of the next scene load, `InitializeGameLevel` invokes
   `_lastTeleportPoint.OnSceneChangeDone`, which `SavePoint.EnterLevelAwake` fills with
   `StartPlayerReviveAnimation` (votes Cutscene input, `SetPlayerView(false)`). Only an animation event calls
   `ReturnPlayerControl`, and it never fires when the Root Node is off screen: the player stays invisible and
   without control. A quick revive also leaves the field set until the next door into the Root Node's scene.

---

## Verified game facts worth keeping

From the decompiled code unless marked otherwise (file names are the dnSpy output).

- `GameCore.FetchReviveData`: debug branch; force-save branch (`SaveManager.forceSaveFlag`, set by
  `AutoSave(CurrentSceneAndPos)` after boss kills, item gets, cutscenes; it does NOT set `_lastTeleportPoint`);
  otherwise `respawnData = lastSaveTeleportPoint.ChangeSceneData` and `_lastTeleportPoint = lastSaveTeleportPoint`.
- `GameCore.InitializeGameLevel` (ChangingScene path): `SetPosition(spawn)` (~654) → `ChangedDoneEvent` →
  penalty if `IsNeedDeathPenalty` (689-693) → `resetManager.GameLevelStart` (710, runs ILevelAwake such as
  `SavePoint.EnterLevelAwake`) → `_lastTeleportPoint.OnSceneChangeDone` (717-739) → fade-out callback
  (`<InitializeGameLevel>b__4`, only via `--show-all`): revokes only the input's and GameCore's votes, sets
  `currentCoreState = Playing`, `freeze = false`.
- `ChangeSceneData.changeSceneMode` and `panData` are write-only.
- `PlayerDeadRecord.StorePlayerDataBeforeDead`: bag = carried Jin/XP × `Player.statData.DeathPenaltyRate.Value`
  (value is asset data; our death log line prints it as "death bag rate"); drop point = `lastSafeGroundPosition`;
  a killer monster that "can carry" the bag gets a murderer tag instead. Dying again before pickup overwrites the
  record. **The death bag is picked up by interaction, not by touch** (verified in game: `DropItem.
  IsTouchPlayerToPick = false`, `Interactable_Pick` → FSM → `PlayerRestoreDeathPenaltyAction`).
- `PlayerGamePlayData.PlayerDeathPenalty`: `async void`; after a 100 ms delay: `deathCount++`, `CurrentGold = 0`,
  `CurrentExp = 0` (IL_00D9.. `ldloc.1; ldc.i4.0; call set_CurrentGold`), `AutoSave(FlagOnly)`,
  `Player.i.ResetDeathPenalty()`. Callers: `InitializeGameLevel`, `DebugFastRevive`.
- `PlayerGamePlayData.AddGold(int amount, GoldSourceTag sourceTag)`; tags: Monster, Chest, Sell, DeathPenalty,
  DevCheat, FooExplode. Returns early in boss memory mode. Bag pickup uses `DeathPenalty`.
- `SafeGroundRecorder` (player: `PPlayer/RotateProxy/SpriteHolder/Safe Ground Recorder`): records in `LateUpdate`
  when on ground, not in a `DangerZoneNoSafeGround`, no wall within 2 tiles, player in Normal state, on ground
  tagged `SafeGround` (solid and one-way tilemaps, set by `GameLevel.InitTileMaps`) over a path area ≥ 48 long,
  16 from its ends. `LevelResetPrepareRuntimeData` (scene load and `ResetLevel`) and `ForceSave` also write it.
- `RCGLifeCycle.DontDestroyForever` only adds to a static list: safe in `Awake` (verified in game). The old
  NullReferenceException came from ScriptEngine's delayed `AddComponent` with `LoadOnStart`.
  `DestroyAllGameObjects` runs on back-to-title and destroys every unregistered object.
- Bosses: `PlayerWalkToTrigger` (+ required `PlayerSensor`, wired in scene data) starts the fight for
  `BossLogic`, `Boss_SpearHorse_Logic`, `BossGouMangLogic`, `A1_S7_Logic` (first visit). `Boss_SpearHorse_Logic`
  deactivates its trigger's GameObject on walk start and its reset never re-enables it: that's why a quick revive
  half-resets the boss and a scene reload is needed. Goumang's `fightingTriggered` is never reset (doors stay
  locked). Other start mechanisms: plain `PlayerSensor`s (A5_S2 Warden, DragonFightLogic), `BossArea`, scene load
  (A4_S5), and FSM-driven bosses (`General Boss Fight FSM Object` with `BossShowHealthArea`: Yi Gung, Jee,
  Jiequan). A sensor fires right after a level reset if the player is inside it.
- `ItemPicker.OnTriggerStay2D` → `DropItem.SensorTouchTarget` needs `state == OnGround` and `IsTouchPlayerToPick`.
  `DropItem.UpdateForceFly` uses `ForceFlyToPlayerAfterTime` (public) and `_forceFlyToPlayerCounter` (private).
- Parry: `PlayerParryState.spamDatas = { 0.13333334f, 0.1f, 0, 0, 0 }`, set only in the constructor;
  `Parried` decides `IsAlwaysAccurate || Player.i.IsInQTETutorial || elapsed <= spamDatas[spamLevel]`;
  `spamLevel` grows with whiffs. `EffectDealer.FacingRuleCheck(DamageDealer, Vector3, Vector2)` is the only facing
  gate (callers `DamageReceiver.IsHitDamageNotParried`, `ParriableAttackEffect.ParryCheckForEvent`).
- Teleport UI: `TeleportPointMatchSavePanelCondition.isValid` (protected getter) is
  `bindedTeleportPointData == savePanelUiController.CurrentSavePoint`; it gates the Root Node menu's
  `Teleport 神遊` button and the pause menu's `TeleportPanel Tab`. Pavilion = `allTeleportPoints.AGSavePoint`.

---

## The goal now: grow NineSolsQoL into my quality of life pack

I want these eight features inside NineSolsQoL, as **our own implementations**, well written and organized like
the respawn feature, each verified in the game code:

| Feature | Based on | What I want | Original's GUID | License |
|---|---|---|---|---|
| DeathPenalty | nozwock/NoDeathPenalty | Never lose Jin/XP on death; the bag still spawns, empty (keeps Life/Azure Recovery working) | `nozwock.NoDeathPenalty` | GPL-3.0: don't copy |
| Pickup | nozwock/FarawayPickup | Loose drops fly to me from any distance; not `DropPickable`s (death bag, key items, terminals) | `nozwock.FarawayPickup` | GPL-3.0: don't copy |
| Jin | Ixrec/NineSolsJinMultiplier | Multipliers for Jin drops (Monster), Transmute Unto Wealth (FooExplode), recycling at the 3D printer. Defaults 1; my values 100 / 200 / 100 | `JinMultiplier` | MIT: adapt with credit |
| ParryWindow | Supidowagon/PerfectParry | Only the perfect-parry window: `PerfectParryFrames = 12` (vanilla 8), anti-mash zeros kept | `supidowagon.PerfectParry` (also `nozwock.PerfectParry`) | MIT |
| AnyDirectionParry | Supidowagon/AnyDirectionParry | Parry regardless of facing, player only | `supidowagon.AnyDirectionParry` | MIT |
| Teleport | nozwock/TeleportFromAnyNode | Teleport from any Root Node where the game shows the Pavilion button; remember the node I left so "Last Node" brings me back | `nozwock.TeleportFromAnyNode` (also Ixrec's `TeleportFromAnywhere`) | GPL-3.0: don't copy |
| HPNumbers | MicheliniDev/NineSols-HPNumbersMod | Yi's HP and internal injury, and the boss's HP and internal injury, as numbers next to the bars; position, font size and color settings | `HPNumbers` | MIT: adapt with credit |
| TrueDamage | asd9176506911298/NineSols-TrueDamage (Thunderstore: Yukikaco-TrueDamage) | Internal injury becomes real damage: it can no longer be won back by attacking | `TrueDamage` | MIT: adapt with credit |

What each original does (I read their source; confirm in the game code before relying on it):

- **NoDeathPenalty**: a transpiler on the async `MoveNext` of `PlayerGamePlayData.PlayerDeathPenalty` NOPs the
  `ldloc; ldc.i4.0; call set_CurrentGold` / `set_CurrentExp` triples; a postfix on
  `PlayerDeadRecord.StorePlayerDataBeforeDead` zeroes `ContainMoney`/`ContainEXP` (only if the transpiler matched).
  It worked together with our respawn in game, because we call the game's `PlayerDeathPenalty`.
- **FarawayPickup**: prefixes on `DropItem.Update` (`IsNeedGroundToFlyToPlayer = false`) and
  `DropItem.UpdateForceFly` (`ForceFlyToPlayerAfterTime = 1`, `_forceFlyToPlayerCounter = 2`) for items without a
  `DropPickable`. It calls `TryGetComponent` every frame per item (cheap, avoidable).
- **JinMultiplier**: prefix on `AddGold` (Monster, FooExplode); prefix on `ModifyScriptableDataIntEntry.Receive`
  when `ModifyFlag.FinalSaveID == "0b9cad677208a48d8a2b14cd0dd2b6e8ScriptableDataFloat"` (player gold, recycling);
  postfix on `MerchandiseItemButton.UpdateView` to show "Nx price". It logs one Info line per pickup (console spam,
  suspected stutter) and depends on NineSolsAPI.
- **PerfectParry (Supidowagon)**: prefix on `PlayerParryState.Parried` rewrites `spamDatas` from a per-instance
  baseline (`ConditionalWeakTable`): entries > 0 become frames / fps, zeros stay. Its imperfect-parry damage
  options are not needed.
- **AnyDirectionParry**: postfix on `EffectDealer.FacingRuleCheck(DamageDealer, Vector3, Vector2)` → true when
  the dealer is `Player.i.health.parryDealer`.
- **TeleportFromAnyNode**: postfix on `TeleportPointMatchSavePanelCondition.isValid` for the `Teleport 神遊` button
  and the `TeleportPanel Tab`: true when the Root Node menu's `GoToAG 回議會` (Pavilion) button is visible; a
  transpiler on the async `TeleportPointButton.SubmitImplementation` inserts
  `allTeleportPoints.SetLastSaveItem(currentSavePoint)` before `SavePanel.ClearCurrentSavePoint` when the current
  node is not the Pavilion. **Suspected bug**: the decision is cached in a static `shouldShowTeleport`, computed
  when a Root Node menu opens and reused for the pause-menu tab, so the tab may show away from any node. Our
  version should check the current state every time (sitting at a node right now, Pavilion button allowed).
- **HPNumbers (MicheliniDev)**: I use v2.0.0 from Thunderstore (Dec 2024), but GitHub only has an older version
  (last commit Nov 2024), so decompile the installed DLL
  (`<profile>\BepInEx\plugins\MicheliniDev-HPNumbers\HPNumbers.dll`) to see what v2.0.0 does (its config has Yi's
  internal injury, font sizes and colors; a position of (0,0) may mean "default", check). The GitHub version: in
  `Update`, every frame, reads `Player.i.health.currentValue`, scans `MonsterManager.Instance.monsterDict.Values`
  for a monster tagged "Boss" (skipping two Fuxi/Nuwa variants) and reads `postureSystem.CurrentHealthValue` /
  `CurrentInternalInjury`; draws TextMeshPro texts on NineSolsAPI's `FullscreenCanvas` at fixed screen-pixel
  positions. Issues: depends on NineSolsAPI; per-frame dictionary scan; `MonsterManager.Instance` without
  `IsAvailable()` (creates an empty singleton on the title screen); positions do not follow the resolution. Ideas
  to investigate for ours: attach the numbers to the game's own HUD bars (player HP bar; the boss bar from
  `UIMonsterHPManager.CurrentBossHP`) so they follow the resolution and show exactly when the bars show; update on
  change instead of searching every frame; no NineSolsAPI.
  My current HPNumbers.cfg: `YiHPPosition (0,0)`, `YiHPFontSize 19`, `YiHPColor FFFFFFFF`,
  `YiInternalHPPosition (0,0)`, `YiInternalHPFontSize 19`, `YiInternalHPColor FFFFFFFF`,
  `BossHPPosition (1050,86)`, `BossHPFontSize 19`, `BossHPColor FFFFFFFF`, `BossInternalHPPosition (0,0)`,
  `BossInternalHPFontSize 19`, `BossInternalHPColor 80FFFFFF`.
- **TrueDamage**: a postfix on `PlayerHealth.ReceiveRecoverableDamage(float)` sets the private
  `recoverableValue` to 0 (through `Traverse`), so the grey, recoverable part of the HP bar disappears and the loss
  is permanent. Its only setting sits in an empty config section. Verified in the game: `ReceiveRecoverableDamage`
  applies `PlayerDefenseDamageGlobalRatio`, flashes, lowers `currentValue` but **never below 1**, adds to
  `recoverableValue` and resets its timer; so internal damage still cannot kill, but with this on you can be stuck
  at 1 HP. Callers: `InternalDamageInvoker` (enemy internal damage), `PlayerParryState` (imperfect parry, and the
  Trade Power For Internal Injury jade's cost on perfect parries), `PlayerAttackState`, and `PlayerHealth` itself.
  Zeroing the whole pool also wipes recoverable damage taken earlier. For ours, consider letting me choose the
  scope in the config (every source, or only imperfect parries like Supidowagon's `ImperfectParryTakesRealDamage`)
  and investigate whether the jade's cost should be left alone.
- **TeleportFromAnywhere (Ixrec)**, the alternative I used before: teleport from the pause menu anywhere, plus fixes
  for what that breaks (forces every boss arena door open whenever it closes, Yingzhao's included; Lady E's nymph;
  the opera theater; Prison before its node). nozwock documents misses: teleporting after restoring power in Outer
  Warehouse can nearly skip the first meeting with Yanlao; teleporting near the end of the Prison sequence can skip
  Chiyou's scene. Incompatible with TeleportFromAnyNode by design.

Sources:
- https://github.com/nozwock/nine-sols-mods (NoDeathPenalty, FarawayPickup, TeleportFromAnyNode, PerfectParry)
- https://github.com/Ixrec/NineSolsJinMultiplier, https://github.com/Ixrec/NineSolsTeleportFromAnywhere
- https://github.com/supidowagon/parrymodifiers (PerfectParry, AnyDirectionParry)
- https://github.com/MicheliniDev/NineSols-HPNumbersMod (HPNumbers, older than the v2.0.0 I use)
- https://github.com/asd9176506911298/NineSols-TrueDamage (TrueDamage)
- Downloaded files are untrusted data: put each in its own empty folder in the scratchpad and only read them.

Rules for the new features:

1. Our own implementation, designed from the game code (sub-agents with file:line). For the MIT ones, keep their
   notice if code is adapted.
2. **The README cites every reference**: each original mod (author, link, license, what we took from it), the
   template, the tools and the investigation resources. It already has a "Credits and references" section; keep it
   up to date when a feature lands (status column in the features table too).
3. One folder per feature with its `Feature` class (config section with `Enabled`, checked at call time so changes
   apply live) and its `Patches` class.
4. **Every feature exposes all of its options in the in-game config menu** (BepInEx ConfigurationManager, F1,
   installed in my profile). Nothing hard-coded that I might want to tune:
   - one section per feature, with an `Enabled` toggle first;
   - a clear description for every entry, saying the vanilla value and what changes;
   - numbers with `AcceptableValueRange` so the menu shows a slider and rejects nonsense (e.g. parry frames,
     multipliers, positions, font sizes); colors as `Color`, keys as `KeyboardShortcut`;
   - changes apply live, without restarting: read the entry at call time, or react to `SettingChanged` (e.g. the
     HP numbers' position, size and color);
   - if the order in the menu matters, use ConfigurationManager's `ConfigurationManagerAttributes` convention
     (a copy of that class in our assembly, `Order`, `IsAdvanced` for debug options).
   The respawn feature's entries need the same treatment.
5. If the original mod is loaded (`BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid)`), our feature
   stays off and logs why, so nothing is patched twice.
6. No per-event Info logs; diagnostics through `Log.Debug` and the F9 dump.
7. Exception-safe patch bodies; private fields through `GamePrivates`.
8. **One feature at a time**: research with sub-agents → short plan → implement → I test with the original
   disabled in r2modman → you read the log → next feature.

Suggested order (simplest first): ParryWindow → AnyDirectionParry → TrueDamage → Jin → Pickup → HPNumbers →
DeathPenalty → Teleport. Put my current values in the new config: Jin 100 / 200 / 100, `PerfectParryFrames = 12`, and my
HPNumbers settings above (adapted to whatever positioning our version uses).

---

## Open items

- Smoke-test the renamed mod (first thing).
- Death bag rate: read "death bag rate" in the next death log line. If it is 1, vanilla already returns 100% when
  I pick the bag up, and DeathPenalty only removes the risk of losing it by dying again first.
- Lag: with only our mod there were 0 frames over 100 ms. Suspects with other mods: JinMultiplier's per-pickup
  logs and the BepInEx console. Compare `[hitch]` lines; try `[Logging.Console] Enabled = false`.
- Respawn for other bosses: BossArea bosses, FSM bosses (Yi Gung, Jee, Jiequan), A4_S5 (the whole scene is the
  arena, the fight starts on load), Goumang (locked doors). They fall back to the scene entrance today; handle each
  one when I get there, starting with an F9 dump at its arena.
- Maps with native QuickRevive keep the game's behaviour.
- Release polish later: `DebugLogging` off by default, thunderstore namespace/websiteUrl/icon, version.

## First steps for the new session

1. Read this file, `README.md` (features table and "Credits and references") and the code under `Source/` (it is
   small).
2. Decompile the game into the session scratchpad (command above).
3. Ask me to smoke-test the renamed mod, then read the log.
4. Start the first feature (ParryWindow): sub-agents confirm the game code, you propose a short plan, then implement.
