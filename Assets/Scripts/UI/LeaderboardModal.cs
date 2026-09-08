using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Administra el modal de Ranking (Leaderboard) en MainMenu.
/// Gestiona la lista scroleable de posiciones de jugadores, poblando inicialmente datos mockeados
/// desacoplados y listos para ser consumidos desde un microservicio / DB remota.
/// </summary>
public class LeaderboardModal : MonoBehaviour
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string username;
        public int score;

        public LeaderboardEntry(string username, int score)
        {
            this.username = username;
            this.score = score;
        }
    }

    [Header("Componentes de Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private GameObject rowTemplate;

    [Header("Botón Inferior de Cierre")]
    [SerializeField] private Button closeButton;

    // Lista hardcodeada de ejemplo para simular los registros de la DB del microservicio
    private static readonly List<LeaderboardEntry> DefaultMockData = new List<LeaderboardEntry>
    {
        new LeaderboardEntry("DIEGOVILLA92", 3200),
        new LeaderboardEntry("CYBER_PILOT", 2950),
        new LeaderboardEntry("NOVA_STRIKER", 2700),
        new LeaderboardEntry("COSMIC_ACE", 2520),
        new LeaderboardEntry("STELLAR_FOX", 2310),
        new LeaderboardEntry("NEON_VORTEX", 2100),
        new LeaderboardEntry("QUANTUM_GHOST", 1980),
        new LeaderboardEntry("SHADOW_RUNNER", 1850),
        new LeaderboardEntry("ASTRO_KNIGHT", 1620),
        new LeaderboardEntry("HYPER_DRIVE", 1400)
    };

    private readonly List<LeaderboardRowItem> activeRows = new List<LeaderboardRowItem>();

    void Awake()
    {
        WireButtons();
    }

    void OnEnable()
    {
        // TODO: Cuando el microservicio esté disponible, llamar a la API/DB remota aquí.
        // Mientras tanto, se puebla con la lista hardcodeada predeterminada.
        PopulateLeaderboard(DefaultMockData);
        ResetScrollPosition();
    }

    private void WireButtons()
    {
        if (closeButton == null) return;

        for (int i = 0; i < closeButton.onClick.GetPersistentEventCount(); i++)
        {
            if (closeButton.onClick.GetPersistentTarget(i) == (UnityEngine.Object)this &&
                closeButton.onClick.GetPersistentMethodName(i) == nameof(Close))
            {
                return;
            }
        }

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
    }

    /// <summary>
    /// Puebla la lista scroleable con cualquier colección de entradas de ranking.
    /// Diseñado para integrarse transparentemente con microservicios externos o DB remota.
    /// </summary>
    public void PopulateLeaderboard(List<LeaderboardEntry> entries)
    {
        if (rowTemplate == null || contentContainer == null) return;

        // Ocultar plantilla base
        rowTemplate.SetActive(false);

        int targetCount = entries != null ? entries.Count : 0;

        // Reutilizar o instanciar filas necesarias
        while (activeRows.Count < targetCount)
        {
            GameObject newRow = Instantiate(rowTemplate, contentContainer);
            var item = newRow.GetComponent<LeaderboardRowItem>();
            activeRows.Add(item);
        }

        // Configurar datos y activar filas
        for (int i = 0; i < activeRows.Count; i++)
        {
            if (i < targetCount)
            {
                activeRows[i].gameObject.SetActive(true);
                activeRows[i].SetData(entries[i].username, entries[i].score);
            }
            else
            {
                activeRows[i].gameObject.SetActive(false);
            }
        }
    }

    public void ResetScrollPosition()
    {
        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f; // Scroll al inicio
        }
    }

    public void Close()
    {
        if (HapticFeedback.IsVibrationEnabled)
        {
            HapticFeedback.VibrateCollect();
        }

        var menuController = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        if (menuController != null)
        {
            menuController.CloseLeaderboard();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
