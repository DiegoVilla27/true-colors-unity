using System;
using UnityEngine;
using UnityEngine.UI;
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
  public bool IsReviveImmune { get; private set; } = false;
  public float GameTime => gameTime;
  public bool IsTimerRunning => isTimerRunning;

  private bool hasPendingDeath = false;
  private Coroutine reviveGraceRoutine;

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
  [SerializeField] private GameObject revivePanel;

  public GameObject SettingsPanel => settingsPanel;
  public GameObject RevivePanel => revivePanel;
  #endregion

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;

    // Restaurar escala de tiempo al iniciar/reiniciar
    Time.timeScale = 1f;

    // Desbloquear rendimiento fluido en dispositivos móviles
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = 60;

    EnsureModularArchitecture();
  }

  void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  void Start()
  {
    Time.timeScale = 1f;
    gameTime = 0f;
    hasPendingDeath = false;
    IsReviveImmune = false;
    AdsManager.Instance?.ResetRevivesForNewGame();
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

    // AdsManager se autoinicializa en su propio GameObject independiente [AdsManager]
    _ = AdsManager.Instance;

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

    if (GetComponent<ReviveVFXOverlay>() == null)
    {
      gameObject.AddComponent<ReviveVFXOverlay>();
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
    if (IsOverdriveActive || IsReviveImmune) return; // Inmune durante Overdrive o gracia de Revive

    if (reviveGraceRoutine != null)
    {
      StopCoroutine(reviveGraceRoutine);
      reviveGraceRoutine = null;
    }
    IsReviveImmune = false;

    if (ReviveVFXOverlay.Instance != null)
    {
      ReviveVFXOverlay.Instance.StopReviveEffect();
    }

    if (GameHUD.Instance != null)
    {
      GameHUD.Instance.ClearAlertText();
    }

    SetState(GameState.GameOver);
    hasPendingDeath = true;

    // Garantizar que la UI esté 100% activa e interactuable al mostrar Game Over
    AdsManager.Instance?.EnsureUIInteractable();

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
    if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;

    AdsManager.Instance?.ResetRevivesForNewGame();

    if (hasPendingDeath && AdsManager.Instance != null)
    {
      hasPendingDeath = false;
      AdsManager.Instance.OnDeath(onAdFinished: () =>
      {
        Time.timeScale = 1f;
        SceneFader.LoadScene(SceneManager.GetActiveScene().buildIndex, 0.25f, 0.35f);
      });
    }
    else
    {
      hasPendingDeath = false;
      Time.timeScale = 1f;
      SceneFader.LoadScene(SceneManager.GetActiveScene().buildIndex, 0.25f, 0.35f);
    }
  }

  public void OpenSettings()
  {
    if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;

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

  public void OpenLeaderboard()
  {
    if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;
    Debug.Log("[GameManager] OpenLeaderboard pulsado desde GameOver.");
  }

  public void OpenReviveModal()
  {
    if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;

    EnsureRevivePanelBound();
    if (revivePanel != null)
    {
      var modal = revivePanel.GetComponent<ReviveModal>();
      if (modal == null) modal = revivePanel.AddComponent<ReviveModal>();
      modal.UpdateDisplay();

      revivePanel.SetActive(true);
    }
    else
    {
      Debug.LogWarning("[GameManager] RevivePanel no encontrado ni asignado.");
    }
  }

  public void CloseReviveModal()
  {
    if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;

    EnsureRevivePanelBound();
    if (revivePanel != null)
    {
      revivePanel.SetActive(false);
    }
  }

  /// <summary>
  /// Solicita revivir viendo un anuncio largo a través de AdsManager (máximo 2 por juego).
  /// </summary>
  public void RequestReviveWithAd()
  {
    if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;

    if (AdsManager.Instance != null)
    {
      AdsManager.Instance.RequestRevive(
        onSuccess: () => ReviveGame(),
        onFailed: () => CloseReviveModal()
      );
    }
    else
    {
      ReviveGame();
    }
  }

  public void ReviveGame()
  {
    // 1. Cancelar cualquier muerte pendiente ya que el jugador revivió
    hasPendingDeath = false;
    AdsManager.Instance?.EnsureUIInteractable();

    // 2. Cerrar modales y paneles
    CloseReviveModal();
    if (gameOverPanel != null)
    {
      gameOverPanel.SetActive(false);
    }
    if (GameHUD.Instance != null && GameHUD.Instance.GameOverPanel != null)
    {
      GameHUD.Instance.GameOverPanel.SetActive(false);
    }

    // 3. Limpiar todos los obstáculos y bloques activos en pantalla
    var activeBlocks = CollectibleBlock.ActiveBlocks;
    for (int i = activeBlocks.Count - 1; i >= 0; i--)
    {
      if (i < activeBlocks.Count && activeBlocks[i] != null)
      {
        activeBlocks[i].Recycle();
      }
    }

    // 4. Reactivar spawner con retraso de gracia de 2 segundos
    var spawner = FindAnyObjectByType<BlockSpawner>();
    if (spawner != null)
    {
      spawner.enabled = true;
      spawner.ResetSpawnDelay(2.0f);
    }

    var input = FindAnyObjectByType<InputHandler>();
    if (input != null) input.enabled = true;

    // 5. Iniciar inmunidad temporal y parpadeo visual de 2 segundos
    if (reviveGraceRoutine != null) StopCoroutine(reviveGraceRoutine);
    reviveGraceRoutine = StartCoroutine(ReviveGraceRoutine(2.0f));

    // 6. Restaurar estado de juego y tiempo
    SetState(GameState.Playing);
    Time.timeScale = 1f;
    isTimerRunning = true;

    // 7. Feedback háptico y cámara
    if (CameraShake.Instance != null)
    {
      CameraShake.Instance.Shake(0.2f, 0.15f);
    }
    HapticFeedback.VibrateCollect();
  }

  private System.Collections.IEnumerator ReviveGraceRoutine(float duration)
  {
    IsReviveImmune = true;

    // 1. Activar efectos visuales cinemáticos de alta gama (onda expansiva + marco perimetral de escudo)
    if (ReviveVFXOverlay.Instance != null)
    {
      ReviveVFXOverlay.Instance.PlayReviveEffect(duration);
    }

    // 2. Notificación limpia y elegante en el HUD
    if (GameHUD.Instance != null)
    {
      GameHUD.Instance.ShowAlertText("<color=#FFFFFF>SHIELD ACTIVE (2s)</color>");
    }

    float elapsed = 0f;
    while (elapsed < duration)
    {
      elapsed += Time.unscaledDeltaTime;
      if (elapsed >= 1.0f && GameHUD.Instance != null)
      {
        GameHUD.Instance.ShowAlertText("<color=#FFFFFF>SHIELD ACTIVE (1s)</color>", false);
      }
      yield return null;
    }

    if (GameHUD.Instance != null)
    {
      GameHUD.Instance.ClearAlertText();
    }

    IsReviveImmune = false;
    reviveGraceRoutine = null;
  }

  private void EnsureRevivePanelBound()
  {
    if (revivePanel != null) return;
    var canvas = GameObject.Find("Canvas");
    if (canvas != null)
    {
      var trans = canvas.transform.Find("Panels/RevivePanel");
      if (trans != null) revivePanel = trans.gameObject;
    }
  }

  public void GoToMainMenu()
  {
    if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;

    // 1. Detener el estado de la partida
    isTimerRunning = false;

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

    // 4. Si la partida concluyó en derrota definitiva, procesar muerte para la regla de 5 muertes
    if (hasPendingDeath && AdsManager.Instance != null)
    {
      hasPendingDeath = false;
      AdsManager.Instance.OnDeath(onAdFinished: () =>
      {
        Time.timeScale = 1f;
        SceneFader.LoadScene("MainMenu", 0.25f, 0.35f);
      });
    }
    else
    {
      hasPendingDeath = false;
      Time.timeScale = 1f;
      SceneFader.LoadScene("MainMenu", 0.25f, 0.35f);
    }
  }
  #endregion
}
