using Daeume.Contamination;
using Daeume.Core;
using Daeume.Flow;
using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// Stage 1의 "회상 완료 → 오염 전환 → 추격 시작 → 탈출" 순서를 실제로 실행하는 지휘자다.
    /// (spec-005의 후속 흐름 요청 + spec-006의 추격 규칙을 잇는 자리)
    ///
    /// 각 시스템이 서로를 직접 부르지 않고 이 컨트롤러 한 곳에 순서를 모아 둔 것이 핵심이다.
    /// 순서가 흩어져 있으면 "체크포인트가 저장되기 전에 추격이 시작되는" 류의 버그가 반드시 생긴다.
    /// </summary>
    public sealed class StageOneChaseController : MonoBehaviour
    {
        public const string CheckpointId = "Stage01_Chase";

        [SerializeField] private string checkpointId = CheckpointId;
        [SerializeField] private ContaminationDirector director;
        [SerializeField] private SceneFlowController flow;
        [SerializeField] private Transform player;
        [SerializeField] private GameObject trauma;

        public ContaminationDirector Director => director;
        public bool ChaseStarted { get; private set; }

        public void Configure(ContaminationDirector chaseDirector, SceneFlowController sceneFlow, Transform playerTransform, GameObject traumaActor)
        {
            director = chaseDirector;
            flow = sceneFlow;
            player = playerTransform;
            trauma = traumaActor;
        }

        /// <summary>
        /// 회상이 끝났을 때 호출된다. 스펙이 정한 순서를 그대로 실행한다.
        ///
        /// 순서: 상태 Memory 확인 → Chase로 전환 → 기억 역류 Echo 시작 → 추격 시작(압박 상승)
        ///       → 트라우마 등장 → ChaseCheckpoint 저장
        /// </summary>
        public bool BeginChaseFromMemory()
        {
            ResolveReferences();
            var manager = GameManager.Instance;
            if (manager == null || director == null) return false;

            // 디버그 트리거로 탐색 중에 바로 부를 수도 있어, 먼저 Memory 상태를 만들어 준다.
            if (manager.StageState == StageState.Explore) manager.SetStageState(StageState.Memory);

            // 상태 전이가 거절됐다면(이미 추격 중이거나 실패 상태) 아무 것도 하지 않는다.
            // 이 가드 덕분에 디버그 경로와 실제 경로가 함께 있어도 추격이 두 번 시작되지 않는다.
            if (manager.StageState != StageState.Memory) return false;

            manager.SetStageState(StageState.Chase);
            if (manager.StageState != StageState.Chase) return false;

            // 수정(spec-005 준수): 회상 완료의 첫 후속 결과는 "기억 역류 Echo 시작"이다.
            // 예전에는 이 단계를 건너뛰고 곧바로 Intrusion으로 뛰어, 오염이 서서히 번지는 연출 구간이 사라졌다.
            // Echo를 먼저 켜야 C의 압박 연출(사운드·카메라)도 단계적으로 올라간다.
            director.SetPressure(PressureStage.Echo);

            // 추격 시작. 압박을 Intrusion으로 올리는 판단은 director가 소유한다(spec-006).
            if (!director.BeginChase()) return false;

            trauma?.SetActive(true);

            // 체크포인트는 추격이 실제로 시작된 뒤 저장한다.
            // 그래야 추격 중 사망 시 "회상을 다시 보지 않고" 같은 지점에서 재개된다(spec-011).
            var health = player == null ? 3 : player.GetComponent<PlayerHealth>()?.CurrentHealth ?? 3;
            flow?.SaveChaseCheckpoint(string.IsNullOrWhiteSpace(checkpointId) ? CheckpointId : checkpointId,
                player == null ? Vector2.zero : (Vector2)player.position, health, director.VariantId);

            ChaseStarted = true;
            return true;
        }

        /// <summary>탈출 지점에 도달했을 때 호출된다. 추격 중일 때만 성립한다(spec-001).</summary>
        public bool CompleteAtEscape()
        {
            ResolveReferences();
            var manager = GameManager.Instance;
            if (manager == null || manager.StageState != StageState.Chase) return false;

            // 정상 경로: 씬 흐름 소유자(A)가 클리어 연출·저장·씬 전환 순서를 처리한다.
            if (flow != null) return flow.CompleteStageOne();

            // 흐름 컨트롤러가 없는 단독 테스트 씬에서는 상태 계약만 확인한다.
            manager.SetStageState(StageState.Cleared);
            return manager.StageState == StageState.Cleared;
        }

        /// <summary>막다른 길 구간에 들어갔는지 여부를 director에게 전달한다.</summary>
        public void SetDeadEndBlocked(bool blocked) => director?.SetDeadEndBlocked(blocked);

        /// <summary>
        /// 인스펙터 연결이 비어 있으면 씬에서 찾아 채운다.
        /// </summary>
        /// <remarks>
        /// 플레이어와 카메라는 Persistent 씬에 있고 추격 구성은 Stage 씬에 있어서,
        /// 씬 파일 안에서는 서로를 미리 연결해 둘 수 없다(다른 씬의 오브젝트는 참조가 저장되지 않는다).
        /// 그래서 실행 시점에 이름으로 찾아 잇는다. 이름 의존이라 취약한 부분이므로,
        /// 오브젝트 이름("Player", "Trauma")을 바꿀 때는 이 함수도 함께 봐야 한다.
        /// </remarks>
        private void ResolveReferences()
        {
            if (director == null) director = FindAnyObjectByType<ContaminationDirector>();
            if (flow == null) flow = FindAnyObjectByType<SceneFlowController>();
            if (player == null)
            {
                var found = GameObject.Find("Player");
                if (found != null) player = found.transform;
            }
            if (trauma == null)
            {
                // 트라우마는 추격 전까지 꺼져 있다. GameObject.Find는 꺼진 오브젝트를 찾지 못하므로
                // 씬에 배치된 상태(활성)에서만 이 경로가 성공한다.
                // 확실하게 하려면 인스펙터 또는 Configure로 직접 지정하는 편이 안전하다.
                var found = GameObject.Find("Trauma");
                if (found != null) trauma = found;
            }
        }
    }
}
