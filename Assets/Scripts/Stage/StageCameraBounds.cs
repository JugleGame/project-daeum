using Daeume.ContaminationRuntime;
using Daeume.Player;
using UnityEngine;

namespace Daeume.Stage
{
    /// <summary>
    /// 카메라가 플레이어를 따라가되 정해진 범위와 스테이지별 프레이밍을 유지한다.
    /// </summary>
    public sealed class StageCameraBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 minimum = new(-2f, -2f);
        [SerializeField] private Vector2 maximum = new(28f, 4f);
        [SerializeField] private bool followVertical;
        [SerializeField, Min(0.01f)] private float orthographicSize = 4.21875f;
        [SerializeField] private float fixedCameraY;

        private Transform target;
        private Camera targetCamera;
        private ContaminationDirector director;

        public Vector2 Minimum => minimum;
        public Vector2 Maximum => maximum;
        public float OrthographicSize => orthographicSize;
        public float FixedCameraY => fixedCameraY;

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                target = player == null ? null : player.transform;
            }

            targetCamera ??= Camera.main;
            if (target == null || targetCamera == null)
                return;

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = orthographicSize;

            director ??= FindAnyObjectByType<ContaminationDirector>();

            var position = targetCamera.transform.position;
            var lookaheadX = target.position.x;
            if (director != null && director.ChaseActive && director.Data != null)
            {
                // 추격 중에는 진행 방향인 왼쪽을 더 넓게 보여 준다.
                lookaheadX -= director.Data.ChaseLookaheadUnits;
            }

            position.x = Mathf.Clamp(lookaheadX, minimum.x, maximum.x);
            position.y = followVertical
                ? Mathf.Clamp(target.position.y, minimum.y, maximum.y)
                : fixedCameraY;

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
