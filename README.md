# Nine Sols QoL

BepInEx 5 + Harmony mod with quality of life tweaks for Nine Sols. Each feature has its own config section and
can be turned off. Work in progress. The player-facing description is in
[thunderstore/README.md](./thunderstore/README.md).

## Features

Grouped as in the in-game menu (F1).

| Group | Feature | What it does | Status | Based on |
|---|---|---|---|---|
| Abilities | EarlyAirDash | The game's own air dash (once per air time) before you acquire its upgrade; off by default | done | original |
| Abilities | FastHeal | Heal with the pipe as fast as with the Quick Dose jade, without the jade | done | original |
| Combat | PerfectParryWindow | Configurable perfect-parry window (12 frames instead of 8) | done | Supidowagon's PerfectParry |
| Combat | ParryFromBehind | Parry attacks that come from behind Yi | done | Supidowagon's AnyDirectionParry |
| Combat | DashInvulnerability | Invulnerable a little longer after a dash (0.1 s instead of 0.05), also when dashes follow each other | done | original |
| Death | RespawnNearDeath | Respawn near where you died; boss deaths reload the room and respawn outside the arena | done | original |
| Death | NoDeathPenalty | Never lose Jin or XP on death; the death bag still appears, empty | done | nozwock's NoDeathPenalty |
| HUD | BossHPNumbers | The boss's HP (and, if you want, its internal injury) as numbers | done | MicheliniDev's HPNumbers |
| Loot and Jin | LootMagnet | Loose drops fly to you from any distance | done | nozwock's FarawayPickup |
| Loot and Jin | JinMultipliers | Multipliers for Jin drops, Transmute Unto Wealth and selling (recycling) | done | Ixrec's JinMultiplier |
| Travel | TeleportFromAnyNode | Teleport from any Root Node, not just the Pavilion's | planned | nozwock's TeleportFromAnyNode |

Every feature is our own implementation, written from the game's code. Ideas taken from other mods are credited
below.

## Code layout

```
Source/
  Plugin.cs                  entry point: each feature's Init and patches, RCGLifeCycle registration
  Log.cs                     logging (Log.Debug only when DebugLogging is on)
  Core/
    ConfigSection.cs         binds a feature's config entries and places them in the in-game menu's groups
    ConfigurationManagerAttributes.cs  how the in-game config menu (F1) shows an entry
    GameAccess.cs            safe access to the game singletons
    GamePrivates.cs          the private game fields the mod reads or writes
    UnityExtensions.cs       small helpers for logging Unity objects
    DebugTools.cs            [Debug] config, F9 dump, post-respawn watchdog, [hitch] frame logger
  Features/                  one folder per menu group, one folder per feature
    Abilities/
      EarlyAirDash/
        EarlyAirDashFeature.cs     [EarlyAirDash] config, the check the game reads instead of its upgrade
        EarlyAirDashPatches.cs     transpiler: Player.DodgeCheck asks the mod whether the air dash is unlocked
      FastHeal/
        FastHealFeature.cs         [FastHeal] config, the jade check the heal reads instead
        FastHealPatches.cs         transpiler: the heal asks the mod whether to play at the Quick Dose speed
    Combat/
      PerfectParryWindow/
        PerfectParryWindowFeature.cs  [PerfectParryWindow] config, the window the game reads
        PerfectParryWindowPatches.cs  transpiler: PlayerParryState.Parried reads the window from the mod
      ParryFromBehind/
        ParryFromBehindFeature.cs  [ParryFromBehind] config, the player-only override
        ParryFromBehindPatches.cs  postfix on the facing check that runs before a parry
      DashInvulnerability/
        DashInvulnerabilityFeature.cs  [DashInvulnerability] config, the delay the dash reads
        DashInvulnerabilityPatches.cs  transpiler on the dash's exit, prefix on DodgeEnd, hit diagnostics
    Death/
      RespawnNearDeath/
        RespawnNearDeathFeature.cs  [RespawnNearDeath] config and dump section
        RespawnNearDeathPatches.cs  every Harmony patch of the feature; they only forward to the code below
        DeathSnapshot.cs           what the mod needs to know about a death, recorded when it happens
        RespawnController.cs       decides: the game's normal respawn or the mod's
        Respawner.cs               carries out the mod's respawns
        SafeTrail.cs               the safe ground the player walked on in the current scene
        ArenaTracker.cs            fight entrances and boss areas, and the respawn point for a boss fight
      NoDeathPenalty/
        NoDeathPenaltyFeature.cs     [NoDeathPenalty] config, the decision taken at each death
        NoDeathPenaltyPatches.cs     prefix and postfix where the game fills the death bag: empties it
        NoDeathPenaltyTranspiler.cs  transpiler: the death penalty skips its two lines that zero Jin and XP
    Hud/
      BossHPNumbers/
        BossHPNumbersFeature.cs    [BossHPNumbers] config, the numbers' own canvas, read every frame; no patches
    Loot/
      LootMagnet/
        LootMagnetFeature.cs       [LootMagnet] config, starts the game's own flight for loose drops
        LootMagnetPatches.cs       prefix on DropItem.Update
      JinMultipliers/
        JinMultipliersFeature.cs   [JinMultipliers] config, the multiplied amounts, shop price
        JinMultipliersPatches.cs   AddGold prefix, shop trade and shop row postfixes
```

Every `<Name>Feature` also adds its lines to the F9 dump.

A new feature goes in `Features/<Group>/<Name>/`: a `<Name>Feature` class that binds its config section in
`Init` (with its menu group from `MenuCategory` and its title), a `<Name>Patches` class with its Harmony
patches, and the code they forward to. `Plugin.Awake` calls each feature's `Init` in the order the menu lists
them; `Plugin.Start` applies each feature's patches with its own Harmony instance, unless a mod the
feature replaces is loaded (then the feature stays off and the log says why). Harmony only applies the methods
declared in the `<Name>Patches` class itself, so patches must not go in nested classes. A patch whose target is
found by code (`[HarmonyTargetMethod]`, e.g. the state machine of an async method) needs a class of its own,
because HarmonyX refuses it next to patches that name their target; `Plugin.Start` applies all of a feature's
classes with its one Harmony instance. A feature that patches nothing (BossHPNumbers) gets the same check for
the mod it replaces (`IsReplaced`) and runs from `Plugin.LateUpdate`.

Every option is in the in-game config menu ([ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager),
`F1`), grouped under titles such as `--------- Combat ---------` (the menu sorts the groups by name). Each
feature shows its `Enabled` switch under its own name, with its other options indented below; every option has a
description that says the vanilla value, numbers have a range (shown as a slider), and changes apply right
away. The `.cfg` file keeps one `[Section]` per feature. Debug options are under "Advanced settings".

## Building

`dotnet build` (Ctrl+Shift+B in VS Code) builds `Source/NineSolsQoL.csproj` and copies the DLL to the
r2modman profile's `BepInEx/scripts` folder. Adjust `<NineSolsPath>` and `<ProfileDir>` in the `.csproj` if
your game or profile live elsewhere.

`dotnet publish` builds a release DLL and packages it with `tcli` (`dotnet tool install -g tcli`) into
`thunderstore/build/`. The release goes to `BepInEx/plugins` like any other mod.

## Development loop (ScriptEngine)

With the [BepinExScriptEngine](https://thunderstore.io/c/nine-sols/p/ninesolsmodding/BepinExScriptEngine/) mod,
in `BepInEx/config/com.bepis.bepinex.scriptengine.cfg`:

- `LoadOnStart = false`: loading scripts at startup fails in Nine Sols.
- `EnableFileSystemWatcher = true` and `AutoReloadDelay = 1`: every build reloads the mod.

Start the game and press `F6` once. The log is in `<profile>/BepInEx/LogOutput.log`.

Do not keep the mod enabled in `BepInEx/plugins` at the same time, or it loads twice.

## Debugging

- `F9` (`DumpKey`): writes the player state, each feature's dump section and every collider around the player
  to the log.
- `DebugLogging` (on by default while in development): detailed diagnostics, including `[hitch]` lines for frames
  slower than 100 ms during gameplay.

## Reading the game code

The game DLLs are in `Nine Sols/NineSols_Data/Managed`. dnSpy's console exports them as C# in a few seconds:

```sh
dnSpy.Console.exe --no-sln --no-resources --no-tokens -o decomp Assembly-CSharp.dll rcg.rcgmakercore.Runtime.dll
```

Compiler-generated code (async lambdas) only shows up with `--show-all` or `-l IL`.

## Credits and references

This project is MIT licensed. Features inspired by GPL-3.0 mods are independent implementations written from the
game's code; no code was copied from them. Code adapted from MIT mods keeps their notice.

Mods whose ideas these features build on:

| Mod | Author | License | Used for |
|---|---|---|---|
| [PerfectParry](https://github.com/supidowagon/parrymodifiers) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Supidowagon/PerfectParry/)) | Supidowagon | MIT | PerfectParryWindow (the idea and its analysis of `PlayerParryState.Parried`; ours scales the game's windows at the point they are read instead of rewriting them) |
| [AnyDirectionParry](https://github.com/supidowagon/parrymodifiers) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Supidowagon/AnyDirectionParry/)) | Supidowagon | MIT | ParryFromBehind (the idea and the patch point, `EffectDealer.FacingRuleCheck`; ours is always player-only) |
| [JinMultiplier](https://github.com/Ixrec/NineSolsJinMultiplier) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Ixrec/JinMultiplier/)) | Ixrec | MIT | JinMultipliers (the idea and the patch points, `AddGold` and the shop's gold income; ours only multiplies sales and shows the real amount in the shop) |
| [FarawayPickup](https://github.com/nozwock/nine-sols-mods) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/nozwock/FarawayPickup/)) | nozwock | GPL-3.0 | LootMagnet (idea only; ours starts the game's own touch-pickup flight and leaves no field changed on pooled drops) |
| [HPNumbers](https://github.com/MicheliniDev/NineSols-HPNumbersMod) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/MicheliniDev/HPNumbers/)) | MicheliniDev | MIT | BossHPNumbers (the idea and its approach, numbers on a canvas of our own read every frame; ours follows the game's boss bar and hides with the game's HUD) |
| [NoDeathPenalty](https://github.com/nozwock/nine-sols-mods) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/nozwock/NoDeathPenalty/)) | nozwock | GPL-3.0 | NoDeathPenalty (idea only; ours decides at each death, so it can be turned off at any time, and never empties an older bag) |
| [NoDeathPenaltyNineSols](https://github.com/Baiker000/NoDeathPenaltyNineSols), the original nozwock's version builds on | Baiker000 | none stated | NoDeathPenalty (idea only) |
| [TeleportFromAnyNode](https://github.com/nozwock/nine-sols-mods) | nozwock | GPL-3.0 | TeleportFromAnyNode (idea only) |
| [TeleportFromAnywhere](https://github.com/Ixrec/NineSolsTeleportFromAnywhere) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Ixrec/TeleportFromAnywhere/)) | Ixrec | MIT | TeleportFromAnyNode (reference for edge cases) |
| [nine-sols-no-penalty](https://github.com/KiViXJ/nine-sols-no-penalty) | KiViXJ | MIT | NoDeathPenalty (reference) |

Tools and resources:

- [NineSols-ExampleMod](https://github.com/nine-sols-modding/NineSols-ExampleMod), the template this project
  started from.
- [BepInEx](https://github.com/BepInEx/BepInEx), [HarmonyX](https://github.com/BepInEx/HarmonyX) and
  [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug) for hot reloading.
- [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager), the in-game config
  menu; `ConfigurationManagerAttributes` follows its convention.
- [libs-stripped](https://github.com/nine-sols-modding/libs-stripped), the game DLLs used by the CI build.
- [dnSpy](https://github.com/dnSpy/dnSpy) to read the game's code.
- [Nine Sols modding wiki](https://github.com/nine-sols-modding/Resources/wiki),
  [UnityExplorer](https://thunderstore.io/c/nine-sols/p/ninesolsmodding/UnityExplorer/),
  [DebugModPlus](https://github.com/jakobhellermann/NineSols-DebugModPlus),
  [NKVDebugMod](https://github.com/Gogas1/NKVDebugMod) and
  [StateDump](https://github.com/nine-sols-modding/StateDump) for investigation.
