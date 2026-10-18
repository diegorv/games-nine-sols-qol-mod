using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using NineSolsQoL.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NineSolsQoL.Features.Loot.LootMagnet;

// Loose drops (Jin, health, qi, ammo, arrows) fly to Yi from anywhere in the room.
//
// Vanilla, a DropItem flies to Yi when Yi touches it on the ground (SensorTouchTarget) or when its
// prefab has a force-fly timer (UpdateForceFly): flyToTarget is set, the state becomes FlyToPicker
// and ForceFlyToTargetTurnOffSolid turns the collider into a trigger. DropItem.Update then chases
// the target and calls Picked, which credits the drop and returns it to its pool. The feature
// starts that same flight from far away, towards player.CenterAnchor like UpdateForceFly, and
// leaves the rest to the game. Only value drops move: the death bag and key items are
// DropPickables (the death bag also has IsTouchPlayerToPick off, seen in game).
//
// A drop still flying when a scene loads is returned to its pool without being credited, as in
// vanilla; the game logs "When DropItem destroyed and absorbed?" for it.
internal static class LootMagnetFeature {
    private const int DumpedDrops = 15;

    public static ConfigEntry<bool> Enabled = null!;
    public static ConfigEntry<float> Delay = null!;

    // Flights started since the mod loaded, by what the drop gives, for the F9 dump
    private static readonly Dictionary<Type, int> flights = new();

    // The player, worked out once per frame
    private static int playerFrame = -1;
    private static Player? playerToFlyTo;
    private static bool playerDead;

    // Set after an unexpected error: the game's own pickup takes over until the game restarts
    private static bool failed;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "LootMagnet", MenuCategory.Loot, "Loot magnet");
        Enabled = section.Bind("Enabled", true,
            "Jin, health, qi, ammo and arrows dropped anywhere in the room fly to Yi on their own. The death " +
            "bag and key items still have to be picked up by hand. Off: drops only come to Yi when Yi " +
            "touches them");
        Delay = section.Bind("Delay", 0.5f,
            "Seconds after a drop appears before it flies to Yi, so you still see it pop out. A drop that " +
            "falls into a pit flies back too. 0: at once",
            new AcceptableValueRange<float>(0f, 3f));

        DebugTools.AddDumpSection(Describe);
    }

    // Before DropItem.Update, every frame for every active drop. False skips the game's Update for
    // this frame. Must not throw.
    public static bool BeforeUpdate(DropItem item) {
        if (!Enabled.Value || failed) return true;

        try {
            var state = item.state;
            if (state == DropItem.States.FlyToPicker) return !HoldWhileDead(item);
            if (state != DropItem.States.Drop && state != DropItem.States.OnGround) return true;
            // A drop without IsNeedGroundToFlyToPlayer only flies on the game's own timer, and the game
            // would not turn its body back from kinematic after a flight the mod started
            if (!item.IsTouchPlayerToPick || !item.IsNeedGroundToFlyToPlayer) return true;

            StartFlight(item);
        } catch (Exception e) {
            failed = true;
            Log.Error($"LootMagnet is off until the game restarts, after an error with '{item.name}': {e}");
        }
        return true;
    }

    private static void StartFlight(DropItem item) {
        var privates = GamePrivates.DropItems;
        // Update ignores a drop before its first reset; a target means it already flies
        if (privates == null || !privates.AfterLevelReset(item) || privates.FlyTarget(item) != null) return;
        var effect = privates.Effect(item);
        if (!IsLooseDrop(effect)) return;

        UpdatePlayer();
        var target = playerToFlyTo != null ? playerToFlyTo.CenterAnchor : null;
        if (target == null) return;

        // The game's force-fly timer. A drop whose prefab uses it (ForceFlyToPlayerAfterTime > 0) has
        // it counted by UpdateForceFly, only once it lies on the ground, so the mod only reads it and
        // the game's timing stays as it is; such a drop does not fly back from a pit
        ref var timer = ref privates.FlyCounter(item);
        if (item.ForceFlyToPlayerAfterTime <= 0f) timer += Time.deltaTime;
        if (timer < Delay.Value) return;

        privates.TurnOffSolid(item);
        privates.FlyTarget(item) = target;
        item.state = DropItem.States.FlyToPicker;
        Count(effect);
    }

    // While Yi is dead, a flying drop waits where it is. Jin it gave now would be wiped by the death
    // penalty (the death bag was filled when Yi died), and health would be thrown away. After the
    // revive the game's Update resumes the chase.
    private static bool HoldWhileDead(DropItem item) {
        UpdatePlayer();
        var privates = GamePrivates.DropItems;
        if (!playerDead || privates == null) return false;

        // The body is kinematic during a flight: without this it would keep drifting
        var body = privates.Body(item);
        if (body != null) body.velocity = Vector2.zero;
        return true;
    }

    // Value drops: Jin, health, qi and ammo (AbstractDropping) and arrows. A DropPickable (death bag,
    // key items) or a drop without an effect is left alone.
    private static bool IsLooseDrop(IDropping? effect) => effect is AbstractDropping or DropArrow;

    // Drops fly only while the game is playing and Yi is alive: not during loading or a scene change
    private static void UpdatePlayer() {
        if (playerFrame == Time.frameCount) return;
        playerFrame = Time.frameCount;

        var core = GameAccess.CoreInLevel();
        playerDead = core != null && core.player.CurrentStateType == PlayerStateType.Dead;
        playerToFlyTo = core != null && core.currentCoreState == GameCore.GameCoreState.Playing && !playerDead
            ? core.player
            : null;
    }

    private static void Count(IDropping effect) {
        var type = effect.GetType();
        flights.TryGetValue(type, out var count);
        flights[type] = count + 1;
    }

    // The flights so far and the drops around, to check what flies and what does not
    private static IEnumerable<string> Describe(GameCore core) {
        var counts = flights.Count > 0 ? string.Join(", ", flights.Select(f => $"{f.Key.Name} {f.Value}")) : "none";
        yield return $"LootMagnet: enabled={Enabled.Value} delay={Delay.Value}s failed={failed} flights: {counts}";

        var privates = GamePrivates.DropItems;
        if (privates == null) {
            yield return "LootMagnet: DropItem's private fields are missing";
            yield break;
        }

        var center = core.player.transform.position;
        var drops = Object.FindObjectsOfType<DropItem>()
            .OrderBy(d => Vector2.Distance(center, d.transform.position))
            .ToList();
        yield return $"LootMagnet: {drops.Count} active drops, nearest {Math.Min(drops.Count, DumpedDrops)}:";
        foreach (var drop in drops.Take(DumpedDrops)) {
            var effect = privates.Effect(drop);
            yield return $"  '{drop.transform.HierarchyPath()}' state={drop.state} touchToPick={drop.IsTouchPlayerToPick} " +
                         $"effect={(effect != null ? effect.GetType().Name : "none")} " +
                         $"distance={Vector2.Distance(center, drop.transform.position):0}";
        }
    }
}
