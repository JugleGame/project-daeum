using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// Director가 지정한 거리를 유지하면서 플레이어의 X/Y 위치를 직접 따라가는 비행형 추격자다.
    /// 중력, 점프, 착지, 벽타기 상태를 사용하지 않으므로 지형 높이와 관계없이 같은 규칙으로 이동한다.
    /// </summary>
    [RequireComponent(typeof(TraumaContactSource), typeof(CircleCollider2D))]
    public sealed class TraumaChaseActor : MonoBehaviour
    {
        public ChaseDirectiveIssued LastDirective { get; private set; }
        public int AppliedDirectiveCount { get; private set; }
        public float LastHorizontalMovement { get; private set; }

        /// <summary>
        /// 지시를 받아 목표 지점 쪽으로 정해진 속도만큼 이동한다.
        /// MoveTowards는 "목표를 지나치지 않고 정확히 멈추는" 이동이라 거리 유지에 적합하다.
        /// </summary>
        public void ApplyDirective(ChaseDirectiveIssued directive, float deltaTime, float targetDistance)
        {
            LastDirective = directive;
            AppliedDirectiveCount++;
            LastHorizontalMovement = 0f;
            if (deltaTime <= 0f) return;

            var position = (Vector2)transform.position;
            var side = Mathf.Approximately(directive.PursuerPosition.x, directive.PlayerPosition.x)
                ? 1f
                : Mathf.Sign(directive.PursuerPosition.x - directive.PlayerPosition.x);
            var target = new Vector2(
                directive.PlayerPosition.x + side * Mathf.Max(0f, targetDistance),
                directive.PlayerPosition.y);
            var next = Vector2.MoveTowards(position, target, Mathf.Max(0f, directive.Speed) * deltaTime);

            LastHorizontalMovement = next.x - position.x;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }
    }
}
