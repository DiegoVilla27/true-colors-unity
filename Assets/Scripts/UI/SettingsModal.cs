using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Administra el modal de ajustes/configuración de MainMenu,
/// controlando las preferencias de Sonido, Música, Vibración y Notificaciones con un estado booleano
/// por ajuste, alternando entre los sprites activos (_ACTIVE) e inactivos.
/// </summary>
public class SettingsModal : MonoBehaviour
{
    [Header("Botones de Toggle")]
    [SerializeField] private Button soundButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button vibrationButton;
    [SerializeField] private Button notificationsButton;

    [Header("Botones de Acción")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    [Header("Fondo de los Botones (BTN_ACTIVE / BTN_ICON)")]
    [SerializeField] private Image soundButtonBg;
    [SerializeField] private Image musicButtonBg;
    [SerializeField] private Image vibrationButtonBg;
    [SerializeField] private Image notificationsButtonBg;

    [Header("Sprites de Botón")]
    [SerializeField] private Sprite btnActiveSprite;     // BTN_ACTIVE.png
    [SerializeField] private Sprite btnInactiveSprite;   // BTN_ICON.png

    [Header("Iconos de los Botones")]
    [SerializeField] private Image soundIcon;
    [SerializeField] private Image musicIcon;
    [SerializeField] private Image vibrationIcon;
    [SerializeField] private Image notificationsIcon;

    [Header("Sprites de Iconos")]
    [SerializeField] private Sprite soundActiveIcon;         // SOUND_ACTIVE.png
    [SerializeField] private Sprite soundInactiveIcon;       // SOUND.png
    [SerializeField] private Sprite musicActiveIcon;         // MUSIC_ACTIVE.png
    [SerializeField] private Sprite musicInactiveIcon;       // MUSIC.png
    [SerializeField] private Sprite vibrationActiveIcon;     // VIBRATION_ACTIVE.png
    [SerializeField] private Sprite vibrationInactiveIcon;   // VIBRATION.png
    [SerializeField] private Sprite notificationsActiveIcon; // COMMENT_ACTIVE.png
    [SerializeField] private Sprite notificationsInactiveIcon;// COMMENT.png

    [Header("Fondos HIGHLIGHT")]
    [SerializeField] private Image soundHighlight;
    [SerializeField] private Image musicHighlight;
    [SerializeField] private Image vibrationHighlight;
    [SerializeField] private Image notificationsHighlight;

    [Header("Estados Booleanos de los Ajustes")]
    [SerializeField] private bool isSoundActive = true;
    [SerializeField] private bool isMusicActive = true;
    [SerializeField] private bool isVibrationActive = true;
    [SerializeField] private bool isNotificationsActive = true;

    // Snapshot para restaurar si el usuario pulsa CANCEL
    private SettingsDatabase.SettingsData snapshot;

    // Propiedades públicas para interactuar desde código con actualización visual inmediata y persistencia en DB
    public bool IsSoundActive
    {
        get => isSoundActive;
        set
        {
            if (isSoundActive == value) return;
            isSoundActive = value;
            UpdateSoundUI();
            SaveToDatabase();
        }
    }

    public bool IsMusicActive
    {
        get => isMusicActive;
        set
        {
            if (isMusicActive == value) return;
            isMusicActive = value;
            UpdateMusicUI();
            SaveToDatabase();
        }
    }

    public bool IsVibrationActive
    {
        get => isVibrationActive;
        set
        {
            if (isVibrationActive == value) return;
            isVibrationActive = value;
            UpdateVibrationUI();
            SaveToDatabase();
        }
    }

    public bool IsNotificationsActive
    {
        get => isNotificationsActive;
        set
        {
            if (isNotificationsActive == value) return;
            isNotificationsActive = value;
            UpdateNotificationsUI();
            SaveToDatabase();
        }
    }

    // Color HIGHLIGHT: #00739A con Alpha exacto de 70 (70 / 255f)
    private readonly Color highlightColor = new Color(0f, 115f / 255f, 154f / 255f, 70f / 255f);

    void Awake()
    {
        LoadFromDatabase();
        WireButtons();
    }

    void OnEnable()
    {
        LoadFromDatabase();
        // Guardar snapshot para poder revertir en caso de CANCEL
        snapshot = new SettingsDatabase.SettingsData(isSoundActive, isMusicActive, isVibrationActive, isNotificationsActive);
        UpdateAllUI();
    }

    void Start()
    {
        UpdateAllUI();
    }

    private void LoadFromDatabase()
    {
        var data = SettingsDatabase.Load();
        isSoundActive = data.isSoundActive;
        isMusicActive = data.isMusicActive;
        isVibrationActive = data.isVibrationActive;
        isNotificationsActive = data.isNotificationsActive;
    }

    private void SaveToDatabase()
    {
        SettingsDatabase.Save(isSoundActive, isMusicActive, isVibrationActive, isNotificationsActive);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Se ejecuta en el Editor al cambiar cualquier valor o booleano en el Inspector,
    /// garantizando que los Source Image se actualicen al instante en la vista y en el Inspector.
    /// </summary>
    private void OnValidate()
    {
        UpdateAllUI();
    }
#endif

    private void WireButtons()
    {
        WireButton(soundButton, ToggleSound);
        WireButton(musicButton, ToggleMusic);
        WireButton(vibrationButton, ToggleVibration);
        WireButton(notificationsButton, ToggleNotifications);
        WireButton(confirmButton, Confirm);
        WireButton(cancelButton, Cancel);
    }

    private void WireButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;

        // Comprobar si ya existe como listener persistente en la escena/inspector
        // para evitar agregar un listener dinámico duplicado que cause doble ejecución (anulando el toggle)
        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
        {
            if (btn.onClick.GetPersistentTarget(i) == (Object)this &&
                btn.onClick.GetPersistentMethodName(i) == action.Method.Name)
            {
                return;
            }
        }

        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    [ContextMenu("Toggle Sound")]
    public void ToggleSound()
    {
        isSoundActive = !isSoundActive;
        // TODO: Lógica adicional de sonido más adelante
        UpdateSoundUI();
        SaveToDatabase();
        if (isVibrationActive) HapticFeedback.VibrateCollect();
    }

    [ContextMenu("Toggle Music")]
    public void ToggleMusic()
    {
        isMusicActive = !isMusicActive;
        // TODO: Lógica adicional de música más adelante
        UpdateMusicUI();
        SaveToDatabase();
        if (isVibrationActive) HapticFeedback.VibrateCollect();
    }

    [ContextMenu("Toggle Vibration")]
    public void ToggleVibration()
    {
        isVibrationActive = !isVibrationActive;
        // TODO: Lógica adicional de vibración más adelante
        UpdateVibrationUI();
        SaveToDatabase();
        if (isVibrationActive) HapticFeedback.VibrateCollect();
    }

    [ContextMenu("Toggle Notifications")]
    public void ToggleNotifications()
    {
        isNotificationsActive = !isNotificationsActive;
        // TODO: Lógica adicional de notificaciones más adelante
        UpdateNotificationsUI();
        SaveToDatabase();
        if (isVibrationActive) HapticFeedback.VibrateCollect();
    }

    public void UpdateAllUI()
    {
        UpdateSoundUI();
        UpdateMusicUI();
        UpdateVibrationUI();
        UpdateNotificationsUI();
        UpdateHighlights();
    }

    private void UpdateSoundUI()
    {
        if (soundButtonBg != null && btnActiveSprite != null && btnInactiveSprite != null)
        {
            soundButtonBg.sprite = isSoundActive ? btnActiveSprite : btnInactiveSprite;
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(soundButtonBg);
#endif
        }

        if (soundIcon != null)
        {
            if (soundActiveIcon != null && soundInactiveIcon != null)
            {
                soundIcon.sprite = isSoundActive ? soundActiveIcon : soundInactiveIcon;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(soundIcon);
#endif
            }
            soundIcon.color = Color.white;
        }
    }

    private void UpdateMusicUI()
    {
        if (musicButtonBg != null && btnActiveSprite != null && btnInactiveSprite != null)
        {
            musicButtonBg.sprite = isMusicActive ? btnActiveSprite : btnInactiveSprite;
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(musicButtonBg);
#endif
        }

        if (musicIcon != null)
        {
            if (musicActiveIcon != null && musicInactiveIcon != null)
            {
                musicIcon.sprite = isMusicActive ? musicActiveIcon : musicInactiveIcon;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(musicIcon);
#endif
            }
            musicIcon.color = Color.white;
        }
    }

    private void UpdateVibrationUI()
    {
        if (vibrationButtonBg != null && btnActiveSprite != null && btnInactiveSprite != null)
        {
            vibrationButtonBg.sprite = isVibrationActive ? btnActiveSprite : btnInactiveSprite;
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(vibrationButtonBg);
#endif
        }

        if (vibrationIcon != null)
        {
            if (vibrationActiveIcon != null && vibrationInactiveIcon != null)
            {
                vibrationIcon.sprite = isVibrationActive ? vibrationActiveIcon : vibrationInactiveIcon;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(vibrationIcon);
#endif
            }
            vibrationIcon.color = Color.white;
        }
    }

    private void UpdateNotificationsUI()
    {
        if (notificationsButtonBg != null && btnActiveSprite != null && btnInactiveSprite != null)
        {
            notificationsButtonBg.sprite = isNotificationsActive ? btnActiveSprite : btnInactiveSprite;
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(notificationsButtonBg);
#endif
        }

        if (notificationsIcon != null)
        {
            if (notificationsActiveIcon != null && notificationsInactiveIcon != null)
            {
                notificationsIcon.sprite = isNotificationsActive ? notificationsActiveIcon : notificationsInactiveIcon;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(notificationsIcon);
#endif
            }
            notificationsIcon.color = Color.white;
        }
    }

    private void UpdateHighlights()
    {
        if (soundHighlight != null) soundHighlight.color = highlightColor;
        if (musicHighlight != null) musicHighlight.color = highlightColor;
        if (vibrationHighlight != null) vibrationHighlight.color = highlightColor;
        if (notificationsHighlight != null) notificationsHighlight.color = highlightColor;
    }

    public void Confirm()
    {
        // Guardar y persistir los cambios en la base de datos
        SaveToDatabase();
        snapshot = new SettingsDatabase.SettingsData(isSoundActive, isMusicActive, isVibrationActive, isNotificationsActive);
        if (isVibrationActive) HapticFeedback.VibrateCollect();
        CloseModal();
    }

    public void Cancel()
    {
        // Revertir a los valores previos guardados en el snapshot al abrir el modal
        isSoundActive = snapshot.isSoundActive;
        isMusicActive = snapshot.isMusicActive;
        isVibrationActive = snapshot.isVibrationActive;
        isNotificationsActive = snapshot.isNotificationsActive;
        SaveToDatabase();
        UpdateAllUI();
        if (isVibrationActive) HapticFeedback.VibrateCollect();
        CloseModal();
    }

    private void CloseModal()
    {
        var menuController = Object.FindAnyObjectByType<MainMenuController>();
        if (menuController != null)
        {
            menuController.CloseOptions();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
