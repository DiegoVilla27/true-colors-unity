#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TrueColors.EditorTools
{
    public static class ApplyUnifiedIconButtonsSetup
    {
        private const string APPLIED_KEY = "UnifiedIconButtonsSetup_v1";

        [InitializeOnLoadMethod]
        private static void RunSetupOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(APPLIED_KEY, false)) return;
                Execute();
                EditorPrefs.SetBool(APPLIED_KEY, true);
            };
        }

        [MenuItem("Tools/UI/Aplicar Tamaño Unificado a Todos los Botones Icon")]
        public static void Execute()
        {
            Debug.Log("<color=#FFCC00><b>[UnifiedIconTool]</b></color> Iniciando actualización global de consistencia para todos los botones tipo icon...");

            // 1. ACTUALIZAR MAINMENU
            var mainMenuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            // Reconstruir paneles modales de MainMenu con las nuevas dimensiones estándar
            SettingsPanelAutoSetup.SetupSettingsPanel();
            LeaderboardPanelAutoSetup.SetupLeaderboardPanel();
            ShopPanelAutoSetup.SetupShopPanel();

            // Ajustar botones de navegación en el Canvas principal de MainMenu
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                UpdateIconButton(canvas.transform.Find("Btn_Leaderboard"));
                UpdateIconButton(canvas.transform.Find("Btn_Options"));
                UpdateIconButton(canvas.transform.Find("Btn_Shop"));
            }

            EditorSceneManager.MarkSceneDirty(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene);
            Debug.Log("<color=#55FF55><b>[UnifiedIconTool]</b></color> Escena MainMenu guardada con éxito.");

            // 2. ACTUALIZAR MAINGAME
            var mainGameScene = EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");

            // Reconstruir paneles modales de MainGame con las nuevas dimensiones estándar
            PausePanelAutoSetup.SetupPausePanelContent();
            GameOverPanelAutoSetup.SetupGameOverPanel();
            RevivePanelAutoSetup.SetupRevivePanel();

            // Ajustar botón de pausa en HUD de MainGame
            var gameCanvas = GameObject.Find("Canvas");
            if (gameCanvas != null)
            {
                var btnPauseTrans = FindDeepChild(gameCanvas.transform, "BtnPause");
                if (btnPauseTrans != null)
                {
                    UpdateIconButton(btnPauseTrans);
                }
            }

            EditorSceneManager.MarkSceneDirty(mainGameScene);
            EditorSceneManager.SaveScene(mainGameScene);
            Debug.Log("<color=#55FF55><b>[UnifiedIconTool]</b></color> Escena MainGame guardada con éxito.");

            // 3. Volver a MainMenu como escena activa
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Debug.Log("<color=#55FF55><b>[UnifiedIconTool]</b></color> ¡ÉXITO TOTAL! Todos los botones tipo icon estandarizados a 150x150, iconos a 80x80 con X: -3.");
        }

        private static void UpdateIconButton(Transform btnTrans)
        {
            if (btnTrans == null) return;

            var btnRect = btnTrans.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.sizeDelta = new Vector2(150f, 150f);
            }

            var iconTrans = btnTrans.Find("Icon");
            if (iconTrans != null)
            {
                var iconRect = iconTrans.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(80f, 80f);
                    iconRect.anchoredPosition = new Vector2(-3f, 0f);
                }
            }

            EditorUtility.SetDirty(btnTrans.gameObject);
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
