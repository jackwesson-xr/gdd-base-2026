 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        /// <summary>Set by the GDD 110 settings window (mouse sensitivity). 0 while a menu is open.</summary>
        public static float LookSensitivity = 1f;
        /// <summary>Extra jumps allowed in the air (developer window). 0 = normal single jump.</summary>
        public static int AirJumps = 0;
        private int _airJumpsUsed;

        [Tooltip("Maximum extra jumps in one airtime. A negative value uses the course setting.")]
        public int MaxAirJumps = -1;
        [Range(0.1f, 1f)] public float AirJumpHeightMultiplier = 1f;

        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Jump Feel")]
        [Min(0f)] public float CoyoteTime = .14f;
        [Min(0f)] public float JumpBuffer = .16f;
        [Min(0f)] public float ApexSpeed = 1.65f;
        [Range(.1f, 1f)] public float ApexGravityMultiplier = .45f;
        [Min(0f)] public float MantleHeight = 1.5f;
        public ParticleSystem JumpDust;

        float coyoteRemaining, bufferedJump;
        Vector3 mantleStart, mantleTop, mantleEnd;
        float mantleElapsed;
        bool mantling, playerJump;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

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

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        [Tooltip("Optional Humanoid skin Animator. The root Animator is used for the original course character.")]
        public Animator CharacterAnimator;
        private Animator _animator;

        private void ResolveAnimator()
        {
            _animator = CharacterAnimator != null ? CharacterAnimator : GetComponent<Animator>();
            _hasAnimator = _animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null;
        }
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            ResolveAnimator();
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = 0f;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f) return;
            ResolveAnimator();

            if (mantling)
            {
                _input.jump = false;
                UpdateMantle();
                return;
            }

            JumpAndGravity();
            if (!mantling) Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        /// <summary>Throw the player upward with a real jump arc (bounce pads, launchers). Gravity takes over from here.</summary>
        public void Launch(float upSpeed)
        {
            mantling = false;
            playerJump = false;
            coyoteRemaining = bufferedJump = 0f;
            _verticalVelocity = upSpeed;
            Grounded = false;
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            if (_hasAnimator) _animator.SetBool(_animIDJump, true);
        }

        /// <summary>
        /// Called when the view cuts to another camera (security cams, cutscenes). For a moment afterwards "forward"
        /// keeps meaning what it meant before the cut and then turns smoothly to match the new view, so holding
        /// forward doesn't suddenly walk you back the way you came.
        /// </summary>
        public static void NotifyCameraCut() { _lastCameraCut = Time.time; }
        static float _lastCameraCut = -10f;
        float _moveYaw;
        bool _moveYawReady;

        float MoveBasisYaw()
        {
            float cam = _mainCamera.transform.eulerAngles.y;
            if (!_moveYawReady) { _moveYaw = cam; _moveYawReady = true; }
            if (Time.time - _lastCameraCut < 3f) _moveYaw = Mathf.MoveTowardsAngle(_moveYaw, cam, 70f * Time.deltaTime);
            else _moveYaw = cam;
            return _moveYaw;
        }

        /// <summary>Point the camera (used when loading a saved position).</summary>
        public void SetLook(float yaw, float pitch)
        {
            _cinemachineTargetYaw = yaw;
            _cinemachineTargetPitch = pitch;
            _verticalVelocity = 0f;
            _jumpTimeoutDelta = 0f;
            coyoteRemaining = bufferedJump = 0f;
            mantling = playerJump = false;
            interestOffset = Vector2.zero;
            Grounded = false;
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                manualLookUntil = Time.unscaledTime + .4f;
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier * LookSensitivity;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * LookSensitivity;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            float pitch = _cinemachineTargetPitch + CameraAngleOverride;
            Vector2 desired = Vector2.zero;
            if (CameraInterest is Vector3 point && !AimFacing && !LockCameraPosition &&
                Time.unscaledTime >= manualLookUntil)
            {
                Vector3 toward = point - CinemachineCameraTarget.transform.position;
                if (toward.sqrMagnitude > .4f)
                {
                    Vector3 angles = Quaternion.LookRotation(toward).eulerAngles;
                    desired = Vector2.ClampMagnitude(new Vector2(
                        Mathf.DeltaAngle(_cinemachineTargetYaw, angles.y), Mathf.DeltaAngle(pitch, angles.x)),
                        CameraInterestDegrees);
                }
            }
            interestOffset = Vector2.MoveTowards(interestOffset, desired, Time.unscaledDeltaTime * 36f);
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(pitch + interestOffset.y,
                _cinemachineTargetYaw + interestOffset.x, 0f);
        }

        public Vector3? CameraInterest { get; set; }
        public float CameraInterestDegrees { get; set; }
        Vector2 interestOffset;
        float manualLookUntil;
        public bool AimFacing { get; set; }
        public float VerticalSpeed => _verticalVelocity;

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = AimFacing ? Mathf.Min(MoveSpeed, 2.55f) : _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            float basisYaw = MoveBasisYaw();

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  basisYaw;
            }
            if (_input.move != Vector2.zero || AimFacing)
            {
                float goal = AimFacing ? _mainCamera.transform.eulerAngles.y : _targetRotation;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, goal, ref _rotationVelocity,
                    RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            bool wasGrounded = Grounded;
            float landingSpeed = -_verticalVelocity;
            var collision = _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if ((collision & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;
            // Only a supporting collision can recharge jumps. Nearby walls and rocks cannot.
            Grounded = _verticalVelocity <= 0f && (collision & CollisionFlags.Below) != 0;
            if (Grounded && !wasGrounded && landingSpeed > 3f)
                EmitDust(landingSpeed > 8f ? 14 : 9);
            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _jumpTimeoutDelta -= dt;
            coyoteRemaining = Grounded ? CoyoteTime : coyoteRemaining - dt;
            bufferedJump = _input.jump ? JumpBuffer : bufferedJump - dt;
            bool jumpRequested = _input.jump || bufferedJump > 0f;
            _input.jump = false;

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                _airJumpsUsed = 0;
                playerJump = false;
                _verticalVelocity = -2f;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }
            }
            else if ((_fallTimeoutDelta -= dt) <= 0f && _verticalVelocity < -ApexSpeed && _hasAnimator)
            {
                _animator.SetBool(_animIDFreeFall, true);
            }

            if (jumpRequested && TryBeginMantle())
                return;

            bool groundJump = (Grounded || coyoteRemaining > 0f) && _jumpTimeoutDelta <= 0f;
            int allowedAirJumps = MaxAirJumps < 0 ? AirJumps : MaxAirJumps;
            if (jumpRequested && (groundJump || (!Grounded && _airJumpsUsed < allowedAirJumps)))
            {
                if (!groundJump) _airJumpsUsed++;
                if (Grounded) EmitDust(8);
                float height = JumpHeight * (groundJump ? 1f : AirJumpHeightMultiplier);
                _verticalVelocity = Mathf.Max(_verticalVelocity, Mathf.Sqrt(height * -2f * Gravity));
                bufferedJump = coyoteRemaining = 0f;
                _jumpTimeoutDelta = JumpTimeout;
                _fallTimeoutDelta = FallTimeout;
                Grounded = false;
                playerJump = true;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                    _animator.SetBool(_animIDFreeFall, false);
                }
            }

            float gravityScale = playerJump && Mathf.Abs(_verticalVelocity) < ApexSpeed ? ApexGravityMultiplier : 1f;
            _verticalVelocity = Mathf.Max(-_terminalVelocity, _verticalVelocity + Gravity * gravityScale * dt);
        }

        bool TryBeginMantle()
        {
            if (MantleHeight <= 0f || AimFacing || _input.move.sqrMagnitude < .16f || _verticalVelocity < -7f)
                return false;

            Vector3 direction = Quaternion.Euler(0f, MoveBasisYaw(), 0f) *
                new Vector3(_input.move.x, 0f, _input.move.y).normalized;
            Vector3 feet = transform.position;
            float radius = _controller.radius;
            const QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore;
            if (!Physics.Raycast(feet + Vector3.up * .37f, direction, out var wall, radius + .55f, GroundLayers, triggers) ||
                Mathf.Abs(wall.normal.y) > .3f || Vector3.Dot(wall.normal, direction) > -.65f)
                return false;

            Vector3 landingRay = wall.point + direction * (radius + .12f);
            landingRay.y = feet.y + MantleHeight + .08f;
            if (!Physics.Raycast(landingRay, Vector3.down, out var ledge, MantleHeight - .30f, GroundLayers, triggers) ||
                ledge.normal.y < .75f)
                return false;

            Vector3 end = ledge.point + Vector3.up * .045f;
            Vector3 top = new Vector3(feet.x, end.y, feet.z);
            Vector3 low = feet + transform.rotation * _controller.center -
                Vector3.up * (_controller.height * .5f - radius);
            Vector3 high = low + Vector3.up * (_controller.height - 2f * radius);
            float sweepRadius = Mathf.Max(.05f, radius - _controller.skinWidth);
            Vector3 across = end - top;
            if (Physics.CapsuleCast(low, high, sweepRadius, Vector3.up, top.y - feet.y, GroundLayers, triggers) ||
                Physics.CapsuleCast(low + top - feet, high + top - feet, sweepRadius, across.normalized,
                    across.magnitude, GroundLayers, triggers))
                return false;

            mantleStart = feet;
            mantleTop = top;
            mantleEnd = end;
            mantleElapsed = 0f;
            mantling = true;
            Grounded = false;
            _verticalVelocity = 0f;
            bufferedJump = coyoteRemaining = 0f;
            transform.rotation = Quaternion.LookRotation(direction);
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, true);
                _animator.SetBool(_animIDFreeFall, false);
                _animator.SetBool(_animIDGrounded, false);
            }
            return true;
        }

        void UpdateMantle()
        {
            mantleElapsed += Time.deltaTime;
            const float riseTime = .18f;
            const float traverseTime = .14f;
            Vector3 target = mantleElapsed < riseTime
                ? Vector3.Lerp(mantleStart, mantleTop, Mathf.SmoothStep(0f, 1f, mantleElapsed / riseTime))
                : Vector3.Lerp(mantleTop, mantleEnd, Mathf.SmoothStep(0f, 1f, (mantleElapsed - riseTime) / traverseTime));
            // Large frame steps still finish the up leg before crossing the wall.
            if (mantleElapsed >= riseTime && transform.position.y < mantleTop.y - .015f)
                _controller.Move(Vector3.up * (mantleTop.y - transform.position.y));
            _controller.Move(target - transform.position);
            if (mantleElapsed < riseTime + traverseTime) return;
            mantling = false;
            _verticalVelocity = -2f;
            if ((transform.position - mantleEnd).sqrMagnitude < .04f) EmitDust(6);
        }

        void EmitDust(int count)
        {
            if (!JumpDust) return;
            var particle = new ParticleSystem.EmitParams
            {
                position = transform.position + Vector3.up * .1f,
            };
            for (int i = 0; i < count; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                particle.velocity = new Vector3(Mathf.Cos(angle), Random.Range(.25f, .75f), Mathf.Sin(angle)) *
                    Random.Range(.9f, 2.4f);
                JumpDust.Emit(particle, 1);
            }
        }

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
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        public void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (_controller != null && FootstepAudioClips != null && FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        public void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (_controller != null && LandingAudioClip != null)
                    AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}