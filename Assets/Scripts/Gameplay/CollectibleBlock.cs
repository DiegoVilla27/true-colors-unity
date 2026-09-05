using System.Collections.Generic;
using UnityEngine;

public class CollectibleBlock : MonoBehaviour
{
  public enum BlockType
  {
    Normal,
    Bomb,
    SlowMotion
  }

  #region Optimization Registries & Static Colors
  public static readonly List<CollectibleBlock> ActiveBlocks = new List<CollectibleBlock>(32);

  private static readonly Color ColorRed = new Color(1f, 0.2f, 0.2f);
  private static readonly Color ColorBlue = new Color(0.2f, 0.5f, 1f);
  private static readonly Color ColorYellow = new Color(1f, 0.9f, 0.2f);
  private static readonly Color ColorGreen = new Color(0.2f, 0.9f, 0.3f);
  private static readonly Color ColorBomb = new Color(0.88f, 0.25f, 0.98f); // Neon Magenta (#E040FB)
  private static readonly Color ColorSlowMo = new Color(0.0f, 0.9f, 1.0f);   // Electric Cyan (#00E5FF)

  public static Color GetColorForType(GameColor color)
  {
    switch (color)
    {
      case GameColor.Red: return ColorRed;
      case GameColor.Blue: return ColorBlue;
      case GameColor.Yellow: return ColorYellow;
      case GameColor.Green: return ColorGreen;
      default: return Color.white;
    }
  }
  #endregion

  public BlockType blockType = BlockType.Normal;
  public GameColor blockColor;
  public float fallSpeed = 5f;

  [Header("VFX")]
  [SerializeField] private GameObject particlePrefab;

  private SpriteRenderer spriteRenderer;

  void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
  }

  void OnEnable()
  {
    ActiveBlocks.Add(this);
  }

  void OnDisable()
  {
    ActiveBlocks.Remove(this);
  }

  public void Setup(GameColor newColor, float speed, BlockType type = BlockType.Normal)
  {
    blockType = type;
    blockColor = newColor;
    fallSpeed = speed;

    if (spriteRenderer == null)
      spriteRenderer = GetComponent<SpriteRenderer>();

    if (blockType == BlockType.Bomb)
    {
      spriteRenderer.color = ColorBomb;
    }
    else if (blockType == BlockType.SlowMotion)
    {
      spriteRenderer.color = ColorSlowMo;
    }
    else
    {
      spriteRenderer.color = GetColorForType(blockColor);
    }
  }

  void Update()
  {
    float speedMultiplier = PowerUpManager.Instance != null ? PowerUpManager.Instance.GlobalSpeedMultiplier : 1f;
    transform.Translate(Vector3.down * (fallSpeed * speedMultiplier) * Time.deltaTime);

    if (transform.position.y < -5.5f)
    {
      // Los power-ups nunca causan Game Over al salir de pantalla
      if (blockType != BlockType.Normal)
      {
        Recycle();
        return;
      }

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
      // Recolección universal de Power-Ups (cualquier nave puede recogerlos con éxito)
      if (blockType == BlockType.Bomb)
      {
        SpawnParticles();
        HapticFeedback.VibrateCollect();
        if (PowerUpManager.Instance != null)
        {
          PowerUpManager.Instance.ActivateBomb();
        }
        Recycle();
        return;
      }

      if (blockType == BlockType.SlowMotion)
      {
        SpawnParticles();
        HapticFeedback.VibrateCollect();
        if (PowerUpManager.Instance != null)
        {
          PowerUpManager.Instance.ActivateSlowMotion();
        }
        Recycle();
        return;
      }

      // Bloques normales
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

  /// <summary>
  /// Determina si este bloque es un obstáculo peligroso según el carril donde se encuentra.
  /// </summary>
  public bool IsHazard()
  {
    if (blockType != BlockType.Normal) return false;

    bool isLeftSide = transform.position.x < 0;
    return isLeftSide ? (blockColor != GameColor.Red) : (blockColor != GameColor.Blue);
  }

  /// <summary>
  /// Limpia y recicla este bloque con partículas cuando la Bomba es activada.
  /// </summary>
  public void WipeByBomb()
  {
    SpawnParticles();
    Recycle();
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

    Color pColor = spriteRenderer != null ? spriteRenderer.color : ColorRed;

    if (BlockPool.Instance != null)
    {
      BlockPool.Instance.SpawnCollectParticles(particlePrefab, transform.position, pColor);
    }
    else
    {
      GameObject fx = Instantiate(particlePrefab, transform.position, Quaternion.identity);
      var mainModule = fx.GetComponent<ParticleSystem>().main;
      mainModule.startColor = pColor;
    }
  }

  public void ForceColor(GameColor newColor)
  {
    blockType = BlockType.Normal;
    blockColor = newColor;

    if (spriteRenderer == null)
      spriteRenderer = GetComponent<SpriteRenderer>();

    spriteRenderer.color = GetColorForType(blockColor);
  }
}