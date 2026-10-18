using System;
using System.Collections.Generic;
using System.Linq;
using NineSolsQoL.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NineSolsQoL.Features.Death.RespawnNearDeath;

// Where boss fights start, to respawn the player just outside the arena.
//
// Most boss fights start with a PlayerWalkToTrigger: a sensor area that, when the player steps in,
// takes control, walks the player to a spot and starts the fight (BossLogic, Boss_SpearHorse_Logic,
// BossGouMangLogic...). When one fires, the respawn point for that fight is picked: the newest safe
// ground the player walked on before it, outside the trigger area. Spawning inside the area would
// start the fight again right away.
//
// Some bosses mark their arena with a BossArea instead. Without either, the fallback is the spot
// where the player entered the scene.
internal static class ArenaTracker {
    // Distance kept from the trigger and boss areas
    private const float Margin = 64f;

    // The last fight entrance that fired and the respawn point picked for it (relative to the level root)
    private static string entranceScene = "";
    private static string entranceName = "";
    private static Vector3? entranceRespawn;

    public static void OnSceneLoaded(GameLevel level) {
        // The same scene loaded again (reload, or back through a door) keeps its arena where it was
        if (level.SceneName != entranceScene) entranceRespawn = null;
    }

    public static void OnWalkTrigger(PlayerWalkToTrigger trigger) {
        try {
            if (GamePrivates.WasTriggered(trigger)) return; // Triggered does nothing the second time
            var core = GameAccess.CoreInLevel();
            if (core == null) return;

            var origin = core.gameLevel.transform.position;
            var colliders = trigger.GetComponentsInChildren<Collider2D>();
            var areas = colliders.Select(c => Relative(c.bounds, origin)).ToList();

            entranceScene = core.gameLevel.SceneName;
            entranceName = trigger.transform.HierarchyPath();
            entranceRespawn = NewestTrailPointOutside(core, areas);

            if (!Log.DebugEnabled) return;
            var playerPos = core.player.transform.position;
            Log.Debug($"Fight entrance '{entranceName}' fired with the player at {playerPos}: respawn point " +
                      (entranceRespawn.HasValue ? $"{entranceRespawn.Value + origin}" : "none found"));
            foreach (var c in colliders) Log.Debug($"  entrance area '{c.transform.HierarchyPath()}' {c.bounds.Describe()}");
            if (trigger.walkTo != null) Log.Debug($"  walks the player to {trigger.walkTo.position}");

            // Other sensors the player is standing in, in case the sensor that fired the trigger
            // lives on another object (the link is scene data)
            foreach (var c in Physics2D.OverlapPointAll(playerPos)) {
                if (!c.isTrigger || colliders.Contains(c) || c.GetComponent<PlayerSensor>() == null) continue;
                Log.Debug($"  player also inside sensor '{c.transform.HierarchyPath()}' {c.bounds.Describe()}");
            }
        } catch (Exception e) {
            Log.Error($"ArenaTracker.OnWalkTrigger failed: {e}");
        }
    }

    // Where to respawn after dying in a boss fight, relative to the level root
    public static Vector3? FindBossRespawn(GameCore core, out string how) {
        var scene = core.gameLevel.SceneName;
        if (entranceRespawn.HasValue && entranceScene == scene) {
            how = $"before the fight entrance '{entranceName}'";
            return entranceRespawn;
        }

        var origin = core.gameLevel.transform.position;
        var bossAreas = Object.FindObjectsOfType<BossArea>()
            .Select(area => area.GetComponent<Collider2D>())
            .Where(c => c != null && c.enabled)
            .Select(c => Relative(c.bounds, origin))
            .ToList();
        if (bossAreas.Count > 0) {
            var outside = NewestTrailPointOutside(core, bossAreas);
            if (outside.HasValue) {
                how = "outside the boss area";
                return outside;
            }
        }

        if (SafeTrail.Scene == scene && SafeTrail.StartsAtEntrance && SafeTrail.Points.Count > 0) {
            how = "where the player entered the scene";
            return SafeTrail.Points[0];
        }

        how = "no fight entrance, boss area or scene entrance known";
        return null;
    }

    public static IEnumerable<string> Describe(GameCore core) {
        yield return entranceRespawn.HasValue
            ? $"last fight entrance '{entranceName}' in {entranceScene}, respawn point {entranceRespawn} (relative)"
            : "no fight entrance fired yet";
        foreach (var trigger in Object.FindObjectsOfType<PlayerWalkToTrigger>(true)) {
            var areas = string.Join(", ", trigger.GetComponentsInChildren<Collider2D>(true).Select(c => c.bounds.Describe()));
            yield return $"walk trigger '{trigger.transform.HierarchyPath()}' active={trigger.isActiveAndEnabled} " +
                         $"triggered={GamePrivates.WasTriggered(trigger)} areas=[{areas}] " +
                         $"walkTo={(trigger.walkTo != null ? trigger.walkTo.position.ToString() : "none")}";
        }
        foreach (var area in Object.FindObjectsOfType<BossArea>(true)) {
            var collider = area.GetComponent<Collider2D>();
            yield return $"boss area '{area.transform.HierarchyPath()}' active={area.isActiveAndEnabled} " +
                         $"bounds={(collider != null ? collider.bounds.Describe() : "none")}";
        }
    }

    private static Vector3? NewestTrailPointOutside(GameCore core, List<Bounds> areas) {
        if (SafeTrail.Scene != core.gameLevel.SceneName) return null;
        var trail = SafeTrail.Points;
        for (var i = trail.Count - 1; i >= 0; i--) {
            var point = trail[i];
            if (!areas.Any(area => IsInside(area, point))) return point;
        }
        return null;
    }

    private static bool IsInside(Bounds area, Vector3 point) =>
        point.x >= area.min.x - Margin && point.x <= area.max.x + Margin &&
        point.y >= area.min.y - Margin && point.y <= area.max.y + Margin;

    private static Bounds Relative(Bounds bounds, Vector3 origin) => new(bounds.center - origin, bounds.size);
}
