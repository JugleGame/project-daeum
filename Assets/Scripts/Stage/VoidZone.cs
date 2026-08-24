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
            if (IsPlayer(other)) GameManager.Instance?.Events.Publish(new PlayerFellOutOfBounds());
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
