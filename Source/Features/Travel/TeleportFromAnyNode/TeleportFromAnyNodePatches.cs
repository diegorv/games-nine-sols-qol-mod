using HarmonyLib;

namespace NineSolsQoL.Features.Travel.TeleportFromAnyNode;

// The hooks of the teleport-from-any-node feature
[HarmonyPatch]
internal static class TeleportFromAnyNodePatches {
    // "This Root Node is the Pavilion" (TeleportPointMatchSavePanelCondition.cs:5-11 in the decompiled game). Only a
    // false result is changed, and only for the Teleport button and tab. The getter is protected, hence its name;
    // AbstractConditionComp.FinalResult reaches it through a virtual call.
    [HarmonyPatch(typeof(TeleportPointMatchSavePanelCondition), "isValid", MethodType.Getter)]
    [HarmonyPostfix]
    private static void AfterPavilionCheck(TeleportPointMatchSavePanelCondition __instance, ref bool __result) {
        if (!__result && TeleportFromAnyNodeFeature.ShowTeleport(__instance)) __result = true;
    }

    // A Root Node in the teleport list was chosen (TeleportPointButton.cs:10-23). The async method clears the Root
    // Node menu's node before its first await, and its stub starts it synchronously: a prefix on the stub runs before
    // all of it.
    [HarmonyPatch(typeof(TeleportPointButton), "SubmitImplementation")]
    [HarmonyPrefix]
    private static void BeforeTeleport() {
        TeleportFromAnyNodeFeature.RememberNodeLeft();
    }
}
