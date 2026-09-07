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
  [SerializeField] private float gameTime = 0f;
  private bool isTimerRunning = false;

  public GameState CurrentState => currentState;
  public bool IsGameOver => currentState == GameState.GameOver;
  public bool IsPaused => currentState == GameState.Paused;
  public bool IsOverdriveActive => OverdriveController.Instance != null && OverdriveController.Instance.IsOverdriveActive;
  public float GameTime => gameTime;
  public bool IsTimerRunning => isTimerRunning;

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
  [SerializeField] private GameObject settingsPanel;

  public GameObject SettingsPanel => settingsPanel;
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
    gameTime = 0f;
    SetState(GameState.Playing);

    var countdown = GetComponent<CountdownController>() ?? FindAnyObjectByType<CountdownController>();
    if (countdown == null || !countdown.enabled || !countdown.gameObject.activeInHierarchy)
    {
      StartGameTimer();
    }
    else
    {
      isTimerRunning = false;
    }
  }

  void Update()
  {
    if (currentState == GameState.Playing && isTimerRunning)
    {
      gameTime += Time.deltaTime;
    }
  }

  /// <summary>
  /// Inicia el conteo del tiempo de la partida una vez concluido el Countdown.
  /// </summary>
  public void StartGameTimer()
  {
    gameTime = 0f;
    isTimerRunning = true;
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

    if (GetComponent<CurrencyManager>() == null)
    {
      gameObject.AddComponent<CurrencyManager>();
    }

    if (settingsPanel == null)
    {
      var found = GameObject.Find("SettingsPanel");
      if (found != null) settingsPanel = found;
    }

    var hud = GetComponent<GameHUD>();
    if (hud == null)
    {
      hud = gameObject.AddComponent<GameHUD>();
    }
    hud.Initialize(scoreText, comboText, highScoreText, finalScoreText, gameOverPanel, pausePanel, pauseButton, settingsPanel);

    if (GetComponent<OverdriveVFXOverlay>() == null)
    {
      gameObject.AddComponent<OverdriveVFXOverlay>();
    }

    if (GetComponent<LaneManager>() == null && FindAnyObjectByType<LaneManager>() == null)
    {
      gameObject.AddComponent<LaneManager>();
    }

    var ships = FindObjectsByType<ShipController>();
    for (int i = 0; i < ships.Length; i++)
    {
      if (ships[i].GetComponent<ShipThruster>() == null)
      {
        ships[i].gameObject.AddComponent<ShipThruster>();
      }
    }

    Camera mainCam = Camera.main;
    if (mainCam == null) mainCam = FindAnyObjectByType<Camera>();
    if (mainCam != null && mainCam.GetComponent<SpaceStarfield>() == null)
    {
      mainCam.gameObject.AddComponent<SpaceStarfield>();
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

  public void AddDestroyedRock(int amount = 1)
  {
    if (IsGameOver) return;
    if (CurrencyManager.Instance != null)
    {
      CurrencyManager.Instance.AddRocks(amount);
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
    SceneFader.LoadScene(SceneManager.GetActiveScene().buildIndex, 0.25f, 0.35f);
  }

  public void OpenSettings()
  {
    if (settingsPanel != null)
    {
      settingsPanel.SetActive(true);
    }
    else
    {
      Debug.Log("[GameManager] SettingsPanel abierto.");
    }
  }

  public void CloseSettings()
  {
    if (settingsPanel != null)
    {
      settingsPanel.SetActive(false);
    }
  }

  public void GoToMainMenu()
  {
    // 1. Detener el estado de la partida y restaurar escala de tiempo
    isTimerRunning = false;
    Time.timeScale = 1f;

    // 2. Detener inmediatamente el Spawner y la entrada del jugador
    var spawner = FindAnyObjectByType<BlockSpawner>();
    if (spawner != null) spawner.enabled = false;

    var input = FindAnyObjectByType<InputHandler>();
    if (input != null) input.enabled = false;

    // 3. Detener corrutinas y efectos en ejecución
    StopAllCoroutines();

    if (OverdriveController.Instance != null)
    {
      OverdriveController.Instance.StopAllCoroutines();
    }
    if (PowerUpManager.Instance != null)
    {
      PowerUpManager.Instance.StopAllCoroutines();
    }

    // 4. Transición suave y limpia hacia el menú principal
    SceneFader.LoadScene("MainMenu", 0.25f, 0.35f);
  }
  #endregion
}
