using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Responsabilidad Única (SRP): Proporciona transiciones suaves de pantalla (Fade In / Fade Out)
/// cinemáticas entre escenas. Es un singleton persistente (DontDestroyOnLoad) auto-inicializable
/// que no requiere prefabs ni configuración manual en el editor.
/// </summary>
public class SceneFader : MonoBehaviour
{
  private static SceneFader instance;
  public static SceneFader Instance
  {
    get
    {
      if (instance == null)
      {
        instance = FindAnyObjectByType<SceneFader>();
        if (instance == null)
        {
          GameObject faderObj = new GameObject("[SceneFader]");
          instance = faderObj.AddComponent<SceneFader>();
          DontDestroyOnLoad(faderObj);
        }
      }
      return instance;
    }
  }

  private Canvas faderCanvas;
  private CanvasGroup faderGroup;
  private Image faderImage;
  private Coroutine currentRoutine;

  void Awake()
  {
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }

    instance = this;
    DontDestroyOnLoad(gameObject);
    InitializeCanvasOverlay();
  }

  private void InitializeCanvasOverlay()
  {
    if (faderCanvas != null) return;

    // Canvas overlay garantizado en la capa superior absoluta (SortingOrder máximo)
    faderCanvas = gameObject.AddComponent<Canvas>();
    faderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    faderCanvas.sortingOrder = 32767;

    CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1080, 1920);

    faderGroup = gameObject.AddComponent<CanvasGroup>();
    faderGroup.alpha = 0f;
    faderGroup.blocksRaycasts = false;
    faderGroup.interactable = false;

    GameObject imgObj = new GameObject("BlackOverlay");
    imgObj.transform.SetParent(transform, false);

    faderImage = imgObj.AddComponent<Image>();
    faderImage.color = Color.black;
    faderImage.raycastTarget = false;

    RectTransform rt = imgObj.GetComponent<RectTransform>();
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.sizeDelta = Vector2.zero;
    rt.anchoredPosition = Vector2.zero;
  }

  /// <summary>
  /// Realiza un Fade a negro, carga la nueva escena y luego realiza un Fade In transparente.
  /// </summary>
  public static void LoadScene(string sceneName, float fadeOutDuration = 0.45f, float fadeInDuration = 0.45f)
  {
    Instance.StartSceneTransition(sceneName, fadeOutDuration, fadeInDuration);
  }

  /// <summary>
  /// Carga la escena por índice de compilación con transición suave.
  /// </summary>
  public static void LoadScene(int sceneIndex, float fadeOutDuration = 0.45f, float fadeInDuration = 0.45f)
  {
    Instance.StartSceneTransition(sceneIndex, fadeOutDuration, fadeInDuration);
  }

  public void StartSceneTransition(string sceneName, float fadeOutDuration, float fadeInDuration)
  {
    if (currentRoutine != null) StopCoroutine(currentRoutine);
    currentRoutine = StartCoroutine(TransitionRoutine(sceneName, -1, fadeOutDuration, fadeInDuration));
  }

  public void StartSceneTransition(int sceneIndex, float fadeOutDuration, float fadeInDuration)
  {
    if (currentRoutine != null) StopCoroutine(currentRoutine);
    currentRoutine = StartCoroutine(TransitionRoutine(null, sceneIndex, fadeOutDuration, fadeInDuration));
  }

  private IEnumerator TransitionRoutine(string sceneName, int sceneIndex, float fadeOutDuration, float fadeInDuration)
  {
    InitializeCanvasOverlay();

    // 1. Fade Out hacia negro
    yield return FadeAlpha(0f, 1f, fadeOutDuration);

    // 2. Cargar escena de forma asíncrona mientras la pantalla está en negro
    AsyncOperation op = !string.IsNullOrEmpty(sceneName)
      ? SceneManager.LoadSceneAsync(sceneName)
      : SceneManager.LoadSceneAsync(sceneIndex);

    while (op != null && !op.isDone)
    {
      yield return null;
    }

    // Restaurar escala de tiempo garantizada para la nueva escena
    Time.timeScale = 1f;

    // Esperar un frame para que la escena inicialice sus componentes
    yield return null;

    // 3. Fade In revelando la nueva escena suavemente
    yield return FadeAlpha(1f, 0f, fadeInDuration);

    currentRoutine = null;
  }

  public IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
  {
    InitializeCanvasOverlay();

    faderGroup.blocksRaycasts = endAlpha > 0.05f;

    if (duration <= 0.001f)
    {
      faderGroup.alpha = endAlpha;
      faderGroup.blocksRaycasts = endAlpha > 0.05f;
      yield break;
    }

    float elapsed = 0f;
    while (elapsed < duration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / duration);

      // Interpolación suave SmoothStep
      float smoothT = Mathf.SmoothStep(0f, 1f, t);
      faderGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, smoothT);

      yield return null;
    }

    faderGroup.alpha = endAlpha;
    faderGroup.blocksRaycasts = endAlpha > 0.05f;
  }
}
