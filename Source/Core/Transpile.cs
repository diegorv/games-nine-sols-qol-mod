using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace NineSolsQoL.Core;

// For the transpilers that change one spot of a game method: find it, or log why the feature is off
// and leave the method as it is. Outside the [HarmonyPatch] classes, because the Harmony analyzer
// treats every static method there as a patch.
//
// outcome is what happens without the change, e.g. "EarlyAirDash is off".
internal static class Transpile {
    // False after logging, when a member the transpiler needs was not found
    public static bool Has(string outcome, string names, params MemberInfo?[] members) {
        if (members.All(m => m != null)) return true;
        Log.Error($"{outcome}: {names} not found, a game update probably changed it");
        return false;
    }

    // The index of the only instruction for which matches(code, index) holds, or -1 after logging
    // when there is none or more than one. matches must check its own bounds.
    public static int FindOne(List<CodeInstruction> code, Func<List<CodeInstruction>, int, bool> matches,
                              string outcome, string what, string method) {
        var found = new List<int>();
        for (var i = 0; i < code.Count; i++) {
            if (matches(code, i)) found.Add(i);
        }
        if (found.Count == 1) return found[0];

        Log.Error($"{outcome}: found {found.Count} {what} in {method} instead of 1, a game update probably changed it");
        return -1;
    }

    // Turns the instruction into a plain call to a static helper; its labels and blocks stay
    public static void CallInstead(CodeInstruction instruction, MethodInfo helper) {
        instruction.opcode = OpCodes.Call;
        instruction.operand = helper;
    }
}
