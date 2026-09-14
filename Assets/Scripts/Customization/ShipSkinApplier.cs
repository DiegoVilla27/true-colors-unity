using UnityEngine;
using UnityEngine.UI;

namespace TrueColors.Customization
{
    /// <summary>
    /// Componente reactivo desacoplado que aplica el sprite de la nave seleccionada
    /// tanto a elementos de interfaz (Image en MainMenu) como a GameObjects de mundo (SpriteRenderer en MainGame).
    /// Se suscribe al evento OnShipSelected para reflejar cualquier cambio en tiempo real.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShipSkinApplier : MonoBehaviour
    {
        private Image _uiImage;
        private SpriteRenderer _spriteRenderer;

        void Awake()
        {
            CacheComponents();
            ApplyCurrentSkin();
        }

        void OnEnable()
        {
            CacheComponents();
            ShipCustomizationManager.OnShipSelected += HandleShipSelected;
            ApplyCurrentSkin();
        }

        void OnDisable()
        {
            ShipCustomizationManager.OnShipSelected -= HandleShipSelected;
        }

        void Start()
        {
            ApplyCurrentSkin();
        }

        private void CacheComponents()
        {
            if (_uiImage == null) _uiImage = GetComponent<Image>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void HandleShipSelected(ShipDefinition ship)
        {
            if (ship != null && ship.sprite != null)
            {
                ApplySprite(ship.sprite);
            }
            else
            {
                ApplyCurrentSkin();
            }
        }

        public void ApplyCurrentSkin()
        {
            CacheComponents();

            Sprite targetSprite = null;

            // 1. Obtener sprite desde el Singleton de Customización
            var manager = ShipCustomizationManager.Instance;
            if (manager != null)
            {
                targetSprite = manager.GetSelectedShipSprite();
            }

            // 2. Respaldo blindado directo de PlayerPrefs + Resources.Load
            if (targetSprite == null)
            {
                string savedId = PlayerPrefs.GetString(ShipCustomizationManager.SELECTED_SHIP_KEY, "ship_classic");
                var catalog = Resources.Load<ShipCatalogSO>("ShipCatalog");
                if (catalog != null)
                {
                    var def = catalog.GetShipById(savedId);
                    if (def != null) targetSprite = def.sprite;
                }
            }

            if (targetSprite != null)
            {
                ApplySprite(targetSprite);
            }
        }

        public void ApplySprite(Sprite sprite)
        {
            if (sprite == null) return;
            CacheComponents();

            if (_uiImage != null)
            {
                _uiImage.sprite = sprite;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }

        /// <summary>
        /// Aplica inmediatamente el sprite seleccionado a todas las naves de la escena (MainMenu y MainGame),
        /// incluso si los GameObjects están actualmente inactivos.
        /// </summary>
        public static void ApplyToAllInScene(Sprite sprite = null)
        {
            if (sprite == null)
            {
                var manager = ShipCustomizationManager.Instance;
                if (manager != null) sprite = manager.GetSelectedShipSprite();

                if (sprite == null)
                {
                    string savedId = PlayerPrefs.GetString(ShipCustomizationManager.SELECTED_SHIP_KEY, "ship_classic");
                    var catalog = Resources.Load<ShipCatalogSO>("ShipCatalog");
                    if (catalog != null)
                    {
                        var def = catalog.GetShipById(savedId);
                        if (def != null) sprite = def.sprite;
                    }
                }
            }

            if (sprite == null) return;

            // 1. Actualizar todas las instancias de ShipSkinApplier (activas e inactivas)
            var appliers = Resources.FindObjectsOfTypeAll<ShipSkinApplier>();
            for (int i = 0; i < appliers.Length; i++)
            {
                var applier = appliers[i];
                if (applier == null || applier.gameObject == null) continue;
                // Ignorar assets/prefabs que no pertenecen a una escena cargada
                if (!applier.gameObject.scene.isLoaded) continue;

                applier.ApplySprite(sprite);
            }

            // 2. Actualizar naves conocidas por nombre explícito como blindaje
            string[] knownNames = { "MenuShip_Red", "MenuShip_Blue", "Ship_Left", "Ship_Right" };
            for (int i = 0; i < knownNames.Length; i++)
            {
                var go = GameObject.Find(knownNames[i]);
                if (go != null)
                {
                    var img = go.GetComponent<Image>();
                    if (img != null) img.sprite = sprite;

                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = sprite;
                }
            }
        }
    }
}
