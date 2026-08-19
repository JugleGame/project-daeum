using Daeume.Player;
using UnityEngine;

namespace Daeume.Stage
{
    public sealed class StageCameraBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 minimum = new(-2f, -2f);
        [SerializeField] private Vector2 maximum = new(28f, 4f);
        [SerializeField] private bool followVertical;

        private Transform target;
        private Camera targetCamera;

        public Vector2 Minimum => minimum;
        public Vector2 Maximum => maximum;

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                target = player == null ? null : player.transform;
            }

            targetCamera ??= Camera.main;
            if (target == null || targetCamera == null)
            {
                return;
            }

            var position = targetCamera.transform.position;
            position.x = Mathf.Clamp(target.position.x, minimum.x, maximum.x);
            if (followVertical)
            {
                position.y = Mathf.Clamp(target.position.y, minimum.y, maximum.y);
            }

            targetCamera.transform.position = position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            var center = (minimum + maximum) * 0.5f;
            var size = maximum - minimum;
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 0f));
        }
    }
}
