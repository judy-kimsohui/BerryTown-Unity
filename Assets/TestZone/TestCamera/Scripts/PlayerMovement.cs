using UnityEngine;

namespace TestCamera
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private PlayerInputHandler inputHandler;
        private float rotationVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputHandler = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            Move();
        }

        private void Move()
        {
            // 입력값 읽기
            Vector2 moveInput = inputHandler.MoveInput;
            Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y).normalized;

            if (direction.magnitude >= 0.1f)
            {
                // 카메라 기준으로 회전 방향 계산
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg 
                                    + cameraTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);

                // 플레이어 회전 적용
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                // 이동 방향 벡터 (카메라 기준)
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                // CharacterController로 이동
                controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
            }
        }
    }
}
