using UnityEngine;

namespace TrueColors.Leaderboard
{
    [CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "TrueColors/Leaderboard Config")]
    public class LeaderboardConfigSO : ScriptableObject
    {
        [Header("Unity Gaming Services (Cross-Platform 100% Gratuito)")]
        [Tooltip("ID del Leaderboard en Unity Cloud Dashboard (ej. 'tc_global_highscores')")]
        [SerializeField] private string ugsLeaderboardId = "tc_global_highscores";

        [Header("Apple Game Center (iOS - Opcional)")]
        [Tooltip("ID del Leaderboard configurado en App Store Connect")]
        [SerializeField] private string iosLeaderboardId = "tc_global_highscores";

        [Header("Google Play Games Services (Android - Opcional)")]
        [Tooltip("ID del Leaderboard generado en Google Play Console")]
        [SerializeField] private string androidLeaderboardId = "";

        [Header("Comportamiento")]
        [Tooltip("Si está activo, intenta autenticar silenciosamente al usuario al iniciar el juego.")]
        [SerializeField] private bool autoAuthenticateOnStart = true;

        public string UgsLeaderboardId => ugsLeaderboardId;
        public string IosLeaderboardId => iosLeaderboardId;
        public string AndroidLeaderboardId => androidLeaderboardId;
        public bool AutoAuthenticateOnStart => autoAuthenticateOnStart;

        /// <summary>
        /// Obtiene el ID del leaderboard correspondiente para UGS o plataforma activa.
        /// </summary>
        public string ActiveLeaderboardId => string.IsNullOrEmpty(ugsLeaderboardId) ? "tc_global_highscores" : ugsLeaderboardId;
    }
}
