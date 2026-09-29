using System;
using System.Collections.Generic;

namespace TrueColors.Leaderboard
{
    /// <summary>
    /// Contrato arquitectónico para proveedores de Leaderboard y ranking.
    /// Desacopla la lógica del juego de las APIs específicas del backend o sistema operativo.
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>
        /// Inicializa el servicio con la configuración provista.
        /// </summary>
        void Initialize(LeaderboardConfigSO config);

        /// <summary>
        /// Autentica al usuario en el servicio correspondiente.
        /// </summary>
        void Authenticate(Action<bool, string> onComplete = null);

        /// <summary>
        /// Envía una puntuación al leaderboard activo.
        /// </summary>
        void SubmitScore(long score, Action<bool> onComplete = null);

        /// <summary>
        /// Obtiene de forma asíncrona la lista de los mejores puntajes globales.
        /// </summary>
        void GetTopScores(int limit, Action<List<LeaderboardEntryData>> onComplete);

        /// <summary>
        /// Indica si el usuario está actualmente autenticado en el servicio.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
