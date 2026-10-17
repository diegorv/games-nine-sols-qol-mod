using HarmonyLib;
using UnityEngine;

namespace NineSolsQoL.Features.AnyDirectionParry;

// The only hook of the any-direction parry feature
[HarmonyPatch]
internal static class AnyDirectionParryPatches {
    // The game asks whether the player faces an attack before letting it be parried. Only a failed
    // check is changed; the other overload, FacingRuleCheck(EffectReceiver), is never called.
    [HarmonyPatch(typeof(EffectDealer), nameof(EffectDealer.FacingRuleCheck),
        new[] { typeof(DamageDealer), typeof(Vector3), typeof(Vector2) })]
    [HarmonyPostfix]
    private static void AfterFacingCheck(EffectDealer __instance, DamageDealer damageDealer, ref bool __result) {
        if (!__result && AnyDirectionParryFeature.AllowFromBehind(__instance, damageDealer)) __result = true;
    }
}
