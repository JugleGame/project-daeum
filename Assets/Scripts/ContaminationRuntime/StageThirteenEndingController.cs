using Daeume.Contamination;
using Daeume.Core;
using Daeume.Flow;
using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>Stage 13의 도주 루프와 수용 결말을 실제 오염·플레이어 시스템에 연결한다.</summary>
    public sealed class StageThirteenEndingController : MonoBehaviour
    {
        [SerializeField] private ContaminationDirector director;
        [SerializeField] private SceneFlowController flow;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private TraumaContactHandler traumaContact;
        [SerializeField] private Transform player;
        [SerializeField] private Transform trauma;
        [SerializeField, Min(0.1f)] private float acceptanceDistance = 3f;

        private readonly StageThirteenEndingState state = new();

        public StageThirteenEndingState State => state;
        public int HintStage => state.HintStage;
        public bool TraumaWaiting => state.TraumaWaiting;

        private void Awake()
        {
            ResolveReferences();
            var data = flow?.CurrentData;
            state.Restore(data?.StageThirteenLoopCount ?? 0, data?.WeaponLowered ?? false, data?.EndingCompleted ?? false);
        }

        private void Update()
        {
            if (player == null || trauma == null || state.EndingCompleted) return;
            ApplyPressureReversal(Vector2.Distance(player.position, trauma.position));
        }

        /// <summary>마지막 기억 직후 Stage 13 전용 규칙을 활성화한다.</summary>
        public void BeginAcceptance()
        {
            ResolveReferences();
            combat?.SetCombatEnabled(false);
            traumaContact?.SetContactFailureEnabled(false);
            director?.SetPressure(PressureStage.Collapse);
            director?.SetMovementSuppressed(state.TraumaWaiting);
        }

        /// <summary>루프 통로가 같은 벤치로 되돌렸을 때 호출한다. 체력·저장 페널티는 없다.</summary>
        public int RegisterRunawayLoop()
        {
            var hint = state.RegisterRunawayLoop();
            flow?.SaveStageThirteenProgress(state.LoopCount, state.WeaponLowered);
            director?.SetMovementSuppressed(state.TraumaWaiting);
            return hint;
        }

        /// <summary>수용 거리에서만 [E] 내려놓기 입력을 인정한다.</summary>
        public bool TryLowerWeapon()
        {
            if (player == null || trauma == null) return false;
            var lowered = state.TryLowerWeapon(Vector2.Distance(player.position, trauma.position), acceptanceDistance);
            if (lowered) flow?.SaveStageThirteenProgress(state.LoopCount, true);
            return lowered;
        }

        /// <summary>작별 대사와 버스 탑승이 모두 끝난 후 엔딩을 확정한다.</summary>
        public bool CompleteAfterFarewell(bool farewellPlayed, bool boardedBus)
        {
            if (!state.CompleteAfterFarewell(farewellPlayed, boardedBus)) return false;
            flow?.SaveEndingCompleted();
            return true;
        }

        private void ApplyPressureReversal(float distance)
        {
            var reversal = state.ResolvePressureReversal(distance, acceptanceDistance);
            var pressure = reversal switch
            {
                0 => PressureStage.Collapse,
                1 => PressureStage.Intrusion,
                2 => PressureStage.Echo,
                _ => PressureStage.Stable
            };
            director?.SetPressure(pressure);
        }

        private void ResolveReferences()
        {
            if (director == null) director = FindAnyObjectByType<ContaminationDirector>();
            if (flow == null) flow = FindAnyObjectByType<SceneFlowController>();
            if (combat == null) combat = FindAnyObjectByType<PlayerCombat>();
            if (traumaContact == null) traumaContact = FindAnyObjectByType<TraumaContactHandler>();
            if (player == null) player = GameObject.Find("Player")?.transform;
            if (trauma == null) trauma = GameObject.Find("Trauma")?.transform;
        }
    }
}
