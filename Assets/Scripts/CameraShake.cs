using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
  public static CameraShake Instance { get; private set; }

  private Vector3 initialPosition;

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

  public void Shake(float duration = 0.25f, float magnitude = 0.2f)
  {
    // Detener corrutinas previas para evitar desajustes de posición
    StopAllCoroutines();
    StartCoroutine(ShakeRoutine(duration, magnitude));
  }

  private IEnumerator ShakeRoutine(float duration, float magnitude)
  {
    float elapsed = 0f;

    // Usamos tiempo real sin escalar para que funcione incluso si Time.timeScale = 0
    while (elapsed < duration)
    {
      float xOffset = Random.Range(-1f, 1f) * magnitude;
      float yOffset = Random.Range(-1f, 1f) * magnitude;

      transform.localPosition = new Vector3(initialPosition.x + xOffset, initialPosition.y + yOffset, initialPosition.z);

      elapsed += Time.unscaledDeltaTime;
      yield return null;
    }

    transform.localPosition = initialPosition;
  }
}