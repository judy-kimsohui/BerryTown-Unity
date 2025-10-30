using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat
{
    public class InputHandler : MonoBehaviour
    {
        // 이동 입력 (WASD)
        public Vector2 MoveInput { get; private set; }
        public bool JumpInput;

        // --- 이동 ---
        public void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            JumpInput = value.isPressed;
        }
    }
}
