// 플레이어의 이동을 담당

using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]

    public class PlayerController : MonoBehaviour
    {

        #region Variables

        public GameObject Player;

        // 플레이어 WASD
        [Header("MoveInput")]

        [Tooltip("이동 속도")]
        public float MoveSpeed = 2.0f;

        [Tooltip("회전 보간 시간")]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("가속도")]
        public float Acceleration = 10.0f;


        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;


        // Cinemachine
        public float playerYaw;
        public float playerPitch;


        // 점프 Space
        [Header("JumpInput & Fall")]
        [Tooltip("점프 높이")]
        public float JumpHeight = 1.2f;

        [Tooltip("중력 가속도")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("점프 시간")]
        public float JumpTimeout = 0.50f;

        // 착지
        [Tooltip("착지 시간")]
        public float FallTimeout = 0.15f;

        [Tooltip("플레이어가 땅 위에 있는지, 공중에 있는지 체크")]
        public bool Grounded = true;

        [Tooltip("보간 상수")]
        public float GroundedOffset = -0.14f;

        [Tooltip("그라운드 체크 Collider 반지름")]
        public float GroundedRadius = 0.28f;

        [Tooltip("그라운드 레이어")]
        public LayerMask GroundLayers;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;


        // 애니메이션
        // animation IDs
        private bool _hasAnimator;
        private int _animIDSpeed;
        private int _animIDMotionSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;


        // 인풋 시스템
        private PlayerInput _playerInput;

        private Animator _animator;
        private CharacterController _controller;

        // 인풋 액션 _input
        private InputHandler _input;


        [Header("Main Camera")]
        [SerializeField] private GameObject MainCamera;

        #endregion

        private void Awake()
        {
            // get a reference to our main camera
            if (MainCamera == null)
            {
                // 게임 오브젝트를 태그로 찾음
                MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }


        private void Start()
        {
            // Target : 카메라가 따라다닐 위치(또는 오브젝트)
            // 오브젝트의 현재 회전 각도 중 Y축 값

            // transform.position → 위치 (Vector3)
            // transform.rotation → 회전 (Quaternion) → eulerAngles로 변환(x, y, z축 기준의 회전각(Vector3)) 단위: 도°, 예: (0, 90, 0)
            // Y축 회전각 → 지금 오브젝트가 Y축으로 몇 도 돌아가 있는가?”
            // transform.localScale → 크기 (Vector3)
            playerYaw = Player.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<InputHandler>();

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
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
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
            MoveInput();
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

                // JumpInput 입력이 있을 때 초기 속력을 붙여서 위로 점프 (수직속도)
                if (_input.JumpInput && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 점프 애니메이션 작동
                    if (_hasAnimator){ _animator.SetBool(_animIDJump, true); }
                }

                // JumpInput 시간을 측정
                if (_jumpTimeoutDelta >= 0.0f){ _jumpTimeoutDelta -= Time.deltaTime; }
            }

            // **** 공중이라면 착지 애니메이션 작동
            else
            {
                // reset the JumpInput timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall 시간을 설정
                if (_fallTimeoutDelta >= 0.0f) { _fallTimeoutDelta -= Time.deltaTime; }

                // 착지 애니메이션 설정
                else { if (_hasAnimator) { _animator.SetBool(_animIDFreeFall, true); } }

                // 점프를 끝냄
                _input.JumpInput = false;
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
        private void MoveInput()
        {
            float targetSpeed = MoveSpeed;

            // 움직이는 벡터가 0이라면 속력도 0입니다
            if (_input.MoveInput == Vector2.zero) targetSpeed = 0.0f;

            // 플레이어의 현재 수평 속도 = x축 + z축을 합성한 벡터의 길이(magnitude)
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            // speedOffset : 허용 오차 (이 정도 차이는 그냥 같다고 보자)
            float speedOffset = 0.1f;
            float inputMagnitude = 1f;

            
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
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * Acceleration);

                // 십진법의 3번째 소수점까지 반올림 / round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // Input 방향
            Vector3 inputDirection = new Vector3(_input.MoveInput.x, 0.0f, _input.MoveInput.y).normalized;

            // 움직이는 경우
            if (_input.MoveInput != Vector2.zero)
            {
                // Atan2 : 두 점 사이의 각도(라디안) 를 구하는 함수, 방향 벡터를 각도로 바꾸는 역할
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + MainCamera.transform.eulerAngles.y;
                
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
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * Acceleration);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // 플레이어 애니메이션 실행
            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

    }
}