using System;
using NineSolsQoL.Core;
using UnityEngine;

namespace NineSolsQoL.Features.Death.RespawnNearDeath;

// What the mod needs to know about a death, recorded the moment the player dies. Right after
// that the game clears the boss health bars and the engaged monsters, and the level reset before
// the respawn moves the last safe ground to where the player died.
internal sealed class DeathSnapshot {
    public GameLevel Level = null!;
    public Vector3 Position;
    public Vector3 SafeGround;
    public bool InBossFight;

    // Recorded on death, taken by the revive two seconds later
    private static DeathSnapshot? pending;

    // Runs inside the game's death handling: it must never throw
    public static void Record() {
        pending = null;
        try {
            var core = GameAccess.CoreInLevel();
            if (core == null) return;

            var player = core.player;
            pending = new DeathSnapshot {
                Level = core.gameLevel,
                Position = player.transform.position,
                SafeGround = player.lastSafeGroundPosition,
                InBossFight = GameAccess.IsBossFightActive(core),
            };
            // The death bag gets carried Jin/XP times DeathPenaltyRate (PlayerDeadRecord.StorePlayerDataBeforeDead)
            if (Log.DebugEnabled) {
                Log.Debug($"Died in {core.CurrentSceneName} at {pending.Position}: last safe ground {pending.SafeGround}, " +
                          $"boss fight {pending.InBossFight}, carrying Jin {core.playerGameData.CurrentGold} " +
                          $"XP {core.playerGameData.CurrentExp}, death bag rate {player.statData.DeathPenaltyRate.Value}");
            }
        } catch (Exception e) {
            pending = null;
            Log.Error($"DeathSnapshot.Record failed: {e}");
        }
    }

    // The snapshot of the current death, only once
    public static DeathSnapshot? Take() {
        var death = pending;
        pending = null;
        return death;
    }
}
