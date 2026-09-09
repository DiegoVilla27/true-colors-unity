using System.Collections;
using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Controla la presentación cinética y dinámica al revivir:
/// 1. Micro-fade cinemático limpio a negro/transparente (0.25s).
/// 2. Punch Zoom y sacudida de impacto en la cámara (CameraShake).
/// 3. Rebote elástico de reactivación en las naves (ShipController.TriggerRevivePop).
/// 4. Destruye cualquier overlay plano, marco o color sólido en la pantalla.
/// </summary>
public class ReviveVFXOverlay : MonoBehaviour
{
  public static ReviveVFXOverlay Instance { get; private set; }

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(this);
      return;
    }
    Instance = this;
    CleanUpLegacyOverlays();
  }

  void Start()
  {
    CleanUpLegacyOverlays();
  }

  void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  /// <summary>
  /// Dispara el efecto cinético puro de revivir (cámara, micro-fade y movimiento elástico).
  /// </summary>
  public void PlayReviveEffect(float duration = 2.0f)
  {
    CleanUpLegacyOverlays();

    // 1. Dinámica de cámara: Punch Zoom elástico + Sacudida de impacto
    if (CameraShake.Instance != null)
    {
      CameraShake.Instance.PunchZoom(0.35f, 0.40f);
      CameraShake.Instance.Shake(0.25f, 0.22f);
    }

    // 2. Movimiento elástico de las naves (spring pop cinemático)
    var ships = FindObjectsByType<ShipController>(FindObjectsSortMode.None);
    for (int i = 0; i < ships.Length; i++)
    {
      if (ships[i] != null)
      {
        ships[i].TriggerRevivePop();
      }
    }

    // 3. Micro-fade cinemático suave y rápido (0.25s) hacia transparencia absoluta
    if (SceneFader.Instance != null)
    {
      SceneFader.Instance.StartCoroutine(SceneFader.Instance.FadeAlpha(0.40f, 0f, 0.25f));
    }

    // 4. Feedback háptico
    HapticFeedback.VibrateCollect();
  }

  /// <summary>
  /// Detiene inmediatamente cualquier efecto residual.
  /// </summary>
  public void StopReviveEffect()
  {
    CleanUpLegacyOverlays();
  }

  public void CleanUpLegacyOverlays()
  {
    Canvas canvas = FindAnyObjectByType<Canvas>();
    if (canvas == null) return;

    // Eliminar overlays con colores planos o texturas residuales
    Transform legacyMaster = canvas.transform.Find("[ReviveVFXOverlay]");
    if (legacyMaster != null) Destroy(legacyMaster.gameObject);

    Transform legacyFlash = canvas.transform.Find("ReviveScreenFlashOverlay");
    if (legacyFlash != null) Destroy(legacyFlash.gameObject);

    Transform legacyVignette = canvas.transform.Find("OverdriveVignetteOverlay");
    if (legacyVignette != null) Destroy(legacyVignette.gameObject);
  }
}
