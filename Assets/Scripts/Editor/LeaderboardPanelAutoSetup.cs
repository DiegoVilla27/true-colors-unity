#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

namespace TrueColors.EditorTools
{
    [InitializeOnLoad]
    public static class LeaderboardPanelAutoSetup
    {
        private const string PREF_KEY = "LeaderboardPanelSetupApplied_v3";

        static LeaderboardPanelAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    SetupLeaderboardPanel();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/Leaderboard/Construir y Conectar LeaderboardPanel en MainMenu")]
        public static void SetupLeaderboardPanel()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "MainMenu")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[LeaderboardPanelTool] No se encontró Canvas en MainMenu.");
                return;
            }

            // 1. Reemplazar o crear LeaderboardPanel
            Transform existingPanel = canvas.transform.Find("LeaderboardPanel");
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            GameObject panelObj = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(canvas.transform, false);

            var rootRect = panelObj.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImg = panelObj.GetComponent<Image>();
            rootImg.color = new Color(0f, 0f, 0f, 0.92f);
            rootImg.raycastTarget = true;

            // 2. Cargar Assets visuales
            var headerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/HEADER.png");
            var bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/BODY_BIG.png");
            var footerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/FOOTER.png");
            var highlightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/HIGHLIGHT.png");
            var avatarSprite = LoadBestSprite("Assets/Art/Sprites/container/AVATAR.png");
            var starSprite = LoadBestSprite("Assets/Art/Sprites/stars/STAR_FULL.png");
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

            // 4. Header (940 x 130, Y: 482)
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            header.transform.SetParent(container.transform, false);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 0.5f);
            headerRect.anchorMax = new Vector2(0.5f, 0.5f);
            headerRect.pivot = new Vector2(0.5f, 0.5f);
            headerRect.anchoredPosition = new Vector2(0f, 482f);
            headerRect.sizeDelta = new Vector2(940f, 130f);

            var headerImg = header.GetComponent<Image>();
            headerImg.sprite = headerSprite;
            headerImg.color = Color.white;
            headerImg.raycastTarget = false;

            // Header -> Title (RANKING)
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "RANKING";
            if (fontAsset != null) titleTMP.font = fontAsset;
            titleTMP.fontSize = 48f;
            titleTMP.characterSpacing = 4f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;
            titleTMP.raycastTarget = false;

            // 5. Content / Body (940 x 880, Y: -20)
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            content.transform.SetParent(container.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, -20f);
            contentRect.sizeDelta = new Vector2(940f, 880f);

            var contentImg = content.GetComponent<Image>();
            contentImg.sprite = bodySprite;
            contentImg.color = Color.white;
            contentImg.raycastTarget = false;

            // 6. ScrollView Scroleable (860 x 570, Y: 40)
            GameObject scrollViewObj = new GameObject("ScrollView_Ranking", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollViewObj.transform.SetParent(content.transform, false);
            var scrollRectTrans = scrollViewObj.GetComponent<RectTransform>();
            scrollRectTrans.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTrans.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTrans.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTrans.anchoredPosition = new Vector2(0f, 40f);
            scrollRectTrans.sizeDelta = new Vector2(860f, 570f);

            var scrollBgImg = scrollViewObj.GetComponent<Image>();
            scrollBgImg.color = new Color(0f, 0f, 0f, 0f); // Transparente para ver el hex-grid
            scrollBgImg.raycastTarget = true; // Permite arrastrar el scroll desde zonas vacías

            var scrollRect = scrollViewObj.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 30f;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            scrollRect.viewport = viewportRect;

            // ScrollContent (Contenedor de Filas con VerticalLayoutGroup y ContentSizeFitter)
            GameObject scrollContentObj = new GameObject("ScrollContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            scrollContentObj.transform.SetParent(viewportObj.transform, false);
            var scrollContentRect = scrollContentObj.GetComponent<RectTransform>();
            scrollContentRect.anchorMin = new Vector2(0f, 1f);
            scrollContentRect.anchorMax = new Vector2(1f, 1f);
            scrollContentRect.pivot = new Vector2(0.5f, 1f);
            scrollContentRect.anchoredPosition = Vector2.zero;
            scrollContentRect.sizeDelta = Vector2.zero;

            var vLayout = scrollContentObj.GetComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(0, 0, 8, 12);
            vLayout.spacing = 15f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;

            var fitter = scrollContentObj.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = scrollContentRect;

            // Color HIGHLIGHT: #00739A con Alpha 70 (70 / 255f)
            Color highlightColor = new Color(0f, 115f / 255f, 154f / 255f, 70f / 255f);

            // 7. Crear Plantilla de Fila (RowTemplate)
            GameObject rowTemplateObj = CreateRowGameObject("Row_Template", scrollContentObj.transform, highlightSprite, highlightColor, avatarSprite, starSprite, "DIEGOVILLA92", "3200", fontAsset);
            rowTemplateObj.SetActive(false); // Mantener como plantilla inactiva para instanciación

            // Crear 10 Filas Hardcodeadas Iniciales en el Editor para previsualización inmediata y scroll suave
            var mockEntries = new List<LeaderboardModal.LeaderboardEntry>
            {
                new LeaderboardModal.LeaderboardEntry("DIEGOVILLA92", 3200),
                new LeaderboardModal.LeaderboardEntry("CYBER_PILOT", 2950),
                new LeaderboardModal.LeaderboardEntry("NOVA_STRIKER", 2700),
                new LeaderboardModal.LeaderboardEntry("COSMIC_ACE", 2520),
                new LeaderboardModal.LeaderboardEntry("STELLAR_FOX", 2310),
                new LeaderboardModal.LeaderboardEntry("NEON_VORTEX", 2100),
                new LeaderboardModal.LeaderboardEntry("QUANTUM_GHOST", 1980),
                new LeaderboardModal.LeaderboardEntry("SHADOW_RUNNER", 1850),
                new LeaderboardModal.LeaderboardEntry("ASTRO_KNIGHT", 1620),
                new LeaderboardModal.LeaderboardEntry("HYPER_DRIVE", 1400)
            };

            for (int i = 0; i < mockEntries.Count; i++)
            {
                string rowName = $"Row_{i + 1:00}";
                CreateRowGameObject(rowName, scrollContentObj.transform, highlightSprite, highlightColor, avatarSprite, starSprite, mockEntries[i].username, mockEntries[i].score.ToString(), fontAsset);
            }

            // 8. Botón Inferior Central (MENU.png sobre BTN_ICON.png) (Y: -330)
            GameObject btnMenuObj = new GameObject("BtnMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
            btnMenuObj.transform.SetParent(content.transform, false);
            var btnMenuRect = btnMenuObj.GetComponent<RectTransform>();
            btnMenuRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnMenuRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnMenuRect.pivot = new Vector2(0.5f, 0.5f);
            btnMenuRect.anchoredPosition = new Vector2(0f, -330f);
            btnMenuRect.sizeDelta = new Vector2(140f, 140f);

            var btnMenuImg = btnMenuObj.GetComponent<Image>();
            btnMenuImg.sprite = btnIconSprite;
            btnMenuImg.type = Image.Type.Simple;
            btnMenuImg.color = Color.white;
            btnMenuImg.raycastTarget = true;

            var btnMenuComp = btnMenuObj.GetComponent<Button>();
            btnMenuComp.targetGraphic = btnMenuImg;
            btnMenuComp.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = btnMenuComp.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            btnMenuComp.colors = colors;

            // Icono MENU dentro del botón
            GameObject menuIconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            menuIconObj.transform.SetParent(btnMenuObj.transform, false);
            var menuIconRect = menuIconObj.GetComponent<RectTransform>();
            menuIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuIconRect.pivot = new Vector2(0.5f, 0.5f);
            menuIconRect.anchoredPosition = Vector2.zero;
            menuIconRect.sizeDelta = new Vector2(75f, 75f);

            var menuIconImg = menuIconObj.GetComponent<Image>();
            menuIconImg.sprite = menuSprite;
            menuIconImg.preserveAspect = true;
            menuIconImg.color = Color.white;
            menuIconImg.raycastTarget = false;

            // 9. Footer (940 x 35, Y: -475)
            GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            footer.transform.SetParent(container.transform, false);
            var footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.5f, 0.5f);
            footerRect.anchorMax = new Vector2(0.5f, 0.5f);
            footerRect.pivot = new Vector2(0.5f, 0.5f);
            footerRect.anchoredPosition = new Vector2(0f, -475f);
            footerRect.sizeDelta = new Vector2(940f, 35f);

            var footerImg = footer.GetComponent<Image>();
            footerImg.sprite = footerSprite;
            footerImg.color = Color.white;
            footerImg.raycastTarget = false;

            // 10. Configurar LeaderboardModal
            var modal = panelObj.GetComponent<LeaderboardModal>();
            if (modal == null) modal = panelObj.AddComponent<LeaderboardModal>();

            var so = new SerializedObject(modal);
            so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("contentContainer").objectReferenceValue = scrollContentRect;
            so.FindProperty("rowTemplate").objectReferenceValue = rowTemplateObj;
            so.FindProperty("closeButton").objectReferenceValue = btnMenuComp;
            so.ApplyModifiedProperties();

            // Enlazar evento OnClick al botón de cerrar
            while (btnMenuComp.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(btnMenuComp.onClick, 0);
            UnityEventTools.AddPersistentListener(btnMenuComp.onClick, modal.Close);

            // 11. Conectar a MainMenuController
            var menuController = Object.FindAnyObjectByType<MainMenuController>();
            if (menuController != null)
            {
                var menuSo = new SerializedObject(menuController);
                var pLeaderboard = menuSo.FindProperty("leaderboardPanel");
                if (pLeaderboard != null)
                {
                    pLeaderboard.objectReferenceValue = panelObj;
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

            Debug.Log("<color=#55FF55><b>[LeaderboardPanelTool]</b></color> LeaderboardPanel scroleable con lista hardcodeada y botón BTN_ICON construido exitosamente.");
        }

        private static GameObject CreateRowGameObject(string name, Transform parent, Sprite highlightSprite, Color highlightColor, Sprite avatarSprite, Sprite starSprite, string username, string score, TMP_FontAsset font)
        {
            // Contenedor de la Fila (820 x 112)
            GameObject rowObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(LeaderboardRowItem));
            rowObj.transform.SetParent(parent, false);

            var rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(820f, 112f);

            var layoutElement = rowObj.GetComponent<LayoutElement>();
            layoutElement.minWidth = 820f;
            layoutElement.preferredWidth = 820f;
            layoutElement.minHeight = 112f;
            layoutElement.preferredHeight = 112f;

            var rowImg = rowObj.GetComponent<Image>();
            rowImg.sprite = highlightSprite;
            rowImg.color = highlightColor;
            rowImg.raycastTarget = true; // Permite arrastrar el scroll desde la propia fila

            // 1. AVATAR (Izquierda)
            GameObject avatarObj = new GameObject("Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            avatarObj.transform.SetParent(rowObj.transform, false);

            var avatarRect = avatarObj.GetComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0f, 0.5f);
            avatarRect.anchorMax = new Vector2(0f, 0.5f);
            avatarRect.pivot = new Vector2(0f, 0.5f);
            avatarRect.anchoredPosition = new Vector2(10f, 0f);
            avatarRect.sizeDelta = new Vector2(96f, 96f);

            var avatarImg = avatarObj.GetComponent<Image>();
            avatarImg.sprite = avatarSprite;
            avatarImg.type = Image.Type.Simple;
            avatarImg.preserveAspect = true;
            avatarImg.color = Color.white;
            avatarImg.raycastTarget = false;

            // 2. TEXTOS (Username arriba, Puntos abajo)
            // Username
            GameObject userObj = new GameObject("Username", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            userObj.transform.SetParent(rowObj.transform, false);

            var userRect = userObj.GetComponent<RectTransform>();
            userRect.anchorMin = new Vector2(0f, 0.5f);
            userRect.anchorMax = new Vector2(0f, 0.5f);
            userRect.pivot = new Vector2(0f, 0.5f);
            userRect.anchoredPosition = new Vector2(126f, 18f);
            userRect.sizeDelta = new Vector2(460f, 38f);

            var userTMP = userObj.GetComponent<TextMeshProUGUI>();
            userTMP.text = username;
            if (font != null) userTMP.font = font;
            userTMP.fontSize = 28f;
            userTMP.characterSpacing = 2f;
            userTMP.alignment = TextAlignmentOptions.MidlineLeft;
            userTMP.color = Color.white;
            userTMP.raycastTarget = false;

            // Score
            GameObject scoreObj = new GameObject("Score", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            scoreObj.transform.SetParent(rowObj.transform, false);

            var scoreRect = scoreObj.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 0.5f);
            scoreRect.anchorMax = new Vector2(0f, 0.5f);
            scoreRect.pivot = new Vector2(0f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(126f, -20f);
            scoreRect.sizeDelta = new Vector2(460f, 38f);

            var scoreTMP = scoreObj.GetComponent<TextMeshProUGUI>();
            scoreTMP.text = score;
            if (font != null) scoreTMP.font = font;
            scoreTMP.fontSize = 30f;
            scoreTMP.characterSpacing = 2f;
            scoreTMP.alignment = TextAlignmentOptions.MidlineLeft;
            scoreTMP.color = Color.white;
            scoreTMP.raycastTarget = false;

            // 3. STAR_FULL (Derecha)
            GameObject starObj = new GameObject("Star", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            starObj.transform.SetParent(rowObj.transform, false);

            var starRect = starObj.GetComponent<RectTransform>();
            starRect.anchorMin = new Vector2(1f, 0.5f);
            starRect.anchorMax = new Vector2(1f, 0.5f);
            starRect.pivot = new Vector2(1f, 0.5f);
            starRect.anchoredPosition = new Vector2(-25f, 0f);
            starRect.sizeDelta = new Vector2(75f, 75f);

            var starImg = starObj.GetComponent<Image>();
            starImg.sprite = starSprite;
            starImg.type = Image.Type.Simple;
            starImg.preserveAspect = true;
            starImg.color = Color.white;
            starImg.raycastTarget = false;

            var item = rowObj.GetComponent<LeaderboardRowItem>();
            item.ConfigureReferences(rowImg, avatarImg, userTMP, scoreTMP, starImg);

            return rowObj;
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
