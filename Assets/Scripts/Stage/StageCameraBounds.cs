using Daeume.ContaminationRuntime;
using Daeume.Player;
using UnityEngine;

namespace Daeume.Stage
{
    /// <summary>
    /// 카메라가 플레이어를 따라가되 화면이 정해진 범위 밖을 비추지 않게 한다. (spec-007 blockout 범위)
    ///
    /// 경계가 없으면 레벨 밖의 빈 공간이 화면에 들어와 "만들다 만" 인상을 준다.
    ///
    /// minimum·maximum은 카메라 중심이 아니라 <b>화면에 담겨도 되는 세계 좌표의 끝</b>이다.
    /// 클램프할 때 화면 반너비·반높이를 빼므로, 종횡비가 달라져도 새어 나오지 않는다.
    /// 예전에는 중심 좌표를 그대로 클램프해서, 값을 16:9에 맞춰 놓으면 더 넓은 화면에서
    /// 그만큼 빈 공간이 드러났다. 스테이지마다 종횡비별로 값을 다시 재야 했다.
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

        /// <summary>켜면 시작할 때 이 오브젝트 아래의 콘텐츠에서 경계를 직접 구한다.</summary>
        [SerializeField] private bool autoFitToContent;

        /// <summary>자동으로 구한 경계를 안쪽으로 물리는 여유. 가장자리 아트가 지저분할 때 쓴다.</summary>
        [SerializeField] private Vector2 autoFitInset = new(0.35f, 0f);

        private Transform target;
        private Camera targetCamera;
        private ContaminationDirector director;

        private void Awake()
        {
            if (autoFitToContent) FitToContent();
        }

        /// <summary>
        /// 이 오브젝트 아래에 실제로 놓인 것에서 경계를 구한다. 스테이지마다 좌표를 손으로
        /// 재지 않아도 되게 하는 장치다.
        /// </summary>
        /// <remarks>
        /// 자기 하위만 훑는다. 하늘(StageSkyBackground)은 스테이지 루트 밖에 있어 자연히 빠진다.
        /// 이게 중요하다 - 하늘을 포함하면 경계가 화면 몇 개 분량으로 벌어져 아무 의미가 없어진다.
        ///
        /// 지면은 TilemapRenderer 대신 TilemapCollider2D를 쓴다. 렌더러 경계는 빈 셀까지 포함해
        /// 실제 그림보다 넓게 나온다(Stage 01에서 오른쪽으로 0.5유닛 더 나왔다).
        ///
        /// 꺼져 있는 것은 세지 않는다. 추격 전까지 비활성인 트라우마가 경계를 끌어당기면 안 된다.
        /// </remarks>
        public void FitToContent()
        {
            var found = false;
            var content = new Bounds();

            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(false))
            {
                if (renderer.sprite == null) continue;
                if (found) content.Encapsulate(renderer.bounds);
                else { content = renderer.bounds; found = true; }
            }

            foreach (var collider in GetComponentsInChildren<UnityEngine.Tilemaps.TilemapCollider2D>(false))
            {
                if (found) content.Encapsulate(collider.bounds);
                else { content = collider.bounds; found = true; }
            }

            if (!found) return;

            minimum = new Vector2(content.min.x + autoFitInset.x, content.min.y + autoFitInset.y);
            maximum = new Vector2(content.max.x - autoFitInset.x, content.max.y - autoFitInset.y);
        }

        /// <summary>화면에 담겨도 되는 세계 좌표의 왼쪽·아래 끝.</summary>
        public Vector2 Minimum => minimum;

        /// <summary>화면에 담겨도 되는 세계 좌표의 오른쪽·위 끝.</summary>
        public Vector2 Maximum => maximum;

        public float FixedCameraY => fixedCameraY;

        /// <summary>
        /// 주어진 카메라로 이 경계를 지킬 때 카메라 중심이 들어갈 수 있는 범위.
        /// 테스트와 다른 스테이지가 같은 계산을 다시 쓰지 않도록 여기서 한 번만 정의한다.
        /// </summary>
        /// <remarks>
        /// 경계가 화면보다 좁으면 뺄셈 결과가 뒤집힌다. 그때는 양쪽으로 새는 대신
        /// 경계 한가운데에 고정해 좌우로 균등하게 넘치게 한다.
        /// </remarks>
        public static void ResolveCameraLimits(Camera camera, Vector2 minimum, Vector2 maximum, out Vector2 lower, out Vector2 upper)
        {
            var halfHeight = camera == null ? 0f : camera.orthographicSize;
            var halfWidth = camera == null ? 0f : halfHeight * camera.aspect;

            lower = new Vector2(minimum.x + halfWidth, minimum.y + halfHeight);
            upper = new Vector2(maximum.x - halfWidth, maximum.y - halfHeight);

            if (lower.x > upper.x) lower.x = upper.x = (minimum.x + maximum.x) * 0.5f;
            if (lower.y > upper.y) lower.y = upper.y = (minimum.y + maximum.y) * 0.5f;
        }

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

            ResolveCameraLimits(targetCamera, minimum, maximum, out var lower, out var upper);

            position.x = Mathf.Clamp(lookaheadX, lower.x, upper.x);
            if (followVertical)
            {
                position.y = Mathf.Clamp(target.position.y, lower.y, upper.y);
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
