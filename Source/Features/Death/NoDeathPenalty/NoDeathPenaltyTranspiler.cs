using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace NineSolsQoL.Features.Death.NoDeathPenalty;

// The death penalty half of the feature. A class of its own because its target is found by code
// (TargetMethod), and HarmonyX refuses a TargetMethod in a class whose patches name their own target.
[HarmonyPatch]
internal static class NoDeathPenaltyTranspiler {
    // PlayerGamePlayData.PlayerDeathPenalty is async void: its body runs in the MoveNext of a state machine
    // the compiler generated (<PlayerDeathPenalty>d__32 in this build), which the method's attribute names.
    // Null (the method is no longer async) makes Harmony fail, and the feature stays off.
    [HarmonyTargetMethod]
    private static MethodBase? PenaltyBody() {
        var penalty = AccessTools.Method(typeof(PlayerGamePlayData), nameof(PlayerGamePlayData.PlayerDeathPenalty));
        var stateMachine = penalty?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        return stateMachine != null ? AccessTools.Method(stateMachine, "MoveNext") : null;
    }

    // After its 100 ms delay, the penalty does (PlayerGamePlayData.cs:147-152 in the decompiled game)
    //     data.deathCount += 1f; Debug.Log(...); CurrentGold = 0; CurrentExp = 0;
    //     AutoSave(FlagOnly).Forget(); Player.i.ResetDeathPenalty();
    // where the two zeroings compile to
    //     ldloc.1; ldc.i4.0; call set_CurrentGold; ldloc.1; ldc.i4.0; call set_CurrentExp
    // This points those two calls at NoDeathPenaltyFeature.SetGoldAtPenalty and SetExpAtPenalty, which take
    // the same instance and value and call the setter unless the death keeps Jin and experience. The rest
    // of the penalty stays as it is. If the code is not exactly as expected (a game update), it is left
    // untouched and the feature does nothing, the bag included.
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> KeepJinAndExperience(IEnumerable<CodeInstruction> instructions) {
        // Harmony runs the transpiler again whenever the method is patched again (e.g. by another mod)
        NoDeathPenaltyFeature.Patched = false;
        var code = instructions.ToList();
        var setGold = AccessTools.PropertySetter(typeof(PlayerGamePlayData), nameof(PlayerGamePlayData.CurrentGold));
        var setExp = AccessTools.PropertySetter(typeof(PlayerGamePlayData), nameof(PlayerGamePlayData.CurrentExp));

        var golds = new List<int>();
        var exps = new List<int>();
        for (var i = 1; i < code.Count; i++) {
            if (!code[i - 1].LoadsConstant(0)) continue;
            if (code[i].Calls(setGold)) golds.Add(i);
            else if (code[i].Calls(setExp)) exps.Add(i);
        }
        if (golds.Count != 1 || exps.Count != 1) {
            Log.Error($"NoDeathPenalty is off: found {golds.Count} zeroing(s) of Jin and {exps.Count} of " +
                      "experience in PlayerDeathPenalty instead of 1 each, a game update probably changed it");
            return code;
        }

        // A plain call (Calls also matches callvirt): the helpers are static, with the instance as their
        // first parameter
        code[golds[0]].opcode = OpCodes.Call;
        code[golds[0]].operand =
            AccessTools.Method(typeof(NoDeathPenaltyFeature), nameof(NoDeathPenaltyFeature.SetGoldAtPenalty));
        code[exps[0]].opcode = OpCodes.Call;
        code[exps[0]].operand =
            AccessTools.Method(typeof(NoDeathPenaltyFeature), nameof(NoDeathPenaltyFeature.SetExpAtPenalty));
        NoDeathPenaltyFeature.Patched = true;
        return code;
    }
}
