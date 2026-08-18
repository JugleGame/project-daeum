using Daeume.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        public const string MoveActionName = "Move";
        public const string JumpActionName = "Jump";
        public const string GrabActionName = "Grab";

        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference grabAction;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float jumpVelocity = 8f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.75f;
        [SerializeField, Min(0.01f)] private float grabHoldSeconds = 1.5f;
        [SerializeField] private Transform groundProbe;
        [SerializeField, Min(0.01f)] private float groundProbeRadius = 0.08f;
        [SerializeField] private LayerMask groundMask = ~0;

        private Rigidbody2D body;
        private InputAction move;
        private InputAction jump;
        private InputAction grab;
        private GrabbableSurface grabCandidate;
        private Vector2 moveInput;
        private float grabRemaining;
        private bool grounded;

        public bool IsGrounded => grounded;
        public bool IsGrabbing { get; private set; }
        public float GrabHoldSeconds => grabHoldSeconds;
        public float FacingDirection { get; private set; } = 1f;
        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            var playerInput = GetComponentInParent<PlayerInput>();
            move = moveAction == null ? playerInput?.actions?.FindAction(MoveActionName) : moveAction.action;
            jump = jumpAction == null ? playerInput?.actions?.FindAction(JumpActionName) : jumpAction.action;
            grab = grabAction == null ? playerInput?.actions?.FindAction(GrabActionName) : grabAction.action;
        }

        private void OnEnable()
        {
            move?.Enable();
            jump?.Enable();
            grab?.Enable();
            GameManager.Instance?.Events.Subscribe<PlayerRestoreRequested>(Restore);
        }

        private void OnDisable()
        {
            move?.Disable();
            jump?.Disable();
            grab?.Disable();
            GameManager.Instance?.Events.Unsubscribe<PlayerRestoreRequested>(Restore);
        }

        private void Update()
        {
            if (!InputEnabled)
            {
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

        private void FixedUpdate()
        {
            RefreshGrounded();
            if (IsGrabbing)
            {
                TickGrab(Time.fixedDeltaTime, moveInput.y < -0.5f);
                return;
            }

            var control = grounded ? 1f : airControl;
            body.linearVelocity = new Vector2(moveInput.x * moveSpeed * control, body.linearVelocity.y);
        }

        public void SetMoveInput(float value)
        {
            SetMoveInput(new Vector2(value, 0f));
        }

        public void SetMoveInput(Vector2 value)
        {
            moveInput = Vector2.ClampMagnitude(value, 1f);
            if (!Mathf.Approximately(moveInput.x, 0f))
            {
                FacingDirection = Mathf.Sign(moveInput.x);
            }
        }

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
                return true;
            }

            if (!grounded)
            {
                return false;
            }

            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
            grounded = false;
            return true;
        }

        public bool TryBeginGrab(GrabbableSurface surface)
        {
            if (!InputEnabled || surface == null || grounded)
            {
                return false;
            }

            IsGrabbing = true;
            grabRemaining = grabHoldSeconds;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
            return true;
        }

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

        public void SetGroundedForTest(bool value) => grounded = value;

        private void ReleaseGrab()
        {
            IsGrabbing = false;
            body.gravityScale = 1f;
        }

        private void RefreshGrounded()
        {
            var probePosition = groundProbe == null ? transform.position : groundProbe.position;
            grounded = false;
            foreach (var overlap in Physics2D.OverlapCircleAll(probePosition, groundProbeRadius, groundMask))
            {
                if (overlap != null && !overlap.transform.IsChildOf(transform))
                {
                    grounded = true;
                    break;
                }
            }
        }

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
            if (grabCandidate != null && other.GetComponentInParent<GrabbableSurface>() == grabCandidate)
            {
                grabCandidate = null;
            }
        }

        private void Restore(PlayerRestoreRequested request)
        {
            transform.position = request.Position;
            body.position = request.Position;
            body.linearVelocity = Vector2.zero;
            InputEnabled = true;
            GetComponent<PlayerHealth>()?.Restore(request.Health);
        }

    }
}
