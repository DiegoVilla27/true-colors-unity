using System.Collections;
using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Controla las dinámicas cinéticas de velocidad durante Overdrive:
/// 1. Ampliación dinámica de campo de visión (Warp Speed Zoom) para sensación real de velocidad.
/// 2. Micro-vibración de alta velocidad en la cámara (Speed Rumble).
/// 3. Destruye cualquier viñeta amarilla o capa de color para mantener la pantalla 100% limpia y profesional.
/// </summary>
public class OverdriveVFXOverlay : MonoBehaviour
{
  void Start()
  {
    CleanUpLegacyOverlays();

    if (OverdriveController.Instance != null)
    {
      OverdriveController.Instance.OnOverdriveStarted += HandleOverdriveStarted;
      OverdriveController.Instance.OnOverdriveEnded += HandleOverdriveEnded;

      if (OverdriveController.Instance.IsOverdriveActive)
      {
        HandleOverdriveStarted();
      }
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
    CleanUpLegacyOverlays();

    // Dinámica cinética de alta velocidad: cámara se amplía y vibra sutilmente
    if (CameraShake.Instance != null)
    {
      CameraShake.Instance.SetWarpZoom(true, 0.35f);
      CameraShake.Instance.SetSpeedRumble(true, 0.035f);
      CameraShake.Instance.PunchZoom(0.2f, 0.25f);
    }
    HapticFeedback.VibrateCollect();
  }

  private void HandleOverdriveEnded()
  {
    // Restaurar cámara suavemente a su estado normal
    if (CameraShake.Instance != null)
    {
      CameraShake.Instance.SetWarpZoom(false);
      CameraShake.Instance.SetSpeedRumble(false);
    }
  }

  private void CleanUpLegacyOverlays()
  {
    // Eliminar cualquier viñeta amarilla o overlay residual del Canvas
    Canvas canvas = FindAnyObjectByType<Canvas>();
    if (canvas == null) return;

    Transform legacyVignette = canvas.transform.Find("OverdriveVignetteOverlay");
    if (legacyVignette != null)
    {
      Destroy(legacyVignette.gameObject);
    }
  }
}
