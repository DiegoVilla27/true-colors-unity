using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TrueColors.Leaderboard;

/// <summary>
/// Responsabilidad Única (SRP): Administra el modal de Ranking (Leaderboard) en MainMenu.
/// Gestiona la lista scroleable de posiciones de jugadores, consultando la nube de Unity Gaming Services (UGS)
/// de manera asíncrona y mostrando ÚNICAMENTE datos reales existentes con estrellas de distinción:
/// - Top 1: STAR_FULL
/// - Top 2: STAR_HALF
/// - Top 3 en adelante: STAR_EMPTY
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

    [Header("Estrellas de Clasificación")]
    [SerializeField] private Sprite starFullSprite;
    [SerializeField] private Sprite starHalfSprite;
    [SerializeField] private Sprite starEmptySprite;

    private readonly List<LeaderboardRowItem> activeRows = new List<LeaderboardRowItem>();

    public Sprite StarFullSprite => starFullSprite;
    public Sprite StarHalfSprite => starHalfSprite;
    public Sprite StarEmptySprite => starEmptySprite;

    void Awake()
    {
        WireButtons();
    }

    void OnEnable()
    {
        // 1. Ocultar inmediatamente todas las filas para evitar mostrar datos antiguos
        CollectAndHideAllRows();
        ResetScrollPosition();

        // 2. Si el jugador local ya tiene un récord personal real, mostrar su fila como punto de partida
        ShowInitialRealState();

        // 3. Consultar a Unity Gaming Services el ranking en vivo
        FetchLiveScores();
    }

    public void ConfigureStarSprites(Sprite full, Sprite half, Sprite empty)
    {
        starFullSprite = full;
        starHalfSprite = half;
        starEmptySprite = empty;
    }

    private void CollectAndHideAllRows()
    {
        if (contentContainer == null) return;

        if (activeRows.Count == 0)
        {
            var items = contentContainer.GetComponentsInChildren<LeaderboardRowItem>(true);
            foreach (var item in items)
            {
                if (rowTemplate != null && item.gameObject == rowTemplate) continue;
                activeRows.Add(item);
            }
        }

        for (int i = 0; i < activeRows.Count; i++)
        {
            if (activeRows[i] != null)
            {
                activeRows[i].gameObject.SetActive(false);
            }
        }

        if (rowTemplate != null)
        {
            rowTemplate.SetActive(false);
        }
    }

    private void ShowInitialRealState()
    {
        int localBest = PlayerPrefs.GetInt("TrueColors_HighScore", 0);
        string callsign = PlayerPrefs.GetString("TrueColors_PlayerCallsign", "PILOT");

        var realList = new List<LeaderboardEntryData>();
        if (localBest > 0)
        {
            realList.Add(new LeaderboardEntryData(1, $"{callsign} (TU RÉCORD)", localBest, isLocalPlayer: true));
        }

        PopulateLeaderboard(realList);
    }

    private void FetchLiveScores()
    {
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.GetTopScores(10, (liveEntries) =>
            {
                if (this != null && gameObject.activeInHierarchy)
                {
                    if (liveEntries != null && liveEntries.Count > 0)
                    {
                        PopulateLeaderboard(liveEntries);
                    }
                    else
                    {
                        ShowInitialRealState();
                    }
                }
            });
        }
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
    /// Asigna la estrella adecuada según el puesto en el ranking:
    /// - 1º: STAR_FULL
    /// - 2º: STAR_HALF
    /// - 3º en adelante: STAR_EMPTY
    /// </summary>
    private Sprite GetStarForRank(int rank)
    {
        if (rank == 1) return starFullSprite;
        if (rank == 2) return starHalfSprite;
        return starEmptySprite;
    }

    /// <summary>
    /// Puebla la lista scroleable activando ÚNICAMENTE las filas que tienen datos reales existentes.
    /// </summary>
    public void PopulateLeaderboard(List<LeaderboardEntryData> entries)
    {
        if (contentContainer == null) return;

        if (rowTemplate != null) rowTemplate.SetActive(false);

        // Asegurar que activeRows contenga todas las filas hijas del contenedor
        if (activeRows.Count == 0)
        {
            var items = contentContainer.GetComponentsInChildren<LeaderboardRowItem>(true);
            foreach (var item in items)
            {
                if (rowTemplate != null && item.gameObject == rowTemplate) continue;
                activeRows.Add(item);
            }
        }

        int targetCount = entries != null ? entries.Count : 0;

        // Instanciar únicamente si se requieren más filas de las ya existentes
        while (activeRows.Count < targetCount)
        {
            if (rowTemplate == null) break;
            GameObject newRow = Instantiate(rowTemplate, contentContainer);
            var item = newRow.GetComponent<LeaderboardRowItem>();
            activeRows.Add(item);
        }

        // Configurar y activar SOLO las filas reales
        for (int i = 0; i < activeRows.Count; i++)
        {
            if (i < targetCount && entries != null)
            {
                activeRows[i].gameObject.SetActive(true);
                var entry = entries[i];
                Sprite star = GetStarForRank(entry.rank);
                activeRows[i].SetData(entry.rank, entry.username, entry.score, entry.isLocalPlayer, star);
            }
            else
            {
                // OCULTAR cualquier fila que no tenga un jugador real
                activeRows[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Sobrecarga de compatibilidad con versiones anteriores.
    /// </summary>
    public void PopulateLeaderboard(List<LeaderboardEntry> entries)
    {
        if (entries == null) return;
        var converted = new List<LeaderboardEntryData>();
        for (int i = 0; i < entries.Count; i++)
        {
            converted.Add(new LeaderboardEntryData(i + 1, entries[i].username, entries[i].score));
        }
        PopulateLeaderboard(converted);
    }

    public void ResetScrollPosition()
    {
        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
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
