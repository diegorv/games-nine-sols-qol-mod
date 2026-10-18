using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace NineSolsQoL.Features.HealSpeed;

// The only hook of the heal speed feature
[HarmonyPatch]
internal static class HealSpeedPatches {
    // PlayerDrinkPotionState.OnStateEnter (PlayerDrinkPotionState.cs:16-25 in the decompiled game) does
    //     if (this.isSpeedUpHeal) this.player.animator.speed = 1.66666f;
    // which compiles to
    //     ldarg.0; call get_isSpeedUpHeal; brfalse ...
    // This turns that call into a call to HealSpeedFeature.SpeedUpHeal, which takes the same state. If
    // OnStateEnter does not call the getter exactly once (a game update), it is left as it is and the
    // feature does nothing.
    [HarmonyPatch(typeof(PlayerDrinkPotionState), nameof(PlayerDrinkPotionState.OnStateEnter))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> HealFaster(IEnumerable<CodeInstruction> instructions) {
        // Harmony runs the transpiler again whenever OnStateEnter is patched again (e.g. by another mod)
        HealSpeedFeature.Patched = false;
        var code = instructions.ToList();
        var isSpeedUpHeal = AccessTools.PropertyGetter(typeof(PlayerDrinkPotionState), "isSpeedUpHeal");
        if (isSpeedUpHeal == null) {
            Log.Error("HealSpeed is off: PlayerDrinkPotionState.isSpeedUpHeal not found, a game update probably " +
                      "changed it");
            return code;
        }

        var matches = new List<int>();
        for (var i = 0; i < code.Count; i++) {
            if (code[i].Calls(isSpeedUpHeal)) matches.Add(i);
        }
        if (matches.Count != 1) {
            Log.Error($"HealSpeed is off: found {matches.Count} checks of the heal jade in " +
                      "PlayerDrinkPotionState.OnStateEnter instead of 1, a game update probably changed it");
            return code;
        }

        // A plain call: the helper is static and takes the state the getter was called on
        code[matches[0]].opcode = OpCodes.Call;
        code[matches[0]].operand = AccessTools.Method(typeof(HealSpeedFeature), nameof(HealSpeedFeature.SpeedUpHeal));
        HealSpeedFeature.Patched = true;
        return code;
    }
}
