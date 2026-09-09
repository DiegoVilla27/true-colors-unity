using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Presentador de la interfaz de usuario (HUD).
/// Actualiza textos TextMeshPro con cero asignaciones de memoria, ejecuta animaciones
/// de rebote (Punch Scale) y gestiona la visibilidad de los paneles modales (Pausa / Game Over)
/// y del indicador visual de Power-Ups (BoostType).
/// </summary>
public class GameHUD : MonoBehaviour
{
  public static GameHUD Instance { get; private set; }

  [Header("UI Text References")]
  [SerializeField] private TextMeshProUGUI scoreText;
  [SerializeField] private TextMeshProUGUI comboText;
  [SerializeField] private TextMeshProUGUI highScoreText;
  [SerializeField] private TextMeshProUGUI finalScoreText;
  [SerializeField] private TextMeshProUGUI rocksText;
  [SerializeField] private TextMeshProUGUI timeText;

  [Header("Power-Up / Boost Display")]
  [Tooltip("Imagen que muestra el Power-Up activo (BoostType)")]
  [SerializeField] private Image boostTypeImage;
  [Tooltip("Sprite de la bomba que se muestra al recogerla")]
  [SerializeField] private Sprite bombSprite;
  [Tooltip("Sprite del reloj que se muestra durante el Slow-Motion")]
  [SerializeField] private Sprite clockSprite;
  [Tooltip("Tiempo en segundos que se muestra el icono de la bomba")]
  [SerializeField] private float bombDisplayDuration = 1.2f;

  [Header("Paneles & Modales")]
  [SerializeField] private GameObject gameOverPanel;
  [SerializeField] private GameObject pausePanel;
  [SerializeField] private GameObject pauseButton;
  [SerializeField] private GameObject settingsPanel;
  [SerializeField] private GameObject revivePanel;
  [SerializeField] private TextMeshProUGUI pauseScoreText;
  [SerializeField] private TextMeshProUGUI gameOverScoreText;
  [SerializeField] private TextMeshProUGUI gameOverRecordText;

  public GameObject GameOverPanel => gameOverPanel;
  public GameObject RevivePanel => revivePanel;

  [Header("Juice & Animaciones")]
  [SerializeField] private float scorePunchMultiplier = 1.25f;
  [SerializeField] private float comboPunchMultiplier = 1.35f;
  [SerializeField] private float punchDuration = 0.14f;

  private Vector3 scoreOriginalScale = Vector3.one;
  private Vector3 comboOriginalScale = Vector3.one;
  private Vector3 boostOriginalScale = Vector3.one;
  private Vector3 rocksOriginalScale = Vector3.one;
  private Coroutine scorePunchRoutine;
  private Coroutine comboPunchRoutine;
  private Coroutine boostPunchRoutine;
  private Coroutine rocksPunchRoutine;
  private Coroutine bombClearRoutine;

  private int lastDisplayedSecond = -1;

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
    if (scoreText != null) scoreOriginalScale = scoreText.transform.localScale;
    if (comboText != null) comboOriginalScale = comboText.transform.localScale;

    EnsureBoostTypeBound();
    ClearBoostImage();
    EnsureRocksTextBound();
    EnsureTimeTextBound();
    EnsurePauseScoreBound();
    EnsureGameOverReferencesBound();
    EnsureRevivePanelBound();

    if (gameOverPanel != null) gameOverPanel.SetActive(false);
    if (pausePanel != null) pausePanel.SetActive(false);
    if (settingsPanel != null) settingsPanel.SetActive(false);
    if (revivePanel != null) revivePanel.SetActive(false);
    if (pauseButton != null) pauseButton.SetActive(true);

    SubscribeEvents();
    UpdateInitialUI();
  }

  void Update()
  {
    UpdateTimeDisplay();
  }

  void OnDestroy()
  {
    UnsubscribeEvents();
  }

  public void Initialize(
    TextMeshProUGUI score,
    TextMeshProUGUI combo,
    TextMeshProUGUI highScore,
    TextMeshProUGUI finalScore,
    GameObject gameOver,
    GameObject pause,
    GameObject pauseBtn,
    GameObject settings = null,
    Image boostType = null,
    Sprite bomb = null,
    Sprite clock = null,
    TextMeshProUGUI rocks = null,
    TextMeshProUGUI time = null,
    TextMeshProUGUI pauseScore = null)
  {
    scoreText = score;
    comboText = combo;
    highScoreText = highScore;
    finalScoreText = finalScore;
    gameOverPanel = gameOver;
    pausePanel = pause;
    pauseButton = pauseBtn;
    if (settings != null) settingsPanel = settings;
    if (pauseScore != null) pauseScoreText = pauseScore;
    if (boostType != null) boostTypeImage = boostType;
    if (bomb != null) bombSprite = bomb;
    if (clock != null) clockSprite = clock;
    if (rocks != null) rocksText = rocks;
    if (time != null) timeText = time;

    if (scoreText != null) scoreOriginalScale = scoreText.transform.localScale;
    if (comboText != null) comboOriginalScale = comboText.transform.localScale;

    EnsureBoostTypeBound();
    ClearBoostImage();
    EnsureRocksTextBound();
    EnsureTimeTextBound();
    EnsurePauseScoreBound();
    AutoWirePauseButtons();

    if (gameOverPanel != null) gameOverPanel.SetActive(false);
    if (pausePanel != null) pausePanel.SetActive(false);
    if (settingsPanel != null) settingsPanel.SetActive(false);
    if (pauseButton != null) pauseButton.SetActive(true);

    UpdateInitialUI();
  }

  private void UpdateInitialUI()
  {
    if (ScoreManager.Instance != null)
    {
      if (highScoreText != null)
      {
        highScoreText.SetText("BEST: {0}", ScoreManager.Instance.HighScore);
      }

      if (scoreText != null)
      {
        scoreText.SetText("{0}", ScoreManager.Instance.CurrentScore);
      }
    }

    if (comboText != null)
    {
      comboText.SetText(string.Empty);
    }

    EnsureRocksTextBound();
    if (rocksText != null)
    {
      int currentRocks = CurrencyManager.Instance != null
        ? CurrencyManager.Instance.TotalRocks
        : PlayerPrefs.GetInt(CurrencyManager.ROCKS_KEY, 0);
      rocksText.SetText("{0}", currentRocks);
    }

    EnsureTimeTextBound();
    if (timeText != null)
    {
      timeText.SetText("00:00");
    }
    lastDisplayedSecond = -1;
  }

  private void UpdateTimeDisplay()
  {
    if (timeText == null)
    {
      EnsureTimeTextBound();
      if (timeText == null) return;
    }

    if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
      return;

    // Si el cronómetro aún no ha empezado (estamos en countdown), mantener en 00:00
    if (!GameManager.Instance.IsTimerRunning)
    {
      if (lastDisplayedSecond != 0)
      {
        lastDisplayedSecond = 0;
        timeText.text = "00:00";
      }
      return;
    }

    int totalSeconds = (int)GameManager.Instance.GameTime;
    if (totalSeconds != lastDisplayedSecond)
    {
      lastDisplayedSecond = totalSeconds;
      int minutes = totalSeconds / 60;
      int seconds = totalSeconds % 60;
      timeText.text = $"{minutes:00}:{seconds:00}";
    }
  }

  private void SubscribeEvents()
  {
    if (ScoreManager.Instance != null)
    {
      ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
      ScoreManager.Instance.OnStreakChanged += HandleStreakChanged;
      ScoreManager.Instance.OnHighScoreChanged += HandleHighScoreChanged;
    }

    if (OverdriveController.Instance != null)
    {
      OverdriveController.Instance.OnOverdriveStarted += HandleOverdriveStarted;
      OverdriveController.Instance.OnOverdriveEnded += HandleOverdriveEnded;
    }

    if (PowerUpManager.Instance != null)
    {
      PowerUpManager.Instance.OnPowerUpActivated += HandlePowerUpActivated;
      PowerUpManager.Instance.OnPowerUpEnded += HandlePowerUpEnded;
    }

    if (GameManager.Instance != null)
    {
      GameManager.Instance.OnStateChanged += HandleGameStateChanged;
    }

    if (CurrencyManager.Instance != null)
    {
      CurrencyManager.Instance.OnTotalRocksChanged += HandleTotalRocksChanged;
    }
  }

  private void UnsubscribeEvents()
  {
    if (ScoreManager.Instance != null)
    {
      ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
      ScoreManager.Instance.OnStreakChanged -= HandleStreakChanged;
      ScoreManager.Instance.OnHighScoreChanged -= HandleHighScoreChanged;
    }

    if (OverdriveController.Instance != null)
    {
      OverdriveController.Instance.OnOverdriveStarted -= HandleOverdriveStarted;
      OverdriveController.Instance.OnOverdriveEnded -= HandleOverdriveEnded;
    }

    if (PowerUpManager.Instance != null)
    {
      PowerUpManager.Instance.OnPowerUpActivated -= HandlePowerUpActivated;
      PowerUpManager.Instance.OnPowerUpEnded -= HandlePowerUpEnded;
    }

    if (GameManager.Instance != null)
    {
      GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
    }

    if (CurrencyManager.Instance != null)
    {
      CurrencyManager.Instance.OnTotalRocksChanged -= HandleTotalRocksChanged;
    }
  }

  #region Handlers de Eventos
  private void HandleScoreChanged(int newScore)
  {
    if (scoreText != null)
    {
      scoreText.SetText("{0}", newScore);
      TriggerScorePunch();
    }
  }

  private void HandleStreakChanged(int streak, int multiplier)
  {
    if (comboText == null) return;

    if (OverdriveController.Instance != null && OverdriveController.Instance.IsOverdriveActive)
    {
      comboText.SetText("<color=#FFD700>OVERDRIVE x5!</color>");
    }
    else if (multiplier > 1)
    {
      comboText.SetText("STREAK {0} (x{1})", (float)streak, (float)multiplier);
      if (streak > 1 && (streak % 5 == 0 || multiplier > 1))
      {
        TriggerComboPunch();
      }
    }
    else
    {
      comboText.SetText(string.Empty);
    }
  }

  private void HandleHighScoreChanged(int newHighScore)
  {
    if (highScoreText != null)
    {
      highScoreText.SetText("BEST: {0}", newHighScore);
    }
  }

  private void HandleTotalRocksChanged(int newTotal)
  {
    EnsureRocksTextBound();
    if (rocksText != null)
    {
      rocksText.SetText("{0}", newTotal);
      TriggerRocksPunch();
    }
  }

  private void HandleOverdriveStarted()
  {
    if (comboText != null)
    {
      comboText.SetText("<color=#FFD700>OVERDRIVE x5!</color>");
      TriggerComboPunch();
    }
  }

  private void HandleOverdriveEnded()
  {
    if (comboText != null)
    {
      comboText.SetText(string.Empty);
    }
  }

  private void HandlePowerUpActivated(PowerUpType type)
  {
    if (comboText != null)
    {
      if (type == PowerUpType.Bomb)
      {
        comboText.SetText("<color=#E040FB>SCREEN WIPE! +25</color>");
        TriggerComboPunch();
      }
      else if (type == PowerUpType.SlowMotion)
      {
        comboText.SetText("<color=#00E5FF>SLOW-MO! 3s</color>");
        TriggerComboPunch();
      }
    }

    if (type == PowerUpType.Bomb)
    {
      if (bombClearRoutine != null) StopCoroutine(bombClearRoutine);
      SetBoostImage(bombSprite);
      bombClearRoutine = StartCoroutine(ClearBombAfterDelay());
    }
    else if (type == PowerUpType.SlowMotion)
    {
      if (bombClearRoutine != null) StopCoroutine(bombClearRoutine);
      SetBoostImage(clockSprite);
    }
  }

  private IEnumerator ClearBombAfterDelay()
  {
    yield return new WaitForSeconds(bombDisplayDuration);
    if (PowerUpManager.Instance == null || !PowerUpManager.Instance.IsSlowMoActive)
    {
      ClearBoostImage();
    }
    bombClearRoutine = null;
  }

  private void HandlePowerUpEnded(PowerUpType type)
  {
    if (type == PowerUpType.SlowMotion)
    {
      ClearBoostImage();
    }

    if (comboText == null) return;

    if (OverdriveController.Instance != null && OverdriveController.Instance.IsOverdriveActive)
    {
      comboText.SetText("<color=#FFD700>OVERDRIVE x5!</color>");
    }
    else if (ScoreManager.Instance != null && ScoreManager.Instance.ScoreMultiplier > 1)
    {
      comboText.SetText("STREAK {0} (x{1})", (float)ScoreManager.Instance.CurrentStreak, (float)ScoreManager.Instance.ScoreMultiplier);
    }
    else
    {
      comboText.SetText(string.Empty);
    }
  }

  private void HandleGameStateChanged(GameManager.GameState newState)
  {
    switch (newState)
    {
      case GameManager.GameState.Playing:
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (revivePanel != null) revivePanel.SetActive(false);
        break;

      case GameManager.GameState.Paused:
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseButton != null) pauseButton.SetActive(false);
        EnsurePauseScoreBound();
        if (pauseScoreText != null && ScoreManager.Instance != null)
        {
          pauseScoreText.SetText("{0}", ScoreManager.Instance.CurrentScore);
        }
        AutoWirePauseButtons();
        break;

      case GameManager.GameState.GameOver:
        ClearBoostImage();
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(false);
        if (gameOverPanel != null)
        {
          gameOverPanel.SetActive(true);
          EnsureGameOverReferencesBound();
          if (gameOverScoreText != null && ScoreManager.Instance != null)
          {
            gameOverScoreText.SetText("{0}", ScoreManager.Instance.CurrentScore);
          }
          else if (finalScoreText != null && ScoreManager.Instance != null)
          {
            finalScoreText.SetText("{0}", ScoreManager.Instance.CurrentScore);
          }

          if (gameOverRecordText != null && ScoreManager.Instance != null)
          {
            gameOverRecordText.SetText("{0}", ScoreManager.Instance.HighScore);
          }

          AutoWireGameOverButtons();
        }
        break;
    }
  }

  public void SetBoostImage(Sprite sprite)
  {
    EnsureBoostTypeBound();

    if (boostTypeImage != null)
    {
      boostTypeImage.sprite = sprite;
      boostTypeImage.enabled = (sprite != null);
      boostTypeImage.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
      if (sprite != null)
      {
        boostTypeImage.preserveAspect = true;
        TriggerBoostPunch();
      }
    }
  }

  public void ClearBoostImage()
  {
    if (boostTypeImage != null)
    {
      boostTypeImage.sprite = null;
      boostTypeImage.enabled = false;
      boostTypeImage.color = new Color(1f, 1f, 1f, 0f);
    }
  }

  private void EnsureBoostTypeBound()
  {
    if (boostTypeImage == null)
    {
      var boostGo = GameObject.Find("BoostType");
      if (boostGo != null)
      {
        boostTypeImage = boostGo.GetComponent<Image>();
      }
    }

    if (boostTypeImage != null && boostTypeImage.transform.localScale != Vector3.zero)
    {
      boostOriginalScale = boostTypeImage.transform.localScale;
    }

#if UNITY_EDITOR
    if (bombSprite == null)
    {
      bombSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/rocks/rock_bomb.png");
    }
    if (clockSprite == null)
    {
      clockSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/rocks/rock_time.png")
                 ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/icons/CLOCK.png");
    }
#endif
  }

  private void EnsureRocksTextBound()
  {
    if (rocksText == null)
    {
      var hudContainer = GameObject.Find("HUD/Container");
      if (hudContainer != null)
      {
        var rocksCol = hudContainer.transform.Find("Rocks") ?? hudContainer.transform.Find("Rock");
        if (rocksCol != null)
        {
          var textChild = rocksCol.Find("Text") ?? rocksCol.Find("Text (TMP)");
          if (textChild != null)
          {
            rocksText = textChild.GetComponent<TextMeshProUGUI>();
          }
        }
      }

      if (rocksText == null)
      {
        var go = GameObject.Find("Rocks/Text") ?? GameObject.Find("Rock/Text");
        if (go != null) rocksText = go.GetComponent<TextMeshProUGUI>();
      }
    }

    if (rocksText != null && rocksText.transform.localScale != Vector3.zero)
    {
      rocksOriginalScale = rocksText.transform.localScale;
    }
  }

  private void EnsureTimeTextBound()
  {
    if (timeText == null)
    {
      var hudContainer = GameObject.Find("HUD/Container");
      if (hudContainer != null)
      {
        var timeCol = hudContainer.transform.Find("Time");
        if (timeCol != null)
        {
          var textChild = timeCol.Find("Text") ?? timeCol.Find("Text (TMP)");
          if (textChild != null)
          {
            timeText = textChild.GetComponent<TextMeshProUGUI>();
          }
        }
      }

      if (timeText == null)
      {
        var go = GameObject.Find("Time/Text");
        if (go != null) timeText = go.GetComponent<TextMeshProUGUI>();
      }
    }
  }

  public void EnsurePauseScoreBound()
  {
    if (pauseScoreText != null) return;

    if (pausePanel != null)
    {
      Transform scoreValTrans = pausePanel.transform.Find("Container/Content/Score/ScoreBackground/ScoreValue");
      if (scoreValTrans != null)
      {
        pauseScoreText = scoreValTrans.GetComponent<TextMeshProUGUI>();
      }
      else
      {
        var allTexts = pausePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
          if (allTexts[i].gameObject.name == "ScoreValue")
          {
            pauseScoreText = allTexts[i];
            break;
          }
        }
      }
    }
  }

  public void EnsureGameOverReferencesBound()
  {
    if (gameOverPanel == null) return;

    if (gameOverScoreText == null)
    {
      Transform scoreValTrans = gameOverPanel.transform.Find("Container/Content/ScoreRow/ScoreBadge/ScoreValue");
      if (scoreValTrans != null)
      {
        gameOverScoreText = scoreValTrans.GetComponent<TextMeshProUGUI>();
      }
      else
      {
        var allTexts = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
          if (allTexts[i].gameObject.name == "ScoreValue" || allTexts[i].gameObject.name == "GameOverScoreValue")
          {
            gameOverScoreText = allTexts[i];
            break;
          }
        }
      }
      if (finalScoreText == null && gameOverScoreText != null)
      {
        finalScoreText = gameOverScoreText;
      }
    }

    if (gameOverRecordText == null)
    {
      Transform recordValTrans = gameOverPanel.transform.Find("Container/Content/RecordRow/RecordBadge/RecordValue");
      if (recordValTrans != null)
      {
        gameOverRecordText = recordValTrans.GetComponent<TextMeshProUGUI>();
      }
      else
      {
        var allTexts = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
          if (allTexts[i].gameObject.name == "RecordValue" || allTexts[i].gameObject.name == "GameOverRecordValue")
          {
            gameOverRecordText = allTexts[i];
            break;
          }
        }
      }
    }
  }

  private void AutoWireGameOverButtons()
  {
    if (gameOverPanel == null) return;

    // Actualizar visibilidad de botón de revivir por anuncios según disponibilidad
    var adsBtn = gameOverPanel.transform.Find("Container/Header/BtnAdsRevive");
    if (adsBtn != null)
    {
      bool canRevive = AdsManager.Instance == null || AdsManager.Instance.CanRevive;
      adsBtn.gameObject.SetActive(canRevive);
    }

    var buttons = gameOverPanel.GetComponentsInChildren<Button>(true);
    for (int i = 0; i < buttons.Length; i++)
    {
      if (buttons[i].GetComponent<UIButtonPressEffect>() == null)
      {
        buttons[i].gameObject.AddComponent<UIButtonPressEffect>();
      }
    }
  }

  private void AutoWirePauseButtons()
  {
    if (pausePanel == null) return;
    var buttons = pausePanel.GetComponentsInChildren<Button>(true);
    for (int i = 0; i < buttons.Length; i++)
    {
      if (buttons[i].GetComponent<UIButtonPressEffect>() == null)
      {
        buttons[i].gameObject.AddComponent<UIButtonPressEffect>();
      }
    }
  }

  private void TriggerBoostPunch()
  {
    if (boostTypeImage == null) return;
    if (boostPunchRoutine != null) StopCoroutine(boostPunchRoutine);
    boostPunchRoutine = StartCoroutine(PunchRoutine(boostTypeImage.transform, boostOriginalScale, comboPunchMultiplier, punchDuration * 1.2f));
  }

  public void EnsureRevivePanelBound()
  {
    if (revivePanel != null) return;
    var canvas = GameObject.Find("Canvas");
    if (canvas != null)
    {
      var trans = canvas.transform.Find("Panels/RevivePanel");
      if (trans != null) revivePanel = trans.gameObject;
    }
  }

#if UNITY_EDITOR
  void OnValidate()
  {
    EnsureBoostTypeBound();
    EnsureRocksTextBound();
    EnsureTimeTextBound();
    EnsurePauseScoreBound();
    EnsureGameOverReferencesBound();
    EnsureRevivePanelBound();
  }
#endif
  #endregion

  #region Punch Scale Juice
  private void TriggerScorePunch()
  {
    if (scoreText == null) return;
    if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
    scorePunchRoutine = StartCoroutine(PunchRoutine(scoreText.transform, scoreOriginalScale, scorePunchMultiplier, punchDuration));
  }

  private void TriggerRocksPunch()
  {
    if (rocksText == null) return;
    if (rocksPunchRoutine != null) StopCoroutine(rocksPunchRoutine);
    rocksPunchRoutine = StartCoroutine(PunchRoutine(rocksText.transform, rocksOriginalScale, scorePunchMultiplier, punchDuration));
  }

  private void TriggerComboPunch()
  {
    if (comboText == null) return;
    if (comboPunchRoutine != null) StopCoroutine(comboPunchRoutine);
    comboPunchRoutine = StartCoroutine(PunchRoutine(comboText.transform, comboOriginalScale, comboPunchMultiplier, punchDuration * 1.2f));
  }

  private IEnumerator PunchRoutine(Transform target, Vector3 baseScale, float multiplier, float duration)
  {
    Vector3 targetScale = baseScale * multiplier;
    float half = duration * 0.5f;
    float elapsed = 0f;

    while (elapsed < half)
    {
      elapsed += Time.unscaledDeltaTime;
      target.localScale = Vector3.Lerp(baseScale, targetScale, elapsed / half);
      yield return null;
    }

    elapsed = 0f;
    while (elapsed < half)
    {
      elapsed += Time.unscaledDeltaTime;
      target.localScale = Vector3.Lerp(targetScale, baseScale, elapsed / half);
      yield return null;
    }

    target.localScale = baseScale;
  }
  #endregion
}
