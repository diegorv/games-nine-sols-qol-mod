using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace NineSolsQoL.Features.Combat.PerfectParryWindow;

// The only hook of the perfect parry window feature
[HarmonyPatch]
internal static class PerfectParryWindowPatches {
    // PlayerParryState.Parried tests, in short (PlayerParryState.cs:454-456 in the decompiled game,
    // where elapsed is num5)
    //     float elapsed = Time.time - <last parry press>;
    //     if (IsAlwaysAccurate || Player.i.IsInQTETutorial || elapsed <= spamDatas[spamLevel] + 0f)
    // which compiles to
    //     ldloc elapsed; ldarg.0; ldfld spamDatas; ldarg.0; ldfld spamLevel; ldelem.r4; ...
    // This turns the ldelem.r4 into ldloc elapsed; call PerfectParryWindowFeature.PerfectWindow, which
    // takes (spamDatas, spamLevel, elapsed). If the code is not exactly as expected (a game update),
    // the method is left as it is and the feature does nothing.
    [HarmonyPatch(typeof(PlayerParryState), nameof(PlayerParryState.Parried))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ReadWindowFromMod(IEnumerable<CodeInstruction> instructions) {
        // Harmony runs the transpiler again whenever Parried is patched again (e.g. by another mod)
        PerfectParryWindowFeature.Patched = false;
        var code = instructions.ToList();
        var windows = AccessTools.Field(typeof(PlayerParryState), "spamDatas");
        var level = AccessTools.Field(typeof(PlayerParryState), nameof(PlayerParryState.spamLevel));
        var perfectWindow = AccessTools.Method(typeof(PerfectParryWindowFeature), nameof(PerfectParryWindowFeature.PerfectWindow));
        if (windows == null || level == null || perfectWindow == null) {
            Log.Error("PerfectParryWindow is off: PlayerParryState.spamDatas not found, a game update probably changed it");
            return code;
        }

        var matches = new List<int>();
        for (var i = 0; i + 5 < code.Count; i++) {
            if (code[i].IsLdloc() &&
                code[i + 1].IsLdarg(0) && code[i + 2].LoadsField(windows) &&
                code[i + 3].IsLdarg(0) && code[i + 4].LoadsField(level) &&
                code[i + 5].opcode == OpCodes.Ldelem_R4) {
                matches.Add(i);
            }
        }
        if (matches.Count != 1) {
            Log.Error($"PerfectParryWindow is off: found {matches.Count} reads of the parry window in " +
                      "PlayerParryState.Parried instead of 1, a game update probably changed it");
            return code;
        }

        var start = matches[0];
        var read = code[start + 5];
        // Same opcode and local as the instruction that loads elapsed for the comparison
        var loadElapsed = new CodeInstruction(code[start].opcode, code[start].operand);
        // Anything that jumped to the read now jumps to the new first instruction (nothing does today)
        loadElapsed.labels.AddRange(read.labels);
        read.labels.Clear();
        read.opcode = OpCodes.Call;
        read.operand = perfectWindow;
        code.Insert(start + 5, loadElapsed);

        PerfectParryWindowFeature.Patched = true;
        return code;
    }
}
