using System.Collections;
using UnityEngine;

/// <summary>
/// Responsabilidad Única (SRP): Genera un campo estelar cósmico de fondo con micro-estrellas
/// en color estrictamente blanco puro de baja opacidad y suave titileo (twinkle).
/// Es completamente no-invasivo para la jugabilidad: se dibuja en el fondo absoluto (SortingOrder -10),
/// tiene tamaño diminuto y en modo Overdrive (Boost) acelera con un efecto sutil de hiperespacio (Warp).
/// </summary>
[ExecuteAlways]
public class SpaceStarfield : MonoBehaviour
{
  [Header("Densidad y Tamaño de Estrellas")]
  [Tooltip("Cantidad total de estrellas visibles en pantalla")]
  [SerializeField] private int maxStars = 60;
  [Tooltip("Tamaño mínimo de los micro-puntos")]
  [SerializeField] private float minStarSize = 0.02f;
  [Tooltip("Tamaño máximo de los micro-puntos")]
  [SerializeField] private float maxStarSize = 0.045f;

  [Header("Velocidad de Caída")]
  [Tooltip("Velocidad de caída normal (deriva cósmica lenta)")]
  [SerializeField] private float normalFallSpeed = 1.4f;
  [Tooltip("Velocidad durante el modo Overdrive (Boost)")]
  [SerializeField] private float boostFallSpeed = 4.2f;

  [Header("Opacidad (Estrictamente Blanco Puro)")]
  [Tooltip("Opacidad mínima de las estrellas distantes")]
  [Range(0.05f, 0.4f)]
  [SerializeField] private float minAlpha = 0.12f;
  [Tooltip("Opacidad máxima del brillo")]
  [Range(0.2f, 0.6f)]
  [SerializeField] private float maxAlpha = 0.38f;

  [Header("Efecto Warp Speed (Overdrive)")]
  [SerializeField] private bool enableWarpStretch = true;
  [SerializeField] private float normalLengthScale = 1.0f;
  [SerializeField] private float boostLengthScale = 2.4f;

  private ParticleSystem starfieldParticles;
  private ParticleSystemRenderer starfieldRenderer;
  private ParticleSystem.MainModule mainModule;
  private ParticleSystem.EmissionModule emissionModule;

  private float currentWarpProgress = 0f;
  private float targetWarpProgress = 0f;

  private static Texture2D s_StarTexture;
  private static Material s_StarMaterial;

  void Awake()
  {
    InitializeStarfield();
  }

  void OnEnable()
  {
    InitializeStarfield();
    if (starfieldParticles != null && Application.isPlaying)
    {
      starfieldParticles.Play();
    }
  }

  void OnDisable()
  {
    if (starfieldParticles != null)
    {
      starfieldParticles.Stop();
    }
  }

  void Start()
  {
    if (Application.isPlaying && OverdriveController.Instance != null)
    {
      OverdriveController.Instance.OnOverdriveStarted += HandleOverdriveStarted;
      OverdriveController.Instance.OnOverdriveEnded += HandleOverdriveEnded;

      if (OverdriveController.Instance.IsOverdriveActive)
      {
        targetWarpProgress = 1f;
        currentWarpProgress = 1f;
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
    if (!Application.isPlaying) return;

    // Transición suave entre velocidad normal y boost warp
    currentWarpProgress = Mathf.MoveTowards(currentWarpProgress, targetWarpProgress, Time.deltaTime * 3.5f);

    if (starfieldParticles != null)
    {
      mainModule.simulationSpeed = Mathf.Lerp(1.0f, boostFallSpeed / normalFallSpeed, currentWarpProgress);
    }

    if (starfieldRenderer != null && enableWarpStretch)
    {
      starfieldRenderer.lengthScale = Mathf.Lerp(normalLengthScale, boostLengthScale, currentWarpProgress);
      starfieldRenderer.velocityScale = Mathf.Lerp(0.0f, 0.03f, currentWarpProgress);
    }
  }

  private void HandleOverdriveStarted()
  {
    targetWarpProgress = 1f;
  }

  private void HandleOverdriveEnded()
  {
    targetWarpProgress = 0f;
  }

  public void SetWarpTarget(bool active)
  {
    targetWarpProgress = active ? 1f : 0f;
  }

  public void InitializeStarfield()
  {
    Transform existingChild = transform.Find("StarfieldSystem");
    GameObject starfieldObj;
    if (existingChild != null)
    {
      starfieldObj = existingChild.gameObject;
    }
    else
    {
      starfieldObj = new GameObject("StarfieldSystem");
      starfieldObj.transform.SetParent(transform, false);
    }

    Camera cam = GetComponent<Camera>();
    if (cam == null) cam = Camera.main;
    if (cam == null) cam = FindAnyObjectByType<Camera>();

    float halfHeight = cam != null ? cam.orthographicSize : 5.0f;
    float halfWidth = cam != null ? halfHeight * cam.aspect : 5.0f * (9f / 16f);

    // Ajustar Z local para garantizar que la emisión esté exactamente en el plano Z = 0 de la cámara
    float localZ = (transform.position.z != 0f) ? -transform.position.z : 0f;
    starfieldObj.transform.localPosition = new Vector3(0f, halfHeight + 0.6f, localZ);
    // Orientar hacia abajo (-Y en 2D)
    starfieldObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    starfieldObj.transform.localScale = Vector3.one;

    starfieldParticles = starfieldObj.GetComponent<ParticleSystem>();
    if (starfieldParticles == null)
    {
      starfieldParticles = starfieldObj.AddComponent<ParticleSystem>();
    }

    starfieldRenderer = starfieldObj.GetComponent<ParticleSystemRenderer>();
    if (starfieldRenderer == null)
    {
      starfieldRenderer = starfieldObj.AddComponent<ParticleSystemRenderer>();
    }

    // Capa de fondo absoluto: SortingOrder = -10 (garantizado DETRÁS de bloques, divisor y naves)
    starfieldRenderer.sortingOrder = -10;
    starfieldRenderer.sortingLayerID = 0;
    starfieldRenderer.renderMode = ParticleSystemRenderMode.Stretch;
    starfieldRenderer.lengthScale = normalLengthScale;
    starfieldRenderer.velocityScale = 0.0f;

    // Asignar material con textura circular suave procedural
    if (starfieldRenderer.sharedMaterial == null || starfieldRenderer.sharedMaterial.name != "SpaceStarfield_Material")
    {
      starfieldRenderer.sharedMaterial = GetOrCreateStarMaterial();
    }

    // Configuración del módulo Main
    mainModule = starfieldParticles.main;
    mainModule.loop = true;
    mainModule.playOnAwake = true;
    mainModule.prewarm = true; // La pantalla ya contiene estrellas distribuidas desde el frame 0
    mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

    float totalTravelDistance = halfHeight * 2f + 1.5f;
    float lifetime = totalTravelDistance / normalFallSpeed;
    mainModule.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.85f, lifetime * 1.15f);

    mainModule.startSpeed = new ParticleSystem.MinMaxCurve(normalFallSpeed * 0.85f, normalFallSpeed * 1.15f);
    mainModule.startSize = new ParticleSystem.MinMaxCurve(minStarSize, maxStarSize);
    mainModule.maxParticles = maxStars;

    // Color ESTRICTAMENTE blanco puro con opacidad suave
    mainModule.startColor = new ParticleSystem.MinMaxGradient(
      new Color(1f, 1f, 1f, minAlpha),
      new Color(1f, 1f, 1f, maxAlpha)
    );

    // Emisión continua balanceada
    emissionModule = starfieldParticles.emission;
    emissionModule.enabled = true;
    emissionModule.rateOverTime = (float)maxStars / lifetime;

    // Forma del emisor: rectángulo horizontal que cubre todo el ancho de pantalla
    var shape = starfieldParticles.shape;
    shape.enabled = true;
    shape.shapeType = ParticleSystemShapeType.Box;
    shape.scale = new Vector3(halfWidth * 2.2f, 0.2f, 1f);

    // Módulo de Titileo (Twinkle) mediante ColorOverLifetime: pulsa la opacidad suavemente
    var colorOverLifetime = starfieldParticles.colorOverLifetime;
    colorOverLifetime.enabled = true;

    Gradient twinkleGradient = new Gradient();
    twinkleGradient.SetKeys(
      new GradientColorKey[]
      {
        new GradientColorKey(Color.white, 0f),
        new GradientColorKey(Color.white, 1f)
      },
      new GradientAlphaKey[]
      {
        new GradientAlphaKey(0f, 0f),
        new GradientAlphaKey(1f, 0.15f),
        new GradientAlphaKey(0.35f, 0.38f),
        new GradientAlphaKey(1f, 0.62f),
        new GradientAlphaKey(0.45f, 0.85f),
        new GradientAlphaKey(0f, 1f)
      }
    );
    colorOverLifetime.color = twinkleGradient;
  }

  private static Texture2D GetOrCreateStarTexture()
  {
    if (s_StarTexture != null) return s_StarTexture;

    int size = 32;
    s_StarTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
    s_StarTexture.name = "ProceduralStarDot";
    s_StarTexture.filterMode = FilterMode.Bilinear;
    s_StarTexture.wrapMode = TextureWrapMode.Clamp;

    float center = (size - 1) / 2f;
    float maxRadius = size / 2f;

    for (int y = 0; y < size; y++)
    {
      for (int x = 0; x < size; x++)
      {
        float dx = (x - center) / maxRadius;
        float dy = (y - center) / maxRadius;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        if (dist >= 1f)
        {
          s_StarTexture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
        }
        else
        {
          // Caída suave tipo campana (smoothstep cuádruple)
          float t = 1f - dist;
          float alpha = t * t * (3f - 2f * t);
          s_StarTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
      }
    }
    s_StarTexture.Apply();
    return s_StarTexture;
  }

  private static Material GetOrCreateStarMaterial()
  {
    if (s_StarMaterial != null) return s_StarMaterial;

    Shader shader = Shader.Find("Sprites/Default");
    if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
    if (shader == null) shader = Shader.Find("Unlit/Transparent");

    s_StarMaterial = new Material(shader);
    s_StarMaterial.name = "SpaceStarfield_Material";
    s_StarMaterial.mainTexture = GetOrCreateStarTexture();
    return s_StarMaterial;
  }
}
