using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Controla la cuenta regresiva inicial (3... 2... 1... GO!).
/// Utiliza tiempo no escalado (WaitForSecondsRealtime) para garantizar que jamás se congele,
/// incluso si Time.timeScale estaba en 0 tras un Game Over o anuncio.
/// </summary>
public class CountdownController : MonoBehaviour
{
  public static CountdownController Instance { get; private set; }

  [Header("Referencias")]
  [SerializeField] private TextMeshProUGUI countdownText;
  [SerializeField] private BlockSpawner blockSpawner;
  [SerializeField] private InputHandler inputHandler;

  [Header("Duración")]
  [SerializeField] private float stepDuration = 0.8f;

  public bool IsCountingDown { get; private set; } = true;
  public event Action OnCountdownFinished;

  void Awake()
  {
    Instance = this;
    IsCountingDown = true;

    // Garantizar que la escala de tiempo siempre se restaure al inicializar
    Time.timeScale = 1f;

    if (countdownText == null)
    {
      var obj = GameObject.Find("CountdownText");
      if (obj != null) countdownText = obj.GetComponent<TextMeshProUGUI>();
    }

    if (blockSpawner == null) blockSpawner = FindAnyObjectByType<BlockSpawner>();
    if (inputHandler == null) inputHandler = FindAnyObjectByType<InputHandler>();
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

    // Bloquea el spawner y los controles mientras dura el conteo
    if (blockSpawner != null) blockSpawner.enabled = false;
    if (inputHandler != null) inputHandler.enabled = false;

    StartCoroutine(CountdownRoutine());
  }

  private IEnumerator CountdownRoutine()
  {
    // Garantizar escala de tiempo en ejecución
    Time.timeScale = 1f;

    if (countdownText != null)
    {
      countdownText.gameObject.SetActive(true);

      countdownText.text = "3";
      yield return new WaitForSecondsRealtime(stepDuration);

      countdownText.text = "2";
      yield return new WaitForSecondsRealtime(stepDuration);

      countdownText.text = "1";
      yield return new WaitForSecondsRealtime(stepDuration);

      countdownText.text = "GO!";
      yield return new WaitForSecondsRealtime(0.4f);

      countdownText.gameObject.SetActive(false);
    }

    // Habilita la caída de bloques y el control del jugador
    if (blockSpawner != null) blockSpawner.enabled = true;
    if (inputHandler != null) inputHandler.enabled = true;

    IsCountingDown = false;
    Time.timeScale = 1f;
    OnCountdownFinished?.Invoke();

    if (GameManager.Instance != null)
    {
      GameManager.Instance.StartGameTimer();
    }
  }
}