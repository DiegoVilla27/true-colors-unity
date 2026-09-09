using System;
using UnityEngine;
using Unity.Services.LevelPlay;

/// <summary>
/// Responsabilidad Única (SRP): Administrador directo y limpio de anuncios con Unity LevelPlay.
/// 1. Cada 5 muertes -> Muestra anuncio corto (Intersticial) y reinicia contador a 0.
/// 2. Si el jugador pulsa Revivir -> Muestra anuncio largo (Rewarded), máximo 2 veces por juego.
public class AdsManager : MonoBehaviour
{
    private static AdsManager instance;
    public static AdsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AdsManager>();
                if (instance == null)
                {
                    GameObject adsObj = new GameObject("[AdsManager]");
                    instance = adsObj.AddComponent<AdsManager>();
                    DontDestroyOnLoad(adsObj);
                }
            }
            return instance;
        }
    }

    private const string PREF_DEATH_COUNT = "TC_AdDeathCount";

    [Header("Credenciales LevelPlay")]
    [SerializeField] private string androidAppKey = "800370605";
    [SerializeField] private string iosAppKey = "800370604";

    [Header("Placements Android")]
    [SerializeField] private string androidInterstitialPlacement = "BP_Interstitial_Android";
    [SerializeField] private string androidRewardedPlacement = "BP_Rewarded_Android";

    [Header("Placements iOS")]
    [SerializeField] private string iosInterstitialPlacement = "BP_Interstitial_iOS";
    [SerializeField] private string iosRewardedPlacement = "BP_Rewarded_iOS";

    [Header("Reglas del Juego")]
    [Tooltip("Cada cuántas muertes se muestra el anuncio corto.")]
    [SerializeField] private int deathsForInterstitial = 5;
    [SerializeField] private int currentDeathCount = 0;

    [Tooltip("Máximo de revividas permitidas por partida/juego.")]
    [SerializeField] private int maxRevivesPerGame = 2;
    [SerializeField] private int revivesUsedThisGame = 0;

    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;

    private bool isSdkInitialized = false;
    private bool isRewardEarned = false;
    public bool IsAdShowing { get; private set; } = false;

    private Coroutine rewardedTimeoutRoutine;
    private Coroutine interstitialTimeoutRoutine;

    private Action onRewardedSuccess;
    private Action onRewardedFailed;
    private Action onInterstitialClosed;

    public int CurrentDeathCount => currentDeathCount;
    public int DeathsForInterstitial => deathsForInterstitial;
    public int MaxRevivesPerGame => maxRevivesPerGame;
    public int RevivesUsedThisGame => revivesUsedThisGame;
    public int RemainingRevives => Mathf.Max(0, maxRevivesPerGame - revivesUsedThisGame);
    public bool CanRevive => revivesUsedThisGame < maxRevivesPerGame;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;

        // Solo persistir si está en su propio GameObject dedicado
        if (transform.parent == null && gameObject.name == "[AdsManager]")
        {
            DontDestroyOnLoad(gameObject);
        }

        // Cargar contador de muertes persistente
        currentDeathCount = PlayerPrefs.GetInt(PREF_DEATH_COUNT, 0);

        InitLevelPlay();
    }

    private void InitLevelPlay()
    {
        string appKey = androidAppKey;
#if UNITY_IOS
        appKey = iosAppKey;
#endif

        LevelPlay.OnInitSuccess += config =>
        {
            isSdkInitialized = true;
            Debug.Log("<color=#55FF55>[AdsManager]</color> SDK LevelPlay inicializado.");
            LoadInterstitial();
            LoadRewarded();
        };

        LevelPlay.OnInitFailed += error =>
        {
            isSdkInitialized = false;
            Debug.LogWarning($"<color=#FF5555>[AdsManager]</color> Error inicializando LevelPlay: {error.ErrorMessage}");
        };

        LevelPlay.Init(appKey);
    }

    #region Interstitial (Ad Corta: Cada 5 Muertes)
    private void LoadInterstitial()
    {
        string placement = androidInterstitialPlacement;
#if UNITY_IOS
        placement = iosInterstitialPlacement;
#endif
        if (string.IsNullOrEmpty(placement)) return;

        interstitialAd = new LevelPlayInterstitialAd(placement);

        interstitialAd.OnAdLoaded += info =>
        {
            Debug.Log("<color=#55FF55>[AdsManager]</color> ✅ Ad corta (Intersticial) cargada con éxito y lista.");
        };

        interstitialAd.OnAdLoadFailed += error =>
        {
            Debug.LogWarning($"<color=#FF5555>[AdsManager]</color> ❌ Error cargando Ad corta: {error.ErrorMessage} (Código: {error.ErrorCode})");
        };

        interstitialAd.OnAdClosed += info =>
        {
            Debug.Log("[AdsManager] Ad corta (Intersticial) cerrada.");
            if (interstitialTimeoutRoutine != null) StopCoroutine(interstitialTimeoutRoutine);
            ExitAdMode();
            onInterstitialClosed?.Invoke();
            onInterstitialClosed = null;
            interstitialAd.LoadAd();
        };

        interstitialAd.OnAdDisplayFailed += (info, error) =>
        {
            Debug.LogWarning($"[AdsManager] Falló al mostrar Ad corta: {error.ErrorMessage}");
            if (interstitialTimeoutRoutine != null) StopCoroutine(interstitialTimeoutRoutine);
            ExitAdMode();
            onInterstitialClosed?.Invoke();
            onInterstitialClosed = null;
            interstitialAd.LoadAd();
        };

        interstitialAd.LoadAd();
    }

    private UnityEngine.EventSystems.EventSystem cachedEventSystem;

    public void EnsureUIInteractable()
    {
        IsAdShowing = false;
        if (cachedEventSystem == null)
        {
            cachedEventSystem = UnityEngine.EventSystems.EventSystem.current;
        }
        if (cachedEventSystem == null)
        {
            cachedEventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        }
        if (cachedEventSystem != null)
        {
            cachedEventSystem.enabled = true;
        }
    }

    private void EnterAdMode()
    {
        IsAdShowing = true;
        Time.timeScale = 0f; // Pausa absoluta: el juego de fondo queda 100% congelado
        AudioListener.pause = true; // Silenciar completamente el audio del juego durante el anuncio

        if (cachedEventSystem == null)
        {
            cachedEventSystem = UnityEngine.EventSystems.EventSystem.current;
        }
        if (cachedEventSystem == null)
        {
            cachedEventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        }
        if (cachedEventSystem != null)
        {
            cachedEventSystem.enabled = false; // Bloqueo total de clics y toques en la UI
        }
    }

    private void ExitAdMode()
    {
        IsAdShowing = false;
        AudioListener.pause = false; // Restaurar audio del juego

        if (cachedEventSystem == null)
        {
            cachedEventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        }
        if (cachedEventSystem != null)
        {
            cachedEventSystem.enabled = true; // Reactivar interacción con la UI de forma 100% garantizada
        }
    }

    /// <summary>
    /// Registra 1 muerte. Al llegar a 5 muertes, muestra la ad corta y resetea a 0.
    /// </summary>
    public void OnDeath(Action onAdFinished = null)
    {
        currentDeathCount++;
        Debug.Log($"<color=#33CCFF>[AdsManager]</color> Muerte registrada: {currentDeathCount}/{deathsForInterstitial}");

        if (currentDeathCount >= deathsForInterstitial)
        {
            currentDeathCount = 0;
            PlayerPrefs.SetInt(PREF_DEATH_COUNT, 0);
            PlayerPrefs.Save();

            Debug.Log("<color=#FFAA00>[AdsManager]</color> ¡5 Muertes alcanzadas! Mostrando anuncio corto de pantalla completa...");
            ShowInterstitial(onAdFinished);
        }
        else
        {
            PlayerPrefs.SetInt(PREF_DEATH_COUNT, currentDeathCount);
            PlayerPrefs.Save();
            onAdFinished?.Invoke();
        }
    }

    /// <summary>
    /// Restablece manualmente el contador de muertes a 0.
    /// </summary>
    public void ResetDeathCount()
    {
        currentDeathCount = 0;
        PlayerPrefs.SetInt(PREF_DEATH_COUNT, 0);
        PlayerPrefs.Save();
        Debug.Log("<color=#33CCFF>[AdsManager]</color> Contador de muertes reseteado a 0.");
    }

    public void ShowInterstitial(Action onClosed = null)
    {
        onInterstitialClosed = onClosed;

        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            EnterAdMode();
            if (interstitialTimeoutRoutine != null) StopCoroutine(interstitialTimeoutRoutine);
            interstitialTimeoutRoutine = StartCoroutine(InterstitialSafetyTimeoutRoutine(10f));

            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("[AdsManager] Ad corta no disponible en este instante (se continúa normalmente).");
            ExitAdMode();
            onClosed?.Invoke();
            interstitialAd?.LoadAd();
        }
    }

    private System.Collections.IEnumerator InterstitialSafetyTimeoutRoutine(float timeoutSeconds)
    {
        yield return new WaitForSecondsRealtime(timeoutSeconds);
        if (IsAdShowing)
        {
            Debug.LogWarning("[AdsManager] ⚠️ Timeout de seguridad en Ad corta. Continuando el juego limpiamente.");
            ExitAdMode();
            onInterstitialClosed?.Invoke();
            onInterstitialClosed = null;
            interstitialAd?.LoadAd();
        }
    }
    #endregion

    #region Rewarded (Ad Larga: Revivir)
    private void LoadRewarded()
    {
        string placement = androidRewardedPlacement;
#if UNITY_IOS
        placement = iosRewardedPlacement;
#endif
        if (string.IsNullOrEmpty(placement)) return;

        rewardedAd = new LevelPlayRewardedAd(placement);

        rewardedAd.OnAdLoaded += info =>
        {
            Debug.Log("<color=#55FF55>[AdsManager]</color> ✅ Ad larga (Rewarded) cargada con éxito y lista.");
        };

        rewardedAd.OnAdLoadFailed += error =>
        {
            Debug.LogWarning($"<color=#FF5555>[AdsManager]</color> ❌ Error cargando Ad larga: {error.ErrorMessage} (Código: {error.ErrorCode})");
        };

        rewardedAd.OnAdRewarded += (info, reward) =>
        {
            isRewardEarned = true;
            Debug.Log("<color=#55FF55>[AdsManager]</color> ¡Anuncio largo completado con éxito!");
        };

        rewardedAd.OnAdClosed += info =>
        {
            Debug.Log("[AdsManager] Ad larga cerrada.");
            if (rewardedTimeoutRoutine != null) StopCoroutine(rewardedTimeoutRoutine);
            StartCoroutine(ProcessRewardedClosedRoutine());
        };

        rewardedAd.OnAdDisplayFailed += (info, error) =>
        {
            Debug.LogWarning($"<color=#FFAA00>[AdsManager]</color> Falló al mostrar ad larga: {error.ErrorMessage}. Otorgando revivida de cortesía para no penalizar al jugador.");
            if (rewardedTimeoutRoutine != null) StopCoroutine(rewardedTimeoutRoutine);
            ExitAdMode();
            revivesUsedThisGame++;
            onRewardedSuccess?.Invoke();
            onRewardedSuccess = null;
            onRewardedFailed = null;
            rewardedAd.LoadAd();
        };

        rewardedAd.LoadAd();
    }

    private System.Collections.IEnumerator ProcessRewardedClosedRoutine()
    {
        if (rewardedTimeoutRoutine != null) StopCoroutine(rewardedTimeoutRoutine);

        // Esperar 1 frame para resolver el orden de eventos del SDK/simulador
        yield return null;

        ExitAdMode();

        if (isRewardEarned)
        {
            isRewardEarned = false;
            revivesUsedThisGame++;
            Debug.Log($"<color=#55FF55>[AdsManager]</color> ¡Revivida confirmada! ({revivesUsedThisGame}/{maxRevivesPerGame} revividas usadas en esta partida).");
            onRewardedSuccess?.Invoke();
        }
        else
        {
            Debug.LogWarning("<color=#FFAA00>[AdsManager]</color> El anuncio se cerró sin completar la recompensa. No revive.");
            onRewardedFailed?.Invoke();
        }

        onRewardedSuccess = null;
        onRewardedFailed = null;
        rewardedAd?.LoadAd();
    }

    /// <summary>
    /// Reinicia las revividas cuando comienza una nueva partida.
    /// </summary>
    public void ResetRevivesForNewGame()
    {
        revivesUsedThisGame = 0;
        Debug.Log($"<color=#33CCFF>[AdsManager]</color> Nueva partida: revividas disponibles 2/{maxRevivesPerGame}.");
    }

    /// <summary>
    /// Solicita ver el anuncio largo para revivir (máximo 2 veces por juego).
    /// </summary>
    public void RequestRevive(Action onSuccess, Action onFailed = null)
    {
        if (!CanRevive)
        {
            Debug.LogWarning("[AdsManager] Ya alcanzaste el límite de 2 revividas para esta partida.");
            onFailed?.Invoke();
            return;
        }

        onRewardedSuccess = onSuccess;
        onRewardedFailed = onFailed;
        isRewardEarned = false;

        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            EnterAdMode();
#if UNITY_EDITOR
            Time.timeScale = 1f; // Permitir que la cuenta regresiva de 5s del mock en Editor no se congele
#endif
            if (rewardedTimeoutRoutine != null) StopCoroutine(rewardedTimeoutRoutine);
            float timeout = Application.isEditor ? 8f : 35f;
            rewardedTimeoutRoutine = StartCoroutine(RewardedSafetyTimeoutRoutine(timeout));

            rewardedAd.ShowAd();
        }
        else
        {
            // BLINDAJE: Si el anuncio no está disponible aún o la red falla, otorgar revivida de cortesía
            // para que el jugador nunca se quede bloqueado ni castigado por un fallo del ad network.
            Debug.LogWarning("<color=#FFAA00>[AdsManager]</color> Ad larga no lista / fallo de red. Otorgando revivida de cortesía para no penalizar al jugador.");
            ExitAdMode();
            revivesUsedThisGame++;
            onRewardedSuccess?.Invoke();
            onRewardedSuccess = null;
            onRewardedFailed = null;
            rewardedAd?.LoadAd();
        }
    }

    private System.Collections.IEnumerator RewardedSafetyTimeoutRoutine(float timeoutSeconds)
    {
        yield return new WaitForSecondsRealtime(timeoutSeconds);
        if (IsAdShowing)
        {
            Debug.LogWarning("<color=#FFAA00>[AdsManager]</color> ⚠️ TIMEOUT DE SEGURIDAD: El anuncio no respondió a tiempo o se detuvo. Desbloqueando juego y otorgando revivida de cortesía.");
            ExitAdMode();
            revivesUsedThisGame++;
            onRewardedSuccess?.Invoke();
            onRewardedSuccess = null;
            onRewardedFailed = null;
            rewardedAd?.LoadAd();
        }
    }
    #endregion
}
