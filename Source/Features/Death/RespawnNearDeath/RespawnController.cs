using System;
using RCGSetting;
using NineSolsQoL.Core;
using UnityEngine;

namespace NineSolsQoL.Features.Death.RespawnNearDeath;

// Decides, when the game is about to revive the player, whether the mod respawns the player
// itself or lets the game's own respawn run.
internal static class RespawnController {
    // Called instead of PlayerDeadState.Revive. True when the mod took care of the respawn.
    public static bool TryRespawn(PlayerDeadState deadState) {
        try {
            return Respawn(deadState);
        } catch (Exception e) {
            Log.Error($"Respawn failed, using the game's normal respawn: {e}");
            return false;
        }
    }

    private static bool Respawn(PlayerDeadState deadState) {
        var death = DeathSnapshot.Take();
        var core = GameAccess.CoreInLevel();
        if (core == null || death == null) {
            Log.Info("Normal respawn: no death recorded in a loaded level");
            return false;
        }

        var reason = WhyNormalRespawn(core, deadState, death);
        if (reason != null) {
            Log.Info($"Normal respawn: {reason}");
            return false;
        }

        return death.InBossFight ? RespawnAfterBossFight(core) : RespawnOnSafeGround(core, death);
    }

    // Cases where the game's own respawn must run, or null when the mod can take over
    private static string? WhyNormalRespawn(GameCore core, PlayerDeadState deadState, DeathSnapshot death) {
        if (!RespawnNearDeathFeature.Enabled.Value) return "disabled in the config";
        if (DebugSetting.IsDebugMode) return "game debug mode";
        if (deadState.CanQuickRevive()) return "this map has its own quick revive";
        if (VRChallengeConfig.Instance.IsChallengingVR) return "VR challenge";
        if (core.playerGameData.memoryMode.CurrentValue) return "memory mode";
        if (core.IsInLeaderboardChallengeMode) return "leaderboard challenge";
        if (core.tutorialSettings.FindNextTutorialEntry() != null) return "the game respawns you into a tutorial";
        if (core.currentCoreState != GameCore.GameCoreState.Playing) return $"game state {core.currentCoreState}";
        if (death.Level != core.gameLevel) return "the level changed since the death";
        return null;
    }

    private static bool RespawnOnSafeGround(GameCore core, DeathSnapshot death) {
        if (death.SafeGround == Vector3.zero) {
            Log.Info("Normal respawn: no safe ground recorded");
            return false;
        }

        Log.Info($"Respawning on the last safe ground {death.SafeGround} (died at {death.Position})");
        Respawner.QuickReviveAt(core, death.SafeGround);
        return true;
    }

    // A quick revive leaves a boss half reset (Boss_SpearHorse_Logic switches its fight entrance off
    // and nothing switches it back on), so the scene is reloaded instead
    private static bool RespawnAfterBossFight(GameCore core) {
        if (!RespawnNearDeathFeature.BossRespawnOutsideArena.Value) {
            Log.Info("Normal respawn: boss respawn disabled in the config");
            return false;
        }

        var point = ArenaTracker.FindBossRespawn(core, out var how);
        if (!point.HasValue) {
            Log.Info($"Normal respawn: died in a boss fight but found no respawn point ({how})");
            return false;
        }

        Log.Info($"Died in a boss fight: reloading {core.CurrentSceneName} with the player at " +
                 $"{point.Value + core.gameLevel.transform.position} ({how})");
        Respawner.ReloadSceneAt(core, point.Value);
        return true;
    }
}
