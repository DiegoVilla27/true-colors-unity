using System;
using System.Collections;
using UnityEngine;

public enum PowerUpType
{
  None,
  Bomb,
  SlowMotion
}

/// <summary>
/// Responsabilidad Única (SRP): Administra el ciclo de vida, efectos y temporizadores
/// de los Power-Ups activos (Bomba de pantalla y Slow-Motion).
/// Expone un multiplicador de velocidad global para desacoplar el movimiento de los bloques
/// del tiempo general del motor (manteniendo la agilidad intacta en el jugador).
/// </summary>
public class PowerUpManager : MonoBehaviour
{
  public static PowerUpManager Instance { get; private set; }

  [Header("Configuración Slow-Motion")]
  [Tooltip("Duración en segundos del efecto Slow-Mo")]
  [SerializeField] private float slowMoDuration = 3.0f;
  [Tooltip("Multiplicador de velocidad para los bloques (0.5 = 50% de velocidad)")]
  [SerializeField] private float slowMoMultiplier = 0.5f;

  [Header("Puntuación & Impacto")]
  [SerializeField] private int bombScoreBonus = 25;
  [SerializeField] private int slowMoScoreBonus = 15;
  [SerializeField] private float bombShakeDuration = 0.35f;
  [SerializeField] private float bombShakeMagnitude = 0.25f;

  private float currentSpeedMultiplier = 1.0f;
  private bool isSlowMoActive = false;
  private float slowMoTimer = 0f;
  private Coroutine slowMoCoroutine;

  public float GlobalSpeedMultiplier => currentSpeedMultiplier;
  public bool IsSlowMoActive => isSlowMoActive;
  public float SlowMoDuration => slowMoDuration;

  #region Eventos de Dominio
  public event Action<PowerUpType> OnPowerUpActivated;
  public event Action<PowerUpType> OnPowerUpEnded;
  #endregion

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
  }

  /// <summary>
  /// Activa el Bloque Bomba / Limpiador:
  /// Elimina instantáneamente todos los obstáculos peligrosos activos en pantalla,
  /// sacude la cámara y otorga puntos de bonificación.
  /// </summary>
  public void ActivateBomb()
  {
    if (CameraShake.Instance != null)
    {
      CameraShake.Instance.Shake(bombShakeDuration, bombShakeMagnitude);
    }

    if (GameManager.Instance != null)
    {
      GameManager.Instance.AddScore(bombScoreBonus);
    }

    // Iteración inversa segura sobre los bloques activos
    var activeBlocks = CollectibleBlock.ActiveBlocks;
    for (int i = activeBlocks.Count - 1; i >= 0; i--)
    {
      if (i >= activeBlocks.Count) continue;
      var block = activeBlocks[i];
      if (block == null || !block.gameObject.activeInHierarchy) continue;

      // Limpia únicamente los obstáculos que representan una amenaza
      if (block.IsHazard())
      {
        block.WipeByBomb();
      }
    }

    OnPowerUpActivated?.Invoke(PowerUpType.Bomb);
  }

  /// <summary>
  /// Activa el Bloque Reloj / Slow-Motion:
  /// Ralentiza la caída de los bloques durante el tiempo configurado.
  /// Si ya estaba activo, refresca el temporizador.
  /// </summary>
  public void ActivateSlowMotion()
  {
    if (GameManager.Instance != null)
    {
      GameManager.Instance.AddScore(slowMoScoreBonus);
    }

    slowMoTimer = slowMoDuration;

    if (!isSlowMoActive)
    {
      if (slowMoCoroutine != null) StopCoroutine(slowMoCoroutine);
      slowMoCoroutine = StartCoroutine(SlowMoRoutine());
    }
  }

  private IEnumerator SlowMoRoutine()
  {
    isSlowMoActive = true;
    currentSpeedMultiplier = slowMoMultiplier;

    OnPowerUpActivated?.Invoke(PowerUpType.SlowMotion);

    while (slowMoTimer > 0f)
    {
      slowMoTimer -= Time.deltaTime;
      yield return null;
    }

    currentSpeedMultiplier = 1.0f;
    isSlowMoActive = false;

    OnPowerUpEnded?.Invoke(PowerUpType.SlowMotion);
  }

  void OnDestroy()
  {
    currentSpeedMultiplier = 1.0f;
    isSlowMoActive = false;
  }
}
