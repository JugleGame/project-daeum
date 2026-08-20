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

        /// <summary>
        /// 지시를 받아 목표 지점 쪽으로 정해진 속도만큼 이동한다.
        /// MoveTowards는 "목표를 지나치지 않고 정확히 멈추는" 이동이라 거리 유지에 적합하다.
        /// </summary>
        public void ApplyDirective(ChaseDirectiveIssued directive, float deltaTime, float targetDistance)
        {
            LastDirective = directive;
            AppliedDirectiveCount++;
            if (deltaTime <= 0f) return;

            var direction = Mathf.Approximately(directive.PursuerPosition.x, directive.PlayerPosition.x)
                ? 1f
                : Mathf.Sign(directive.PursuerPosition.x - directive.PlayerPosition.x);
            var target = directive.PlayerPosition + Vector2.right * direction * Mathf.Max(0f, targetDistance);
            transform.position = Vector2.MoveTowards(transform.position, target, directive.Speed * deltaTime);
        }
    }
}
