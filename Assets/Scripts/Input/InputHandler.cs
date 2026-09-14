using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Gestiona la entrada táctil (móvil), ratón y teclado (editor/PC) para ambas naves.
/// En móvil reemplaza el gesto de swipe por toque directo e instantáneo:
/// - Mitad izquierda de la pantalla -> Controla nave izquierda (Roja).
/// - Mitad derecha de la pantalla -> Controla nave derecha (Azul).
/// - Toque a la izquierda de la posición de la nave -> Desplaza a la izquierda.
/// - Toque a la derecha de la posición de la nave -> Desplaza a la derecha.
/// </summary>
public class InputHandler : MonoBehaviour
{
    [Header("Naves")]
    [SerializeField] private ShipController leftShip;
    [SerializeField] private ShipController rightShip;

    [Header("Cámara")]
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (leftShip == null)
        {
            var go = GameObject.Find("Ship_Red");
            if (go != null) leftShip = go.GetComponent<ShipController>();
        }
        if (rightShip == null)
        {
            var go = GameObject.Find("Ship_Blue");
            if (go != null) rightShip = go.GetComponent<ShipController>();
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        // Atajo de teclado para pausar/reanudar en PC/Editor (Escape o P)
#if UNITY_EDITOR || UNITY_STANDALONE
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TogglePause();
                }
            }
        }
#endif

        // Si el juego terminó, está pausado o hay un anuncio mostrándose, no procesar controles de naves
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused || (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing)))
        {
            return;
        }

        // 1. Manejo multitáctil en móvil mediante toque directo e instantáneo
        ProcessTouchInput();

        // 2. Testing con ratón y teclado en Editor y builds de Escritorio
#if UNITY_EDITOR || UNITY_STANDALONE
        ProcessMouseInput();
        HandleKeyboardTesting();
#endif
    }

    /// <summary>
    /// Procesa toques instantáneos (TouchPhase.Began) en pantalla táctil móvil.
    /// Divide la pantalla verticalmente: la mitad izquierda controla la nave roja y la derecha la azul.
    /// Un toque a la izquierda de la posición actual de la nave la desplaza un carril a la izquierda;
    /// un toque a la derecha la desplaza un carril a la derecha.
    /// </summary>
    private void ProcessTouchInput()
    {
        if (Touch.activeTouches.Count == 0) return;

        float screenMidX = Screen.width * 0.5f;

        foreach (var touch in Touch.activeTouches)
        {
            // Responder de forma inmediata al instante exacto del contacto físico (cero latencia)
            if (touch.phase != TouchPhase.Began) continue;

            // Ignorar toques sobre elementos UI interactivos (ej. botón de pausa)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId))
            {
                continue;
            }

            Vector2 touchPos = touch.screenPosition;

            if (touchPos.x < screenMidX)
            {
                HandleShipTap(leftShip, touchPos.x, screenMidX * 0.5f);
            }
            else
            {
                HandleShipTap(rightShip, touchPos.x, screenMidX + (screenMidX * 0.5f));
            }
        }
    }

    private void HandleShipTap(ShipController ship, float touchScreenX, float fallbackCenterScreenX)
    {
        if (ship == null) return;

        float shipScreenX = fallbackCenterScreenX;
        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera != null)
        {
            shipScreenX = mainCamera.WorldToScreenPoint(ship.transform.position).x;
        }

        if (touchScreenX < shipScreenX)
        {
            ship.MoveLeft();
        }
        else
        {
            ship.MoveRight();
        }
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void ProcessMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 mousePos = mouse.position.ReadValue();
            float screenMidX = Screen.width * 0.5f;

            if (mousePos.x < screenMidX)
            {
                HandleShipTap(leftShip, mousePos.x, screenMidX * 0.5f);
            }
            else
            {
                HandleShipTap(rightShip, mousePos.x, screenMidX + (screenMidX * 0.5f));
            }
        }
    }

    private void HandleKeyboardTesting()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.aKey.wasPressedThisFrame && leftShip != null) leftShip.MoveLeft();
        if (keyboard.dKey.wasPressedThisFrame && leftShip != null) leftShip.MoveRight();
        if (keyboard.leftArrowKey.wasPressedThisFrame && rightShip != null) rightShip.MoveLeft();
        if (keyboard.rightArrowKey.wasPressedThisFrame && rightShip != null) rightShip.MoveRight();
    }
#endif
}