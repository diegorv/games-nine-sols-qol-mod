using HarmonyLib;

namespace NineSolsQoL.Features.JinMultipliers;

// Every hook of the Jin multipliers feature; they only forward to JinMultipliersFeature
[HarmonyPatch]
internal static class JinMultipliersPatches {
    // Every coin pickup, Transmute Unto Wealth kill and death bag pickup adds its Jin here
    [HarmonyPatch(typeof(PlayerGamePlayData), nameof(PlayerGamePlayData.AddGold))]
    [HarmonyPrefix]
    private static void BeforeAddGold(PlayerGamePlayData __instance, ref int amount, GoldSourceTag sourceTag) =>
        amount = JinMultipliersFeature.MultiplyEarning(__instance, amount, sourceTag);

    // A shop trade (buy, sell or craft) just completed
    [HarmonyPatch(typeof(MerchandiseData), nameof(MerchandiseData.Trade))]
    [HarmonyPostfix]
    private static void AfterTrade(MerchandiseData __instance) => JinMultipliersFeature.AfterTrade(__instance);

    // A shop row was drawn, with the price the game shows for it
    [HarmonyPatch(typeof(MerchandiseItemButton), nameof(MerchandiseItemButton.UpdateView))]
    [HarmonyPostfix]
    private static void AfterShopRowUpdate(MerchandiseItemButton __instance) =>
        JinMultipliersFeature.AfterShopRowUpdate(__instance);
}
