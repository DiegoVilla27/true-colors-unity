using System;

namespace TrueColors.Leaderboard
{
    /// <summary>
    /// Modelo de datos estándar para una entrada de Leaderboard.
    /// Desacoplado de la API específica de UGS o proveedores externos.
    /// </summary>
    [Serializable]
    public class LeaderboardEntryData
    {
        public int rank;
        public string username;
        public int score;
        public bool isLocalPlayer;

        public LeaderboardEntryData(int rank, string username, int score, bool isLocalPlayer = false)
        {
            this.rank = rank;
            this.username = username;
            this.score = score;
            this.isLocalPlayer = isLocalPlayer;
        }
    }
}
