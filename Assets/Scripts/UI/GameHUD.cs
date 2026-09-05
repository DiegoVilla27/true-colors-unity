using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Presentador de la interfaz de usuario (HUD).
/// Actualiza textos TextMeshPro con cero asignaciones de memoria, ejecuta animaciones
/// de rebote (Punch Scale) y gestiona la visibilidad de los paneles modales (Pausa / Game Over).
/// </summary>
public class GameHUD : MonoBehaviour
{
  public static GameHUD Instance { get; private set; }

  [Header("UI Text References")]
  [SerializeField] private TextMeshProUGUI scoreText;
  [SerializeField] private TextMeshProUGUI comboText;
  [SerializeField] private TextMeshProUGUI highScoreText;
  [SerializeField] private TextMeshProUGUI finalScoreText;

  [Header("Paneles & Modales")]
  [SerializeField] private GameObject gameOverPanel;
  [SerializeField] private GameObject pausePanel;
  [SerializeField] private GameObject pauseButton;

  [Header("Juice & Animaciones")]
  [SerializeField] private float scorePunchMultiplier = 1.25f;
  [SerializeField] private float comboPunchMultiplier = 1.35f;
  [SerializeField] private float punchDuration = 0.14f;

  private Vector3 scoreOriginalScale = Vector3.one;
  private Vector3 comboOriginalScale = Vector3.one;
  private Coroutine scorePunchRoutine;
  private Coroutine comboPunchRoutine;

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

    if (gameOverPanel != null) gameOverPanel.SetActive(false);
    if (pausePanel != null) pausePanel.SetActive(false);
    if (pauseButton != null) pauseButton.SetActive(true);

    SubscribeEvents();
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
    GameObject pauseBtn)
  {
    scoreText = score;
    comboText = combo;
    highScoreText = highScore;
    finalScoreText = finalScore;
    gameOverPanel = gameOver;
    pausePanel = pause;
    pauseButton = pauseBtn;

    if (scoreText != null) scoreOriginalScale = scoreText.transform.localScale;
    if (comboText != null) comboOriginalScale = comboText.transform.localScale;

    if (gameOverPanel != null) gameOverPanel.SetActive(false);
    if (pausePanel != null) pausePanel.SetActive(false);
    if (pauseButton != null) pauseButton.SetActive(true);
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

    if (GameManager.Instance != null)
    {
      GameManager.Instance.OnStateChanged += HandleGameStateChanged;
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

    if (GameManager.Instance != null)
    {
      GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
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
      comboText.SetText("<color=#FFD700>🔥 OVERDRIVE x5! 🔥</color>");
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

  private void HandleOverdriveStarted()
  {
    if (comboText != null)
    {
      comboText.SetText("<color=#FFD700>🔥 OVERDRIVE x5! 🔥</color>");
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

  private void HandleGameStateChanged(GameManager.GameState newState)
  {
    switch (newState)
    {
      case GameManager.GameState.Playing:
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        break;

      case GameManager.GameState.Paused:
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseButton != null) pauseButton.SetActive(false);
        break;

      case GameManager.GameState.GameOver:
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(false);
        if (gameOverPanel != null)
        {
          gameOverPanel.SetActive(true);
          if (finalScoreText != null && ScoreManager.Instance != null)
          {
            finalScoreText.SetText("SCORE: {0}", ScoreManager.Instance.CurrentScore);
          }
        }
        break;
    }
  }
  #endregion

  #region Punch Scale Juice
  private void TriggerScorePunch()
  {
    if (scoreText == null) return;
    if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
    scorePunchRoutine = StartCoroutine(PunchRoutine(scoreText.transform, scoreOriginalScale, scorePunchMultiplier, punchDuration));
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
