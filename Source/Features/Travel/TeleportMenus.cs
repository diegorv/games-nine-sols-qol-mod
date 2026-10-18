namespace NineSolsQoL.Features.Travel;

// The parts of the game's teleport menus both Travel features work with. Which conditions sit on which button
// or tab is prefab data, seen in game with an F9 dump:
//  - Root Node menu (GameCore.savePanelUiController), button "Teleport 神遊" (SavePanel.TeleportButton), each
//    condition on a child of the button: "神遊已解鎖" (GameFlagPropertyCondition: the story unlocked teleporting),
//    "不在最後階段" (GameFlagPropertyCondition, inverted: not in the final stage), "match YiBase"
//    (TeleportPointMatchSavePanelCondition with the Pavilion's node) and "NotMemoryChallengeMode" (inverted).
//  - Root Node menu, button "GoToAG 回議會" (Pavilion): a TeleportToSavePointAction to the Pavilion with the flute
//    cutscene; the home flute, "not match YiBase" (inverted), TreeGotMadCondition and PlayerInPrisionCondition
//    (both inverted), the final stage and memory challenges; its action has the same conditions.
//  - Pause menu (UIManager.menuUI.tabsUI), tab "TeleportPanel Tab" (bindPanel = UIManager.teleportPanel):
//    PlayerSitAtSavePointCondition, CurrentTeleportPointActivateCondition, "[Ability] 神遊Activated"
//    (AbilityActivatedCondition: the teleport ability), the final stage, "match YiBase" and memory challenges.
// The game evaluates a button's conditions when it opens the Root Node menu (RCGUIComponentResetGroup.OnEnable ->
// UIControlButton.EnableCheck) and a tab's on every tab change (TabsUI.PrepareValidTab), and needs all of them.
internal static class TeleportMenus {
    // The Root Node the player sits at, with its menu or a pause menu opened from it, or null.
    // SavePanel.CurrentSavePoint alone goes stale (only a teleport clears it), so the Root Node bound in the scene
    // must be the same one. Its flute cutscene is what the teleport plays first (TeleportPointButton).
    public static TeleportPointData? SittingNode(GameCore core) {
        var player = core.player;
        if (player == null || player.CurrentStateType != PlayerStateType.EnterSavePoint) return null;

        var panel = core.savePanelUiController;
        var bound = panel.CurrentSavePointGameObjectOnScene;
        var node = panel.CurrentSavePoint;
        if (bound == null || node == null || bound.BindingTeleportPoint != node || bound.fluteCutScene == null) {
            return null;
        }
        return node;
    }

    // Whether the condition is one of the Root Node menu's Teleport button's (on the button or a direct child, the
    // ones UIControlButton checks)
    public static bool IsOnTeleportButton(AbstractConditionComp condition, GameCore core) {
        var button = core.savePanelUiController.TeleportButton;
        if (button == null) return false;
        var transform = condition.transform;
        return transform == button.transform || transform.parent == button.transform;
    }

    // Whether the condition is one of the pause menu's Teleport tab's (at any depth, the ones UITabsItem checks)
    public static bool IsOnTeleportTab(AbstractConditionComp condition) {
        var tab = condition.GetComponentInParent<UITabsItem>(true);
        return tab != null && IsTeleportTab(tab);
    }

    // The pause menu's Teleport tab, or null
    public static UITabsItem? TeleportTab() {
        if (!UIManager.IsAvailable()) return null;
        var menu = UIManager.Instance.menuUI;
        if (menu == null || menu.tabsUI == null) return null;
        foreach (var tab in menu.tabsUI.AllItems) {
            if (tab != null && IsTeleportTab(tab)) return tab;
        }
        return null;
    }

    // The teleport ability the Teleport tab checks, or null
    public static PlayerAbilityData? TeleportAbility() {
        var tab = TeleportTab();
        if (tab == null) return null;
        foreach (var condition in tab.GetComponentsInChildren<AbilityActivatedCondition>(true)) {
            if (condition.ability.AbilityData != null) return condition.ability.AbilityData;
        }
        return null;
    }

    // Whether the Root Node menu's Pavilion button is valid right now: the button whose action teleports to the
    // Pavilion's node with the flute cutscene, its conditions and its action's, evaluated now (never cached).
    // Evaluating them runs this feature's postfix again for the button's own "not match YiBase", which is inverted
    // and not on the Teleport button, so it is left alone.
    public static bool PavilionButtonValid(GameCore core) {
        var pavilion = core.allTeleportPoints != null ? core.allTeleportPoints.AGSavePoint : null;
        if (pavilion == null) return false;

        foreach (var action in core.savePanelUiController.GetComponentsInChildren<TeleportToSavePointAction>(true)) {
            if (action.teleportPointData != pavilion ||
                action.teleportCutscene != TeleportToSavePointAction.TeleportCutscene.GoHomeFlute) {
                continue;
            }
            var button = action.GetComponentInParent<UIControlButton>(true);
            return button != null && button.IsAllValid && action.conditions.IsAllValid();
        }
        return false;
    }

    private static bool IsTeleportTab(UITabsItem tab) =>
        tab.PanelType == PlayerInfoPanelType.TeleportPanel && UIManager.IsAvailable() &&
        tab.bindPanel != null && tab.bindPanel == UIManager.Instance.teleportPanel;
}
