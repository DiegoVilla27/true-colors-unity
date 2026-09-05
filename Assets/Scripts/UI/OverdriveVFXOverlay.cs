using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Controla exclusivamente la presentación visual de pantalla
/// completa durante el modo Overdrive (generación de viñeta procedural y animación de pulso).
/// Escucha eventos de OverdriveController sin acoplarse a la lógica de juego.
/// </summary>
public class OverdriveVFXOverlay : MonoBehaviour
{
  [Header("Visual References")]
  [Tooltip("Overlay a pantalla completa para el modo Overdrive (se auto-genera si está vacío)")]
  [SerializeField] private CanvasGroup overdriveOverlay;

  private Coroutine pulseRoutine;

  void Start()
  {
    InitializeOverlay();

    if (OverdriveController.Instance != null)
    {
      OverdriveController.Instance.OnOverdriveStarted += HandleOverdriveStarted;
      OverdriveController.Instance.OnOverdriveEnded += HandleOverdriveEnded;
    }
  }

  void OnDestroy()
  {
    if (OverdriveController.Instance != null)
    {
      OverdriveController.Instance.OnOverdriveStarted -= HandleOverdriveStarted;
      OverdriveController.Instance.OnOverdriveEnded -= HandleOverdriveEnded;
    }
  }

  private void HandleOverdriveStarted()
  {
    if (pulseRoutine != null) StopCoroutine(pulseRoutine);
    pulseRoutine = StartCoroutine(OverdrivePulseRoutine());
  }

  private void HandleOverdriveEnded()
  {
    if (pulseRoutine != null)
    {
      StopCoroutine(pulseRoutine);
      pulseRoutine = null;
    }
    StartCoroutine(FadeOutRoutine());
  }

  private IEnumerator OverdrivePulseRoutine()
  {
    if (overdriveOverlay == null) yield break;

    float elapsed = 0f;
    while (true)
    {
      elapsed += Time.deltaTime;
      // Pulso sutil y elegante en el marco exterior
      float pulse = 0.22f + 0.10f * Mathf.Sin(elapsed * 6f);
      overdriveOverlay.alpha = pulse;
      yield return null;
    }
  }

  private IEnumerator FadeOutRoutine()
  {
    if (overdriveOverlay == null) yield break;

    float fadeDuration = 0.35f;
    float fadeElapsed = 0f;
    float startAlpha = overdriveOverlay.alpha;

    while (fadeElapsed < fadeDuration)
    {
      fadeElapsed += Time.deltaTime;
      overdriveOverlay.alpha = Mathf.Lerp(startAlpha, 0f, fadeElapsed / fadeDuration);
      yield return null;
    }

    overdriveOverlay.alpha = 0f;
  }

  private void InitializeOverlay()
  {
    if (overdriveOverlay != null)
    {
      overdriveOverlay.alpha = 0f;
      overdriveOverlay.blocksRaycasts = false;
      return;
    }

    // Auto-generación procedural bajo el Canvas de la escena para no requerir configuración manual
    Canvas canvas = FindAnyObjectByType<Canvas>();
    if (canvas == null) return;

    GameObject overlayObj = new GameObject("OverdriveVignetteOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
    overlayObj.transform.SetParent(canvas.transform, false);
    overlayObj.transform.SetAsFirstSibling(); // Detrás del texto pero delante del fondo

    RectTransform rt = overlayObj.GetComponent<RectTransform>();
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.sizeDelta = Vector2.zero;
    rt.anchoredPosition = Vector2.zero;

    Image img = overlayObj.GetComponent<Image>();
    img.sprite = CreateVignetteSprite();
    img.type = Image.Type.Simple;
    img.color = new Color(1f, 0.88f, 0.35f, 1f); // Dorado cálido sutil

    overdriveOverlay = overlayObj.GetComponent<CanvasGroup>();
    overdriveOverlay.alpha = 0f;
    overdriveOverlay.blocksRaycasts = false;
    overdriveOverlay.interactable = false;
  }

  private Sprite CreateVignetteSprite()
  {
    int size = 128;
    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
    tex.wrapMode = TextureWrapMode.Clamp;

    for (int y = 0; y < size; y++)
    {
      float ny = Mathf.Abs(((y + 0.5f) / size - 0.5f) * 2f);
      for (int x = 0; x < size; x++)
      {
        float nx = Mathf.Abs(((x + 0.5f) / size - 0.5f) * 2f);

        // El centro 85% de la pantalla queda 100% despejado y transparente.
        // Solo el 15% más externo del marco tiene un resplandor suave.
        float edgeX = Mathf.Clamp01((nx - 0.85f) / 0.15f);
        float edgeY = Mathf.Clamp01((ny - 0.85f) / 0.15f);
        float edgeFactor = Mathf.Max(edgeX, edgeY);

        float alpha = Mathf.SmoothStep(0f, 1f, edgeFactor);
        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
      }
    }
    tex.Apply();
    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
  }
}
