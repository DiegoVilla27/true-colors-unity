using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Calcula matemáticamente en tiempo de ejecución las coordenadas
/// exactas de los 6 carriles del juego, la altura de las naves y los límites de spawn/despawn,
/// adaptándose automáticamente a cualquier relación de aspecto de pantalla móvil (16:9, 19.5:9, 20:9)
/// con división 50/50 y márgenes configurables.
/// </summary>
public class LaneManager : MonoBehaviour
{
  public static LaneManager Instance { get; private set; }

  [Header("Márgenes de Pantalla (Paddings)")]
  [Tooltip("Margen horizontal exterior para que los elementos no toquen los bordes laterales del móvil")]
  [SerializeField] private float horizontalPadding = 0.35f;

  [Tooltip("Margen central de separación entre la mitad izquierda y la derecha")]
  [SerializeField] private float centerDividerSpacing = 0.15f;

  [Header("Márgenes Verticales")]
  [Tooltip("Distancia desde la base inferior de la pantalla donde se sitúan las naves")]
  [SerializeField] private float shipBottomOffset = 1.3f;

  [Tooltip("Distancia por encima del borde superior de la pantalla desde donde caen los bloques")]
  [SerializeField] private float topSpawnOffset = 1.0f;

  [Tooltip("Distancia por debajo del borde inferior de la pantalla donde se reciclan los bloques")]
  [SerializeField] private float bottomDespawnOffset = 1.2f;

  [Header("Referencias de Naves (Opcional - se auto-detectan si están vacías)")]
  [SerializeField] private ShipController leftShip;
  [SerializeField] private ShipController rightShip;

  private float[] leftLanes = new float[3];
  private float[] rightLanes = new float[3];
  private float[] allLanes = new float[6];

  private float screenHalfWidth;
  private float screenHalfHeight;
  private float laneColumnWidth = 0.65f;
  private float shipY;
  private float spawnY;
  private float despawnY;

  public float[] LeftLanes => leftLanes;
  public float[] RightLanes => rightLanes;
  public float[] AllLanes => allLanes;
  public float LaneColumnWidth => laneColumnWidth;
  public float ShipY => shipY;
  public float SpawnY => spawnY;
  public float DespawnY => despawnY;

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;

    CalculateLayout();
    ApplyLayoutToShips();
  }

  /// <summary>
  /// Calcula las dimensiones de la pantalla y la posición de los carriles.
  /// </summary>
  public void CalculateLayout()
  {
    Camera cam = Camera.main;
    if (cam == null) cam = FindAnyObjectByType<Camera>();

    if (cam != null)
    {
      screenHalfHeight = cam.orthographicSize;
      screenHalfWidth = screenHalfHeight * cam.aspect;
    }
    else
    {
      // Valores de respaldo seguros en caso de ejecución sin cámara
      screenHalfHeight = 5.0f;
      screenHalfWidth = 5.0f * (9f / 16f);
    }

    // Cálculos verticales
    shipY = -screenHalfHeight + shipBottomOffset;
    spawnY = screenHalfHeight + topSpawnOffset;
    despawnY = -screenHalfHeight - bottomDespawnOffset;

    // Ancho útil para cada 50% de la pantalla
    // El sector izquierdo abarca desde: (-screenHalfWidth + horizontalPadding) hasta (-centerDividerSpacing)
    float usableSectorWidth = screenHalfWidth - horizontalPadding - centerDividerSpacing;
    if (usableSectorWidth <= 0.5f) usableSectorWidth = 1.5f; // Salvaguarda

    laneColumnWidth = usableSectorWidth / 3f;

    // 3 Carriles Izquierdos (ordenados de izquierda a derecha: Exterior, Centro, Interior)
    float leftSectorStart = -screenHalfWidth + horizontalPadding;
    leftLanes[0] = leftSectorStart + (laneColumnWidth * 0.5f);
    leftLanes[1] = leftSectorStart + (laneColumnWidth * 1.5f);
    leftLanes[2] = leftSectorStart + (laneColumnWidth * 2.5f);

    // 3 Carriles Derechos (ordenados de izquierda a derecha: Interior, Centro, Exterior)
    float rightSectorStart = centerDividerSpacing;
    rightLanes[0] = rightSectorStart + (laneColumnWidth * 0.5f);
    rightLanes[1] = rightSectorStart + (laneColumnWidth * 1.5f);
    rightLanes[2] = rightSectorStart + (laneColumnWidth * 2.5f);

    // Array combinado para el Spawner
    allLanes[0] = leftLanes[0];
    allLanes[1] = leftLanes[1];
    allLanes[2] = leftLanes[2];
    allLanes[3] = rightLanes[0];
    allLanes[4] = rightLanes[1];
    allLanes[5] = rightLanes[2];
  }

  /// <summary>
  /// Asigna los carriles calculados a las naves en la escena.
  /// </summary>
  public void ApplyLayoutToShips()
  {
    if (leftShip == null || rightShip == null)
    {
      var ships = FindObjectsByType<ShipController>();
      foreach (var s in ships)
      {
        if (s.transform.position.x < 0) leftShip = s;
        else rightShip = s;
      }
    }

    if (leftShip != null)
    {
      leftShip.ApplyLanes(leftLanes, shipY);
    }

    if (rightShip != null)
    {
      rightShip.ApplyLanes(rightLanes, shipY);
    }
  }

  #if UNITY_EDITOR
  void OnValidate()
  {
    CalculateLayout();
  }

  void OnDrawGizmos()
  {
    if (!Application.isPlaying)
    {
      CalculateLayout();
    }

    // Dibujar carriles izquierdos (Rojo)
    Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.65f);
    for (int i = 0; i < leftLanes.Length; i++)
    {
      Gizmos.DrawLine(new Vector3(leftLanes[i], -screenHalfHeight, 0), new Vector3(leftLanes[i], screenHalfHeight, 0));
    }

    // Dibujar carriles derechos (Azul)
    Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.65f);
    for (int i = 0; i < rightLanes.Length; i++)
    {
      Gizmos.DrawLine(new Vector3(rightLanes[i], -screenHalfHeight, 0), new Vector3(rightLanes[i], screenHalfHeight, 0));
    }

    // Divisor central (Gris suave)
    Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
    Gizmos.DrawLine(new Vector3(0, -screenHalfHeight, 0), new Vector3(0, screenHalfHeight, 0));

    // Línea de posición de naves (Verde)
    Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.5f);
    Gizmos.DrawLine(new Vector3(-screenHalfWidth, shipY, 0), new Vector3(screenHalfWidth, shipY, 0));

    // Línea de spawn superior (Amarillo)
    Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f);
    Gizmos.DrawLine(new Vector3(-screenHalfWidth, spawnY, 0), new Vector3(screenHalfWidth, spawnY, 0));
  }
  #endif
}
