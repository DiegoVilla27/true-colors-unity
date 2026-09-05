using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
  public static GameManager Instance { get; private set; }

  [Header("UI References")]
  [SerializeField] private TextMeshProUGUI scoreText;
  [SerializeField] private TextMeshProUGUI comboText;
  [SerializeField] private GameObject gameOverPanel;
  [SerializeField] private TextMeshProUGUI finalScoreText;

  [Header("Configuración de Combo & Overdrive")]
  [SerializeField] private int overdriveStreakRequired = 15;
  [SerializeField] private float overdriveDuration = 6f;

  private int score = 0;
  private int currentStreak = 0;
  private int scoreMultiplier = 1;
  private bool isGameOver = false;
  private bool isOverdriveActive = false;

  public bool IsGameOver => isGameOver;
  public bool IsOverdriveActive => isOverdriveActive;

  [Header("High Score UI")]
  [SerializeField] private TextMeshProUGUI highScoreText;

  private const string HIGH_SCORE_KEY = "TrueColors_HighScore";
  private int highScore = 0;

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
  }

  void Start()
  {
    highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    UpdateHighScoreUI();

    score = 0;
    currentStreak = 0;
    scoreMultiplier = 1;
    isGameOver = false;
    isOverdriveActive = false;

    UpdateScoreUI();
    UpdateComboUI();
    if (gameOverPanel != null) gameOverPanel.SetActive(false);
  }

  public void AddScore(int basePoints)
  {
    if (isGameOver) return;

    currentStreak++;
    CalculateMultiplier();

    score += basePoints * scoreMultiplier;
    UpdateScoreUI();
    UpdateComboUI();

    // Guardar si superó el récord
    if (score > highScore)
    {
      highScore = score;
      PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
      PlayerPrefs.Save();
      UpdateHighScoreUI();
    }

    if (currentStreak >= overdriveStreakRequired && !isOverdriveActive)
    {
      StartCoroutine(ActivateOverdriveRoutine());
    }
  }

  private void CalculateMultiplier()
  {
    if (isOverdriveActive)
    {
      scoreMultiplier = 5;
      return;
    }

    if (currentStreak >= 10) scoreMultiplier = 3;
    else if (currentStreak >= 5) scoreMultiplier = 2;
    else scoreMultiplier = 1;
  }

  private IEnumerator ActivateOverdriveRoutine()
  {
    isOverdriveActive = true;
    scoreMultiplier = 5;
    UpdateComboUI();

    // Convierte inmediatamente los bloques que ya estén cayendo en pantalla
    ConvertExistingBlocksToTargetColors();

    yield return new WaitForSeconds(overdriveDuration);

    isOverdriveActive = false;
    currentStreak = 0; // Se resetea la racha tras el Overdrive
    scoreMultiplier = 1;
    UpdateComboUI();
  }

  private void ConvertExistingBlocksToTargetColors()
  {
    CollectibleBlock[] activeBlocks = FindObjectsByType<CollectibleBlock>(FindObjectsSortMode.None);
    foreach (var block in activeBlocks)
    {
      if (block.gameObject.activeInHierarchy)
      {
        // Si está en la mitad izquierda, conviértelo en Rojo; si está en la derecha, en Azul
        GameColor target = block.transform.position.x < 0 ? GameColor.Red : GameColor.Blue;
        block.ForceColor(target);
      }
    }
  }

  public void TriggerGameOver()
  {
    if (isGameOver) return;

    // Si estás en Overdrive, eres inmune a Game Over
    if (isOverdriveActive) return;

    isGameOver = true;

    if (CameraShake.Instance != null)
    {
      CameraShake.Instance.Shake(0.35f, 0.3f);
    }
    HapticFeedback.VibrateGameOver();
    Time.timeScale = 0f;

    if (gameOverPanel != null)
    {
      gameOverPanel.SetActive(true);
      if (finalScoreText != null)
      {
        finalScoreText.text = $"SCORE: {score}";
      }
    }
  }

  public void RestartGame()
  {
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
  }

  private void UpdateScoreUI()
  {
    if (scoreText != null)
    {
      scoreText.text = score.ToString();
    }
  }

  private void UpdateHighScoreUI()
  {
    if (highScoreText != null)
    {
      highScoreText.text = $"BEST: {highScore}";
    }
  }

  private void UpdateComboUI()
  {
    if (comboText == null) return;

    if (isOverdriveActive)
    {
      comboText.text = "<color=#FFD700>OVERDRIVE x5!</color>";
    }
    else if (scoreMultiplier > 1)
    {
      comboText.text = $"STREAK {currentStreak} (x{scoreMultiplier})";
    }
    else
    {
      comboText.text = "";
    }
  }

  public void GoToMainMenu()
  {
    Time.timeScale = 1f; // Siempre restaurar el tiempo antes de cambiar de escena
    SceneManager.LoadScene("MainMenu");
  }
}
