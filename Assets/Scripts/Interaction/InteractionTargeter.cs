using System;
using System.Collections.Generic;
using Daeume.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Interaction
{
    public sealed class InteractionTargeter : MonoBehaviour
    {
        public const string InteractActionName = "Interact";

        [SerializeField] private InputActionReference interactAction;
        [SerializeField, Min(0.01f)] private float range = 1.25f;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private float facingDirection = 1f;

        private IInteractable current;
        private InputAction interact;

        public IInteractable Current => current;

        private void Awake()
        {
            var playerInput = GetComponentInParent<PlayerInput>();
            interact = interactAction == null
                ? playerInput?.actions?.FindAction(InteractActionName)
                : interactAction.action;
        }

        private void OnEnable() => interact?.Enable();

        private void OnDisable()
        {
            interact?.Disable();
            SetCurrent(null);
        }

        private void Update()
        {
            RefreshTarget();
            if (interact != null && interact.WasPressedThisFrame())
            {
                TryInteract();
            }
        }

        public void SetFacingDirection(float direction)
        {
            if (!Mathf.Approximately(direction, 0f))
            {
                facingDirection = Mathf.Sign(direction);
            }
        }

        public IInteractable RefreshTarget()
        {
            if (!IsInteractionStateAllowed())
            {
                SetCurrent(null);
                return null;
            }

            var overlaps = Physics2D.OverlapCircleAll(transform.position, range, targetMask);
            var candidates = new List<Candidate>(overlaps.Length);
            var seen = new HashSet<IInteractable>();
            for (var index = 0; index < overlaps.Length; index++)
            {
                var target = FindInteractable(overlaps[index]);
                if (target == null || !seen.Add(target) || !target.CanInteract(gameObject))
                {
                    continue;
                }

                var component = target as Component;
                if (component == null)
                {
                    continue;
                }

                var offset = component.transform.position - transform.position;
                candidates.Add(new Candidate(target, offset.sqrMagnitude, Mathf.Sign(offset.x) == facingDirection));
            }

            candidates.Sort(Candidate.Compare);
            SetCurrent(candidates.Count == 0 ? null : candidates[0].Target);
            return current;
        }

        public bool TryInteract()
        {
            if (!IsInteractionStateAllowed() || current == null || !current.CanInteract(gameObject))
            {
                return false;
            }

            current.Interact(gameObject);
            return true;
        }

        private bool IsInteractionStateAllowed()
        {
            var state = GameManager.Instance == null ? StageState.Explore : GameManager.Instance.StageState;
            return state != StageState.Memory && state != StageState.Failed && state != StageState.Cleared;
        }

        private void SetCurrent(IInteractable next)
        {
            if (ReferenceEquals(current, next))
            {
                return;
            }

            current = next;
            if (current == null)
            {
                GameManager.Instance?.Events.Publish(new InteractionPromptChanged(false, string.Empty, string.Empty));
                return;
            }

            var prompt = current.GetPrompt();
            GameManager.Instance?.Events.Publish(
                new InteractionPromptChanged(true, prompt.ActionName, prompt.StringTableKey));
        }

        private static IInteractable FindInteractable(Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            foreach (var component in collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        private readonly struct Candidate
        {
            public Candidate(IInteractable target, float distanceSquared, bool facesTarget)
            {
                Target = target;
                DistanceSquared = distanceSquared;
                FacesTarget = facesTarget;
            }

            public IInteractable Target { get; }
            private float DistanceSquared { get; }
            private bool FacesTarget { get; }

            public static int Compare(Candidate left, Candidate right)
            {
                var distance = left.DistanceSquared.CompareTo(right.DistanceSquared);
                if (distance != 0)
                {
                    return distance;
                }

                var facing = right.FacesTarget.CompareTo(left.FacesTarget);
                return facing != 0
                    ? facing
                    : string.Compare(left.Target.StableId, right.Target.StableId, StringComparison.Ordinal);
            }
        }
    }
}
