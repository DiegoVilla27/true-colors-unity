using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Controla la presentación visual profesional y cinemática
/// durante el periodo de gracia/inmunidad tras revivir.
/// Incluye:
/// 1. Anillo de onda expansiva holográfica (Shockwave EMP) desde la posición de las naves.
/// 2. Viñeta perimetral de escudo de energía futurista (el centro queda 100% despejado y legible).
/// 3. Resplandor pulsante sutil en los bordes y disipación suave al terminar los 2 segundos.
/// </summary>
public class ReviveVFXOverlay : MonoBehaviour
{
  public static ReviveVFXOverlay Instance { get; private set; }

  private CanvasGroup vignetteGroup;
  private Image vignetteImage;

  private CanvasGroup shockwaveGroup;
  private RectTransform shockwaveRect;

  private Coroutine currentVFXRoutine;

  private static Sprite s_VignetteSprite;
  private static Sprite s_RingSprite;

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(this);
      return;
    }
    Instance = this;
    InitializeVFXHierarchy();
  }

  void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  /// <summary>
  /// Dispara la secuencia visual completa de revivir (onda expansiva + borde de escudo).
  /// </summary>
  public void PlayReviveEffect(float duration = 2.0f)
  {
    InitializeVFXHierarchy();

    if (currentVFXRoutine != null)
    {
      StopCoroutine(currentVFXRoutine);
    }
    currentVFXRoutine = StartCoroutine(ReviveSequenceRoutine(duration));
  }

  /// <summary>
  /// Detiene y oculta inmediatamente cualquier efecto de escudo activo.
  /// </summary>
  public void StopReviveEffect()
  {
    if (currentVFXRoutine != null)
    {
      StopCoroutine(currentVFXRoutine);
      currentVFXRoutine = null;
    }

    if (vignetteGroup != null) vignetteGroup.alpha = 0f;
    if (shockwaveGroup != null) shockwaveGroup.alpha = 0f;
  }

  private IEnumerator ReviveSequenceRoutine(float totalDuration)
  {
    float shockwaveDuration = 0.55f;
    float elapsed = 0f;

    // Resetear posiciones y opacidades
    if (vignetteGroup != null) vignetteGroup.alpha = 0f;
    if (shockwaveGroup != null)
    {
      shockwaveGroup.alpha = 0.9f;
      if (shockwaveRect != null) shockwaveRect.localScale = Vector3.one * 0.15f;
    }

    // Micro-impacto de cámara para dar peso al renacer
    if (CameraShake.Instance != null)
    {
      CameraShake.Instance.Shake(0.18f, 0.12f);
    }
    HapticFeedback.VibrateCollect();

    while (elapsed < totalDuration)
    {
      elapsed += Time.unscaledDeltaTime;

      // 1. Animación de Onda Expansiva (Shockwave EMP) durante los primeros 0.55s
      if (elapsed < shockwaveDuration && shockwaveRect != null && shockwaveGroup != null)
      {
        float tShock = elapsed / shockwaveDuration;
        // Ease-out cúbico para aceleración inicial y frenado suave
        float ease = 1f - Mathf.Pow(1f - tShock, 3f);

        shockwaveRect.localScale = Vector3.Lerp(Vector3.one * 0.2f, Vector3.one * 4.8f, ease);
        shockwaveGroup.alpha = Mathf.Lerp(0.9f, 0f, ease);
      }
      else if (shockwaveGroup != null && shockwaveGroup.alpha > 0f)
      {
        shockwaveGroup.alpha = 0f;
      }

      // 2. Viñeta perimetral de Escudo de Energía (Cybernetic Border)
      if (vignetteGroup != null)
      {
        // Pulso sutil y elegante en el marco exterior (#00E5FF)
        float basePulse = 0.40f + 0.12f * Mathf.Sin(elapsed * 8f);

        // Desvanecimiento suave al entrar (primeros 0.15s)
        if (elapsed < 0.15f)
        {
          vignetteGroup.alpha = Mathf.Lerp(0f, basePulse, elapsed / 0.15f);
        }
        // Desvanecimiento suave al salir (últimos 0.45s)
        else if (elapsed > totalDuration - 0.45f)
        {
          float fadeOutProgress = (totalDuration - elapsed) / 0.45f;
          vignetteGroup.alpha = basePulse * Mathf.Clamp01(fadeOutProgress);
        }
        else
        {
          vignetteGroup.alpha = basePulse;
        }
      }

      yield return null;
    }

    if (vignetteGroup != null) vignetteGroup.alpha = 0f;
    if (shockwaveGroup != null) shockwaveGroup.alpha = 0f;

    currentVFXRoutine = null;
  }

  private void InitializeVFXHierarchy()
  {
    if (vignetteGroup != null && shockwaveGroup != null) return;

    Canvas canvas = FindAnyObjectByType<Canvas>();
    if (canvas == null) return;

    // 1. Contenedor Maestro
    Transform existingMaster = canvas.transform.Find("[ReviveVFXOverlay]");
    GameObject masterObj;
    if (existingMaster != null)
    {
      masterObj = existingMaster.gameObject;
    }
    else
    {
      masterObj = new GameObject("[ReviveVFXOverlay]", typeof(RectTransform));
      masterObj.transform.SetParent(canvas.transform, false);
      masterObj.transform.SetAsFirstSibling(); // Detrás de la UI y los textos para no tapar nada
    }

    RectTransform masterRt = masterObj.GetComponent<RectTransform>();
    masterRt.anchorMin = Vector2.zero;
    masterRt.anchorMax = Vector2.one;
    masterRt.sizeDelta = Vector2.zero;
    masterRt.anchoredPosition = Vector2.zero;

    // 2. Viñeta Perimetral de Escudo (Bordes futuristas, centro 100% transparente)
    Transform existingVignette = masterObj.transform.Find("ShieldBorderVignette");
    GameObject vignetteObj = existingVignette != null ? existingVignette.gameObject : new GameObject("ShieldBorderVignette", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
    vignetteObj.transform.SetParent(masterObj.transform, false);

    RectTransform vigRt = vignetteObj.GetComponent<RectTransform>();
    vigRt.anchorMin = Vector2.zero;
    vigRt.anchorMax = Vector2.one;
    vigRt.sizeDelta = Vector2.zero;
    vigRt.anchoredPosition = Vector2.zero;

    vignetteImage = vignetteObj.GetComponent<Image>();
    vignetteImage.sprite = GetOrCreateShieldVignetteSprite();
    vignetteImage.type = Image.Type.Simple;
    vignetteImage.color = new Color(0.0f, 0.90f, 1.0f, 1.0f); // Electric Cyan Shield
    vignetteImage.raycastTarget = false;

    vignetteGroup = vignetteObj.GetComponent<CanvasGroup>();
    vignetteGroup.alpha = 0f;
    vignetteGroup.blocksRaycasts = false;
    vignetteGroup.interactable = false;

    // 3. Anillo de Onda Expansiva (Shockwave Ring)
    Transform existingShockwave = masterObj.transform.Find("ShockwaveRing");
    GameObject shockwaveObj = existingShockwave != null ? existingShockwave.gameObject : new GameObject("ShockwaveRing", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
    shockwaveObj.transform.SetParent(masterObj.transform, false);

    shockwaveRect = shockwaveObj.GetComponent<RectTransform>();
    shockwaveRect.anchorMin = new Vector2(0.5f, 0.22f); // Centrado en la zona de las naves
    shockwaveRect.anchorMax = new Vector2(0.5f, 0.22f);
    shockwaveRect.sizeDelta = new Vector2(320f, 320f);
    shockwaveRect.anchoredPosition = Vector2.zero;

    Image shockImage = shockwaveObj.GetComponent<Image>();
    shockImage.sprite = GetOrCreateShockwaveRingSprite();
    shockImage.type = Image.Type.Simple;
    shockImage.color = new Color(0.2f, 0.95f, 1.0f, 1.0f); // Radiant Cyan Pulse
    shockImage.raycastTarget = false;

    shockwaveGroup = shockwaveObj.GetComponent<CanvasGroup>();
    shockwaveGroup.alpha = 0f;
    shockwaveGroup.blocksRaycasts = false;
    shockwaveGroup.interactable = false;
  }

  /// <summary>
  /// Genera una viñeta procedural donde el 80% central es 100% transparente
  /// y los bordes exteriores tienen un halo de energía de alta tecnología.
  /// </summary>
  private static Sprite GetOrCreateShieldVignetteSprite()
  {
    if (s_VignetteSprite != null) return s_VignetteSprite;

    int size = 128;
    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Bilinear;

    for (int y = 0; y < size; y++)
    {
      float ny = Mathf.Abs(((y + 0.5f) / size - 0.5f) * 2f);
      for (int x = 0; x < size; x++)
      {
        float nx = Mathf.Abs(((x + 0.5f) / size - 0.5f) * 2f);

        // El centro (78% de ancho y alto) es 100% cristalino y despejado
        float edgeX = Mathf.Clamp01((nx - 0.78f) / 0.22f);
        float edgeY = Mathf.Clamp01((ny - 0.78f) / 0.22f);
        float edgeFactor = Mathf.Max(edgeX, edgeY);

        // Curva suave de energía
        float alpha = Mathf.SmoothStep(0f, 1f, edgeFactor);

        // Resalte de esquinas para dar sensación de visor táctico
        if (nx > 0.75f && ny > 0.75f)
        {
          float cornerBoost = Mathf.Clamp01((nx - 0.75f) * (ny - 0.75f) * 16f);
          alpha = Mathf.Max(alpha, cornerBoost * 0.9f);
        }

        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
      }
    }
    tex.Apply();
    s_VignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    return s_VignetteSprite;
  }

  /// <summary>
  /// Genera un anillo de energía circular con desvanecimiento gaussiano suave.
  /// </summary>
  private static Sprite GetOrCreateShockwaveRingSprite()
  {
    if (s_RingSprite != null) return s_RingSprite;

    int size = 128;
    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Bilinear;

    Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
    float maxRadius = size * 0.5f;

    for (int y = 0; y < size; y++)
    {
      for (int x = 0; x < size; x++)
      {
        float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxRadius;

        // Anillo con pico de intensidad en radio 0.82
        float ring = 0f;
        if (dist <= 1f)
        {
          float delta = dist - 0.82f;
          ring = Mathf.Exp(-Mathf.Pow(delta / 0.09f, 2f));
        }

        tex.SetPixel(x, y, new Color(1f, 1f, 1f, ring));
      }
    }
    tex.Apply();
    s_RingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    return s_RingSprite;
  }
}
