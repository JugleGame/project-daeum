using Daeume.Core;
using UnityEngine;

namespace Daeume.Player
{
    /// <summary>
    /// 플레이어의 체력·무적 시간·사망 처리를 담당한다. (spec-003)
    ///
    /// IDamageable을 구현했기 때문에, 때리는 쪽(잔재)은 상대가 플레이어인지 몰라도
    /// ApplyDamage만 호출하면 된다. 적과 플레이어가 같은 규약을 공유하는 구조라 적합하다.
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 0.5f;        // 피격 직후 무적(연타 즉사 방지)
        [SerializeField, Min(0f)] private float respawnInvulnerabilitySeconds = 1.5f; // 부활 직후 무적

        // "언제까지 무적인가"를 시각(時刻)으로 저장한다.
        // 남은 시간을 매 프레임 깎는 방식보다 간단하고, Update가 필요 없어 비용도 낮다. 좋은 선택이다.
        private float invulnerableUntil;

        public DamageTargetKind TargetKind => DamageTargetKind.Player;
        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public int DamageSequence { get; private set; }

        private void Awake() => CurrentHealth = maxHealth;

        private void Start()
        {
            // HUD(C)는 이 이벤트를 받아야 체력 숫자를 그린다.
            // 시작 시 한 번 알려 주지 않으면 첫 피격 전까지 HUD가 비어 보이는 문제가 생긴다.
            GameManager.Instance?.Events.Publish(new PlayerHealthChanged(CurrentHealth, maxHealth));
        }

        public DamageResult ApplyDamage(DamageRequest request) => ApplyDamageAt(request, Time.time);

        /// <summary>
        /// 시각을 직접 넘겨받는 버전. 테스트가 실제 시간을 기다리지 않고
        /// 무적 시간 동작을 검증할 수 있게 하려고 분리했다. 적합한 테스트 친화 설계다.
        /// </summary>
        public DamageResult ApplyDamageAt(DamageRequest request, float now)
        {
            // spec-005: 회상 재생 중에는 전투 피해가 발생하지 않는다.
            // 공격하는 쪽마다 예외를 넣는 대신 "맞는 쪽" 한곳에서 막는다.
            // 그래야 잔재·지형 요소 등 피해 경로가 늘어나도 규칙이 저절로 지켜진다.
            if (GameManager.Instance != null && GameManager.Instance.StageState == StageState.Memory)
            {
                return new DamageResult(false, 0);
            }

            if (request.Amount <= 0 || CurrentHealth <= 0 || now < invulnerableUntil)
            {
                // 0 피해 / 이미 사망 / 무적 중 → 아무 일도 없었음을 명확히 돌려준다.
                // 호출한 쪽은 이 값을 보고 타격 이펙트를 낼지 결정한다.
                return new DamageResult(false, 0);
            }

            var previous = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - request.Amount);
            invulnerableUntil = now + invulnerabilitySeconds;
            var applied = previous - CurrentHealth;
            if (applied > 0) DamageSequence++;
            GameManager.Instance?.Events.Publish(new PlayerHealthChanged(CurrentHealth, maxHealth));

            if (CurrentHealth == 0)
            {
                // spec-001이 허용한 두 실패 원인 중 하나. 여기서 상태 전환을 직접 하지 않고
                // GameManager에 "원인"을 알려 규칙 판단을 맡긴다. 책임 분리가 올바르다.
                GameManager.Instance?.Fail(StageFailureCause.HealthDepleted);
            }

            return new DamageResult(applied > 0, applied);
        }

        /// <summary>체크포인트 복귀 시 체력을 되돌린다.</summary>
        /// <remarks>
        /// 최소 1로 보정한다. 0으로 되살리면 부활하자마자 다시 죽는 무한 루프에 빠진다.
        /// 부활 직후 무적을 주는 것도 같은 이유다 — 되살아난 자리에 적이 겹쳐 있어도 살아남게 한다.
        /// </remarks>
        public void Restore(int health)
        {
            CurrentHealth = Mathf.Clamp(health, 1, maxHealth);
            invulnerableUntil = Time.time + respawnInvulnerabilitySeconds;
            GameManager.Instance?.Events.Publish(new PlayerHealthChanged(CurrentHealth, maxHealth));
        }
    }
}
