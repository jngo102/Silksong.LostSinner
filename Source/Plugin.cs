using BepInEx;
using HarmonyLib;

namespace God;

/// <summary>
/// The main plugin class.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    private static Harmony _harmony = null!;
    
    private void Awake() {
        Log.Init(Logger);

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(typeof(Patches));
    }

    private void OnDestroy() {
        _harmony.UnpatchSelf();
    }
}