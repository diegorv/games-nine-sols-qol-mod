using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using NineSolsQoL.Core;
using NineSolsQoL.Features.AnyDirectionParry;
using NineSolsQoL.Features.JinMultipliers;
using NineSolsQoL.Features.NoDeathPenalty;
using NineSolsQoL.Features.ParryWindow;
using NineSolsQoL.Features.Pickup;
using NineSolsQoL.Features.Respawn;

namespace NineSolsQoL;

// Entry point. BepInEx creates this component when the game starts, or ScriptEngine does when
// the DLL is in BepInEx/scripts and you press F6.
//
// Each feature lives in Features/<Name>: its config section (Init), its Harmony patches and the
// code they forward to. The mod only depends on BepInEx, Harmony and the game itself.
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    // One Harmony instance per feature, so a feature whose patches fail can be undone on its own
    private readonly List<Harmony> harmonies = new();
    private bool initialized;

    private void Awake() {
        Log.Init(Logger);
        DebugTools.Init(Config);
        RespawnFeature.Init(Config);
        ParryWindowFeature.Init(Config);
        AnyDirectionParryFeature.Init(Config);
        JinMultipliersFeature.Init(Config);
        PickupFeature.Init(Config);
        NoDeathPenaltyFeature.Init(Config);
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

        Patch("Respawn", typeof(RespawnPatches));
        Patch("ParryWindow", typeof(ParryWindowPatches), "supidowagon.PerfectParry", "nozwock.PerfectParry");
        Patch("AnyDirectionParry", typeof(AnyDirectionParryPatches), "supidowagon.AnyDirectionParry");
        Patch("JinMultipliers", typeof(JinMultipliersPatches), "JinMultiplier");
        Patch("Pickup", typeof(PickupPatches), "nozwock.FarawayPickup");
        Patch("NoDeathPenalty", new[] { typeof(NoDeathPenaltyPatches), typeof(NoDeathPenaltyTranspiler) },
            "nozwock.NoDeathPenalty");
    }

    private void Update() {
        DebugTools.Update();
    }

    private void OnDestroy() {
        // A ScriptEngine reload creates a new instance: undo the patches so they are not applied twice
        foreach (var harmony in harmonies) harmony.UnpatchSelf();
        Log.Info($"{MyPluginInfo.PLUGIN_NAME} unloaded");
    }

    private void Patch(string feature, Type patches, params string[] replacedMods) =>
        Patch(feature, new[] { patches }, replacedMods);

    // Applies one feature's [HarmonyPatch] classes, unless one of the mods it replaces (by GUID) is
    // loaded: then the feature stays off so the game is not patched twice for the same thing.
    // Harmony only looks at the methods declared in each class, not at nested classes. The classes
    // share one Harmony instance, so a failure in any of them undoes all of them.
    private void Patch(string feature, Type[] patches, params string[] replacedMods) {
        var loaded = replacedMods.FirstOrDefault(Chainloader.PluginInfos.ContainsKey);
        if (loaded != null) {
            Log.Warning($"{feature} is off because {loaded} is loaded and does the same");
            return;
        }

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
