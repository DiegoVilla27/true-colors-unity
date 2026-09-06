using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Proporciona un efecto físico de "hundimiento" (sink/press)
/// y compresión (squish) arcade ultra-reactivo al presionar cualquier botón de UI.
/// Soporta vibración háptica al pulsar y retorno con amortiguación elástica.
/// Funciona con Time.unscaledDeltaTime (inmune a pausas del juego).
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UIButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
  [Header("Efecto Físico de Hundimiento")]
  [Tooltip("Desplazamiento vertical hacia abajo al presionar (en píxeles de Canvas)")]
  [SerializeField] private float sinkYOffset = -10f;

  [Tooltip("Escala al hundirse (efecto squish)")]
  [SerializeField] private Vector3 pressedScale = new Vector3(0.96f, 0.94f, 1f);

  [Tooltip("Velocidad de transición al hundirse y al rebotar")]
  [SerializeField] private float animationSpeed = 22f;

  [Header("Retroalimentación Háptica")]
  [SerializeField] private bool triggerHaptics = true;

  private RectTransform rectTransform;
  private Selectable selectable;
  private Vector3 originalAnchoredPos;
  private Vector3 targetAnchoredPos;
  private Vector3 targetScale;
  private bool isPressed = false;

  void Awake()
  {
    rectTransform = GetComponent<RectTransform>();
    selectable = GetComponent<Selectable>();

    if (rectTransform != null)
    {
      originalAnchoredPos = rectTransform.anchoredPosition3D;
      targetAnchoredPos = originalAnchoredPos;
      targetScale = Vector3.one;
    }
  }

  void OnEnable()
  {
    ResetToOriginal();
  }

  void OnDisable()
  {
    ResetToOriginal();
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (selectable != null && !selectable.interactable) return;

    isPressed = true;
    targetAnchoredPos = originalAnchoredPos + new Vector3(0f, sinkYOffset, 0f);
    targetScale = pressedScale;

    if (triggerHaptics)
    {
      HapticFeedback.VibrateCollect();
    }
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (!isPressed) return;
    isPressed = false;
    targetAnchoredPos = originalAnchoredPos;
    targetScale = Vector3.one;
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    if (!isPressed) return;
    isPressed = false;
    targetAnchoredPos = originalAnchoredPos;
    targetScale = Vector3.one;
  }

  void Update()
  {
    if (rectTransform == null) return;

    // Interpolación suave y elástica con tiempo no escalado
    float dt = Time.unscaledDeltaTime * animationSpeed;

    rectTransform.anchoredPosition3D = Vector3.Lerp(rectTransform.anchoredPosition3D, targetAnchoredPos, dt);
    rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, dt);
  }

  public void ResetToOriginal()
  {
    isPressed = false;
    if (rectTransform != null)
    {
      rectTransform.anchoredPosition3D = originalAnchoredPos;
      rectTransform.localScale = Vector3.one;
      targetAnchoredPos = originalAnchoredPos;
      targetScale = Vector3.one;
    }
  }
}
