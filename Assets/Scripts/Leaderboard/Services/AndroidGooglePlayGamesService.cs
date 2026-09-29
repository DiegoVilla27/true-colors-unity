using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TrueColors.Leaderboard.Services
{
    /// <summary>
    /// Implementación nativa para Google Play Games Services (Android).
    /// Soporta tanto el plugin oficial GooglePlayGames si está importado en el proyecto,
    /// como la interfaz estándar UnityEngine.SocialPlatforms.
    /// </summary>
    public class AndroidGooglePlayGamesService : ILeaderboardService
    {
        private LeaderboardConfigSO _config;
        private bool _isAuthenticating;
        private bool _gpgsPluginDetected;

        public bool IsAuthenticated
        {
            get
            {
#if UNITY_ANDROID
                return Social.localUser != null && Social.localUser.authenticated;
#else
                return false;
#endif
            }
        }

        public void Initialize(LeaderboardConfigSO config)
        {
            _config = config;
            Debug.Log("[GooglePlayGamesService] Inicializando para plataforma Android...");

            TryInitializeGooglePlayGamesPlugin();

            if (_config != null && _config.AutoAuthenticateOnStart)
            {
                Authenticate();
            }
        }

        /// <summary>
        /// Detecta dinámicamente si el paquete Google Play Games Plugin for Unity está importado
        /// y lo activa sin generar errores de compilación si aún no ha sido instalado.
        /// </summary>
        private void TryInitializeGooglePlayGamesPlugin()
        {
#if UNITY_ANDROID
            try
            {
                Type playGamesPlatformType = Type.GetType("GooglePlayGames.PlayGamesPlatform, GooglePlayGames");
                if (playGamesPlatformType == null)
                {
                    // Buscar en los ensamblados cargados
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        playGamesPlatformType = assembly.GetType("GooglePlayGames.PlayGamesPlatform");
                        if (playGamesPlatformType != null) break;
                    }
                }

                if (playGamesPlatformType != null)
                {
                    MethodInfo activateMethod = playGamesPlatformType.GetMethod("Activate", BindingFlags.Public | BindingFlags.Static);
                    if (activateMethod != null)
                    {
                        activateMethod.Invoke(null, null);
                        _gpgsPluginDetected = true;
                        Debug.Log("<color=#55FF55>[GooglePlayGamesService] Plugin oficial GooglePlayGames detectado y activado exitosamente.</color>");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GooglePlayGamesService] Excepción al intentar activar GPGS plugin: {ex.Message}");
            }

            Debug.Log("[GooglePlayGamesService] Usando UnityEngine.SocialPlatforms estándar para Android.");
#endif
        }

        public void Authenticate(Action<bool, string> onComplete = null)
        {
#if UNITY_ANDROID
            if (IsAuthenticated)
            {
                Debug.Log("[GooglePlayGamesService] Usuario ya autenticado en Google Play: " + Social.localUser.userName);
                onComplete?.Invoke(true, null);
                return;
            }

            if (_isAuthenticating)
            {
                Debug.Log("[GooglePlayGamesService] Autenticación ya en progreso...");
                return;
            }

            _isAuthenticating = true;
            Debug.Log("[GooglePlayGamesService] Iniciando sesión en Google Play Games Services...");

            Social.localUser.Authenticate((bool success, string error) =>
            {
                _isAuthenticating = false;
                if (success)
                {
                    Debug.Log($"<color=#55FF55>[GooglePlayGamesService] Autenticado en Google Play! Jugador: {Social.localUser.userName} (ID: {Social.localUser.id})</color>");
                }
                else
                {
                    Debug.LogWarning($"[GooglePlayGamesService] Error de autenticación en Google Play: {error}");
                }
                onComplete?.Invoke(success, error);
            });
#else
            Debug.LogWarning("[GooglePlayGamesService] Google Play Games solo está disponible en dispositivos Android.");
            onComplete?.Invoke(false, "Not Android platform");
#endif
        }

        public void SubmitScore(long score, Action<bool> onComplete = null)
        {
#if UNITY_ANDROID
            string leaderboardId = _config != null ? _config.AndroidLeaderboardId : string.Empty;

            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogWarning("[GooglePlayGamesService] No se puede reportar récord: androidLeaderboardId no está configurado en LeaderboardConfig.");
                onComplete?.Invoke(false);
                return;
            }

            if (!IsAuthenticated)
            {
                Debug.Log("[GooglePlayGamesService] Autenticando antes de enviar puntuación a Google Play...");
                Authenticate((success, error) =>
                {
                    if (success)
                    {
                        DoSubmitScore(score, leaderboardId, onComplete);
                    }
                    else
                    {
                        Debug.LogWarning("[GooglePlayGamesService] No se pudo enviar el récord por fallo en autenticación.");
                        onComplete?.Invoke(false);
                    }
                });
                return;
            }

            DoSubmitScore(score, leaderboardId, onComplete);
#else
            onComplete?.Invoke(false);
#endif
        }

#if UNITY_ANDROID
        private void DoSubmitScore(long score, string leaderboardId, Action<bool> onComplete)
        {
            Debug.Log($"[GooglePlayGamesService] Enviando puntuación {score} a Leaderboard '{leaderboardId}'...");
            Social.ReportScore(score, leaderboardId, (bool success) =>
            {
                if (success)
                {
                    Debug.Log($"<color=#55FF55>[GooglePlayGamesService] Puntuación {score} registrada exitosamente en Google Play Games!</color>");
                }
                else
                {
                    Debug.LogWarning($"[GooglePlayGamesService] Error al registrar puntuación en Google Play.");
                }
                onComplete?.Invoke(success);
            });
        }
#endif

        public void ShowLeaderboardUI()
        {
#if UNITY_ANDROID
            string leaderboardId = _config != null ? _config.AndroidLeaderboardId : string.Empty;

            if (!IsAuthenticated)
            {
                Debug.Log("[GooglePlayGamesService] Autenticando usuario antes de abrir overlay nativo...");
                Authenticate((success, error) =>
                {
                    if (success)
                    {
                        ShowNativeOverlay(leaderboardId);
                    }
                    else
                    {
                        Debug.LogWarning("[GooglePlayGamesService] No se puede desplegar UI nativa de Google Play sin autenticación.");
                    }
                });
                return;
            }

            ShowNativeOverlay(leaderboardId);
#else
            Debug.LogWarning("[GooglePlayGamesService] ShowLeaderboardUI solo está disponible en Android.");
#endif
        }

#if UNITY_ANDROID
        private void ShowNativeOverlay(string leaderboardId)
        {
            Debug.Log($"[GooglePlayGamesService] Abriendo overlay nativo de Google Play Games...");

            // Si detectamos el plugin GPGS, intentar llamar a ShowLeaderboardUI con el ID específico
            if (_gpgsPluginDetected && !string.IsNullOrEmpty(leaderboardId))
            {
                try
                {
                    Type playGamesPlatformType = Type.GetType("GooglePlayGames.PlayGamesPlatform, GooglePlayGames");
                    if (playGamesPlatformType != null)
                    {
                        PropertyInfo instanceProp = playGamesPlatformType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                        object instance = instanceProp?.GetValue(null);
                        if (instance != null)
                        {
                            MethodInfo showLeaderboardMethod = playGamesPlatformType.GetMethod("ShowLeaderboardUI", new[] { typeof(string) });
                            if (showLeaderboardMethod != null)
                            {
                                showLeaderboardMethod.Invoke(instance, new object[] { leaderboardId });
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GooglePlayGamesService] Error invocando ShowLeaderboardUI en plugin GPGS: {ex.Message}");
                }
            }

            // Fallback a interfaz estándar de Unity Social
            Social.ShowLeaderboardUI();
        }
#endif

        public void GetTopScores(int limit, Action<List<LeaderboardEntryData>> onComplete)
        {
            var fallback = new List<LeaderboardEntryData>
            {
                new LeaderboardEntryData(1, "CYBER_PILOT", 3500),
                new LeaderboardEntryData(2, "NOVA_STRIKER", 3100),
                new LeaderboardEntryData(3, "COSMIC_ACE", 2800),
                new LeaderboardEntryData(4, "STELLAR_FOX", 2400),
                new LeaderboardEntryData(5, "NEON_VORTEX", 2100)
            };
            onComplete?.Invoke(fallback);
        }
    }
}
