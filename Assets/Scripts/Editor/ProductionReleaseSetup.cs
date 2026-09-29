#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace TrueColors.EditorTools
{
    [InitializeOnLoad]
    public static class ProductionReleaseSetup
    {
        private const string PREF_KEY = "ProductionReleaseSetupApplied_v1_0_0";
        private const string ICON_PATH = "Assets/Art/AppIcon/app_icon_1024.png";
        private const string BUNDLE_ID = "com.bbgames.truecolors";
        private const string COMPANY_NAME = "BBGames";
        private const string PRODUCT_NAME = "TrueColors";
        private const string APP_VERSION = "1.0.0";

        static ProductionReleaseSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    ApplyProductionSettings();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/Release/Apply Production Settings (Bundle ID, Icons, Orientation)")]
        public static void ApplyProductionSettings()
        {
            Debug.Log("<color=#00FFAA><b>[ProductionReleaseSetup]</b></color> Aplicando configuración de producción...");

            // 1. Identificadores y Nombres
            PlayerSettings.companyName = COMPANY_NAME;
            PlayerSettings.productName = PRODUCT_NAME;
            PlayerSettings.bundleVersion = APP_VERSION;

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BUNDLE_ID);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BUNDLE_ID);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, BUNDLE_ID);

            // 2. Orientación fija en Portrait
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // 3. Versiones Mobile
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8.0 Oreo
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            // 4. Asignación de App Icon
            var iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
            if (iconTexture != null)
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { iconTexture });
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { iconTexture });
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new[] { iconTexture });
                Debug.Log($"<color=#00FFAA><b>[ProductionReleaseSetup]</b></color> Icono '{ICON_PATH}' asignado exitosamente como ícono por defecto para todas las plataformas.");
            }
            else
            {
                Debug.LogWarning($"<color=#FFAA00><b>[ProductionReleaseSetup]</b></color> No se pudo encontrar el ícono en '{ICON_PATH}'. Asegúrate de que el archivo existe.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00FFAA><b>[ProductionReleaseSetup]</b></color> ✅ Configuración de producción completada con éxito:\n" +
                      $" - Bundle ID: {BUNDLE_ID}\n" +
                      $" - Company: {COMPANY_NAME}\n" +
                      $" - Product: {PRODUCT_NAME}\n" +
                      $" - Version: {APP_VERSION} (Build Code: 1)\n" +
                      $" - Orientation: Portrait Only");
        }
    }
}
#endif
