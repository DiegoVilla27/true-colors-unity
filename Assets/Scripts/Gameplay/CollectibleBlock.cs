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

  [Header("Movimiento & Rotación")]
  public BlockType blockType = BlockType.Normal;
  public GameColor blockColor;
  public float fallSpeed = 5f;
  [Tooltip("Velocidad mínima de rotación sutil en grados por segundo")]
  [SerializeField] private float minRotationSpeed = 20f;
  [Tooltip("Velocidad máxima de rotación sutil en grados por segundo")]
  [SerializeField] private float maxRotationSpeed = 45f;

  private float currentRotationSpeed = 0f;

  [Header("Rock Sprites")]
  [SerializeField] private Sprite spriteRedRock;
  [SerializeField] private Sprite spriteBlueRock;
  [SerializeField] private Sprite spriteYellowRock;
  [SerializeField] private Sprite spriteGreenRock;
  [SerializeField] private Sprite spriteBombRock;
  [SerializeField] private Sprite spriteTimeRock;

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

  private bool isHandled = false;

  public void Setup(GameColor newColor, float speed, BlockType type = BlockType.Normal)
  {
    isHandled = false;
    blockType = type;
    blockColor = newColor;
    fallSpeed = speed;

    // Asignar velocidad y dirección de giro sutil y variada (horaria o antihoraria)
    float rotSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
    currentRotationSpeed = (Random.value < 0.5f) ? rotSpeed : -rotSpeed;

    if (spriteRenderer == null)
      spriteRenderer = GetComponent<SpriteRenderer>();

    Sprite targetSprite = null;

    if (blockType == BlockType.Bomb)
    {
      targetSprite = spriteBombRock;
      spriteRenderer.color = (targetSprite != null) ? Color.white : ColorBomb;
    }
    else if (blockType == BlockType.SlowMotion)
    {
      targetSprite = spriteTimeRock;
      spriteRenderer.color = (targetSprite != null) ? Color.white : ColorSlowMo;
    }
    else
    {
      switch (blockColor)
      {
        case GameColor.Red: targetSprite = spriteRedRock; break;
        case GameColor.Blue: targetSprite = spriteBlueRock; break;
        case GameColor.Yellow: targetSprite = spriteYellowRock; break;
        case GameColor.Green: targetSprite = spriteGreenRock; break;
      }
      spriteRenderer.color = (targetSprite != null) ? Color.white : GetColorForType(blockColor);
    }

    if (targetSprite != null)
    {
      spriteRenderer.sprite = targetSprite;
    }
  }

  void Update()
  {
    float speedMultiplier = PowerUpManager.Instance != null ? PowerUpManager.Instance.GlobalSpeedMultiplier : 1f;

    // Desplazamiento vertical en espacio de mundo para que el giro local no desvíe el carril
    transform.position += Vector3.down * (fallSpeed * speedMultiplier * Time.deltaTime);

    // Giro sutil continuo desacoplado del movimiento pero reactivo a Slow-Motion
    transform.Rotate(0f, 0f, currentRotationSpeed * speedMultiplier * Time.deltaTime);

    float despawnThreshold = (LaneManager.Instance != null) ? LaneManager.Instance.DespawnY : -5.5f;
    if (transform.position.y < despawnThreshold)
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
      HandleShipCollision(ship);
    }
  }

  private void OnTriggerStay2D(Collider2D other)
  {
    if (other.TryGetComponent<ShipTarget>(out var ship))
    {
      HandleShipCollision(ship);
    }
  }

  /// <summary>
  /// Procesa la colisión con la nave con validación de carril y soporte para barridos rápidos (swipes).
  /// </summary>
  public void HandleShipCollision(ShipTarget ship)
  {
    if (isHandled || ship == null) return;

    // Validar que la nave y la roca se encuentren en el mismo carril o en trayectoria de intersección válida.
    // La separación entre centros de carriles contiguos es ~0.60f.
    // Con un umbral de 0.85f * ancho de carril (~0.51f), se descartan colisiones con carriles adyacentes no invadidos,
    // mientras se asegura el 100% de impactos cuando la nave atraviesa o se posa en el carril a gran velocidad.
    float maxAllowedLaneDelta = (LaneManager.Instance != null && LaneManager.Instance.LaneColumnWidth > 0f)
      ? (LaneManager.Instance.LaneColumnWidth * 0.85f)
      : 0.52f;

    if (Mathf.Abs(transform.position.x - ship.transform.position.x) > maxAllowedLaneDelta)
    {
      return;
    }

    isHandled = true;

    // Recolección universal de Power-Ups (cualquier nave puede recogerlos con éxito)
    if (blockType == BlockType.Bomb)
    {
      SpawnParticles();
      HapticFeedback.VibrateCollect();
      if (PowerUpManager.Instance != null)
      {
        PowerUpManager.Instance.ActivateBomb();
      }
      NotifyRockDestroyed();
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
      NotifyRockDestroyed();
      Recycle();
      return;
    }

    // Bloques normales
    if (ship.targetColor == this.blockColor)
    {
      SpawnParticles();
      HapticFeedback.VibrateCollect();
      GameManager.Instance.AddScore(10);
      NotifyRockDestroyed();
    }
    else
    {
      GameManager.Instance.TriggerGameOver();
    }

    Recycle();
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
    NotifyRockDestroyed();
    Recycle();
  }

  private void NotifyRockDestroyed()
  {
    if (CurrencyManager.Instance != null)
    {
      CurrencyManager.Instance.AddRocks(1);
    }
    else if (GameManager.Instance != null)
    {
      GameManager.Instance.AddDestroyedRock(1);
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

  public void Recycle()
  {
    transform.rotation = Quaternion.identity;
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

    Sprite targetSprite = (blockColor == GameColor.Red) ? spriteRedRock : spriteBlueRock;
    if (targetSprite != null)
    {
      spriteRenderer.sprite = targetSprite;
      spriteRenderer.color = Color.white;
    }
    else
    {
      spriteRenderer.color = GetColorForType(blockColor);
    }
  }
}