using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

namespace TrueColors.Leaderboard.Services
{
    /// <summary>
    /// Servicio de Leaderboard en la nube 100% gratuito utilizando Unity Gaming Services (UGS).
    /// Permite ranking global unificado (cross-platform) para iOS, Android y PC sin pagar
    /// las cuentas de desarrollador de Apple ($99/año) ni Google Play ($25) para funcionar.
    /// </summary>
    public class UGSLeaderboardService : ILeaderboardService
    {
        private const string CALLSIGN_PREF_KEY = "TrueColors_PlayerCallsign";
        private LeaderboardConfigSO _config;
        private Task<bool> _initTask;

        public bool IsAuthenticated
        {
            get
            {
                try
                {
                    return UnityServices.State == ServicesInitializationState.Initialized &&
                           AuthenticationService.Instance != null &&
                           AuthenticationService.Instance.IsSignedIn;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Initialize(LeaderboardConfigSO config)
        {
            _config = config;
            Debug.Log("<color=#00CCFF><b>[UGSLeaderboard]</b></color> Inicializando Unity Gaming Services Leaderboard...");

            if (_config != null && _config.AutoAuthenticateOnStart)
            {
                _ = EnsureInitializedAndAuthenticatedAsync();
            }
        }

        public async void Authenticate(Action<bool, string> onComplete = null)
        {
            bool success = await EnsureInitializedAndAuthenticatedAsync();
            if (success)
            {
                string playerId = AuthenticationService.Instance.PlayerId;
                string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
                onComplete?.Invoke(true, null);
            }
            else
            {
                onComplete?.Invoke(false, "Initialization or Authentication failed");
            }
        }

        /// <summary>
        /// Garantiza la inicialización secuencial segura de UnityServices y AuthenticationService
        /// sin condiciones de carrera (Race Conditions) entre múltiples llamadas simultáneas.
        /// </summary>
        private async Task<bool> EnsureInitializedAndAuthenticatedAsync()
        {
            if (IsAuthenticated)
            {
                return true;
            }

            if (_initTask != null)
            {
                return await _initTask;
            }

            _initTask = DoInitAsync();
            return await _initTask;
        }

        private async Task<bool> DoInitAsync()
        {
            try
            {
                // 1. Inicializar Core Services
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                // Esperar si otro proceso está inicializando Core
                while (UnityServices.State == ServicesInitializationState.Initializing)
                {
                    await Task.Delay(50);
                }

                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    Debug.LogWarning("[UGSLeaderboard] UnityServices no pudo inicializarse correctamente.");
                    return false;
                }

                // 2. Iniciar sesión anónima como invitado
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                // 3. Configurar nombre legible (Callsign)
                await EnsurePlayerCallsignAsync();

                string playerId = AuthenticationService.Instance.PlayerId;
                string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
                Debug.Log($"<color=#55FF55><b>[UGSLeaderboard]</b></color> Conectado a UGS. Jugador: <color=#FFFF55>{playerName}</color> (ID: {playerId})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"<color=#FFAA00><b>[UGSLeaderboard]</b></color> Aviso al inicializar UGS (se usará modo local si estás sin internet o no has activado UGS en el dashboard): {ex.Message}");
                return false;
            }
            finally
            {
                _initTask = null;
            }
        }

        private async Task EnsurePlayerCallsignAsync()
        {
            try
            {
                string savedCallsign = PlayerPrefs.GetString(CALLSIGN_PREF_KEY, string.Empty);
                if (string.IsNullOrEmpty(savedCallsign))
                {
                    string shortId = AuthenticationService.Instance.PlayerId;
                    if (!string.IsNullOrEmpty(shortId) && shortId.Length >= 4)
                    {
                        shortId = shortId.Substring(0, 4).ToUpper();
                    }
                    else
                    {
                        shortId = UnityEngine.Random.Range(1000, 9999).ToString();
                    }

                    savedCallsign = "PILOT_" + shortId;
                    PlayerPrefs.SetString(CALLSIGN_PREF_KEY, savedCallsign);
                    PlayerPrefs.Save();
                }

                string currentRemoteName = await AuthenticationService.Instance.GetPlayerNameAsync();
                if (string.IsNullOrEmpty(currentRemoteName) || currentRemoteName.Contains("#"))
                {
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(savedCallsign);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGSLeaderboard] No se pudo sincronizar el callsign en la nube: {ex.Message}");
            }
        }

        public async void SubmitScore(long score, Action<bool> onComplete = null)
        {
            string leaderboardId = _config != null ? _config.ActiveLeaderboardId : "tc_global_highscores";

            try
            {
                bool isReady = await EnsureInitializedAndAuthenticatedAsync();
                if (!isReady)
                {
                    Debug.LogWarning("[UGSLeaderboard] No conectado a UGS. Puntuación conservada localmente.");
                    onComplete?.Invoke(false);
                    return;
                }

                Debug.Log($"[UGSLeaderboard] Enviando puntuación {score} a Leaderboard '{leaderboardId}'...");
                var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
                Debug.Log($"<color=#55FF55><b>[UGSLeaderboard]</b></color> Puntuación {score} subida con éxito! Rango actual: #{entry.Rank + 1}");
                onComplete?.Invoke(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGSLeaderboard] No se pudo enviar el récord a la nube UGS ({ex.Message}).");
                onComplete?.Invoke(false);
            }
        }

        public async void GetTopScores(int limit, Action<List<LeaderboardEntryData>> onComplete)
        {
            string leaderboardId = _config != null ? _config.ActiveLeaderboardId : "tc_global_highscores";
            List<LeaderboardEntryData> results = new List<LeaderboardEntryData>();

            try
            {
                bool isReady = await EnsureInitializedAndAuthenticatedAsync();

                if (isReady)
                {
                    var options = new GetScoresOptions { Limit = limit, Offset = 0 };
                    LeaderboardScoresPage scoresPage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);

                    if (scoresPage != null && scoresPage.Results != null && scoresPage.Results.Count > 0)
                    {
                        string localPlayerId = AuthenticationService.Instance.PlayerId;

                        for (int i = 0; i < scoresPage.Results.Count; i++)
                        {
                            var item = scoresPage.Results[i];
                            int rank = item.Rank + 1; // 0-indexed a 1-indexed
                            string name = !string.IsNullOrEmpty(item.PlayerName) ? CleanPlayerName(item.PlayerName) : $"PILOT_{item.PlayerId.Substring(0, Math.Min(4, item.PlayerId.Length)).ToUpper()}";
                            int score = (int)item.Score;
                            bool isLocal = item.PlayerId == localPlayerId;

                            results.Add(new LeaderboardEntryData(rank, name, score, isLocal));
                        }

                        Debug.Log($"<color=#55FF55><b>[UGSLeaderboard]</b></color> {results.Count} puntuaciones globales descargadas de UGS.");
                        onComplete?.Invoke(results);
                        return;
                    }
                    else
                    {
                        Debug.Log("<color=#00CCFF><b>[UGSLeaderboard]</b></color> El Leaderboard está conectado, pero aún no hay puntuaciones en la nube.");
                        results = GetLocalOnlyData();
                        onComplete?.Invoke(results);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGSLeaderboard] No se pudo consultar UGS ({ex.Message}). Mostrando récord local.");
            }

            // Solo mostrar los datos reales locales si está offline o sin registros
            results = GetLocalOnlyData();
            onComplete?.Invoke(results);
        }

        private string CleanPlayerName(string rawName)
        {
            int hashIndex = rawName.IndexOf('#');
            return hashIndex > 0 ? rawName.Substring(0, hashIndex) : rawName;
        }

        private List<LeaderboardEntryData> GetLocalOnlyData()
        {
            int localBest = PlayerPrefs.GetInt("TrueColors_HighScore", 0);
            string callsign = PlayerPrefs.GetString(CALLSIGN_PREF_KEY, "PILOT");

            var list = new List<LeaderboardEntryData>();
            if (localBest > 0)
            {
                list.Add(new LeaderboardEntryData(1, $"{callsign} (YOU)", localBest, isLocalPlayer: true));
            }
            return list;
        }
    }
}
