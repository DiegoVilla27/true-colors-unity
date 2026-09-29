using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Gestiona la presentación visual de una fila individual
/// en la lista scroleable del Leaderboard (Ranking).
/// </summary>
public class LeaderboardRowItem : MonoBehaviour
{
    [Header("Elementos de Fila")]
    [SerializeField] private Image highlightBg;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image starImage;

    public Image HighlightBg => highlightBg;
    public Image AvatarImage => avatarImage;
    public TextMeshProUGUI UsernameText => usernameText;
    public TextMeshProUGUI ScoreText => scoreText;
    public Image StarImage => starImage;

    /// <summary>
    /// Configura los datos de rango, usuario, puntuación, estrella e indicador de jugador local.
    /// </summary>
    public void SetData(int rank, string username, int score, bool isLocalPlayer = false, Sprite star = null, Sprite avatar = null)
    {
        string rankPrefix = rank > 0 ? $"{rank}. " : string.Empty;
        if (usernameText != null)
        {
            usernameText.text = $"{rankPrefix}{username}";
            if (isLocalPlayer)
            {
                usernameText.color = new Color(1f, 0.9f, 0.2f, 1f); // Amarillo dorado para el jugador local
            }
            else
            {
                usernameText.color = Color.white;
            }
        }

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        if (starImage != null)
        {
            if (star != null)
            {
                starImage.sprite = star;
                starImage.enabled = true;
            }
            else
            {
                starImage.enabled = false;
            }
        }

        if (avatar != null && avatarImage != null)
        {
            avatarImage.sprite = avatar;
        }

        if (highlightBg != null)
        {
            highlightBg.enabled = isLocalPlayer;
        }
    }

    /// <summary>
    /// Sobrecarga compatible con versiones anteriores sin estrella.
    /// </summary>
    public void SetData(int rank, string username, int score, bool isLocalPlayer, Sprite avatar)
    {
        SetData(rank, username, score, isLocalPlayer, null, avatar);
    }

    /// <summary>
    /// Sobrecarga compatible con versiones anteriores sin rango.
    /// </summary>
    public void SetData(string username, int score, Sprite avatar = null)
    {
        SetData(0, username, score, false, null, avatar);
    }

    public void ConfigureReferences(Image highlight, Image avatar, TextMeshProUGUI user, TextMeshProUGUI score, Image star)
    {
        highlightBg = highlight;
        avatarImage = avatar;
        usernameText = user;
        scoreText = score;
        starImage = star;
    }
}
