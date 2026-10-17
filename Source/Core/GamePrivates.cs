using System;
using HarmonyLib;
using UnityEngine;

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

    private static readonly Lazy<AccessTools.FieldRef<PlayerParryState, float[]>?> ParryWindowTable =
        new(() => Find<PlayerParryState, float[]>("spamDatas"));

    private static readonly Lazy<DropItemPrivates?> DropItemAccess = new(DropItemPrivates.Find);

    // The private parts of DropItem the pickup feature uses, or null when a game update renamed one
    public static DropItemPrivates? DropItems => DropItemAccess.Value;

    // The game's perfect-parry windows in seconds, indexed by PlayerParryState.spamLevel. Read only:
    // the parry window feature never changes the array.
    public static float[]? ParryWindows(PlayerParryState state) =>
        ParryWindowTable.Value is { } field ? field(state) : null;

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

    internal static AccessTools.FieldRef<T, TField>? Find<T, TField>(string name) where T : class {
        try {
            return AccessTools.FieldRefAccess<T, TField>(name);
        } catch (Exception e) {
            Log.Error($"Game field {typeof(T).Name}.{name} not found, a game update probably changed it: {e.Message}");
            return null;
        }
    }

    // An instance method as a delegate whose first parameter is the instance
    internal static TDelegate? FindMethod<TDelegate>(Type type, string name) where TDelegate : Delegate {
        try {
            var method = AccessTools.Method(type, name) ?? throw new MissingMethodException(type.Name, name);
            return AccessTools.MethodDelegate<TDelegate>(method);
        } catch (Exception e) {
            Log.Error($"Game method {type.Name}.{name} not found, a game update probably changed it: {e.Message}");
            return null;
        }
    }
}

// DropItem's private members, all found or none
internal sealed class DropItemPrivates {
    // False until the drop's first EnterLevelReset; DropItem.Update does nothing before that
    public AccessTools.FieldRef<DropItem, bool> AfterLevelReset = null!;

    // Where a flying drop goes; set only while it flies to Yi
    public AccessTools.FieldRef<DropItem, Transform> FlyTarget = null!;

    // What the drop gives (GetComponent<IDropping> on the drop, done once by the game)
    public AccessTools.FieldRef<DropItem, IDropping> Effect = null!;

    // The game's force-fly timer, set to 0 by EnterLevelReset (called by LootSpawner on every spawn
    // and by the pool on every reuse)
    public AccessTools.FieldRef<DropItem, float> FlyCounter = null!;

    // The drop's body, which the game moves while the drop flies
    public AccessTools.FieldRef<DropItem, Rigidbody2D> Body = null!;

    // What the game calls when a flight starts: the collider becomes a trigger (for drops with
    // IsNeedGroundToFlyToPlayer, the default) and the body kinematic
    public Action<DropItem> TurnOffSolid = null!;

    public static DropItemPrivates? Find() {
        var afterLevelReset = GamePrivates.Find<DropItem, bool>("_afterLevelReset");
        var flyTarget = GamePrivates.Find<DropItem, Transform>("flyToTarget");
        var effect = GamePrivates.Find<DropItem, IDropping>("_iDropping");
        var flyCounter = GamePrivates.Find<DropItem, float>("_forceFlyToPlayerCounter");
        var body = GamePrivates.Find<DropItem, Rigidbody2D>("_rigidBody2D");
        var turnOffSolid = GamePrivates.FindMethod<Action<DropItem>>(typeof(DropItem), "ForceFlyToTargetTurnOffSolid");
        if (afterLevelReset == null || flyTarget == null || effect == null || flyCounter == null || body == null ||
            turnOffSolid == null) {
            return null;
        }

        return new DropItemPrivates {
            AfterLevelReset = afterLevelReset,
            FlyTarget = flyTarget,
            Effect = effect,
            FlyCounter = flyCounter,
            Body = body,
            TurnOffSolid = turnOffSolid,
        };
    }
}
