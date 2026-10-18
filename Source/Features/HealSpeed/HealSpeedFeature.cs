using System.Collections.Generic;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.HealSpeed;

// Heal with the pipe at the Quick Dose jade's speed without the jade.
//
// The game has one faster heal: PlayerDrinkPotionState.OnStateEnter plays the whole heal animation
// 1.66666 times faster (animator.speed) when the state's SpeedUpHealJade ability is active; the heal
// itself comes from the animation's Done event, and OnStateExit sets the speed back to 1.
// HealSpeedPatches makes that check also pass while the feature is on; the jade itself is not touched,
// and with the jade equipped the speed is the same.
internal static class HealSpeedFeature {
    public static ConfigEntry<bool> Enabled = null!;

    // Set by the transpiler when it found the check
    public static bool Patched;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "HealSpeed");
        Enabled = section.Bind("Enabled", true,
            "Heal with the pipe as fast as with the Quick Dose jade, without equipping it: the animation plays " +
            "1.67 times faster, so a heal takes 60% of the time. With the jade equipped nothing changes. Off: " +
            "the game's speed, faster only with the jade");

        DebugTools.AddDumpSection(Describe);
    }

    // Called by PlayerDrinkPotionState.OnStateEnter in place of its private isSpeedUpHeal, which reads
    // the jade's IsActivated. Only the game's own getter can throw, as it would without the mod.
    public static bool SpeedUpHeal(PlayerDrinkPotionState state) {
        // The jade's getter still runs: it applies or removes the jade's effects when its state changes
        if (state.SpeedUpHealJade.IsActivated) return true;
        if (!Enabled.Value) return false;

        Log.Debug("[heal] at the Quick Dose jade's speed, without the jade");
        return true;
    }

    // The jade the heal state checks, read without IsActivated (which applies its effects)
    private static IEnumerable<string> Describe(GameCore core) {
        var state = core.player.GetState<PlayerDrinkPotionState>(PlayerStateType.DrinkPotion);
        var jade = state != null ? state.SpeedUpHealJade.AbilityData : null;
        var jadeState = jade != null ? $"'{jade.name}' acquired={jade.IsAcquired} equipped={jade.IsEquipped}" : "none";
        return new[] { $"HealSpeed: enabled={Enabled.Value} patched={Patched} jade {jadeState}" };
    }
}
