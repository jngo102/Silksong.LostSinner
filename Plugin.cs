using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostSinner;

/// <summary>
/// The main plugin class.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    private static Harmony _harmony = null!;

    internal static Texture2D[] AtlasTextures = new Texture2D[2];

    private void Awake() {
        Log.Init(Logger);

        LoadSinnerTextures();

        SceneManager.activeSceneChanged += OnSceneChange;
    }

    private void OnSceneChange(Scene oldScene, Scene newScene) {
        // Only change things when loading a save file
        if (oldScene.name != "Menu_Title") {
            return;
        }
        
        // Remove all patches when returning to the main menu
        if (newScene.name == "Menu_Title") {
            AssetManager.UnloadCustomBundles();
            _harmony.UnpatchSelf();
        } else {
            AssetManager.Initialize();

            if (GameManager.GetSaveStatsFromData(GameManager.instance.GetSaveGameData(PlayerData.instance.profileID))
                .IsAct3) {
                _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
                _harmony.PatchAll(typeof(Patches));
            }
        }
    }

    /// <summary>
    /// Load textures embedded in the assembly.
    /// </summary>
    private void LoadSinnerTextures() {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (string resourceName in assembly.GetManifestResourceNames()) {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            if (resourceName.Contains("atlas0")) {
                var buffer = new byte[stream.Length];
                int read = stream.Read(buffer, 0, buffer.Length);
                var atlasTex = new Texture2D(2, 2);
                atlasTex.LoadImage(buffer);
                AtlasTextures[0] = atlasTex;
            } else if (resourceName.Contains("atlas1")) {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                var atlasTex = new Texture2D(2, 2);
                atlasTex.LoadImage(buffer);
                AtlasTextures[1] = atlasTex;
            }
        }
    }

    private void OnDestroy() {
        _harmony.UnpatchSelf();
        AssetManager.UnloadAll();
    }
}