using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrueColors.Audio
{
    public enum SFXType
    {
        UIClick,
        ShipMove,
        BlockMatch,
        RockCollect,
        PowerUpBomb,
        PowerUpSlowMo,
        OverdriveActivate,
        GameOver,
        RocketLaunch,
        PackPurchased
    }

    public enum MusicType
    {
        None,
        Menu,
        Gameplay
    }

    /// <summary>
    /// Responsabilidad Única (SRP): Administrador central de Audio y Música para TrueColors.
    /// - Cero asignaciones (Zero-Allocation) con pool pre-instanciado de AudioSources.
    /// - Reproducción de música con crossfade suave.
    /// - Soporta modulación dinámica de tono (pitch) para streaks de rocas y variación en propulsores.
    /// - Integrado en tiempo real con SettingsDatabase (SOUND / MUSIC).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AudioManager>();
                    if (_instance == null && Application.isPlaying)
                    {
                        GameObject go = new GameObject("[AudioManager]");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            _ = Instance;
        }

        [Header("Configuración de Pooling")]
        [SerializeField] private int sfxPoolSize = 16;

        [Header("Volúmenes Base Maestros")]
        [Range(0f, 1f)] [SerializeField] private float masterSfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float masterMusicVolume = 0.65f;

        // Fuentes de Música para Crossfade
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private bool _isPlayingA = true;
        private MusicType _currentMusicType = MusicType.None;
        private Coroutine _crossfadeRoutine;

        // Pool de AudioSources para SFX
        private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        private readonly Dictionary<SFXType, AudioClip> _sfxClips = new Dictionary<SFXType, AudioClip>();
        private readonly Dictionary<MusicType, AudioClip> _musicClips = new Dictionary<MusicType, AudioClip>();
        private readonly Dictionary<SFXType, float> _sfxVolumes = new Dictionary<SFXType, float>();

        // Estado de Settings
        private bool _isSoundActive = true;
        private bool _isMusicActive = true;

        // Pitch dinámico para racha de rocas
        private int _rockStreak = 0;
        private float _lastRockCollectTime = 0f;

        public bool IsSoundActive => _isSoundActive;
        public bool IsMusicActive => _isMusicActive;
        public MusicType CurrentMusicType => _currentMusicType;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (Application.isPlaying && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadSettings();
            InitializeAudioSources();
            LoadAudioClips();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Sincronizar música según la escena cargada
            if (scene.name.Contains("MainMenu"))
            {
                PlayMusic(MusicType.Menu, 0.8f);
            }
            else if (scene.name.Contains("MainGame"))
            {
                PlayMusic(MusicType.Gameplay, 0.8f);
            }
        }

        private void LoadSettings()
        {
            _isSoundActive = PlayerPrefs.GetInt(SettingsDatabase.SOUND_KEY, 1) == 1;
            _isMusicActive = PlayerPrefs.GetInt(SettingsDatabase.MUSIC_KEY, 1) == 1;
        }

        private void InitializeAudioSources()
        {
            // 1. Fuentes de Música
            GameObject musicHolder = new GameObject("Music_Channels");
            musicHolder.transform.SetParent(transform);

            _musicSourceA = musicHolder.AddComponent<AudioSource>();
            _musicSourceA.loop = true;
            _musicSourceA.playOnAwake = false;
            _musicSourceA.spatialBlend = 0f; // 2D

            _musicSourceB = musicHolder.AddComponent<AudioSource>();
            _musicSourceB.loop = true;
            _musicSourceB.playOnAwake = false;
            _musicSourceB.spatialBlend = 0f; // 2D

            // 2. Pool de SFX
            GameObject sfxHolder = new GameObject("SFX_Pool");
            sfxHolder.transform.SetParent(transform);

            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFX_{i:D2}");
                sfxObj.transform.SetParent(sfxHolder.transform);

                AudioSource src = sfxObj.AddComponent<AudioSource>();
                src.loop = false;
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D
                _sfxPool.Enqueue(src);
            }
        }

        private void LoadAudioClips()
        {
            // Cargar Clips de SFX desde Resources/Audio/SFX/
            LoadSFX(SFXType.UIClick, "Audio/SFX/sfx_ui_click", 0.70f);
            LoadSFX(SFXType.ShipMove, "Audio/SFX/sfx_ship_move", 0.75f);
            LoadSFX(SFXType.BlockMatch, "Audio/SFX/sfx_block_match", 0.85f);
            LoadSFX(SFXType.RockCollect, "Audio/SFX/sfx_rock_collect", 0.80f);
            LoadSFX(SFXType.PowerUpBomb, "Audio/SFX/sfx_powerup_bomb", 0.90f);
            LoadSFX(SFXType.PowerUpSlowMo, "Audio/SFX/sfx_powerup_slowmo", 0.75f);
            LoadSFX(SFXType.OverdriveActivate, "Audio/SFX/sfx_overdrive_activate", 0.85f);
            LoadSFX(SFXType.GameOver, "Audio/SFX/sfx_game_over", 0.95f);
            LoadSFX(SFXType.RocketLaunch, "Audio/SFX/sfx_rocket_launch", 0.90f);
            LoadSFX(SFXType.PackPurchased, "Audio/SFX/sfx_pack_purchased", 0.85f);

            // Cargar Clips de Música desde Resources/Audio/Music/
            LoadMusic(MusicType.Menu, "Audio/Music/bgm_menu");
            LoadMusic(MusicType.Gameplay, "Audio/Music/bgm_gameplay");
        }

        private void LoadSFX(SFXType type, string resourcePath, float defaultVolume)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null)
            {
                _sfxClips[type] = clip;
                _sfxVolumes[type] = defaultVolume;
            }
            else
            {
                Debug.LogWarning($"<color=#FFAA00><b>[AudioManager]</b></color> No se pudo cargar SFX en ruta: {resourcePath}");
            }
        }

        private void LoadMusic(MusicType type, string resourcePath)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null)
            {
                _musicClips[type] = clip;
            }
            else
            {
                Debug.LogWarning($"<color=#FFAA00><b>[AudioManager]</b></color> No se pudo cargar Música en ruta: {resourcePath}");
            }
        }

        // =========================================================================
        // REPRODUCCIÓN DE EFECTOS SONOROS (SFX)
        // =========================================================================

        public void PlaySFX(SFXType type, float pitchOffset = 0f)
        {
            if (!_isSoundActive) return;
            if (!_sfxClips.TryGetValue(type, out var clip) || clip == null) return;
            if (_sfxPool.Count == 0) return;

            float baseVol = _sfxVolumes.TryGetValue(type, out float v) ? v : 1f;
            float finalVol = baseVol * masterSfxVolume;
            float pitch = 1f + pitchOffset;

            // Variación especial para sonidos específicos
            if (type == SFXType.RockCollect)
            {
                // Streak dinámico: cada roca recolectada en menos de 1.2s incrementa el pitch
                if (Time.unscaledTime - _lastRockCollectTime < 1.2f)
                {
                    _rockStreak = Mathf.Min(_rockStreak + 1, 7);
                }
                else
                {
                    _rockStreak = 0;
                }
                _lastRockCollectTime = Time.unscaledTime;
                pitch = 1.0f + (_rockStreak * 0.06f);
            }

            AudioSource source = _sfxPool.Dequeue();
            source.clip = clip;
            source.volume = finalVol;
            source.pitch = pitch;
            source.Play();

            StartCoroutine(ReturnSourceToPool(source, clip.length / Mathf.Max(0.1f, pitch)));
        }

        private IEnumerator ReturnSourceToPool(AudioSource source, float delay)
        {
            yield return new WaitForSecondsRealtime(delay + 0.05f);
            source.Stop();
            source.clip = null;
            _sfxPool.Enqueue(source);
        }

        // =========================================================================
        // REPRODUCCIÓN DE MÚSICA CON CROSSFADE
        // =========================================================================

        public void PlayMusic(MusicType type, float crossfadeDuration = 1.0f)
        {
            if (type == _currentMusicType && type != MusicType.None) return;
            _currentMusicType = type;

            if (!_musicClips.TryGetValue(type, out var newClip) || newClip == null)
            {
                StopMusic(crossfadeDuration);
                return;
            }

            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(newClip, crossfadeDuration));
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            _currentMusicType = MusicType.None;
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);

            var activeSource = _isPlayingA ? _musicSourceA : _musicSourceB;
            StartCoroutine(FadeOutSource(activeSource, fadeDuration));
        }

        private IEnumerator CrossfadeRoutine(AudioClip newTrack, float duration)
        {
            var incoming = _isPlayingA ? _musicSourceB : _musicSourceA;
            var outgoing = _isPlayingA ? _musicSourceA : _musicSourceB;
            _isPlayingA = !_isPlayingA;

            incoming.clip = newTrack;
            incoming.volume = 0f;

            if (_isMusicActive)
            {
                incoming.Play();
            }

            float elapsed = 0f;
            float outStartVol = outgoing.volume;
            float targetInVol = masterMusicVolume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (outgoing.isPlaying)
                {
                    outgoing.volume = Mathf.Lerp(outStartVol, 0f, t);
                }

                if (_isMusicActive)
                {
                    incoming.volume = Mathf.Lerp(0f, targetInVol, t);
                }

                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;

            if (_isMusicActive)
            {
                incoming.volume = targetInVol;
            }

            _crossfadeRoutine = null;
        }

        private IEnumerator FadeOutSource(AudioSource source, float duration)
        {
            float elapsed = 0f;
            float startVol = source.volume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
        }

        // =========================================================================
        // CONTROL DE SETTINGS (SOUND / MUSIC)
        // =========================================================================

        public void SetSoundActive(bool active)
        {
            _isSoundActive = active;
        }

        public void SetMusicActive(bool active)
        {
            _isMusicActive = active;

            var currentSource = _isPlayingA ? _musicSourceA : _musicSourceB;

            if (_isMusicActive)
            {
                if (currentSource.clip != null)
                {
                    currentSource.volume = masterMusicVolume;
                    if (!currentSource.isPlaying) currentSource.Play();
                }
                else if (_currentMusicType != MusicType.None)
                {
                    PlayMusic(_currentMusicType, 0.4f);
                }
            }
            else
            {
                currentSource.Pause();
            }
        }
    }
}
