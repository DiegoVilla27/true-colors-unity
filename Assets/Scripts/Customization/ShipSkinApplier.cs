using UnityEngine;
using UnityEngine.UI;

namespace TrueColors.Customization
{
    /// <summary>
    /// Componente reactivo desacoplado que aplica el sprite de la nave seleccionada
    /// tanto a elementos de interfaz (Image en MainMenu) como a GameObjects de mundo (SpriteRenderer en MainGame).
    /// Se suscribe al evento OnShipSelected para reflejar cualquier cambio en tiempo real.
    /// </summary>
    public class ShipSkinApplier : MonoBehaviour
    {
        private Image _uiImage;
        private SpriteRenderer _spriteRenderer;

        void Awake()
        {
            _uiImage = GetComponent<Image>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void OnEnable()
        {
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

        private void HandleShipSelected(ShipDefinition ship)
        {
            if (ship != null && ship.sprite != null)
            {
                ApplySprite(ship.sprite);
            }
        }

        public void ApplyCurrentSkin()
        {
            var manager = ShipCustomizationManager.Instance;
            if (manager != null)
            {
                var sprite = manager.GetSelectedShipSprite();
                if (sprite != null)
                {
                    ApplySprite(sprite);
                }
            }
        }

        private void ApplySprite(Sprite sprite)
        {
            if (_uiImage != null)
            {
                _uiImage.sprite = sprite;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }
    }
}
