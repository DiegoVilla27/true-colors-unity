#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;
using TrueColors.Customization;
using TrueColors.UI;

namespace TrueColors.EditorTools
{
    [InitializeOnLoad]
    public static class ShopPanelAutoSetup
    {
        private const string PREF_KEY = "ShopPanelSetupApplied_v8";

        static ShopPanelAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    ExecuteFullSetup();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/Shop/Construir y Conectar ShopPanel y Naves en Todo el Proyecto")]
        public static void ExecuteFullSetup()
        {
            EnsureShipCatalogAsset();
            SetupShopPanel();
            ApplyShipSkinAppliersToScenes();
        }

        public static void EnsureShipCatalogAsset()
        {
            string resourcesDir = "Assets/Resources";
            if (!Directory.Exists(resourcesDir))
            {
                Directory.CreateDirectory(resourcesDir);
                AssetDatabase.Refresh();
            }

            string assetPath = "Assets/Resources/ShipCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<ShipCatalogSO>(assetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ShipCatalogSO>();
                AssetDatabase.CreateAsset(catalog, assetPath);
            }

            catalog.packPrice = 10000;
            catalog.ships.Clear();

            // 1. Classic (Default)
            var classicSprite = LoadBestSprite("Assets/Art/Sprites/starship/starship.png");
            catalog.ships.Add(new ShipDefinition
            {
                id = "ship_classic",
                displayName = "CLASSIC",
                sprite = classicSprite,
                isDefaultUnlocked = true,
                description = "Standard multi-role exploration rocket. Reliable and balanced."
            });

            // 2. Valkyrie
            var valkyrieSprite = LoadBestSprite("Assets/Art/Sprites/starship/ship_valkyrie.png");
            catalog.ships.Add(new ShipDefinition
            {
                id = "ship_valkyrie",
                displayName = "VALKYRIE",
                sprite = valkyrieSprite,
                isDefaultUnlocked = false,
                description = "Hypersonic agile interceptor with cutting-edge cyan energy wings."
            });

            // 3. Titan
            var titanSprite = LoadBestSprite("Assets/Art/Sprites/starship/ship_titan.png");
            catalog.ships.Add(new ShipDefinition
            {
                id = "ship_titan",
                displayName = "TITAN",
                sprite = titanSprite,
                isDefaultUnlocked = false,
                description = "Heavy armored cyber juggernaut engineered with reinforced hull plating."
            });

            // 4. Phantom
            var phantomSprite = LoadBestSprite("Assets/Art/Sprites/starship/ship_phantom.png");
            catalog.ships.Add(new ShipDefinition
            {
                id = "ship_phantom",
                displayName = "PHANTOM",
                sprite = phantomSprite,
                isDefaultUnlocked = false,
                description = "Stealth arrowhead fighter infused with neon-crimson plasma channels."
            });

            // 5. Solaris
            var solarisSprite = LoadBestSprite("Assets/Art/Sprites/starship/ship_solaris.png");
            catalog.ships.Add(new ShipDefinition
            {
                id = "ship_solaris",
                displayName = "SOLARIS",
                sprite = solarisSprite,
                isDefaultUnlocked = false,
                description = "Advanced solar vanguard vessel powered by a radiant fusion energy core."
            });

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#55FF55><b>[ShopPanelTool]</b></color> ShipCatalogSO configurado con 5 naves en Assets/Resources/ShipCatalog.asset.");
        }

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
            var bodyBigSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/BODY_BIG.png");
            var footerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/FOOTER.png");
            var highlightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/HIGHLIGHT.png");
            var btnActiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_ACTIVE.png");
            var btnEmptySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_EMPTY.png");
            var btnEmptyActiveSprite = LoadBestSprite("Assets/Art/Sprites/btns/BTN_EMPTY_ACTIVE.png");
            if (btnEmptyActiveSprite == null)
            {
                btnEmptyActiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_EMPTY_ACTIVE.png");
            }
            var btnIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_ICON.png");

            var menuSprite = LoadBestSprite("Assets/Art/Sprites/icons/MENU.png");
            var leftSprite = LoadBestSprite("Assets/Art/Sprites/icons/LEFT.png");
            var rightSprite = LoadBestSprite("Assets/Art/Sprites/icons/RIGHT.png");
            var rockIconSprite = LoadBestSprite("Assets/Art/Sprites/icons/ROCK.png");
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

            // =========================================================================
            // SUBVISTA 1: MAIN CATEGORY VIEW (NO ADS / SHIPS)
            // =========================================================================
            GameObject mainCategoryView = new GameObject("MainCategoryView", typeof(RectTransform));
            mainCategoryView.transform.SetParent(container.transform, false);
            var mainCatRect = mainCategoryView.GetComponent<RectTransform>();
            mainCatRect.anchorMin = Vector2.zero;
            mainCatRect.anchorMax = Vector2.one;
            mainCatRect.offsetMin = Vector2.zero;
            mainCatRect.offsetMax = Vector2.zero;

            // Header (940 x 130, Y: 380)
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            header.transform.SetParent(mainCategoryView.transform, false);
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

            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
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

            // Content (BODY_SMALL: 940 x 650, Y: 0)
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            content.transform.SetParent(mainCategoryView.transform, false);
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

            // Fila 1: Botón NO ADS (Y: 145)
            GameObject btnNoAdsObj = CreateShopRowButton("BtnNoAds", content.transform, 145f, btnEmptySprite, "NO ADS", fontAsset);

            // Fila 2: Botón SHIPS (Y: -25)
            GameObject btnShipsObj = CreateShopRowButton("BtnShips", content.transform, -25f, btnEmptySprite, "SHIPS", fontAsset);

            // Botón Inferior Central: MENU (Y: -205)
            GameObject btnMenuObj = CreateIconButton("BtnMenu", content.transform, new Vector2(0f, -205f), 150f, btnIconSprite, menuSprite, 80f);

            // Footer (940 x 35, Y: -342)
            GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            footer.transform.SetParent(mainCategoryView.transform, false);
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

            // =========================================================================
            // SUBVISTA 2: SHIPS VIEW (HANGAR DE NAVES)
            // =========================================================================
            GameObject shipsView = new GameObject("ShipsView", typeof(RectTransform));
            shipsView.transform.SetParent(container.transform, false);
            var shipsViewRect = shipsView.GetComponent<RectTransform>();
            shipsViewRect.anchorMin = Vector2.zero;
            shipsViewRect.anchorMax = Vector2.one;
            shipsViewRect.offsetMin = Vector2.zero;
            shipsViewRect.offsetMax = Vector2.zero;

            // Header (940 x 130, Y: 482)
            GameObject shipsHeader = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shipsHeader.transform.SetParent(shipsView.transform, false);
            var sHeaderRect = shipsHeader.GetComponent<RectTransform>();
            sHeaderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sHeaderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sHeaderRect.pivot = new Vector2(0.5f, 0.5f);
            sHeaderRect.anchoredPosition = new Vector2(0f, 482f);
            sHeaderRect.sizeDelta = new Vector2(940f, 130f);

            var sHeaderImg = shipsHeader.GetComponent<Image>();
            sHeaderImg.sprite = headerSprite;
            sHeaderImg.color = Color.white;
            sHeaderImg.raycastTarget = false;

            GameObject sTitleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            sTitleObj.transform.SetParent(shipsHeader.transform, false);
            var sTitleRect = sTitleObj.GetComponent<RectTransform>();
            sTitleRect.anchorMin = Vector2.zero;
            sTitleRect.anchorMax = Vector2.one;
            sTitleRect.offsetMin = new Vector2(50f, 0f);
            sTitleRect.offsetMax = new Vector2(-280f, 0f);

            var sTitleTMP = sTitleObj.GetComponent<TextMeshProUGUI>();
            sTitleTMP.text = "SHIPS";
            if (fontAsset != null) sTitleTMP.font = fontAsset;
            sTitleTMP.fontSize = 44f;
            sTitleTMP.characterSpacing = 3f;
            sTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            sTitleTMP.color = Color.white;
            sTitleTMP.raycastTarget = false;

            // Rocks Currency Counter en el Header (Derecha)
            GameObject rocksCounterObj = new GameObject("RocksCounter", typeof(RectTransform));
            rocksCounterObj.transform.SetParent(shipsHeader.transform, false);
            var rocksCounterRect = rocksCounterObj.GetComponent<RectTransform>();
            rocksCounterRect.anchorMin = new Vector2(1f, 0.5f);
            rocksCounterRect.anchorMax = new Vector2(1f, 0.5f);
            rocksCounterRect.pivot = new Vector2(1f, 0.5f);
            rocksCounterRect.anchoredPosition = new Vector2(-40f, 0f);
            rocksCounterRect.sizeDelta = new Vector2(250f, 80f);

            GameObject rockIconObj = new GameObject("RockIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rockIconObj.transform.SetParent(rocksCounterObj.transform, false);
            var rockIconRect = rockIconObj.GetComponent<RectTransform>();
            rockIconRect.anchorMin = new Vector2(0f, 0.5f);
            rockIconRect.anchorMax = new Vector2(0f, 0.5f);
            rockIconRect.pivot = new Vector2(0f, 0.5f);
            rockIconRect.anchoredPosition = new Vector2(0f, 0f);
            rockIconRect.sizeDelta = new Vector2(50f, 50f);

            var rockIconImg = rockIconObj.GetComponent<Image>();
            rockIconImg.sprite = rockIconSprite;
            rockIconImg.preserveAspect = true;
            rockIconImg.raycastTarget = false;

            GameObject rocksTextObj = new GameObject("RocksText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            rocksTextObj.transform.SetParent(rocksCounterObj.transform, false);
            var rocksTextRect = rocksTextObj.GetComponent<RectTransform>();
            rocksTextRect.anchorMin = new Vector2(0f, 0f);
            rocksTextRect.anchorMax = new Vector2(1f, 1f);
            rocksTextRect.offsetMin = new Vector2(60f, 0f);
            rocksTextRect.offsetMax = Vector2.zero;

            var rocksTMP = rocksTextObj.GetComponent<TextMeshProUGUI>();
            rocksTMP.text = "0";
            if (fontAsset != null) rocksTMP.font = fontAsset;
            rocksTMP.fontSize = 32f;
            rocksTMP.alignment = TextAlignmentOptions.MidlineLeft;
            rocksTMP.color = new Color(1f, 0.85f, 0.2f, 1f);
            rocksTMP.raycastTarget = false;

            // Content (BODY_BIG: 940 x 1000, Y: -80)
            GameObject shipsContent = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shipsContent.transform.SetParent(shipsView.transform, false);
            var sContentRect = shipsContent.GetComponent<RectTransform>();
            sContentRect.anchorMin = new Vector2(0.5f, 0.5f);
            sContentRect.anchorMax = new Vector2(0.5f, 0.5f);
            sContentRect.pivot = new Vector2(0.5f, 0.5f);
            sContentRect.anchoredPosition = new Vector2(0f, -80f);
            sContentRect.sizeDelta = new Vector2(940f, 1000f);

            var sContentImg = shipsContent.GetComponent<Image>();
            sContentImg.sprite = bodyBigSprite;
            sContentImg.color = Color.white;
            sContentImg.raycastTarget = false;

            // 1. Exhibición de Nave (Y: 345)
            GameObject showcaseContainer = new GameObject("ShowcaseContainer", typeof(RectTransform));
            showcaseContainer.transform.SetParent(shipsContent.transform, false);
            var scRect = showcaseContainer.GetComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.5f, 0.5f);
            scRect.anchorMax = new Vector2(0.5f, 0.5f);
            scRect.pivot = new Vector2(0.5f, 0.5f);
            scRect.anchoredPosition = new Vector2(0f, 345f);
            scRect.sizeDelta = new Vector2(860f, 210f);

            // Flecha Izquierda (<)
            GameObject btnPrevObj = CreateIconButton("BtnPrev", showcaseContainer.transform, new Vector2(-330f, 0f), 100f, btnIconSprite, leftSprite, 60f);
            // Flecha Derecha (>)
            GameObject btnNextObj = CreateIconButton("BtnNext", showcaseContainer.transform, new Vector2(330f, 0f), 100f, btnIconSprite, rightSprite, 60f);

            // Sprite Central con flotación suave
            GameObject shipPreviewObj = new GameObject("ShipPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MenuShipHover));
            shipPreviewObj.transform.SetParent(showcaseContainer.transform, false);
            var spRect = shipPreviewObj.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 0.5f);
            spRect.anchorMax = new Vector2(0.5f, 0.5f);
            spRect.pivot = new Vector2(0.5f, 0.5f);
            spRect.anchoredPosition = Vector2.zero;
            spRect.sizeDelta = new Vector2(210f, 210f);

            var spImg = shipPreviewObj.GetComponent<Image>();
            spImg.preserveAspect = true;
            spImg.raycastTarget = false;

            var hoverComp = shipPreviewObj.GetComponent<MenuShipHover>();
            var hoverSo = new SerializedObject(hoverComp);
            hoverSo.FindProperty("hoverAmplitude").floatValue = 8f;
            hoverSo.FindProperty("hoverFrequency").floatValue = 2f;
            hoverSo.ApplyModifiedProperties();

            // 2. Información de Nave: Nombre, Lore, Indicador de página
            GameObject shipNameObj = new GameObject("ShipName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            shipNameObj.transform.SetParent(shipsContent.transform, false);
            var snRect = shipNameObj.GetComponent<RectTransform>();
            snRect.anchorMin = new Vector2(0.5f, 0.5f);
            snRect.anchorMax = new Vector2(0.5f, 0.5f);
            snRect.pivot = new Vector2(0.5f, 0.5f);
            snRect.anchoredPosition = new Vector2(0f, 220f);
            snRect.sizeDelta = new Vector2(800f, 40f);

            var snTMP = shipNameObj.GetComponent<TextMeshProUGUI>();
            snTMP.text = "CLASSIC";
            if (fontAsset != null) snTMP.font = fontAsset;
            snTMP.fontSize = 40f;
            snTMP.characterSpacing = 3f;
            snTMP.alignment = TextAlignmentOptions.Center;
            snTMP.color = Color.white;
            snTMP.raycastTarget = false;

            GameObject shipDescObj = new GameObject("ShipDescription", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            shipDescObj.transform.SetParent(shipsContent.transform, false);
            var sdRect = shipDescObj.GetComponent<RectTransform>();
            sdRect.anchorMin = new Vector2(0.5f, 0.5f);
            sdRect.anchorMax = new Vector2(0.5f, 0.5f);
            sdRect.pivot = new Vector2(0.5f, 0.5f);
            sdRect.anchoredPosition = new Vector2(0f, 175f);
            sdRect.sizeDelta = new Vector2(820f, 50f);

            var sdTMP = shipDescObj.GetComponent<TextMeshProUGUI>();
            sdTMP.text = "Standard multi-role exploration rocket.";
            sdTMP.fontSize = 26f;
            sdTMP.textWrappingMode = TextWrappingModes.Normal;
            sdTMP.alignment = TextAlignmentOptions.Center;
            sdTMP.color = new Color(0.85f, 0.94f, 1f, 1f);
            sdTMP.raycastTarget = false;

            GameObject pageIndObj = new GameObject("PageIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            pageIndObj.transform.SetParent(shipsContent.transform, false);
            var piRect = pageIndObj.GetComponent<RectTransform>();
            piRect.anchorMin = new Vector2(0.5f, 0.5f);
            piRect.anchorMax = new Vector2(0.5f, 0.5f);
            piRect.pivot = new Vector2(0.5f, 0.5f);
            piRect.anchoredPosition = new Vector2(0f, 125f);
            piRect.sizeDelta = new Vector2(300f, 25f);

            var piTMP = pageIndObj.GetComponent<TextMeshProUGUI>();
            piTMP.text = "1 / 5";
            if (fontAsset != null) piTMP.font = fontAsset;
            piTMP.fontSize = 22f;
            piTMP.alignment = TextAlignmentOptions.Center;
            piTMP.color = new Color(0.6f, 0.75f, 0.88f, 1f);
            piTMP.raycastTarget = false;

            // 3. Fila de Miniaturas Rápidas (Y: 65)
            GameObject thumbRowObj = new GameObject("ThumbnailsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            thumbRowObj.transform.SetParent(shipsContent.transform, false);
            var trRect = thumbRowObj.GetComponent<RectTransform>();
            trRect.anchorMin = new Vector2(0.5f, 0.5f);
            trRect.anchorMax = new Vector2(0.5f, 0.5f);
            trRect.pivot = new Vector2(0.5f, 0.5f);
            trRect.anchoredPosition = new Vector2(0f, 65f);
            trRect.sizeDelta = new Vector2(580f, 75f);

            var hlg = thumbRowObj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            List<Button> thumbButtons = new List<Button>();
            List<Image> thumbImages = new List<Image>();

            for (int i = 0; i < 5; i++)
            {
                GameObject thumbBtnObj = new GameObject($"Thumb_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
                thumbBtnObj.transform.SetParent(thumbRowObj.transform, false);
                var tbRect = thumbBtnObj.GetComponent<RectTransform>();
                tbRect.sizeDelta = new Vector2(70f, 70f);

                var tbImg = thumbBtnObj.GetComponent<Image>();
                tbImg.sprite = btnIconSprite;
                tbImg.color = new Color(1f, 1f, 1f, 0.7f);

                var tbBtn = thumbBtnObj.GetComponent<Button>();
                tbBtn.targetGraphic = tbImg;
                thumbButtons.Add(tbBtn);

                // Icon inside thumbnail
                GameObject tIconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                tIconObj.transform.SetParent(thumbBtnObj.transform, false);
                var tiRect = tIconObj.GetComponent<RectTransform>();
                tiRect.anchorMin = new Vector2(0.5f, 0.5f);
                tiRect.anchorMax = new Vector2(0.5f, 0.5f);
                tiRect.pivot = new Vector2(0.5f, 0.5f);
                tiRect.sizeDelta = new Vector2(55f, 55f);

                var tiImg = tIconObj.GetComponent<Image>();
                tiImg.preserveAspect = true;
                tiImg.raycastTarget = false;
                thumbImages.Add(tiImg);
            }

            // 4. Botón Equipar (Y: -50, Tamaño estándar ShopPanel: 450 x 150)
            GameObject btnEquipObj = CreateShopRowButton("BtnEquip", shipsContent.transform, -50f, btnEmptySprite, "EQUIP", fontAsset, 450f, 150f, 40f);
            var btnEquipComp = btnEquipObj.GetComponent<Button>();
            var btnEquipImg = btnEquipObj.GetComponent<Image>();
            var equipLabelTMP = btnEquipObj.GetComponentInChildren<TextMeshProUGUI>();

            // 5. Texto de Feedback (Y: -140)
            GameObject feedbackObj = new GameObject("FeedbackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            feedbackObj.transform.SetParent(shipsContent.transform, false);
            var fbRect = feedbackObj.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.5f);
            fbRect.anchorMax = new Vector2(0.5f, 0.5f);
            fbRect.pivot = new Vector2(0.5f, 0.5f);
            fbRect.anchoredPosition = new Vector2(0f, -140f);
            fbRect.sizeDelta = new Vector2(750f, 25f);

            var fbTMP = feedbackObj.GetComponent<TextMeshProUGUI>();
            fbTMP.text = "";
            if (fontAsset != null) fbTMP.font = fontAsset;
            fbTMP.fontSize = 22f;
            fbTMP.alignment = TextAlignmentOptions.Center;
            fbTMP.color = Color.yellow;
            fbTMP.raycastTarget = false;
            feedbackObj.SetActive(false);

            // 6. Botón de Compra del Paquete (Y: -225, Tamaño estándar ShopPanel: 450 x 150)
            GameObject btnBuyPackObj = CreateShopRowButton("BtnBuyPack", shipsContent.transform, -225f, btnEmptySprite, "BUY PACK: 10,000", fontAsset, 450f, 150f, 32f);
            var btnBuyPackComp = btnBuyPackObj.GetComponent<Button>();
            var buyPackLabelTMP = btnBuyPackObj.GetComponentInChildren<TextMeshProUGUI>();

            // 7. Botón Volver / Cerrar (Y: -395, Tamaño estándar botón de icono ShopPanel: 150 x 150)
            GameObject btnBackObj = CreateIconButton("BtnBack", shipsContent.transform, new Vector2(0f, -395f), 150f, btnIconSprite, menuSprite, 80f);
            var btnBackComp = btnBackObj.GetComponent<Button>();

            // Footer (940 x 35, Y: -595)
            GameObject shipsFooter = new GameObject("Footer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shipsFooter.transform.SetParent(shipsView.transform, false);
            var sFooterRect = shipsFooter.GetComponent<RectTransform>();
            sFooterRect.anchorMin = new Vector2(0.5f, 0.5f);
            sFooterRect.anchorMax = new Vector2(0.5f, 0.5f);
            sFooterRect.pivot = new Vector2(0.5f, 0.5f);
            sFooterRect.anchoredPosition = new Vector2(0f, -595f);
            sFooterRect.sizeDelta = new Vector2(940f, 35f);

            var sFooterImg = shipsFooter.GetComponent<Image>();
            sFooterImg.sprite = footerSprite;
            sFooterImg.color = Color.white;
            sFooterImg.raycastTarget = false;

            // =========================================================================
            // CONFIGURAR COMPONENTES Y SERIALIZED OBJECTS
            // =========================================================================
            // ShopModal
            var modal = panelObj.GetComponent<ShopModal>();
            if (modal == null) modal = panelObj.AddComponent<ShopModal>();

            var btnNoAdsComp = btnNoAdsObj.GetComponent<Button>();
            var btnShipsComp = btnShipsObj.GetComponent<Button>();
            var btnMenuComp = btnMenuObj.GetComponent<Button>();

            var so = new SerializedObject(modal);
            so.FindProperty("mainCategoryView").objectReferenceValue = mainCategoryView;
            so.FindProperty("shipsView").objectReferenceValue = shipsView;
            so.FindProperty("noAdsButton").objectReferenceValue = btnNoAdsComp;
            so.FindProperty("shipsButton").objectReferenceValue = btnShipsComp;
            so.FindProperty("closeButton").objectReferenceValue = btnMenuComp;
            so.ApplyModifiedProperties();

            // ShipsModalView
            var shipsModalView = shipsView.GetComponent<ShipsModalView>();
            if (shipsModalView == null) shipsModalView = shipsView.AddComponent<ShipsModalView>();

            var smSo = new SerializedObject(shipsModalView);
            smSo.FindProperty("rocksText").objectReferenceValue = rocksTMP;
            smSo.FindProperty("shipPreviewImage").objectReferenceValue = spImg;
            smSo.FindProperty("shipNameText").objectReferenceValue = snTMP;
            smSo.FindProperty("shipDescriptionText").objectReferenceValue = sdTMP;
            smSo.FindProperty("pageIndicatorText").objectReferenceValue = piTMP;
            smSo.FindProperty("prevButton").objectReferenceValue = btnPrevObj.GetComponent<Button>();
            smSo.FindProperty("nextButton").objectReferenceValue = btnNextObj.GetComponent<Button>();
            smSo.FindProperty("equipButton").objectReferenceValue = btnEquipComp;
            smSo.FindProperty("equipButtonText").objectReferenceValue = equipLabelTMP;
            smSo.FindProperty("equipButtonBg").objectReferenceValue = btnEquipImg;
            smSo.FindProperty("buyPackButton").objectReferenceValue = btnBuyPackComp;
            smSo.FindProperty("buyPackText").objectReferenceValue = buyPackLabelTMP;
            smSo.FindProperty("feedbackText").objectReferenceValue = fbTMP;
            smSo.FindProperty("backButton").objectReferenceValue = btnBackComp;

            var spThumbBtns = smSo.FindProperty("thumbnailButtons");
            spThumbBtns.arraySize = thumbButtons.Count;
            for (int i = 0; i < thumbButtons.Count; i++)
            {
                spThumbBtns.GetArrayElementAtIndex(i).objectReferenceValue = thumbButtons[i];
            }

            var spThumbImgs = smSo.FindProperty("thumbnailImages");
            spThumbImgs.arraySize = thumbImages.Count;
            for (int i = 0; i < thumbImages.Count; i++)
            {
                spThumbImgs.GetArrayElementAtIndex(i).objectReferenceValue = thumbImages[i];
            }

            smSo.FindProperty("btnActiveSprite").objectReferenceValue = btnEmptySprite;
            smSo.FindProperty("btnInactiveSprite").objectReferenceValue = btnEmptySprite;
            smSo.ApplyModifiedProperties();

            // Persistent listeners
            WirePersistentEvent(btnNoAdsComp, modal.BuyNoAds);
            WirePersistentEvent(btnShipsComp, modal.OpenShips);
            WirePersistentEvent(btnMenuComp, modal.Close);
            WirePersistentEvent(btnBackComp, modal.CloseShipsView);

            // Conectar a MainMenuController
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

            // Subvista inicial: activa categorías principales, oculta naves
            mainCategoryView.SetActive(true);
            shipsView.SetActive(false);
            panelObj.SetActive(false);

            EditorUtility.SetDirty(panelObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#55FF55><b>[ShopPanelTool]</b></color> ShopPanel reconstruido exitosamente con MainCategoryView y ShipsView.");
        }

        public static void ApplyShipSkinAppliersToScenes()
        {
            // 1. MAINMENU
            var mainMenuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var redShipObj = GameObject.Find("MenuShip_Red");
            if (redShipObj != null)
            {
                if (redShipObj.GetComponent<ShipSkinApplier>() == null)
                    redShipObj.AddComponent<ShipSkinApplier>();
                EditorUtility.SetDirty(redShipObj);
            }

            var blueShipObj = GameObject.Find("MenuShip_Blue");
            if (blueShipObj != null)
            {
                if (blueShipObj.GetComponent<ShipSkinApplier>() == null)
                    blueShipObj.AddComponent<ShipSkinApplier>();
                EditorUtility.SetDirty(blueShipObj);
            }

            // Asegurar que CurrencyManager y ShipCustomizationManager estén disponibles en escena
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                if (canvas.GetComponent<CurrencyManager>() == null)
                    canvas.AddComponent<CurrencyManager>();
                if (canvas.GetComponent<ShipCustomizationManager>() == null)
                    canvas.AddComponent<ShipCustomizationManager>();
                EditorUtility.SetDirty(canvas);
            }

            EditorSceneManager.MarkSceneDirty(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene);
            Debug.Log("<color=#55FF55><b>[ShopPanelTool]</b></color> ShipSkinApplier conectado a MenuShip_Red y MenuShip_Blue en MainMenu.");

            // 2. MAINGAME
            var mainGameScene = EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
            var shipLeftObj = GameObject.Find("Ship_Left");
            if (shipLeftObj != null)
            {
                if (shipLeftObj.GetComponent<ShipSkinApplier>() == null)
                    shipLeftObj.AddComponent<ShipSkinApplier>();
                EditorUtility.SetDirty(shipLeftObj);
            }

            var shipRightObj = GameObject.Find("Ship_Right");
            if (shipRightObj != null)
            {
                if (shipRightObj.GetComponent<ShipSkinApplier>() == null)
                    shipRightObj.AddComponent<ShipSkinApplier>();
                EditorUtility.SetDirty(shipRightObj);
            }

            EditorSceneManager.MarkSceneDirty(mainGameScene);
            EditorSceneManager.SaveScene(mainGameScene);
            Debug.Log("<color=#55FF55><b>[ShopPanelTool]</b></color> ShipSkinApplier conectado a Ship_Left y Ship_Right en MainGame.");

            // Volver a MainMenu como activa
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        private static GameObject CreateShopRowButton(string name, Transform parent, float yPos, Sprite btnSprite, string text, TMP_FontAsset font, float width = 450f, float height = 150f, float fontSize = 40f)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, yPos);
            rect.sizeDelta = new Vector2(width, height);

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
            labelTMP.fontSize = fontSize;
            labelTMP.characterSpacing = 3f;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = Color.white;
            labelTMP.raycastTarget = false;

            return btnObj;
        }

        private static GameObject CreateIconButton(string name, Transform parent, Vector2 pos, float btnSize, Sprite btnSprite, Sprite iconSprite, float iconSize)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(btnSize, btnSize);

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
            iconImg.raycastTarget = false;

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
