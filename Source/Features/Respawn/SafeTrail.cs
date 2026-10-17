using System;
using System.Collections.Generic;
using NineSolsQoL.Core;
using UnityEngine;

namespace NineSolsQoL.Features.Respawn;

// The safe ground the player walked on in the current scene, oldest first. Fed by every write to
// the player's SafeGroundRecorder.LastSafeGroundPosition: standing on safe ground, level resets
// (scene load: the spawn point; resting at a Root Node) and ForceSave.
//
// Positions are relative to the level root, like the positions the game saves itself (death bag,
// force save), so they stay valid when the scene is loaded again.
internal static class SafeTrail {
    private const int MaxPoints = 500;
    private const float MinSpacing = 16f;

    private static readonly List<Vector3> points = new();

    public static IReadOnlyList<Vector3> Points => points;

    public static string Scene { get; private set; } = "";

    // The first point is where the player entered the scene (the mod saw the scene load)
    public static bool StartsAtEntrance { get; private set; }

    // Set while the mod resets the level itself: that reset writes the death spot as safe ground
    public static bool Paused;

    public static void OnSceneLoaded(GameLevel level) {
        Restart(level.SceneName, startsAtEntrance: true);
    }

    public static void Record(SafeGroundRecorder recorder, Vector3 position) {
        if (Paused) return;
        try {
            var core = GameAccess.CoreInLevel();
            if (core == null || recorder != core.player.SafeGroundRecorder) return; // monsters can have one too

            var level = core.gameLevel;
            // The mod was loaded in the middle of the scene, or the player walked into a seamlessly
            // loaded neighbour scene
            if (level.SceneName != Scene) Restart(level.SceneName, startsAtEntrance: false);

            var point = position - level.transform.position;
            if (points.Count > 0 && Vector2.Distance(points[points.Count - 1], point) < MinSpacing) return;
            points.Add(point);
            if (points.Count > MaxPoints) points.RemoveAt(MaxPoints / 2); // keep the entrance and the latest steps
        } catch (Exception e) {
            Log.Error($"SafeTrail.Record failed: {e}");
        }
    }

    public static string Describe() {
        if (points.Count == 0) return $"trail in '{Scene}': empty";
        return $"trail in '{Scene}': {points.Count} points, startsAtEntrance={StartsAtEntrance}, " +
               $"first {points[0]}, last {points[points.Count - 1]} (relative to the level root)";
    }

    private static void Restart(string scene, bool startsAtEntrance) {
        Scene = scene;
        StartsAtEntrance = startsAtEntrance;
        points.Clear();
        Log.Debug($"Safe ground trail restarted in {scene} (startsAtEntrance={startsAtEntrance})");
    }
}
