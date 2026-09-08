using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador principal de navegación para la escena de bienvenida (MainMenu).
/// Gestiona la transición cinemática de despegue (Launch) con posquemadores de naves,
/// desvaneciendo únicamente botones y títulos sin afectar al fondo del hangar.
/// </summary>
public class MainMenuController : MonoBehaviour
{
  [Header("Paneles Auxiliares")]
  [SerializeField] private GameObject optionsPanel;
  [SerializeField] private GameObject shopPanel;
  [SerializeField] private GameObject leaderboardPanel;

  [Header("Secuencia Cinemática de Despegue (Launch)")]
  [Tooltip("CanvasGroup opcional de un contenedor de botones/título (NO del Canvas raíz)")]
  [SerializeField] private CanvasGroup menuCanvasGroup;

  [Tooltip("Imagen de fondo del hangar espacial para fundido suave hacia el cosmos")]
  [SerializeField] private Image backgroundImage;

  [Tooltip("Nave o contenedor de nave izquierda (Roja)")]
  [SerializeField] private Transform leftMenuShip;

  [Tooltip("Nave o contenedor de nave derecha (Azul)")]
  [SerializeField] private Transform rightMenuShip;

  [Tooltip("Duración de la aceleración y despegue antes de cambiar de escena")]
  [SerializeField] private float launchDuration = 1.05f;

  [Tooltip("Duración del fade a negro")]
  [SerializeField] private float fadeOutDuration = 0.25f;

  [Tooltip("Duración del fade in revelando el juego")]
  [SerializeField] private float fadeInDuration = 0.45f;

  private bool isLaunching = false;
  private readonly List<Graphic> fadeGraphics = new List<Graphic>();

  void Awake()
  {
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = 60;
    SettingsDatabase.Load();
  }

  void Start()
  {
    AutoWireButtonPressEffects();
    AutoDetectMenuReferences();
  }

  private void AutoDetectMenuReferences()
  {
    // 1. Auto-detectar naves o contenedores de naves
    if (leftMenuShip == null)
    {
      GameObject redContainer = GameObject.Find("ShipContainer_Red");
      if (redContainer != null) leftMenuShip = redContainer.transform;
      else
      {
        GameObject redObj = GameObject.Find("MenuShip_Red");
        if (redObj != null) leftMenuShip = redObj.transform;
      }
    }

    if (rightMenuShip == null)
    {
      GameObject blueContainer = GameObject.Find("ShipContainer_Blue");
      if (blueContainer != null) rightMenuShip = blueContainer.transform;
      else
      {
        GameObject blueObj = GameObject.Find("MenuShip_Blue");
        if (blueObj != null) rightMenuShip = blueObj.transform;
      }
    }

    // 2. Auto-asegurar que las llamas siempre se rendericen por detrás del casco de las naves
    EnsureFlameRendersBehind(leftMenuShip);
    EnsureFlameRendersBehind(rightMenuShip);

    // 3. Auto-detectar imagen de fondo del hangar si no está asignada
    if (backgroundImage == null)
    {
      GameObject bgObj = GameObject.Find("Background");
      if (bgObj != null) backgroundImage = bgObj.GetComponent<Image>();
    }

    // 4. Localizar elementos de UI que deben desvanecerse (botones y títulos, NUNCA el fondo ni las naves)
    fadeGraphics.Clear();
    if (menuCanvasGroup == null)
    {
      GameObject uiContainer = GameObject.Find("UI_Elements");
      if (uiContainer != null)
      {
        menuCanvasGroup = uiContainer.GetComponent<CanvasGroup>();
        if (menuCanvasGroup == null)
        {
          menuCanvasGroup = uiContainer.AddComponent<CanvasGroup>();
        }
      }
      else
      {
        var buttons = FindObjectsByType<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
          var graphics = buttons[i].GetComponentsInChildren<Graphic>();
          fadeGraphics.AddRange(graphics);
        }

        GameObject title = GameObject.Find("GameTitle");
        if (title != null)
        {
          var titleGraphics = title.GetComponentsInChildren<Graphic>();
          fadeGraphics.AddRange(titleGraphics);
        }

        GameObject version = GameObject.Find("Version");
        if (version != null)
        {
          var verGraphics = version.GetComponentsInChildren<Graphic>();
          fadeGraphics.AddRange(verGraphics);
        }
      }
    }
  }

  /// <summary>
  /// Garantiza que la llama del propulsor quede visualmente debajo de la nave,
  /// tanto si la llama es hija directa de la nave como si son hermanas en un contenedor.
  /// </summary>
  private void EnsureFlameRendersBehind(Transform shipOrContainer)
  {
    if (shipOrContainer == null) return;

    Transform directFlame = shipOrContainer.Find("Thruster_Flame");
    if (directFlame != null)
    {
      // La llama es hija directa de la nave: en UGUI los hijos se dibujan encima del padre.
      // Solución automática: Canvas local con overrideSorting
      var shipCanvas = shipOrContainer.GetComponent<Canvas>();
      if (shipCanvas == null)
      {
        shipCanvas = shipOrContainer.gameObject.AddComponent<Canvas>();
        shipOrContainer.gameObject.AddComponent<GraphicRaycaster>();
      }
      shipCanvas.overrideSorting = true;
      shipCanvas.sortingOrder = 10;

      var flameCanvas = directFlame.GetComponent<Canvas>();
      if (flameCanvas == null)
      {
        flameCanvas = directFlame.gameObject.AddComponent<Canvas>();
      }
      flameCanvas.overrideSorting = true;
      flameCanvas.sortingOrder = 5; // Orden menor = dibujado por detrás
    }
    else
    {
      // Es un contenedor con la llama y el casco como hermanos
      Transform siblingFlame = null;
      Transform siblingShip = null;
      for (int i = 0; i < shipOrContainer.childCount; i++)
      {
        var child = shipOrContainer.GetChild(i);
        if (child.name.Contains("Flame") || child.name.Contains("Thruster")) siblingFlame = child;
        if (child.name.Contains("Ship") || child.name.Contains("Hull")) siblingShip = child;
      }
      if (siblingFlame != null && siblingShip != null)
      {
        siblingFlame.SetSiblingIndex(0); // Primero = debajo
        siblingShip.SetSiblingIndex(1);  // Segundo = encima
      }
    }
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
    if (isLaunching) return;
    StartCoroutine(LaunchSequenceRoutine());
  }

  private IEnumerator LaunchSequenceRoutine()
  {
    isLaunching = true;
    HapticFeedback.VibrateCollect();

    // 1. Congelar flotación idle (hover)
    if (leftMenuShip != null)
    {
      var hover = leftMenuShip.GetComponent<MenuShipHover>() ?? leftMenuShip.GetComponentInChildren<MenuShipHover>();
      if (hover != null) hover.Freeze();
    }
    if (rightMenuShip != null)
    {
      var hover = rightMenuShip.GetComponent<MenuShipHover>() ?? rightMenuShip.GetComponentInChildren<MenuShipHover>();
      if (hover != null) hover.Freeze();
    }

    // 2. Encender posquemadores a máxima potencia (tanto para UI como Mundo)
    if (leftMenuShip != null)
    {
      var thrusterWorld = leftMenuShip.GetComponent<ShipThruster>() ?? leftMenuShip.GetComponentInChildren<ShipThruster>();
      if (thrusterWorld != null) thrusterWorld.SetBoostActive(true);

      var thrusterUI = leftMenuShip.GetComponent<UIThrusterFlicker>() ?? leftMenuShip.GetComponentInChildren<UIThrusterFlicker>();
      if (thrusterUI != null) thrusterUI.SetBoostActive(true);
    }
    if (rightMenuShip != null)
    {
      var thrusterWorld = rightMenuShip.GetComponent<ShipThruster>() ?? rightMenuShip.GetComponentInChildren<ShipThruster>();
      if (thrusterWorld != null) thrusterWorld.SetBoostActive(true);

      var thrusterUI = rightMenuShip.GetComponent<UIThrusterFlicker>() ?? rightMenuShip.GetComponentInChildren<UIThrusterFlicker>();
      if (thrusterUI != null) thrusterUI.SetBoostActive(true);
    }

    var starfield = FindAnyObjectByType<SpaceStarfield>();
    if (starfield != null) starfield.SetWarpTarget(true);

    // 3. Detectar si son RectTransforms de Canvas UI o Transforms de Mundo
    RectTransform leftRT = leftMenuShip != null ? leftMenuShip.GetComponent<RectTransform>() : null;
    RectTransform rightRT = rightMenuShip != null ? rightMenuShip.GetComponent<RectTransform>() : null;

    Vector2 leftStartUI = leftRT != null ? leftRT.anchoredPosition : Vector2.zero;
    Vector2 rightStartUI = rightRT != null ? rightRT.anchoredPosition : Vector2.zero;
    Vector2 leftTargetUI = leftStartUI + new Vector2(0f, 2000f);
    Vector2 rightTargetUI = rightStartUI + new Vector2(0f, 2000f);

    Vector3 leftStartWorld = leftMenuShip != null ? leftMenuShip.position : Vector3.zero;
    Vector3 rightStartWorld = rightMenuShip != null ? rightMenuShip.position : Vector3.zero;
    Vector3 leftTargetWorld = leftStartWorld + new Vector3(0f, 16f, 0f);
    Vector3 rightTargetWorld = rightStartWorld + new Vector3(0f, 16f, 0f);

    Color bgOriginalColor = backgroundImage != null ? backgroundImage.color : Color.white;

    float elapsed = 0f;
    while (elapsed < launchDuration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / launchDuration);

      // Curva cúbica de aceleración de cohete supersónico
      float accelT = t * t * t;

      // Desvanecer SOLO botones y títulos rápidamente al inicio (0.0 -> 0.35)
      float uiAlpha = Mathf.Lerp(1f, 0f, t * 2.8f);
      if (menuCanvasGroup != null)
      {
        menuCanvasGroup.alpha = uiAlpha;
      }
      else
      {
        for (int i = 0; i < fadeGraphics.Count; i++)
        {
          if (fadeGraphics[i] != null)
          {
            Color c = fadeGraphics[i].color;
            c.a = uiAlpha;
            fadeGraphics[i].color = c;
          }
        }
      }

      // Desvanecer suavemente el fondo del hangar hacia el espacio profundo (0.35 -> 0.95)
      if (backgroundImage != null)
      {
        float bgT = Mathf.Clamp01((t - 0.35f) / 0.6f);
        float bgSmooth = Mathf.SmoothStep(0f, 1f, bgT);
        Color c = bgOriginalColor;
        c.a = Mathf.Lerp(bgOriginalColor.a, 0f, bgSmooth);
        backgroundImage.color = c;
      }

      // Propulsar naves verticalmente por las pistas del hangar hacia el espacio
      if (leftRT != null)
      {
        leftRT.anchoredPosition = Vector2.Lerp(leftStartUI, leftTargetUI, accelT);
      }
      else if (leftMenuShip != null)
      {
        leftMenuShip.position = Vector3.Lerp(leftStartWorld, leftTargetWorld, accelT);
      }

      if (rightRT != null)
      {
        rightRT.anchoredPosition = Vector2.Lerp(rightStartUI, rightTargetUI, accelT);
      }
      else if (rightMenuShip != null)
      {
        rightMenuShip.position = Vector3.Lerp(rightStartWorld, rightTargetWorld, accelT);
      }

      yield return null;
    }

    // 4. Transición fluida con SceneFader: Fundido a negro y revelación suave en MainGame
    SceneFader.LoadScene("MainGame", fadeOutDuration, fadeInDuration);
  }

  public void OpenOptions()
  {
    if (isLaunching) return;
    HapticFeedback.VibrateCollect();
    if (leftMenuShip != null) leftMenuShip.gameObject.SetActive(false);
    if (rightMenuShip != null) rightMenuShip.gameObject.SetActive(false);
    if (optionsPanel != null)
    {
      optionsPanel.SetActive(true);
      optionsPanel.transform.SetAsLastSibling();
    }
    else Debug.Log("[MainMenu] Panel de opciones activado.");
  }

  public void CloseOptions()
  {
    HapticFeedback.VibrateCollect();
    if (leftMenuShip != null) leftMenuShip.gameObject.SetActive(true);
    if (rightMenuShip != null) rightMenuShip.gameObject.SetActive(true);
    if (optionsPanel != null) optionsPanel.SetActive(false);
  }

  public void OpenShop()
  {
    if (isLaunching) return;
    HapticFeedback.VibrateCollect();
    if (shopPanel != null) shopPanel.SetActive(true);
    else Debug.Log("[MainMenu] Tienda activada (Próximamente).");
  }

  public void OpenLeaderboard()
  {
    if (isLaunching) return;
    HapticFeedback.VibrateCollect();
    if (leftMenuShip != null) leftMenuShip.gameObject.SetActive(false);
    if (rightMenuShip != null) rightMenuShip.gameObject.SetActive(false);
    if (leaderboardPanel != null)
    {
      leaderboardPanel.SetActive(true);
      leaderboardPanel.transform.SetAsLastSibling();
    }
    else Debug.Log("[MainMenu] Leaderboard activado.");
  }

  public void CloseLeaderboard()
  {
    HapticFeedback.VibrateCollect();
    if (leftMenuShip != null) leftMenuShip.gameObject.SetActive(true);
    if (rightMenuShip != null) rightMenuShip.gameObject.SetActive(true);
    if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
  }
}
