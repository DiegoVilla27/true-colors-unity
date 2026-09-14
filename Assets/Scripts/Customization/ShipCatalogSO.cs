using System.Collections.Generic;
using UnityEngine;

namespace TrueColors.Customization
{
    /// <summary>
    /// ScriptableObject que almacena el catálogo maestro de naves espaciales y el precio del paquete.
    /// Diseñado bajo ScriptableObject-Driven Architecture para permitir configuración sin tocar código.
    /// </summary>
    [CreateAssetMenu(fileName = "ShipCatalog", menuName = "TrueColors/Ship Catalog")]
    public class ShipCatalogSO : ScriptableObject
    {
        [Header("Economía del Paquete")]
        [Tooltip("Costo en rocas para comprar el paquete de 4 naves")]
        public int packPrice = 10000;

        [Header("Catálogo de Naves")]
        [Tooltip("Lista completa de naves disponibles (incluye la nave clásica y las del paquete)")]
        public List<ShipDefinition> ships = new List<ShipDefinition>();

        public ShipDefinition GetShipById(string id)
        {
            if (string.IsNullOrEmpty(id) || ships == null) return GetDefaultShip();
            for (int i = 0; i < ships.Count; i++)
            {
                if (ships[i] != null && ships[i].id == id)
                    return ships[i];
            }
            return GetDefaultShip();
        }

        public ShipDefinition GetDefaultShip()
        {
            if (ships == null || ships.Count == 0) return null;
            for (int i = 0; i < ships.Count; i++)
            {
                if (ships[i] != null && ships[i].isDefaultUnlocked)
                    return ships[i];
            }
            return ships[0];
        }

        public int Count => ships != null ? ships.Count : 0;
    }
}
