using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NineSolsQoL.Core;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace NineSolsQoL.Features.Travel.TeleportFromAnyNode;

// One-off F9 dump for TeleportFromAnyNode: how the Root Node menu's buttons and the pause menu's tabs are
// wired. Which conditions sit on which button or tab, whether they are inverted and which actions a button
// runs live in the game's prefabs, not in its code. Read only: it evaluates the conditions the way the game
// does when it opens these menus. Remove it once the feature's design is settled.
internal static class TeleportFromAnyNodeDiagnostics {
    private static readonly Lazy<AccessTools.FieldRef<UIControlButton, AbstractConditionComp[]>?> ButtonConditions =
        new(() => GamePrivates.Find<UIControlButton, AbstractConditionComp[]>("_activateConditions"));

    private static readonly Lazy<AccessTools.FieldRef<UIControlButton, AbstractStateAction[]>?> ButtonActions =
        new(() => GamePrivates.Find<UIControlButton, AbstractStateAction[]>("_onSubmitActions"));

    private static readonly Lazy<AccessTools.FieldRef<UITabsItem, AbstractConditionComp[]>?> TabConditions =
        new(() => GamePrivates.Find<UITabsItem, AbstractConditionComp[]>("conditions"));

    public static void Init() {
        DebugTools.AddDumpSection(Describe);
    }

    private static IEnumerable<string> Describe(GameCore core) {
        var lines = new List<string>();
        Try(lines, "state", () => DescribeState(core, lines));
        Try(lines, "Root Node menu", () => DescribeRootNodeMenu(core.savePanelUiController, lines));
        Try(lines, "pause menu", () => DescribePauseMenu(lines));
        return lines;
    }

    private static void DescribeState(GameCore core, List<string> lines) {
        var player = core.player;
        var panel = core.savePanelUiController;
        var points = core.allTeleportPoints;
        var bound = panel.CurrentSavePointGameObjectOnScene;
        var menuNode = panel.CurrentSavePoint;
        var respawnNode = ApplicationCore.IsAvailable() ? ApplicationCore.Instance.lastSaveTeleportPoint : null;
        var tabMenu = UIManager.IsAvailable() ? Value(UIManager.Instance.IsTabMenuAvailable) : "no UIManager";
        var memoryMode = PlayerGamePlayData.Instance != null ? Value(PlayerGamePlayData.Instance.memoryMode) : "?";

        lines.Add($"Teleport: player state={player.CurrentStateType} sitAtSavePoint={player.IsSitAtSavePoint} " +
                  $"tabMenuAvailable={tabMenu} memoryMode={memoryMode}");
        lines.Add($"Teleport: menu node={Name(menuNode)} bound node={(bound != null ? bound.transform.HierarchyPath() : "none")} " +
                  $"its data={(bound != null ? Name(bound.BindingTeleportPoint) : "-")} " +
                  $"flute={(bound != null ? (bound.fluteCutScene != null).ToString() : "-")} " +
                  $"respawn node={Name(respawnNode)} last node={Name(points.LastSavedSavePoint)} " +
                  $"pavilion={Name(points.AGSavePoint)} " +
                  $"menu node in collection={(menuNode != null ? points.gameFlagDataList.Contains(menuNode).ToString() : "-")}");
    }

    private static void DescribeRootNodeMenu(SavePanel panel, List<string> lines) {
        var sameAsUIManager = UIManager.IsAvailable() ? (UIManager.Instance.SavePointUI == panel).ToString() : "?";
        lines.Add($"Teleport: Root Node menu '{panel.transform.HierarchyPath()}' state={panel.state} toMenu={panel.ToMenu} " +
                  $"sameAsUIManager.SavePointUI={sameAsUIManager} resetGroup={Path(panel.UICompResetGroup, panel.transform)} " +
                  $"TeleportButton field={Path(panel.TeleportButton, panel.transform)}");

        foreach (var button in panel.GetComponentsInChildren<UIControlButton>(true)) {
            Try(lines, $"button {button.name}", () => DescribeButton(button, panel.transform, lines));
        }
    }

    private static void DescribeButton(UIControlButton button, Transform root, List<string> lines) {
        lines.Add($"Teleport:   button {Path(button, root)} ({button.GetType().Name}) " +
                  $"activeSelf={button.gameObject.activeSelf} activeInHierarchy={button.gameObject.activeInHierarchy} " +
                  $"valid={Evaluate(() => button.IsAllValid)} onSubmit=[{Listeners(button.onSubmit)}]");

        var conditions = ButtonConditions.Value is { } conditionsField ? conditionsField(button) : null;
        DescribeConditions(conditions, button.transform, "    ", lines);

        var actions = ButtonActions.Value is { } actionsField ? actionsField(button) : null;
        if (actions == null) return;
        foreach (var action in actions) {
            if (action == null) continue;
            lines.Add($"Teleport:     action {Path(action, button.transform)} {action.GetType().Name} " +
                      $"activeSelf={action.gameObject.activeSelf} {Fields(action, typeof(AbstractStateAction))}");
            DescribeConditions(action.conditions, action.transform, "      ", lines);
        }
    }

    private static void DescribePauseMenu(List<string> lines) {
        if (!UIManager.IsAvailable()) {
            lines.Add("Teleport: pause menu: no UIManager");
            return;
        }

        var ui = UIManager.Instance;
        var menu = ui.menuUI;
        var tabs = menu.tabsUI;
        lines.Add($"Teleport: pause menu '{menu.transform.HierarchyPath()}' state={menu.state} " +
                  $"tabs={Path(tabs, menu.transform)} valid now=[{string.Join(", ", tabs.Items.Select(t => t != null ? t.name : "null"))}] " +
                  $"teleport panel={(ui.teleportPanel != null ? ui.teleportPanel.transform.HierarchyPath() : "none")}");

        foreach (var tab in tabs.AllItems) {
            if (tab == null) continue;
            Try(lines, $"tab {tab.name}", () => {
                var bindPanel = tab.bindPanel;
                lines.Add($"Teleport:   tab {Path(tab, tabs.transform)} panelType={tab.PanelType} " +
                          $"override={(tab.OverridePanelType != null ? tab.OverridePanelType.name : "none")} " +
                          $"bindPanel={(bindPanel != null ? bindPanel.name : "none")} " +
                          $"isTeleportPanel={bindPanel != null && bindPanel == ui.teleportPanel} " +
                          $"activeSelf={tab.gameObject.activeSelf} valid={Evaluate(() => tab.IsAllValid)}");
                var conditions = TabConditions.Value is { } field ? field(tab) : null;
                DescribeConditions(conditions, tab.transform, "    ", lines);
            });
        }
    }

    private static void DescribeConditions(AbstractConditionComp[]? conditions, Transform owner, string indent,
        List<string> lines) {
        if (conditions == null) return;
        foreach (var condition in conditions) {
            if (condition == null) {
                lines.Add($"Teleport: {indent}condition null");
                continue;
            }
            lines.Add($"Teleport: {indent}condition {Path(condition, owner)} {condition.GetType().Name} " +
                      $"inverted={condition.FinalResultInverted} activeSelf={condition.gameObject.activeSelf} " +
                      $"result={Evaluate(() => condition.FinalResult)} {Fields(condition, typeof(AbstractConditionComp))}");
        }
    }

    // The public fields a condition or action type adds to its base (flags, abilities, teleport points)
    private static string Fields(Component component, Type stopAt) {
        var parts = new List<string>();
        for (var type = component.GetType(); type != null && type != stopAt; type = type.BaseType) {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
                string value;
                try {
                    value = Value(field.GetValue(component));
                } catch (Exception e) {
                    value = $"threw {e.GetType().Name}";
                }
                parts.Add($"{field.Name}={value}");
            }
        }
        return string.Join(" ", parts);
    }

    // Flags (ScriptableDataBool and the like) and abilities are matched by their members, not their types:
    // their base type implements an interface from an assembly the mod does not reference
    private static string Value(object? value) {
        switch (value) {
            case null:
                return "null";
            case Object unityObject when unityObject == null:
                return "missing";
            case Object unityObject:
                if (Property(unityObject, "CurrentValue") is bool flag) return $"'{unityObject.name}':{flag}";
                if (Property(unityObject, "IsAcquired") is bool acquired) return $"'{unityObject.name}':acquired={acquired}";
                return $"'{unityObject.name}'";
            case string text:
                return $"\"{text}\"";
            // GameFlagPropertyCondition's flag: an asset and the name of one of its fields
            case FlagFieldBoolEntry entry:
                return $"{Value((object?)entry.flagBase)}.{entry.fieldName}={Evaluate(() => entry.Value)} " +
                       $"(inverted={entry.IsResultInverted})";
            // AbilityActivatedCondition's ability
            case AbilityWrapper wrapper:
                return Value((object?)wrapper.AbilityData);
            case System.Collections.ICollection collection:
                return $"{value.GetType().Name}[{collection.Count}]";
            default:
                return value.GetType().IsPrimitive || value.GetType().IsEnum ? value.ToString() : value.GetType().Name;
        }
    }

    // A public property by name, the most derived one (ScriptableDataBool.CurrentValue hides its base's)
    private static object? Property(object target, string name) {
        for (var type = target.GetType(); type != null; type = type.BaseType) {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(target);
        }
        return null;
    }

    // The persistent (prefab) listeners of a button's onSubmit; the menu adds its own at runtime
    private static string Listeners(UnityEventBase unityEvent) {
        var count = unityEvent.GetPersistentEventCount();
        return string.Join(", ", Enumerable.Range(0, count).Select(i => {
            var target = unityEvent.GetPersistentTarget(i);
            return $"{(target != null ? target.GetType().Name : "null")}.{unityEvent.GetPersistentMethodName(i)}";
        }));
    }

    private static string Evaluate(Func<bool> check) {
        try {
            return check().ToString();
        } catch (Exception e) {
            return $"threw {e.GetType().Name}";
        }
    }

    private static string Name(Object? value) => value != null ? $"'{value.name}'" : "none";

    // The object's path below root, or its full path when it is elsewhere
    private static string Path(Component? component, Transform root) {
        if (component == null) return "none";
        var transform = component.transform;
        if (transform == root) return "'.'";
        var path = transform.name;
        for (var parent = transform.parent; parent != null; parent = parent.parent) {
            if (parent == root) return $"'{path}'";
            path = parent.name + "/" + path;
        }
        return $"'{path}'";
    }

    private static void Try(List<string> lines, string part, Action describe) {
        try {
            describe();
        } catch (Exception e) {
            lines.Add($"Teleport: {part} failed: {e}");
        }
    }
}
