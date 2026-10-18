using System.Collections.Generic;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.Abilities.EarlyAirDash;

// Dash in the air before the game's own air dash upgrade.
//
// The game already has an air dash: Player.DodgeCheck lets Yi dash while airborne once the
// RollDodgeInAirUpgrade ability is active, once per air time (CanRoll needs airDodgeCount == 0, which
// PlayerRollState raises on an air dash and ResetJumpAndDash clears on landing, wall slides, ledges,
// ropes, grapples, parries, jump-kick bounces and talisman sticks; a double jump does not). The one
// exception is the game's own: with the Down Dash jade, down dashes are never limited (CanDownDash).
// EarlyAirDashPatches makes only that check pass while the upgrade is not acquired yet: the ability itself
// stays as it is, so the save, the skill tree and the rest of the game still see it as not unlocked.
// Once the upgrade is acquired the game's own rules decide again, including when the game turns it off
// on purpose (ability packs of challenges, unequip actions, conditions).
internal static class EarlyAirDashFeature {
    public static ConfigEntry<bool> Enabled = null!;

    // Set by the transpiler when it found the check
    public static bool Patched;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "EarlyAirDash", MenuCategory.Abilities, "Early air dash");
        Enabled = section.Bind("Enabled", false,
            "Dash in the air before the game gives you its air dash upgrade: once per air time (a double jump " +
            "does not give another), recharged when you land, slide on a wall, grab a ledge or rope, parry and " +
            "the like, as the game does after the upgrade. Off: the game's rule, no air dash until the upgrade");

        DebugTools.AddDumpSection(Describe);
    }

    // Called by Player.DodgeCheck in place of reading RollDodgeInAirUpgrade.IsActivated, when Yi is
    // airborne and every earlier check passed (the QTE check still follows). Runs inside the game's input
    // handling; only the game's own getter can throw, as it would without the mod.
    public static bool AirDashUnlocked(PlayerAbilityData upgrade) {
        // The game's getter still runs: it applies or removes the ability's effects when its state changes
        if (upgrade.IsActivated) return true;
        // Once acquired, an inactive upgrade is the game's decision (a plain read of the acquired flag)
        if (!Enabled.Value || upgrade.IsAcquired) return false;

        Log.Debug("[air dash] the game's air dash check passed because of EarlyAirDash (upgrade not acquired yet)");
        return true;
    }

    // The game's upgrade, read without IsActivated (which applies its effects): acquired decides whether
    // the feature acts
    private static IEnumerable<string> Describe(GameCore core) {
        var upgrade = core.playerAbilityCollection != null ? core.playerAbilityCollection.RollDodgeInAirUpgrade : null;
        var state = upgrade != null ? $"acquired={upgrade.IsAcquired} equipped={upgrade.IsEquipped}" : "missing";
        return new[] {
            $"EarlyAirDash: enabled={Enabled.Value} patched={Patched} game's upgrade {state} " +
            $"airDodgeCount={core.player.airDodgeCount}",
        };
    }
}
