using System;
using UnityEngine;

namespace TrueColors.Customization
{
    /// <summary>
    /// Entidad de datos que define los atributos de cada nave espacial disponible en el juego.
    /// </summary>
    [Serializable]
    public class ShipDefinition
    {
        [Tooltip("Identificador único de la nave (ej. 'ship_classic', 'ship_valkyrie')")]
        public string id;

        [Tooltip("Nombre legible para la interfaz de usuario")]
        public string displayName;

        [Tooltip("Sprite visual de la nave usado tanto en UI como en SpriteRenderer")]
        public Sprite sprite;

        [Tooltip("Si es verdadero, el jugador tiene esta nave desbloqueada por defecto sin necesidad de compra")]
        public bool isDefaultUnlocked;

        [Tooltip("Descripción temática o subtítulo lore de la nave")]
        [TextArea(2, 4)]
        public string description;
    }
}
