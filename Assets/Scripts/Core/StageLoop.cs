namespace Daeume.Core
{
    /// <summary>
    /// 스테이지가 실패로 끝나는 원인. spec-001은 이 두 가지 외의 실패 경로를 금지한다.
    /// (예: 낙사나 함정으로 즉사시키지 않는다 — "보이지 않는 즉사" 금지)
    /// </summary>
    public enum StageFailureCause
    {
        HealthDepleted,        // 체력이 0이 됨
        TraumaGrabCompleted    // 트라우마에게 붙잡히는 연출이 끝남 (spec-003)
    }

    /// <summary>스테이지 상태가 바뀌었음을 알리는 메시지. EventBus로 전파된다.</summary>
    public readonly struct StageStateChanged
    {
        public StageStateChanged(StageState state) => State = state;
        public StageState State { get; }
    }

    /// <summary>스테이지가 실패했음을 알리는 메시지. 원인까지 함께 전달한다.</summary>
    public readonly struct StageFailed
    {
        public StageFailed(StageFailureCause cause) => Cause = cause;
        public StageFailureCause Cause { get; }
    }

    /// <summary>
    /// 스테이지 상태 전이 규칙만 담당하는 순수 C# 클래스다(MonoBehaviour가 아니다).
    ///
    /// 왜 유니티 컴포넌트가 아닌가:
    /// 씬이나 GameObject 없이도 테스트할 수 있게 하려고 일부러 분리했다.
    /// 덕분에 EditMode 테스트(StageLoopTests)가 게임을 실행하지 않고 순서 규칙만 빠르게 검증한다. 적합하다.
    ///
    /// 핵심 원칙: 상태를 바깥에서 아무렇게나 대입하지 못하게 하고, 허용된 전이만 통과시킨다.
    /// </summary>
    public sealed class StageLoop
    {
        // private set → 이 클래스 밖에서는 읽기만 가능하다. 규칙을 우회한 상태 변경을 막는다.
        public StageState State { get; private set; } = StageState.Explore;

        /// <summary>
        /// 정상 진행 방향으로만 상태를 옮긴다. 성공하면 true.
        /// </summary>
        public bool TryTransition(StageState next)
        {
            // Failed는 여기로 들어올 수 없다. 반드시 TryFail을 거치게 해서
            // "실패 원인 없는 실패"가 생기지 않도록 막는다. spec-001 요구사항과 일치한다.
            if (next == StageState.Failed || !IsAllowed(State, next))
            {
                return false;
            }

            State = next;
            return true;
        }

        /// <summary>
        /// 선언된 두 원인으로만 실패 상태에 들어간다.
        /// </summary>
        public bool TryFail(StageFailureCause cause)
        {
            if (cause != StageFailureCause.HealthDepleted &&
                cause != StageFailureCause.TraumaGrabCompleted)
            {
                // enum이라 사실상 도달하지 않지만, 나중에 원인이 추가될 때
                // "허용 목록에 넣는 것을 잊는" 실수를 컴파일이 아니라 이 가드가 잡아 준다.
                return false;
            }

            State = StageState.Failed;
            return true;
        }

        /// <summary>탈출 지점에서 클리어가 가능한 상태인가? 추격 중일 때만 참이다.</summary>
        /// <remarks>
        /// spec-001: "Stage 1~12는 지정 출구에서만 Cleared가 된다."
        /// 즉 회상 전에 출구로 걸어가도 스테이지가 끝나면 안 된다. 이 속성이 그 규칙을 담고 있다.
        /// </remarks>
        public bool CanClearAtExit => State == StageState.Chase;

        public bool TryClearAtExit()
        {
            return CanClearAtExit && TryTransition(StageState.Cleared);
        }

        /// <summary>
        /// 상태를 초기값으로 되돌린다. 씬을 새로 불러오거나 체크포인트에서 복귀할 때 쓴다.
        /// 실패(Failed)에서 다시 추격(Chase)으로 돌아가는 유일한 통로이기도 하다
        /// (IsAllowed에는 Failed에서 나가는 경로가 없기 때문이다. 의도된 설계다).
        /// </summary>
        public void Reset(StageState state = StageState.Explore) => State = state;

        /// <summary>
        /// 허용된 전이표. 한 줄로 읽으면 "탐색 다음은 회상, 회상 다음은 추격, 추격 다음은 클리어"다.
        /// switch 식(=>)은 "현재 상태가 이것이면 이 조건을 돌려준다"는 뜻이다.
        /// </summary>
        private static bool IsAllowed(StageState current, StageState next)
        {
            return current switch
            {
                StageState.Explore => next == StageState.Memory,
                StageState.Memory => next == StageState.Chase,
                StageState.Chase => next == StageState.Cleared,
                _ => false // Cleared/Failed에서 스스로 빠져나가는 길은 없다. Reset으로만 나간다.
            };
        }
    }
}
