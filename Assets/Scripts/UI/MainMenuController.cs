using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
  [Header("Panels")]
  [SerializeField] private GameObject optionsPanel;

  void Awake()
  {
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = 60;
  }

  public void PlayGame()
  {
    // Carga la escena del juego
    SceneManager.LoadScene("MainGame");
  }

  public void OpenOptions()
  {
    if (optionsPanel != null) optionsPanel.SetActive(true);
  }

  public void CloseOptions()
  {
    if (optionsPanel != null) optionsPanel.SetActive(false);
  }
}
