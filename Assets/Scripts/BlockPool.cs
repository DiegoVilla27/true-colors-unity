using System.Collections.Generic;
using UnityEngine;

public class BlockPool : MonoBehaviour
{
  public static BlockPool Instance { get; private set; }

  [SerializeField] private GameObject blockPrefab;
  [SerializeField] private int initialPoolSize = 25;

  private readonly Queue<GameObject> pool = new Queue<GameObject>();

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;

    InitializePool();
  }

  private void InitializePool()
  {
    for (int i = 0; i < initialPoolSize; i++)
    {
      GameObject block = Instantiate(blockPrefab, transform);
      block.SetActive(false);
      pool.Enqueue(block);
    }
  }

  public GameObject GetBlock()
  {
    GameObject block;

    if (pool.Count > 0)
    {
      block = pool.Dequeue();
    }
    else
    {
      // Si la cadencia exige más bloques de los previstos, expande el pool
      block = Instantiate(blockPrefab, transform);
    }

    block.SetActive(true);
    return block;
  }

  public void ReturnBlock(GameObject block)
  {
    block.SetActive(false);
    pool.Enqueue(block);
  }
}