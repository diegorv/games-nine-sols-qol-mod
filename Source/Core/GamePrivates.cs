using System;
using HarmonyLib;

namespace NineSolsQoL.Core;

// Private game fields the mod needs. Each one is looked up on first use: if a game update renamed
// it, the error is logged once and only the feature that needs it is skipped (an exception while
// initializing a class used by a patch would break the game's own code instead).
internal static class GamePrivates {
    private static readonly Lazy<AccessTools.FieldRef<PlayerWalkToTrigger, bool>?> WalkTriggered =
        new(() => Find<PlayerWalkToTrigger, bool>("_triggered"));

    private static readonly Lazy<AccessTools.FieldRef<GameCore, TeleportPointData>?> LastTeleportPoint =
        new(() => Find<GameCore, TeleportPointData>("_lastTeleportPoint"));

    private static readonly Lazy<AccessTools.FieldRef<PlayerDeadRecord, DropPickable>?> LastDroppedBag =
        new(() => Find<PlayerDeadRecord, DropPickable>("lastDroppedBag"));

    // The death bag object the game spawned last (PlayerDeadRecord.GenerateDeadBag), if any
    public static DropPickable? DroppedBag(PlayerDeadRecord record) =>
        LastDroppedBag.Value is { } field ? field(record) : null;

    // PlayerWalkToTrigger.Triggered only acts the first time
    public static bool WasTriggered(PlayerWalkToTrigger trigger) =>
        WalkTriggered.Value is { } field && field(trigger);

    // At the end of a scene load the game invokes _lastTeleportPoint.OnSceneChangeDone. When that
    // Root Node is in the loaded scene, this plays its revive animation: the player is hidden and
    // the input is blocked until the animation ends, which never happens away from the Root Node.
    public static void ClearLastTeleportPoint(GameCore core) {
        if (LastTeleportPoint.Value is not { } field) return;
        ref var point = ref field(core);
        if (point == null) return;
        Log.Info($"Cleared GameCore._lastTeleportPoint ({point.name}) so its revive animation does not play");
        point = null!;
    }

    private static AccessTools.FieldRef<T, TField>? Find<T, TField>(string name) where T : class {
        try {
            return AccessTools.FieldRefAccess<T, TField>(name);
        } catch (Exception e) {
            Log.Error($"Game field {typeof(T).Name}.{name} not found, a game update probably changed it: {e.Message}");
            return null;
        }
    }
}
