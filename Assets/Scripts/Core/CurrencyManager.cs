using System;
using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Administra la economía del juego (rocas recolectadas/destruidas).
/// Mantiene la persistencia en disco (PlayerPrefs / DB local) para su uso como meta-moneda
/// en la tienda de compras, y expone eventos desacoplados para la interfaz de usuario.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
  public static CurrencyManager Instance { get; private set; }

  public const string ROCKS_KEY = "TrueColors_TotalRocks";

  [Header("Estado de Economía")]
  [SerializeField] private int totalRocks = 0;
  [SerializeField] private int sessionRocks = 0;

  public int TotalRocks => totalRocks;
  public int SessionRocks => sessionRocks;

  #region Eventos de Dominio
  public event Action<int> OnTotalRocksChanged;
  public event Action<int> OnSessionRocksChanged;
  #endregion

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;

    LoadCurrency();
  }

  void Start()
  {
    sessionRocks = 0;
    OnSessionRocksChanged?.Invoke(sessionRocks);
    OnTotalRocksChanged?.Invoke(totalRocks);
  }

  /// <summary>
  /// Carga el saldo de rocas persistido en disco.
  /// </summary>
  private void LoadCurrency()
  {
    totalRocks = PlayerPrefs.GetInt(ROCKS_KEY, 0);
  }

  /// <summary>
  /// Guarda el saldo total en disco.
  /// </summary>
  private void SaveCurrency()
  {
    PlayerPrefs.SetInt(ROCKS_KEY, totalRocks);
    PlayerPrefs.Save();
  }

  /// <summary>
  /// Suma rocas destruidas a la sesión y al saldo global acumulado en DB.
  /// </summary>
  public void AddRocks(int amount = 1)
  {
    if (amount <= 0) return;

    sessionRocks += amount;
    totalRocks += amount;

    SaveCurrency();

    OnSessionRocksChanged?.Invoke(sessionRocks);
    OnTotalRocksChanged?.Invoke(totalRocks);
  }

  /// <summary>
  /// Intenta consumir rocas para compras en la tienda futura si el saldo es suficiente.
  /// </summary>
  public bool TrySpendRocks(int amount)
  {
    if (amount <= 0) return false;
    if (totalRocks < amount) return false;

    totalRocks -= amount;
    SaveCurrency();

    OnTotalRocksChanged?.Invoke(totalRocks);
    return true;
  }

  /// <summary>
  /// Verifica si el usuario cuenta con saldo suficiente.
  /// </summary>
  public bool HasEnoughRocks(int amount) => totalRocks >= amount;

  /// <summary>
  /// Resetea el contador de la partida actual sin afectar el saldo guardado en disco.
  /// </summary>
  public void ResetSession()
  {
    sessionRocks = 0;
    OnSessionRocksChanged?.Invoke(sessionRocks);
  }

  #region Debug / Editor Tools
  [ContextMenu("Añadir 50 Rocas")]
  public void DebugAdd50Rocks() => AddRocks(50);

  [ContextMenu("Resetear Saldo de Rocas")]
  public void DebugResetRocks()
  {
    totalRocks = 0;
    sessionRocks = 0;
    SaveCurrency();
    OnTotalRocksChanged?.Invoke(totalRocks);
    OnSessionRocksChanged?.Invoke(sessionRocks);
  }
  #endregion
}
