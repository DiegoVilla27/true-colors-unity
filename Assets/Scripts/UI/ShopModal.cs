using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Administra el modal de Tienda (Shop) en MainMenu.
/// Controla las opciones de compra de 'NO ADS', acceso a 'SHIPS'
/// y el cierre de la tienda con retorno al menú principal.
/// </summary>
public class ShopModal : MonoBehaviour
{
    [Header("Botones de Opciones")]
    [SerializeField] private Button noAdsButton;
    [SerializeField] private Button shipsButton;

    [Header("Botón Inferior de Cierre")]
    [SerializeField] private Button closeButton;

    void Awake()
    {
        WireButtons();
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

        // TODO: Integrar apertura del hangar de naves y compras con rocas/moneda más adelante.
        Debug.Log("[ShopModal] Opción 'SHIPS' seleccionada.");
    }

    /// <summary>
    /// Cierra el modal de la Tienda y regresa al menú principal.
    /// </summary>
    public void Close()
    {
        if (HapticFeedback.IsVibrationEnabled)
        {
            HapticFeedback.VibrateCollect();
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
