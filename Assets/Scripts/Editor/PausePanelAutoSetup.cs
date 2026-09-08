#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

namespace TrueColors.EditorTools
{
    public static class PausePanelAutoSetup
    {
        [MenuItem("Tools/PausePanel/Aplicar Rediseño a Content")]
        public static void SetupPausePanelContent()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "MainGame")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[PausePanelTool] No se encontró Canvas en MainGame.");
                return;
            }

            Transform pausePanelTrans = canvas.transform.Find("Panels/PausePanel");
            if (pausePanelTrans == null)
            {
                Debug.LogError("[PausePanelTool] No se encontró PausePanel.");
                return;
            }

            Transform contentTrans = pausePanelTrans.Find("Container/Content");
            if (contentTrans == null)
            {
                Debug.LogError("[PausePanelTool] No se encontró Container/Content.");
                return;
            }

            var gm = Object.FindFirstObjectByType<GameManager>();

            // 1. Configurar Score (Zona Superior)
            Transform scoreTrans = contentTrans.Find("Score");
            if (scoreTrans != null)
            {
                var scoreRect = scoreTrans.GetComponent<RectTransform>();
                scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
                scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
                scoreRect.pivot = new Vector2(0.5f, 0.5f);
                scoreRect.anchoredPosition = new Vector2(0f, 95f);
                scoreRect.sizeDelta = new Vector2(500f, 240f);

                // 1a. ScoreLabel
                Transform labelTrans = scoreTrans.Find("ScoreLabel");
                if (labelTrans != null)
                {
                    var labelRect = labelTrans.GetComponent<RectTransform>();
                    labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    labelRect.pivot = new Vector2(0.5f, 0.5f);
                    labelRect.anchoredPosition = new Vector2(0f, 75f);
                    labelRect.sizeDelta = new Vector2(400f, 45f);

                    var labelTMP = labelTrans.GetComponent<TextMeshProUGUI>();
                    if (labelTMP != null)
                    {
                        labelTMP.text = "SCORE";
                        labelTMP.fontSize = 34f;
                        labelTMP.alignment = TextAlignmentOptions.Center;
                        labelTMP.characterSpacing = 4f;
                        labelTMP.color = Color.white;
                    }
                }

                // 1b. ScoreBackground
                Transform bgTrans = scoreTrans.Find("ScoreBackground");
                if (bgTrans != null)
                {
                    var bgRect = bgTrans.GetComponent<RectTransform>();
                    bgRect.anchorMin = new Vector2(0.5f, 0.5f);
                    bgRect.anchorMax = new Vector2(0.5f, 0.5f);
                    bgRect.pivot = new Vector2(0.5f, 0.5f);
                    bgRect.anchoredPosition = new Vector2(0f, -10f);
                    bgRect.sizeDelta = new Vector2(420f, 115f);

                    // 1c. ScoreValue
                    Transform valTrans = bgTrans.Find("ScoreValue");
                    if (valTrans != null)
                    {
                        var valRect = valTrans.GetComponent<RectTransform>();
                        valRect.anchorMin = Vector2.zero;
                        valRect.anchorMax = Vector2.one;
                        valRect.pivot = new Vector2(0.5f, 0.5f);
                        valRect.offsetMin = Vector2.zero;
                        valRect.offsetMax = Vector2.zero;

                        var valTMP = valTrans.GetComponent<TextMeshProUGUI>();
                        if (valTMP != null)
                        {
                            valTMP.text = "0";
                            valTMP.fontSize = 48f;
                            valTMP.enableAutoSizing = true;
                            valTMP.fontSizeMin = 28f;
                            valTMP.fontSizeMax = 50f;
                            valTMP.alignment = TextAlignmentOptions.Center;
                            valTMP.color = Color.white;
                        }
                    }
                }
            }

            // 2. Configurar Buttons (Zona Inferior)
            Transform buttonsTrans = contentTrans.Find("Buttons");
            if (buttonsTrans != null)
            {
                var buttonsRect = buttonsTrans.GetComponent<RectTransform>();
                buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonsRect.pivot = new Vector2(0.5f, 0.5f);
                buttonsRect.anchoredPosition = new Vector2(0f, -130f);
                buttonsRect.sizeDelta = new Vector2(600f, 180f);

                var layout = buttonsTrans.GetComponent<HorizontalLayoutGroup>();
                if (layout == null) layout = buttonsTrans.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 45f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                // 2a. BtnMenu
                Transform menuTrans = buttonsTrans.Find("BtnMenu");
                if (menuTrans != null)
                {
                    var menuRect = menuTrans.GetComponent<RectTransform>();
                    menuRect.sizeDelta = new Vector2(150f, 150f);

                    Transform menuIcon = menuTrans.Find("Icon");
                    if (menuIcon != null)
                    {
                        var iconRect = menuIcon.GetComponent<RectTransform>();
                        iconRect.sizeDelta = new Vector2(80f, 80f);
                        iconRect.anchoredPosition = new Vector2(-3f, 0f);
                    }

                    var menuBtn = menuTrans.GetComponent<Button>();
                    if (menuBtn != null && gm != null)
                    {
                        menuBtn.navigation = new Navigation { mode = Navigation.Mode.None };
                        while (menuBtn.onClick.GetPersistentEventCount() > 0)
                        {
                            UnityEventTools.RemovePersistentListener(menuBtn.onClick, 0);
                        }
                        UnityEventTools.AddPersistentListener(menuBtn.onClick, gm.GoToMainMenu);
                    }
                }

                // 2b. BtnSettings
                Transform settingsTrans = buttonsTrans.Find("BtnSettings");
                if (settingsTrans != null)
                {
                    var settingsRect = settingsTrans.GetComponent<RectTransform>();
                    settingsRect.sizeDelta = new Vector2(150f, 150f);

                    Transform settingsIcon = settingsTrans.Find("Icon");
                    if (settingsIcon != null)
                    {
                        var iconRect = settingsIcon.GetComponent<RectTransform>();
                        iconRect.sizeDelta = new Vector2(80f, 80f);
                        iconRect.anchoredPosition = new Vector2(-3f, 0f);
                    }

                    var settingsBtn = settingsTrans.GetComponent<Button>();
                    if (settingsBtn != null && gm != null)
                    {
                        settingsBtn.navigation = new Navigation { mode = Navigation.Mode.None };
                        while (settingsBtn.onClick.GetPersistentEventCount() > 0)
                        {
                            UnityEventTools.RemovePersistentListener(settingsBtn.onClick, 0);
                        }
                        UnityEventTools.AddPersistentListener(settingsBtn.onClick, gm.OpenSettings);
                    }
                }

                // 2c. BtnPlay
                Transform playTrans = buttonsTrans.Find("BtnPlay");
                if (playTrans != null)
                {
                    var playRect = playTrans.GetComponent<RectTransform>();
                    playRect.sizeDelta = new Vector2(150f, 150f);

                    Transform playIcon = playTrans.Find("Icon");
                    if (playIcon != null)
                    {
                        var iconRect = playIcon.GetComponent<RectTransform>();
                        iconRect.sizeDelta = new Vector2(80f, 80f);
                        iconRect.anchoredPosition = new Vector2(-3f, 0f);
                    }

                    var playBtn = playTrans.GetComponent<Button>();
                    if (playBtn != null && gm != null)
                    {
                        playBtn.navigation = new Navigation { mode = Navigation.Mode.None };
                        while (playBtn.onClick.GetPersistentEventCount() > 0)
                        {
                            UnityEventTools.RemovePersistentListener(playBtn.onClick, 0);
                        }
                        UnityEventTools.AddPersistentListener(playBtn.onClick, gm.ResumeGame);
                    }
                }
            }

            // Marcar dirty y guardar escena limpiamente
            EditorUtility.SetDirty(pausePanelTrans.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#55FF55><b>[PausePanelTool]</b></color> PausePanel > Container > Content rediseñado y guardado con éxito.");
        }
    }
}
#endif
