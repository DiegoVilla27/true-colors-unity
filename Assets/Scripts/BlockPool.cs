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

  #region Particle Pooling
  private readonly Queue<ParticleSystem> particlePool = new Queue<ParticleSystem>();

  public void SpawnCollectParticles(GameObject prefab, Vector3 position, Color color)
  {
    if (prefab == null) return;

    ParticleSystem ps = null;
    while (particlePool.Count > 0)
    {
      ps = particlePool.Dequeue();
      if (ps != null) break;
    }

    if (ps == null)
    {
      GameObject obj = Instantiate(prefab, transform);
      ps = obj.GetComponent<ParticleSystem>();
      var main = ps.main;
      main.stopAction = ParticleSystemStopAction.None;
    }

    ps.transform.position = position;
    var mainModule = ps.main;
    mainModule.startColor = color;
    ps.gameObject.SetActive(true);
    ps.Clear();
    ps.Play();

    StartCoroutine(ReturnParticleAfterDelay(ps, 0.35f));
  }

  private System.Collections.IEnumerator ReturnParticleAfterDelay(ParticleSystem ps, float delay)
  {
    yield return new WaitForSeconds(delay);
    if (ps != null)
    {
      ps.gameObject.SetActive(false);
      particlePool.Enqueue(ps);
    }
  }
  #endregion
}