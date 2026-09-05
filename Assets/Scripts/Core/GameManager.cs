using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Coordinador del estado de partida (Playing, Paused, GameOver)
/// y control del tiempo (Time.timeScale).
/// Actúa además como Facade para garantizar 100% de compatibilidad hacia atrás con los botones
/// de la escena MainGame.unity y las llamadas externas de otras clases.
/// </summary>
public class GameManager : MonoBehaviour
{
  public static GameManager Instance { get; private set; }

  public enum GameState
  {
    Playing,
    Paused,
    GameOver
  }

  [Header("Estado de Partida")]
  [SerializeField] private GameState currentState = GameState.Playing;

  public GameState CurrentState => currentState;
  public bool IsGameOver => currentState == GameState.GameOver;
  public bool IsPaused => currentState == GameState.Paused;
  public bool IsOverdriveActive => OverdriveController.Instance != null && OverdriveController.Instance.IsOverdriveActive;

  public event Action<GameState> OnStateChanged;

  #region Serialized UI Bindings (Inyectados a GameHUD para no perder referencias de Unity)
  [Header("UI References (Inyectados a GameHUD)")]
  [SerializeField] private TextMeshProUGUI scoreText;
  [SerializeField] private TextMeshProUGUI comboText;
  [SerializeField] private TextMeshProUGUI highScoreText;
  [SerializeField] private TextMeshProUGUI finalScoreText;

  [Header("Paneles & Modales")]
  [SerializeField] private GameObject gameOverPanel;
  [SerializeField] private GameObject pausePanel;
  [SerializeField] private GameObject pauseButton;
  #endregion

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;

    // Desbloquear rendimiento fluido en dispositivos móviles
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = 60;

    EnsureModularArchitecture();
  }

  void Start()
  {
    SetState(GameState.Playing);
  }

  /// <summary>
  /// Garantiza que los componentes modulares existan y se inyecten las referencias
  /// de la escena sin que el usuario tenga que reconfigurar nada en Unity.
  /// </summary>
  private void EnsureModularArchitecture()
  {
    if (GetComponent<ScoreManager>() == null)
    {
      gameObject.AddComponent<ScoreManager>();
    }

    if (GetComponent<OverdriveController>() == null)
    {
      gameObject.AddComponent<OverdriveController>();
    }

    if (GetComponent<PowerUpManager>() == null)
    {
      gameObject.AddComponent<PowerUpManager>();
    }

    var hud = GetComponent<GameHUD>() ?? gameObject.AddComponent<GameHUD>();
    hud.Initialize(scoreText, comboText, highScoreText, finalScoreText, gameOverPanel, pausePanel, pauseButton);

    if (GetComponent<OverdriveVFXOverlay>() == null)
    {
      gameObject.AddComponent<OverdriveVFXOverlay>();
    }
  }

  private void SetState(GameState newState)
  {
    currentState = newState;
    OnStateChanged?.Invoke(currentState);
  }

  #region Facade Methods (Compatibilidad con Escena y Scripts Externos)
  public void AddScore(int basePoints)
  {
    if (IsGameOver) return;
    if (ScoreManager.Instance != null)
    {
      ScoreManager.Instance.AddScore(basePoints);
    }
  }

  public void TriggerGameOver()
  {
    if (IsGameOver) return;
    if (IsOverdriveActive) return; // Inmune durante Overdrive

    SetState(GameState.GameOver);

    if (CameraShake.Instance != null)
    {
      CameraShake.Instance.Shake(0.35f, 0.3f);
    }
    HapticFeedback.VibrateGameOver();
    Time.timeScale = 0f;
  }

  public void PauseGame()
  {
    if (IsGameOver) return;
    SetState(GameState.Paused);
    Time.timeScale = 0f;
  }

  public void ResumeGame()
  {
    if (IsGameOver) return;
    SetState(GameState.Playing);
    Time.timeScale = 1f;
  }

  public void TogglePause()
  {
    if (IsPaused) ResumeGame();
    else PauseGame();
  }

  public void RestartGame()
  {
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
  }

  public void GoToMainMenu()
  {
    Time.timeScale = 1f;
    SceneManager.LoadScene("MainMenu");
  }
  #endregion
}
