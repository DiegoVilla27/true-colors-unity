#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TrueColors.CloudSave;

namespace TrueColors.EditorTools
{
    /// <summary>
    /// Herramientas de Editor para probar y depurar la sincronización de Unity Gaming Services (UGS) Cloud Save.
    /// </summary>
    public static class CloudSaveDevTools
    {
        [MenuItem("Tools/Cloud Save/Sincronizar Datos Locales hacia la Nube Ahora")]
        public static async void SyncLocalToCloud()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CloudSaveDevTools] Inicia el juego en Play Mode para interactuar con los servicios activos de UGS.");
                return;
            }

            if (CloudSaveManager.Instance != null)
            {
                Debug.Log("<color=#00CCFF><b>[CloudSaveDevTools]</b></color> Forzando sincronización hacia UGS Cloud...");
                await CloudSaveManager.Instance.SaveAllLocalDataToCloudAsync();
            }
        }

        [MenuItem("Tools/Cloud Save/Forzar Descarga y Restauración desde la Nube")]
        public static async void ForceDownloadFromCloud()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CloudSaveDevTools] Inicia el juego en Play Mode para interactuar con los servicios activos de UGS.");
                return;
            }

            if (CloudSaveManager.Instance != null)
            {
                Debug.Log("<color=#00CCFF><b>[CloudSaveDevTools]</b></color> Descargando datos desde UGS Cloud...");
                await CloudSaveManager.Instance.LoadAndSyncAsync();
            }
        }

        [MenuItem("Tools/Cloud Save/Simular Borrado de App (Limpiar PlayerPrefs Locales)")]
        public static void SimulateAppUninstall()
        {
            PlayerPrefs.DeleteKey(CurrencyManager.ROCKS_KEY);
            PlayerPrefs.DeleteKey(ShipCustomizationManager.PACK_PURCHASED_KEY);
            PlayerPrefs.DeleteKey(ShipCustomizationManager.SELECTED_SHIP_KEY);
            PlayerPrefs.DeleteKey("TrueColors_HighScore");
            PlayerPrefs.Save();

            Debug.Log("<color=#FF5555><b>[CloudSaveDevTools]</b></color> ¡PlayerPrefs locales eliminados! (Simulación de app desinstalada). Ahora entra a Play Mode para comprobar cómo UGS Cloud Save restaura automáticamente tus compras y rocas.");
        }
    }
}
#endif
