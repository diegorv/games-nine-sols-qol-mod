using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.Abilities.FastHeal;

// The only hook of FastHeal
[HarmonyPatch]
internal static class FastHealPatches {
    // PlayerDrinkPotionState.OnStateEnter (PlayerDrinkPotionState.cs:16-25 in the decompiled game) does
    //     if (this.isSpeedUpHeal) this.player.animator.speed = 1.66666f;
    // which compiles to
    //     ldarg.0; call get_isSpeedUpHeal; brfalse ...
    // This adds a call to FastHealFeature.SpeedUpHeal right after the getter: it takes the getter's
    // result and passes it on, or true while the feature is on. The game's getter runs as it is. If
    // OnStateEnter does not call the getter exactly once (a game update), it is left as it is and the
    // feature does nothing.
    [HarmonyPatch(typeof(PlayerDrinkPotionState), nameof(PlayerDrinkPotionState.OnStateEnter))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> HealFaster(IEnumerable<CodeInstruction> instructions) {
        // Harmony runs the transpiler again whenever OnStateEnter is patched again (e.g. by another mod)
        FastHealFeature.Patched = false;
        var code = instructions.ToList();
        const string off = "FastHeal is off";
        var isSpeedUpHeal = AccessTools.PropertyGetter(typeof(PlayerDrinkPotionState), "isSpeedUpHeal");
        var helper = AccessTools.Method(typeof(FastHealFeature), nameof(FastHealFeature.SpeedUpHeal));
        if (!Transpile.Has(off, "PlayerDrinkPotionState.isSpeedUpHeal", isSpeedUpHeal, helper)) return code;

        var check = Transpile.FindOne(code, (c, i) => c[i].Calls(isSpeedUpHeal),
            off, "checks of the heal jade", "PlayerDrinkPotionState.OnStateEnter");
        if (check < 0) return code;

        // Takes the bool the getter left on the stack and leaves one
        code.Insert(check + 1, new CodeInstruction(OpCodes.Call, helper));
        FastHealFeature.Patched = true;
        return code;
    }
}
