using Daeume.Core;
using UnityEngine;

namespace Daeume.Stage
{
    /// <summary>
    /// 발판 밖으로 떨어진 플레이어를 감지하는 낙사 트리거. (#11)
    ///
    /// spec-001은 낙사·함정으로 인한 "보이지 않는 즉사"를 금지한다. 그래서 이 컴포넌트는
    /// 체력을 깎거나 스테이지를 실패시키지 않고, PlayerFellOutOfBounds만 발행한다.
    /// 실제 복귀 처리(SaveSystem.ResolveRespawnHealth 기반)는 SceneFlowController가 맡는다 —
    /// 씬 흐름을 소유한 쪽에서만 복귀 위치를 결정해야 다른 리스폰 경로와 어긋나지 않는다.
    ///
    /// 배치: Stage01_Base 씬의 blockout 바닥보다 한참 아래(예: y &lt; -20)에 폭넓은 트리거로 둔다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class VoidZone : MonoBehaviour
    {
        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other)) GameManager.Instance?.Events.Publish(new PlayerFellOutOfBounds(ResolveRecoveryPosition()));
        }

        /// <summary>
        /// 레벨이 선언한 낙사 복귀 지점을 찾는다. 없으면 null을 돌려주고, 그때는 흐름 쪽이
        /// 예전처럼 저장된 위치로 되돌린다.
        /// </summary>
        /// <remarks>
        /// 마커를 여기서 찾는 이유: 복귀를 결정하는 SceneFlowController(Daeume.Flow)는 StageMarker를
        /// 볼 수 없다. Flow가 Daeume.Stage를 참조하면 Stage → Player → Flow 순환이 생긴다.
        /// VoidZone은 StageMarker와 같은 asmdef라 순환 없이 마커를 읽을 수 있다.
        ///
        /// 낙사는 자주 일어나는 일이 아니라 매번 전체 탐색을 해도 부담이 없다. 대신 캐시를 두면
        /// 씬을 다시 올렸을 때 파괴된 마커를 붙들고 있게 되므로 그쪽이 오히려 위험하다.
        /// </remarks>
        private static Vector2? ResolveRecoveryPosition()
        {
            // 추격 중에는 전용 복귀 지점(ChaseFallRecovery)을 먼저 찾는다.
            // 탐색용 FallRecovery는 구덩이 왼쪽(맵 시작 쪽)에 있는데, 추격은 오른쪽에서 왼쪽으로
            // 달아나는 반대 방향이다. 그대로 쓰면 구덩이에 빠지는 게 구덩이를 건너뛰는 지름길이 된다.
            var preferred = GameManager.Instance?.StageState == StageState.Chase
                ? StageMarkerKind.ChaseFallRecovery
                : StageMarkerKind.FallRecovery;

            Vector2? fallback = null;
            foreach (var marker in FindObjectsByType<StageMarker>(FindObjectsSortMode.None))
            {
                if (marker.Kind == preferred) return marker.transform.position;
                // 추격용 마커를 두지 않은 스테이지는 예전처럼 탐색용 마커로 복귀한다.
                if (marker.Kind == StageMarkerKind.FallRecovery) fallback = marker.transform.position;
            }

            return fallback;
        }

        /// <summary>
        /// 이름/태그가 아니라 IDamageable 종류로 판단한다(EncounterController와 같은 방식).
        /// </summary>
        private static bool IsPlayer(Collider2D other)
        {
            foreach (var behaviour in other.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IDamageable damageable) return damageable.TargetKind == DamageTargetKind.Player;
            }

            return false;
        }
    }
}
