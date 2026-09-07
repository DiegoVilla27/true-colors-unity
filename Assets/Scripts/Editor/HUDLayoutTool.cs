#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace TrueColors.EditorTools
{
    public class HUDLayoutTool : EditorWindow
    {
        [SerializeField] private float hudHeight = 110f;
        [SerializeField] private float sidePadding = 20f;
        [SerializeField] private float verticalPadding = 8f;
        [SerializeField] private float iconTextSpacing = 12f;
        [SerializeField] private float scoreOffsetX = 0f;
        [SerializeField] private float scoreOffsetY = 0f;

        [MenuItem("Tools/HUD/Abrir Ventana de Configuración HUD...")]
        public static void OpenWindow()
        {
            var window = GetWindow<HUDLayoutTool>("Configurador HUD");
            window.minSize = new Vector2(380, 340);
            window.Show();
        }

        [MenuItem("Tools/HUD/Ajustar HUD Completo (3 Columnas con Iconos)")]
        public static void QuickAdjustDefault()
        {
            ApplyHUDSetup(110f, 20f, 8f, 12f, 0f, 0f);
        }

        [MenuItem("Tools/HUD/Score/Mover Score: 6px a la Izquierda")]
        public static void NudgeScoreLeft()
        {
            NudgeScore(-6f, 0f);
        }

        [MenuItem("Tools/HUD/Score/Mover Score: 6px a la Derecha")]
        public static void NudgeScoreRight()
        {
            NudgeScore(6f, 0f);
        }

        [MenuItem("Tools/HUD/Score/Mover Score: 4px hacia Arriba")]
        public static void NudgeScoreUp()
        {
            NudgeScore(0f, 4f);
        }

        [MenuItem("Tools/HUD/Score/Mover Score: 4px hacia Abajo")]
        public static void NudgeScoreDown()
        {
            NudgeScore(0f, -4f);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Configurador de HUD (3 Columnas)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• HUD: Fondo al 100% con HUD.png\n" +
                "• Container: 3 columnas simétricas (1/3 cada una)\n" +
                "  [ Time: Icono + Texto ]\n" +
                "  [ Score: Solo Texto centrado con micro-ajuste ]\n" +
                "  [ Rocks: Icono + Texto ]",
                MessageType.Info
            );

            GUILayout.Space(8);
            EditorGUI.BeginChangeCheck();

            hudHeight = EditorGUILayout.Slider("Alto del HUD", hudHeight, 90f, 220f);
            sidePadding = EditorGUILayout.Slider("Padding Lateral", sidePadding, 0f, 50f);
            verticalPadding = EditorGUILayout.Slider("Padding Vertical", verticalPadding, 0f, 25f);
            iconTextSpacing = EditorGUILayout.Slider("Espacio Icono-Texto", iconTextSpacing, 4f, 30f);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Micro-Ajuste Óptico del Score Central", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Debido a la inclinación cursiva de la fuente (Ethnocentric) o al marco del sprite, " +
                "puedes ajustar el texto central aquí en tiempo real:",
                MessageType.None
            );

            scoreOffsetX = EditorGUILayout.Slider("Ajuste X Score (Izq <-> Der)", scoreOffsetX, -40f, 40f);
            scoreOffsetY = EditorGUILayout.Slider("Ajuste Y Score (Abajo <-> Arriba)", scoreOffsetY, -25f, 25f);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyHUDSetup(hudHeight, sidePadding, verticalPadding, iconTextSpacing, scoreOffsetX, scoreOffsetY, isSilent: true);
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Centrar Score (0,0)"))
            {
                scoreOffsetX = 0f;
                scoreOffsetY = 0f;
                ApplyHUDSetup(hudHeight, sidePadding, verticalPadding, iconTextSpacing, scoreOffsetX, scoreOffsetY);
            }
            if (GUILayout.Button("Compensar Cursiva (-8px X)"))
            {
                scoreOffsetX = -8f;
                ApplyHUDSetup(hudHeight, sidePadding, verticalPadding, iconTextSpacing, scoreOffsetX, scoreOffsetY);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(12);
            if (GUILayout.Button("¡Aplicar / Reconstruir Todo el HUD!", GUILayout.Height(36)))
            {
                ApplyHUDSetup(hudHeight, sidePadding, verticalPadding, iconTextSpacing, scoreOffsetX, scoreOffsetY);
            }
        }

        private static void NudgeScore(float deltaX, float deltaY)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var scoreCol = canvas.transform.Find("HUD/Container/Score");
            if (scoreCol == null) return;

            Transform textChild = scoreCol.Find("Text") ?? scoreCol.Find("Text (TMP)");
            RectTransform targetRect = textChild != null ? textChild.GetComponent<RectTransform>() : scoreCol.GetComponent<RectTransform>();

            if (targetRect != null)
            {
                Undo.RecordObject(targetRect, "Ajustar Posición Score");
                Vector2 pos = targetRect.anchoredPosition;
                pos.x += deltaX;
                pos.y += deltaY;
                targetRect.anchoredPosition = pos;
                EditorUtility.SetDirty(targetRect);
                EditorSceneManager.MarkSceneDirty(targetRect.gameObject.scene);
                Debug.Log($"<color=#55FF55><b>[HUD Tool]</b></color> Score movido a: X={pos.x:F1}, Y={pos.y:F1}");
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoAdjustOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                var hud = GameObject.Find("HUD");
                if (hud != null)
                {
                    ApplyHUDSetup(110f, 20f, 8f, 12f, 0f, 0f, isSilent: true);
                }
            };
        }

        public static void ApplyHUDSetup(
            float height,
            float paddingSides,
            float paddingVertical,
            float spacing,
            float scoreShiftX = 0f,
            float scoreShiftY = 0f,
            bool isSilent = false)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                if (!isSilent) EditorUtility.DisplayDialog("Error", "No se encontró ningún Canvas en la escena activa.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Ajustar HUD (3 Columnas)");

            // Assets
            Sprite hudSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/hud/HUD.png");
            Sprite clockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/icons/CLOCK.png");
            Sprite rockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/icons/ROCK.png");
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Ethnocentric-Regular SDF.asset");

            // 1. HUD (Contenedor Maestro al 100% de ancho)
            Transform hudTransform = canvas.transform.Find("HUD");
            GameObject hudGo;
            if (hudTransform == null)
            {
                hudGo = new GameObject("HUD", typeof(RectTransform));
                hudGo.transform.SetParent(canvas.transform, false);
            }
            else
            {
                hudGo = hudTransform.gameObject;
            }

            var hudRect = hudGo.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f); // Top Stretch
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.anchoredPosition = Vector2.zero;
            hudRect.sizeDelta = new Vector2(0f, height);
            hudRect.offsetMin = new Vector2(0f, -height);
            hudRect.offsetMax = new Vector2(0f, 0f);

            // 2. Background (Fondo estirado al 100%)
            Transform bgTransform = hudGo.transform.Find("Background");
            GameObject bgGo;
            if (bgTransform == null)
            {
                bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(hudGo.transform, false);
            }
            else
            {
                bgGo = bgTransform.gameObject;
            }

            bgGo.transform.SetSiblingIndex(0); // Detrás de todo
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGo.GetComponent<Image>();
            if (bgImage != null)
            {
                if (hudSprite != null)
                {
                    bgImage.sprite = hudSprite;
                    bgImage.type = Image.Type.Simple;
                    bgImage.preserveAspect = false;
                }
                bgImage.raycastTarget = false;
                bgImage.color = Color.white;
            }

            // 3. Container (Fila horizontal dividida en 3 columnas iguales)
            Transform containerTransform = hudGo.transform.Find("Container");
            GameObject containerGo;
            if (containerTransform == null)
            {
                containerGo = new GameObject("Container", typeof(RectTransform));
                containerGo.transform.SetParent(hudGo.transform, false);
            }
            else
            {
                containerGo = containerTransform.gameObject;
            }

            containerGo.transform.SetSiblingIndex(1); // Encima del background
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(paddingSides, paddingVertical);
            containerRect.offsetMax = new Vector2(-paddingSides, -paddingVertical);

            // HorizontalLayoutGroup del Container (Garantiza 3 columnas de exactamente el mismo ancho)
            var containerLayout = containerGo.GetComponent<HorizontalLayoutGroup>();
            if (containerLayout == null)
            {
                containerLayout = containerGo.AddComponent<HorizontalLayoutGroup>();
            }

            containerLayout.padding = new RectOffset(0, 0, 0, 0);
            containerLayout.spacing = 0f;
            containerLayout.childAlignment = TextAnchor.MiddleCenter;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = true;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = true;

            // 4. Configurar Columna 1: Time [Icono + Texto]
            SetupIconTextColumn(
                containerGo.transform,
                hudGo.transform,
                "Time",
                clockSprite,
                new Vector2(46f, 50f),
                "00:00",
                spacing,
                fontAsset,
                out TextMeshProUGUI timeTMP
            );

            // 5. Configurar Columna 2: Score [Solo Texto Centrado con micro-ajuste]
            SetupScoreColumn(
                containerGo.transform,
                hudGo.transform,
                "Score",
                "0",
                fontAsset,
                scoreShiftX,
                scoreShiftY,
                out TextMeshProUGUI scoreTMP
            );

            // 6. Configurar Columna 3: Rocks [Icono + Texto]
            SetupIconTextColumn(
                containerGo.transform,
                hudGo.transform,
                "Rocks",
                rockSprite,
                new Vector2(32f, 50f),
                "0",
                spacing,
                fontAsset,
                out TextMeshProUGUI rocksTMP
            );

            // Asegurar orden de las 3 columnas en Container: Time (0), Score (1), Rocks (2)
            var timeCol = containerGo.transform.Find("Time");
            var scoreCol = containerGo.transform.Find("Score");
            var rocksCol = containerGo.transform.Find("Rocks");

            if (timeCol != null) timeCol.SetSiblingIndex(0);
            if (scoreCol != null) scoreCol.SetSiblingIndex(1);
            if (rocksCol != null) rocksCol.SetSiblingIndex(2);

            // 7. Auto-vincular al GameHUD si existe
            var gameHUD = Object.FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                var so = new SerializedObject(gameHUD);
                if (scoreTMP != null)
                {
                    var propScore = so.FindProperty("scoreText");
                    if (propScore != null) propScore.objectReferenceValue = scoreTMP;
                }
                if (rocksTMP != null)
                {
                    var propRocks = so.FindProperty("rocksText");
                    if (propRocks != null) propRocks.objectReferenceValue = rocksTMP;
                }
                if (timeTMP != null)
                {
                    var propTime = so.FindProperty("timeText");
                    if (propTime != null) propTime.objectReferenceValue = timeTMP;
                }
                so.ApplyModifiedProperties();
            }

            // Marcar dirty y registrar Undo
            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(hudGo);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            EditorGUIUtility.PingObject(hudGo);
            Selection.activeGameObject = hudGo;

            if (!isSilent)
            {
                Debug.Log($"<color=#55FF55><b>[HUD Tool]</b></color> HUD configurado con éxito. Score offset: ({scoreShiftX}, {scoreShiftY}).");
            }
        }

        private static void SetupIconTextColumn(
            Transform container,
            Transform hud,
            string columnName,
            Sprite iconSprite,
            Vector2 iconSize,
            string defaultText,
            float spacing,
            TMP_FontAsset fontAsset,
            out TextMeshProUGUI textTMP)
        {
            Transform col = container.Find(columnName) ?? hud.Find(columnName);
            if (col == null && columnName == "Rocks") col = container.Find("Rock") ?? hud.Find("Rock");

            GameObject colGo;
            if (col == null)
            {
                colGo = new GameObject(columnName, typeof(RectTransform));
                colGo.transform.SetParent(container, false);
            }
            else
            {
                colGo = col.gameObject;
                if (colGo.transform.parent != container)
                {
                    colGo.transform.SetParent(container, false);
                }
            }

            // Fijar pivote central en la columna para que la alineación sea idéntica a las otras
            var colRect = colGo.GetComponent<RectTransform>();
            colRect.pivot = new Vector2(0.5f, 0.5f);

            // HorizontalLayoutGroup en la columna para centrar el icono + texto juntos
            var layout = colGo.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = colGo.AddComponent<HorizontalLayoutGroup>();
            }

            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter; // Centrado total
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 1. Icono (Image)
            Transform iconTransform = colGo.transform.Find("Icon") ?? colGo.transform.Find("Image");
            GameObject iconGo;
            if (iconTransform == null)
            {
                iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(colGo.transform, false);
            }
            else
            {
                iconGo = iconTransform.gameObject;
            }

            iconGo.transform.SetSiblingIndex(0); // Primero el icono
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = iconSize;

            var image = iconGo.GetComponent<Image>();
            if (image != null)
            {
                if (iconSprite != null) image.sprite = iconSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = Color.white;
            }

            // 2. Texto (TextMeshProUGUI)
            Transform textTransform = colGo.transform.Find("Text") ?? colGo.transform.Find("Text (TMP)");
            GameObject textGo;
            if (textTransform == null)
            {
                textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(colGo.transform, false);
            }
            else
            {
                textGo = textTransform.gameObject;
            }

            textGo.transform.SetSiblingIndex(1); // Después del icono
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(130f, 50f);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textGo.AddComponent<TextMeshProUGUI>();

            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18;
            tmp.fontSizeMax = 36;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            if (fontAsset != null) tmp.font = fontAsset;
            if (string.IsNullOrEmpty(tmp.text)) tmp.text = defaultText;

            textTMP = tmp;
        }

        private static void SetupScoreColumn(
            Transform container,
            Transform hud,
            string columnName,
            string defaultText,
            TMP_FontAsset fontAsset,
            float shiftX,
            float shiftY,
            out TextMeshProUGUI scoreTMP)
        {
            Transform col = container.Find(columnName) ?? hud.Find(columnName);
            GameObject colGo;
            if (col == null)
            {
                colGo = new GameObject(columnName, typeof(RectTransform));
                colGo.transform.SetParent(container, false);
            }
            else
            {
                colGo = col.gameObject;
                if (colGo.transform.parent != container)
                {
                    colGo.transform.SetParent(container, false);
                }
            }

            var colRect = colGo.GetComponent<RectTransform>();
            colRect.pivot = new Vector2(0.5f, 0.5f);

            // Eliminar HorizontalLayoutGroup si existía en la columna de Score para evitar conflictos
            var oldLayout = colGo.GetComponent<HorizontalLayoutGroup>();
            if (oldLayout != null)
            {
                DestroyImmediate(oldLayout);
            }

            // Asegurar estructura limpia: Score (Columna) -> Text (Hijo con TextMeshPro)
            Transform textChild = colGo.transform.Find("Text") ?? colGo.transform.Find("Text (TMP)");
            GameObject textGo;

            // Si el propio colGo tenía el TextMeshPro, lo migramos limpiamente a hijo si es necesario o lo usamos
            var directTMP = colGo.GetComponent<TextMeshProUGUI>();
            if (directTMP != null && textChild == null)
            {
                // Usamos el directo
                scoreTMP = directTMP;
                textGo = colGo;
            }
            else
            {
                if (directTMP != null)
                {
                    DestroyImmediate(directTMP);
                }

                if (textChild == null)
                {
                    textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(colGo.transform, false);
                }
                else
                {
                    textGo = textChild.gameObject;
                }
                scoreTMP = textGo.GetComponent<TextMeshProUGUI>() ?? textGo.AddComponent<TextMeshProUGUI>();
            }

            // Configurar RectTransform con el offset
            var rect = scoreTMP.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(shiftX, shiftY);
            rect.offsetMax = new Vector2(shiftX, shiftY);

            scoreTMP.alignment = TextAlignmentOptions.Center; // Centrado total
            scoreTMP.textWrappingMode = TextWrappingModes.NoWrap;
            scoreTMP.overflowMode = TextOverflowModes.Ellipsis;
            scoreTMP.enableAutoSizing = true;
            scoreTMP.fontSizeMin = 22;
            scoreTMP.fontSizeMax = 44;
            scoreTMP.color = Color.white;
            scoreTMP.raycastTarget = false;

            if (fontAsset != null) scoreTMP.font = fontAsset;
            if (string.IsNullOrEmpty(scoreTMP.text)) scoreTMP.text = defaultText;
        }
    }
}
#endif
