using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
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
using NineSolsQoL.Features.Travel.EarlyTeleport;
using NineSolsQoL.Features.Travel.TeleportFromAnyNode;
using UnityEngine;

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
    // Features whose setup threw: they stay off, the others still work
    private readonly HashSet<string> failed = new();
    private bool debugToolsFailed;

    private void Awake() {
        Log.Init(Logger);
        // First, so that no failure below can leave the in-game menu to be destroyed on the title screen
        RegisterLifeCycle();

        // BepInEx saves the whole .cfg for every new entry: save once at the end instead. If the file
        // cannot be written (read-only, locked), the entries still work with their values in memory.
        Config.SaveOnConfigSet = false;
        // The in-game menu shows each group's features in this order (and the groups by name)
        Init("Debug", DebugTools.Init);
        Init("EarlyAirDash", EarlyAirDashFeature.Init);
        Init("FastHeal", FastHealFeature.Init);
        Init("PerfectParryWindow", PerfectParryWindowFeature.Init);
        Init("ParryFromBehind", ParryFromBehindFeature.Init);
        Init("DashInvulnerability", DashInvulnerabilityFeature.Init);
        Init("RespawnNearDeath", RespawnNearDeathFeature.Init);
        Init("NoDeathPenalty", NoDeathPenaltyFeature.Init);
        Init("BossHPNumbers", BossHPNumbersFeature.Init);
        Init("LootMagnet", LootMagnetFeature.Init);
        Init("JinMultipliers", JinMultipliersFeature.Init);
        Init("TeleportFromAnyNode", TeleportFromAnyNodeFeature.Init);
        Init("EarlyTeleport", EarlyTeleportFeature.Init);
        Config.SaveOnConfigSet = true;
        try {
            Config.Save();
        } catch (Exception e) {
            Log.Warning($"Could not save {Config.ConfigFilePath}, the settings still apply: {e.Message}");
        }

        Log.Info($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded on '{gameObject.name}'");
    }

    // A feature whose setup throws stays off (Patch skips it); the others still start
    private void Init(string feature, Action<ConfigFile> init) {
        try {
            init(Config);
        } catch (Exception e) {
            failed.Add(feature);
            Log.Error($"{feature} is off because its setup failed: {e}");
        }
    }

    // BepInEx creates every plugin before any of them gets its Start, so here a feature can tell
    // whether the mod it replaces is loaded too
    private void Start() {
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
        Patch("TeleportFromAnyNode", typeof(TeleportFromAnyNodePatches), "nozwock.TeleportFromAnyNode",
            "TeleportFromAnywhere");
        Patch("EarlyTeleport", typeof(EarlyTeleportPatches));
        // Patches nothing: it only draws numbers
        BossHPNumbersFeature.Active = !failed.Contains("BossHPNumbers") && !IsReplaced("BossHPNumbers", "HPNumbers");
    }

    private void Update() {
        if (debugToolsFailed || failed.Contains("Debug")) return;
        try {
            DebugTools.Update();
        } catch (Exception e) {
            debugToolsFailed = true;
            Log.Error($"The debug tools (F9 dump, [hitch] lines) are off until the game restarts, after an error: {e}");
        }
    }

    private void LateUpdate() {
        BossHPNumbersFeature.LateUpdate();
    }

    private void OnDestroy() {
        // A ScriptEngine reload creates a new instance: remove what the mod draws (first: it cannot throw)
        // and undo the patches so they are not applied twice
        BossHPNumbersFeature.Destroy();
        foreach (var harmony in harmonies) Unpatch(harmony);
        Log.Info($"{MyPluginInfo.PLUGIN_NAME} unloaded");
    }

    // Undoes one feature's patches. An error here must not keep the other features from being undone
    // (after a hot reload they would apply twice) or from being patched.
    private static void Unpatch(Harmony harmony) {
        try {
            harmony.UnpatchSelf();
        } catch (Exception e) {
            Log.Error($"Undoing the patches of {harmony.Id} failed: {e}");
        }
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

    // Applies one feature's [HarmonyPatch] classes, unless the feature failed its setup or is replaced
    // (IsReplaced). Harmony only looks at the methods declared in each class, not at nested classes.
    // The classes share one Harmony instance, so a failure in any of them, or a class that patches
    // nothing, undoes all of them.
    private void Patch(string feature, Type[] patches, params string[] replacedMods) {
        if (failed.Contains(feature) || IsReplaced(feature, replacedMods)) return;

        var harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}.{feature}");
        harmonies.Add(harmony);
        try {
            foreach (var type in patches) {
                // What PatchAll(Type) does, keeping the list of methods it patched
                var patched = harmony.CreateClassProcessor(type, true).Patch();
                if (patched == null || patched.Count == 0) {
                    throw new InvalidOperationException($"{type.Name} patched nothing: no [HarmonyPatch] method declared in it");
                }
            }
        } catch (Exception e) {
            // Harmony stops at the first method it cannot patch: undo the ones before it
            Log.Error($"{feature} is off because patching the game failed: {e}");
            Unpatch(harmony);
            return;
        }

        Log.Debug($"{feature}: {harmony.GetPatchedMethods().Count()} game method(s) patched");
    }

    // Keeps the object the mod lives on, and BepInEx's manager object, alive. In a release install they
    // are the same object, which also holds every other plugin (ConfigurationManager's F1 menu included);
    // under ScriptEngine (dev) the mod lives on ScriptEngine's own object. This is what NineSolsAPI is
    // usually installed for (NineSolsAPICore.Awake does the same two things).
    private void RegisterLifeCycle() {
        Keep(gameObject);
        var manager = Chainloader.ManagerObject;
        if (manager != null && manager != gameObject) Keep(manager);
    }

    // Two things destroy it otherwise (seen in game, without NineSolsAPI):
    //  - At startup, right after BepInEx loads the plugins, Unity destroys the manager object when the
    //    first scene loads (DontDestroyOnLoad does not hold yet): HideAndDontSave keeps it, as BepInEx's
    //    own HideManagerGameObject option would.
    //  - When the player quits to the title screen, the game destroys every GameObject not registered in
    //    RCGLifeCycle (DestroyAllGameObjects). DontDestroyForever only adds the object to a static list,
    //    so it is safe to call from Awake.
    // If either ever throws, the full exception goes to the log.
    private static void Keep(GameObject target) {
        try {
            target.hideFlags = HideFlags.HideAndDontSave;
            RCGLifeCycle.DontDestroyForever(target);
            Log.Info($"Registered '{target.name}' in RCGLifeCycle");
        } catch (Exception e) {
            Log.Error($"Could not keep '{target.name}' alive: {e}");
        }
    }
}
