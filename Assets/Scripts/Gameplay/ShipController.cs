using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Header("Carriles (Coordenadas X)")]
    [Tooltip("Posiciones X para [Izquierda, Centro, Derecha]")]
    [SerializeField] private float[] lanePositions = new float[3];

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 15f;

    [Header("Inclinación Dinámica (Banking / Tilt)")]
    [Tooltip("Ángulo máximo de inclinación en grados al desplazarse de carril")]
    [SerializeField] private float maxTiltAngle = 14f;
    [Tooltip("Velocidad de suavizado del giro")]
    [SerializeField] private float tiltSpeed = 12f;

    // 0 = Carril Izquierdo, 1 = Centro, 2 = Carril Derecho
    private int currentLane = 1;
    private Vector3 targetPosition;
    private readonly Collider2D[] _sweepHits = new Collider2D[8];
    private ShipTarget _shipTarget;

    void Awake()
    {
        _shipTarget = GetComponent<ShipTarget>();
    }

    void Start()
    {
        // Iniciar centrada en el carril 1
        targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, 0f);
        transform.position = targetPosition;
    }

    void Update()
    {
        float previousX = transform.position.x;

        // Desplazamiento fluido hacia el carril seleccionado
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        float currentX = transform.position.x;
        float deltaX = currentX - previousX;

        // Detección continua por barrido (Sweep Test) para evitar tunneling/esquivar rocas en cambios rápidos de carril
        if (Mathf.Abs(deltaX) > 0.02f && _shipTarget != null)
        {
            float minX = Mathf.Min(previousX, currentX);
            float maxX = Mathf.Max(previousX, currentX);
            Vector2 sweepCenter = new Vector2((minX + maxX) * 0.5f, transform.position.y);
            Vector2 sweepSize = new Vector2((maxX - minX) + 0.4f, 0.6f);

            int hitCount = Physics2D.OverlapBoxNonAlloc(sweepCenter, sweepSize, 0f, _sweepHits);
            for (int i = 0; i < hitCount; i++)
            {
                if (_sweepHits[i] != null && _sweepHits[i].TryGetComponent<CollectibleBlock>(out var block))
                {
                    block.HandleShipCollision(_shipTarget);
                }
            }
        }

        // Inclinación dinámica proporcional a la velocidad horizontal de desplazamiento
        if (Time.deltaTime > 0f)
        {
            float velocityX = deltaX / Time.deltaTime;
            float targetTilt = -Mathf.Clamp(velocityX / 6f, -1f, 1f) * maxTiltAngle;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetTilt);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * tiltSpeed);
        }
    }

    public void MoveLeft()
    {
        if (currentLane > 0)
        {
            currentLane--;
            targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, 0f);
        }
    }

    public void MoveRight()
    {
        if (currentLane < lanePositions.Length - 1)
        {
            currentLane++;
            targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, 0f);
        }
    }

    /// <summary>
    /// Configura dinámicamente los carriles y la altura de la nave en base al layout de pantalla.
    /// </summary>
    public void ApplyLanes(float[] newLanes, float? newY = null)
    {
        if (newLanes == null || newLanes.Length == 0) return;
        lanePositions = newLanes;
        currentLane = Mathf.Clamp(currentLane, 0, lanePositions.Length - 1);

        float posY = newY.HasValue ? newY.Value : transform.position.y;
        targetPosition = new Vector3(lanePositions[currentLane], posY, 0f);
        transform.position = targetPosition;
    }
}