using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrueColors.Leaderboard.Services
{
    /// <summary>
    /// Servicio de simulación para Unity Editor y entornos de desarrollo/pruebas.
    /// Permite testear el ciclo completo de ranking sin requerir un dispositivo físico ni credenciales activas.
    /// </summary>
    public class EditorMockLeaderboardService : ILeaderboardService
    {
        private LeaderboardConfigSO _config;
        private bool _isAuthenticated;
        private long _simulatedBestScore;

        public bool IsAuthenticated => _isAuthenticated;

        public void Initialize(LeaderboardConfigSO config)
        {
            _config = config;
            _simulatedBestScore = PlayerPrefs.GetInt("TrueColors_HighScore", 2200);
            Debug.Log("<color=#00CCFF><b>[EditorLeaderboardMock]</b></color> Inicializado en modo Simulación (Unity Editor / PC).");

            if (_config != null && _config.AutoAuthenticateOnStart)
            {
                Authenticate();
            }
        }

        public void Authenticate(Action<bool, string> onComplete = null)
        {
            _isAuthenticated = true;
            Debug.Log("<color=#55FF55><b>[EditorLeaderboardMock]</b></color> Autenticación simulada exitosa. Jugador: <color=#FFFF55>EDITOR_TEST_PILOT</color> (ID: mock_usr_001)");
            onComplete?.Invoke(true, null);
        }

        public void SubmitScore(long score, Action<bool> onComplete = null)
        {
            string targetId = _config != null ? _config.ActiveLeaderboardId : "mock_leaderboard";
            if (score > _simulatedBestScore)
            {
                _simulatedBestScore = score;
            }

            Debug.Log($"<color=#55FF55><b>[EditorLeaderboardMock]</b></color> Puntuación <b>{score}</b> reportada exitosamente al Leaderboard '<b>{targetId}</b>' (Nuevo récord simulado: {_simulatedBestScore}).");
            onComplete?.Invoke(true);
        }

        public void GetTopScores(int limit, Action<List<LeaderboardEntryData>> onComplete)
        {
            var list = new List<LeaderboardEntryData>
            {
                new LeaderboardEntryData(1, "CYBER_PILOT", 3500),
                new LeaderboardEntryData(2, "NOVA_STRIKER", 3100),
                new LeaderboardEntryData(3, "COSMIC_ACE", 2800),
                new LeaderboardEntryData(4, "YOU (PILOT)", (int)_simulatedBestScore, isLocalPlayer: true),
                new LeaderboardEntryData(5, "STELLAR_FOX", 2400),
                new LeaderboardEntryData(6, "NEON_VORTEX", 2100),
                new LeaderboardEntryData(7, "QUANTUM_GHOST", 1950),
                new LeaderboardEntryData(8, "SHADOW_RUNNER", 1800),
                new LeaderboardEntryData(9, "ASTRO_KNIGHT", 1600),
                new LeaderboardEntryData(10, "HYPER_DRIVE", 1400)
            };

            // Ordenar descendentemente por puntuación
            list.Sort((a, b) => b.score.CompareTo(a.score));

            // Re-asignar rangos
            for (int i = 0; i < list.Count; i++)
            {
                list[i].rank = i + 1;
            }

            if (limit > 0 && list.Count > limit)
            {
                list = list.GetRange(0, limit);
            }

            onComplete?.Invoke(list);
        }
    }
}
