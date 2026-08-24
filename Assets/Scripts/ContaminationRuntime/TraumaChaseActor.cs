using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    [RequireComponent(typeof(TraumaContactSource), typeof(CircleCollider2D))]
    /// <summary>
    /// 트라우마 본체. 감독이 내린 지시를 "실행만" 한다. (spec-006)
    ///
    /// 이 클래스에 추격 종료 판단이나 속도 결정 로직이 없는 것이 핵심이다.
    /// 스펙은 "트라우마 액터는 director가 지시한 목표만 실행하며 스스로 추격 종료를 결정하지 않는다"를
    /// 명시적 검증 항목(Test_Chase_DirectorOwnsChaseLength)으로 두고 있다.
    /// 여기에 판단 코드를 추가하면 그 테스트가 깨지고, 스테이지별 페이싱 설계도 무너진다.
    ///
    /// [RequireComponent(TraumaContactSource)]: 공격 무효·접촉 판정 표식이 항상 함께 붙도록 강제한다.
    /// </summary>
    public sealed class TraumaChaseActor : MonoBehaviour
    {
        public ChaseDirectiveIssued LastDirective { get; private set; }
        public int AppliedDirectiveCount { get; private set; }
        public float LastHorizontalMovement { get; private set; }

        /// <summary>
        /// 지시를 받아 목표 지점 쪽으로 정해진 속도만큼 이동한다.
        /// MoveTowards는 "목표를 지나치지 않고 정확히 멈추는" 이동이라 거리 유지에 적합하다.
        ///
        /// X축(좌우 거리 유지)만 지시를 따르고 Y는 트라우마 자신의 현재 높이를 그대로 쓴다.
        /// 예전에는 목표 지점에 PlayerPosition.y를 그대로 섞어 써서, 플레이어가 점프하면
        /// 트라우마도 같이 점프하는 것처럼 보였다(Y까지 플레이어를 쫓아간 것이 원인).
        /// spec-006은 좌향 도주(수평 추격)만 규정하므로 Y를 플레이어에게 묶을 이유가 없다.
        /// </summary>
        public void ApplyDirective(ChaseDirectiveIssued directive, float deltaTime, float targetDistance)
        {
            LastDirective = directive;
            AppliedDirectiveCount++;
            LastHorizontalMovement = 0f;
            if (deltaTime <= 0f) return;

            var direction = Mathf.Approximately(directive.PursuerPosition.x, directive.PlayerPosition.x)
                ? 1f
                : Mathf.Sign(directive.PursuerPosition.x - directive.PlayerPosition.x);
            var targetX = directive.PlayerPosition.x + direction * Mathf.Max(0f, targetDistance);
            var target = new Vector2(targetX, transform.position.y);
            var previousX = transform.position.x;
            transform.position = Vector2.MoveTowards(transform.position, target, directive.Speed * deltaTime);
            LastHorizontalMovement = transform.position.x - previousX;
        }
    }
}
