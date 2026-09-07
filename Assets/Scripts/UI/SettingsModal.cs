using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Administra el modal de ajustes/configuración en juego,
/// controlando las preferencias de Sonido, Música y Vibración con persistencia en PlayerPrefs.
/// </summary>
public class SettingsModal : MonoBehaviour
{
  public const string SOUND_KEY = "Setting_Sound";
  public const string MUSIC_KEY = "Setting_Music";

  [Header("Botones de Control")]
  [SerializeField] private Button soundButton;
  [SerializeField] private Button musicButton;
  [SerializeField] private Button vibrationButton;
  [SerializeField] private Button closeButton;

  [Header("Iconos o Indicadores")]
  [SerializeField] private Image soundIcon;
  [SerializeField] private Image musicIcon;
  [SerializeField] private Image vibrationIcon;

  [Header("Textos de Estado (Opcionales)")]
  [SerializeField] private TextMeshProUGUI soundStatusText;
  [SerializeField] private TextMeshProUGUI musicStatusText;
  [SerializeField] private TextMeshProUGUI vibrationStatusText;

  private bool soundEnabled = true;
  private bool musicEnabled = true;
  private bool vibrationEnabled = true;

  private readonly Color activeColor = Color.white;
  private readonly Color inactiveColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);

  void Awake()
  {
    LoadPreferences();
    WireButtons();
  }

  void OnEnable()
  {
    LoadPreferences();
    UpdateUI();
  }

  private void LoadPreferences()
  {
    soundEnabled = PlayerPrefs.GetInt(SOUND_KEY, 1) == 1;
    musicEnabled = PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
    vibrationEnabled = HapticFeedback.IsVibrationEnabled;

    ApplySoundSetting();
  }

  private void WireButtons()
  {
    if (soundButton != null)
    {
      soundButton.onClick.RemoveListener(ToggleSound);
      soundButton.onClick.AddListener(ToggleSound);
      EnsurePressEffect(soundButton.gameObject);
    }

    if (musicButton != null)
    {
      musicButton.onClick.RemoveListener(ToggleMusic);
      musicButton.onClick.AddListener(ToggleMusic);
      EnsurePressEffect(musicButton.gameObject);
    }

    if (vibrationButton != null)
    {
      vibrationButton.onClick.RemoveListener(ToggleVibration);
      vibrationButton.onClick.AddListener(ToggleVibration);
      EnsurePressEffect(vibrationButton.gameObject);
    }

    if (closeButton != null)
    {
      closeButton.onClick.RemoveListener(Close);
      closeButton.onClick.AddListener(Close);
      EnsurePressEffect(closeButton.gameObject);
    }
  }

  private void EnsurePressEffect(GameObject target)
  {
    if (target != null && target.GetComponent<UIButtonPressEffect>() == null)
    {
      target.AddComponent<UIButtonPressEffect>();
    }
  }

  public void ToggleSound()
  {
    soundEnabled = !soundEnabled;
    PlayerPrefs.SetInt(SOUND_KEY, soundEnabled ? 1 : 0);
    PlayerPrefs.Save();

    ApplySoundSetting();
    UpdateUI();

    if (vibrationEnabled) HapticFeedback.VibrateCollect();
  }

  public void ToggleMusic()
  {
    musicEnabled = !musicEnabled;
    PlayerPrefs.SetInt(MUSIC_KEY, musicEnabled ? 1 : 0);
    PlayerPrefs.Save();

    UpdateUI();

    if (vibrationEnabled) HapticFeedback.VibrateCollect();
  }

  public void ToggleVibration()
  {
    vibrationEnabled = !vibrationEnabled;
    HapticFeedback.IsVibrationEnabled = vibrationEnabled;

    UpdateUI();

    if (vibrationEnabled) HapticFeedback.VibrateCollect();
  }

  private void ApplySoundSetting()
  {
    AudioListener.volume = soundEnabled ? 1f : 0f;
  }

  private void UpdateUI()
  {
    // Sonido
    if (soundIcon != null) soundIcon.color = soundEnabled ? activeColor : inactiveColor;
    if (soundStatusText != null)
    {
      soundStatusText.text = soundEnabled ? "ON" : "OFF";
      soundStatusText.color = soundEnabled ? activeColor : inactiveColor;
    }

    // Música
    if (musicIcon != null) musicIcon.color = musicEnabled ? activeColor : inactiveColor;
    if (musicStatusText != null)
    {
      musicStatusText.text = musicEnabled ? "ON" : "OFF";
      musicStatusText.color = musicEnabled ? activeColor : inactiveColor;
    }

    // Vibración
    if (vibrationIcon != null) vibrationIcon.color = vibrationEnabled ? activeColor : inactiveColor;
    if (vibrationStatusText != null)
    {
      vibrationStatusText.text = vibrationEnabled ? "ON" : "OFF";
      vibrationStatusText.color = vibrationEnabled ? activeColor : inactiveColor;
    }
  }

  public void Close()
  {
    if (vibrationEnabled) HapticFeedback.VibrateCollect();

    if (GameManager.Instance != null)
    {
      GameManager.Instance.CloseSettings();
    }
    else
    {
      gameObject.SetActive(false);
    }
  }
}
