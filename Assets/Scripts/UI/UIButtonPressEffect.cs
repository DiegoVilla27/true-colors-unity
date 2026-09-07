using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Proporciona un efecto físico de compresión (squish)
/// arcade ultra-reactivo al presionar cualquier botón de UI.
/// Soporta vibración háptica al pulsar y retorno suave con amortiguación.
/// Funciona con Time.unscaledDeltaTime (inmune a pausas del juego).
/// Compatible al 100% con LayoutGroups (Horizontal, Vertical, Grid), previniendo
/// oscilaciones, jitter y conflictos de posicionamiento.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UIButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
  [Header("Efecto Físico")]
  [Tooltip("Escala al presionar (efecto squish)")]
  [SerializeField] private Vector3 pressedScale = new Vector3(0.92f, 0.92f, 1f);

  [Tooltip("Desplazamiento vertical hacia abajo al presionar (solo aplica si NO está en un LayoutGroup)")]
  [SerializeField] private float sinkYOffset = 0f;

  [Tooltip("Velocidad de transición al hundirse y al rebotar")]
  [SerializeField] private float animationSpeed = 24f;

  [Header("Retroalimentación Háptica")]
  [SerializeField] private bool triggerHaptics = true;

  private RectTransform rectTransform;
  private Selectable selectable;
  private bool isDrivenByLayout = false;
  private Vector3 originalAnchoredPos;
  private Vector3 targetAnchoredPos;
  private Vector3 targetScale = Vector3.one;
  private bool isPressed = false;
  private bool isAnimating = false;

  void Awake()
  {
    rectTransform = GetComponent<RectTransform>();
    selectable = GetComponent<Selectable>();
    CheckLayoutDriver();
    targetScale = Vector3.one;
  }

  void OnEnable()
  {
    CheckLayoutDriver();
    ResetToOriginal();
  }

  void OnDisable()
  {
    ResetToOriginal();
  }

  private void CheckLayoutDriver()
  {
    // Si el padre directo gestiona el layout de este elemento, NUNCA alteramos anchoredPosition
    isDrivenByLayout = (transform.parent != null && transform.parent.GetComponent<LayoutGroup>() != null);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (selectable != null && !selectable.interactable) return;

    isPressed = true;
    isAnimating = true;
    targetScale = pressedScale;

    // Solo capturamos y desplazamos posición si el botón no está gobernado por un LayoutGroup
    if (!isDrivenByLayout && Mathf.Abs(sinkYOffset) > 0.01f && rectTransform != null)
    {
      originalAnchoredPos = rectTransform.anchoredPosition3D;
      targetAnchoredPos = originalAnchoredPos + new Vector3(0f, sinkYOffset, 0f);
    }

    if (triggerHaptics)
    {
      HapticFeedback.VibrateCollect();
    }
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    Release();
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    Release();
  }

  private void Release()
  {
    if (!isPressed) return;
    isPressed = false;
    isAnimating = true;
    targetScale = Vector3.one;

    if (!isDrivenByLayout && Mathf.Abs(sinkYOffset) > 0.01f)
    {
      targetAnchoredPos = originalAnchoredPos;
    }
  }

  void Update()
  {
    // Optimización de rendimiento: no hacer nada en idle para no ensuciar el Canvas
    if (!isAnimating || rectTransform == null) return;

    float dt = Time.unscaledDeltaTime * animationSpeed;

    // 1. Interpolar escala
    rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, dt);

    // 2. Interpolar posición SOLO si NO está gobernado por un LayoutGroup
    if (!isDrivenByLayout && Mathf.Abs(sinkYOffset) > 0.01f)
    {
      rectTransform.anchoredPosition3D = Vector3.Lerp(rectTransform.anchoredPosition3D, targetAnchoredPos, dt);
    }

    // Comprobar si ha convergido para finalizar animación y evitar recalcular layout
    bool scaleDone = Vector3.Distance(rectTransform.localScale, targetScale) < 0.002f;
    bool posDone = isDrivenByLayout || Mathf.Abs(sinkYOffset) <= 0.01f || Vector3.Distance(rectTransform.anchoredPosition3D, targetAnchoredPos) < 0.05f;

    if (scaleDone && posDone)
    {
      rectTransform.localScale = targetScale;
      if (!isDrivenByLayout && Mathf.Abs(sinkYOffset) > 0.01f)
      {
        rectTransform.anchoredPosition3D = targetAnchoredPos;
      }
      isAnimating = false;
    }
  }

  public void ResetToOriginal()
  {
    isPressed = false;
    isAnimating = false;
    targetScale = Vector3.one;

    if (rectTransform != null)
    {
      rectTransform.localScale = Vector3.one;
      if (!isDrivenByLayout && Mathf.Abs(sinkYOffset) > 0.01f && originalAnchoredPos != Vector3.zero)
      {
        rectTransform.anchoredPosition3D = originalAnchoredPos;
      }
    }
  }
}
