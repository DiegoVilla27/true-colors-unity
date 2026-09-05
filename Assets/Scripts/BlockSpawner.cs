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
    if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

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

  private void SpawnRandomBlock()
  {
    float laneX = AllLanes[Random.Range(0, AllLanes.Length)];
    Vector3 spawnPos = new Vector3(laneX, 6f, 0f);

    GameObject newBlock = BlockPool.Instance.GetBlock();
    newBlock.transform.position = spawnPos;
    newBlock.transform.rotation = Quaternion.identity;

    GameColor selectedColor;

    // Si estamos en Overdrive, garantiza el 100% de bloques válidos
    if (GameManager.Instance != null && GameManager.Instance.IsOverdriveActive)
    {
      selectedColor = (laneX < 0) ? GameColor.Red : GameColor.Blue;
    }
    else
    {
      // Lógica estándar con obstáculos (cero reservas GC)
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
    }

    newBlock.GetComponent<CollectibleBlock>().Setup(selectedColor, currentBlockSpeed);
  }
}