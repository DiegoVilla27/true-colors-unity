using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.SocialPlatforms;
using UnityEngine.SocialPlatforms.GameCenter;
#endif

namespace TrueColors.Leaderboard.Services
{
    /// <summary>
    /// Implementación nativa para Apple Game Center (iOS).
    /// Utiliza UnityEngine.SocialPlatforms y GameCenterPlatform nativo de iOS.
    /// </summary>
    public class iOSGameCenterService : ILeaderboardService
    {
        private LeaderboardConfigSO _config;
        private bool _isAuthenticating;

        public bool IsAuthenticated
        {
            get
            {
#if UNITY_IOS
                return Social.localUser != null && Social.localUser.authenticated;
#else
                return false;
#endif
            }
        }

        public void Initialize(LeaderboardConfigSO config)
        {
            _config = config;
            Debug.Log("[GameCenterService] Inicializado para iOS.");

            if (_config != null && _config.AutoAuthenticateOnStart)
            {
                Authenticate();
            }
        }

        public void Authenticate(Action<bool, string> onComplete = null)
        {
#if UNITY_IOS
            if (IsAuthenticated)
            {
                Debug.Log("[GameCenterService] El usuario ya está autenticado en Game Center: " + Social.localUser.userName);
                onComplete?.Invoke(true, null);
                return;
            }

            if (_isAuthenticating)
            {
                Debug.Log("[GameCenterService] Proceso de autenticación ya en curso...");
                return;
            }

            _isAuthenticating = true;
            Debug.Log("[GameCenterService] Iniciando autenticación en Apple Game Center...");

            Social.localUser.Authenticate((bool success, string error) =>
            {
                _isAuthenticating = false;
                if (success)
                {
                    Debug.Log($"<color=#55FF55>[GameCenterService] Autenticación exitosa! Usuario: {Social.localUser.userName} (ID: {Social.localUser.id})</color>");
                }
                else
                {
                    Debug.LogWarning($"[GameCenterService] Falló autenticación en Game Center: {error}");
                }
                onComplete?.Invoke(success, error);
            });
#else
            Debug.LogWarning("[GameCenterService] Game Center solo está disponible en plataformas iOS.");
            onComplete?.Invoke(false, "Not iOS platform");
#endif
        }

        public void SubmitScore(long score, Action<bool> onComplete = null)
        {
#if UNITY_IOS
            string leaderboardId = _config != null ? _config.IosLeaderboardId : string.Empty;

            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogWarning("[GameCenterService] No se puede enviar puntuación: iosLeaderboardId está vacío en LeaderboardConfig.");
                onComplete?.Invoke(false);
                return;
            }

            if (!IsAuthenticated)
            {
                Debug.Log("[GameCenterService] Usuario no autenticado. Intentando autenticar antes de enviar puntuación...");
                Authenticate((success, error) =>
                {
                    if (success)
                    {
                        DoSubmitScore(score, leaderboardId, onComplete);
                    }
                    else
                    {
                        Debug.LogWarning("[GameCenterService] No se pudo enviar el récord porque falló la autenticación en Game Center.");
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

#if UNITY_IOS
        private void DoSubmitScore(long score, string leaderboardId, Action<bool> onComplete)
        {
            Debug.Log($"[GameCenterService] Enviando puntuación {score} al Leaderboard '{leaderboardId}'...");
            Social.ReportScore(score, leaderboardId, (bool success) =>
            {
                if (success)
                {
                    Debug.Log($"<color=#55FF55>[GameCenterService] Puntuación {score} enviada a Game Center exitosamente!</color>");
                }
                else
                {
                    Debug.LogWarning($"[GameCenterService] Error al reportar puntuación {score} a Game Center.");
                }
                onComplete?.Invoke(success);
            });
        }
#endif

        public void ShowLeaderboardUI()
        {
#if UNITY_IOS
            string leaderboardId = _config != null ? _config.IosLeaderboardId : string.Empty;

            if (!IsAuthenticated)
            {
                Debug.Log("[GameCenterService] Usuario no autenticado. Autenticando antes de desplegar UI nativa...");
                Authenticate((success, error) =>
                {
                    if (success)
                    {
                        ShowNativeOverlay(leaderboardId);
                    }
                    else
                    {
                        Debug.LogWarning("[GameCenterService] No se puede mostrar el Leaderboard nativo sin autenticación.");
                    }
                });
                return;
            }

            ShowNativeOverlay(leaderboardId);
#else
            Debug.LogWarning("[GameCenterService] ShowLeaderboardUI solo está disponible en iOS.");
#endif
        }

#if UNITY_IOS
        private void ShowNativeOverlay(string leaderboardId)
        {
            Debug.Log($"[GameCenterService] Mostrando overlay oficial de Game Center (Leaderboard: '{leaderboardId}')...");
            if (!string.IsNullOrEmpty(leaderboardId))
            {
                GameCenterPlatform.ShowDefaultLeaderboardCompletionHandler = () =>
                {
                    Debug.Log("[GameCenterService] Overlay de Game Center cerrado por el usuario.");
                };
                GameCenterPlatform.ShowLeaderboardUI(leaderboardId, UnityEngine.SocialPlatforms.TimeScope.AllTime);
            }
            else
            {
                Social.ShowLeaderboardUI();
            }
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
