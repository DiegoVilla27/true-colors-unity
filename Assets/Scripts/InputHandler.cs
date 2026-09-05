using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class InputHandler : MonoBehaviour
{
  [SerializeField] private ShipController leftShip;
  [SerializeField] private ShipController rightShip;

  void OnEnable()
  {
    EnhancedTouchSupport.Enable();
  }

  void OnDisable()
  {
    EnhancedTouchSupport.Disable();
  }

  void Update()
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
      return;
    }

    // 1. Manejo multitáctil en móvil
    if (Touch.activeTouches.Count > 0)
    {
      float screenMidX = Screen.width * 0.5f;

      foreach (var touch in Touch.activeTouches)
      {
        if (touch.phase == TouchPhase.Began)
        {
          Vector2 touchPos = touch.screenPosition;

          if (touchPos.x < screenMidX)
          {
            float leftHalfCenter = screenMidX * 0.5f;
            if (touchPos.x < leftHalfCenter) leftShip.MoveLeft();
            else leftShip.MoveRight();
          }
          else
          {
            float rightHalfCenter = screenMidX + (screenMidX * 0.5f);
            if (touchPos.x < rightHalfCenter) rightShip.MoveLeft();
            else rightShip.MoveRight();
          }
        }
      }
    }

    // 2. Teclado en Editor y builds de Escritorio con New Input System
#if UNITY_EDITOR || UNITY_STANDALONE
    HandleKeyboardTesting();
#endif
  }

  private void HandleKeyboardTesting()
  {
    var keyboard = Keyboard.current;
    if (keyboard == null) return;

    if (keyboard.aKey.wasPressedThisFrame) leftShip.MoveLeft();
    if (keyboard.dKey.wasPressedThisFrame) leftShip.MoveRight();
    if (keyboard.leftArrowKey.wasPressedThisFrame) rightShip.MoveLeft();
    if (keyboard.rightArrowKey.wasPressedThisFrame) rightShip.MoveRight();
  }
}