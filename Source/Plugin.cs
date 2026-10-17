using System;
using BepInEx;
using HarmonyLib;
using NineSolsQoL.Core;
using NineSolsQoL.Features.Respawn;

namespace NineSolsQoL;

// Entry point. BepInEx creates this component when the game starts, or ScriptEngine does when
// the DLL is in BepInEx/scripts and you press F6.
//
// Each feature lives in Features/<Name>: its config section (Init), its Harmony patches and the
// code they forward to. The mod only depends on BepInEx, Harmony and the game itself.
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    private Harmony? harmony;

    private void Awake() {
        Log.Init(Logger);
        DebugTools.Init(Config);
        RespawnFeature.Init(Config);
        RegisterLifeCycle();

        // Applies every [HarmonyPatch] class in this assembly
        harmony = Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, MyPluginInfo.PLUGIN_GUID);

        Log.Info($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded on '{gameObject.name}'");
    }

    private void Update() {
        DebugTools.Update();
    }

    private void OnDestroy() {
        // A ScriptEngine reload creates a new instance: undo the patches so they are not applied twice
        harmony?.UnpatchSelf();
        Log.Info($"{MyPluginInfo.PLUGIN_NAME} unloaded");
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
