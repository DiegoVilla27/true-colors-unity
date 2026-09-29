using System;
using System.Collections.Generic;
using UnityEngine;
using TrueColors.Leaderboard.Services;

namespace TrueColors.Leaderboard
{
    /// <summary>
    /// Administrador central de Leaderboards para TrueColors.
    /// Orquesta el backend de Unity Gaming Services (UGS) para ranking global unificado y gratuito,
    /// gestionando la autenticación silenciosa y la sincronización con la UI in-game.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        private static LeaderboardManager instance;
        public static LeaderboardManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<LeaderboardManager>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("[LeaderboardManager]");
                        instance = obj.AddComponent<LeaderboardManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return instance;
            }
        }

        private const string CONFIG_RESOURCE_PATH = "LeaderboardConfig";

        [Header("Configuración")]
        [SerializeField] private LeaderboardConfigSO config;

        private ILeaderboardService _service;

        public bool IsAuthenticated => _service != null && _service.IsAuthenticated;
        public LeaderboardConfigSO Config => config;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeService();
        }

        private void InitializeService()
        {
            if (config == null)
            {
                config = Resources.Load<LeaderboardConfigSO>(CONFIG_RESOURCE_PATH);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<LeaderboardConfigSO>();
                    Debug.LogWarning("[LeaderboardManager] No se encontró 'Resources/LeaderboardConfig'. Usando configuración predeterminada en memoria.");
                }
            }

            // Usar Unity Gaming Services (UGS) para ranking global cross-platform gratuito
            _service = new UGSLeaderboardService();
            _service.Initialize(config);
        }

        /// <summary>
        /// Solicita autenticación del jugador en la plataforma activa.
        /// </summary>
        public void Authenticate(Action<bool, string> onComplete = null)
        {
            if (_service == null)
            {
                Debug.LogWarning("[LeaderboardManager] El servicio de leaderboard no está inicializado.");
                onComplete?.Invoke(false, "Service not initialized");
                return;
            }

            _service.Authenticate(onComplete);
        }

        /// <summary>
        /// Envía el puntaje del jugador al leaderboard global en la nube.
        /// </summary>
        public void SubmitScore(int score, Action<bool> onComplete = null)
        {
            if (_service == null)
            {
                Debug.LogWarning("[LeaderboardManager] El servicio de leaderboard no está inicializado.");
                onComplete?.Invoke(false);
                return;
            }

            _service.SubmitScore(score, onComplete);
        }

        /// <summary>
        /// Obtiene de forma asíncrona las mejores puntuaciones globales para poblar el modal.
        /// </summary>
        public void GetTopScores(int limit, Action<List<LeaderboardEntryData>> onComplete)
        {
            if (_service == null)
            {
                Debug.LogWarning("[LeaderboardManager] El servicio de leaderboard no está inicializado.");
                onComplete?.Invoke(new List<LeaderboardEntryData>());
                return;
            }

            _service.GetTopScores(limit, onComplete);
        }
    }
}
