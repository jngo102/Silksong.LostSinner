using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace LostSinner;

/// <summary>
/// Manages all loaded assets in the mod.
/// </summary>
internal static class AssetManager {
    private static string[] _bundleNames = new[] {
        "localpoolprefabs_assets_laceboss"
    };

    private static string[] _assetNames = new[] {
        // "Abyss Bullet",
        "Abyss Vomit Glob",
        "Audio Player Actor Simple",
        "Lost Lace Ground Tendril",
        "Lost Lace Summon Bullet",
        "mini_mawlek_spit"
    };

    private static readonly Dictionary<Type, Dictionary<string, Object>> Assets = new();

    /// <summary>
    /// Load all desired assets from loaded asset bundles.
    /// </summary>
    internal static void Initialize() {
        foreach (string bundleName in _bundleNames) {
            string platformFolder = Application.platform switch {
                RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
                RuntimePlatform.OSXPlayer => "StandaloneOSX",
                RuntimePlatform.LinuxPlayer => "StandaloneLinux64",
                _ => ""
            };

            string bundlePath = Path.Combine(Addressables.RuntimePath, platformFolder, $"{bundleName}.bundle");
            var bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
            bundleLoadRequest.completed += _ => {
                AssetBundle bundle = bundleLoadRequest.assetBundle;
                foreach (var assetPath in bundle.GetAllAssetNames()) {
                    foreach (var assetName in _assetNames) {
                        if (assetPath.Contains(assetName)) {
                            var assetLoadRequest = bundle.LoadAssetAsync(assetPath);
                            assetLoadRequest.completed += _ => {
                                var loadedAsset = assetLoadRequest.asset;
                                
                                if (loadedAsset is GameObject prefab && loadedAsset.name == "Lost Lace Ground Tendril") {
                                    Log.Info("Tendrils");
                                    var tendrilConstraints = prefab.GetComponent<ConstrainPosition>();
                                tendrilConstraints.xMax = 100;
                                tendrilConstraints.yMin = 0;
                                tendrilConstraints.yMax = 100;
                                var tendrilControl = prefab.LocateMyFSM("Control");
                                foreach (var tendrilState in tendrilControl.FsmStates) {
                                    if (tendrilState.Name == "Antic") {
                                        var anticActions = tendrilState.Actions;
                                        foreach (var action in anticActions) {
                                            switch (action) {
                                                case SetFloatValue setFloat:
                                                    setFloat.floatValue.Value = 13 - 2;
                                                    break;
                                                case EaseFloat easeFloat:
                                                    easeFloat.fromValue.Value = 13 - 2;
                                                    easeFloat.toValue.Value = 13;
                                                    break;
                                            }
                                        }

                                        tendrilState.Actions = anticActions;
                                    } else if (tendrilState.Name == "Recycle") {
                                        var recycleActions = tendrilState.Actions;
                                        foreach (var action in recycleActions) {
                                            if (action is SetPosition2D setPosition) {
                                                setPosition.Y.Value = 13 - 2;
                                            }
                                        }

                                        tendrilState.Actions = recycleActions;
                                    }
                                }

                                prefab.CreatePool(6);
                                }
                                
                                var assetType = loadedAsset.GetType();
                                if (Assets.TryGetValue(assetType, out var assetEntry)) {
                                    if (assetEntry.ContainsKey(assetName)) {
                                        Log.Warn($"There is already an asset \"{assetName}\" of type \"{assetType}\"!");
                                    } else {
                                        Log.Debug($"Adding asset {assetName} of type {assetType}");
                                        assetEntry.Add(assetName, loadedAsset);
                                    }
                                } else {
                                    Assets.Add(assetType, new Dictionary<string, Object> { [assetName] = loadedAsset });
                                    Log.Debug(
                                        $"Added new sub-dictionary of type {assetType} with initial asset {assetName}");
                                }
                            };
                        }
                    }
                }
            };
        }

        foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles()) {
            foreach (var assetPath in bundle.GetAllAssetNames()) {
                if (_assetNames.Any(objName => assetPath.Contains(objName))) {
                    var assetLoadHandle = bundle.LoadAssetAsync(assetPath);
                    assetLoadHandle.completed += _ => {
                        var asset = assetLoadHandle.asset;
                        if (asset != null) {
                            Type assetType = asset.GetType();
                            string assetName = asset.name;
                            if (Assets.TryGetValue(assetType, out var assetEntry)) {
                                if (assetEntry.ContainsKey(assetName)) {
                                    Log.Warn($"There is already an asset \"{assetName}\" of type \"{assetType}\"!");
                                } else {
                                    Log.Debug($"Adding asset {assetName} of type {assetType}");
                                    assetEntry.Add(assetName, asset);
                                }
                            } else {
                                Assets.Add(assetType, new Dictionary<string, Object> { [assetName] = asset });
                                Log.Debug(
                                    $"Added new sub-dictionary of type {assetType} with initial asset {assetName}");
                            }
                        }
                    };
                }
            }
        }
    }

    /// <summary>
    /// Unload all saved assets.
    /// </summary>
    internal static void Unload() {
        foreach (var assetDict in Assets.Values) {
            foreach (var asset in assetDict.Values) {
                Object.DestroyImmediate(asset);
            }
        }

        Assets.Clear();
        GC.Collect();
    }

    /// <summary>
    /// Fetch an asset.
    /// </summary>
    /// <param name="assetName">The name of the asset to fetch.</param>
    /// <param name="asset">The variable to output the found asset to.</param>
    /// <typeparam name="T">The type of asset to fetch.</typeparam>
    internal static bool TryGet<T>(string assetName, out T? asset) where T : Object {
        if (Assets.TryGetValue(typeof(T), out var subDict)) {
            if (subDict.TryGetValue(assetName, out var assetObj)) {
                asset = assetObj as T;
                return true;
            }

            Log.Error($"Failed to get asset {assetName}!");
            asset = null;
            return false;
        }

        Log.Error($"Failed to get sub-dictionary of type {typeof(T)}!");
        asset = null;
        return false;
    }
}