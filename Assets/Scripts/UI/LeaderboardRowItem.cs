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
    /// Configura los datos de usuario y puntuación en la fila.
    /// </summary>
    public void SetData(string username, int score, Sprite avatar = null)
    {
        if (usernameText != null) usernameText.text = username;
        if (scoreText != null) scoreText.text = score.ToString();
        if (avatar != null && avatarImage != null) avatarImage.sprite = avatar;
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
