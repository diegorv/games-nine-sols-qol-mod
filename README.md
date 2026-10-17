# Nine Sols QoL

BepInEx 5 + Harmony mod with quality of life tweaks for Nine Sols. Each feature has its own config section and
can be turned off. Work in progress. The player-facing description is in
[thunderstore/README.md](./thunderstore/README.md).

## Features

| Feature | What it does | Status | Based on |
|---|---|---|---|
| Respawn | Respawn near where you died; boss deaths reload the room and respawn outside the arena | done | original |
| ParryWindow | Configurable perfect-parry window (12 frames instead of 8) | planned | Supidowagon's PerfectParry |
| AnyDirectionParry | Parry regardless of which way Yi is facing | planned | Supidowagon's AnyDirectionParry |
| TrueDamage | Internal injury becomes real damage that cannot be won back | planned | Yukikaco's TrueDamage |
| Jin | Multipliers for Jin drops, Transmute Unto Wealth and recycling | planned | Ixrec's JinMultiplier |
| Pickup | Loose drops fly to you from any distance | planned | nozwock's FarawayPickup |
| HPNumbers | Yi's and the boss's HP and internal injury as numbers | planned | MicheliniDev's HPNumbers |
| DeathPenalty | Never lose Jin or XP on death | planned | nozwock's NoDeathPenalty |
| Teleport | Teleport from any Root Node, not just the Pavilion's | planned | nozwock's TeleportFromAnyNode |

Every feature is our own implementation, written from the game's code. Ideas taken from other mods are credited
below.

## Code layout

```
Source/
  Plugin.cs                  entry point: each feature's Init, RCGLifeCycle registration, Harmony
  Log.cs                     logging (Log.Debug only when DebugLogging is on)
  Core/
    GameAccess.cs            safe access to the game singletons
    GamePrivates.cs          the private game fields the mod reads or writes
    UnityExtensions.cs       small helpers for logging Unity objects
    DebugTools.cs            [Debug] config, F9 dump, post-respawn watchdog, [hitch] frame logger
  Features/
    Respawn/
      RespawnFeature.cs      [Respawn] config and dump section
      RespawnPatches.cs      every Harmony patch of the feature; they only forward to the code below
      DeathSnapshot.cs       what the mod needs to know about a death, recorded when it happens
      RespawnController.cs   decides: the game's normal respawn or the mod's
      Respawner.cs           carries out the mod's respawns
      SafeTrail.cs           the safe ground the player walked on in the current scene
      ArenaTracker.cs        fight entrances and boss areas, and the respawn point for a boss fight
```

A new feature goes in `Features/<Name>/`: a `<Name>Feature` class that binds its config section in `Init`, a
`<Name>Patches` class with its Harmony patches, and the code they forward to. `Plugin.Awake` calls each
feature's `Init`.

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
| [PerfectParry](https://github.com/supidowagon/parrymodifiers) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Supidowagon/PerfectParry/)) | Supidowagon | MIT | ParryWindow |
| [AnyDirectionParry](https://github.com/supidowagon/parrymodifiers) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Supidowagon/AnyDirectionParry/)) | Supidowagon | MIT | AnyDirectionParry |
| [TrueDamage](https://github.com/asd9176506911298/NineSols-TrueDamage) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Yukikaco/TrueDamage/)) | Yukikaco (asd9176506911298) | MIT | TrueDamage |
| [JinMultiplier](https://github.com/Ixrec/NineSolsJinMultiplier) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Ixrec/JinMultiplier/)) | Ixrec | MIT | Jin |
| [FarawayPickup](https://github.com/nozwock/nine-sols-mods) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/nozwock/FarawayPickup/)) | nozwock | GPL-3.0 | Pickup (idea only) |
| [HPNumbers](https://github.com/MicheliniDev/NineSols-HPNumbersMod) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/MicheliniDev/HPNumbers/)) | MicheliniDev | MIT | HPNumbers |
| [NoDeathPenalty](https://github.com/nozwock/nine-sols-mods) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/nozwock/NoDeathPenalty/)) | nozwock | GPL-3.0 | DeathPenalty (idea only) |
| [NoDeathPenaltyNineSols](https://github.com/Baiker000/NoDeathPenaltyNineSols), the original nozwock's version builds on | Baiker000 | none stated | DeathPenalty (idea only) |
| [TeleportFromAnyNode](https://github.com/nozwock/nine-sols-mods) | nozwock | GPL-3.0 | Teleport (idea only) |
| [TeleportFromAnywhere](https://github.com/Ixrec/NineSolsTeleportFromAnywhere) ([Thunderstore](https://thunderstore.io/c/nine-sols/p/Ixrec/TeleportFromAnywhere/)) | Ixrec | MIT | Teleport (reference for edge cases) |
| [nine-sols-no-penalty](https://github.com/KiViXJ/nine-sols-no-penalty) | KiViXJ | MIT | DeathPenalty (reference) |

Tools and resources:

- [NineSols-ExampleMod](https://github.com/nine-sols-modding/NineSols-ExampleMod), the template this project
  started from.
- [BepInEx](https://github.com/BepInEx/BepInEx), [HarmonyX](https://github.com/BepInEx/HarmonyX) and
  [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug) for hot reloading.
- [libs-stripped](https://github.com/nine-sols-modding/libs-stripped), the game DLLs used by the CI build.
- [dnSpy](https://github.com/dnSpy/dnSpy) to read the game's code.
- [Nine Sols modding wiki](https://github.com/nine-sols-modding/Resources/wiki),
  [UnityExplorer](https://thunderstore.io/c/nine-sols/p/ninesolsmodding/UnityExplorer/),
  [DebugModPlus](https://github.com/jakobhellermann/NineSols-DebugModPlus),
  [NKVDebugMod](https://github.com/Gogas1/NKVDebugMod) and
  [StateDump](https://github.com/nine-sols-modding/StateDump) for investigation.
