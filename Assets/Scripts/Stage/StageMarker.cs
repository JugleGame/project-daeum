using UnityEngine;

namespace Daeume.Stage
{
    /// <summary>마커의 용도 구분. 씬 안에서 색으로 구분해 보기 위한 값이기도 하다.</summary>
    public enum StageMarkerKind
    {
        Start,
        FallRecovery,
        RemnantSpawn,
        EncounterTrigger,
        EncounterExit,
        MemoryAnchor,
        ChaseStart,
        Escape
    }

    /// <summary>
    /// 레벨 안의 "약속된 지점"을 표시한다. (spec-007의 marker 규약)
    ///
    /// 왜 필요한가:
    /// B가 만든 레벨 위에 C의 회상 앵커를 놓아야 하는데, C는 그 씬을 열 수 없다(씬 소유권 규칙).
    /// 그래서 B가 "여기가 회상 지점"이라는 마커를 ID와 함께 심어 두고,
    /// C의 코드는 좌표 대신 그 ID를 찾아 배치한다.
    /// 덕분에 B가 레벨을 손봐도 C 코드를 고칠 필요가 없다. 협업 구조상 매우 적합한 방식이다.
    ///
    /// 실제 사용 예: "stage01.memory.anchor.01" → Stage01PresentationBootstrap이 이 ID로 찾는다.
    /// </summary>
    public sealed class StageMarker : MonoBehaviour
    {
        [SerializeField] private string markerId = string.Empty;
        [SerializeField] private StageMarkerKind kind;

        public string MarkerId => markerId;
        public StageMarkerKind Kind => kind;

        /// <summary>
        /// 씬 뷰에서만 보이는 표시(기즈모)를 그린다. 게임 화면에는 나오지 않는다.
        /// </summary>
        /// <remarks>
        /// 마커는 눈에 보이는 형태가 없어서, 이 기즈모가 없으면 레벨 작업 시 위치를 확인할 수 없다.
        /// 개발 편의용 코드이고 빌드 성능에 영향을 주지 않는다(에디터에서만 호출된다).
        /// </remarks>
        private void OnDrawGizmos()
        {
            Gizmos.color = ColorFor(kind);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            // 십자선을 함께 그려, 색을 구분하기 어려운 경우에도 중심점을 정확히 볼 수 있게 한다.
            Gizmos.DrawLine(transform.position + Vector3.left * 0.4f, transform.position + Vector3.right * 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.4f, transform.position + Vector3.up * 0.4f);
        }

        private static Color ColorFor(StageMarkerKind markerKind)
        {
            return markerKind switch
            {
                StageMarkerKind.Start => Color.green,
                StageMarkerKind.FallRecovery => new Color(1f, 0.5f, 0f),
                StageMarkerKind.RemnantSpawn => Color.red,
                StageMarkerKind.EncounterTrigger => Color.yellow,
                StageMarkerKind.EncounterExit => Color.cyan,
                StageMarkerKind.MemoryAnchor => Color.magenta,
                StageMarkerKind.ChaseStart => new Color(0.7f, 0.2f, 1f),
                StageMarkerKind.Escape => Color.white,
                _ => Color.gray
            };
        }
    }
}
