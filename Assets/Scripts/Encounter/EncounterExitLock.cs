using UnityEngine;

namespace Daeume.Encounter
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EncounterExitLock : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer lockRenderer;
        private Collider2D barrier;

        public bool IsLocked { get; private set; }

        public void ConfigureRenderer(SpriteRenderer value)
        {
            lockRenderer = value;
            if (lockRenderer != null) lockRenderer.enabled = IsLocked;
        }

        private void Awake()
        {
            barrier = GetComponent<Collider2D>();
            SetLocked(false);
        }

        public void SetLocked(bool value)
        {
            IsLocked = value;
            if (barrier == null) barrier = GetComponent<Collider2D>();
            barrier.isTrigger = false;
            barrier.enabled = value;
            if (lockRenderer != null) lockRenderer.enabled = value;
        }
    }
}
