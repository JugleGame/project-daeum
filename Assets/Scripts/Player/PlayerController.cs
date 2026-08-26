using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Player
{
    /// <summary>
    /// 플레이어의 이동·점프·붙잡기(매달리기)를 담당한다. (spec-002)
    ///
    /// [RequireComponent(typeof(Rigidbody2D))]:
    /// 이 스크립트를 붙이면 유니티가 Rigidbody2D(물리 몸체)를 자동으로 함께 붙이고, 지우지 못하게 막는다.
    /// 물리 몸체 없이 이 코드가 돌면 즉시 오류가 나므로 아주 적절한 안전장치다.
    ///
    /// 이 게임의 시그니처 동사는 "붙잡기"다. 친구가 잡아 준 기억 → 혼자 붙드는 행동 →
    /// 마지막에 내려놓기로 이어지는 주제와 연결돼 있어(design-decisions 4번) 잘라낼 수 없는 기능이다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        // 입력을 물리 키(W/A/S/D)가 아니라 "액션 이름"으로 찾는다.
        // 그래야 키 재설정(리매핑)을 해도 코드를 고칠 필요가 없다. spec-002/013의 필수 요구사항이다.
        public const string MoveActionName = "Move";
        public const string JumpActionName = "Jump";
        public const string GrabActionName = "Grab";

        // [SerializeField]: private이지만 유니티 인스펙터 창에 노출돼 값을 조정할 수 있게 한다.
        // 튜닝 값을 코드 수정 없이 바꾸기 위한 표준 방식이다.
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference grabAction;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0.05f)] private float footstepInterval = 0.36f;
        [SerializeField, Min(0f)] private float jumpVelocity = 4.5f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.75f;   // 공중에서의 조작 비율(1이면 지상과 동일)
        [SerializeField, Min(0.01f)] private float grabHoldSeconds = 1.5f;  // spec-002의 GrabHoldSeconds
        [SerializeField] private Transform groundProbe;                     // 발밑 검사 위치(자식 오브젝트)
        [SerializeField, Min(0.01f)] private float groundProbeRadius = 0.08f;
        [SerializeField] private LayerMask groundMask = ~0;                 // 어떤 레이어를 "땅"으로 볼지
        [SerializeField] private SpriteRenderer visualRenderer;             // 바라보는 방향을 뒤집을 스프라이트

        private Rigidbody2D body;
        private PlayerCombat combat;
        private InputAction move;
        private InputAction jump;
        private InputAction grab;
        private GrabbableSurface grabCandidate;   // 지금 붙잡을 수 있는 표면(범위 안에 들어온 것)
        private Vector2 moveInput;
        private float grabRemaining;              // 남은 매달리기 시간
        private bool grounded;
        private float defaultGravityScale = 1f;   // 붙잡기 해제 시 되돌릴 원래 중력 배율
        private float nextFootstepTime;

        public bool IsGrounded => grounded;
        public bool IsGrabbing { get; private set; }

        /// <summary>몸통 스프라이트. 피격 점멸처럼 색을 건드리는 연출이 함께 쓴다.</summary>
        public SpriteRenderer VisualRenderer => visualRenderer;
        public float GrabHoldSeconds => grabHoldSeconds;
        public float HorizontalInput => moveInput.x;
        public float FacingDirection { get; private set; } = 1f;  // 1=오른쪽, -1=왼쪽. 상호작용 대상 선택에도 쓰인다.

        /// <summary>연출 중 조작을 잠글 때 사용한다(붙잡히는 연출, 회상 재생 등).</summary>
        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            combat = GetComponentInChildren<PlayerCombat>(true);
            if (visualRenderer == null) visualRenderer = GetComponentInChildren<SpriteRenderer>();

            // 수정: 예전에는 붙잡기 해제 시 중력 배율을 1로 "고정 대입"했다.
            // 프리팹이 중력 배율을 1이 아닌 값(예: 3)으로 쓰면 붙잡기 한 번에 낙하 감각이 영구히 바뀌는 버그가 된다.
            // 그래서 시작 시점의 값을 기억해 두고 해제 때 그 값으로 되돌린다.
            defaultGravityScale = body.gravityScale;

            // 인스펙터에서 액션을 직접 지정하지 않았으면 PlayerInput이 들고 있는 액션 맵에서 이름으로 찾는다.
            // 둘 다 지원하므로 테스트에서는 액션을 주입하고, 실제 씬에서는 PlayerInput을 쓰는 식으로 유연하다.
            var playerInput = GetComponentInParent<PlayerInput>();

            // spec-013 컨트롤 리매핑: 저장된 재배정 결과를 실제 액션에 적용한다.
            // 액션을 찾기 전에 적용해야 이후 ReadValue가 바뀐 키를 곧바로 읽는다.
            var overridesJson = FindAnyObjectByType<SceneFlowController>()?.CurrentData?.AssistSettings?.BindingOverridesJson;
            if (!string.IsNullOrEmpty(overridesJson) && playerInput?.actions != null)
            {
                playerInput.actions.LoadBindingOverridesFromJson(overridesJson);
            }

            move = moveAction == null ? playerInput?.actions?.FindAction(MoveActionName) : moveAction.action;
            jump = jumpAction == null ? playerInput?.actions?.FindAction(JumpActionName) : jumpAction.action;
            grab = grabAction == null ? playerInput?.actions?.FindAction(GrabActionName) : grabAction.action;
        }

        // OnEnable/OnDisable: 오브젝트가 켜지고 꺼질 때 호출된다.
        // 입력 활성화와 이벤트 구독을 여기서 짝을 맞춰 처리해야 죽은 구독이 남지 않는다.
        private void OnEnable()
        {
            move?.Enable();
            jump?.Enable();
            grab?.Enable();
            GameManager.Instance?.Events.Subscribe<PlayerRestoreRequested>(Restore);
            GameManager.Instance?.Events.Subscribe<StageStateChanged>(OnStageStateChanged);
        }

        private void OnDisable()
        {
            move?.Disable();
            jump?.Disable();
            grab?.Disable();
            GameManager.Instance?.Events.Unsubscribe<PlayerRestoreRequested>(Restore);
            GameManager.Instance?.Events.Unsubscribe<StageStateChanged>(OnStageStateChanged);
        }

        /// <summary>
        /// 회상(Memory) 중에는 이동을 멈춘다. (spec-005: "회상 중 일반 이동·전투 피해를 중지한다")
        /// </summary>
        /// <remarks>
        /// 회상 쪽 코드가 플레이어를 직접 붙잡아 잠그게 하면 Memory 모듈이 Player 모듈을 알아야 해서
        /// 역할 간 의존이 늘어난다. 대신 플레이어가 "상태 변경 소식"만 듣고 스스로 잠그게 했다.
        /// 덕분에 회상뿐 아니라 나중에 다른 연출이 Memory 상태를 써도 같은 규칙이 자동 적용된다.
        ///
        /// 붙잡히는 연출(TraumaContactHandler)도 InputEnabled를 쓰지만, 그쪽은 Failed 상태로 넘어가며
        /// 자체적으로 다시 켜 주므로 이 처리와 충돌하지 않는다.
        /// </remarks>
        private void OnStageStateChanged(StageStateChanged message)
        {
            if (message.State == StageState.Memory)
            {
                InputEnabled = false;
                SetMoveInput(0f);
                return;
            }

            // 회상이 끝나 추격/탐색으로 돌아오면 조작을 되돌려 준다.
            if (message.State == StageState.Explore || message.State == StageState.Chase)
            {
                InputEnabled = true;
            }
        }

        // Update: 매 화면 프레임 실행된다. 입력은 프레임 단위로 읽어야 눌림을 놓치지 않는다.
        private void Update()
        {
            if (!InputEnabled)
            {
                // 조작이 잠긴 동안에는 입력을 0으로 눌러 둔다.
                // 이렇게 하지 않으면 잠기기 직전 방향키 값이 남아 캐릭터가 혼자 걸어간다.
                SetMoveInput(0f);
                return;
            }

            if (move != null)
            {
                SetMoveInput(move.ReadValue<Vector2>());
            }

            if (jump != null && jump.WasPressedThisFrame())
            {
                TryJump();
            }

            if (grab != null && grab.WasPressedThisFrame())
            {
                TryBeginGrab(grabCandidate);
            }
        }

        // FixedUpdate: 물리 계산 주기에 맞춰 일정 간격으로 실행된다.
        // 속도(velocity)를 다루는 코드는 반드시 여기에 둬야 프레임 수에 따라 이동 거리가 달라지지 않는다.
        private void FixedUpdate()
        {
            RefreshGrounded();
            if (IsGrabbing)
            {
                // 매달린 동안에는 일반 이동을 하지 않는다.
                // spec-002가 허용한 행동은 "점프로 이탈 / 아래 입력으로 낙하 / 시간 만료 자동 낙하" 뿐이다.
                TickGrab(Time.fixedDeltaTime, moveInput.y < -0.5f);
                return;
            }

            if (combat != null && combat.IsAttacking)
            {
                // 공격 동작이 끝날 때까지 제자리에 선다. 달리면서 공격하면 다리가 멈춘 공격 자세로
                // 미끄러져 애니메이션이 고장 난 것처럼 보였다.
                // y는 여기서도 건드리지 않는다. 공중에서 공격해도 중력은 그대로 작동해야 한다.
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }

            var control = grounded ? 1f : airControl;
            if (grounded && Mathf.Abs(moveInput.x) > 0.05f && Time.time >= nextFootstepTime)
            {
                AudioRuntime.PlaySfx("PlayerWalk");
                nextFootstepTime = Time.time + footstepInterval;
            }

            // y 속도는 건드리지 않는다. 중력이 만든 낙하 속도를 덮어쓰면 점프가 이상해진다.
            body.linearVelocity = new Vector2(moveInput.x * moveSpeed * control, body.linearVelocity.y);
        }

        public void SetMoveInput(float value)
        {
            SetMoveInput(new Vector2(value, 0f));
        }

        /// <summary>이동 입력을 넣는다. 테스트에서 키보드 없이 조작을 흉내 낼 때도 쓴다.</summary>
        public void SetMoveInput(Vector2 value)
        {
            // ClampMagnitude: 대각선 입력이 1보다 커져 더 빨라지는 현상을 막는다.
            moveInput = Vector2.ClampMagnitude(value, 1f);

            // 공격 중과 매달린 동안에는 바라보는 방향을 고정한다.
            //
            // 공격: 제자리에 선 채로 스프라이트만 홱 돌면 이동을 막은 의미가 없다.
            // 매달리기: 벽을 잡은 자세 그대로 좌우 입력만으로 몸이 뒤집혀 벽에 등을 대고
            // 붙어 있는 그림이 됐다. 잡은 순간의 방향이 곧 벽 방향이므로 그대로 둔다.
            //
            // 입력 자체는 계속 기록한다. 동작이 끝나는 즉시 누르고 있던 방향으로 이어서 움직인다.
            if (IsGrabbing || (combat != null && combat.IsAttacking))
            {
                return;
            }

            if (!Mathf.Approximately(moveInput.x, 0f))
            {
                // 입력이 0일 때는 방향을 유지한다. 멈춰 섰다고 정면이 바뀌면 상호작용 대상이 튄다.
                FacingDirection = Mathf.Sign(moveInput.x);
                if (visualRenderer != null) visualRenderer.flipX = FacingDirection < 0f;
            }
        }

        /// <summary>점프. 매달린 상태에서는 "이탈 점프"가 된다(spec-002가 허용한 탈출 경로).</summary>
        public bool TryJump()
        {
            if (!InputEnabled)
            {
                return false;
            }

            if (IsGrabbing)
            {
                ReleaseGrab();
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
                AudioRuntime.PlaySfx("PlayerJump");
                return true;
            }

            if (!grounded)
            {
                // 공중 2단 점프 금지. spec-002의 Test_Player_NoDoubleJump가 이 규칙을 검사한다.
                return false;
            }

            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
            AudioRuntime.PlaySfx("PlayerJump");
            // 같은 프레임에 점프가 두 번 처리되는 것을 막기 위해 즉시 접지 상태를 내린다.
            grounded = false;
            return true;
        }

        /// <summary>지정된 붙잡기 표면에 매달린다. 땅에 서 있을 때는 매달리지 않는다.</summary>
        public bool TryBeginGrab(GrabbableSurface surface)
        {
            if (!InputEnabled || surface == null || grounded)
            {
                return false;
            }

            IsGrabbing = true;
            grabRemaining = grabHoldSeconds;
            body.gravityScale = 0f;        // 중력을 잠시 꺼서 그 자리에 머물게 한다.
            body.linearVelocity = Vector2.zero;

            // 매달린 상태는 전용 애니메이션이 알려 준다. 색을 덧칠하지 않는다.
            return true;
        }

        /// <summary>매달린 시간을 줄이고, 시간이 다하거나 아래 입력이 오면 놓는다.</summary>
        public void TickGrab(float deltaTime, bool dropRequested)
        {
            if (!IsGrabbing)
            {
                return;
            }

            grabRemaining -= Mathf.Max(0f, deltaTime);
            body.linearVelocity = Vector2.zero;
            if (dropRequested || grabRemaining <= 0f)
            {
                ReleaseGrab();
            }
        }

        /// <summary>테스트에서 접지 상태를 강제로 지정한다. 실게임 코드에서는 호출하지 않는다.</summary>
        public void SetGroundedForTest(bool value) => grounded = value;

        private void ReleaseGrab()
        {
            IsGrabbing = false;
            body.gravityScale = defaultGravityScale;
        }

        /// <summary>발밑에 땅이 있는지 검사한다.</summary>
        /// <remarks>
        /// 자기 자신의 콜라이더를 땅으로 착각하지 않도록 IsChildOf로 걸러 낸다. 꼭 필요한 처리다.
        ///
        /// 트리거 콜라이더는 제외한다. OverlapCircleAll은 기본적으로 트리거도 함께 돌려주는데,
        /// GrabbableSurface 같은 트리거 존이 발밑 검사 반경과 겹치면 "땅에 서 있다"고 오판해서
        /// TryBeginGrab의 grounded 가드에 걸려 붙잡기가 아예 시작되지 않는 실제 버그였다.
        /// 땅은 항상 solid(비-트리거) 콜라이더여야 한다.
        ///
        /// 검토 메모: OverlapCircleAll은 호출할 때마다 배열을 새로 만들어 쓰레기(GC)를 만든다.
        /// 지금은 플레이어 1명뿐이라 체감 부담이 없지만, 성능 문제가 보이면
        /// Physics2D.OverlapCircle(비할당 버전)로 바꾸면 된다.
        /// </remarks>
        private void RefreshGrounded()
        {
            var probePosition = groundProbe == null ? transform.position : groundProbe.position;
            var wasGrounded = grounded;
            grounded = false;
            foreach (var overlap in Physics2D.OverlapCircleAll(probePosition, groundProbeRadius, groundMask))
            {
                if (overlap != null && !overlap.isTrigger && !overlap.transform.IsChildOf(transform))
                {
                    grounded = true;
                    break;
                }
            }

            if (!wasGrounded && grounded && body.linearVelocity.y <= 0f)
            {
                AudioRuntime.PlaySfx("PlayerLand");
            }
        }

        // 붙잡기 가능한 표면에 몸이 닿아 있는 동안 후보로 기억해 둔다.
        private void OnTriggerStay2D(Collider2D other)
        {
            var surface = other.GetComponentInParent<GrabbableSurface>();
            if (surface != null)
            {
                grabCandidate = surface;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // 지금 기억 중인 후보에서 벗어났을 때만 지운다.
            // 다른 표면에서 빠져나온 것 때문에 유효한 후보가 사라지는 일을 막는다.
            if (grabCandidate != null && other.GetComponentInParent<GrabbableSurface>() == grabCandidate)
            {
                grabCandidate = null;
            }
        }

        /// <summary>체크포인트 복귀 요청을 받아 위치·속도·체력을 되돌린다.</summary>
        private void Restore(PlayerRestoreRequested request)
        {
            transform.position = request.Position;
            // Rigidbody2D는 자체 위치를 따로 들고 있어, transform만 바꾸면 한 프레임 뒤 원래 자리로 끌려간다.
            // 그래서 body.position도 함께 맞춘다.
            body.position = request.Position;
            body.linearVelocity = Vector2.zero;   // 낙하 속도를 안 지우면 부활 직후 그대로 떨어진다.

            // 붙잡기 도중 사망했을 수 있으므로 매달림 상태와 중력도 원상 복구한다.
            if (IsGrabbing)
            {
                ReleaseGrab();
            }

            InputEnabled = true;
            GetComponent<PlayerHealth>()?.Restore(request.Health);
        }
    }
}
