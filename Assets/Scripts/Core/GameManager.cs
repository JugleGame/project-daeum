using UnityEngine;

namespace Daeume.Core
{
    /// <summary>
    /// 게임 전체에서 딱 하나만 존재하는 관리자. 스테이지 상태와 이벤트 중계소(EventBus)를 소유한다.
    ///
    /// [DefaultExecutionOrder(-100)]의 의미:
    /// 유니티는 여러 스크립트의 Awake를 임의 순서로 부른다. 음수 값을 주면 "남들보다 먼저" 실행된다.
    /// 다른 스크립트들이 Awake에서 GameManager.Instance를 찾아 구독하기 때문에,
    /// 이 값이 없으면 "아직 Instance가 없어서 구독 실패" 같은 타이밍 버그가 난다. 적합한 처리다.
    ///
    /// 배치 위치: Persistent 씬에 놓여 게임 내내 살아 있다(Role A 소유).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        // 싱글턴: 어디서든 GameManager.Instance로 접근한다.
        // 단점(전역 상태)이 있지만, 3인 분업에서 "관리자를 어떻게 찾을지"를 통일하는 비용이 더 싸다.
        public static GameManager Instance { get; private set; }

        // 상태 전이 규칙은 StageLoop에 위임한다. 이 클래스는 "알리는 일"만 한다. 책임 분리가 적절하다.
        private readonly StageLoop stageLoop = new();

        public EventBus Events { get; } = new();
        public StageState StageState => stageLoop.State;

        private void Awake()
        {
            // 씬을 다시 불러와 GameManager가 두 개가 되면 이벤트가 두 번 발행되는 등 사고가 난다.
            // 나중에 생긴 쪽이 스스로 사라져 항상 하나만 남게 한다.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// 상태 전이를 시도한다. 규칙에 어긋나면 아무 일도 일어나지 않는다(예외를 던지지 않는다).
        /// </summary>
        /// <remarks>
        /// 검토 메모: 실패해도 조용히 무시하므로 호출한 쪽이 "왜 안 바뀌었지?"를 모를 수 있다.
        /// 다만 호출부(MemoryAnchor, StageOneChaseController 등)는 모두 호출 직후 StageState를 다시 확인해
        /// 성공 여부를 판단하도록 작성돼 있어 실사용상 문제는 없다.
        /// </remarks>
        public void SetStageState(StageState nextState)
        {
            if (!stageLoop.TryTransition(nextState))
            {
                return;
            }

            Events.Publish(new StageStateChanged(nextState));
        }

        /// <summary>
        /// 선언된 원인으로 스테이지를 실패시킨다. 실패 이벤트와 상태 변경 이벤트를 함께 알린다.
        /// </summary>
        public bool Fail(StageFailureCause cause)
        {
            if (!stageLoop.TryFail(cause))
            {
                return false;
            }

            // 두 번 알리는 이유: 실패 "원인"이 필요한 쪽(연출·오디오)과
            // 단순히 "상태가 바뀌었다"만 필요한 쪽(HUD)이 각각 다른 메시지를 구독하기 때문이다.
            Events.Publish(new StageFailed(cause));
            Events.Publish(new StageStateChanged(StageState.Failed));
            return true;
        }

        /// <summary>
        /// 씬 로드/체크포인트 복귀처럼 규칙을 건너뛰고 상태를 강제로 맞춰야 할 때 쓴다.
        /// 흐름 소유자(SceneFlowController)만 호출하는 것이 원칙이다.
        /// </summary>
        public void ResetStage(StageState state = StageState.Explore)
        {
            stageLoop.Reset(state);
            Events.Publish(new StageStateChanged(state));
        }

        private void OnDestroy()
        {
            // 자기 자신이 현역일 때만 정리한다.
            // (중복 생성으로 파괴되는 쪽이 남아 있는 진짜 Instance를 지워 버리는 사고를 막는다.)
            if (Instance == this)
            {
                Events.Clear();
                Instance = null;
            }
        }
    }
}
