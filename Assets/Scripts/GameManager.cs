using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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

  [Header("Overdrive Visual FX")]
  [Tooltip("Overlay a pantalla completa para el modo Overdrive (se auto-genera si está vacío)")]
  [SerializeField] private CanvasGroup overdriveOverlay;

  [Header("Juice & Animaciones UI")]
  [SerializeField] private float scorePunchMultiplier = 1.25f;
  [SerializeField] private float comboPunchMultiplier = 1.35f;
  [SerializeField] private float punchDuration = 0.14f;

  private Vector3 scoreOriginalScale = Vector3.one;
  private Vector3 comboOriginalScale = Vector3.one;
  private Coroutine scorePunchRoutine;
  private Coroutine comboPunchRoutine;

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

    // Cachear escalas originales para las animaciones de rebote (Punch Scale)
    if (scoreText != null) scoreOriginalScale = scoreText.transform.localScale;
    if (comboText != null) comboOriginalScale = comboText.transform.localScale;

    InitializeOverdriveOverlay();

    UpdateScoreUI();
    UpdateComboUI();
    if (gameOverPanel != null) gameOverPanel.SetActive(false);
  }

  #region Overdrive Fullscreen Visual FX
  private void InitializeOverdriveOverlay()
  {
    if (overdriveOverlay != null)
    {
      overdriveOverlay.alpha = 0f;
      overdriveOverlay.blocksRaycasts = false;
      return;
    }

    // Auto-generación procedural bajo el Canvas de la escena para no requerir configuración manual
    Canvas canvas = FindAnyObjectByType<Canvas>();
    if (canvas == null) return;

    GameObject overlayObj = new GameObject("OverdriveVignetteOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
    overlayObj.transform.SetParent(canvas.transform, false);
    overlayObj.transform.SetAsFirstSibling(); // Detrás del texto pero delante del fondo

    RectTransform rt = overlayObj.GetComponent<RectTransform>();
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.sizeDelta = Vector2.zero;
    rt.anchoredPosition = Vector2.zero;

    Image img = overlayObj.GetComponent<Image>();
    img.sprite = CreateVignetteSprite();
    img.type = Image.Type.Simple;
    img.color = new Color(1f, 0.88f, 0.35f, 1f); // Dorado cálido sutil

    overdriveOverlay = overlayObj.GetComponent<CanvasGroup>();
    overdriveOverlay.alpha = 0f;
    overdriveOverlay.blocksRaycasts = false;
    overdriveOverlay.interactable = false;
  }

  private Sprite CreateVignetteSprite()
  {
    int size = 128;
    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
    tex.wrapMode = TextureWrapMode.Clamp;

    for (int y = 0; y < size; y++)
    {
      float ny = Mathf.Abs(((y + 0.5f) / size - 0.5f) * 2f);
      for (int x = 0; x < size; x++)
      {
        float nx = Mathf.Abs(((x + 0.5f) / size - 0.5f) * 2f);

        // El centro 85% de la pantalla queda 100% despejado y transparente.
        // Solo el 15% más externo del marco tiene un resplandor suave.
        float edgeX = Mathf.Clamp01((nx - 0.85f) / 0.15f);
        float edgeY = Mathf.Clamp01((ny - 0.85f) / 0.15f);
        float edgeFactor = Mathf.Max(edgeX, edgeY);

        float alpha = Mathf.SmoothStep(0f, 1f, edgeFactor);
        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
      }
    }
    tex.Apply();
    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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

  public void AddScore(int basePoints)
  {
    if (isGameOver) return;

    currentStreak++;
    CalculateMultiplier();

    score += basePoints * scoreMultiplier;
    UpdateScoreUI();
    UpdateComboUI();
    TriggerScorePunch();

    if (currentStreak > 1 && (currentStreak % 5 == 0 || scoreMultiplier > 1))
    {
      TriggerComboPunch();
    }

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
    TriggerComboPunch();

    // Convierte inmediatamente los bloques que ya estén cayendo en pantalla
    ConvertExistingBlocksToTargetColors();

    float elapsed = 0f;
    while (elapsed < overdriveDuration)
    {
      elapsed += Time.deltaTime;

      // Pulso sutil y elegante en el marco exterior
      if (overdriveOverlay != null)
      {
        float pulse = 0.22f + 0.10f * Mathf.Sin(elapsed * 6f);
        overdriveOverlay.alpha = pulse;
      }

      yield return null;
    }

    // Suave fade out del overlay dorado
    if (overdriveOverlay != null)
    {
      float fadeDuration = 0.35f;
      float fadeElapsed = 0f;
      float startAlpha = overdriveOverlay.alpha;
      while (fadeElapsed < fadeDuration)
      {
        fadeElapsed += Time.deltaTime;
        overdriveOverlay.alpha = Mathf.Lerp(startAlpha, 0f, fadeElapsed / fadeDuration);
        yield return null;
      }
      overdriveOverlay.alpha = 0f;
    }

    isOverdriveActive = false;
    currentStreak = 0; // Se resetea la racha tras el Overdrive
    scoreMultiplier = 1;
    UpdateComboUI();
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

    // Apagar overlay si estaba encendido
    if (overdriveOverlay != null) overdriveOverlay.alpha = 0f;

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
        finalScoreText.SetText("SCORE: {0}", score);
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
      scoreText.SetText("{0}", score);
    }
  }

  private void UpdateHighScoreUI()
  {
    if (highScoreText != null)
    {
      highScoreText.SetText("BEST: {0}", highScore);
    }
  }

  private void UpdateComboUI()
  {
    if (comboText == null) return;

    if (isOverdriveActive)
    {
      comboText.SetText("<color=#FFD700>🔥 OVERDRIVE x5! 🔥</color>");
    }
    else if (scoreMultiplier > 1)
    {
      comboText.SetText("STREAK {0} (x{1})", (float)currentStreak, (float)scoreMultiplier);
    }
    else
    {
      comboText.SetText(string.Empty);
    }
  }

  public void GoToMainMenu()
  {
    Time.timeScale = 1f; // Siempre restaurar el tiempo antes de cambiar de escena
    SceneManager.LoadScene("MainMenu");
  }
}
