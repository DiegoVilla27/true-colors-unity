#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TrueColors.CloudSave;
using TrueColors.Customization;

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

        [MenuItem("Tools/Cloud Save/Restablecer TODO a Cero (Nube y Local)")]
        public static async void ResetEverythingToZero()
        {
            // 1. Limpieza de PlayerPrefs
            PlayerPrefs.DeleteKey(CurrencyManager.ROCKS_KEY);
            PlayerPrefs.DeleteKey(ShipCustomizationManager.PACK_PURCHASED_KEY);
            PlayerPrefs.DeleteKey(ShipCustomizationManager.SELECTED_SHIP_KEY);
            PlayerPrefs.DeleteKey("TrueColors_HighScore");
            PlayerPrefs.DeleteKey("TrueColors_PlayerCallsign");
            PlayerPrefs.DeleteKey("TC_OpenLeaderboardOnMenu");
            PlayerPrefs.Save();

            // 2. Limpieza de instancias en memoria y nube (en Play Mode)
            if (Application.isPlaying)
            {
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.SyncFromCloud(0);
                }
                if (ShipCustomizationManager.Instance != null)
                {
                    ShipCustomizationManager.Instance.ApplyCloudPurchasedState(false);
                    ShipCustomizationManager.Instance.SelectShip("ship_classic");
                }
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.ResetHighScore();
                }

                // 3. Sobrescribir estado en la nube a 0
                if (CloudSaveManager.Instance != null)
                {
                    await CloudSaveManager.Instance.ResetAllCloudDataAsync();
                }

                Debug.Log("<color=#55FF55><b>[RESET TOTAL]</b></color> ¡Todo restablecido a 0 (0 Rocas, 0 Récord, Pack de Naves BLOQUEADO) tanto en Local como en la Nube!");
            }
            else
            {
                Debug.Log("<color=#55FF55><b>[RESET LOCAL]</b></color> ¡PlayerPrefs locales restablecidos a 0! (0 Rocas, Pack bloqueado, Récord 0).");
            }
        }
    }
}
#endif
