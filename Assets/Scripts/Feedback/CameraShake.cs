using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
  public static CameraShake Instance { get; private set; }

  private Vector3 initialPosition;
  private Coroutine shakeRoutine;

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    initialPosition = transform.localPosition;
  }

  /// <summary>
  /// Sacudida clásica de impacto con caída suave sin alterar zoom ni escala del mundo.
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
}