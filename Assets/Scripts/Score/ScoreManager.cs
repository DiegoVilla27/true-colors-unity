using System;
using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Administra exclusivamente el sistema de puntuación,
/// multiplicadores, cálculo de rachas y persistencia de récords en disco.
/// Totalmente desacoplado de la interfaz de usuario mediante eventos de C#.
/// </summary>
public class ScoreManager : MonoBehaviour
{
  public static ScoreManager Instance { get; private set; }

  [Header("Configuración de Racha & Overdrive")]
  [SerializeField] private int overdriveStreakRequired = 15;

  private const string HIGH_SCORE_KEY = "TrueColors_HighScore";

  private int score = 0;
  private int currentStreak = 0;
  private int scoreMultiplier = 1;
  private int highScore = 0;
  private bool isOverdriveMultiplierActive = false;

  public int CurrentScore => score;
  public int CurrentStreak => currentStreak;
  public int ScoreMultiplier => scoreMultiplier;
  public int HighScore => highScore;

  #region Eventos de Dominio
  public event Action<int> OnScoreChanged;
  public event Action<int, int> OnStreakChanged; // (streak, multiplier)
  public event Action<int> OnHighScoreChanged;
  public event Action OnOverdriveThresholdReached;
  #endregion

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;

    LoadHighScore();
  }

  void Start()
  {
    ResetScore();
  }

  public void ResetScore()
  {
    score = 0;
    currentStreak = 0;
    scoreMultiplier = 1;
    isOverdriveMultiplierActive = false;

    OnScoreChanged?.Invoke(score);
    OnStreakChanged?.Invoke(currentStreak, scoreMultiplier);
    OnHighScoreChanged?.Invoke(highScore);
  }

  public void AddScore(int basePoints)
  {
    currentStreak++;
    CalculateMultiplier();

    score += basePoints * scoreMultiplier;
    OnScoreChanged?.Invoke(score);
    OnStreakChanged?.Invoke(currentStreak, scoreMultiplier);

    // Persistencia y notificación de nuevo récord
    if (score > highScore)
    {
      highScore = score;
      PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
      PlayerPrefs.Save();
      OnHighScoreChanged?.Invoke(highScore);
    }

    // Comprobar umbral para Overdrive
    if (currentStreak >= overdriveStreakRequired && !isOverdriveMultiplierActive)
    {
      OnOverdriveThresholdReached?.Invoke();
    }
  }

  private void CalculateMultiplier()
  {
    if (isOverdriveMultiplierActive)
    {
      scoreMultiplier = 5;
      return;
    }

    if (currentStreak >= 10) scoreMultiplier = 3;
    else if (currentStreak >= 5) scoreMultiplier = 2;
    else scoreMultiplier = 1;
  }

  public void SetOverdriveMultiplier(bool active)
  {
    isOverdriveMultiplierActive = active;

    if (active)
    {
      scoreMultiplier = 5;
    }
    else
    {
      currentStreak = 0; // Se resetea la racha al concluir Overdrive
      scoreMultiplier = 1;
    }

    OnStreakChanged?.Invoke(currentStreak, scoreMultiplier);
  }

  private void LoadHighScore()
  {
    highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
  }
}
