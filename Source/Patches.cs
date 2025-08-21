namespace God;

/// <summary>
/// Patches methods for the mod.
/// </summary>
internal static class Patches {
    /// <summary>
    /// Modify the final boss behavior.
    /// </summary>
    /// <param name="__instance">The instance.</param>
    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(), nameof())]
    // private static void ModifyGod(__instance) {
    //     
    // }
    
    #if DEBUG
    /// <summary>
    /// Force invincibility for debug purposes.
    /// </summary>
    // private static void ForceInvincible(__instance) {
    //     
    // }
    #endif
}