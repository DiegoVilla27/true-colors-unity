using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador principal de navegación para la escena de bienvenida (MainMenu).
/// Gestiona la transición al juego, paneles auxiliares (Opciones, Tienda, Récords),
/// retroalimentación háptica y auto-cableado del efecto de hundimiento en botones.
/// </summary>
public class MainMenuController : MonoBehaviour
{
  [Header("Paneles Auxiliares")]
  [SerializeField] private GameObject optionsPanel;
  [SerializeField] private GameObject shopPanel;
  [SerializeField] private GameObject leaderboardPanel;

  void Awake()
  {
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = 60;
  }

  void Start()
  {
    AutoWireButtonPressEffects();
  }

  private void AutoWireButtonPressEffects()
  {
    var buttons = FindObjectsByType<Button>();
    for (int i = 0; i < buttons.Length; i++)
    {
      if (buttons[i].GetComponent<UIButtonPressEffect>() == null)
      {
        buttons[i].gameObject.AddComponent<UIButtonPressEffect>();
      }
    }
  }

  public void PlayGame()
  {
    HapticFeedback.VibrateCollect();
    SceneManager.LoadScene("MainGame");
  }

  public void OpenOptions()
  {
    HapticFeedback.VibrateCollect();
    if (optionsPanel != null) optionsPanel.SetActive(true);
    else Debug.Log("[MainMenu] Panel de opciones activado.");
  }

  public void CloseOptions()
  {
    HapticFeedback.VibrateCollect();
    if (optionsPanel != null) optionsPanel.SetActive(false);
  }

  public void OpenShop()
  {
    HapticFeedback.VibrateCollect();
    if (shopPanel != null) shopPanel.SetActive(true);
    else Debug.Log("[MainMenu] Tienda activada (Próximamente).");
  }

  public void OpenLeaderboard()
  {
    HapticFeedback.VibrateCollect();
    if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
    else Debug.Log("[MainMenu] Leaderboard activado (Próximamente).");
  }
}
