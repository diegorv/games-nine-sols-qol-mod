# Nine Sols QoL: handoff for a new session

Paste this whole file as the first message of a new session. It describes who I am, how I want to work, the
current state of the mod, what was verified in the game code, and what comes next.

---

## Who I am and how to work with me

- I'm Diego, a programmer with 10+ years of experience, but new to modding and to Unity.
- **Reply to me in Brazilian Portuguese.** Code, comments, docs and commit messages stay in English.
- Explain what changed and why, briefly. When I ask what a feature does in the game, explain it in gameplay
  terms with a small example before the code.
- **Don't guess.** Before writing or changing a Harmony patch, read the real game code. Use sub-agents to read
  the decompiled code, one focused question set each. They must cite file:line, quote the code, and mark as
  UNCONFIRMED anything that lives in scene, prefab, animation or asset data. Then confirm yourself the few facts
  the design depends on.
- **Surgical patches.** Pick the option that changes the least game behaviour, at the known patch point (often the
  original mod's): e.g. swap two calls inside a method rather than skip or replace the whole method. Don't invent
  new mechanisms; a broader design only when the narrow one has a real blocker.
- **Follow the original when it is simple.** If the mod a feature replaces already does it simply (HPNumbers:
  texts on its own canvas), copy its approach and fix only its real bugs, instead of designing something new.
- **Don't make me wait.** Run the reviews in the background while I test; commit after my test and apply the
  review's real findings in a separate commit.
- **One hypothesis at a time.** Change one thing, I test it in the game, you read the log.
- **Instrument before fixing.** Logs and the F9 dump answer in minutes what guessing does not.
- I test in the game. You read `LogOutput.log` directly from the profile (path below).
- **Commit at the end of each stage**, yourself, with an English message (one commit per stage, only that
  stage's files, on `master`; never push). I sometimes commit unrelated files myself: check `git log` before
  committing.
- Ask me before design decisions that change how the game plays (defaults, what a feature covers). Give a
  recommendation first.

---

## The process: repeat it for every feature

Every feature so far went through this and passed its in-game test on the first or second try.

1. **Set up the session.** Decompile the game into the scratchpad (command below). Original mods' sources, when
   needed, go into their own empty scratchpad folders (`git clone --depth 1`), read only.
2. **Research.** Every remaining feature already has a report in `research/` (see "What comes next"). Read it
   and `research/verification-summary.md`. For a new feature, run the discovery fan-out first: 1 Opus agent per
   feature writes `research/<feature>.md` (numbered claims `[Cn]`, file:line citations, a "Patch options (facts
   only)" section), then 2 Haiku agents check it (one for citations, one for missing callers/writers/overloads).
3. **Synthesize for me** in Portuguese: what is confirmed, what is UNCONFIRMED (prefab/scene data), what changes
   the design, bugs in the original mod, and the decisions I need to take.
4. **Verify yourself** every signature, accessibility, caller and field the design depends on (grep the
   decompiled code, dnSpy `-l IL` for transpilers) before writing code.
5. **Propose the plan** (files, patches, config entries with defaults and ranges, diagnostics, test steps) and
   wait for my OK.
6. **Implement**, build (the build hot-reloads the mod in the running game) and check the log for the reload
   (`unloaded` → `Registered ... in RCGLifeCycle` → `loaded` → `<Feature>: N game method(s) patched`) and for
   exceptions.
7. **Review while I test**: start the review as a background Workflow (1 Opus agent does an adversarial review
   of the diff, trying to find real bugs with quoted evidence; 1-2 Haiku agents check the library APIs against
   the runtime DLLs in the profile and the game facts in the comments) and at the same time tell me exactly what
   to test, in a short numbered list, and what the log and F9 should show.
8. **Commit** after my test (docs included, feature marked `done`); apply the review's real findings later in
   their own commit ("Apply the <Feature> review findings"), build again, and tell me if anything needs a retest.
9. **When the answer lives in game data**, add a diagnostic (F9 dump section, counters, a Log.Debug line) and
   ask me for one concrete action. Remove one-off diagnostics once they answered their question.
10. Keep `README.md` (features table, code layout, credits) and `thunderstore/README.md` (player-facing) up to
    date. Mark a feature `done` after my in-game test.

**Sub-agent budget.** I grant it per session (last grant: 20 Opus + 40 Haiku for everything left; about 12 Opus
and 36 Haiku were unused after HealSpeed). Use Opus for discovery and adversarial review, Haiku for checking
citations, completeness, runtime APIs and game facts. When I allow multi-agent orchestration, run discovery
and review as Workflow scripts (pipeline: discovery agent → its verifiers in parallel; review: Opus + Haikus in
parallel). Their results are files in `research/` or structured output, so your context stays small.

Sub-agent prompt template (this structure produced reports precise enough to design from):

```
You are investigating the decompiled C# source of the Unity game Nine Sols (Mono) to support a BepInEx 5 +
Harmony mod (NineSolsQoL, repo C:\c\dev\NineSols-ExampleMod). Thoroughness: very thorough.

RULES
- Do not guess. Every claim must cite file path + line numbers and quote the relevant lines.
- If something cannot be confirmed in code (scene, prefab, animation or asset data), mark it UNCONFIRMED and say why.
- Read-only. Write only the report to <repo>\research\<feature>.md and dumps under <scratchpad>\agent-<topic>\.
- Report: one "## Qn" section per question, numbered claims [C1].., then "## Surprises", then
  "## Patch options (facts only)" (signature, access, virtual, overloads, IL size; safe/risky facts: does
  turning the feature off restore vanilla, does it survive unpatch/re-patch, does it leave objects modified).

WHERE THE CODE IS
- <scratchpad>\decomp\Assembly-CSharp\, \rcg.rcgmakercore.Runtime\, \RCG_General\ (one file per type; grep the whole folder)
- Game DLLs: J:\SteamLibrary\steamapps\common\Nine Sols\NineSols_Data\Managed
- For compiler-generated code: dnSpy.Console.exe --no-tokens --no-color --show-all -t TypeName <dll> (or -l IL)

MOD CONVENTIONS
- Enabled read at call time, every option in the F1 menu, off = vanilla exactly, hot reload unpatches and
  re-patches while the game runs, private fields through lazy FieldRefAccess, patch bodies never throw,
  SingletonBehaviour<T>.Instance only after IsAvailable().

CONTEXT
<what the feature must do, what the original mod patches, what is already known>

QUESTIONS
1. ...
```

---

## Environment (Windows)

| Item | Path / value |
|---|---|
| Game | `J:\SteamLibrary\steamapps\common\Nine Sols` |
| Game DLLs | `J:\SteamLibrary\steamapps\common\Nine Sols\NineSols_Data\Managed` |
| Repo | `C:\c\dev\NineSols-ExampleMod` (folder name is still the template's) |
| Project | `NineSolsQoL.sln`, `Source\NineSolsQoL.csproj` |
| Research notes | `research\` in the repo, **gitignored** (they quote decompiled game code) |
| Mod manager | r2modman, profile **Default** |
| Profile | `%AppData%\r2modmanPlus-local\NineSols\profiles\Default` |
| BepInEx log | `<profile>\BepInEx\LogOutput.log` |
| Our config | `<profile>\BepInEx\config\diegorv.NineSolsQoL.cfg` |
| Tools | .NET SDK 10, Git, VS Code + C# Dev Kit, `C:\Users\diegorv\Downloads\dnSpy-net-win64\dnSpy.Console.exe` |
| Build | `dotnet build Source/NineSolsQoL.csproj` (Ctrl+Shift+B in VS Code) |
| CI | `.github/workflows/build.yml` builds against `nine-sols-modding/libs-stripped` (it has UniTask, Unity.TextMeshPro and UnityEngine.UI) |
| Runtime | BepInEx 5.4.23.5, HarmonyX 2.9.0 in `<profile>\BepInEx\core` (we compile against HarmonyX 2.7 from NuGet), ConfigurationManager 18.3.1 |

Installed in the profile: BepInExPack, NineSolsAPI, ScriptEngine, ConfigurationManager, UnityExplorer, NKVDebugMod,
CutsceneSkip, CelesteSols. The originals our features replace are **disabled** (`.dll.old`): JinMultiplier,
TeleportFromAnywhere, HPNumbers, YiXPNumber, FarawayPickup, NoDeathPenalty. I play at 1920x1080.

### Dev loop

- The build copies the DLL to `<profile>\BepInEx\scripts` (`CopyDir` in the csproj).
- ScriptEngine: `LoadOnStart = false` (loading at startup fails in Nine Sols), `EnableFileSystemWatcher = true`,
  `AutoReloadDelay = 1`. I start the game from r2modman, load a save and press **F6 once**; after that every
  build hot-reloads the mod. Touching the DLL in `scripts` (`(Get-Item dll).LastWriteTime = Get-Date`) also
  reloads it, e.g. after editing the `.cfg` on disk (a new instance reads the file again).
- A hot reload resets every static of the mod: e.g. reloading in the middle of a boss fight loses the fight
  entrance, and the next death uses the game's respawn. Dev-only.
- Never have the DLL in both `scripts` and `plugins`.
- If you rename or delete files that may be open in VS Code, tell me to close those tabs without saving.
- **F8 opens one of the game's own debug screens.** Our dump key is F9. F1 is the config menu.
- The game spams `Adding null Transform to TransformAccessArray` (vanilla). The log uses my system's decimal
  comma (`200,0 ms`).

### Decompiling the game (do this first, the output is per session)

```sh
dnSpy.Console.exe --no-sln --no-resources --no-tokens --no-color --spaces 4 --asm-path "<Managed>" -o "<scratchpad>/decomp" "<Managed>/Assembly-CSharp.dll" "<Managed>/rcg.rcgmakercore.Runtime.dll" "<Managed>/RCG_General.dll"
```

About 15 s. One file per type, flat folders (namespaced types in subfolders). Async methods and lambdas only
show up with `--show-all` or `-l IL`; `-t TypeName` prints one type to stdout. Line numbers in `research/` and
below come from this output.

---

## Current state of the code (NineSolsQoL 0.1.0)

```
Source/
  Plugin.cs                  Awake: each feature's Init; Start: each feature's patches; RCGLifeCycle registration
  Log.cs                     Log.Debug only when DebugLogging is on (written at Info level); Log.DebugEnabled
  Core/
    ConfigSection.cs         binds a section's entries in menu order (Enabled first), ranges, advanced flag
    ConfigurationManagerAttributes.cs  Order / IsAdvanced, read by ConfigurationManager by type and field name
    GameAccess.cs            CoreInLevel (null on title/loading), IsBossFightActive
    GamePrivates.cs          private game fields and methods, looked up lazily (DropItemPrivates for Pickup)
    UnityExtensions.cs       HierarchyPath, Bounds.Describe
    DebugTools.cs            [Debug] config, F9 dump (+ AddDumpSection, each section in try/catch), watchdog, [hitch]
  Features/
    Respawn/                 RespawnFeature, RespawnPatches, DeathSnapshot, RespawnController, Respawner, SafeTrail, ArenaTracker
    ParryWindow/             ParryWindowFeature, ParryWindowPatches (transpiler)
    AnyDirectionParry/       AnyDirectionParryFeature, AnyDirectionParryPatches (postfix)
    JinMultipliers/          JinMultipliersFeature, JinMultipliersPatches (AddGold prefix, Trade/UpdateView postfixes)
    Pickup/                  PickupFeature, PickupPatches (DropItem.Update prefix)
    NoDeathPenalty/          NoDeathPenaltyFeature, NoDeathPenaltyPatches (bag prefix/postfix), NoDeathPenaltyTranspiler
    HPNumbers/               HPNumbersFeature, HPNumber (own overlay canvas, read in LateUpdate; no patches)
    AirDash/                 AirDashFeature, AirDashPatches (Player.DodgeCheck transpiler)
    HealSpeed/               HealSpeedFeature, HealSpeedPatches (PlayerDrinkPotionState.OnStateEnter transpiler)
```

Conventions (keep them):

- A feature lives in `Features/<Name>/`: `<Name>Feature.Init(ConfigFile)` binds its section with
  `new ConfigSection(config, "<Name>")` and registers its F9 lines with `DebugTools.AddDumpSection`;
  `<Name>Patches` holds only Harmony patches that forward to the feature. Add the Init call to `Plugin.Awake`
  and `Patch("<Name>", typeof(<Name>Patches), "<original mod GUID>", ...)` to `Plugin.Start`.
- `Plugin.Start` patches each feature with its own Harmony instance (`diegorv.NineSolsQoL.<Name>`): a feature
  whose patching fails is undone on its own, a feature whose original mod is loaded
  (`Chainloader.PluginInfos`) is skipped with a warning, and a class that patches nothing is an error. Harmony
  only applies methods declared in that class: **no nested patch classes**. Start does not patch if Awake failed.
- A patch whose target is found by code (`[HarmonyTargetMethod]`, e.g. an async method's state machine) goes in a
  class of its own: HarmonyX's `PatchClassProcessor.BulkPatch` throws "You cannot combine TargetMethod, TargetMethods
  or [HarmonyPatchAll] with individual annotations". Pass a feature's classes together,
  `Patch("<Name>", new[] { typeof(A), typeof(B) }, ...)`: one Harmony instance, all or nothing.
- A feature that patches nothing (HPNumbers) gets `Active = !IsReplaced("<Name>", "<original GUID>")` in
  `Plugin.Start`, runs from `Plugin.LateUpdate` and destroys what it created in `Plugin.OnDestroy`.
- Config: `Enabled` first, a description that states the vanilla value and what changes, numbers with
  `AcceptableValueRange` (slider), everything read at call time so F1 changes apply live. Debug entries are
  `advanced`.
- Private game members only through `Core/GamePrivates` (lazy FieldRefAccess / `FindMethod` delegates): a
  missing one is logged once and only that feature stops.
- Never read `X.Instance` of a `SingletonBehaviour` without `X.IsAvailable()`.
- Every method called from a patch catches its exceptions (the game's own exceptions, e.g. its index error in
  ParryWindow, are left as they are).
- Off = vanilla exactly, and hot reload must leave nothing behind: no writes to assets (ScriptableObject,
  StatData) or pooled objects unless the game itself resets them. Prefer transpilers that replace one read, or
  postfixes on return values, over mutating game state.
- The Harmony analyzer treats every static method of a `[HarmonyPatch]` class as a patch: keep helpers out.
- No per-event `Log.Info`. `Log.Debug` for diagnostics, guarded with `Log.DebugEnabled` when the message is
  expensive; per-coin or per-frame events go to counters shown by F9, not to the log.

---

## Features done (all tested in game)

| Feature | Config `[Section]` | How it works |
|---|---|---|
| Respawn | `Enabled`, `BossRespawnOutsideArena` | Ordinary deaths: quick revive on the last safe ground (`PlayerDeadState.Revive` prefix). Boss deaths: reload the scene and spawn just outside the arena. Details below. |
| ParryWindow | `Enabled`, `PerfectParryFrames` (1-30, default 12) | Transpiler in `PlayerParryState.Parried` replaces the read `spamDatas[spamLevel]` with `PerfectWindow(windows, level, elapsed)`; every non-zero window × frames/8 (12 → 200/150 ms), zeros (anti-mash) kept. `[parry]` Debug line per parry. Also applies in the Yingzhao leaderboard challenge (my choice). |
| AnyDirectionParry | `Enabled` | Postfix on `EffectDealer.FacingRuleCheck(DamageDealer, Vector3, Vector2)`: a failed check becomes true when the instance is `Player.i.health.parryDealer`. Always player-only. |
| JinMultipliers | `Enabled`, `MonsterDrops`, `TransmuteUntoWealth`, `Selling` (1-1000, default 1; mine 100/200/100) | `AddGold` prefix multiplies Monster/Chest coins and FooExplode; `MerchandiseData.Trade` postfix adds the rest for sales (type 販賣 with a `mainMaterial`, paid in the gold field); `MerchandiseItemButton.UpdateView` postfix shows the multiplied price. Death bag and purchases untouched. |
| Pickup | `Enabled`, `Delay` (0-3 s, default 0.5) | `DropItem.Update` prefix starts the game's own flight (`ForceFlyToTargetTurnOffSolid`, `flyToTarget = CenterAnchor`, `FlyToPicker`) for value drops (`AbstractDropping` or `DropArrow`) with touch pickup; timer = the game's `_forceFlyToPlayerCounter`; no flights while loading or dead; flying drops wait in the air while Yi is dead. All loose drops, like FarawayPickup (my choice). |
| NoDeathPenalty | `Enabled` | Transpiler on the async `PlayerDeathPenalty`'s `MoveNext` (found through `AsyncStateMachineAttribute`) points its two `CurrentGold = 0` / `CurrentExp = 0` calls at helpers that skip them when the death decided to keep; the rest of the penalty (delay, deathCount, AutoSave, ResetDeathPenalty) stays vanilla. Prefix + postfix on `PlayerDeadRecord.StorePlayerDataBeforeDead` take the decision at death (`Enabled` and the transpiler matched, kept in a static until the penalty) and empty the bag only when the game stored a new one (`CanStoreDeadRecord`). Bag still spawns, empty, no option (my choice). |
| HPNumbers | `Enabled`; `BossHP` (on) and `BossInternalHP` (off), each with `Position` (pixels of a 1920x1080 screen, left end of the number), `FontSize` (8-72, 19), `Color` | Like MicheliniDev's HPNumbers: TMP texts on a canvas of our own (ScreenSpaceOverlay + CanvasScaler 1920x1080, registered in RCGLifeCycle, destroyed on unload), read in `LateUpdate`, text rebuilt only when the int changes. The boss is the one whose bar the game shows (`monsterHpUI.CurrentBossHP.bindingPosture`: `GetPostureValue()`, `InternalInjury`); everything hides with the HUD (`playerInGameUIDisplayVote.Result`). No Yi numbers and current value only (my choice). Off when `HPNumbers` (the original's GUID) is loaded. |
| AirDash | `Enabled` (default **off**, my choice) | The game has its own air dash (one per air time: `CanRoll` needs `airDodgeCount == 0`, refilled by `ResetJumpAndDash`), gated by the `RollDodgeInAirUpgrade` ability, unlocked later in the game. A transpiler swaps the one `RollDodgeInAirUpgrade.IsActivated` read in `Player.DodgeCheck` (IL_0054/IL_0059) for `AirDashUnlocked(upgrade)` = the game's getter OR `Enabled`. The ability is never written (no save change; UI and skill tree still see it locked). `[air dash]` Debug line when the mod made the difference. |
| HealSpeed | `Enabled` (default on) | Heal at the Quick Dose jade's speed without the jade (my choice; no multiplier, no stacking). `PlayerDrinkPotionState.OnStateEnter` sets `animator.speed = 1.66666f` when its private `isSpeedUpHeal` (`SpeedUpHealJade.IsActivated`) is true; a transpiler swaps that one call for `SpeedUpHeal(state)` = the jade's getter OR `Enabled`. The jade the state checks is `(Jade Data) Fast Pipe 速藥玉` (seen in game with F9). `[heal]` Debug line when the mod made the difference. |

TrueDamage was **dropped** (it makes the game harder); its report stays in `research/truedamage.md`.

### Respawn feature, in short

1. Prefix `PlayerDeadState.OnStateEnter` → `DeathSnapshot.Record()` (position, last safe ground, level, boss
   fight: `monsterHpUI.CurrentBossHP != null` or an engaged monster tagged "Boss").
2. Prefix on the private async `PlayerDeadState.Revive` → `RespawnController.TryRespawn`; false skips the game's
   revive. The game's respawn still runs in debug mode, maps with native `CanQuickRevive()`, VR challenge, memory
   mode, leaderboard challenge, a pending tutorial respawn, core state not Playing, or a level change.
3. Ordinary death → `Respawner.QuickReviveAt`: `PlayerDeathPenalty()` if `IsNeedDeathPenalty` (vanilla quick
   revive never applies it), `FadeToBlack` → `ResetLevel(false)` (spawns the bag) → `RestoreEverything` →
   Normal → `SetPosition` → `ForceOnGround` → `SafeGroundRecorder.ForceSave(pos)` (the reset moved the last safe
   ground to the death spot) → camera → BGM → `FadeOutBlack`. Fallback: `RespawnAtSavePoint`.
4. Boss death → `Respawner.ReloadSceneAt`: `ChangeScene` to the same scene at a level-relative point (newest
   `SafeTrail` point outside the fight entrance + 64 margin, recorded when `PlayerWalkToTrigger` first fires;
   fallbacks: outside active `BossArea`s, then the scene entrance); clears `GameCore._lastTeleportPoint` first.
   Validated on Yingzhao many times.
5. Never patch `CanQuickRevive`/`FetchReviveData`: `FetchReviveData` sets `_lastTeleportPoint`, whose
   `OnSceneChangeDone` plays the Root Node revive animation that only an animation event ends: invisible player
   with no control away from the node.

---

## Verified game facts worth keeping

From the decompiled code unless marked otherwise ("seen in game" = F9 dump or log).

**Respawn and death**
- `GameCore.FetchReviveData`: debug branch; force-save branch (`SaveManager.forceSaveFlag`); otherwise
  `lastSaveTeleportPoint.ChangeSceneData` and `_lastTeleportPoint = lastSaveTeleportPoint`.
- `GameCore.InitializeGameLevel`: `SetPosition(spawn)` → `ChangedDoneEvent` → penalty if `IsNeedDeathPenalty` →
  `resetManager.GameLevelStart` → `_lastTeleportPoint.OnSceneChangeDone` → fade-out callback (sets Playing).
- `PlayerDeadRecord.StorePlayerDataBeforeDead`: bag = carried Jin/XP × `DeathPenaltyRate` (= **1**, seen in
  game); drop point = last safe ground; a killer monster that can carry the bag gets a murderer tag. Dying again
  before pickup overwrites the record. The bag is picked up by interaction (`PlayerRestoreDeathPenaltyAction`).
  The bag is a `DropItem` with `IsTouchPlayerToPick = false` and a `DropPickable` (seen in game).
- `PlayerGamePlayData.PlayerDeathPenalty`: `async void`; after 100 ms: `deathCount++`, `CurrentGold = 0`,
  `CurrentExp = 0`, `AutoSave(FlagOnly)`, `Player.i.ResetDeathPenalty()`.
- `SafeGroundRecorder` records in `LateUpdate` on safe ground only; `ResetLevel` and `ForceSave` also write it.
  The player sits under the ResetManager (`GameCore(Clone)/RCG LifeCycle`, seen in game).
- `RCGLifeCycle.DontDestroyForever` only adds to a static list (safe in Awake). Back to title destroys every
  unregistered GameObject.
- Bosses: `PlayerWalkToTrigger` starts the fight for `BossLogic`, `Boss_SpearHorse_Logic`, `BossGouMangLogic`,
  `A1_S7_Logic`; others use `PlayerSensor`, `BossArea`, scene load or FSMs (Yi Gung, Jee, Jiequan).

**Death penalty**
- `PlayerGamePlayData.PlayerDeathPenalty` is `async void`; its body is `<PlayerDeathPenalty>d__32.MoveNext` (a
  struct): after `UniTask.Delay(100)`, `data.deathCount += 1`, a Debug.Log, `ldloc.1; ldc.i4.0; call set_CurrentGold`
  and the same for `set_CurrentExp` (once each, IL_00D9-IL_00E2, no labels), `AutoSave(FlagOnly).Forget()`,
  `Player.i.ResetDeathPenalty()` (the only writer that clears `_lastNeedDeathPenalty`).
- Callers, all `if (IsNeedDeathPenalty)`: `GameCore.InitializeGameLevel` (every scene load), `DebugFastRevive` (debug
  mode), our `Respawner.QuickReviveAt`. Maps with the game's own quick revive defer it to the next scene load.
- That AutoSave is the only save after most deaths with our Respawn (quick revive and boss reload skip the Root
  Node's save): a design that skipped the whole penalty would lose the kept Jin on quit.
- `StorePlayerDataBeforeDead` runs at death only when the death has a penalty (`NeedDeathPenaltyCheck`: not in memory
  mode, VR challenge or with `DeathPanaltyImmune`) and returns early when `CanStoreDeadRecord` fails (the record still
  holds an older bag). Bag spawn and pickup never read the contents: an empty bag spawns, shows on the map and gives
  `AddGold(0)`/`GainExp(0)` (seen in game). `deathCount` has no reader in code.

**Parry**
- `PlayerParryState.Parried` (public virtual) tests `elapsed <= spamDatas[spamLevel] + 0f`; elapsed =
  `Time.time` − the latest buffered parry press (`PlayerInputCommandQueue`, presses expire after 0.5 s).
  `spamDatas` is private, `{0.13333334, 0.1, 0, 0, 0}`, read only there (IL_01EB..IL_0200). `spamLevel++` on a
  Parry exit without a parry; reset when exactly one press is buffered on enter, or on level reset.
- `ParryCounterDefenseState` overrides `Parried` and is always perfect. Auto parry, `IsAlwaysAccurate` and the
  QTE tutorial skip the window.
- `EffectDealer.FacingRuleCheck(DamageDealer, Vector3, Vector2)` is called only by
  `DamageReceiver.IsHitDamageNotParried` and `ParriableAttackEffect.ParryCheckForEvent`. Unparriable attacks are
  refused later in `ParriableAttackEffect.PlayerParriedCheck` (`if (!component.parriable) return false`).
  `Health.parryDealer` is the same object as `PlayerParryState.parryEffectDealer` (`.../SpriteHolder/ParryArea`,
  seen in game), whose rule the Parry state sets on enter (FaceToFace on ground/wall/ledge/rope).
- After a parry from behind, `KnockBack` turns Yi to face the attacker.

**Jin**
- `AddGold(int, GoldSourceTag)` has 4 callers: coins (`DropMoney`), Transmute Unto Wealth (10 per talisman kill,
  a skill), the `add.money` console command and the death bag (`DeathPenalty` tag). Coins report `Monster`
  (pooled copies do not inherit the `[NonSerialized]` tag) or `Chest`.
- Gold is a `ScriptableDataFloat`. `AddGold` writes past its cap; every shop write (`ConsumeCheck`, `Receive`)
  goes through the clamped setter. The cap is **99,999 with no ExternalRepository** (seen in game): the excess is
  lost at the next purchase or sale. Kept vanilla by my choice (documented in F1 and the Thunderstore README).
- Selling = `MerchandiseData.Trade` → `OutcomeResult.Receive()` on a 販賣 trade. Recycling at the 3D printer is
  assumed to be one (not yet seen in the log: look for `[jin] sold` or `is not a sale`).

**Drops**
- `DropItem` states Drop → OnGround (landing) → FlyToPicker → PickedUp. Vanilla flights: touch on the ground
  (`SensorTouchTarget`) or a prefab force-fly timer. `Picked()` credits (`IDropping.AbsorbedByPlayer`) and
  returns to the pool. `EnterLevelReset` (each spawn and reuse) resets state, target, trigger and the timer.
- `ResetLevel(false)` returns only monsters to the pool: drops stay where they were. A full scene load returns
  every pooled drop; a drop flying at that moment loses its value (the game logs "When DropItem destroyed and
  absorbed?").

**HUD**
- The boss bar: `GameCore.monsterHpUI` (`UIMonsterHPManager`) keeps a private `bossHPList`; `CurrentBossHP` is its
  first entry (null when no bar shows); `UIBossHP.bindingPosture` is public. The bar draws `GetPostureValue()` (int)
  and `RemainTotal = GetPostureValue() + InternalInjury`. `DestroyBossHealth` removes the bar from the list at once
  (it only plays an Exit animation); `BossArea.localBossHP` bars bypass the list (whether any scene uses one:
  UNCONFIRMED).
- The HUD shows while `UIManager.playerInGameUIDisplayVote.Result` (public `RuntimeConditionVote`) is true: an AND
  of the gameplay input state (votes false on pause, map, dialogue, cutscene, death, loading), the "show gameplay
  UI" setting, the area name banner and a few scripted scenes. HPNumbers hides with it (seen in game).
- Back to title destroys every GameObject not registered with `RCGLifeCycle.DontDestroyForever` (which registers
  the object and the children it has at that moment).

**Dash and heal**
- The dash is the `Roll` state, entered only by `Player.DodgeCheck` (`CanRoll || CanDownDash`, `RollDodgeAbility`,
  in the air `!IsWeak` and `RollDodgeInAirUpgrade`, `!IsInQTETutorial`). One air dash per air time:
  `airDodgeCount` (raised in `PlayerRollState.OnStateEnter`, cleared only by `ResetJumpAndDash`, read only by
  `CanRoll`). `ResetJumpAndDash` runs on landing (`VelY <= 0`), wall slide/run, ledge, rope, pile, grapple pass,
  zipline, every parry, jump-kick bounce, weakness hit, talisman stick and respawn; it also refills double jump,
  air attack, spin kick and the parry cooldown, so never call it to grant a dash. Each roll zeroes VelY and has no
  gravity. `RollDodgeInAirUpgrade` is read in code only by `DodgeCheck` (and the `activate.airDash` console command).
- Healing: `PlayerDrinkPotionState`; the heal happens at the "Heal" clip's `Done` animation event, and the Quick
  Dose jade (`(Jade Data) Fast Pipe 速藥玉`, the state's own `SpeedUpHealJade` wrapper) only sets `animator.speed =
  1.66666f` on enter (`OnStateExit` sets 1). `PlayerMainAbilityCollection.SpeedUpHealJade` is never read.

**Config menu and runtime**
- ConfigurationManager 18.3.1 finds `ConfigurationManagerAttributes` by type name, copies `Order`/`IsAdvanced`
  by field name, sorts by Order descending.
- BepInEx's chainloader adds every plugin in one loop, so every plugin's Awake runs before any Start.
- HarmonyX 2.9 `PatchAll(Type)` = `CreateClassProcessor(type, true).Patch()`; `[HarmonyPatch(type, "Name")]`
  resolves with `AccessTools.DeclaredMethod`; `AccessTools.MethodDelegate` builds open-instance delegates for
  private methods; there is no `AccessTools.AsyncMoveNext` (resolve state machines through
  `AsyncStateMachineAttribute`), and `MethodType.Enumerator` handles iterators and UniTask-returning methods, not
  `async void`. `__state` is one local per patch declaring type, so a prefix and postfix in the same class share
  it; postfixes run on every return path (not when the original throws).

---

## What comes next

Only Teleport is left (then release polish, see Open items).

### Teleport (research: `research/teleport.md`)

What I want: **teleport from any Root Node where the game shows the Pavilion button, and remember the node I
left so "Last Node" brings me back.** Today the node list only works from the Pavilion (the game's hub), so a
trip between two nodes means going through the Pavilion first.

Where to start:
- Read `research/teleport.md`, its checkers `research/teleport.verify-a.md` and `teleport.verify-b.md`, and the
  "teleport" section of `research/verification-summary.md` (top risks, the originals' bugs, UNCONFIRMED list and
  the checkers' missing references: other writers of `lastSaveTeleportPoint`/`SetReviveSavePoint`, the giant
  elevator path, `TeleportUIButtonWithPermission`, tab-click re-evaluation).
- The originals, to clone read-only into the scratchpad and compare (follow the simplest proven patch point):
  nozwock's TeleportFromAnyNode (`github.com/nozwock/nine-sols-mods`, GPL-3.0: idea only, no code copied) and
  Ixrec's TeleportFromAnywhere (`github.com/Ixrec/NineSolsTeleportFromAnywhere`, MIT; installed but disabled
  in my profile as `.dll.old`).
- The buttons' prefab wiring (which condition components sit on the Root Node "Teleport" button, the
  "GoToAG" (Pavilion) button and the pause-menu "TeleportPanel Tab", and whether any is inverted) is
  UNCONFIRMED and the design depends on it: get it first with a one-off F9 dump section (list the Root Node menu
  buttons and the tab with their conditions, `FinalResultInverted` and active state) while I sit at a node,
  or with UnityExplorer.

What the research found:
- `TeleportPointMatchSavePanelCondition.isValid` = `bindedTeleportPointData == savePanelUiController.CurrentSavePoint`,
  never cached by the game; buttons re-check it on enable, tabs on every `GoToTab`.
- A postfix on `isValid` hits every instance (some may be inverted): scope it to the Root Node "Teleport" button
  and the pause-menu "TeleportPanel Tab" only, both decided by the same live check.
- `CurrentSavePoint` is cleared only by `TeleportPointButton.SubmitImplementation` and goes stale after standing
  up; "sitting at a node right now" = `CurrentStateType == EnterSavePoint && CurrentSavePointGameObjectOnScene !=
  null && its BindingTeleportPoint == CurrentSavePoint`.
- A prefix on the `SubmitImplementation` stub runs before `ClearCurrentSavePoint` (no transpiler needed). Guard
  `SetLastSaveItem` (it throws for nodes missing from `allTeleportPoints`). `SetLastSaveItem(Pavilion)` is a
  no-op, and the respawn point is not affected.
- Gating on the live Pavilion button validity keeps us to places vanilla already reaches via the Pavilion.
- nozwock's static-cache bug is confirmed in code. Prefab wiring of the buttons is UNCONFIRMED (UnityExplorer).
- Story-skip risks documented by the originals (for teleporting from anywhere): after restoring power in Outer
  Warehouse you can nearly skip the first meeting with Yanlao; near the end of the Prison sequence you can skip
  Chiyou's scene. Ixrec also forces boss arena doors open and fixes Lady E's nymph and the opera theater. Check
  which still apply when teleporting only from Root Nodes that show the Pavilion button.
- Original GUIDs: `nozwock.TeleportFromAnyNode`; Ixrec's `TeleportFromAnywhere` is an alternative (expected
  GUID `TeleportFromAnywhere`).

---

## Open items

- Pickup: the "flying drops wait while Yi is dead" path is verified by review only, not seen in game.
- Pickup: health, qi and ammo drops fly in even when full and the excess is wasted (like FarawayPickup;
  accepted). Possible improvement: leave them on the ground while the resource is full.
- Pickup: the F9 list of nearby drops was a one-off check (the bag is ignored, seen in game): remove it at
  release polish.
- JinMultipliers: Transmute Unto Wealth and selling not yet seen in the log (`[jin] Transmute Unto Wealth`,
  `[jin] sold`; `is not a sale` would mean the 3D printer uses another trade type).
- The game froze once during the HPNumbers session (2026-10-17) and had to be restarted. Cause unknown: the restart
  overwrote `LogOutput.log` (`[Logging.Disk] AppendLog = false` in `BepInEx.cfg`) and the game no longer writes
  Unity's `Player.log`. If it happens again, copy the log before reopening the game (or turn `AppendLog` on).
- NoDeathPenalty: picking up an empty bag with the Life Recovery or Azure Recovery skill is not tested yet (I don't
  have them). Their effects live in prefab FSMs; nothing in code reads the bag's contents, so they should still
  trigger.
- NoDeathPenalty, accepted limits: a hot reload between a death and its penalty (2 s, or until the next door on maps
  with the game's own quick revive) loses that death's Jin (dev only). On such a map two deaths share one pending
  penalty: if the setting is toggled between them and the second death stores no bag (`CanStoreDeadRecord` fails),
  the second death's decision wins, giving the old full bag plus the kept Jin (off, then on) or an empty bag and
  zeroed Jin (on, then off). Fixing it needs one more patch (the pending flag before `NeedDeathPenaltyCheck`);
  whether `CanStoreDeadRecord` can flip between two deaths without a scene load is UNCONFIRMED. An unretrieved bag
  from a vanilla death is overwritten by the next death, as in vanilla.
- Respawn for other bosses: BossArea bosses, FSM bosses (Yi Gung, Jee, Jiequan), A4_S5 (fight starts on load),
  Goumang (locked doors). They fall back to the scene entrance today; start with an F9 dump at the arena.
- Lag: with only our mod there were no frames over 100 ms. With chatty mods the BepInEx console is the prime
  suspect: compare `[hitch]` lines and try `[Logging.Console] Enabled = false`.
- History: commit `1712573` ("Remove old prompt") carries part of the Pickup code and does not build alone;
  `31a03bc` completes it.
- Reviews still running when this handoff was written: HPNumbers (`8f49ab4`), AirDash (`2fbd23d`) and HealSpeed
  (`9bd509c`); the three features passed their in-game tests. If `git log` has no "Apply the <Feature> review
  findings" commit for one of them, run an adversarial review of that commit's diff before the release.
- Release polish later: `DebugLogging` off by default, Thunderstore namespace/websiteUrl/icon, version,
  remove one-off dump lines.

## First steps for the new session

1. Read this file, `README.md` and the code under `Source/` (the AirDash and HealSpeed transpilers are the
   pattern to copy for "make one game check pass").
2. Decompile the game into the session scratchpad (command above).
3. Read the end of `LogOutput.log` to see where I left off, and `git log --oneline -10`.
4. Start **Teleport** (see "What comes next"): read its research and checkers, clone the two originals into the
   scratchpad, verify the key facts yourself, and give me the synthesis in Portuguese. Then the F9 diagnostic for
   the buttons' wiring (ask me to sit at a Root Node and press F9, and also F9 with the pause menu's teleport tab),
   and only then the plan.
