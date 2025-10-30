using UnityEngine;
 
// Input System이 가능할 경우
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    // 이 스크립트와 꼭 같이 있어야 하는 컴포넌트를 미리 강제하는 장치
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]

        // 이동 속도
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        // 조금 빠르게 뛰기
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;


        // 오디오 클립
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;


        // 점프
        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        
        // 착지
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        
        // 카메라
        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        
        // 애니메이션
        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

// 인풋 시스템
#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        
        // 인풋 액션 _input
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        // 함수처럼 동작하지만 변수처럼 보이는 "프로퍼티" 
        private bool IsCurrentDeviceMouse
        {
            get
            {
                // Input System의 Scheme이 KeyboardMouse
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        // 오브젝트가 활성화될 때 실행됨
        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                // 게임 오브젝트를 태그로 찾음
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        // 
        private void Start()
        {
            // Target : 카메라가 따라다닐 위치(또는 오브젝트)
            // 오브젝트의 현재 회전 각도 중 Y축 값

            // transform.position → 위치 (Vector3)
            // transform.rotation → 회전 (Quaternion) → eulerAngles로 변환(x, y, z축 기준의 회전각(Vector3)) 단위: 도°, 예: (0, 90, 0)
                // Y축 회전각 → 지금 오브젝트가 Y축으로 몇 도 돌아가 있는가?”
            // transform.localScale → 크기 (Vector3)
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            // 플레이어 인풋 시스템
            #if ENABLE_INPUT_SYSTEM 
                _playerInput = GetComponent<PlayerInput>();
            #else
			    Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
            #endif

            // 모든 파라미터를 미리 "해시"로 만들어두어 문자열 비교 연산 줄임, 성능 최적화
            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        // 애니메이션 ID를 해시값으로 등록
        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        // ============================================================================================================//
        // 플레이어 이동
        // ============================================================================================================//

        // 업데이트 함수, 매 프레임마다 작동
        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // 점프 혹은 착지
            JumpAndGravity();
            GroundedCheck();

            // 이동
            Move();
        }

        private void JumpAndGravity()
        {
            // **** 땅이라면 점프 애니메이션 작동
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // 점프 애니메이션, 착지 애니메이션 끄기
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // vertical : 수직
                // 속력이 0 이하가 되면, 떨어진다
                if (_verticalVelocity < 0.0f){ _verticalVelocity = -2f; }

                // Jump 입력이 있을 때 초기 속력을 붙여서 위로 점프 (수직속도)
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 점프 애니메이션 작동
                    if (_hasAnimator){ _animator.SetBool(_animIDJump, true); }
                }

                // jump 시간을 측정
                if (_jumpTimeoutDelta >= 0.0f){ _jumpTimeoutDelta -= Time.deltaTime; }
            }

            // **** 공중이라면 착지 애니메이션 작동
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall 시간을 설정
                if (_fallTimeoutDelta >= 0.0f) { _fallTimeoutDelta -= Time.deltaTime; }

                // 착지 애니메이션 설정
                else { if (_hasAnimator) { _animator.SetBool(_animIDFreeFall, true); } }

                // 점프를 끝냄
                _input.jump = false;
            }

            // 속력에 가속도를 붙입니다!
            if (_verticalVelocity < _terminalVelocity){ _verticalVelocity += Gravity * Time.deltaTime; }
        }

        // 현재 땅 위인지, 공중인지 체크
        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator){ _animator.SetBool(_animIDGrounded, Grounded); }
        }

        // 플레이어의 이동!
        private void Move()
        {
            // sprint : 뛰기 / 걸을건지 뛸건지 초기 속력을 정해줍니다
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // 움직이는 벡터가 0이라면 속력도 0입니다
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // 플레이어의 현재 수평 속도 = x축 + z축을 합성한 벡터의 길이(magnitude)
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            // speedOffset : 허용 오차 (이 정도 차이는 그냥 같다고 보자)
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            
            // ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** 
            // 현재 속도를 목표 속도로 부드럽게 맞춰주는 로직
            // 캐릭터가 갑자기 ‘뚝’ 멈추거나 ‘휙’ 가속하지 않도록 감속·가속을 자연스럽게 만들어줌
                // 현재 속도가 목표 속도보다 충분히 느리거나 빠르면 보정하자
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
                // currentHorizontalSpeed : 현재 속도
                // targetSpeed : 목표로 하고 싶은 속도
            {
                // 선형 보간하여 곡선 속도를 형성
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // 십진법의 3번째 소수점까지 반올림 / round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // Input 방향
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 움직이는 경우
            if (_input.move != Vector2.zero)
            {
                // Atan2 : 두 점 사이의 각도(라디안) 를 구하는 함수, 방향 벡터를 각도로 바꾸는 역할
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                
                // SmoothDampAngle : 현재 각도를 목표 각도로 부드럽게 회전
                    // Lerp가 “선형 보간”이라면, SmoothDampAngle은 “감속하며 도착하는 곡선형 보간”
                    // 속도가 자동으로 조절돼서 자연스러운 회전을 만든다
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                // 플레이어 회전
                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // 플레이어가 바라보는 방향 (오일러 회전각 Y축, Yaw)
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // 컨트롤러에게 이동 요청 - 바라보는 방향으로 _speed 속력만큼 -
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // 플레이어 애니메이션 블렌딩 결정
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // 플레이어 애니메이션 실행
            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }


        // ============================================================================================================//
        // 카메라 전환
        // ============================================================================================================//

        private void LateUpdate()
        {
            // 캐릭터의 위치가 정해진 이후 카메라 전환
            CameraRotation();
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        // 카메라의 회전각을 클램프 (제한)
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}