using HarmonyLib;

namespace NineSolsQoL.Features.Death.NoDeathPenalty;

// The death bag half of the feature. The game fills the death bag only for a death that has a penalty
// (PlayerDeadState.NormalDeadHandle, the only caller of StorePlayerDataBeforeDead).
[HarmonyPatch]
internal static class NoDeathPenaltyPatches {
    // Whether the game will write a new bag: it skips that when its CanStoreDeadRecord condition fails
    [HarmonyPatch(typeof(PlayerDeadRecord), nameof(PlayerDeadRecord.StorePlayerDataBeforeDead))]
    [HarmonyPrefix]
    private static void BeforeStoreBag(PlayerDeadRecord __instance, out bool __state) =>
        __state = NoDeathPenaltyFeature.WillStoreBag(__instance);

    // The bag is filled: this death's decision, and the bag emptied when Jin and experience are kept
    [HarmonyPatch(typeof(PlayerDeadRecord), nameof(PlayerDeadRecord.StorePlayerDataBeforeDead))]
    [HarmonyPostfix]
    private static void AfterStoreBag(PlayerDeadRecord __instance, bool __state) =>
        NoDeathPenaltyFeature.AfterBagStored(__instance, __state);
}
