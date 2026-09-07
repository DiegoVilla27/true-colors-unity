using UnityEngine;

public static class HapticFeedback
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static AndroidJavaClass vibrationEffectClass;
    private static int apiLevel = -1;
    private static bool isInitialized = false;

    private static void InitializeAndroid()
    {
        if (isInitialized) return;
        isInitialized = true;

        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                apiLevel = version.GetStatic<int>("SDK_INT");
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }

            if (apiLevel >= 26)
            {
                vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[HapticFeedback] Error al inicializar vibrador Android: " + ex.Message);
        }
    }
#endif

    public const string VIBRATION_KEY = "Setting_Vibration";

    public static bool IsVibrationEnabled
    {
        get => PlayerPrefs.GetInt(VIBRATION_KEY, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(VIBRATION_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void VibrateCollect()
    {
        if (!IsVibrationEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroid();
        if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
        {
            try
            {
                if (apiLevel >= 26 && vibrationEffectClass != null)
                {
                    // 45ms para asegurar respuesta táctil en cualquier motor vibrador
                    using (var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", 45L, -1))
                    {
                        vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    vibrator.Call("vibrate", 45L);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[HapticFeedback] Error en VibrateCollect: " + ex.Message);
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    public static void VibrateGameOver()
    {
        if (!IsVibrationEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
