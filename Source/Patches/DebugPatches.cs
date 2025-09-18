using HarmonyLib;

namespace LostSinner.Patches;

/// <summary>
/// Patches for debug use.
/// </summary>
internal static class DebugPatches {
    /// <summary>
    /// Force invincibility for debug purposes.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HeroController), "Awake")]
    private static void ForceInvincible() {
        CheatManager.Invincibility = CheatManager.InvincibilityStates.PreventDeath;
    }
}