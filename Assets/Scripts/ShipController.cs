using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Header("Carriles (Coordenadas X)")]
    [Tooltip("Posiciones X para [Izquierda, Centro, Derecha]")]
    [SerializeField] private float[] lanePositions = new float[3];

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 15f;

    // 0 = Carril Izquierdo, 1 = Centro, 2 = Carril Derecho
    private int currentLane = 1;
    private Vector3 targetPosition;

    void Start()
    {
        // Iniciar centrada en el carril 1
        targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, 0f);
        transform.position = targetPosition;
    }

    void Update()
    {
        // Desplazamiento fluido hacia el carril seleccionado
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
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
}