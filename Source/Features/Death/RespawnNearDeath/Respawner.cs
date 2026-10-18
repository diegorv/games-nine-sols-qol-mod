using System;
using Cysharp.Threading.Tasks;
using NineSolsQoL.Core;
using UnityEngine;

namespace NineSolsQoL.Features.Death.RespawnNearDeath;

// Carries out the respawns decided by RespawnController: a quick revive in the same scene for
// ordinary deaths, a scene reload for boss deaths
internal static class Respawner {
    private const float FadeTime = 0.2f;

    // The game's own quick revive (PlayerDeadState.Revive, used on the few maps that have it) at
    // another position, and without its call to GameCore.FetchReviveData: that call leaves
    // GameCore._lastTeleportPoint set, and the next door into the Root Node's scene would then
    // play the Root Node's revive animation and leave the player hidden and without control.
    public static void QuickReviveAt(GameCore core, Vector3 position) {
        // What the next scene load would do otherwise: the game's death penalty, which zeroes the
        // carried Jin and XP (the death bag holds them now) unless NoDeathPenalty keeps them. Calling
        // the game's own method keeps NoDeathPenalty and other no-penalty mods, which patch it, working.
        var penalty = core.player.IsNeedDeathPenalty;
        if (penalty) core.playerGameData.PlayerDeathPenalty();
        Log.Debug($"Death penalty {(penalty ? "applied with the game's PlayerDeathPenalty" : "not needed")}");

        core.FadeToBlack(FadeTime, () => FinishQuickRevive(position), 0f).Forget();
    }

    // Runs when the screen is black
    private static void FinishQuickRevive(Vector3 position) {
        var core = GameAccess.CoreInLevel();
        if (core == null) return; // left the level during the fade

        var player = core.player;
        try {
            // Monsters and level back to their start; also spawns the death bag. The reset writes
            // where the player died as safe ground, which must not go into the trail.
            SafeTrail.Paused = true;
            try {
                core.ResetLevel(false);
            } finally {
                SafeTrail.Paused = false;
            }
            player.RestoreEverything();
            player.ChangeState(PlayerStateType.Normal);
            player.SetPosition(position, true);
            player.ForceOnGround();
            // The player is under the ResetManager, so ResetLevel moved the last safe ground to
            // where the player died. Put it back, or the next death would respawn in the same pit.
            player.SafeGroundRecorder.ForceSave(position);
            player.IsJustDead = false;

            core.gameLevel.UpdateCameraBounds();
            if (CameraManager.IsAvailable()) CameraManager.Instance.ResetCamera2DDockerToPlayer();
            core.soundManager.ambienceManager.BGMBackToNormalState();
        } catch (Exception e) {
            Log.Error($"Quick revive failed, reloading at the last Root Node: {e}");
            player.RespawnAtSavePoint();
            return;
        }

        core.FadeOutBlack(FadeTime, 1f, null);
        Log.Info($"Respawned at {position}");
        DebugTools.Watch("quick revive", 5f);
    }

    // Reloads the current scene with the player at levelPosition (relative to the level root), the
    // way the game's own GameCore.ReloadScene console command does, so everything in it starts over,
    // the boss fight included. The scene load applies the death penalty and spawns the death bag,
    // as after a normal death.
    public static void ReloadSceneAt(GameCore core, Vector3 levelPosition) {
        GamePrivates.ClearLastTeleportPoint(core);

        var data = new SceneConnectionPoint.ChangeSceneData {
            sceneName = core.gameLevel.gameObject.scene.name,
            playerSpawnPosition = () => SpawnPosition(levelPosition),
            ChangedDoneEvent = core.player.AllFull, // like GameCore.SpawnAtSavePoint
        };
        data.panData.panType = SceneConnectionPoint.CameraPanType.NoPan;

        core.ChangeScene(data, true, false, 0f).Forget();
        DebugTools.Watch("boss reload", 15f);
    }

    // Called by the game once the scene is loaded again
    private static Vector3 SpawnPosition(Vector3 levelPosition) {
        var core = GameAccess.CoreInLevel();
        var origin = core != null ? core.gameLevel.transform.position : Vector3.zero;
        return levelPosition + origin;
    }
}
