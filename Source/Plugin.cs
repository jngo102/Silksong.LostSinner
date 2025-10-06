using BepInEx;
using HarmonyLib;
using LostSinner.Patches;

namespace LostSinner;

/// <summary>
/// The main plugin class.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    private static Harmony _harmony = null!;

    private void Awake() {
        Log.Init(Logger);
        
        AssetManager.LoadTextures();

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        #if DEBUG
        _harmony.PatchAll(typeof(DebugPatches));
        #endif
        MainPatches.Initialize();
        _harmony.PatchAll(typeof(MainPatches));
    }

    private void OnDestroy() {
        _harmony.UnpatchSelf();
        AssetManager.UnloadAll();
    }
}