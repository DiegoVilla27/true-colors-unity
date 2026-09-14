using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrueColors.Customization;

namespace TrueColors.UI
{
    /// <summary>
    /// Controlador de la vista de Naves (Ships Hangar) en MainMenu / Shop.
    /// Muestra las naves disponibles, navegación en carrusel, compra del paquete por 10.000 rocas,
    /// y equipamiento dinámico con persistencia local inmediata.
    /// </summary>
    public class ShipsModalView : MonoBehaviour
    {
        [Header("Moneda (Rocas)")]
        [SerializeField] private TextMeshProUGUI rocksText;

        [Header("Nave Central en Exhibición")]
        [SerializeField] private Image shipPreviewImage;
        [SerializeField] private TextMeshProUGUI shipNameText;
        [SerializeField] private TextMeshProUGUI shipDescriptionText;
        [SerializeField] private TextMeshProUGUI pageIndicatorText;

        [Header("Navegación")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        [Header("Acción de Equipar")]
        [SerializeField] private Button equipButton;
        [SerializeField] private TextMeshProUGUI equipButtonText;
        [SerializeField] private Image equipButtonBg;

        [Header("Compra de Paquete (4 Naves)")]
        [SerializeField] private Button buyPackButton;
        [SerializeField] private TextMeshProUGUI buyPackText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Navegación / Cierre")]
        [SerializeField] private Button backButton;

        [Header("Miniaturas Rápidas")]
        [SerializeField] private List<Button> thumbnailButtons = new List<Button>();
        [SerializeField] private List<Image> thumbnailImages = new List<Image>();

        [Header("Sprites de Botones")]
        [SerializeField] private Sprite btnActiveSprite;
        [SerializeField] private Sprite btnInactiveSprite;

        private int _currentIndex = 0;
        private Coroutine _feedbackCoroutine;

        void Awake()
        {
            WireButtons();
        }

        void OnEnable()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnTotalRocksChanged += HandleTotalRocksChanged;
            }

            ShipCustomizationManager.OnPackPurchased += HandlePackPurchased;
            ShipCustomizationManager.OnShipSelected += HandleShipSelected;

            // Inicializar en la nave actualmente equipada
            SyncToEquippedShipIndex();
            RefreshUI();
        }

        void OnDisable()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnTotalRocksChanged -= HandleTotalRocksChanged;
            }

            ShipCustomizationManager.OnPackPurchased -= HandlePackPurchased;
            ShipCustomizationManager.OnShipSelected -= HandleShipSelected;

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
                _feedbackCoroutine = null;
            }
        }

        private void WireButtons()
        {
            if (prevButton != null)
            {
                prevButton.onClick.RemoveListener(PrevShip);
                prevButton.onClick.AddListener(PrevShip);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(NextShip);
                nextButton.onClick.AddListener(NextShip);
            }

            if (equipButton != null)
            {
                equipButton.onClick.RemoveListener(EquipCurrentShip);
                equipButton.onClick.AddListener(EquipCurrentShip);
            }

            if (buyPackButton != null)
            {
                buyPackButton.onClick.RemoveListener(BuyPack);
                buyPackButton.onClick.AddListener(BuyPack);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(CloseView);
                backButton.onClick.AddListener(CloseView);
            }

            for (int i = 0; i < thumbnailButtons.Count; i++)
            {
                int index = i;
                if (thumbnailButtons[i] != null)
                {
                    thumbnailButtons[i].onClick.RemoveAllListeners();
                    thumbnailButtons[i].onClick.AddListener(() => SelectIndex(index));
                }
            }
        }

        private void SyncToEquippedShipIndex()
        {
            var manager = ShipCustomizationManager.Instance;
            if (manager == null || manager.Catalog == null || manager.Catalog.ships == null) return;

            string selectedId = manager.SelectedShipId;
            var list = manager.Catalog.ships;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].id == selectedId)
                {
                    _currentIndex = i;
                    return;
                }
            }
        }

        public void NextShip()
        {
            if (HapticFeedback.IsVibrationEnabled) HapticFeedback.VibrateCollect();

            var catalog = ShipCustomizationManager.Instance?.Catalog;
            if (catalog == null || catalog.ships == null || catalog.ships.Count == 0) return;

            _currentIndex = (_currentIndex + 1) % catalog.ships.Count;
            RefreshUI();
        }

        public void PrevShip()
        {
            if (HapticFeedback.IsVibrationEnabled) HapticFeedback.VibrateCollect();

            var catalog = ShipCustomizationManager.Instance?.Catalog;
            if (catalog == null || catalog.ships == null || catalog.ships.Count == 0) return;

            _currentIndex = (_currentIndex - 1 + catalog.ships.Count) % catalog.ships.Count;
            RefreshUI();
        }

        public void SelectIndex(int index)
        {
            var catalog = ShipCustomizationManager.Instance?.Catalog;
            if (catalog == null || catalog.ships == null) return;
            if (index < 0 || index >= catalog.ships.Count) return;

            if (HapticFeedback.IsVibrationEnabled) HapticFeedback.VibrateCollect();
            _currentIndex = index;
            RefreshUI();
        }

        public void EquipCurrentShip()
        {
            var manager = ShipCustomizationManager.Instance;
            if (manager == null || manager.Catalog == null) return;

            var ships = manager.Catalog.ships;
            if (_currentIndex < 0 || _currentIndex >= ships.Count) return;

            var ship = ships[_currentIndex];
            if (ship == null) return;

            if (manager.SelectShip(ship.id))
            {
                if (HapticFeedback.IsVibrationEnabled) HapticFeedback.VibrateCollect();
                ShowFeedback("EQUIPPED!", new Color(0.2f, 1f, 0.4f, 1f));
                RefreshUI();
            }
            else
            {
                ShowFeedback("LOCKED! BUY PACK FIRST", new Color(1f, 0.35f, 0.35f, 1f));
            }
        }

        public void BuyPack()
        {
            var manager = ShipCustomizationManager.Instance;
            if (manager == null) return;

            if (manager.IsPackPurchased)
            {
                ShowFeedback("PACK ALREADY OWNED!", Color.yellow);
                return;
            }

            if (manager.TryBuyPack())
            {
                if (HapticFeedback.IsVibrationEnabled) HapticFeedback.VibrateCollect();
                ShowFeedback("PACK UNLOCKED! ALL SHIPS READY", new Color(0.2f, 1f, 0.4f, 1f));

                // Auto equipar la nave actualmente visible
                var ships = manager.Catalog?.ships;
                if (ships != null && _currentIndex >= 0 && _currentIndex < ships.Count)
                {
                    manager.SelectShip(ships[_currentIndex].id);
                }

                RefreshUI();
            }
            else
            {
                if (HapticFeedback.IsVibrationEnabled) HapticFeedback.VibrateCollect();
                int need = manager.Catalog != null ? manager.Catalog.packPrice : 10000;
                ShowFeedback($"NOT ENOUGH ROCKS! (NEED {need:N0})", new Color(1f, 0.25f, 0.25f, 1f));
            }
        }

        public void CloseView()
        {
            if (HapticFeedback.IsVibrationEnabled) HapticFeedback.VibrateCollect();

            var shopModal = GetComponentInParent<ShopModal>();
            if (shopModal != null)
            {
                shopModal.CloseShipsView();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void RefreshUI()
        {
            var manager = ShipCustomizationManager.Instance;
            var catalog = manager != null ? manager.Catalog : null;

            // 1. Contador de Rocas
            int totalRocks = CurrencyManager.Instance != null
                ? CurrencyManager.Instance.TotalRocks
                : PlayerPrefs.GetInt(CurrencyManager.ROCKS_KEY, 0);

            if (rocksText != null)
            {
                rocksText.text = totalRocks.ToString("N0");
            }

            if (catalog == null || catalog.ships == null || catalog.ships.Count == 0)
            {
                return;
            }

            _currentIndex = Mathf.Clamp(_currentIndex, 0, catalog.ships.Count - 1);
            var ship = catalog.ships[_currentIndex];
            if (ship == null) return;

            // 2. Nave en Exhibición
            if (shipPreviewImage != null)
            {
                shipPreviewImage.sprite = ship.sprite;
                shipPreviewImage.preserveAspect = true;
            }

            if (shipNameText != null)
            {
                shipNameText.text = ship.displayName;
            }

            if (shipDescriptionText != null)
            {
                shipDescriptionText.text = ship.description;
            }

            if (pageIndicatorText != null)
            {
                pageIndicatorText.text = $"{_currentIndex + 1} / {catalog.ships.Count}";
            }

            // 3. Estado de Desbloqueo y Equipamiento
            bool isUnlocked = manager.IsShipUnlocked(ship.id);
            bool isEquipped = (manager.SelectedShipId == ship.id);

            if (equipButton != null)
            {
                if (equipButtonBg != null)
                {
                    equipButtonBg.type = Image.Type.Simple;
                    if (btnInactiveSprite != null) equipButtonBg.sprite = btnInactiveSprite;
                }

                if (isEquipped)
                {
                    equipButton.interactable = false;
                    if (equipButtonText != null)
                    {
                        equipButtonText.text = "EQUIPPED";
                        equipButtonText.color = new Color(1f, 0.85f, 0.2f, 1f);
                    }
                    if (equipButtonBg != null) equipButtonBg.color = Color.white;
                }
                else if (isUnlocked)
                {
                    equipButton.interactable = true;
                    if (equipButtonText != null)
                    {
                        equipButtonText.text = "EQUIP";
                        equipButtonText.color = Color.white;
                    }
                    if (equipButtonBg != null) equipButtonBg.color = Color.white;
                }
                else
                {
                    equipButton.interactable = false;
                    if (equipButtonText != null)
                    {
                        equipButtonText.text = "LOCKED";
                        equipButtonText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (equipButtonBg != null) equipButtonBg.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                }
            }

            // 4. Botón de Compra del Paquete
            bool packPurchased = manager.IsPackPurchased;
            if (buyPackButton != null)
            {
                var buyImg = buyPackButton.GetComponent<Image>();
                if (buyImg != null)
                {
                    buyImg.type = Image.Type.Simple;
                    if (btnInactiveSprite != null) buyImg.sprite = btnInactiveSprite;
                }

                if (packPurchased)
                {
                    buyPackButton.interactable = false;
                    if (buyPackText != null)
                    {
                        buyPackText.text = "PACK OWNED";
                        buyPackText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (buyImg != null) buyImg.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                }
                else
                {
                    buyPackButton.interactable = true;
                    int price = catalog.packPrice;
                    if (buyPackText != null)
                    {
                        buyPackText.text = $"BUY PACK: {price:N0}";
                        buyPackText.color = Color.white;
                    }
                    if (buyImg != null) buyImg.color = Color.white;
                }
            }

            // 5. Miniaturas
            for (int i = 0; i < thumbnailImages.Count; i++)
            {
                if (thumbnailImages[i] != null && i < catalog.ships.Count)
                {
                    thumbnailImages[i].sprite = catalog.ships[i]?.sprite;
                    // Resaltar miniatura seleccionada
                    thumbnailImages[i].color = (i == _currentIndex) ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                }
            }
        }

        private void HandleTotalRocksChanged(int newTotal)
        {
            if (rocksText != null)
            {
                rocksText.text = newTotal.ToString("N0");
            }
        }

        private void HandlePackPurchased()
        {
            RefreshUI();
        }

        private void HandleShipSelected(ShipDefinition ship)
        {
            RefreshUI();
        }

        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText == null) return;

            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(FeedbackRoutine(message, color));
        }

        private IEnumerator FeedbackRoutine(string message, Color color)
        {
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = message;
            feedbackText.color = color;

            yield return new WaitForSecondsRealtime(2.5f);

            float elapsed = 0f;
            Color initial = feedbackText.color;
            while (elapsed < 0.4f)
            {
                elapsed += Time.unscaledDeltaTime;
                Color c = initial;
                c.a = Mathf.Lerp(initial.a, 0f, elapsed / 0.4f);
                feedbackText.color = c;
                yield return null;
            }

            feedbackText.gameObject.SetActive(false);
            _feedbackCoroutine = null;
        }
    }
}
