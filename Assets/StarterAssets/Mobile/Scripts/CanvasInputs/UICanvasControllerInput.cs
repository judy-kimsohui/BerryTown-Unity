using UnityEngine;

namespace PersonController
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        public PersonControllerInputs PersonControllerInputs;

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            PersonControllerInputs.MoveInput(virtualMoveDirection);
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            PersonControllerInputs.LookInput(virtualLookDirection);
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            PersonControllerInputs.JumpInput(virtualJumpState);
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            PersonControllerInputs.SprintInput(virtualSprintState);
        }
        
    }

}
