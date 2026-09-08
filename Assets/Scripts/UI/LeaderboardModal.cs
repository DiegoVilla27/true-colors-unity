using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Administra el modal de Ranking (Leaderboard) en MainMenu.
/// Controla la visualización de los datos de los jugadores en la tabla de clasificación
/// y la navegación de cierre hacia el menú principal.
/// </summary>
public class LeaderboardModal : MonoBehaviour
{
    [Serializable]
    public struct LeaderboardRowUI
    {
        public Image highlightBg;
        public Image avatarImage;
        public TextMeshProUGUI usernameText;
        public TextMeshProUGUI scoreText;
        public Image starImage;
    }

    [Header("Filas de la Tabla de Clasificación")]
    [SerializeField] private LeaderboardRowUI[] rows;

    [Header("Botón Inferior de Cierre")]
    [SerializeField] private Button closeButton;

    void Awake()
    {
        WireButtons();
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
    /// Configura los datos de una fila específica del ranking.
    /// </summary>
    public void SetRowData(int rowIndex, string username, int score)
    {
        if (rows == null || rowIndex < 0 || rowIndex >= rows.Length) return;

        if (rows[rowIndex].usernameText != null)
            rows[rowIndex].usernameText.text = username;

        if (rows[rowIndex].scoreText != null)
            rows[rowIndex].scoreText.text = score.ToString();
    }

    /// <summary>
    /// Cierra el modal de Ranking y regresa al MainMenu.
    /// </summary>
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
