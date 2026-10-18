using System.Collections.Generic;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.AirDash;

// Dash in the air before the game's own air dash upgrade.
//
// The game already has an air dash: Player.DodgeCheck lets Yi dash while airborne once the
// RollDodgeInAirUpgrade ability is active, once per air time (CanRoll needs airDodgeCount == 0, which
// PlayerRollState raises on an air dash and ResetJumpAndDash clears on landing, walls, ledges, ropes,
// grapples, parries, jump-kick bounces and talisman sticks). AirDashPatches makes only that check pass
// while the feature is on: the ability itself stays as it is, so the save, the skill tree and the rest
// of the game still see the upgrade as not unlocked. Once the game unlocks it, the feature changes
// nothing.
internal static class AirDashFeature {
    public static ConfigEntry<bool> Enabled = null!;

    // Set by the transpiler when it found the check
    public static bool Patched;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "AirDash");
        Enabled = section.Bind("Enabled", false,
            "Dash in the air before the game gives you its air dash upgrade: once per jump, recharged when you " +
            "land, touch a wall, ledge or rope, parry, and by everything else that recharges it in the game. " +
            "Off: the game's rule, no air dash until the upgrade");

        DebugTools.AddDumpSection(Describe);
    }

    // Called by Player.DodgeCheck in place of reading RollDodgeInAirUpgrade.IsActivated, when Yi is
    // airborne and could dash otherwise. Runs inside the game's input handling; only the game's own
    // getter can throw, as it would without the mod.
    public static bool AirDashUnlocked(PlayerAbilityData upgrade) {
        // The game's getter still runs: it applies or removes the ability's effects when its state changes
        if (upgrade.IsActivated) return true;
        if (!Enabled.Value) return false;

        Log.Debug("[air dash] allowed by AirDash: the game's air dash upgrade is not unlocked yet");
        return true;
    }

    // Whether the game's upgrade is unlocked, read without IsActivated (which applies its effects)
    private static IEnumerable<string> Describe(GameCore core) {
        var upgrade = core.playerAbilityCollection != null ? core.playerAbilityCollection.RollDodgeInAirUpgrade : null;
        var state = upgrade != null ? $"acquired={upgrade.IsAcquired} equipped={upgrade.IsEquipped}" : "missing";
        return new[] {
            $"AirDash: enabled={Enabled.Value} patched={Patched} game's upgrade {state} " +
            $"airDodgeCount={core.player.airDodgeCount}",
        };
    }
}
