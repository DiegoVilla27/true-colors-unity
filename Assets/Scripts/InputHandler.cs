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

    // 2. Teclado en Editor con New Input System
#if UNITY_EDITOR
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