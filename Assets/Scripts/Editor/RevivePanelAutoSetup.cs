#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

namespace TrueColors.EditorTools
{
    [InitializeOnLoad]
    public static class RevivePanelAutoSetup
    {
        private const string PREF_KEY = "RevivePanelSetupApplied_v2";

        static RevivePanelAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    SetupRevivePanel();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/Revive/Construir y Conectar RevivePanel")]
        public static void SetupRevivePanel()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "MainGame")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[RevivePanelTool] No se encontró Canvas en MainGame.");
                return;
            }

            Transform panelsTrans = canvas.transform.Find("Panels");
            if (panelsTrans == null)
            {
                Debug.LogError("[RevivePanelTool] No se encontró Canvas/Panels.");
                return;
            }

            var gm = Object.FindFirstObjectByType<GameManager>();
            var hud = Object.FindFirstObjectByType<GameHUD>();

            // 1. Cargar o crear RevivePanel bajo Panels
            Transform revivePanelTrans = panelsTrans.Find("RevivePanel");
            GameObject revivePanelObj;
            if (revivePanelTrans == null)
            {
                revivePanelObj = new GameObject("RevivePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                revivePanelObj.transform.SetParent(panelsTrans, false);
                revivePanelTrans = revivePanelObj.transform;
            }
            else
            {
                revivePanelObj = revivePanelTrans.gameObject;
            }

            // Configurar raíz de RevivePanel
            var rootRect = revivePanelObj.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImg = revivePanelObj.GetComponent<Image>();
            if (rootImg == null) rootImg = revivePanelObj.AddComponent<Image>();
            rootImg.color = new Color(0f, 0f, 0f, 0.98f);
            rootImg.raycastTarget = true;

            // Limpiar hijos existentes para garantizar estructura limpia
            while (revivePanelTrans.childCount > 0)
            {
                Object.DestroyImmediate(revivePanelTrans.GetChild(0).gameObject);
            }

            // 2. Cargar Assets visuales
            var headerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/HEADER.png");
            var bodySmallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/BODY_SMALL.png");
            var footerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/FOOTER.png");
            var btnIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_ICON.png");

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Ethnocentric-Regular SDF.asset");

            Sprite cancelSprite = GetSubSprite("Assets/Art/Sprites/icons/CANCEL.png", "CANCEL_0");
            Sprite confirmSprite = GetSubSprite("Assets/Art/Sprites/icons/CONFIRM.png", "CONFIRM_0");
            Sprite adsSprite = GetSubSprite("Assets/Art/Sprites/icons/ADS_ACTIVE.png", "ADS_ACTIVE_0");

            // 3. Crear Container (1080 x 1920)
            GameObject container = new GameObject("Container", typeof(RectTransform));
            container.transform.SetParent(revivePanelTrans, false);
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(1080f, 1920f);

            // 4. Header (920 x 120, Y: 380)
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            header.transform.SetParent(container.transform, false);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.5f);
            headerRect.anchorMax = new Vector2(1f, 0.5f);
            headerRect.pivot = new Vector2(0.5f, 0.5f);
            headerRect.anchoredPosition = new Vector2(0f, 380f);
            headerRect.sizeDelta = new Vector2(-160f, 120f);

            var headerImg = header.GetComponent<Image>();
            headerImg.sprite = headerSprite;
            headerImg.type = Image.Type.Sliced;
            headerImg.color = Color.white;

            // Header -> Title (REVIVE)
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "REVIVE";
            if (fontAsset != null) titleTMP.font = fontAsset;
            titleTMP.fontSize = 50f;
            titleTMP.characterSpacing = 4f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;

            // 5. Content (920 x 650, Y: 0)
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            content.transform.SetParent(container.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(1f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            contentRect.sizeDelta = new Vector2(-160f, 650f);

            var contentImg = content.GetComponent<Image>();
            contentImg.sprite = bodySmallSprite;
            contentImg.type = Image.Type.Sliced;
            contentImg.color = Color.white;

            // 5a. Texto 1 (Pregunta: DO YOU WANT TO REVIVE?) en Y: 140
            GameObject questionObj = new GameObject("QuestionText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            questionObj.transform.SetParent(content.transform, false);
            var qRect = questionObj.GetComponent<RectTransform>();
            qRect.anchorMin = new Vector2(0.5f, 0.5f);
            qRect.anchorMax = new Vector2(0.5f, 0.5f);
            qRect.pivot = new Vector2(0.5f, 0.5f);
            qRect.anchoredPosition = new Vector2(0f, 140f);
            qRect.sizeDelta = new Vector2(780f, 140f);

            var qTMP = questionObj.GetComponent<TextMeshProUGUI>();
            qTMP.text = "DO YOU WANT\nTO REVIVE?";
            if (fontAsset != null) qTMP.font = fontAsset;
            qTMP.fontSize = 42f;
            qTMP.characterSpacing = 3f;
            qTMP.lineSpacing = 10f;
            qTMP.alignment = TextAlignmentOptions.Center;
            qTMP.color = Color.white;

            // 5b. Texto 2 (Explicación: YOU WILL KEEP THE SAME POINTS.) en Y: 10
            GameObject explainObj = new GameObject("ExplanationText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            explainObj.transform.SetParent(content.transform, false);
            var eRect = explainObj.GetComponent<RectTransform>();
            eRect.anchorMin = new Vector2(0.5f, 0.5f);
            eRect.anchorMax = new Vector2(0.5f, 0.5f);
            eRect.pivot = new Vector2(0.5f, 0.5f);
            eRect.anchoredPosition = new Vector2(0f, 10f);
            eRect.sizeDelta = new Vector2(780f, 120f);

            var eTMP = explainObj.GetComponent<TextMeshProUGUI>();
            eTMP.text = "YOU WILL KEEP\nTHE SAME POINTS.";
            if (fontAsset != null) eTMP.font = fontAsset;
            eTMP.fontSize = 34f;
            eTMP.characterSpacing = 3f;
            eTMP.lineSpacing = 10f;
            eTMP.alignment = TextAlignmentOptions.Center;
            eTMP.color = Color.white;

            // 5c. Fila de Botones (Y: -160)
            GameObject buttonsObj = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonsObj.transform.SetParent(content.transform, false);
            var buttonsRect = buttonsObj.GetComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonsRect.pivot = new Vector2(0.5f, 0.5f);
            buttonsRect.anchoredPosition = new Vector2(0f, -160f);
            buttonsRect.sizeDelta = new Vector2(450f, 180f);

            var layout = buttonsObj.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 45f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Botón 1: CANCEL (Cruz) -> CloseReviveModal
            GameObject btnCancelObj = CreateSquareButton("BtnCancel", buttonsObj.transform, btnIconSprite, cancelSprite, 150f, 80f);
            var btnCancel = btnCancelObj.GetComponent<Button>();
            if (gm != null)
            {
                while (btnCancel.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(btnCancel.onClick, 0);
                }
                UnityEventTools.AddPersistentListener(btnCancel.onClick, gm.CloseReviveModal);
            }

            // Botón 2: CONFIRM (Checkmark) -> RequestReviveWithAd
            GameObject btnConfirmObj = CreateSquareButton("BtnConfirm", buttonsObj.transform, btnIconSprite, confirmSprite, 150f, 80f);
            var btnConfirm = btnConfirmObj.GetComponent<Button>();
            if (gm != null)
            {
                while (btnConfirm.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(btnConfirm.onClick, 0);
                }
                UnityEventTools.AddPersistentListener(btnConfirm.onClick, gm.RequestReviveWithAd);
            }

            // 6. Footer (920 x 40, Y: -345)
            GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            footer.transform.SetParent(container.transform, false);
            var footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 0.5f);
            footerRect.anchorMax = new Vector2(1f, 0.5f);
            footerRect.pivot = new Vector2(0.5f, 0.5f);
            footerRect.anchoredPosition = new Vector2(0f, -345f);
            footerRect.sizeDelta = new Vector2(-160f, 40f);

            var footerImg = footer.GetComponent<Image>();
            footerImg.sprite = footerSprite;
            footerImg.type = Image.Type.Sliced;
            footerImg.color = Color.white;

            // 7. Configurar botón ADS_ACTIVE en GameOverPanel para abrir RevivePanel
            Transform gameOverPanelTrans = panelsTrans.Find("GameOverPanel");
            if (gameOverPanelTrans != null)
            {
                Transform headerTrans = gameOverPanelTrans.Find("Container/Header");
                if (headerTrans != null)
                {
                    Transform adsTrans = headerTrans.Find("BtnAdsRevive") ?? headerTrans.Find("AdsIcon");
                    if (adsTrans != null)
                    {
                        adsTrans.name = "BtnAdsRevive";
                        var adsBtn = adsTrans.GetComponent<Button>();
                        if (adsBtn == null) adsBtn = adsTrans.gameObject.AddComponent<Button>();
                        adsBtn.navigation = new Navigation { mode = Navigation.Mode.None };

                        if (adsTrans.GetComponent<UIButtonPressEffect>() == null)
                        {
                            adsTrans.gameObject.AddComponent<UIButtonPressEffect>();
                        }

                        if (gm != null)
                        {
                            while (adsBtn.onClick.GetPersistentEventCount() > 0)
                            {
                                UnityEventTools.RemovePersistentListener(adsBtn.onClick, 0);
                            }
                            UnityEventTools.AddPersistentListener(adsBtn.onClick, gm.OpenReviveModal);
                        }
                    }
                }
            }

            // 8. Inyectar referencias en GameManager y GameHUD
            if (gm != null)
            {
                var soGm = new SerializedObject(gm);
                var pRevive = soGm.FindProperty("revivePanel");
                if (pRevive != null) pRevive.objectReferenceValue = revivePanelObj;
                soGm.ApplyModifiedProperties();
                EditorUtility.SetDirty(gm);
            }

            if (hud != null)
            {
                var soHud = new SerializedObject(hud);
                var pHudRevive = soHud.FindProperty("revivePanel");
                if (pHudRevive != null) pHudRevive.objectReferenceValue = revivePanelObj;
                soHud.ApplyModifiedProperties();
                EditorUtility.SetDirty(hud);
            }

            // Asegurar que ReviveModal esté presente y vinculado
            var modal = revivePanelObj.GetComponent<ReviveModal>();
            if (modal == null) modal = revivePanelObj.AddComponent<ReviveModal>();
            modal.AutoBindComponents();

            // Dejar RevivePanel desactivado por defecto
            revivePanelObj.SetActive(false);

            // Marcar dirty y guardar escena limpiamente
            EditorUtility.SetDirty(revivePanelObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#55FF55><b>[RevivePanelTool]</b></color> RevivePanel construido, conectado a ADS_ACTIVE y guardado con éxito.");
        }

        private static GameObject CreateSquareButton(string name, Transform parent, Sprite bgSprite, Sprite iconSprite, float btnSize, float iconSize)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(btnSize, btnSize);

            var img = btnObj.GetComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = btnObj.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            btn.colors = colors;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(btnObj.transform, false);
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-3f, 0f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            var iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;

            return btnObj;
        }

        private static Sprite GetSubSprite(string path, string subName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (a is Sprite s && s.name == subName) return s;
                }
                foreach (var a in assets)
                {
                    if (a is Sprite s) return s;
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
