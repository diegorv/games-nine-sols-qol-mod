using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace NineSolsQoL.Core;

// Debug helpers, driven by Plugin.Update:
//  - DumpKey (F9) writes the player state, the death bag, what each feature registered with
//    AddDumpSection and every collider around the player to the log, to find out which objects a
//    spot belongs to (triggers, sensors, arena areas).
//  - Watch logs the player state twice a second for a while, to see where a respawn gets stuck
//    (input state, frozen, hidden...) and what happens to the death bag.
//  - With DebugLogging on, every frame slower than HitchSeconds during gameplay is logged, to tell
//    whether stutters line up with something the mod does.
internal static class DebugTools {
    private const float DumpRadius = 48f;
    private const float WatchInterval = 0.5f;
    private const float HitchSeconds = 0.1f;

    public static ConfigEntry<bool> DebugLogging = null!;
    public static ConfigEntry<KeyboardShortcut> DumpKey = null!;

    private static readonly List<Func<GameCore, IEnumerable<string>>> dumpSections = new();

    private static string watchReason = "";
    private static float watchUntil;
    private static float nextWatchLog;
    private static float lastFrameTime;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "Debug", MenuCategory.Debug, null);
        DebugLogging = section.Bind("DebugLogging", false,
            "Write detailed diagnostics to the BepInEx log", advanced: true);
        // F8 opens one of the game's own debug screens
        DumpKey = section.Bind("DumpKey", new KeyboardShortcut(KeyCode.F9),
            "Writes the player state, each feature's state and every collider around the player to the " +
            "BepInEx log", advanced: true);
    }

    // Lines a feature adds to the dump, e.g. its own tracking state
    public static void AddDumpSection(Func<GameCore, IEnumerable<string>> section) {
        dumpSections.Add(section);
    }

    public static void Update() {
        CheckHitch();
        if (DumpKey.Value.IsDown()) DumpSurroundings();
        TickWatch();
    }

    public static void Watch(string reason, float seconds = 10f) {
        watchReason = reason;
        watchUntil = Time.realtimeSinceStartup + seconds;
        nextWatchLog = 0f;
    }

    private static void TickWatch() {
        var now = Time.realtimeSinceStartup;
        if (now >= watchUntil || now < nextWatchLog) return;
        nextWatchLog = now + WatchInterval;

        var core = GameAccess.CoreInLevel();
        Log.Info($"[watch {watchReason}] " +
                 (core != null ? $"{DescribeState(core)} | {DescribeDeathBag(core)}" : "no level loaded"));
    }

    private static void CheckHitch() {
        var now = Time.realtimeSinceStartup;
        var frame = now - lastFrameTime;
        var first = lastFrameTime == 0f;
        lastFrameTime = now;
        if (first || frame < HitchSeconds || !DebugLogging.Value) return;

        // Loading screens and scene changes are slow on purpose
        var core = GameAccess.CoreInLevel();
        if (core == null || core.currentCoreState != GameCore.GameCoreState.Playing) return;
        Log.Info($"[hitch] a frame took {frame * 1000f:0} ms in {core.CurrentSceneName} " +
                 $"(player state {core.player.CurrentStateType})");
    }

    // One line with everything that decides whether the player is visible and can move
    public static string DescribeState(GameCore core) {
        var player = core.player;
        return $"core={core.currentCoreState} scene={core.CurrentSceneName} pos={player.transform.position} " +
               $"state={player.CurrentStateType} input={player.playerInput.currentStateType} " +
               $"lockMoving={player.lockMoving} freeze={player.freeze} isCloaking={player.isCloaking} " +
               $"active={player.gameObject.activeInHierarchy} timeScale={Time.timeScale}";
    }

    // The carried Jin, what the death bag holds, and the bag object the game spawned. The death bag
    // is not picked up by touch (touchToPick is false): the player has to interact with it, which
    // runs PlayerRestoreDeathPenaltyAction in its FSM.
    public static string DescribeDeathBag(GameCore core) {
        var record = core.deadRecord;
        var bag = GamePrivates.DroppedBag(record);
        var bagObject = "none";
        if (bag != null && bag.gameObject.activeInHierarchy) {
            var item = bag.GetComponentInChildren<DropItem>();
            bagObject = item != null
                ? $"at {bag.transform.position} state={item.state} touchToPick={item.IsTouchPlayerToPick}"
                : $"at {bag.transform.position} (no DropItem)";
        }
        return $"jin={core.playerGameData.CurrentGold} bagRecord={record.HasDeadRecord.CurrentValue} " +
               $"bagJin={record.ContainMoney.CurrentValue} bagObject={bagObject}";
    }

    private static void DumpSurroundings() {
        var core = GameAccess.CoreInLevel();
        if (core == null) {
            Log.Info("[dump] no level loaded");
            return;
        }

        var player = core.player;
        var mapData = core.gameLevel.BindingMapData;
        Log.Info($"[dump] {DescribeState(core)}");
        Log.Info($"[dump] levelOrigin={core.gameLevel.transform.position} " +
                 $"lastSafeGround={player.lastSafeGroundPosition} onGround={player.onGround} " +
                 $"nativeQuickRevive={mapData != null && mapData.QuickRevive}");
        foreach (var section in dumpSections) {
            try {
                foreach (var line in section(core)) Log.Info($"[dump] {line}");
            } catch (Exception e) {
                Log.Error($"[dump] a feature's dump section failed: {e}");
            }
        }

        Log.Info($"[dump] {DescribeDeathBag(core)}");

        foreach (var collider in Physics2D.OverlapCircleAll(player.transform.position, DumpRadius)) {
            Log.Info($"[dump]   '{collider.transform.HierarchyPath()}' trigger={collider.isTrigger} " +
                     $"bounds={collider.bounds.Describe()} [{ComponentNames(collider.gameObject)}]");
        }
    }

    private static string ComponentNames(GameObject gameObject) =>
        // GetComponents returns null for components whose script is missing
        string.Join(", ", gameObject.GetComponents<Component>()
            .Where(c => c != null && c is not Transform)
            .Select(c => c.GetType().Name));
}
