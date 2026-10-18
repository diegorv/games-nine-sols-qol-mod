using HarmonyLib;
using RCGMaker.Core;

namespace NineSolsQoL.Features.Travel.EarlyTeleport;

// The hooks of the early teleport feature. Both getters are protected, hence their names; AbstractConditionComp.
// FinalResult reaches them through a virtual call. Only a false result is changed, and only for the Teleport
// button's and tab's own conditions.
[HarmonyPatch]
internal static class EarlyTeleportPatches {
    // The Teleport tab's teleport ability check (AbilityActivatedCondition.cs:5-11 in the decompiled game)
    [HarmonyPatch(typeof(AbilityActivatedCondition), "isValid", MethodType.Getter)]
    [HarmonyPostfix]
    private static void AfterAbilityCheck(AbilityActivatedCondition __instance, ref bool __result) {
        if (!__result && EarlyTeleportFeature.UnlockTab(__instance)) __result = true;
    }

    // The Teleport button's unlock flag check (RCGMaker/Core/GameFlagPropertyCondition.cs:7-13)
    [HarmonyPatch(typeof(GameFlagPropertyCondition), "isValid", MethodType.Getter)]
    [HarmonyPostfix]
    private static void AfterFlagCheck(GameFlagPropertyCondition __instance, ref bool __result) {
        if (!__result && EarlyTeleportFeature.UnlockButton(__instance)) __result = true;
    }
}
