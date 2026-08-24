using Daeume.ContaminationRuntime;
using Daeume.Player;
using UnityEngine;

namespace Daeume.Stage
{
    /// <summary>
    /// 카메라가 플레이어를 따라가되 정해진 범위를 벗어나지 않게 한다. (spec-007 blockout 범위)
    ///
    /// 경계가 없으면 레벨 밖의 빈 공간이 화면에 들어와 "만들다 만" 인상을 준다.
    /// followVertical이 꺼져 있으면 좌우로만 따라간다 — 2D 횡스크롤에서 세로 흔들림을 줄이는 흔한 선택이다.
    ///
    /// LateUpdate에서 움직이는 이유: 플레이어 이동이 끝난 뒤에 카메라를 옮겨야 한 프레임 늦게 따라오는
    /// 떨림이 생기지 않는다.
    /// </summary>
    public sealed class StageCameraBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 minimum = new(-2f, -2f);
        [SerializeField] private Vector2 maximum = new(28f, 4f);
        [SerializeField] private bool followVertical;
        [SerializeField] private float fixedCameraY;

        private Transform target;
        private Camera targetCamera;
        private ContaminationDirector director;

        public Vector2 Minimum => minimum;
        public Vector2 Maximum => maximum;
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
            {
                return;
            }

            director ??= FindAnyObjectByType<ContaminationDirector>();

            var position = targetCamera.transform.position;
            var lookaheadX = target.position.x;
            if (director != null && director.ChaseActive && director.Data != null)
            {
                // spec-014: 좌향 도주 중에는 카메라가 진행 방향(왼쪽)을 미리 보여 준다.
                // 플레이어가 화면 중앙보다 오른쪽에 있어야 왼쪽에 더 넓은 시야가 생긴다.
                lookaheadX -= director.Data.ChaseLookaheadUnits;
            }

            position.x = Mathf.Clamp(lookaheadX, minimum.x, maximum.x);
            if (followVertical)
            {
                position.y = Mathf.Clamp(target.position.y, minimum.y, maximum.y);
            }
            else
            {
                // Persistent 카메라는 씬을 바꿔도 살아남으므로 현재 Y를 그대로 두면
                // 세로 추적 스테이지의 마지막 위치를 다음 횡스크롤 스테이지가 상속한다.
                // 스테이지마다 정해 둔 기준 높이를 그 스테이지의 결정적 값으로 쓴다.
                position.y = fixedCameraY;
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
