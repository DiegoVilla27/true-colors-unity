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
    public static class SettingsPanelAutoSetup
    {
        private const string PREF_KEY = "SettingsPanelSetupApplied_v11";

        static SettingsPanelAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    SetupSettingsPanel();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/Settings/Construir y Conectar SettingsPanel en MainMenu")]
        public static void SetupSettingsPanel()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "MainMenu")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[SettingsPanelTool] No se encontró Canvas en MainMenu.");
                return;
            }

            // 1. Destruir OptionsPanel previo si existía para garantizar estructura 100% limpia sin Canvas fantasma
            Transform optionsPanelTrans = canvas.transform.Find("OptionsPanel");
            if (optionsPanelTrans != null)
            {
                Object.DestroyImmediate(optionsPanelTrans.gameObject);
            }

            GameObject optionsPanelObj = new GameObject("OptionsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            optionsPanelObj.transform.SetParent(canvas.transform, false);
            optionsPanelTrans = optionsPanelObj.transform;

            // Configurar raíz de OptionsPanel (Fondo oscuro semitransparente full screen)
            var rootRect = optionsPanelObj.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImg = optionsPanelObj.GetComponent<Image>();
            if (rootImg == null) rootImg = optionsPanelObj.AddComponent<Image>();
            rootImg.color = new Color(0f, 0f, 0f, 0.92f);
            rootImg.raycastTarget = true;

            // Limpiar hijos existentes para garantizar reconstrucción limpia
            while (optionsPanelTrans.childCount > 0)
            {
                Object.DestroyImmediate(optionsPanelTrans.GetChild(0).gameObject);
            }

            // 2. Cargar Sprites de Contenedor, Botones e Iconos
            var headerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/HEADER.png");
            var bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/BODY_BIG.png");
            var footerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/FOOTER.png");
            var highlightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/container/HIGHLIGHT.png");

            // Sprites de Botón: Activo (BTN_ACTIVE) vs Inactivo (BTN_ICON)
            var btnActiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_ACTIVE.png");
            var btnInactiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/btns/BTN_ICON.png");

            // Sprites de Iconos: Activos (_ACTIVE) vs Inactivos
            Sprite soundActiveIcon = LoadBestSprite("Assets/Art/Sprites/icons/SOUND_ACTIVE.png");
            Sprite soundInactiveIcon = LoadBestSprite("Assets/Art/Sprites/icons/SOUND.png");

            Sprite musicActiveIcon = LoadBestSprite("Assets/Art/Sprites/icons/MUSIC_ACTIVE.png");
            Sprite musicInactiveIcon = LoadBestSprite("Assets/Art/Sprites/icons/MUSIC.png");

            Sprite vibrationActiveIcon = LoadBestSprite("Assets/Art/Sprites/icons/VIBRATION_ACTIVE.png");
            Sprite vibrationInactiveIcon = LoadBestSprite("Assets/Art/Sprites/icons/VIBRATION.png");

            Sprite notificationsActiveIcon = LoadBestSprite("Assets/Art/Sprites/icons/COMMENT_ACTIVE.png");
            Sprite notificationsInactiveIcon = LoadBestSprite("Assets/Art/Sprites/icons/COMMENT.png");

            Sprite cancelSprite = LoadBestSprite("Assets/Art/Sprites/icons/CANCEL.png");
            Sprite confirmSprite = LoadBestSprite("Assets/Art/Sprites/icons/CONFIRM.png");

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Ethnocentric-Regular SDF.asset");

            // 3. Crear Container (1080 x 1920)
            GameObject container = new GameObject("Container", typeof(RectTransform));
            container.transform.SetParent(optionsPanelTrans, false);
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

            // Header -> Title (SETTINGS)
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "SETTINGS";
            if (fontAsset != null) titleTMP.font = fontAsset;
            titleTMP.fontSize = 48f;
            titleTMP.characterSpacing = 4f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;
            titleTMP.raycastTarget = false;

            // 5. Content / Body (940 x 1000, Y: -80)
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            content.transform.SetParent(container.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, -80f);
            contentRect.sizeDelta = new Vector2(940f, 1000f);

            var contentImg = content.GetComponent<Image>();
            contentImg.sprite = bodySprite;
            contentImg.color = Color.white;
            contentImg.raycastTarget = false;

            // Color exacto especificado para HIGHLIGHT: #00739A con Alpha = 70 (70 / 255f)
            Color highlightColor = new Color(0f, 115f / 255f, 154f / 255f, 70f / 255f);

            // 6. Crear las 4 filas interactivas con espacio amplio y uniforme (distancia 180px, gap de 30px entre botones)
            var rowSound = CreateSettingRow("Row_Sound", content.transform, 380f, highlightSprite, highlightColor, btnActiveSprite, soundActiveIcon, "SOUND", fontAsset);
            var rowMusic = CreateSettingRow("Row_Music", content.transform, 200f, highlightSprite, highlightColor, btnActiveSprite, musicActiveIcon, "MUSIC", fontAsset);
            var rowVibration = CreateSettingRow("Row_Vibration", content.transform, 20f, highlightSprite, highlightColor, btnActiveSprite, vibrationActiveIcon, "VIBRATION", fontAsset);
            var rowNotifications = CreateSettingRow("Row_Notifications", content.transform, -160f, highlightSprite, highlightColor, btnActiveSprite, notificationsActiveIcon, "NOTIFICATIONS", fontAsset);

            // 7. Botones de Acción: CANCEL y CONFIRM (Y: -370)
            GameObject buttonsObj = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonsObj.transform.SetParent(content.transform, false);
            var buttonsRect = buttonsObj.GetComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonsRect.pivot = new Vector2(0.5f, 0.5f);
            buttonsRect.anchoredPosition = new Vector2(0f, -370f);
            buttonsRect.sizeDelta = new Vector2(380f, 150f);

            var hLayout = buttonsObj.GetComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 35f;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            GameObject btnCancelObj = CreateActionButton("BtnCancel", buttonsObj.transform, btnInactiveSprite, cancelSprite, 150f, 80f);
            GameObject btnConfirmObj = CreateActionButton("BtnConfirm", buttonsObj.transform, btnInactiveSprite, confirmSprite, 150f, 80f);

            // 8. Footer (940 x 35, Y: -595)
            GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            footer.transform.SetParent(container.transform, false);
            var footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.5f, 0.5f);
            footerRect.anchorMax = new Vector2(0.5f, 0.5f);
            footerRect.pivot = new Vector2(0.5f, 0.5f);
            footerRect.anchoredPosition = new Vector2(0f, -595f);
            footerRect.sizeDelta = new Vector2(940f, 35f);

            var footerImg = footer.GetComponent<Image>();
            footerImg.sprite = footerSprite;
            footerImg.color = Color.white;
            footerImg.raycastTarget = false;

            // 9. Configurar y Enlazar SettingsModal
            var modal = optionsPanelObj.GetComponent<SettingsModal>();
            if (modal == null) modal = optionsPanelObj.AddComponent<SettingsModal>();

            var so = new SerializedObject(modal);

            // Botones de Toggle
            so.FindProperty("soundButton").objectReferenceValue = rowSound.iconButton;
            so.FindProperty("musicButton").objectReferenceValue = rowMusic.iconButton;
            so.FindProperty("vibrationButton").objectReferenceValue = rowVibration.iconButton;
            so.FindProperty("notificationsButton").objectReferenceValue = rowNotifications.iconButton;

            // Fondos de Botón (Image)
            so.FindProperty("soundButtonBg").objectReferenceValue = rowSound.btnBgImage;
            so.FindProperty("musicButtonBg").objectReferenceValue = rowMusic.btnBgImage;
            so.FindProperty("vibrationButtonBg").objectReferenceValue = rowVibration.btnBgImage;
            so.FindProperty("notificationsButtonBg").objectReferenceValue = rowNotifications.btnBgImage;

            // Sprites de Botón (Toggle State)
            so.FindProperty("btnActiveSprite").objectReferenceValue = btnActiveSprite;
            so.FindProperty("btnInactiveSprite").objectReferenceValue = btnInactiveSprite;

            // Iconos de los Botones (Image)
            so.FindProperty("soundIcon").objectReferenceValue = rowSound.iconImage;
            so.FindProperty("musicIcon").objectReferenceValue = rowMusic.iconImage;
            so.FindProperty("vibrationIcon").objectReferenceValue = rowVibration.iconImage;
            so.FindProperty("notificationsIcon").objectReferenceValue = rowNotifications.iconImage;

            // Sprites de Iconos (Activos vs Inactivos)
            so.FindProperty("soundActiveIcon").objectReferenceValue = soundActiveIcon;
            so.FindProperty("soundInactiveIcon").objectReferenceValue = soundInactiveIcon;
            so.FindProperty("musicActiveIcon").objectReferenceValue = musicActiveIcon;
            so.FindProperty("musicInactiveIcon").objectReferenceValue = musicInactiveIcon;
            so.FindProperty("vibrationActiveIcon").objectReferenceValue = vibrationActiveIcon;
            so.FindProperty("vibrationInactiveIcon").objectReferenceValue = vibrationInactiveIcon;
            so.FindProperty("notificationsActiveIcon").objectReferenceValue = notificationsActiveIcon;
            so.FindProperty("notificationsInactiveIcon").objectReferenceValue = notificationsInactiveIcon;

            // Fondos HIGHLIGHT
            so.FindProperty("soundHighlight").objectReferenceValue = rowSound.highlightImage;
            so.FindProperty("musicHighlight").objectReferenceValue = rowMusic.highlightImage;
            so.FindProperty("vibrationHighlight").objectReferenceValue = rowVibration.highlightImage;
            so.FindProperty("notificationsHighlight").objectReferenceValue = rowNotifications.highlightImage;

            // Botones de acción
            so.FindProperty("cancelButton").objectReferenceValue = btnCancelObj.GetComponent<Button>();
            so.FindProperty("confirmButton").objectReferenceValue = btnConfirmObj.GetComponent<Button>();

            // Booleans iniciales cargados desde SettingsDatabase (DB local)
            var initialDbData = SettingsDatabase.Load();
            so.FindProperty("isSoundActive").boolValue = initialDbData.isSoundActive;
            so.FindProperty("isMusicActive").boolValue = initialDbData.isMusicActive;
            so.FindProperty("isVibrationActive").boolValue = initialDbData.isVibrationActive;
            so.FindProperty("isNotificationsActive").boolValue = initialDbData.isNotificationsActive;

            so.ApplyModifiedProperties();

            // Wire persistent listeners
            var cancelBtn = btnCancelObj.GetComponent<Button>();
            while (cancelBtn.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(cancelBtn.onClick, 0);
            UnityEventTools.AddPersistentListener(cancelBtn.onClick, modal.Cancel);

            var confirmBtn = btnConfirmObj.GetComponent<Button>();
            while (confirmBtn.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(confirmBtn.onClick, 0);
            UnityEventTools.AddPersistentListener(confirmBtn.onClick, modal.Confirm);

            WireButtonEvent(rowSound.iconButton, modal.ToggleSound);
            WireButtonEvent(rowMusic.iconButton, modal.ToggleMusic);
            WireButtonEvent(rowVibration.iconButton, modal.ToggleVibration);
            WireButtonEvent(rowNotifications.iconButton, modal.ToggleNotifications);

            // 10. Conectar a MainMenuController
            var menuController = Object.FindAnyObjectByType<MainMenuController>();
            if (menuController != null)
            {
                var menuSo = new SerializedObject(menuController);
                var pOptions = menuSo.FindProperty("optionsPanel");
                if (pOptions != null)
                {
                    pOptions.objectReferenceValue = optionsPanelObj;
                    menuSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(menuController);
                }
            }

            // Sincronizar visualmente los sprites de inmediato según los booleanos
            modal.UpdateAllUI();
            EditorUtility.SetDirty(modal);

            // Asegurar que todos los elementos de UI pertenezcan a la capa UI (5)
            SetLayerRecursively(optionsPanelObj, 5);

            // Ocultar modal por defecto al inicio
            optionsPanelObj.SetActive(false);

            // Guardar cambios en la escena
            EditorUtility.SetDirty(optionsPanelObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#55FF55><b>[SettingsPanelTool]</b></color> SettingsPanel (OptionsPanel) reconstruido con HIGHLIGHT Alpha 70 y Toggles booleanos activos/inactivos.");
        }

        private static void WireButtonEvent(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
            {
                while (btn.onClick.GetPersistentEventCount() > 0)
                    UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                UnityEventTools.AddPersistentListener(btn.onClick, action);
            }
        }

        private struct RowReferences
        {
            public Image highlightImage;
            public Button iconButton;
            public Image btnBgImage;
            public Image iconImage;
            public TextMeshProUGUI labelText;
        }

        private static RowReferences CreateSettingRow(string name, Transform parent, float yPos, Sprite highlightSprite, Color highlightColor, Sprite btnSprite, Sprite iconSprite, string label, TMP_FontAsset font)
        {
            // Row Container
            GameObject rowObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rowObj.transform.SetParent(parent, false);

            var rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = new Vector2(0f, yPos);
            rowRect.sizeDelta = new Vector2(820f, 150f);

            var rowImg = rowObj.GetComponent<Image>();
            rowImg.sprite = highlightSprite;
            rowImg.color = highlightColor;
            rowImg.raycastTarget = false; // Permite que los clicks pasen sin bloquear

            // BTN_ICON (El botón toggle interactivo)
            GameObject btnIconObj = new GameObject("BtnIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
            btnIconObj.transform.SetParent(rowObj.transform, false);

            var btnIconRect = btnIconObj.GetComponent<RectTransform>();
            btnIconRect.anchorMin = new Vector2(0f, 0.5f);
            btnIconRect.anchorMax = new Vector2(0f, 0.5f);
            btnIconRect.pivot = new Vector2(0f, 0.5f);
            btnIconRect.anchoredPosition = new Vector2(0f, 0f);
            btnIconRect.sizeDelta = new Vector2(150f, 150f);

            var btnIconImg = btnIconObj.GetComponent<Image>();
            btnIconImg.sprite = btnSprite;
            btnIconImg.type = Image.Type.Simple;
            btnIconImg.color = Color.white;
            btnIconImg.raycastTarget = true;

            var btnIconComp = btnIconObj.GetComponent<Button>();
            btnIconComp.targetGraphic = btnIconImg;
            btnIconComp.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = btnIconComp.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            btnIconComp.colors = colors;

            // Icon inside BTN_ICON
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(btnIconObj.transform, false);

            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-3f, 0f);
            iconRect.sizeDelta = new Vector2(80f, 80f);

            var iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            // Label Text
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(rowObj.transform, false);

            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.offsetMin = new Vector2(175f, 0f);
            labelRect.offsetMax = new Vector2(-25f, 0f);

            var labelTMP = labelObj.GetComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            if (font != null) labelTMP.font = font;
            labelTMP.fontSize = 36f;
            labelTMP.enableAutoSizing = true;
            labelTMP.fontSizeMin = 24f;
            labelTMP.fontSizeMax = 36f;
            labelTMP.characterSpacing = 3f;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.color = Color.white;
            labelTMP.raycastTarget = false;

            return new RowReferences
            {
                highlightImage = rowImg,
                iconButton = btnIconComp,
                btnBgImage = btnIconImg,
                iconImage = iconImg,
                labelText = labelTMP
            };
        }

        private static GameObject CreateActionButton(string name, Transform parent, Sprite bgSprite, Sprite iconSprite, float btnSize, float iconSize)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonPressEffect));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(btnSize, btnSize);

            var img = btnObj.GetComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = true;

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
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
            iconImg.raycastTarget = false;

            return btnObj;
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
