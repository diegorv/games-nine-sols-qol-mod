using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using NineSolsQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NineSolsQoL.Features.HPNumbers;

// The boss's HP and internal injury as numbers on screen.
//
// Like MicheliniDev's HPNumbers: texts on a canvas of the mod's own, drawn over the game, placed and
// coloured from the config, read every frame. The values are the ones the game's boss bar draws: the
// PostureSystem the bar is bound to (GameCore.monsterHpUI.CurrentBossHP), GetPostureValue() and
// InternalInjury, so the numbers show exactly while the game shows its boss bar, and only while it
// shows its HUD (UIManager.playerInGameUIDisplayVote): not in menus, the map, dialogues, cutscenes,
// after a death or while loading. No game method is patched.
internal static class HPNumbersFeature {
    // The internal injury colour of the original mod's defaults
    private static readonly Color InternalInjuryColor = new(0.5f, 1f, 1f, 1f);

    public static ConfigEntry<bool> Enabled = null!;

    // Set by Plugin.Start, unless the original HPNumbers is loaded
    public static bool Active;

    private static HPNumber bossHP = null!;
    private static HPNumber bossInternal = null!;

    // The canvas with the numbers, created the first time they show. Destroy removes it when the mod
    // unloads; if the game ever destroys it, it is created again.
    private static GameObject? root;

    // Set after an unexpected error: no numbers until the game restarts
    private static bool failed;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "HPNumbers");
        Enabled = section.Bind("Enabled", true,
            "Show the boss's HP, and optionally its internal injury, as numbers while the game shows the boss " +
            "bar and its HUD. Off: no numbers, as in the game");
        bossHP = new HPNumber(section, "BossHP", "the boss's HP", true, new Vector2(950f, 99f), Color.white);
        bossInternal = new HPNumber(section, "BossInternalHP",
            "the boss's internal injury (the HP it regains over time unless a talisman explosion cashes it in)",
            false, new Vector2(1030f, 99f), InternalInjuryColor);

        DebugTools.AddDumpSection(Describe);
    }

    // Every frame, from Plugin.LateUpdate: after the game's own Update, so the numbers match the bars
    public static void LateUpdate() {
        if (!Active || failed) return;
        try {
            var core = Enabled.Value ? GameAccess.CoreInLevel() : null;
            if (core == null || !HudShown()) {
                if (root != null && root.activeSelf) root.SetActive(false);
                return;
            }

            if (root == null) CreateRoot();
            if (!root!.activeSelf) root.SetActive(true);

            var boss = BossPosture(core);
            bossHP.Show(boss != null ? boss.GetPostureValue() : null);
            bossInternal.Show(boss != null ? (int)boss.InternalInjury : null);
        } catch (Exception e) {
            failed = true;
            Log.Error($"HPNumbers is off until the game restarts, after an error: {e}");
        }
    }

    // When the mod unloads (hot reload): the canvas is not part of any patch, so it must go explicitly
    public static void Destroy() {
        if (root != null) Object.Destroy(root);
        root = null;
    }

    // The game shows its HUD when every voter agrees: the gameplay input state, the "show gameplay UI"
    // setting, the area name banner and a few scripted scenes
    private static bool HudShown() {
        if (!UIManager.IsAvailable()) return false;
        var vote = UIManager.Instance.playerInGameUIDisplayVote;
        return vote != null && vote.Result;
    }

    // The boss whose bar the game shows (the first one, like GameCore.monsterHpUI.CurrentBossHP)
    private static PostureSystem? BossPosture(GameCore core) {
        var bar = core.monsterHpUI != null ? core.monsterHpUI.CurrentBossHP : null;
        return bar != null ? bar.bindingPosture : null;
    }

    // An overlay canvas scaled like a 1920x1080 screen, so the configured positions and sizes keep
    // their place at any resolution
    private static void CreateRoot() {
        root = new GameObject("NineSolsQoL HPNumbers");
        Object.DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        bossHP.Create(root.transform);
        bossInternal.Create(root.transform);
        // Back to title destroys every object the game did not register (DestroyAllGameObjects); this
        // registers the canvas and the numbers it holds now
        RCGLifeCycle.DontDestroyForever(root);
    }

    private static IEnumerable<string> Describe(GameCore core) {
        var boss = BossPosture(core);
        return new[] {
            $"HPNumbers: enabled={Enabled.Value} active={Active} failed={failed} hudShown={HudShown()} " +
            $"canvas={(root == null ? "none" : root.activeSelf ? "shown" : "hidden")} boss=" +
            (boss != null ? $"{boss.GetPostureValue()}/{boss.MaxPostureValue} internal={boss.InternalInjury}" : "no bar"),
        };
    }
}

// One number on screen and its config entries
internal sealed class HPNumber {
    private readonly string key;
    private readonly ConfigEntry<bool> show;
    private readonly ConfigEntry<Vector2> position;
    private readonly ConfigEntry<int> fontSize;
    private readonly ConfigEntry<Color> color;

    private TextMeshProUGUI? text;

    // The value the text holds, so the text is only rebuilt when the number changes
    private int shown;

    public HPNumber(ConfigSection section, string key, string what, bool on, Vector2 at, Color tint) {
        this.key = key;
        show = section.Bind(key, on, $"Show {what}");
        position = section.Bind($"{key}Position", at,
            "Where the number starts (its left end, vertically centred), in pixels from the bottom-left corner " +
            "of a 1920x1080 screen; other resolutions scale it");
        fontSize = section.Bind($"{key}FontSize", 19, "Font size", new AcceptableValueRange<int>(8, 72));
        color = section.Bind($"{key}Color", tint, "Colour");
    }

    public void Create(Transform canvas) {
        var holder = new GameObject(key, typeof(RectTransform));
        holder.transform.SetParent(canvas, false);
        text = holder.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 100f);
        shown = int.MinValue;
        holder.SetActive(false);
    }

    // Shows the value, or hides the number when there is none or it is turned off in the config
    public void Show(int? value) {
        if (text == null) return;
        var visible = value.HasValue && show.Value;
        if (text.gameObject.activeSelf != visible) text.gameObject.SetActive(visible);
        if (!visible) return;

        if (value!.Value != shown) {
            shown = value.Value;
            text.text = shown.ToString();
        }
        var rect = text.rectTransform;
        if (rect.anchoredPosition != position.Value) rect.anchoredPosition = position.Value;
        text.fontSize = fontSize.Value;
        text.color = color.Value;
    }
}
