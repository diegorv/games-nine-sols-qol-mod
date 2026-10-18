# NineSolsQoL

BepInEx 5 + Harmony quality of life mod for Nine Sols. `README.md` has the features, code layout, build and dev
loop; `..\CLAUDE.md` maps the workspace (game, r2modman profile, decompiled game, MCP tools).

| Doc | What it holds |
|---|---|
| `docs/features.md` | How each feature works, and the design choices I made |
| `docs/game-facts.md` | Game facts verified in the decompiled code or in game: read before patching that area |
| `docs/open-items.md` | Untested paths, accepted limits, release polish |
| `..\ninesols\research\` | Per-feature research reports (outside the repo: they quote decompiled code) |

The features were renamed on 2026-10-17: Respawn → RespawnNearDeath, ParryWindow → PerfectParryWindow,
AnyDirectionParry → ParryFromBehind, Pickup → LootMagnet, HPNumbers → BossHPNumbers, AirDash → EarlyAirDash,
HealSpeed → FastHeal. `research\` and older commits use the old names.

## How to work with me

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
- I test in the game. You read `LogOutput.log` directly from the profile.
- **Commit at the end of each stage**, yourself, with an English message (one commit per stage, only that
  stage's files, on `master`; never push). I sometimes commit unrelated files myself: check `git log` before
  committing.
- Ask me before design decisions that change how the game plays (defaults, what a feature covers). Give a
  recommendation first.
- If you rename or delete files that may be open in VS Code, tell me to close those tabs without saving.

## Starting a session

1. Check that `..\ninesols\decompiled\BUILD.txt` matches the game's Steam build (`..\ninesols\README.md` says
   how; regenerate if not).
2. Read the end of `LogOutput.log` to see where I left off, and `git log --oneline -10`.
3. Ask me what comes next: release polish (`docs/open-items.md`) or a new feature (the process below). The
   EarlyAirDash and FastHeal transpilers are the pattern to copy for "make one game check pass".

## The process: repeat it for every feature

Every feature so far went through this and passed its in-game test on the first or second try.

1. **Set up.** The decompiled game is in `..\ninesols\decompiled\`. Original mods' sources, when needed, go into
   their own empty scratchpad folders (`git clone --depth 1`), read only.
2. **Research.** If the feature has a report in `..\ninesols\research\`, read it and
   `verification-summary.md`. For a new feature, run the discovery fan-out first: 1 Opus agent per feature
   writes `research\<feature>.md` (numbered claims `[Cn]`, file:line citations, a "Patch options (facts only)"
   section), then 2 Haiku agents check it (one for citations, one for missing callers/writers/overloads).
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
10. Keep `README.md` (features table, code layout, credits), `thunderstore/README.md` (player-facing) and
    `docs/` up to date. Mark a feature `done` after my in-game test.

**Sub-agent budget.** I grant it per session. Use Opus for discovery and adversarial review, Haiku for checking
citations, completeness, runtime APIs and game facts. When I allow multi-agent orchestration, run discovery
and review as Workflow scripts (pipeline: discovery agent → its verifiers in parallel; review: Opus + Haikus in
parallel). Their results are files in `research\` or structured output, so your context stays small.

Sub-agent prompt template (this structure produced reports precise enough to design from):

```
You are investigating the decompiled C# source of the Unity game Nine Sols (Mono) to support a BepInEx 5 +
Harmony mod (NineSolsQoL, repo C:\c\dev\modding\NineSolsQoL). Thoroughness: very thorough.

RULES
- Do not guess. Every claim must cite file path + line numbers and quote the relevant lines.
- If something cannot be confirmed in code (scene, prefab, animation or asset data), mark it UNCONFIRMED and say why.
- Read-only. Write only the report to C:\c\dev\modding\ninesols\research\<feature>.md and dumps under <scratchpad>\agent-<topic>\.
- Report: one "## Qn" section per question, numbered claims [C1].., then "## Surprises", then
  "## Patch options (facts only)" (signature, access, virtual, overloads, IL size; safe/risky facts: does
  turning the feature off restore vanilla, does it survive unpatch/re-patch, does it leave objects modified).

WHERE THE CODE IS
- C:\c\dev\modding\ninesols\decompiled\Assembly-CSharp\, \rcg.rcgmakercore.Runtime\, \RCG_General\ (one file per type; grep the whole folder)
- Third-party libraries (UniTask, DOTween, I2, QFSW...): C:\c\dev\modding\ninesols\decompiled-ilspy\libs\
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

## Environment and dev loop

Paths to the game, the profile and the log are in `..\CLAUDE.md`; build and ScriptEngine setup in `README.md`.

- Our config: `<profile>\BepInEx\config\diegorv.NineSolsQoL.cfg`.
- Runtime: BepInEx 5.4.23.5, HarmonyX 2.9.0 in `<profile>\BepInEx\core` (we compile against HarmonyX 2.7 from
  NuGet), ConfigurationManager 18.3.1. CI (`.github/workflows/build.yml`) builds against
  `nine-sols-modding/libs-stripped` (it has UniTask, Unity.TextMeshPro and UnityEngine.UI).
- Installed in the profile (2026-10-18): BepInExPack, NineSolsAPI, ScriptEngine, ConfigurationManager. The
  originals our features replace are no longer installed. I play at 1920x1080.
- I start the game from r2modman, load a save and press **F6 once**; after that every build hot-reloads the mod.
  Touching the DLL in `scripts` (`(Get-Item dll).LastWriteTime = Get-Date`) also reloads it, e.g. after editing
  the `.cfg` on disk (a new instance reads the file again).
- A hot reload resets every static of the mod: e.g. reloading in the middle of a boss fight loses the fight
  entrance, and the next death uses the game's respawn. Dev-only.
- **Keep NineSolsAPI installed**, even though the mod does not reference it: its Awake hides the BepInEx manager
  GameObject (`hideFlags = 61`) and registers it with `RCGLifeCycle.DontDestroyForever` (`NineSolsAPICore.cs:64,
  85` in 1.3.1). Without it the title screen destroys that object and F1 and F6 stop working (it happened on
  2026-10-17). A reinstall of BepInEx also resets ScriptEngine's `EnableFileSystemWatcher`/`AutoReloadDelay` and
  deletes our `.cfg`.
- Keys: **F1** config menu, **F9** our dump, **F8** opens one of the game's own debug screens.
- The game spams `Adding null Transform to TransformAccessArray` (vanilla). The log uses my system's decimal
  comma (`200,0 ms`). `[Logging.Disk] AppendLog = false`: a game restart overwrites the log, so copy it first
  if something went wrong.

## Code conventions (keep them)

- A feature lives in `Features/<Group>/<Name>/`: `<Name>Feature.Init(ConfigFile)` binds its section with
  `new ConfigSection(config, "<Name>", MenuCategory.<Group>, "<Title shown in F1>")` and registers its F9 lines
  with `DebugTools.AddDumpSection`; `<Name>Patches` holds only Harmony patches that forward to the feature. Add
  the Init call to `Plugin.Awake` (where it sits sets its place within its group) and
  `Patch("<Name>", typeof(<Name>Patches), "<original mod GUID>", ...)` to `Plugin.Start`.
- The F1 menu (ConfigurationManager 18.3) groups entries by their `Category` and sorts the groups by name; each
  group title is drawn centred, so `MenuCategory` names like `--------- Combat ---------` act as separators.
  Within a group, entries go by `Order` (one global counter in `ConfigSection`, so in binding order). A feature's
  `Enabled` entry is labelled with the feature's title, its other entries are indented (`DispName`), and labels
  come from the key's words (`BossHPFontSize` → "Boss HP font size") unless `name:` is given.
- `Plugin.Start` patches each feature with its own Harmony instance (`diegorv.NineSolsQoL.<Name>`): a feature
  whose patching fails is undone on its own, a feature whose original mod is loaded
  (`Chainloader.PluginInfos`) is skipped with a warning, and a class that patches nothing is an error. Harmony
  only applies methods declared in that class: **no nested patch classes**. Start does not patch if Awake failed.
- A patch whose target is found by code (`[HarmonyTargetMethod]`, e.g. an async method's state machine) goes in a
  class of its own: HarmonyX's `PatchClassProcessor.BulkPatch` throws "You cannot combine TargetMethod, TargetMethods
  or [HarmonyPatchAll] with individual annotations". Pass a feature's classes together,
  `Patch("<Name>", new[] { typeof(A), typeof(B) }, ...)`: one Harmony instance, all or nothing.
- A feature that patches nothing (BossHPNumbers) gets `Active = !IsReplaced("<Name>", "<original GUID>")` in
  `Plugin.Start`, runs from `Plugin.LateUpdate` and destroys what it created in `Plugin.OnDestroy`.
- Config: `Enabled` first, a description that states the vanilla value and what changes, numbers with
  `AcceptableValueRange` (slider), everything read at call time so F1 changes apply live. Debug entries are
  `advanced`.
- Private game members only through `Core/GamePrivates` (lazy FieldRefAccess / `FindMethod` delegates): a
  missing one is logged once and only that feature stops.
- Never read `X.Instance` of a `SingletonBehaviour` without `X.IsAvailable()`.
- Every method called from a patch catches its exceptions (the game's own exceptions, e.g. its index error in
  PerfectParryWindow, are left as they are).
- Off = vanilla exactly, and hot reload must leave nothing behind: no writes to assets (ScriptableObject,
  StatData) or pooled objects unless the game itself resets them. Prefer transpilers that replace one read, or
  postfixes on return values, over mutating game state.
- The Harmony analyzer treats every static method of a `[HarmonyPatch]` class as a patch: keep helpers out.
- No per-event `Log.Info`. `Log.Debug` for diagnostics, guarded with `Log.DebugEnabled` when the message is
  expensive; per-coin or per-frame events go to counters shown by F9, not to the log.
- Never patch `CanQuickRevive`/`FetchReviveData` (see `docs/features.md`, RespawnNearDeath).
