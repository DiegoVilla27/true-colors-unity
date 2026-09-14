using System;
using UnityEngine;

namespace TrueColors.Customization
{
    /// <summary>
    /// Servicio Singleton que administra la personalización de naves, persistencia local (PlayerPrefs),
    /// compra del paquete de naves con rocas y notificación mediante eventos C#.
    /// </summary>
    public class ShipCustomizationManager : MonoBehaviour
    {
        public const string PACK_PURCHASED_KEY = "TrueColors_ShipsPackPurchased";
        public const string SELECTED_SHIP_KEY = "TrueColors_SelectedShipId";

        private static ShipCustomizationManager _instance;
        public static ShipCustomizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ShipCustomizationManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[ShipCustomizationManager]");
                        _instance = go.AddComponent<ShipCustomizationManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Catálogo Maestro")]
        [SerializeField] private ShipCatalogSO catalog;

        public ShipCatalogSO Catalog
        {
            get
            {
                if (catalog == null)
                {
                    catalog = Resources.Load<ShipCatalogSO>("ShipCatalog");
                }
                return catalog;
            }
            set => catalog = value;
        }

        #region Eventos de Dominio
        public static event Action<ShipDefinition> OnShipSelected;
        public static event Action OnPackPurchased;
        #endregion

        private bool _isPackPurchased = false;
        private string _selectedShipId = "ship_classic";

        public bool IsPackPurchased => _isPackPurchased;
        public string SelectedShipId => _selectedShipId;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadState();
        }

        public void LoadState()
        {
            _isPackPurchased = PlayerPrefs.GetInt(PACK_PURCHASED_KEY, 0) == 1;
            _selectedShipId = PlayerPrefs.GetString(SELECTED_SHIP_KEY, "ship_classic");

            // Validar que la nave seleccionada esté realmente desbloqueada
            if (!IsShipUnlocked(_selectedShipId))
            {
                var defaultShip = Catalog != null ? Catalog.GetDefaultShip() : null;
                _selectedShipId = defaultShip != null ? defaultShip.id : "ship_classic";
                PlayerPrefs.SetString(SELECTED_SHIP_KEY, _selectedShipId);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Comprueba si una nave específica se encuentra desbloqueada para su uso.
        /// </summary>
        public bool IsShipUnlocked(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return true;

            var ship = Catalog != null ? Catalog.GetShipById(shipId) : null;
            if (ship == null) return true;

            if (ship.isDefaultUnlocked) return true;

            return _isPackPurchased;
        }

        /// <summary>
        /// Intenta comprar el paquete de 4 naves utilizando el saldo de rocas acumuladas en CurrencyManager.
        /// </summary>
        public bool TryBuyPack()
        {
            if (_isPackPurchased) return true;

            int cost = Catalog != null ? Catalog.packPrice : 10000;

            if (CurrencyManager.Instance != null)
            {
                if (!CurrencyManager.Instance.TrySpendRocks(cost))
                {
                    return false;
                }
            }
            else
            {
                int currentRocks = PlayerPrefs.GetInt(CurrencyManager.ROCKS_KEY, 0);
                if (currentRocks < cost) return false;

                PlayerPrefs.SetInt(CurrencyManager.ROCKS_KEY, currentRocks - cost);
                PlayerPrefs.Save();
            }

            _isPackPurchased = true;
            PlayerPrefs.SetInt(PACK_PURCHASED_KEY, 1);
            PlayerPrefs.Save();

            OnPackPurchased?.Invoke();
            return true;
        }

        /// <summary>
        /// Selecciona y equipa la nave indicada si está desbloqueada, persistiendo la elección localmente.
        /// </summary>
        public bool SelectShip(string shipId)
        {
            if (!IsShipUnlocked(shipId)) return false;

            _selectedShipId = shipId;
            PlayerPrefs.SetString(SELECTED_SHIP_KEY, _selectedShipId);
            PlayerPrefs.Save();

            var ship = GetSelectedShip();
            OnShipSelected?.Invoke(ship);
            return true;
        }

        /// <summary>
        /// Obtiene la definición de la nave actualmente equipada.
        /// </summary>
        public ShipDefinition GetSelectedShip()
        {
            if (Catalog == null) return null;
            return Catalog.GetShipById(_selectedShipId);
        }

        /// <summary>
        /// Obtiene directamente el sprite de la nave seleccionada.
        /// </summary>
        public Sprite GetSelectedShipSprite()
        {
            var def = GetSelectedShip();
            return def != null ? def.sprite : null;
        }

        #region Debug / Editor Tools
        [ContextMenu("Desbloquear Paquete de Naves")]
        public void DebugUnlockPack()
        {
            _isPackPurchased = true;
            PlayerPrefs.SetInt(PACK_PURCHASED_KEY, 1);
            PlayerPrefs.Save();
            OnPackPurchased?.Invoke();
            Debug.Log("[ShipCustomizationManager] Paquete de naves desbloqueado manualmente.");
        }

        [ContextMenu("Restablecer Paquete a Bloqueado")]
        public void DebugResetPack()
        {
            _isPackPurchased = false;
            PlayerPrefs.SetInt(PACK_PURCHASED_KEY, 0);
            _selectedShipId = "ship_classic";
            PlayerPrefs.SetString(SELECTED_SHIP_KEY, _selectedShipId);
            PlayerPrefs.Save();
            OnShipSelected?.Invoke(GetSelectedShip());
            OnPackPurchased?.Invoke();
            Debug.Log("[ShipCustomizationManager] Paquete y nave restablecidos a valores iniciales.");
        }
        #endregion
    }
}
