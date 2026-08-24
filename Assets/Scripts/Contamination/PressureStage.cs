namespace Daeume.Contamination
{
    /// <summary>
    /// 기억 역류(Contamination)의 압박 단계 4종. (spec-006)
    ///
    /// 중요한 구분: 이것은 StageState(탐색/회상/추격…)와 별개의 축이다.
    /// 스펙이 "압박 단계는 Stage 상태를 대체하지 않는다"고 명시하고 있어 두 enum을 절대 합치면 안 된다.
    /// 예를 들어 "추격 중이면서 Intrusion" 같은 조합이 자연스럽게 성립해야 한다.
    ///
    /// 단계가 오르면 공간(오버레이 씬), 사운드, 카메라 압박, 잔재의 행동 수치가 함께 변한다.
    ///
    /// Collapse는 8일 슬라이스에서는 제외였으나 Issue #9에서 4번째 단계로 구현됐다.
    /// ContaminationDirector.SetPressure는 이제 Collapse 요청도 받는다. Stage13 붕괴 연출은 별도 이슈(#10)가 소유한다.
    /// </summary>
    public enum PressureStage
    {
        Stable,     // 평상시. 오버레이 없음
        Echo,       // 기억이 새어 나오기 시작
        Intrusion,  // 오염이 공간을 침범
        Collapse    // 붕괴(Issue #9). Stage13 전용 연출은 #10이 소유한다.
    }
}
