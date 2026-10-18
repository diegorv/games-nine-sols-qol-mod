using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.Abilities.EarlyAirDash;

// The only hook of EarlyAirDash
[HarmonyPatch]
internal static class EarlyAirDashPatches {
    // Player.DodgeCheck decides whether a dash may start (Player.cs:668-702 in the decompiled game).
    // In the air it requires
    //     this.mainAbilities.RollDodgeInAirUpgrade.IsActivated
    // which compiles to
    //     call get_mainAbilities; ldfld RollDodgeInAirUpgrade; callvirt get_IsActivated
    // This turns that callvirt into a call to EarlyAirDashFeature.AirDashUnlocked, which takes the same
    // ability. If the code is not exactly as expected (a game update), it is left as it is and the
    // feature does nothing.
    [HarmonyPatch(typeof(Player), nameof(Player.DodgeCheck))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AllowAirDash(IEnumerable<CodeInstruction> instructions) {
        // Harmony runs the transpiler again whenever DodgeCheck is patched again (e.g. by another mod)
        EarlyAirDashFeature.Patched = false;
        var code = instructions.ToList();
        const string off = "EarlyAirDash is off";
        var upgrade = AccessTools.Field(typeof(PlayerMainAbilityCollection),
            nameof(PlayerMainAbilityCollection.RollDodgeInAirUpgrade));
        var isActivated = AccessTools.PropertyGetter(typeof(PlayerAbilityData), nameof(PlayerAbilityData.IsActivated));
        var helper = AccessTools.Method(typeof(EarlyAirDashFeature), nameof(EarlyAirDashFeature.AirDashUnlocked));
        if (!Transpile.Has(off, "PlayerMainAbilityCollection.RollDodgeInAirUpgrade or PlayerAbilityData.IsActivated",
                upgrade, isActivated, helper)) {
            return code;
        }

        var check = Transpile.FindOne(code, (c, i) => i > 0 && c[i - 1].LoadsField(upgrade) && c[i].Calls(isActivated),
            off, "checks of the air dash upgrade", "Player.DodgeCheck");
        if (check < 0) return code;

        // A plain call: the helper is static and takes the ability the callvirt was made on
        Transpile.CallInstead(code[check], helper);
        EarlyAirDashFeature.Patched = true;
        return code;
    }
}
