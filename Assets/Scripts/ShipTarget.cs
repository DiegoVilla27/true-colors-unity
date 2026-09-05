using UnityEngine;

public enum GameColor
{
    Red,
    Blue,
    Yellow,
    Green
}

public class ShipTarget : MonoBehaviour
{
    [Header("Color que esta nave debe recoger")]
    public GameColor targetColor;
}