using HarmonyLib;

namespace NineSolsQoL.Features.Loot.LootMagnet;

// The only hook of the loot magnet feature
[HarmonyPatch]
internal static class LootMagnetPatches {
    // Every frame, for every active drop (DropItem.Update is private). Returning false skips the
    // game's Update for that frame (a flying drop waits while Yi is dead).
    [HarmonyPatch(typeof(DropItem), "Update")]
    [HarmonyPrefix]
    private static bool BeforeDropUpdate(DropItem __instance) => LootMagnetFeature.BeforeUpdate(__instance);
}
