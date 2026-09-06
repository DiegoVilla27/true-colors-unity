using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Controla el parpadeo y la crepitación senoidal
/// de la llama del propulsor de las naves en el Canvas UI, con soporte de posquemador (Boost).
/// </summary>
public class UIThrusterFlicker : MonoBehaviour
{
  [Header("Escala y Parpadeo")]
  [SerializeField] private Vector3 normalScale = new Vector3(1f, 1f, 1f);
  [SerializeField] private Vector3 boostScale = new Vector3(1.35f, 1.9f, 1f);
  [SerializeField] private float flickerSpeed = 32f;
  [SerializeField] private float flickerIntensity = 0.14f;

  private RectTransform rectTransform;
  private float targetBoost = 0f;
  private float currentBoost = 0f;

  void Awake()
  {
    rectTransform = GetComponent<RectTransform>();
  }

  void Update()
  {
    currentBoost = Mathf.MoveTowards(currentBoost, targetBoost, Time.unscaledDeltaTime * 4.5f);
    float flicker = 1f + Mathf.Sin(Time.unscaledTime * flickerSpeed) * flickerIntensity;
    Vector3 baseScale = Vector3.Lerp(normalScale, boostScale, currentBoost);

    if (rectTransform != null)
    {
      rectTransform.localScale = new Vector3(baseScale.x, baseScale.y * flicker, 1f);
    }
  }

  public void SetBoostActive(bool active)
  {
    targetBoost = active ? 1f : 0f;
  }
}
