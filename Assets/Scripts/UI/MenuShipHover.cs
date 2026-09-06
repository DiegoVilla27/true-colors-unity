using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Proporciona un suave movimiento de flotación (hover)
/// senoidal en el eje Y para las naves en el menú principal.
/// Compatible tanto con GameObjects de Mundo (Transform) como con elementos de UI en Canvas (RectTransform).
/// Se desactiva al iniciarse la secuencia de despegue (Launch).
/// </summary>
public class MenuShipHover : MonoBehaviour
{
  [Header("Parámetros de Flotación")]
  [Tooltip("Amplitud del movimiento arriba/abajo (usar ~0.08 para Mundo o ~18 para UI Canvas)")]
  [SerializeField] private float hoverAmplitude = 18f;

  [Tooltip("Frecuencia o velocidad de la oscilación")]
  [SerializeField] private float hoverFrequency = 2.4f;

  [Tooltip("Desfase de fase inicial para que no oscilen exactamente idénticas")]
  [SerializeField] private float phaseOffset = 0f;

  private RectTransform rectTransform;
  private Vector3 basePosition;
  private Vector2 baseAnchoredPos;
  private bool isFrozen = false;

  void Awake()
  {
    rectTransform = GetComponent<RectTransform>();
  }

  void Start()
  {
    if (rectTransform != null)
    {
      baseAnchoredPos = rectTransform.anchoredPosition;
    }
    else
    {
      basePosition = transform.position;
    }
  }

  void Update()
  {
    if (isFrozen) return;

    float yOffset = Mathf.Sin(Time.unscaledTime * hoverFrequency + phaseOffset) * hoverAmplitude;

    if (rectTransform != null)
    {
      rectTransform.anchoredPosition = new Vector2(baseAnchoredPos.x, baseAnchoredPos.y + yOffset);
    }
    else
    {
      transform.position = new Vector3(basePosition.x, basePosition.y + yOffset, basePosition.z);
    }
  }

  public void Freeze()
  {
    isFrozen = true;
  }
}
