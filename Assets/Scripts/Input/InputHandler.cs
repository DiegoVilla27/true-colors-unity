using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class InputHandler : MonoBehaviour
{
    [Header("Naves")]
    [SerializeField] private ShipController leftShip;
    [SerializeField] private ShipController rightShip;

    [Header("Configuración de Swipe")]
    [Tooltip("Distancia horizontal mínima en píxeles para registrar un cambio de carril (swipe).")]
    [SerializeField] private float swipeThreshold = 40f;

    private struct TouchTrack
    {
        public Vector2 startPosition;
        public Vector2 lastPosition;
        public bool isLeftSide;
    }

    private readonly Dictionary<int, TouchTrack> activeTracks = new();

#if UNITY_EDITOR || UNITY_STANDALONE
    private Vector2 mouseStartPos;
    private Vector2 mouseLastPos;
    private bool mouseIsLeftSide;
    private bool isMouseDragging;
#endif

    private void Awake()
    {
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
        activeTracks.Clear();
#if UNITY_EDITOR || UNITY_STANDALONE
        isMouseDragging = false;
#endif
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        activeTracks.Clear();
#if UNITY_EDITOR || UNITY_STANDALONE
        isMouseDragging = false;
#endif
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

        // Si el juego terminó o está pausado, no procesar controles de naves
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused))
        {
            activeTracks.Clear();
#if UNITY_EDITOR || UNITY_STANDALONE
            isMouseDragging = false;
#endif
            return;
        }

        // 1. Manejo multitáctil en móvil mediante Swipes independientes por carril/sector
        ProcessTouchSwipes();

        // 2. Testing con ratón y teclado en Editor y builds de Escritorio
#if UNITY_EDITOR || UNITY_STANDALONE
        ProcessMouseSwipeTesting();
        HandleKeyboardTesting();
#endif
    }

    private void ProcessTouchSwipes()
    {
        if (Touch.activeTouches.Count == 0)
        {
            if (activeTracks.Count > 0) activeTracks.Clear();
            return;
        }

        float screenMidX = Screen.width * 0.5f;

        foreach (var touch in Touch.activeTouches)
        {
            // Ignorar toques sobre elementos UI interactivos (ej. botón de pausa)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId))
            {
                continue;
            }

            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPos = touch.screenPosition;
                activeTracks[touch.touchId] = new TouchTrack
                {
                    startPosition = touchPos,
                    lastPosition = touchPos,
                    isLeftSide = touchPos.x < screenMidX
                };
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Ended)
            {
                if (activeTracks.TryGetValue(touch.touchId, out var track))
                {
                    Vector2 currentPos = touch.screenPosition;
                    Vector2 delta = currentPos - track.lastPosition;

                    // Validar que el gesto sea primordialmente horizontal y supere el umbral de swipe
                    if (Mathf.Abs(delta.x) >= swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        if (delta.x < 0f)
                        {
                            // Swipe hacia la izquierda: mover nave correspondiente al carril izquierdo
                            if (track.isLeftSide)
                            {
                                if (leftShip != null) leftShip.MoveLeft();
                            }
                            else
                            {
                                if (rightShip != null) rightShip.MoveLeft();
                            }
                        }
                        else
                        {
                            // Swipe hacia la derecha: mover nave correspondiente al carril derecho
                            if (track.isLeftSide)
                            {
                                if (leftShip != null) leftShip.MoveRight();
                            }
                            else
                            {
                                if (rightShip != null) rightShip.MoveRight();
                            }
                        }

                        // Actualizar última posición para permitir swipes continuos de múltiples carriles en un mismo trazo
                        track.lastPosition = currentPos;
                        activeTracks[touch.touchId] = track;
                    }

                    if (touch.phase == TouchPhase.Ended)
                    {
                        activeTracks.Remove(touch.touchId);
                    }
                }
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                activeTracks.Remove(touch.touchId);
            }
        }
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void ProcessMouseSwipeTesting()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float screenMidX = Screen.width * 0.5f;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 mousePos = mouse.position.ReadValue();
            mouseStartPos = mousePos;
            mouseLastPos = mousePos;
            mouseIsLeftSide = mousePos.x < screenMidX;
            isMouseDragging = true;
        }
        else if (isMouseDragging && mouse.leftButton.isPressed)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Vector2 delta = mousePos - mouseLastPos;

            if (Mathf.Abs(delta.x) >= swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                if (delta.x < 0f)
                {
                    if (mouseIsLeftSide)
                    {
                        if (leftShip != null) leftShip.MoveLeft();
                    }
                    else
                    {
                        if (rightShip != null) rightShip.MoveLeft();
                    }
                }
                else
                {
                    if (mouseIsLeftSide)
                    {
                        if (leftShip != null) leftShip.MoveRight();
                    }
                    else
                    {
                        if (rightShip != null) rightShip.MoveRight();
                    }
                }

                mouseLastPos = mousePos;
            }
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (isMouseDragging)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                Vector2 delta = mousePos - mouseLastPos;

                if (Mathf.Abs(delta.x) >= swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x < 0f)
                    {
                        if (mouseIsLeftSide)
                        {
                            if (leftShip != null) leftShip.MoveLeft();
                        }
                        else
                        {
                            if (rightShip != null) rightShip.MoveLeft();
                        }
                    }
                    else
                    {
                        if (mouseIsLeftSide)
                        {
                            if (leftShip != null) leftShip.MoveRight();
                        }
                        else
                        {
                            if (rightShip != null) rightShip.MoveRight();
                        }
                    }
                }
            }
            isMouseDragging = false;
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