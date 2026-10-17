using HarmonyLib;

namespace NineSolsQoL.Features.Pickup;

// The only hook of the pickup feature
[HarmonyPatch]
internal static class PickupPatches {
    // Every frame, for every active drop (DropItem.Update is private). Returning false skips the
    // game's Update for that frame (a flying drop waits while Yi is dead).
    [HarmonyPatch(typeof(DropItem), "Update")]
    [HarmonyPrefix]
    private static bool BeforeDropUpdate(DropItem __instance) => PickupFeature.BeforeUpdate(__instance);
}
