using System;
using System.Collections;
using UnityEngine;
using TMPro;

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
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    IsCountingDown = true;
  }

  void Start()
  {
    // Bloquea el spawner y los controles mientras dura el conteo
    if (blockSpawner != null) blockSpawner.enabled = false;
    if (inputHandler != null) inputHandler.enabled = false;

    StartCoroutine(CountdownRoutine());
  }

  private IEnumerator CountdownRoutine()
  {
    if (countdownText != null)
    {
      countdownText.gameObject.SetActive(true);

      countdownText.text = "3";
      yield return new WaitForSeconds(stepDuration);

      countdownText.text = "2";
      yield return new WaitForSeconds(stepDuration);

      countdownText.text = "1";
      yield return new WaitForSeconds(stepDuration);

      countdownText.text = "GO!";
      yield return new WaitForSeconds(0.4f);

      countdownText.gameObject.SetActive(false);
    }

    // Habilita la caída de bloques y el control del jugador
    if (blockSpawner != null) blockSpawner.enabled = true;
    if (inputHandler != null) inputHandler.enabled = true;

    IsCountingDown = false;
    OnCountdownFinished?.Invoke();

    if (GameManager.Instance != null)
    {
      GameManager.Instance.StartGameTimer();
    }
  }
}