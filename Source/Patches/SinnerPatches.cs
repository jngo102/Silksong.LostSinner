using HarmonyLib;
using LostSinner.Behaviours;
using TeamCherry.Localization;
using UnityEngine;

namespace LostSinner.Patches;

/// <summary>
/// SinnerPatches methods for the mod.
/// </summary>
internal static class SinnerPatches {
    /// <summary>
    /// Modify the final boss behavior.
    /// </summary>
    /// <param name="__instance">The instance.</param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    private static void ModifySinner(PlayMakerFSM __instance) {
        if (__instance.name == "First Weaver" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies")) {
            __instance.gameObject.AddComponent<Sinner>();
        } else if (__instance.name == "Pin Projectiles") {
            __instance.gameObject.AddComponent<PinProjectiles>();
        }
    }

    /// <summary>
    /// Change the First Sinner's boss title.
    /// </summary>
    /// <param name="key">The language key associated with a text value.</param>
    /// <param name="sheetTitle">The sheet title containing the language key and text value.</param>
    /// <param name="__result">The resulting localized text.</param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Language), nameof(Language.Get), typeof(string), typeof(string))]
    private static void ChangeSinnerTitle(string key, string sheetTitle, ref string __result) {
        __result = key switch {
            "FIRST_WEAVER_SUPER" => Language.CurrentLanguage() switch {
                LanguageCode.DE => "Verlorene",
                LanguageCode.EN => "Lost",
                LanguageCode.JA => "漆黒の",
                LanguageCode.KO => "광기에 빠진",
                LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "失心",
                _ => __result
            },
            "FIRST_WEAVER_SUB" => Language.CurrentLanguage() switch {
                LanguageCode.ES => "Perdida",
                LanguageCode.FR => "Déchue",
                LanguageCode.IT => "Perduta",
                LanguageCode.PT => "Perdida",
                _ => __result
            },
            _ => __result
        };
    }
}