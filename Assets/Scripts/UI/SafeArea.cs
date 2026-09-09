using UnityEngine;
using UnityEngine.UI;

namespace TrueColors.UI
{
    public enum SafeAreaMode
    {
        TopInset,         // Desplaza elementos anclados arriba (como HUD o BtnPause) debajo del Dynamic Island / Notch
        BottomInset,      // Desplaza elementos anclados abajo (como Version) encima del Home Indicator
        FullContainer     // Ajusta anchorMin y anchorMax para ocupar todo el Safe Area
    }

    public enum EditorCutoutSimulation
    {
        None,
        iPhone_DynamicIsland, // iPhone 14 Pro, 15, 16, 17 (Dynamic Island + Home Indicator)
        iPhone_Notch,         // iPhone X, 11, 12, 13 (Notch clásico + Home Indicator)
        Android_PunchHole,    // Punch Hole superior centrado
        Custom
    }

    /// <summary>
    /// Gestiona de forma automática y de alto rendimiento el Safe Area de cualquier dispositivo (iOS / Android).
    /// Evita que el Dynamic Island, Notches, esquinas redondeadas o barras de inicio (Home Indicator)
    /// obstruyan elementos críticos de la interfaz (HUD, Version, Botones).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeArea : MonoBehaviour
    {
        [Header("Modo")]
        [SerializeField] private SafeAreaMode mode = SafeAreaMode.TopInset;

        [Header("Padding adicional")]
        [SerializeField] private float extraTopPadding = 0f;
        [SerializeField] private float extraBottomPadding = 0f;
        [SerializeField] private float extraLeftPadding = 0f;
        [SerializeField] private float extraRightPadding = 0f;

        [Header("Relleno de Notch / Dynamic Island (Sólo para TopInset)")]
        [Tooltip("Crea una extensión visual de fondo hacia el borde superior del dispositivo")]
        [SerializeField] private bool createNotchFiller = false;
        [SerializeField] private Color notchFillerColor = Color.black;

        [Header("Simulación en Editor (Para testing sin dispositivo físico)")]
        [SerializeField] private bool simulateInEditor = false;
        [SerializeField] private EditorCutoutSimulation simulationPreset = EditorCutoutSimulation.iPhone_DynamicIsland;
        [SerializeField] private Rect customSimulatedSafeArea = new Rect(0, 102, 1179, 2300);

        [Header("Valores Base")]
        [SerializeField] private Vector2 baseAnchoredPosition = Vector2.zero;
        [SerializeField] private Vector2 baseOffsetMin = Vector2.zero;
        [SerializeField] private Vector2 baseOffsetMax = Vector2.zero;
        [SerializeField] private bool hasCapturedBase = false;

        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private RectTransform _canvasRect;
        private GameObject _notchFillerObj;

        // Cache de estado
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        public SafeAreaMode Mode => mode;

        public void SetMode(SafeAreaMode newMode)
        {
            mode = newMode;
            hasCapturedBase = false;
            CaptureBaseValues();
            ApplySafeArea();
        }

        public void SetNotchFiller(bool enabled, Color color)
        {
            createNotchFiller = enabled;
            notchFillerColor = color;
            ApplySafeArea();
        }

        public void SetBaseAnchoredPosition(Vector2 pos)
        {
            baseAnchoredPosition = pos;
            hasCapturedBase = true;
        }

        public void SetBaseValues(Vector2 pos, Vector2 minOffset, Vector2 maxOffset)
        {
            baseAnchoredPosition = pos;
            baseOffsetMin = minOffset;
            baseOffsetMax = maxOffset;
            hasCapturedBase = true;
        }

        public void SetSimulation(bool enable, EditorCutoutSimulation preset = EditorCutoutSimulation.iPhone_DynamicIsland)
        {
            simulateInEditor = enable;
            simulationPreset = preset;
            ApplySafeArea();
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            LocateCanvas();
            CaptureBaseValues();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            LocateCanvas();
            CaptureBaseValues();
            ApplySafeArea();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            // Solo recalcular si la orientación, resolución o safe area física cambiaron
            var currentScreenSize = new Vector2Int(Screen.width, Screen.height);
            var currentSafeArea = GetActiveSafeArea();
            var currentOrientation = Screen.orientation;

            if (currentSafeArea != _lastSafeArea ||
                currentScreenSize != _lastScreenSize ||
                currentOrientation != _lastOrientation)
            {
                ApplySafeArea();
            }
        }

        private void LocateCanvas()
        {
            if (_parentCanvas == null)
            {
                _parentCanvas = GetComponentInParent<Canvas>();
            }
            if (_parentCanvas != null && _canvasRect == null)
            {
                _canvasRect = _parentCanvas.GetComponent<RectTransform>();
            }
        }

        [ContextMenu("Recapturar Valores Base")]
        public void CaptureBaseValues()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) return;

            if (!hasCapturedBase)
            {
                baseAnchoredPosition = _rectTransform.anchoredPosition;
                baseOffsetMin = _rectTransform.offsetMin;
                baseOffsetMax = _rectTransform.offsetMax;
                hasCapturedBase = true;
            }
        }

        public Rect GetActiveSafeArea()
        {
#if UNITY_EDITOR
            if (simulateInEditor)
            {
                float w = Screen.width;
                float h = Screen.height;

                switch (simulationPreset)
                {
                    case EditorCutoutSimulation.iPhone_DynamicIsland:
                    {
                        float topH = h * 0.069f;    // ~59pt en ratio iPhone
                        float bottomH = h * 0.040f; // ~34pt en ratio iPhone
                        return new Rect(0f, bottomH, w, h - topH - bottomH);
                    }
                    case EditorCutoutSimulation.iPhone_Notch:
                    {
                        float topH = h * 0.056f;    // ~47pt en ratio iPhone
                        float bottomH = h * 0.040f; // ~34pt
                        return new Rect(0f, bottomH, w, h - topH - bottomH);
                    }
                    case EditorCutoutSimulation.Android_PunchHole:
                    {
                        float topH = h * 0.045f;
                        float bottomH = h * 0.025f;
                        return new Rect(0f, bottomH, w, h - topH - bottomH);
                    }
                    case EditorCutoutSimulation.Custom:
                        return customSimulatedSafeArea;
                    default:
                        break;
                }
            }
#endif
            return Screen.safeArea;
        }

        [ContextMenu("Aplicar Safe Area Ahora")]
        public void ApplySafeArea()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) return;

            LocateCanvas();
            if (_parentCanvas == null || _canvasRect == null) return;

            Rect safeArea = GetActiveSafeArea();
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            // Insets en unidades Canvas
            Vector4 canvasInsets = GetCanvasInsets(_canvasRect, safeArea, screenSize);
            float leftInset = canvasInsets.x;
            float bottomInset = canvasInsets.y;
            float rightInset = canvasInsets.z;
            float topInset = canvasInsets.w;

            switch (mode)
            {
                case SafeAreaMode.TopInset:
                    ApplyTopInset(topInset, rightInset);
                    break;

                case SafeAreaMode.BottomInset:
                    ApplyBottomInset(bottomInset, leftInset);
                    break;

                case SafeAreaMode.FullContainer:
                    ApplyFullContainer(safeArea, screenSize);
                    break;
            }

            // Gestionar relleno decorativo superior si está habilitado
            UpdateNotchFiller(topInset);

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;
            _lastOrientation = Screen.orientation;
        }

        private void ApplyTopInset(float topInset, float rightInset)
        {
            // Si el objeto está estirado en la parte superior (ej. HUD):
            if (Mathf.Approximately(_rectTransform.anchorMin.y, 1f) && Mathf.Approximately(_rectTransform.anchorMax.y, 1f))
            {
                if (Mathf.Approximately(_rectTransform.anchorMin.x, 0f) && Mathf.Approximately(_rectTransform.anchorMax.x, 1f))
                {
                    // Top Stretch (ej. HUD): desplaza offsetMax.y y offsetMin.y hacia abajo
                    float shift = topInset + extraTopPadding;
                    _rectTransform.offsetMax = new Vector2(baseOffsetMax.x, baseOffsetMax.y - shift);
                    _rectTransform.offsetMin = new Vector2(baseOffsetMin.x, baseOffsetMin.y - shift);
                }
                else
                {
                    // Top Right o Top Left (ej. BtnPause): desplaza anchoredPosition hacia abajo
                    float shiftY = topInset + extraTopPadding;
                    float shiftX = extraRightPadding;
                    if (Mathf.Approximately(_rectTransform.anchorMin.x, 1f))
                    {
                        shiftX += rightInset;
                        _rectTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x - shiftX, baseAnchoredPosition.y - shiftY);
                    }
                    else
                    {
                        _rectTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x, baseAnchoredPosition.y - shiftY);
                    }
                }
            }
            else
            {
                // Desplazamiento general hacia abajo
                float shiftY = topInset + extraTopPadding;
                _rectTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x, baseAnchoredPosition.y - shiftY);
            }
        }

        private void ApplyBottomInset(float bottomInset, float leftInset)
        {
            // Para elementos anclados abajo (ej. Version en esquina inferior izquierda)
            float shiftY = bottomInset + extraBottomPadding;
            float shiftX = leftInset + extraLeftPadding;

            _rectTransform.anchoredPosition = new Vector2(
                baseAnchoredPosition.x + shiftX,
                baseAnchoredPosition.y + shiftY
            );
        }

        private void ApplyFullContainer(Rect safeArea, Vector2Int screenSize)
        {
            Vector2 minAnchor = safeArea.position;
            Vector2 maxAnchor = minAnchor + safeArea.size;

            minAnchor.x /= screenSize.x;
            minAnchor.y /= screenSize.y;
            maxAnchor.x /= screenSize.x;
            maxAnchor.y /= screenSize.y;

            _rectTransform.anchorMin = minAnchor;
            _rectTransform.anchorMax = maxAnchor;
            _rectTransform.offsetMin = new Vector2(extraLeftPadding, extraBottomPadding);
            _rectTransform.offsetMax = new Vector2(-extraRightPadding, -extraTopPadding);
        }

        private void UpdateNotchFiller(float topInset)
        {
            if (!createNotchFiller)
            {
                if (_notchFillerObj != null) _notchFillerObj.SetActive(false);
                return;
            }

            if (topInset <= 0.5f)
            {
                if (_notchFillerObj != null) _notchFillerObj.SetActive(false);
                return;
            }

            if (_notchFillerObj == null)
            {
                Transform existing = transform.Find("TopNotchFiller");
                if (existing != null)
                {
                    _notchFillerObj = existing.gameObject;
                }
                else
                {
                    _notchFillerObj = new GameObject("TopNotchFiller", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    _notchFillerObj.transform.SetParent(transform, false);
                    _notchFillerObj.transform.SetAsFirstSibling(); // Detrás de los elementos del HUD
                }
            }

            _notchFillerObj.SetActive(true);

            var fillerRect = _notchFillerObj.GetComponent<RectTransform>();
            fillerRect.anchorMin = new Vector2(0f, 1f);
            fillerRect.anchorMax = new Vector2(1f, 1f);
            fillerRect.pivot = new Vector2(0.5f, 0f);
            fillerRect.anchoredPosition = Vector2.zero;
            fillerRect.sizeDelta = new Vector2(0f, topInset + 5f); // +5f para garantizar unión perfecta sin huecos

            var fillerImg = _notchFillerObj.GetComponent<Image>();
            if (fillerImg != null)
            {
                fillerImg.color = notchFillerColor;
                fillerImg.raycastTarget = false;
            }
        }

        public static Vector4 GetCanvasInsets(RectTransform canvasRect, Rect safeArea, Vector2Int screenSize)
        {
            if (canvasRect == null || screenSize.x <= 0 || screenSize.y <= 0)
                return Vector4.zero;

            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;

            float leftRatio = safeArea.xMin / screenSize.x;
            float rightRatio = (screenSize.x - safeArea.xMax) / screenSize.x;
            float bottomRatio = safeArea.yMin / screenSize.y;
            float topRatio = (screenSize.y - safeArea.yMax) / screenSize.y;

            return new Vector4(
                leftRatio * canvasWidth,       // x = left
                bottomRatio * canvasHeight,    // y = bottom
                rightRatio * canvasWidth,      // z = right
                topRatio * canvasHeight        // w = top
            );
        }
    }
}
