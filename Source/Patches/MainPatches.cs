using HarmonyLib;
using System.Collections;

namespace LostSinner.Patches;

/// <summary>
/// Persistent patches for the mod.
/// </summary>
internal static class MainPatches {
    /// <summary>
    /// Private MainPatches Harmony instance.
    /// </summary>
    private static Harmony _harmony = null!;

    /// <summary>
    /// Initialize the main patches class.
    /// </summary>
    internal static void Initialize() {
        _harmony = new Harmony(nameof(MainPatches));
    }

    /// <summary>
    /// Check whether to run <see cref="SinnerPatches">sinner patches</see>.
    /// </summary>
    /// <param name="__instance">The game manager instance.</param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.SetLoadedGameData), typeof(SaveGameData), typeof(int))]
    private static void CheckPatchSinner(GameManager __instance) {
        __instance.GetSaveStatsForSlot(PlayerData.instance.profileID, (saveStats, _) => {
            if (saveStats.IsAct3) {
                _harmony.PatchAll(typeof(SinnerPatches));
            }
        });
    }

    /// <summary>
    /// Unpatch <see cref="SinnerPatches">boss-related patches</see> when returning to the main menu.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMainMenu))]
    private static IEnumerator UnpatchSinner(IEnumerator result) {
        while (result.MoveNext()) {
            yield return result.Current;
        }
        
        _harmony.UnpatchSelf();
    }
}