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
    public static class GameOverPanelAutoSetup
    {
        private const string PREF_KEY = "GameOverPanelRedesignApplied_v1";

        static GameOverPanelAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    SetupGameOverPanel();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/GameOver/Aplicar Rediseño a GameOverPanel")]
        public static void SetupGameOverPanel()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "MainGame")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[GameOverPanelTool] No se encontró Canvas en MainGame.");
                return;
            }

            Transform gameOverPanelTrans = canvas.transform.Find("Panels/GameOverPanel");
            if (gameOverPanelTrans == null)
            {
                Debug.LogError("[GameOverPanelTool] No se encontró GameOverPanel en Canvas/Panels.");
                return;
            }

            var gm = Object.FindFirstObjectByType<GameManager>();

            // 1. Configurar raíz de GameOverPanel
            var rootRect = gameOverPanelTrans.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImg = gameOverPanelTrans.GetComponent<Image>();
            if (rootImg == null) rootImg = gameOverPanelTrans.gameObject.AddComponent<Image>();
            rootImg.color = new Color(0f, 0f, 0f, 0.98f);
            rootImg.raycastTarget = true;

            // 2. Limpiar hijos previos para reconstrucción limpia y garantizada
            while (gameOverPanelTrans.childCount > 0)
            {
                Object.DestroyImmediate(gameOverPanelTrans.GetChild(0).gameObject);
            }

            // 3. Cargar Sprites y Fuente
            var headerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/HEADER.png");
            var bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/BODY_BIG.png");
            var footerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/FOOTER.png");
            var btnEmptySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_EMPTY.png");
            var btnIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_ICON.png");

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Ethnocentric-Regular SDF.asset");

            Sprite adsSprite = GetSubSprite("Assets/Art/Sprites/icons/ADS_ACTIVE.png", "ADS_ACTIVE_0");
            Sprite menuSprite = GetSubSprite("Assets/Art/Sprites/icons/MENU.png", "MENU_0");
            Sprite playSprite = GetSubSprite("Assets/Art/Sprites/icons/PLAY.png", "PLAY_0");
            Sprite rankingSprite = GetSubSprite("Assets/Art/Sprites/icons/RANKING.png", "RANKING_0");

            // 4. Crear Container (1080 x 1920)
            GameObject container = new GameObject("Container", typeof(RectTransform));
            container.transform.SetParent(gameOverPanelTrans, false);
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(1080f, 1920f);

            // 5. Header (920 x 120, Y: 380)
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

            // Header -> Title (YOU LOSE)
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "YOU LOSE";
            if (fontAsset != null) titleTMP.font = fontAsset;
            titleTMP.fontSize = 50f;
            titleTMP.characterSpacing = 4f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;

            // Header -> AdsIcon
            GameObject adsObj = new GameObject("AdsIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            adsObj.transform.SetParent(header.transform, false);
            var adsRect = adsObj.GetComponent<RectTransform>();
            adsRect.anchorMin = new Vector2(1f, 0.5f);
            adsRect.anchorMax = new Vector2(1f, 0.5f);
            adsRect.pivot = new Vector2(1f, 0.5f);
            adsRect.anchoredPosition = new Vector2(-45f, 0f);
            adsRect.sizeDelta = new Vector2(65f, 65f);

            var adsImg = adsObj.GetComponent<Image>();
            adsImg.sprite = adsSprite;
            adsImg.preserveAspect = true;
            adsImg.color = Color.white;

            // 6. Content (920 x 650, Y: 0)
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            content.transform.SetParent(container.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(1f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            contentRect.sizeDelta = new Vector2(-160f, 650f);

            var contentImg = content.GetComponent<Image>();
            contentImg.sprite = bodySprite;
            contentImg.type = Image.Type.Sliced;
            contentImg.color = Color.white;

            // 6a. Fila 1: SCORE (Y: 135)
            GameObject scoreRow = new GameObject("ScoreRow", typeof(RectTransform));
            scoreRow.transform.SetParent(content.transform, false);
            var scoreRowRect = scoreRow.GetComponent<RectTransform>();
            scoreRowRect.anchorMin = new Vector2(0.5f, 0.5f);
            scoreRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            scoreRowRect.pivot = new Vector2(0.5f, 0.5f);
            scoreRowRect.anchoredPosition = new Vector2(0f, 135f);
            scoreRowRect.sizeDelta = new Vector2(740f, 110f);

            GameObject scoreLabelObj = new GameObject("ScoreLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            scoreLabelObj.transform.SetParent(scoreRow.transform, false);
            var scoreLabelRect = scoreLabelObj.GetComponent<RectTransform>();
            scoreLabelRect.anchorMin = new Vector2(0f, 0.5f);
            scoreLabelRect.anchorMax = new Vector2(0f, 0.5f);
            scoreLabelRect.pivot = new Vector2(0f, 0.5f);
            scoreLabelRect.anchoredPosition = new Vector2(25f, 0f);
            scoreLabelRect.sizeDelta = new Vector2(300f, 50f);

            var scoreLabelTMP = scoreLabelObj.GetComponent<TextMeshProUGUI>();
            scoreLabelTMP.text = "SCORE";
            if (fontAsset != null) scoreLabelTMP.font = fontAsset;
            scoreLabelTMP.fontSize = 38f;
            scoreLabelTMP.characterSpacing = 4f;
            scoreLabelTMP.alignment = TextAlignmentOptions.Left;
            scoreLabelTMP.color = Color.white;

            GameObject scoreBadgeObj = new GameObject("ScoreBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            scoreBadgeObj.transform.SetParent(scoreRow.transform, false);
            var scoreBadgeRect = scoreBadgeObj.GetComponent<RectTransform>();
            scoreBadgeRect.anchorMin = new Vector2(1f, 0.5f);
            scoreBadgeRect.anchorMax = new Vector2(1f, 0.5f);
            scoreBadgeRect.pivot = new Vector2(1f, 0.5f);
            scoreBadgeRect.anchoredPosition = new Vector2(-15f, 0f);
            scoreBadgeRect.sizeDelta = new Vector2(370f, 105f);

            var scoreBadgeImg = scoreBadgeObj.GetComponent<Image>();
            scoreBadgeImg.sprite = btnEmptySprite;
            scoreBadgeImg.type = Image.Type.Sliced;
            scoreBadgeImg.color = Color.white;

            GameObject scoreValueObj = new GameObject("ScoreValue", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            scoreValueObj.transform.SetParent(scoreBadgeObj.transform, false);
            var scoreValueRect = scoreValueObj.GetComponent<RectTransform>();
            scoreValueRect.anchorMin = Vector2.zero;
            scoreValueRect.anchorMax = Vector2.one;
            scoreValueRect.pivot = new Vector2(0.5f, 0.5f);
            scoreValueRect.offsetMin = new Vector2(20f, 0f);
            scoreValueRect.offsetMax = new Vector2(-20f, 0f);

            var scoreValueTMP = scoreValueObj.GetComponent<TextMeshProUGUI>();
            scoreValueTMP.text = "0";
            if (fontAsset != null) scoreValueTMP.font = fontAsset;
            scoreValueTMP.fontSize = 48f;
            scoreValueTMP.enableAutoSizing = true;
            scoreValueTMP.fontSizeMin = 28f;
            scoreValueTMP.fontSizeMax = 48f;
            scoreValueTMP.alignment = TextAlignmentOptions.Center;
            scoreValueTMP.color = Color.white;
            scoreValueTMP.characterSpacing = 3f;

            // 6b. Fila 2: RECORD (Y: 10)
            GameObject recordRow = new GameObject("RecordRow", typeof(RectTransform));
            recordRow.transform.SetParent(content.transform, false);
            var recordRowRect = recordRow.GetComponent<RectTransform>();
            recordRowRect.anchorMin = new Vector2(0.5f, 0.5f);
            recordRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            recordRowRect.pivot = new Vector2(0.5f, 0.5f);
            recordRowRect.anchoredPosition = new Vector2(0f, 10f);
            recordRowRect.sizeDelta = new Vector2(740f, 110f);

            GameObject recordLabelObj = new GameObject("RecordLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            recordLabelObj.transform.SetParent(recordRow.transform, false);
            var recordLabelRect = recordLabelObj.GetComponent<RectTransform>();
            recordLabelRect.anchorMin = new Vector2(0f, 0.5f);
            recordLabelRect.anchorMax = new Vector2(0f, 0.5f);
            recordLabelRect.pivot = new Vector2(0f, 0.5f);
            recordLabelRect.anchoredPosition = new Vector2(25f, 0f);
            recordLabelRect.sizeDelta = new Vector2(300f, 50f);

            var recordLabelTMP = recordLabelObj.GetComponent<TextMeshProUGUI>();
            recordLabelTMP.text = "RECORD";
            if (fontAsset != null) recordLabelTMP.font = fontAsset;
            recordLabelTMP.fontSize = 38f;
            recordLabelTMP.characterSpacing = 4f;
            recordLabelTMP.alignment = TextAlignmentOptions.Left;
            recordLabelTMP.color = Color.white;

            GameObject recordBadgeObj = new GameObject("RecordBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            recordBadgeObj.transform.SetParent(recordRow.transform, false);
            var recordBadgeRect = recordBadgeObj.GetComponent<RectTransform>();
            recordBadgeRect.anchorMin = new Vector2(1f, 0.5f);
            recordBadgeRect.anchorMax = new Vector2(1f, 0.5f);
            recordBadgeRect.pivot = new Vector2(1f, 0.5f);
            recordBadgeRect.anchoredPosition = new Vector2(-15f, 0f);
            recordBadgeRect.sizeDelta = new Vector2(370f, 105f);

            var recordBadgeImg = recordBadgeObj.GetComponent<Image>();
            recordBadgeImg.sprite = btnEmptySprite;
            recordBadgeImg.type = Image.Type.Sliced;
            recordBadgeImg.color = Color.white;

            GameObject recordValueObj = new GameObject("RecordValue", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            recordValueObj.transform.SetParent(recordBadgeObj.transform, false);
            var recordValueRect = recordValueObj.GetComponent<RectTransform>();
            recordValueRect.anchorMin = Vector2.zero;
            recordValueRect.anchorMax = Vector2.one;
            recordValueRect.pivot = new Vector2(0.5f, 0.5f);
            recordValueRect.offsetMin = new Vector2(20f, 0f);
            recordValueRect.offsetMax = new Vector2(-20f, 0f);

            var recordValueTMP = recordValueObj.GetComponent<TextMeshProUGUI>();
            recordValueTMP.text = "0";
            if (fontAsset != null) recordValueTMP.font = fontAsset;
            recordValueTMP.fontSize = 48f;
            recordValueTMP.enableAutoSizing = true;
            recordValueTMP.fontSizeMin = 28f;
            recordValueTMP.fontSizeMax = 48f;
            recordValueTMP.alignment = TextAlignmentOptions.Center;
            recordValueTMP.color = Color.white;
            recordValueTMP.characterSpacing = 3f;

            // 6c. Fila 3: Botones (Y: -150)
            GameObject buttonsObj = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonsObj.transform.SetParent(content.transform, false);
            var buttonsRect = buttonsObj.GetComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonsRect.pivot = new Vector2(0.5f, 0.5f);
            buttonsRect.anchoredPosition = new Vector2(0f, -150f);
            buttonsRect.sizeDelta = new Vector2(600f, 180f);

            var layout = buttonsObj.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 45f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 1. BtnMenu -> GoToMainMenu
            GameObject btnMenuObj = CreateSquareButton("BtnMenu", buttonsObj.transform, btnIconSprite, menuSprite, 150f, 75f);
            var btnMenu = btnMenuObj.GetComponent<Button>();
            if (gm != null)
            {
                UnityEventTools.AddPersistentListener(btnMenu.onClick, gm.GoToMainMenu);
            }

            // 2. BtnRestart -> RestartGame
            GameObject btnRestartObj = CreateSquareButton("BtnRestart", buttonsObj.transform, btnIconSprite, playSprite, 150f, 75f);
            var btnRestart = btnRestartObj.GetComponent<Button>();
            if (gm != null)
            {
                UnityEventTools.AddPersistentListener(btnRestart.onClick, gm.RestartGame);
            }

            // 3. BtnRanking -> OpenLeaderboard
            GameObject btnRankingObj = CreateSquareButton("BtnRanking", buttonsObj.transform, btnIconSprite, rankingSprite, 150f, 75f);
            var btnRanking = btnRankingObj.GetComponent<Button>();
            if (gm != null)
            {
                UnityEventTools.AddPersistentListener(btnRanking.onClick, gm.OpenLeaderboard);
            }

            // 7. Footer (920 x 40, Y: -345)
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

            // 8. Enlazar referencias en GameHUD
            var hud = Object.FindFirstObjectByType<GameHUD>();
            if (hud != null)
            {
                var so = new SerializedObject(hud);
                var pGameOver = so.FindProperty("gameOverPanel");
                if (pGameOver != null) pGameOver.objectReferenceValue = gameOverPanelTrans.gameObject;

                var pScore = so.FindProperty("gameOverScoreText");
                if (pScore != null) pScore.objectReferenceValue = scoreValueTMP;

                var pRecord = so.FindProperty("gameOverRecordText");
                if (pRecord != null) pRecord.objectReferenceValue = recordValueTMP;

                var pFinal = so.FindProperty("finalScoreText");
                if (pFinal != null) pFinal.objectReferenceValue = scoreValueTMP;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(hud);
            }

            // Dejar GameOverPanel desactivado por defecto para el juego
            gameOverPanelTrans.gameObject.SetActive(false);

            // Guardar cambios en escena
            EditorUtility.SetDirty(gameOverPanelTrans.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#55FF55><b>[GameOverPanelTool]</b></color> GameOverPanel rediseñado, configurado y guardado con éxito.");
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
            iconRect.anchoredPosition = Vector2.zero;
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
