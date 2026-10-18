using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace NineSolsQoL.Features.AirDash;

// The only hook of the air dash feature
[HarmonyPatch]
internal static class AirDashPatches {
    // Player.DodgeCheck decides whether a dash may start (Player.cs:668-702 in the decompiled game).
    // In the air it requires
    //     this.mainAbilities.RollDodgeInAirUpgrade.IsActivated
    // which compiles to
    //     call get_mainAbilities; ldfld RollDodgeInAirUpgrade; callvirt get_IsActivated
    // This turns that callvirt into a call to AirDashFeature.AirDashUnlocked, which takes the same
    // ability. If the code is not exactly as expected (a game update), it is left as it is and the
    // feature does nothing.
    [HarmonyPatch(typeof(Player), nameof(Player.DodgeCheck))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AllowAirDash(IEnumerable<CodeInstruction> instructions) {
        // Harmony runs the transpiler again whenever DodgeCheck is patched again (e.g. by another mod)
        AirDashFeature.Patched = false;
        var code = instructions.ToList();
        var upgrade = AccessTools.Field(typeof(PlayerMainAbilityCollection),
            nameof(PlayerMainAbilityCollection.RollDodgeInAirUpgrade));
        var isActivated = AccessTools.PropertyGetter(typeof(PlayerAbilityData), nameof(PlayerAbilityData.IsActivated));

        var matches = new List<int>();
        for (var i = 1; i < code.Count; i++) {
            if (code[i - 1].LoadsField(upgrade) && code[i].Calls(isActivated)) matches.Add(i);
        }
        if (matches.Count != 1) {
            Log.Error($"AirDash is off: found {matches.Count} checks of the air dash upgrade in Player.DodgeCheck " +
                      "instead of 1, a game update probably changed it");
            return code;
        }

        // A plain call: the helper is static and takes the ability the callvirt was made on
        code[matches[0]].opcode = OpCodes.Call;
        code[matches[0]].operand = AccessTools.Method(typeof(AirDashFeature), nameof(AirDashFeature.AirDashUnlocked));
        AirDashFeature.Patched = true;
        return code;
    }
}
