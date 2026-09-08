#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

namespace TrueColors.EditorTools
{
    [InitializeOnLoad]
    public static class ShopPanelAutoSetup
    {
        private const string PREF_KEY = "ShopPanelSetupApplied_v3";

        static ShopPanelAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    SetupShopPanel();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/Shop/Construir y Conectar ShopPanel en MainMenu")]
        public static void SetupShopPanel()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "MainMenu")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[ShopPanelTool] No se encontró Canvas en MainMenu.");
                return;
            }

            // 1. Reemplazar o crear ShopPanel
            Transform existingPanel = canvas.transform.Find("ShopPanel");
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            GameObject panelObj = new GameObject("ShopPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(canvas.transform, false);

            var rootRect = panelObj.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImg = panelObj.GetComponent<Image>();
            rootImg.color = new Color(0f, 0f, 0f, 0.92f);
            rootImg.raycastTarget = true;

            // 2. Cargar Sprites y Fuente
            var headerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/HEADER.png");
            var bodySmallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/BODY_SMALL.png");
            var footerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/FOOTER.png");
            var btnEmptySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_EMPTY.png");
            var btnIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_ICON.png");
            var menuSprite = LoadBestSprite("Assets/Art/Sprites/icons/MENU.png");
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Ethnocentric-Regular SDF.asset");

            // 3. Crear Container (1080 x 1920)
            GameObject container = new GameObject("Container", typeof(RectTransform));
            container.transform.SetParent(panelObj.transform, false);
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(1080f, 1920f);

            // 4. Header (940 x 130, Y: 380)
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            header.transform.SetParent(container.transform, false);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 0.5f);
            headerRect.anchorMax = new Vector2(0.5f, 0.5f);
            headerRect.pivot = new Vector2(0.5f, 0.5f);
            headerRect.anchoredPosition = new Vector2(0f, 380f);
            headerRect.sizeDelta = new Vector2(940f, 130f);

            var headerImg = header.GetComponent<Image>();
            headerImg.sprite = headerSprite;
            headerImg.color = Color.white;
            headerImg.raycastTarget = false;

            // Header -> Title (SHOP)
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "SHOP";
            if (fontAsset != null) titleTMP.font = fontAsset;
            titleTMP.fontSize = 48f;
            titleTMP.characterSpacing = 4f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;
            titleTMP.raycastTarget = false;

            // 5. Content (BODY_SMALL: 940 x 650, Y: 0)
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            content.transform.SetParent(container.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            contentRect.sizeDelta = new Vector2(940f, 650f);

            var contentImg = content.GetComponent<Image>();
            contentImg.sprite = bodySmallSprite;
            contentImg.color = Color.white;
            contentImg.raycastTarget = false;

            // 6. Fila 1: Botón NO ADS (Y: 145)
            GameObject btnNoAdsObj = CreateShopRowButton("BtnNoAds", content.transform, 145f, btnEmptySprite, "NO ADS", fontAsset);

            // 7. Fila 2: Botón SHIPS (Y: -25)
            GameObject btnShipsObj = CreateShopRowButton("BtnShips", content.transform, -25f, btnEmptySprite, "SHIPS", fontAsset);

            // 8. Botón Inferior Central: MENU (Y: -205)
            GameObject btnMenuObj = new GameObject("BtnMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
            btnMenuObj.transform.SetParent(content.transform, false);
            var btnMenuRect = btnMenuObj.GetComponent<RectTransform>();
            btnMenuRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnMenuRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnMenuRect.pivot = new Vector2(0.5f, 0.5f);
            btnMenuRect.anchoredPosition = new Vector2(0f, -205f);
            btnMenuRect.sizeDelta = new Vector2(150f, 150f);

            var btnMenuImg = btnMenuObj.GetComponent<Image>();
            btnMenuImg.sprite = btnIconSprite;
            btnMenuImg.type = Image.Type.Simple;
            btnMenuImg.color = Color.white;
            btnMenuImg.raycastTarget = true;

            var btnMenuComp = btnMenuObj.GetComponent<Button>();
            btnMenuComp.targetGraphic = btnMenuImg;
            btnMenuComp.navigation = new Navigation { mode = Navigation.Mode.None };
            var menuColors = btnMenuComp.colors;
            menuColors.normalColor = Color.white;
            menuColors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            menuColors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            btnMenuComp.colors = menuColors;

            // Icono MENU dentro del botón
            GameObject menuIconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            menuIconObj.transform.SetParent(btnMenuObj.transform, false);
            var menuIconRect = menuIconObj.GetComponent<RectTransform>();
            menuIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuIconRect.pivot = new Vector2(0.5f, 0.5f);
            menuIconRect.anchoredPosition = new Vector2(-3f, 0f);
            menuIconRect.sizeDelta = new Vector2(80f, 80f);

            var menuIconImg = menuIconObj.GetComponent<Image>();
            menuIconImg.sprite = menuSprite;
            menuIconImg.preserveAspect = true;
            menuIconImg.color = Color.white;
            menuIconImg.raycastTarget = false;

            // 9. Footer (940 x 35, Y: -342)
            GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            footer.transform.SetParent(container.transform, false);
            var footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.5f, 0.5f);
            footerRect.anchorMax = new Vector2(0.5f, 0.5f);
            footerRect.pivot = new Vector2(0.5f, 0.5f);
            footerRect.anchoredPosition = new Vector2(0f, -342f);
            footerRect.sizeDelta = new Vector2(940f, 35f);

            var footerImg = footer.GetComponent<Image>();
            footerImg.sprite = footerSprite;
            footerImg.color = Color.white;
            footerImg.raycastTarget = false;

            // 10. Configurar ShopModal
            var modal = panelObj.GetComponent<ShopModal>();
            if (modal == null) modal = panelObj.AddComponent<ShopModal>();

            var btnNoAdsComp = btnNoAdsObj.GetComponent<Button>();
            var btnShipsComp = btnShipsObj.GetComponent<Button>();

            var so = new SerializedObject(modal);
            so.FindProperty("noAdsButton").objectReferenceValue = btnNoAdsComp;
            so.FindProperty("shipsButton").objectReferenceValue = btnShipsComp;
            so.FindProperty("closeButton").objectReferenceValue = btnMenuComp;
            so.ApplyModifiedProperties();

            // Enlazar listeners persistentes
            WirePersistentEvent(btnNoAdsComp, modal.BuyNoAds);
            WirePersistentEvent(btnShipsComp, modal.OpenShips);
            WirePersistentEvent(btnMenuComp, modal.Close);

            // 11. Conectar a MainMenuController
            var menuController = Object.FindAnyObjectByType<MainMenuController>();
            if (menuController != null)
            {
                var menuSo = new SerializedObject(menuController);
                var pShop = menuSo.FindProperty("shopPanel");
                if (pShop != null)
                {
                    pShop.objectReferenceValue = panelObj;
                    menuSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(menuController);
                }
            }

            // Capa UI (5) recursiva
            SetLayerRecursively(panelObj, 5);

            // Ocultar modal inicialmente
            panelObj.SetActive(false);

            // Guardar cambios en la escena
            EditorUtility.SetDirty(panelObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#55FF55><b>[ShopPanelTool]</b></color> ShopPanel con BODY_SMALL, botones NO ADS y SHIPS construido exitosamente.");
        }

        private static GameObject CreateShopRowButton(string name, Transform parent, float yPos, Sprite btnSprite, string text, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, yPos);
            rect.sizeDelta = new Vector2(450f, 150f);

            var img = btnObj.GetComponent<Image>();
            img.sprite = btnSprite;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = true;

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            btn.colors = colors;

            // Texto dentro del botón
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(btnObj.transform, false);

            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelTMP = labelObj.GetComponent<TextMeshProUGUI>();
            labelTMP.text = text;
            if (font != null) labelTMP.font = font;
            labelTMP.fontSize = 40f;
            labelTMP.characterSpacing = 4f;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = Color.white;
            labelTMP.raycastTarget = false;

            return btnObj;
        }

        private static void WirePersistentEvent(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
            {
                while (btn.onClick.GetPersistentEventCount() > 0)
                    UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                UnityEventTools.AddPersistentListener(btn.onClick, action);
            }
        }

        private static Sprite LoadBestSprite(string path)
        {
            var main = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all != null && all.Length > 0)
            {
                Sprite best = null;
                float maxArea = 0;
                foreach (var a in all)
                {
                    if (a is Sprite s)
                    {
                        float area = s.rect.width * s.rect.height;
                        if (area > maxArea)
                        {
                            maxArea = area;
                            best = s;
                        }
                    }
                }
                if (best != null) return best;
            }
            return main;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
#endif
