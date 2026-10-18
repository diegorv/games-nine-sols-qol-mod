using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.Travel.TeleportFromAnyNode;

// Teleport from every Root Node that shows the Pavilion button, not just from the Pavilion's.
//
// The Root Node menu's Teleport button and the pause menu's Teleport tab need, among other conditions, a
// TeleportPointMatchSavePanelCondition with the Pavilion's node: "this Root Node is the Pavilion" (see
// TeleportMenus). The teleport itself (TeleportPointButton.SubmitImplementation) already works from any Root Node
// the player sits at: it plays that node's flute cutscene, as the Pavilion button does. TeleportFromAnyNodePatches
// lets that one condition pass, for the Teleport button and tab only, while the player sits at another Root Node
// whose Pavilion button is valid at that moment. So the game's story locks stay: where the game hides the Pavilion
// button (the Prison, after Lady Ethereal until the double jump, the final stage, memory challenges) the Teleport
// stays hidden too, and the feature only reaches what the game reaches through the Pavilion. Every other
// condition still applies, such as the story's teleport unlock (EarlyTeleport). The node the player leaves becomes
// the Last Node, as with the Pavilion button: a trip ends as the game's route through the Pavilion would
// (respawn at the destination, Last Node at the node left).
internal static class TeleportFromAnyNodeFeature {
    public static ConfigEntry<bool> Enabled = null!;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "TeleportFromAnyNode", MenuCategory.Travel, "Teleport from any node");
        Enabled = section.Bind("Enabled", true,
            "Teleport from every Root Node that shows the Pavilion button, not just from the Pavilion's: its Teleport " +
            "button opens the list of Root Nodes. The node you leave becomes the Last Node, as when you go to the " +
            "Pavilion. Needs the game's teleport, which the story unlocks later on (or Early teleport). Off: the " +
            "game's rule, teleport only from the Pavilion's Root Node");

        DebugTools.AddDumpSection(Describe);
    }

    // Called when a TeleportPointMatchSavePanelCondition is false: true makes it pass. Runs inside the game's menu
    // code (EnableCheck, PrepareValidTab), so it must not throw.
    public static bool ShowTeleport(TeleportPointMatchSavePanelCondition condition) {
        try {
            // Making an inverted one pass would hide what it guards (the Pavilion button has one)
            if (!Enabled.Value || condition.FinalResultInverted) return false;
            var core = GameAccess.CoreInLevel();
            if (core == null) return false;
            var points = core.allTeleportPoints;
            if (core.savePanelUiController == null || points == null ||
                condition.bindedTeleportPointData != points.AGSavePoint) {
                return false;
            }

            // The cheap check first: the tab check walks the hierarchy
            var node = TeleportMenus.SittingNode(core);
            if (node == null || node == points.AGSavePoint) return false;

            var onButton = TeleportMenus.IsOnTeleportButton(condition, core);
            if (onButton ? !TeleportMenus.TabMenuAvailable() : !TeleportMenus.IsOnTeleportTab(condition)) return false;
            if (!TeleportMenus.PavilionButtonValid(core)) return false;

            Log.Debug($"[teleport] the {(onButton ? "Teleport button" : "Teleport tab")} may show at '{node.name}' " +
                      "(its Pavilion button is shown)");
            return true;
        } catch (Exception e) {
            Log.Error($"TeleportFromAnyNode failed, the Teleport stays as the game decides: {e}");
            return false;
        }
    }

    // Called before TeleportPointButton.SubmitImplementation, which starts the teleport and clears the Root Node
    // menu's node. Stores the node the player leaves as the Last Node, as the Pavilion button does
    // (GameCore.TeleportToSavePoint with SaveLastItem). Not from the Pavilion, which the game never stores as the
    // Last Node (TeleportPointCollection.isValidLastSavedFlag).
    public static void RememberNodeLeft() {
        try {
            if (!Enabled.Value) return;
            var core = GameAccess.CoreInLevel();
            if (core == null) return;
            var points = core.allTeleportPoints;
            var node = core.savePanelUiController != null ? TeleportMenus.SittingNode(core) : null;
            if (node == null || points == null || node == points.AGSavePoint) return;

            // SetLastSaveItem writes the save flag first, then looks the node up and throws if it is missing
            if (string.IsNullOrEmpty(node.FinalSaveID) || !points.gameFlagDataList.Contains(node)) {
                Log.Warning($"TeleportFromAnyNode: '{node.name}' is not in the game's list of Root Nodes, so it does " +
                            "not become the Last Node");
                return;
            }
            points.SetLastSaveItem(node);
            Log.Debug($"[teleport] '{node.name}' is the Last Node now");
        } catch (Exception e) {
            Log.Error($"TeleportFromAnyNode could not store the Last Node: {e}");
        }
    }

    private static IEnumerable<string> Describe(GameCore core) {
        var node = TeleportMenus.SittingNode(core);
        var button = core.savePanelUiController.TeleportButton;
        var lastNode = core.allTeleportPoints != null ? core.allTeleportPoints.LastSavedSavePoint : null;
        return new[] {
            $"TeleportFromAnyNode: enabled={Enabled.Value} sitting at {(node != null ? $"'{node.name}'" : "no Root Node")} " +
            $"pavilion button valid={(node != null ? TeleportMenus.PavilionButtonValid(core).ToString() : "-")} " +
            $"teleport button shown={button != null && button.gameObject.activeInHierarchy} " +
            $"last node={(lastNode != null ? $"'{lastNode.name}'" : "none")}",
        };
    }
}
