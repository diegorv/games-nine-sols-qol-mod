using HarmonyLib;
using UnityEngine;

namespace NineSolsQoL.Features.Death.RespawnNearDeath;

// Every hook the respawn feature has into the game. The patches only forward to the code that does
// the work, so this file is the full list of what the feature changes in the game.
[HarmonyPatch]
internal static class RespawnNearDeathPatches {
    // The player just died. A prefix runs before the game clears the boss health bars and the
    // engaged monsters (PlayerDeadState.NormalDeadHandle).
    [HarmonyPatch(typeof(PlayerDeadState), nameof(PlayerDeadState.OnStateEnter))]
    [HarmonyPrefix]
    private static void BeforeDeath() => DeathSnapshot.Record();

    // Two seconds after the death the game revives the player, normally at the last Root Node.
    // Returning false skips the game's revive because the mod did its own.
    [HarmonyPatch(typeof(PlayerDeadState), "Revive")]
    [HarmonyPrefix]
    private static bool BeforeRevive(PlayerDeadState __instance) => !RespawnController.TryRespawn(__instance);

    // Something stood on safe ground (also written by level resets and ForceSave)
    [HarmonyPatch(typeof(SafeGroundRecorder), nameof(SafeGroundRecorder.LastSafeGroundPosition), MethodType.Setter)]
    [HarmonyPostfix]
    private static void AfterSafeGround(SafeGroundRecorder __instance, Vector3 value) => SafeTrail.Record(__instance, value);

    // A scene was loaded (not called for seamlessly loaded neighbour scenes)
    [HarmonyPatch(typeof(GameCore), nameof(GameCore.SceneLoaded))]
    [HarmonyPostfix]
    private static void AfterSceneLoaded(GameLevel newLevel) {
        SafeTrail.OnSceneLoaded(newLevel);
        ArenaTracker.OnSceneLoaded(newLevel);
    }

    // The player stepped into a fight entrance: the game takes control and walks the player in
    [HarmonyPatch(typeof(PlayerWalkToTrigger), nameof(PlayerWalkToTrigger.Triggered))]
    [HarmonyPrefix]
    private static void BeforeWalkTrigger(PlayerWalkToTrigger __instance) => ArenaTracker.OnWalkTrigger(__instance);
}
