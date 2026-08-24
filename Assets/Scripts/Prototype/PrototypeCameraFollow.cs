using UnityEngine;

namespace Daeume.Prototype
{
    public sealed class PrototypeCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float minimumX = -4f;
        [SerializeField] private float maximumX = 6f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var position = transform.position;
            position.x = Mathf.Clamp(target.position.x, minimumX, maximumX);
            transform.position = position;
        }
    }
}
