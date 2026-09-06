using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Controla el sistema visual de propulsión híbrido (Sprite de llama
/// central + micro-partículas con inercia World Space). Reacciona al modo Overdrive/Boost
/// alargando la llama y duplicando el chorro de partículas (Afterburner) con cero GC allocations.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(ShipController))]
public class ShipThruster : MonoBehaviour
{
  [Header("Configuración de Fuego (Llama Central)")]
  [Tooltip("Sprite de la llama. Si está vacío, se busca automáticamente o se genera proceduralmente.")]
  [SerializeField] private Sprite flameSprite;
  [Tooltip("Punto de anclaje de la tobera inferior de la nave en coordenadas locales")]
  [SerializeField] private Vector3 nozzleOffset = new Vector3(0f, -0.45f, 0f);
  [SerializeField] private Vector2 normalFlameScale = new Vector2(0.45f, 0.65f);
  [SerializeField] private Vector2 boostFlameScale = new Vector2(0.65f, 1.45f);
  [SerializeField] private float flickerSpeed = 28f;
  [SerializeField] private float flickerIntensity = 0.12f;

  [Header("Colores")]
  [SerializeField] private bool autoConfigureColor = true;
  [SerializeField] private Color normalColor = Color.white;
  [SerializeField] private Color boostColor = new Color(1f, 0.9f, 0.3f, 1f);

  [Header("Partículas de Estela (World Space)")]
  [SerializeField] private bool enableParticles = true;
  [SerializeField] private float normalEmissionRate = 15f;
  [SerializeField] private float boostEmissionRate = 40f;
  [SerializeField] private float normalParticleSpeed = 3.5f;
  [SerializeField] private float boostParticleSpeed = 7.0f;

  private Transform flameTransform;
  private SpriteRenderer flameRenderer;
  private ParticleSystem thrusterParticles;
  private ParticleSystem.EmissionModule particleEmission;
  private ParticleSystem.MainModule particleMain;

  private float currentBoostProgress = 0f;
  private float targetBoostProgress = 0f;

  void Awake()
  {
    // Desactivar el antiguo TrailRenderer lateral si existe
    var oldTrail = GetComponent<TrailRenderer>();
    if (oldTrail != null)
    {
      oldTrail.enabled = false;
    }

    ConfigureColorPalette();
    InitializeFlameSprite();
    if (enableParticles)
    {
      InitializeParticleSystem();
    }
  }

  void OnEnable()
  {
    ConfigureColorPalette();
    InitializeFlameSprite();
    if (enableParticles)
    {
      InitializeParticleSystem();
    }

    if (flameRenderer != null) flameRenderer.enabled = true;
    if (thrusterParticles != null && Application.isPlaying) thrusterParticles.Play();
  }

  void OnDisable()
  {
    if (flameRenderer != null) flameRenderer.enabled = false;
    if (thrusterParticles != null) thrusterParticles.Stop();
  }

  void Start()
  {
    if (OverdriveController.Instance != null)
    {
      OverdriveController.Instance.OnOverdriveStarted += HandleOverdriveStarted;
      OverdriveController.Instance.OnOverdriveEnded += HandleOverdriveEnded;

      if (OverdriveController.Instance.IsOverdriveActive)
      {
        targetBoostProgress = 1f;
        currentBoostProgress = 1f;
      }
    }
  }

  void OnDestroy()
  {
    if (OverdriveController.Instance != null)
    {
      OverdriveController.Instance.OnOverdriveStarted -= HandleOverdriveStarted;
      OverdriveController.Instance.OnOverdriveEnded -= HandleOverdriveEnded;
    }
  }

  void Update()
  {
#if UNITY_EDITOR
    float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
    float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
#else
    float time = Time.time;
    float dt = Time.deltaTime;
#endif

    // Suavizado de transición entre crucero y boost
    currentBoostProgress = Mathf.MoveTowards(currentBoostProgress, targetBoostProgress, dt * 4f);

    // Oscilación de crepitación (Flicker)
    float flicker = 1f + Mathf.Sin(time * flickerSpeed) * flickerIntensity
                       + (Mathf.PerlinNoise(time * flickerSpeed * 0.5f, 0f) - 0.5f) * flickerIntensity;

    // Actualizar Sprite de llama
    if (flameTransform != null && flameRenderer != null)
    {
      Vector2 baseScale = Vector2.Lerp(normalFlameScale, boostFlameScale, currentBoostProgress);
      flameTransform.localScale = new Vector3(baseScale.x, baseScale.y * flicker, 1f);
      flameRenderer.color = Color.Lerp(normalColor, boostColor, currentBoostProgress);
    }

    // Actualizar flujo de partículas
    if (thrusterParticles != null && Application.isPlaying)
    {
      particleEmission.rateOverTime = Mathf.Lerp(normalEmissionRate, boostEmissionRate, currentBoostProgress);
      particleMain.startSpeed = Mathf.Lerp(normalParticleSpeed, boostParticleSpeed, currentBoostProgress);
    }
  }

  private void HandleOverdriveStarted()
  {
    targetBoostProgress = 1f;
  }

  private void HandleOverdriveEnded()
  {
    targetBoostProgress = 0f;
  }

  private void ConfigureColorPalette()
  {
    if (!autoConfigureColor) return;

    var shipTarget = GetComponent<ShipTarget>();
    if (shipTarget != null)
    {
      if (shipTarget.targetColor == GameColor.Red)
      {
        normalColor = new Color(1f, 0.38f, 0.12f, 0.95f);      // Rojo/naranja fuego brillante
        boostColor = new Color(1f, 0.88f, 0.22f, 1f);          // Fuego dorado plasma supercargado
      }
      else
      {
        normalColor = new Color(0.18f, 0.70f, 1f, 0.95f);       // Celeste/azul plasma
        boostColor = new Color(0.45f, 0.96f, 1f, 1f);          // Cian hiper-brillante
      }
    }
  }

  private void InitializeFlameSprite()
  {
    Transform existingChild = transform.Find("ThrusterFlame");
    GameObject flameObj;
    if (existingChild != null)
    {
      flameObj = existingChild.gameObject;
    }
    else
    {
      flameObj = new GameObject("ThrusterFlame");
      flameObj.transform.SetParent(transform, false);
    }
    flameObj.transform.localPosition = nozzleOffset;
    flameObj.transform.localRotation = Quaternion.identity;

    flameTransform = flameObj.transform;
    flameRenderer = flameObj.GetComponent<SpriteRenderer>();
    if (flameRenderer == null)
    {
      flameRenderer = flameObj.AddComponent<SpriteRenderer>();
    }

    if (flameSprite == null)
    {
      flameSprite = Resources.Load<Sprite>("Sprites/Thruster/thruster_flame");
#if UNITY_EDITOR
      if (flameSprite == null)
      {
        flameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Thruster/thruster_flame.png");
      }
#endif
      if (flameSprite == null)
      {
        flameSprite = CreateProceduralFlameSprite();
      }
    }

    var shipRenderer = GetComponent<SpriteRenderer>();
    int targetOrder = 1;
    if (shipRenderer != null)
    {
      if (shipRenderer.sortingOrder < 2)
      {
        shipRenderer.sortingOrder = 2; // Asegura que el casco de la nave esté en 2
      }
      targetOrder = shipRenderer.sortingOrder - 1; // 1: delante del fondo (0), detrás de la nave (2)
      flameRenderer.sortingLayerID = shipRenderer.sortingLayerID;
    }

    flameRenderer.sprite = flameSprite;
    flameRenderer.color = normalColor;
    flameRenderer.sortingOrder = targetOrder;
    flameRenderer.enabled = true;
  }

  private void InitializeParticleSystem()
  {
    Transform existingChild = transform.Find("ThrusterParticles");
    GameObject particleObj;
    if (existingChild != null)
    {
      particleObj = existingChild.gameObject;
    }
    else
    {
      particleObj = new GameObject("ThrusterParticles");
      particleObj.transform.SetParent(transform, false);
    }
    particleObj.transform.localPosition = nozzleOffset;
    // Orientado hacia abajo (-Y en 2D)
    particleObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

    thrusterParticles = particleObj.GetComponent<ParticleSystem>();
    if (thrusterParticles == null)
    {
      thrusterParticles = particleObj.AddComponent<ParticleSystem>();
    }

    var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
    if (renderer == null)
    {
      renderer = particleObj.AddComponent<ParticleSystemRenderer>();
    }

    var shipRenderer = GetComponent<SpriteRenderer>();
    int targetOrder = 1;
    if (shipRenderer != null)
    {
      targetOrder = shipRenderer.sortingOrder - 1;
      renderer.sortingLayerID = shipRenderer.sortingLayerID;
      if (shipRenderer.sharedMaterial != null)
      {
        renderer.sharedMaterial = shipRenderer.sharedMaterial;
      }
    }

    renderer.sortingOrder = targetOrder; // Delante del fondo (0)
    if (renderer.sharedMaterial == null)
    {
      renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    particleMain = thrusterParticles.main;
    particleMain.loop = true;
    particleMain.playOnAwake = true;
    particleMain.simulationSpace = ParticleSystemSimulationSpace.World; // Curvatura orgánica por inercia
    particleMain.startLifetime = 0.32f;
    particleMain.startSpeed = normalParticleSpeed;
    particleMain.startSize = 0.12f;
    particleMain.maxParticles = 60;
    particleMain.startColor = new ParticleSystem.MinMaxGradient(normalColor, boostColor);

    particleEmission = thrusterParticles.emission;
    particleEmission.enabled = true;
    particleEmission.rateOverTime = normalEmissionRate;

    var shape = thrusterParticles.shape;
    shape.enabled = true;
    shape.shapeType = ParticleSystemShapeType.Cone;
    shape.angle = 7f;
    shape.radius = 0.04f;

    var sizeOverLifetime = thrusterParticles.sizeOverLifetime;
    sizeOverLifetime.enabled = true;
    AnimationCurve sizeCurve = new AnimationCurve();
    sizeCurve.AddKey(0f, 1f);
    sizeCurve.AddKey(1f, 0f);
    sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

    var colorOverLifetime = thrusterParticles.colorOverLifetime;
    colorOverLifetime.enabled = true;
    Gradient grad = new Gradient();
    grad.SetKeys(
      new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(normalColor, 0.4f) },
      new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
    );
    colorOverLifetime.color = grad;
  }

  private Sprite CreateProceduralFlameSprite()
  {
    int w = 64;
    int h = 128;
    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
    tex.wrapMode = TextureWrapMode.Clamp;

    float cx = w * 0.5f;
    for (int y = 0; y < h; y++)
    {
      float ny = (float)y / h; // 0 en la tobera (arriba), 1 en la punta (abajo)
      float flameWidth = ny < 0.2f ? 14f + 4f * (ny / 0.2f) : 18f * Mathf.Pow(1f - (ny - 0.2f) / 0.8f, 1.4f);

      for (int x = 0; x < w; x++)
      {
        float dx = Mathf.Abs(x - cx);
        if (dx < flameWidth && flameWidth > 0.1f)
        {
          float nx = dx / flameWidth;
          float alphaEdge = Mathf.Pow(Mathf.Cos(nx * Mathf.PI * 0.5f), 2f);
          float alphaVert = Mathf.Pow(1f - ny, 0.8f);
          float core = Mathf.Pow(1f - nx, 2.5f);

          float r = Mathf.Clamp01(0.85f + core * 0.3f);
          float g = Mathf.Clamp01(0.85f + core * 0.3f);
          float b = Mathf.Clamp01(0.9f + core * 0.2f);
          float a = alphaEdge * alphaVert;

          tex.SetPixel(x, h - 1 - y, new Color(r, g, b, a));
        }
        else
        {
          tex.SetPixel(x, h - 1 - y, Color.clear);
        }
      }
    }
    tex.Apply();
    return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 200f);
  }
}
