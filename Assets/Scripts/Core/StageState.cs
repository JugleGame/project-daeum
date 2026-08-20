namespace Daeume.Core
{
    /// <summary>
    /// 한 스테이지가 가질 수 있는 "큰 상태" 5가지다. (spec-001 핵심 루프)
    ///
    /// 유니티 처음이라면: enum은 "정해진 값 중 하나만 담는 타입"이다.
    /// 문자열 "explore" 같은 걸 쓰면 오타가 나도 컴파일러가 못 잡지만, enum은 잡아 준다.
    ///
    /// 진행 순서: Explore(탐색) → Memory(회상 재생) → Chase(추격) → Cleared(탈출 성공)
    /// Failed(실패)는 순서와 무관하게 "체력 0" 또는 "트라우마에게 붙잡힘" 두 경우에만 들어간다.
    ///
    /// 적합성 검토: spec-001이 요구한 5종과 정확히 일치한다. Collapse 같은 오염 압박 단계나
    /// Encounter 상태를 여기에 섞지 않은 것도 스펙 요구사항("압박 단계는 Stage 상태를 대체하지 않는다")과 맞다.
    /// </summary>
    public enum StageState
    {
        Explore,
        Memory,
        Chase,
        Cleared,
        Failed
    }
}
