using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Base de datos local y capa de persistencia para los ajustes
/// del juego (Sonido, Música, Vibración y Notificaciones).
/// Combina persistencia híbrida en disco (PlayerPrefs nativo + JSON en Application.persistentDataPath)
/// garantizando que las preferencias sobrevivan entre sesiones y reinicios.
/// </summary>
public static class SettingsDatabase
{
    // Claves persistentes para la base de datos local
    public const string SOUND_KEY = "Setting_Sound";
    public const string MUSIC_KEY = "Setting_Music";
    public const string VIBRATION_KEY = "Setting_Vibration";
    public const string NOTIFICATIONS_KEY = "Setting_Notifications";

    private static string DbFilePath => Path.Combine(Application.persistentDataPath, "settings_db.json");

    /// <summary>
    /// Modelo de datos serializable para la base de datos de ajustes.
    /// </summary>
    [Serializable]
    public struct SettingsData
    {
        public bool isSoundActive;
        public bool isMusicActive;
        public bool isVibrationActive;
        public bool isNotificationsActive;

        public SettingsData(bool sound, bool music, bool vibration, bool notifications)
        {
            isSoundActive = sound;
            isMusicActive = music;
            isVibrationActive = vibration;
            isNotificationsActive = notifications;
        }

        public static SettingsData Default => new SettingsData(true, true, true, true);
    }

    /// <summary>
    /// Evento de dominio emitido cuando cualquier ajuste es modificado y guardado en la base de datos.
    /// </summary>
    public static event Action<SettingsData> OnSettingsChanged;

    /// <summary>
    /// Carga la configuración desde la base de datos local.
    /// Si no existen registros previos, retorna los valores por defecto (todos activos en true).
    /// </summary>
    public static SettingsData Load()
    {
        // 1. Intentar cargar desde archivo JSON en persistentDataPath
        if (File.Exists(DbFilePath))
        {
            try
            {
                string json = File.ReadAllText(DbFilePath);
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<SettingsData>(json);
                    ApplyAudioListener(data.isSoundActive);
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SettingsDatabase] Error al leer base de datos JSON ({DbFilePath}): {ex.Message}");
            }
        }

        // 2. Si no hay archivo JSON, leer de PlayerPrefs
        bool sound = PlayerPrefs.GetInt(SOUND_KEY, 1) == 1;
        bool music = PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
        bool vibration = PlayerPrefs.GetInt(VIBRATION_KEY, 1) == 1;
        bool notifications = PlayerPrefs.GetInt(NOTIFICATIONS_KEY, 1) == 1;

        var loaded = new SettingsData(sound, music, vibration, notifications);

        // Crear/sincronizar el archivo JSON local
        SaveToJsonFile(loaded);
        ApplyAudioListener(loaded.isSoundActive);

        return loaded;
    }

    /// <summary>
    /// Guarda el estado completo en la base de datos local (PlayerPrefs y archivo JSON).
    /// </summary>
    public static void Save(SettingsData data)
    {
        PlayerPrefs.SetInt(SOUND_KEY, data.isSoundActive ? 1 : 0);
        PlayerPrefs.SetInt(MUSIC_KEY, data.isMusicActive ? 1 : 0);
        PlayerPrefs.SetInt(VIBRATION_KEY, data.isVibrationActive ? 1 : 0);
        PlayerPrefs.SetInt(NOTIFICATIONS_KEY, data.isNotificationsActive ? 1 : 0);
        PlayerPrefs.Save();

        SaveToJsonFile(data);
        ApplyAudioListener(data.isSoundActive);

        OnSettingsChanged?.Invoke(data);
    }

    /// <summary>
    /// Sobrecarga para guardar ajustes pasando booleanos individuales.
    /// </summary>
    public static void Save(bool sound, bool music, bool vibration, bool notifications)
    {
        Save(new SettingsData(sound, music, vibration, notifications));
    }

    /// <summary>
    /// Aplica el ajuste de sonido directamente al AudioListener global de Unity.
    /// </summary>
    public static void ApplyAudioSettings()
    {
        bool soundActive = PlayerPrefs.GetInt(SOUND_KEY, 1) == 1;
        ApplyAudioListener(soundActive);
    }

    private static void ApplyAudioListener(bool soundActive)
    {
        AudioListener.volume = soundActive ? 1f : 0f;
    }

    private static void SaveToJsonFile(SettingsData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(DbFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SettingsDatabase] No se pudo escribir archivo JSON ({DbFilePath}): {ex.Message}");
        }
    }
}
