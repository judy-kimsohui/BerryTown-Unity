using UnityEngine;
using UnityEngine.InputSystem;

namespace TestCamera
{
    public class PlayerInputHandler : MonoBehaviour
    {
        // 이동 입력 (WASD)
        public Vector2 MoveInput { get; private set; }

        // 카메라 전환 입력
        public bool Camera_PlayerPressed { get; private set; }
        public bool Camera_NPC1Pressed { get; private set; }
        public bool Camera_NPC2Pressed { get; private set; }
        public bool Camera_NPC3Pressed { get; private set; }
        public bool Camera_CutScenePressed { get; private set; }

        // --- 이동 ---
        public void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
        }

        // --- 카메라 전환 ---
        public void OnCamera_Player(InputValue value){ Camera_PlayerPressed = value.isPressed; }
        public void OnCamera_NPC1(InputValue value) { Camera_NPC1Pressed = value.isPressed; }
        public void OnCamera_NPC2(InputValue value) { Camera_NPC2Pressed = value.isPressed; }
        public void OnCamera_NPC3(InputValue value) { Camera_NPC3Pressed = value.isPressed; }
        public void OnCamera_CutScene(InputValue value) { Camera_CutScenePressed = value.isPressed; }
    }
}
