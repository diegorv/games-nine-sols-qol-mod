using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.Combat.ParryFromBehind;

// Parry attacks that come from behind Yi.
//
// Before a hit can be parried, the game calls FacingRuleCheck(DamageDealer, Vector3, Vector2) on the
// parry dealer of the Health being hit (Health.parryDealer). When the attack or the parry requires
// FaceToFace, it fails unless the defender faces the attack or holds the stick toward it. Its only
// callers are DamageReceiver.IsHitDamageNotParried and ParriableAttackEffect.ParryCheckForEvent;
// the mod only changes the result for the player's own parry dealer. Every later check still
// applies: the parry input (or the game's auto parry) and attacks that cannot be parried at all.
internal static class ParryFromBehindFeature {
    public static ConfigEntry<bool> Enabled = null!;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "ParryFromBehind", MenuCategory.Combat, "Parry from behind");
        Enabled = section.Bind("Enabled", true,
            "Parry attacks that come from behind Yi. Off: the game's rule, where on the ground, walls, " +
            "ledges and ropes some attacks can only be parried while Yi faces them or the stick points " +
            "toward them (in mid-air, away from walls, ledges and ropes, the game already ignores facing). " +
            "Attacks that cannot be parried at all stay that way");

        DebugTools.AddDumpSection(Describe);
    }

    // Called after the game's facing check failed: true lets the parry go on. Runs inside the
    // game's hit handling, so it must not throw.
    public static bool AllowFromBehind(EffectDealer checker, DamageDealer attack) {
        try {
            if (!Enabled.Value) return false;

            // Only the player's own parry dealer, never another character's Health
            var player = Player.i;
            var health = player != null ? player.health : null;
            if (health == null || !ReferenceEquals(checker, health.parryDealer)) return false;

            if (Log.DebugEnabled) {
                Log.Debug($"[parry direction] '{attack.name}' can be parried from behind (attack rule " +
                          $"{attack.facingDetectionRule}, parry rule {checker.facingDetectionRule})");
            }
            return true;
        } catch (Exception e) {
            Log.Error($"ParryFromBehind failed, using the game's facing rule: {e}");
            return false;
        }
    }

    // sameAsParryState tells whether the Parry state's parryEffectDealer, whose facing rule the state
    // sets on enter, is the dealer the facing check uses (that link only exists in the player prefab)
    private static IEnumerable<string> Describe(GameCore core) {
        var player = core.player;
        var dealer = player.health != null ? player.health.parryDealer : null;
        if (dealer == null) return new[] { $"ParryFromBehind: enabled={Enabled.Value} no parry dealer" };

        var state = player.GetState<PlayerParryState>(PlayerStateType.Parry);
        var shared = state != null && ReferenceEquals(dealer, state.parryEffectDealer);
        return new[] {
            $"ParryFromBehind: enabled={Enabled.Value} parryDealer='{dealer.transform.HierarchyPath()}' " +
            $"rule={dealer.facingDetectionRule} sameAsParryState={shared}",
        };
    }
}
