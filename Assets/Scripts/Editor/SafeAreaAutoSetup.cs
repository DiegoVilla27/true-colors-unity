#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TrueColors.UI;

namespace TrueColors.EditorTools
{
    [InitializeOnLoad]
    public static class SafeAreaAutoSetup
    {
        private const string PREF_KEY = "SafeAreaAutoSetupApplied_v2";

        static SafeAreaAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    Execute();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/UI/Configurar Safe Area en MainMenu y MainGame")]
        public static void Execute()
        {
            Debug.Log("<color=#FFCC00><b>[SafeAreaTool]</b></color> Iniciando configuración de Safe Area en MainGame y MainMenu...");

            // 1. CONFIGURAR MAINGAME (HUD y BtnPause)
            var mainGameScene = EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
            var gameCanvas = GameObject.Find("Canvas");

            if (gameCanvas != null)
            {
                // Configurar HUD
                Transform hudTrans = gameCanvas.transform.Find("HUD");
                if (hudTrans != null)
                {
                    var safeArea = hudTrans.GetComponent<SafeArea>();
                    if (safeArea == null) safeArea = hudTrans.gameObject.AddComponent<SafeArea>();
                    
                    safeArea.SetMode(SafeAreaMode.TopInset);
                    safeArea.SetNotchFiller(true, Color.black);
                    safeArea.CaptureBaseValues();
                    safeArea.ApplySafeArea();
                    EditorUtility.SetDirty(hudTrans.gameObject);
                    Debug.Log("<color=#55FF55><b>[SafeAreaTool]</b></color> SafeArea configurado en MainGame -> HUD con NotchFiller negro.");
                }

                // Configurar BtnPause
                Transform pauseTrans = gameCanvas.transform.Find("BtnPause");
                if (pauseTrans != null)
                {
                    var safeArea = pauseTrans.GetComponent<SafeArea>();
                    if (safeArea == null) safeArea = pauseTrans.gameObject.AddComponent<SafeArea>();

                    safeArea.SetMode(SafeAreaMode.TopInset);
                    safeArea.SetNotchFiller(false, Color.black);
                    safeArea.CaptureBaseValues();
                    safeArea.ApplySafeArea();
                    EditorUtility.SetDirty(pauseTrans.gameObject);
                    Debug.Log("<color=#55FF55><b>[SafeAreaTool]</b></color> SafeArea configurado en MainGame -> BtnPause.");
                }

                // Configurar Boost (indicador de Power-Up en la esquina superior izquierda)
                Transform boostTrans = gameCanvas.transform.Find("Boost");
                if (boostTrans != null)
                {
                    var safeArea = boostTrans.GetComponent<SafeArea>();
                    if (safeArea == null) safeArea = boostTrans.gameObject.AddComponent<SafeArea>();

                    safeArea.SetMode(SafeAreaMode.TopInset);
                    safeArea.SetNotchFiller(false, Color.black);
                    safeArea.SetBaseAnchoredPosition(new Vector2(30f, -130f));
                    safeArea.ApplySafeArea();
                    EditorUtility.SetDirty(boostTrans.gameObject);
                    Debug.Log("<color=#55FF55><b>[SafeAreaTool]</b></color> SafeArea configurado en MainGame -> Boost.");
                }

                // Configurar ComboText (indicador de combo en la zona superior central)
                Transform comboTrans = gameCanvas.transform.Find("ComboText");
                if (comboTrans != null)
                {
                    var safeArea = comboTrans.GetComponent<SafeArea>();
                    if (safeArea == null) safeArea = comboTrans.gameObject.AddComponent<SafeArea>();

                    safeArea.SetMode(SafeAreaMode.TopInset);
                    safeArea.SetNotchFiller(false, Color.black);
                    safeArea.SetBaseValues(new Vector2(0f, -180f), new Vector2(0f, -230f), new Vector2(0f, -180f));
                    safeArea.ApplySafeArea();
                    EditorUtility.SetDirty(comboTrans.gameObject);
                    Debug.Log("<color=#55FF55><b>[SafeAreaTool]</b></color> SafeArea configurado en MainGame -> ComboText.");
                }
            }

            EditorSceneManager.MarkSceneDirty(mainGameScene);
            EditorSceneManager.SaveScene(mainGameScene);

            // 2. CONFIGURAR MAINMENU (Version)
            var mainMenuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var menuCanvas = GameObject.Find("Canvas");

            if (menuCanvas != null)
            {
                Transform versionTrans = menuCanvas.transform.Find("Version");
                if (versionTrans != null)
                {
                    var safeArea = versionTrans.GetComponent<SafeArea>();
                    if (safeArea == null) safeArea = versionTrans.gameObject.AddComponent<SafeArea>();

                    safeArea.SetMode(SafeAreaMode.BottomInset);
                    safeArea.CaptureBaseValues();
                    safeArea.ApplySafeArea();
                    EditorUtility.SetDirty(versionTrans.gameObject);
                    Debug.Log("<color=#55FF55><b>[SafeAreaTool]</b></color> SafeArea configurado en MainMenu -> Version.");
                }
            }

            EditorSceneManager.MarkSceneDirty(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene);

            Debug.Log("<color=#55FF55><b>[SafeAreaTool]</b></color> ¡ÉXITO! Safe Area configurado y guardado en ambas escenas.");
        }
    }
}
#endif
