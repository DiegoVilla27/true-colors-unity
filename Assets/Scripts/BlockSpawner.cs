using System;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
  [Header("Prefab")]
  [SerializeField] private GameObject blockPrefab;

  [Header("Configuración de Carriles")]
  private float[] allLanes = new float[] { -2.1f, -1.4f, -0.7f, 0.7f, 1.4f, 2.1f };

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
  private Array colorValues;

  void Start()
  {
    colorValues = Enum.GetValues(typeof(GameColor));
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
    float laneX = allLanes[UnityEngine.Random.Range(0, allLanes.Length)];
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
      // Lógica estándar con obstáculos
      if (laneX < 0)
      {
        float roll = UnityEngine.Random.value;
        if (roll < 0.5f)
        {
          selectedColor = GameColor.Red;
        }
        else
        {
          GameColor[] obstacles = { GameColor.Yellow, GameColor.Green, GameColor.Blue };
          selectedColor = obstacles[UnityEngine.Random.Range(0, obstacles.Length)];
        }
      }
      else
      {
        float roll = UnityEngine.Random.value;
        if (roll < 0.5f)
        {
          selectedColor = GameColor.Blue;
        }
        else
        {
          GameColor[] obstacles = { GameColor.Yellow, GameColor.Green, GameColor.Red };
          selectedColor = obstacles[UnityEngine.Random.Range(0, obstacles.Length)];
        }
      }
    }

    newBlock.GetComponent<CollectibleBlock>().Setup(selectedColor, currentBlockSpeed);
  }
}