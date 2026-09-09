using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
  [Header("Prefab")]
  [SerializeField] private GameObject blockPrefab;

  [Header("Configuración de Carriles")]
  private static readonly float[] AllLanes = new float[] { -2.1f, -1.4f, -0.7f, 0.7f, 1.4f, 2.1f };

  // Arrays estáticos reutilizables para evitar reservas en Heap por cada spawn
  private static readonly GameColor[] LeftObstacles = new GameColor[] { GameColor.Yellow, GameColor.Green, GameColor.Blue };
  private static readonly GameColor[] RightObstacles = new GameColor[] { GameColor.Yellow, GameColor.Green, GameColor.Red };

  [Header("Dificultad Inicial")]
  [SerializeField] private float initialSpawnRate = 1.1f;
  [SerializeField] private float initialBlockSpeed = 4.5f;

  [Header("Power-Ups")]
  [Tooltip("Probabilidad (0 a 1) de generar un bloque especial Power-Up en vez de uno normal")]
  [SerializeField] [Range(0f, 0.3f)] private float powerUpSpawnChance = 0.08f;

  [Header("Límites de Dificultad")]
  [SerializeField] private float minSpawnRate = 0.45f;
  [SerializeField] private float maxBlockSpeed = 10f;
  [SerializeField] private float difficultyRampTime = 60f; // Tiempo en segundos para llegar al tope

  private float currentSpawnRate;
  private float currentBlockSpeed;
  private float timer = 0f;
  private float gameTime = 0f;

  void Start()
  {
    currentSpawnRate = initialSpawnRate;
    currentBlockSpeed = initialBlockSpeed;
  }

  void Update()
  {
    if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing))) return;

    // Progreso temporal de dificultad
    gameTime += Time.deltaTime;
    float progress = Mathf.Clamp01(gameTime / difficultyRampTime);

    // Interpolar velocidad y cadencia
    currentBlockSpeed = Mathf.Lerp(initialBlockSpeed, maxBlockSpeed, progress);
    currentSpawnRate = Mathf.Lerp(initialSpawnRate, minSpawnRate, progress);

    timer += Time.deltaTime;
    if (timer >= currentSpawnRate)
    {
      SpawnRandomBlock();
      timer = 0f;
    }
  }

  /// <summary>
  /// Reinicia el temporizador de generación con un margen de gracia en segundos
  /// antes de comenzar a generar bloques (ideal tras revivir o cuenta regresiva).
  /// </summary>
  public void ResetSpawnDelay(float delaySeconds = 1.5f)
  {
    timer = -Mathf.Max(0f, delaySeconds);
  }

  private void SpawnRandomBlock()
  {
    float[] lanes = (LaneManager.Instance != null) ? LaneManager.Instance.AllLanes : AllLanes;
    float laneX = lanes[Random.Range(0, lanes.Length)];
    float spawnY = (LaneManager.Instance != null) ? LaneManager.Instance.SpawnY : 6f;
    Vector3 spawnPos = new Vector3(laneX, spawnY, 0f);

    GameObject newBlock = BlockPool.Instance.GetBlock();
    newBlock.transform.position = spawnPos;
    newBlock.transform.rotation = Quaternion.identity;

    var collectible = newBlock.GetComponent<CollectibleBlock>();

    // Si estamos en Overdrive, garantiza el 100% de bloques válidos normales
    if (GameManager.Instance != null && GameManager.Instance.IsOverdriveActive)
    {
      GameColor overdriveColor = (laneX < 0) ? GameColor.Red : GameColor.Blue;
      collectible.Setup(overdriveColor, currentBlockSpeed, CollectibleBlock.BlockType.Normal);
      return;
    }

    // Probabilidad de generar un bloque especial (Power-Up)
    // Se evita generar power-ups si ya hay un Slow-Mo activo para balance y ritmo de juego
    bool canSpawnPowerUp = PowerUpManager.Instance == null || !PowerUpManager.Instance.IsSlowMoActive;
    if (canSpawnPowerUp && Random.value < powerUpSpawnChance)
    {
      CollectibleBlock.BlockType pType = (Random.value < 0.5f)
        ? CollectibleBlock.BlockType.Bomb
        : CollectibleBlock.BlockType.SlowMotion;

      collectible.Setup(GameColor.Yellow, currentBlockSpeed, pType);
      return;
    }

    // Lógica estándar con obstáculos (cero reservas GC)
    GameColor selectedColor;
    if (laneX < 0)
    {
      float roll = Random.value;
      if (roll < 0.5f)
      {
        selectedColor = GameColor.Red;
      }
      else
      {
        selectedColor = LeftObstacles[Random.Range(0, LeftObstacles.Length)];
      }
    }
    else
    {
      float roll = Random.value;
      if (roll < 0.5f)
      {
        selectedColor = GameColor.Blue;
      }
      else
      {
        selectedColor = RightObstacles[Random.Range(0, RightObstacles.Length)];
      }
    }

    collectible.Setup(selectedColor, currentBlockSpeed, CollectibleBlock.BlockType.Normal);
  }
}