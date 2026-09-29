using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;

namespace TrueColors.CloudSave
{
    /// <summary>
    /// Responsabilidad Única (SRP): Administra el guardado y sincronización en la nube mediante UGS Cloud Save.
    /// Implementa estrategia Local-First:
    /// - Escritura y lectura inmediata en PlayerPrefs (latencia cero, 100% offline).
    /// - Sincronización asíncrona en segundo plano con Unity Gaming Services.
    /// - Restauración automática si el jugador desinstala y reinstala la aplicación.
    /// </summary>
    public class CloudSaveManager : MonoBehaviour
    {
        private static CloudSaveManager _instance;
        public static CloudSaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<CloudSaveManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[CloudSaveManager]");
                        _instance = go.AddComponent<CloudSaveManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            _ = Instance;
        }

        public const string KEY_PACK_PURCHASED = "ships_pack_purchased";
        public const string KEY_TOTAL_ROCKS = "total_rocks";
        public const string KEY_SELECTED_SHIP = "selected_ship";
        public const string KEY_HIGH_SCORE = "high_score";

        private bool _isSyncing = false;
        private Task<bool> _authTask;
        private float _rocksSyncCooldown = 0f;
        private bool _pendingRocksSync = false;

        public bool IsSyncing => _isSyncing;

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
        }

        void Start()
        {
            // Iniciar sincronización tras inicializar servicios
            _ = InitializeAndSyncAsync();
        }

        void Update()
        {
            // Debounce / Cooldown para no saturar la red con rocas ganadas continuamente
            if (_pendingRocksSync)
            {
                _rocksSyncCooldown -= Time.unscaledDeltaTime;
                if (_rocksSyncCooldown <= 0f)
                {
                    _pendingRocksSync = false;
                    int rocks = CurrencyManager.Instance != null ? CurrencyManager.Instance.TotalRocks : PlayerPrefs.GetInt(CurrencyManager.ROCKS_KEY, 0);
                    _ = ForceSaveRocksToCloudAsync(rocks);
                }
            }
        }

        /// <summary>
        /// Asegura que Unity Gaming Services esté inicializado y el jugador autenticado.
        /// </summary>
        public async Task<bool> EnsureAuthenticatedAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Initialized &&
                AuthenticationService.Instance != null &&
                AuthenticationService.Instance.IsSignedIn)
            {
                return true;
            }

            if (_authTask != null)
            {
                return await _authTask;
            }

            _authTask = DoAuthAsync();
            return await _authTask;
        }

        private async Task<bool> DoAuthAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                while (UnityServices.State == ServicesInitializationState.Initializing)
                {
                    await Task.Delay(50);
                }

                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    return false;
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"<color=#FFAA00><b>[CloudSave]</b></color> UGS offline o no autenticado ({ex.Message}). Jugando con persistencia local.");
                return false;
            }
            finally
            {
                _authTask = null;
            }
        }

        /// <summary>
        /// Inicializa UGS y sincroniza el estado local con la nube.
        /// </summary>
        public async Task InitializeAndSyncAsync()
        {
            bool ready = await EnsureAuthenticatedAsync();
            if (!ready) return;

            await LoadAndSyncAsync();
        }

        /// <summary>
        /// Descarga los datos de UGS Cloud Save y restaura compras o saldo si el usuario reinstaló el juego.
        /// </summary>
        public async Task LoadAndSyncAsync()
        {
            if (_isSyncing) return;
            _isSyncing = true;

            try
            {
                bool ready = await EnsureAuthenticatedAsync();
                if (!ready) return;

                var keys = new HashSet<string> { KEY_PACK_PURCHASED, KEY_TOTAL_ROCKS, KEY_SELECTED_SHIP, KEY_HIGH_SCORE };
                var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (results == null || results.Count == 0)
                {
                    Debug.Log("<color=#00CCFF><b>[CloudSave]</b></color> Sin datos previos en la nube para este usuario. Respaldando estado actual...");
                    await SaveAllLocalDataToCloudAsync();
                    return;
                }

                Debug.Log($"<color=#55FF55><b>[CloudSave]</b></color> {results.Count} datos descargados de UGS. Sincronizando...");

                // 1. Pack de Naves comprado (TrueColors_ShipsPackPurchased)
                if (results.TryGetValue(KEY_PACK_PURCHASED, out var packItem))
                {
                    bool cloudPurchased = ParseBool(packItem);
                    bool localPurchased = PlayerPrefs.GetInt(ShipCustomizationManager.PACK_PURCHASED_KEY, 0) == 1;

                    if (cloudPurchased && !localPurchased)
                    {
                        Debug.Log("<color=#FFD700><b>[CloudSave]</b></color> ⭐ ¡Paquete de 4 naves restaurado exitosamente desde la nube!");
                        if (ShipCustomizationManager.Instance != null)
                        {
                            ShipCustomizationManager.Instance.ApplyCloudPurchasedState(true);
                        }
                        else
                        {
                            PlayerPrefs.SetInt(ShipCustomizationManager.PACK_PURCHASED_KEY, 1);
                            PlayerPrefs.Save();
                        }
                    }
                    else if (localPurchased && !cloudPurchased)
                    {
                        // Si en local ya lo compró offline, asegurar que suba a la nube
                        _ = SaveShipPackPurchasedAsync(true);
                    }
                }

                // 2. Saldo de Rocas (TrueColors_TotalRocks)
                if (results.TryGetValue(KEY_TOTAL_ROCKS, out var rocksItem))
                {
                    int cloudRocks = ParseInt(rocksItem);
                    int localRocks = PlayerPrefs.GetInt(CurrencyManager.ROCKS_KEY, 0);

                    // Si el usuario borró la app y la reinstaló, localRocks es 0 y cloudRocks tiene su saldo acumulado
                    if (cloudRocks > localRocks)
                    {
                        Debug.Log($"<color=#55FF55><b>[CloudSave]</b></color> Rocas restauradas desde la nube: {cloudRocks} (antes {localRocks})");
                        if (CurrencyManager.Instance != null)
                        {
                            CurrencyManager.Instance.SyncFromCloud(cloudRocks);
                        }
                        else
                        {
                            PlayerPrefs.SetInt(CurrencyManager.ROCKS_KEY, cloudRocks);
                            PlayerPrefs.Save();
                        }
                    }
                    else if (localRocks > cloudRocks)
                    {
                        // Saldo local es mayor (ej. jugó offline y acumuló más): actualizar la nube
                        _ = ForceSaveRocksToCloudAsync(localRocks);
                    }
                }

                // 3. Nave equipada (TrueColors_SelectedShip)
                if (results.TryGetValue(KEY_SELECTED_SHIP, out var shipItem))
                {
                    string cloudShip = ParseString(shipItem);
                    string localShip = PlayerPrefs.GetString(ShipCustomizationManager.SELECTED_SHIP_KEY, "ship_classic");

                    if (!string.IsNullOrEmpty(cloudShip) && cloudShip != localShip)
                    {
                        if (ShipCustomizationManager.Instance != null)
                        {
                            ShipCustomizationManager.Instance.SelectShip(cloudShip);
                        }
                        else
                        {
                            PlayerPrefs.SetString(ShipCustomizationManager.SELECTED_SHIP_KEY, cloudShip);
                            PlayerPrefs.Save();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSave] Excepción durante la sincronización: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// Guarda todo el estado local actual en la nube de UGS.
        /// </summary>
        public async Task SaveAllLocalDataToCloudAsync()
        {
            int rocks = CurrencyManager.Instance != null ? CurrencyManager.Instance.TotalRocks : PlayerPrefs.GetInt(CurrencyManager.ROCKS_KEY, 0);
            bool packPurchased = PlayerPrefs.GetInt(ShipCustomizationManager.PACK_PURCHASED_KEY, 0) == 1;
            string selectedShip = PlayerPrefs.GetString(ShipCustomizationManager.SELECTED_SHIP_KEY, "ship_classic");
            int highScore = PlayerPrefs.GetInt("TrueColors_HighScore", 0);

            var dict = new Dictionary<string, object>
            {
                { KEY_PACK_PURCHASED, packPurchased },
                { KEY_TOTAL_ROCKS, rocks },
                { KEY_SELECTED_SHIP, selectedShip },
                { KEY_HIGH_SCORE, highScore }
            };

            await SendSaveRequestAsync(dict);
        }

        /// <summary>
        /// Notifica la compra del paquete de naves para respaldarlo de inmediato en la nube.
        /// </summary>
        public async Task SaveShipPackPurchasedAsync(bool purchased)
        {
            var dict = new Dictionary<string, object>
            {
                { KEY_PACK_PURCHASED, purchased }
            };
            await SendSaveRequestAsync(dict);
        }

        /// <summary>
        /// Notifica la nave equipada para respaldarla en la nube.
        /// </summary>
        public async Task SaveSelectedShipAsync(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return;

            var dict = new Dictionary<string, object>
            {
                { KEY_SELECTED_SHIP, shipId }
            };
            await SendSaveRequestAsync(dict);
        }

        /// <summary>
        /// Agenda la sincronización de rocas con debounce para optimizar peticiones de red.
        /// </summary>
        public void ScheduleRocksSync(float delaySeconds = 3f)
        {
            _pendingRocksSync = true;
            _rocksSyncCooldown = Mathf.Max(_rocksSyncCooldown, delaySeconds);
        }

        /// <summary>
        /// Fuerza la sincronización inmediata del saldo de rocas (ej. tras una compra en tienda o fin de partida).
        /// </summary>
        public async Task ForceSaveRocksToCloudAsync(int rocks)
        {
            _pendingRocksSync = false;
            var dict = new Dictionary<string, object>
            {
                { KEY_TOTAL_ROCKS, rocks }
            };
            await SendSaveRequestAsync(dict);
        }

        private async Task SendSaveRequestAsync(Dictionary<string, object> data)
        {
            if (data == null || data.Count == 0) return;

            try
            {
                bool ready = await EnsureAuthenticatedAsync();
                if (!ready) return;

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log($"<color=#55FF55><b>[CloudSave]</b></color> Sincronizado en UGS Cloud: {string.Join(", ", data.Keys)}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSave] Error al enviar datos a la nube: {ex.Message}");
            }
        }

        #region Helpers de Parsing
        private bool ParseBool(Item item)
        {
            try { return item.Value.GetAs<bool>(); }
            catch
            {
                string s = item.Value.GetAsString();
                if (bool.TryParse(s, out bool result)) return result;
                return s == "1";
            }
        }

        private int ParseInt(Item item)
        {
            try { return item.Value.GetAs<int>(); }
            catch
            {
                string s = item.Value.GetAsString();
                if (int.TryParse(s, out int result)) return result;
                return 0;
            }
        }

        private string ParseString(Item item)
        {
            try { return item.Value.GetAsString(); }
            catch { return item.Value.GetAs<string>(); }
        }
        #endregion
    }
}
