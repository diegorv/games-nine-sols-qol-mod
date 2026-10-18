using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.Combat.PerfectParryWindow;

// A wider (or narrower) perfect-parry window.
//
// PlayerParryState.Parried decides between a perfect and an imperfect parry with
//     elapsed <= spamDatas[spamLevel]
// where elapsed is the game time since the last parry press, and spamDatas holds the window per
// spam level: { 0.1333 s, 0.1 s, 0, 0, 0 }. Each parry that hits nothing raises spamLevel, so
// mashing first shrinks the window and then removes it. PerfectParryWindowPatches makes Parried read
// PerfectWindow instead; the array itself is never changed.
internal static class PerfectParryWindowFeature {
    // The game's first window, 0.13333334 s, is 8 frames at 60 fps
    private const float VanillaFrames = 8f;

    public static ConfigEntry<bool> Enabled = null!;
    public static ConfigEntry<int> PerfectParryFrames = null!;

    // Set by the transpiler when it found the game's check
    public static bool Patched;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "PerfectParryWindow", MenuCategory.Combat, "Perfect parry window");
        Enabled = section.Bind("Enabled", true,
            "Change how early you can press parry before a hit and still get a perfect parry. " +
            "Off: the game's window, 8 frames (133 ms)");
        PerfectParryFrames = section.Bind("PerfectParryFrames", 12,
            "The perfect-parry window, in frames at 60 fps (the game measures time, so it is the same at " +
            "any frame rate). Vanilla: 8 (133 ms). The game's penalty for mashing parry keeps its shape: " +
            "pressing again soon after a missed parry gives 3/4 of this window (vanilla 6 frames), and " +
            "more misses remove it. The game forgets a parry press after 0.5 s (30 frames at 60 fps), so " +
            "values near 30 make almost every parry perfect",
            new AcceptableValueRange<int>(1, 30));

        DebugTools.AddDumpSection(Describe);
    }

    // Called by PlayerParryState.Parried in place of reading windows[level]: how many seconds a
    // parry press may come before the hit and still give a perfect parry. Runs inside the game's
    // parry handling, so it must not throw (apart from the index error the game's own read of
    // windows[level] would raise).
    public static float PerfectWindow(float[] windows, int level, float elapsed) {
        var vanilla = windows[level];
        try {
            var window = Scale(vanilla);
            if (Log.DebugEnabled) {
                Log.Debug($"[parry] level {level}: pressed {Ms(elapsed)} before the check, window {Ms(window)} " +
                          $"(vanilla {Ms(vanilla)}): {Outcome(elapsed, window, vanilla)}");
            }
            return window;
        } catch (Exception e) {
            Log.Error($"PerfectParryWindow failed, using the game's window: {e}");
            return vanilla;
        }
    }

    // A window that is not 0 is scaled by PerfectParryFrames / 8, so 8 frames is exactly the game's
    // window. The zeros, the game's penalty for mashing parry, stay 0.
    private static float Scale(float vanilla) {
        if (!Enabled.Value || vanilla <= 0f) return vanilla;
        return vanilla * (PerfectParryFrames.Value / VanillaFrames);
    }

    // The game's own test is elapsed <= window
    private static string Outcome(float elapsed, float window, float vanilla) {
        var perfect = elapsed <= window;
        if (perfect == elapsed <= vanilla) return perfect ? "perfect" : "imperfect";
        return perfect ? "perfect (imperfect in vanilla)" : "imperfect (perfect in vanilla)";
    }

    private static IEnumerable<string> Describe(GameCore core) {
        var state = core.player.GetState<PlayerParryState>(PlayerStateType.Parry);
        var windows = state != null ? GamePrivates.ParryWindows(state) : null;
        var table = windows != null
            ? $"{MsList(windows)} -> {MsList(windows.Select(Scale))}, spamLevel {state!.spamLevel}"
            : "parry state not found";
        return new[] {
            $"PerfectParryWindow: enabled={Enabled.Value} frames={PerfectParryFrames.Value} patched={Patched} " +
            $"windows {table}",
        };
    }

    // One decimal: a press 0.4 ms outside the window must not look like it was inside
    private static string Ms(float seconds) => $"{seconds * 1000f:0.0} ms";

    private static string MsList(IEnumerable<float> seconds) =>
        "[" + string.Join(", ", seconds.Select(s => $"{s * 1000f:0}")) + "] ms";
}
