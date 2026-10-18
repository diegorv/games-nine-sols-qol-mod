using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using NineSolsQoL.Core;
using RCGMaker.Core;

namespace NineSolsQoL.Features.Travel.EarlyTeleport;

// Use the game's teleport before the story unlocks it.
//
// The Root Node menu's Teleport button needs the story's teleport unlock flag ("神遊已解鎖", a
// GameFlagPropertyCondition) and the pause menu's Teleport tab needs the teleport ability ("[Ability] 神遊Activated",
// an AbilityActivatedCondition), see TeleportMenus. EarlyTeleportPatches lets those two conditions pass while the
// teleport ability is not acquired yet. Nothing is written: the save, the skill tree and the story still see
// teleporting as locked, and once the game gives the ability its own rules decide again. Every other condition stays,
// so without TeleportFromAnyNode the Teleport only shows at the Pavilion's Root Node, and the destinations are still
// the Root Nodes the player has activated.
internal static class EarlyTeleportFeature {
    public static ConfigEntry<bool> Enabled = null!;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "EarlyTeleport", MenuCategory.Travel, "Early teleport");
        Enabled = section.Bind("Enabled", false,
            "Use the game's teleport before the story unlocks it: the Teleport button shows at the Pavilion's Root " +
            "Node, and with Teleport from any node at every Root Node that shows the Pavilion button. You can only go " +
            "to Root Nodes you have activated. Nothing is unlocked in your save, and once the game gives you the " +
            "teleport its own rules apply. Off: the game's rule, no teleport until the story unlocks it");

        DebugTools.AddDumpSection(Describe);
    }

    // Called when an AbilityActivatedCondition is false: true makes it pass. Runs wherever the game checks an ability
    // with a condition (menus, level logic), so the cheap checks come first, and it must not throw.
    public static bool UnlockTab(AbilityActivatedCondition condition) {
        try {
            // Making an inverted one pass would hide what it guards
            if (!Enabled.Value || condition.FinalResultInverted) return false;
            var ability = condition.ability.AbilityData;
            if (ability == null || ability.IsAcquired || !TeleportMenus.IsOnTeleportTab(condition)) return false;

            Log.Debug($"[early teleport] the Teleport tab may show: '{ability.name}' is not acquired yet");
            return true;
        } catch (Exception e) {
            Log.Error($"EarlyTeleport failed, the Teleport tab stays as the game decides: {e}");
            return false;
        }
    }

    // Called when a GameFlagPropertyCondition is false: true makes it pass. Runs wherever the game checks a flag with
    // a condition, so the cheap checks come first, and it must not throw.
    public static bool UnlockButton(GameFlagPropertyCondition condition) {
        try {
            // The Teleport button also has an inverted one ("not in the final stage") that must stay as it is
            if (!Enabled.Value || condition.FinalResultInverted || !GameCore.IsAvailable()) return false;
            var core = GameCore.Instance;
            if (core.savePanelUiController == null || !TeleportMenus.IsOnTeleportButton(condition, core) ||
                !TeleportMenus.TabMenuAvailable()) {
                return false;
            }
            // Only the unlock flag: a field of the teleport ability itself, read as it is (seen in game: its
            // acquired field)
            var ability = TeleportMenus.TeleportAbility();
            var flag = condition.FieldBool;
            if (ability == null || ability.IsAcquired || flag == null || flag.IsResultInverted ||
                (UnityEngine.Object?)flag.flagBase != ability) {
                return false;
            }

            Log.Debug($"[early teleport] the Teleport button may show: '{ability.name}' is not acquired yet");
            return true;
        } catch (Exception e) {
            Log.Error($"EarlyTeleport failed, the Teleport button stays as the game decides: {e}");
            return false;
        }
    }

    // The teleport ability and the unlock flag(s) the Teleport button checks, read without IsActivated (which applies
    // the ability's effects)
    private static IEnumerable<string> Describe(GameCore core) {
        var ability = TeleportMenus.TeleportAbility();
        var abilityState = ability != null ? $"'{ability.name}' acquired={ability.IsAcquired}" : "not found";
        var button = core.savePanelUiController.TeleportButton;
        var flags = button == null
            ? "no button"
            : string.Join(", ", button.GetComponentsInChildren<GameFlagPropertyCondition>(true)
                .Where(c => !c.FinalResultInverted && TeleportMenus.IsOnTeleportButton(c, core))
                .Select(c => $"'{c.name}' {DescribeFlag(c.FieldBool, ability)}"));
        return new[] { $"EarlyTeleport: enabled={Enabled.Value} ability {abilityState} unlock flag {flags}" };
    }

    private static string DescribeFlag(FlagFieldBoolEntry? entry, PlayerAbilityData? ability) {
        if (entry == null) return "none";
        var owner = (UnityEngine.Object?)entry.flagBase;
        return $"{(owner != null ? $"'{owner.name}'" : "none")}.{entry.fieldName}={entry.Value} " +
               $"inverted={entry.IsResultInverted} isTheAbility={owner != null && owner == ability}";
    }
}
