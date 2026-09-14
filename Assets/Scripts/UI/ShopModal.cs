using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Administra el modal de Tienda (Shop) en MainMenu.
/// Controla las opciones de compra de 'NO ADS', acceso a 'SHIPS'
/// y el cierre de la tienda con retorno al menú principal.
/// </summary>
public class ShopModal : MonoBehaviour
{
    [Header("Subvistas")]
    [SerializeField] private GameObject mainCategoryView;
    [SerializeField] private GameObject shipsView;

    [Header("Botones de Opciones")]
    [SerializeField] private Button noAdsButton;
    [SerializeField] private Button shipsButton;

    [Header("Botón Inferior de Cierre")]
    [SerializeField] private Button closeButton;

    void Awake()
    {
        WireButtons();
    }

    void OnEnable()
    {
        ShowMainCategories();
    }

    public void ShowMainCategories()
    {
        if (mainCategoryView != null) mainCategoryView.SetActive(true);
        if (shipsView != null) shipsView.SetActive(false);
    }

    private void WireButtons()
    {
        WireButton(noAdsButton, BuyNoAds);
        WireButton(shipsButton, OpenShips);
        WireButton(closeButton, Close);
    }

    private void WireButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;

        // Comprobar si ya existe como listener persistente para no duplicar ejecución
        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
        {
            if (btn.onClick.GetPersistentTarget(i) == (UnityEngine.Object)this &&
                btn.onClick.GetPersistentMethodName(i) == action.Method.Name)
            {
                return;
            }
        }

        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    /// <summary>
    /// Acción de compra de eliminación de anuncios.
    /// </summary>
    public void BuyNoAds()
    {
        if (HapticFeedback.IsVibrationEnabled)
        {
            HapticFeedback.VibrateCollect();
        }

        // TODO: Integrar lógica de In-App Purchase (IAP) para eliminar anuncios más adelante.
        Debug.Log("[ShopModal] Opción 'NO ADS' seleccionada.");
    }

    /// <summary>
    /// Acción de apertura de selección/compra de naves.
    /// </summary>
    public void OpenShips()
    {
        if (HapticFeedback.IsVibrationEnabled)
        {
            HapticFeedback.VibrateCollect();
        }

        if (shipsView != null)
        {
            if (mainCategoryView != null) mainCategoryView.SetActive(false);
            shipsView.SetActive(true);
        }
        else
        {
            Debug.Log("[ShopModal] Opción 'SHIPS' seleccionada.");
        }
    }

    /// <summary>
    /// Cierra la vista de Naves y regresa a la vista de categorías de la Tienda.
    /// </summary>
    public void CloseShipsView()
    {
        if (shipsView != null) shipsView.SetActive(false);
        if (mainCategoryView != null) mainCategoryView.SetActive(true);
    }

    /// <summary>
    /// Cierra el modal de la Tienda o regresa al menú principal.
    /// </summary>
    public void Close()
    {
        if (HapticFeedback.IsVibrationEnabled)
        {
            HapticFeedback.VibrateCollect();
        }

        if (shipsView != null && shipsView.activeSelf)
        {
            CloseShipsView();
            return;
        }

        var menuController = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        if (menuController != null)
        {
            menuController.CloseShop();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
