using UnityEngine;

public class CollectibleBlock : MonoBehaviour
{
  public GameColor blockColor;
  public float fallSpeed = 5f;

  [Header("VFX")]
  [SerializeField] private GameObject particlePrefab;

  private SpriteRenderer spriteRenderer;

  void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
  }

  public void Setup(GameColor newColor, float speed)
  {
    blockColor = newColor;
    fallSpeed = speed;

    switch (blockColor)
    {
      case GameColor.Red:
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
        break;
      case GameColor.Blue:
        spriteRenderer.color = new Color(0.2f, 0.5f, 1f);
        break;
      case GameColor.Yellow:
        spriteRenderer.color = new Color(1f, 0.9f, 0.2f);
        break;
      case GameColor.Green:
        spriteRenderer.color = new Color(0.2f, 0.9f, 0.3f);
        break;
    }
  }

  void Update()
  {
    transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

    if (transform.position.y < -5.5f)
    {
      bool isLeftSide = transform.position.x < 0;

      if (isLeftSide && blockColor == GameColor.Red)
      {
        TriggerMissGameOver();
        return;
      }

      if (!isLeftSide && blockColor == GameColor.Blue)
      {
        TriggerMissGameOver();
        return;
      }

      // En lugar de destruir, regresa al pool
      Recycle();
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.TryGetComponent<ShipTarget>(out var ship))
    {
      if (ship.targetColor == this.blockColor)
      {
        SpawnParticles();
        HapticFeedback.VibrateCollect();
        GameManager.Instance.AddScore(10);
      }
      else
      {
        GameManager.Instance.TriggerGameOver();
      }

      Recycle();
    }
  }

  private void TriggerMissGameOver()
  {
    if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
    {
      GameManager.Instance.TriggerGameOver();
    }
    Recycle();
  }

  private void Recycle()
  {
    if (BlockPool.Instance != null)
    {
      BlockPool.Instance.ReturnBlock(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void SpawnParticles()
  {
    if (particlePrefab == null) return;

    GameObject fx = Instantiate(particlePrefab, transform.position, Quaternion.identity);
    var mainModule = fx.GetComponent<ParticleSystem>().main;
    mainModule.startColor = spriteRenderer.color;
  }

  public void ForceColor(GameColor newColor)
  {
    blockColor = newColor;

    if (spriteRenderer == null)
      spriteRenderer = GetComponent<SpriteRenderer>();

    switch (blockColor)
    {
      case GameColor.Red:
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
        break;
      case GameColor.Blue:
        spriteRenderer.color = new Color(0.2f, 0.5f, 1f);
        break;
      case GameColor.Yellow:
        spriteRenderer.color = new Color(1f, 0.9f, 0.2f);
        break;
      case GameColor.Green:
        spriteRenderer.color = new Color(0.2f, 0.9f, 0.3f);
        break;
    }
  }
}