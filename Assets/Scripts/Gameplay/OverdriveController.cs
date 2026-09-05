using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Administra la duración, estado de inmunidad y transmutación
/// de bloques activos durante el modo Overdrive. Emite eventos para que los sistemas visuales
/// y de interfaz reaccionen de forma totalmente desacoplada.
/// </summary>
public class OverdriveController : MonoBehaviour
{
  public static OverdriveController Instance { get; private set; }

  [Header("Configuración de Overdrive")]
  [SerializeField] private float overdriveDuration = 6f;

  private bool isOverdriveActive = false;

  public bool IsOverdriveActive => isOverdriveActive;
  public float OverdriveDuration => overdriveDuration;

  #region Eventos de Dominio
  public event Action OnOverdriveStarted;
  public event Action OnOverdriveEnded;
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

  void Start()
  {
    if (ScoreManager.Instance != null)
    {
      ScoreManager.Instance.OnOverdriveThresholdReached += TriggerOverdrive;
    }
  }

  void OnDestroy()
  {
    if (ScoreManager.Instance != null)
    {
      ScoreManager.Instance.OnOverdriveThresholdReached -= TriggerOverdrive;
    }
  }

  public void TriggerOverdrive()
  {
    if (isOverdriveActive) return;
    StartCoroutine(OverdriveRoutine());
  }

  private IEnumerator OverdriveRoutine()
  {
    isOverdriveActive = true;

    if (ScoreManager.Instance != null)
    {
      ScoreManager.Instance.SetOverdriveMultiplier(true);
    }

    OnOverdriveStarted?.Invoke();

    // Convierte inmediatamente los bloques que ya estén cayendo en pantalla
    ConvertExistingBlocksToTargetColors();

    yield return new WaitForSeconds(overdriveDuration);

    isOverdriveActive = false;

    if (ScoreManager.Instance != null)
    {
      ScoreManager.Instance.SetOverdriveMultiplier(false);
    }

    OnOverdriveEnded?.Invoke();
  }

  private void ConvertExistingBlocksToTargetColors()
  {
    // Cero allocations: itera sobre la lista interna de bloques activos sin recorrer la jerarquía
    var activeBlocks = CollectibleBlock.ActiveBlocks;
    for (int i = 0; i < activeBlocks.Count; i++)
    {
      var block = activeBlocks[i];
      if (block != null && block.gameObject.activeInHierarchy)
      {
        GameColor target = block.transform.position.x < 0 ? GameColor.Red : GameColor.Blue;
        block.ForceColor(target);
      }
    }
  }
}
