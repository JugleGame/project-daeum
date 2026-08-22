using System;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 근접형 잔재. (spec-004)
    ///
    /// 잔재는 단순한 몬스터가 아니라 "주인공에게서 떨어져 나온 기억의 파편"이라는 설정이다.
    /// 공통 상태 머신과 압박·모방·Reactive·소멸 흔적 규칙은 RemnantActor가 갖고 있고,
    /// 이 클래스는 "접근해서 붙어 때린다"는 근접형 고유의 접근 방식만 구현한다.
    /// </summary>
    public sealed class MeleeRemnant : RemnantActor
    {
        [SerializeField] private MeleeRemnantData data;

        private MeleeRemnantData fallbackData;

        public override RemnantArchetype Archetype => RemnantArchetype.Melee;
        public MeleeRemnantData Data => data;
        protected override RemnantDataBase DataBase => DataOrDefault;

        /// <summary>소멸 시 알린다. Encounter가 이 신호로 다음 Wave를 시작한다.</summary>
        public event Action<MeleeRemnant> Died;

        /// <summary>
        /// 데이터 에셋이 비어 있어도 죽지 않도록 임시 기본값을 만들어 쓴다.
        /// 테스트에서 적을 코드로 만들 때 데이터 에셋을 매번 붙이지 않아도 되게 하는 장치다.
        /// </summary>
        private MeleeRemnantData DataOrDefault
        {
            get
            {
                if (data != null) return data;
                if (fallbackData == null)
                {
                    fallbackData = ScriptableObject.CreateInstance<MeleeRemnantData>();
                    fallbackData.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallbackData;
            }
        }

        private void OnDestroy()
        {
            if (fallbackData == null) return;
            if (Application.isPlaying) Destroy(fallbackData);
            else DestroyImmediate(fallbackData);
        }

        public void SetData(MeleeRemnantData value)
        {
            data = value;
            ResetRuntimeState();
        }

        protected override void TickApproach(float deltaTime)
        {
            if (!HasTarget)
            {
                EnterState(RemnantState.Idle);
                return;
            }

            FaceTarget();
            if (TargetInRange(DataBase.AttackRange))
            {
                if (CanInitiateAttack)
                {
                    EnterState(RemnantState.Attack);
                }
                else
                {
                    EnterYielding();
                }

                return;
            }

            // 좌우로만 이동한다. 근접형은 점프하지 않는 단순 추적이며, 이게 슬라이스 범위다.
            var position = transform.position;
            position.x = Mathf.MoveTowards(
                position.x,
                TargetX,
                DataBase.MoveSpeed * Profile.MoveSpeedMultiplier * deltaTime);
            transform.position = position;
        }

        protected override void OnDied() => Died?.Invoke(this);
    }
}
