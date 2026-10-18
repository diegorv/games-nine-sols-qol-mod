using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using NineSolsQoL.Core;
using NineSolsQoL.Features.Abilities.EarlyAirDash;
using NineSolsQoL.Features.Abilities.FastHeal;
using NineSolsQoL.Features.Combat.DashInvulnerability;
using NineSolsQoL.Features.Combat.ParryFromBehind;
using NineSolsQoL.Features.Combat.PerfectParryWindow;
using NineSolsQoL.Features.Death.NoDeathPenalty;
using NineSolsQoL.Features.Death.RespawnNearDeath;
using NineSolsQoL.Features.Hud.BossHPNumbers;
using NineSolsQoL.Features.Loot.JinMultipliers;
using NineSolsQoL.Features.Loot.LootMagnet;

namespace NineSolsQoL;

// Entry point. BepInEx creates this component when the game starts, or ScriptEngine does when
// the DLL is in BepInEx/scripts and you press F6.
//
// Each feature lives in Features/<Group>/<Name>, the group being its group in the in-game menu: its
// config section (Init), its Harmony patches and the code they forward to. The mod only depends on
// BepInEx, Harmony and the game itself.
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    // One Harmony instance per feature, so a feature whose patches fail can be undone on its own
    private readonly List<Harmony> harmonies = new();
    private bool initialized;

    private void Awake() {
        Log.Init(Logger);
        // The in-game menu shows each group's features in this order (and the groups by name)
        DebugTools.Init(Config);
        EarlyAirDashFeature.Init(Config);
        FastHealFeature.Init(Config);
        PerfectParryWindowFeature.Init(Config);
        ParryFromBehindFeature.Init(Config);
        DashInvulnerabilityFeature.Init(Config);
        RespawnNearDeathFeature.Init(Config);
        NoDeathPenaltyFeature.Init(Config);
        BossHPNumbersFeature.Init(Config);
        LootMagnetFeature.Init(Config);
        JinMultipliersFeature.Init(Config);
        RegisterLifeCycle();
        initialized = true;

        Log.Info($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded on '{gameObject.name}'");
    }

    // BepInEx creates every plugin before any of them gets its Start, so here a feature can tell
    // whether the mod it replaces is loaded too
    private void Start() {
        // Unity still calls Start after an exception in Awake; the patches need the config entries
        if (!initialized) {
            Log.Error("Not patching the game: the mod failed to start (see the error above)");
            return;
        }

        Patch("EarlyAirDash", typeof(EarlyAirDashPatches));
        Patch("FastHeal", typeof(FastHealPatches));
        Patch("PerfectParryWindow", typeof(PerfectParryWindowPatches), "supidowagon.PerfectParry", "nozwock.PerfectParry");
        Patch("ParryFromBehind", typeof(ParryFromBehindPatches), "supidowagon.AnyDirectionParry");
        Patch("DashInvulnerability", typeof(DashInvulnerabilityPatches));
        Patch("RespawnNearDeath", typeof(RespawnNearDeathPatches));
        Patch("NoDeathPenalty", new[] { typeof(NoDeathPenaltyPatches), typeof(NoDeathPenaltyTranspiler) },
            "nozwock.NoDeathPenalty");
        Patch("LootMagnet", typeof(LootMagnetPatches), "nozwock.FarawayPickup");
        Patch("JinMultipliers", typeof(JinMultipliersPatches), "JinMultiplier");
        // Patches nothing: it only draws numbers
        BossHPNumbersFeature.Active = !IsReplaced("BossHPNumbers", "HPNumbers");
    }

    private void Update() {
        DebugTools.Update();
    }

    private void LateUpdate() {
        BossHPNumbersFeature.LateUpdate();
    }

    private void OnDestroy() {
        // A ScriptEngine reload creates a new instance: remove what the mod draws (first: it cannot throw)
        // and undo the patches so they are not applied twice
        BossHPNumbersFeature.Destroy();
        foreach (var harmony in harmonies) harmony.UnpatchSelf();
        Log.Info($"{MyPluginInfo.PLUGIN_NAME} unloaded");
    }

    // Whether one of the mods a feature replaces (by GUID) is loaded: then the feature stays off, so
    // the game does not get the same change twice
    private static bool IsReplaced(string feature, params string[] replacedMods) {
        var loaded = replacedMods.FirstOrDefault(Chainloader.PluginInfos.ContainsKey);
        if (loaded == null) return false;
        Log.Warning($"{feature} is off because {loaded} is loaded and does the same");
        return true;
    }

    private void Patch(string feature, Type patches, params string[] replacedMods) =>
        Patch(feature, new[] { patches }, replacedMods);

    // Applies one feature's [HarmonyPatch] classes, unless the feature is replaced (IsReplaced).
    // Harmony only looks at the methods declared in each class, not at nested classes. The classes
    // share one Harmony instance, so a failure in any of them undoes all of them.
    private void Patch(string feature, Type[] patches, params string[] replacedMods) {
        if (IsReplaced(feature, replacedMods)) return;

        var harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}.{feature}");
        harmonies.Add(harmony);
        try {
            foreach (var type in patches) harmony.PatchAll(type);
        } catch (Exception e) {
            // Harmony stops at the first method it cannot patch: undo the ones before it
            harmony.UnpatchSelf();
            Log.Error($"{feature} is off because patching the game failed: {e}");
            return;
        }

        var count = harmony.GetPatchedMethods().Count();
        if (count == 0) {
            Log.Error($"{feature} patched nothing: no [HarmonyPatch] method declared in " +
                      string.Join(", ", patches.Select(p => p.Name)));
        } else {
            Log.Debug($"{feature}: {count} game method(s) patched");
        }
    }

    // When going back to the title screen the game destroys every GameObject that is not
    // registered in RCGLifeCycle (DestroyAllGameObjects). DontDestroyForever only adds the object
    // to a static list, so it is safe to call from Awake. If it ever throws, the full exception
    // goes to the log.
    private void RegisterLifeCycle() {
        try {
            RCGLifeCycle.DontDestroyForever(gameObject);
            Log.Info($"Registered '{gameObject.name}' in RCGLifeCycle");
        } catch (Exception e) {
            Log.Error($"RCGLifeCycle.DontDestroyForever failed: {e}");
        }
    }
}
