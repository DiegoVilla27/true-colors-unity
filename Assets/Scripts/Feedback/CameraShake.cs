using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
  public static CameraShake Instance { get; private set; }

  private Vector3 initialPosition;
  private Camera targetCamera;
  private float baseOrthoSize = 5.0f;

  private float targetWarpZoomOffset = 0f;
  private float currentWarpZoomOffset = 0f;
  private float currentPunchZoomOffset = 0f;

  private Coroutine shakeRoutine;
  private Coroutine punchRoutine;
  private Coroutine rumbleRoutine;

  private bool isSpeedRumbling = false;
  private float speedRumbleMagnitude = 0.035f;

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    initialPosition = transform.localPosition;
    targetCamera = GetComponent<Camera>();
    if (targetCamera == null) targetCamera = Camera.main;
    if (targetCamera != null) baseOrthoSize = targetCamera.orthographicSize;
  }

  void Update()
  {
    if (targetCamera == null) return;

    // 1. Suavizar transición de Warp Zoom (Overdrive)
    currentWarpZoomOffset = Mathf.MoveTowards(
      currentWarpZoomOffset,
      targetWarpZoomOffset,
      Time.unscaledDeltaTime * 2.2f
    );

    // 2. Aplicar tamaño combinado de lente (base + warp + punch)
    targetCamera.orthographicSize = baseOrthoSize + currentWarpZoomOffset - currentPunchZoomOffset;
  }

  /// <summary>
  /// Sacudida clásica de impacto con caída suave.
  /// </summary>
  public void Shake(float duration = 0.25f, float magnitude = 0.2f)
  {
    if (shakeRoutine != null) StopCoroutine(shakeRoutine);
    shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
  }

  private IEnumerator ShakeRoutine(float duration, float magnitude)
  {
    float elapsed = 0f;

    while (elapsed < duration)
    {
      float damp = 1f - (elapsed / duration);
      float xOffset = Random.Range(-1f, 1f) * magnitude * damp;
      float yOffset = Random.Range(-1f, 1f) * magnitude * damp;

      transform.localPosition = new Vector3(initialPosition.x + xOffset, initialPosition.y + yOffset, initialPosition.z);

      elapsed += Time.unscaledDeltaTime;
      yield return null;
    }

    transform.localPosition = initialPosition;
    shakeRoutine = null;
  }

  /// <summary>
  /// Punch Zoom cinemático: la cámara se acerca y rebota elásticamente hacia su posición.
  /// </summary>
  public void PunchZoom(float punchAmount = 0.25f, float duration = 0.35f)
  {
    if (punchRoutine != null) StopCoroutine(punchRoutine);
    punchRoutine = StartCoroutine(PunchZoomRoutine(punchAmount, duration));
  }

  private IEnumerator PunchZoomRoutine(float punchAmount, float duration)
  {
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / duration);

      // Curva elástica / bounce
      float curve = Mathf.Sin(t * Mathf.PI);
      currentPunchZoomOffset = punchAmount * curve;

      yield return null;
    }

    currentPunchZoomOffset = 0f;
    punchRoutine = null;
  }

  /// <summary>
  /// Activa o desactiva la ampliación de campo de visión (Warp FOV) durante alta velocidad.
  /// </summary>
  public void SetWarpZoom(bool active, float zoomOffset = 0.35f)
  {
    targetWarpZoomOffset = active ? zoomOffset : 0f;
  }

  /// <summary>
  /// Activa o desactiva la vibración continua de motor / velocidad de crucero en Overdrive.
  /// </summary>
  public void SetSpeedRumble(bool active, float magnitude = 0.035f)
  {
    isSpeedRumbling = active;
    speedRumbleMagnitude = magnitude;

    if (active)
    {
      if (rumbleRoutine != null) StopCoroutine(rumbleRoutine);
      rumbleRoutine = StartCoroutine(ContinuousRumbleRoutine());
    }
    else
    {
      if (rumbleRoutine != null)
      {
        StopCoroutine(rumbleRoutine);
        rumbleRoutine = null;
      }
      if (shakeRoutine == null)
      {
        transform.localPosition = initialPosition;
      }
    }
  }

  private IEnumerator ContinuousRumbleRoutine()
  {
    while (isSpeedRumbling)
    {
      if (shakeRoutine == null)
      {
        float xOffset = Random.Range(-1f, 1f) * speedRumbleMagnitude;
        float yOffset = Random.Range(-1f, 1f) * speedRumbleMagnitude;

        transform.localPosition = new Vector3(initialPosition.x + xOffset, initialPosition.y + yOffset, initialPosition.z);
      }
      yield return null;
    }

    if (shakeRoutine == null)
    {
      transform.localPosition = initialPosition;
    }
    rumbleRoutine = null;
  }
}