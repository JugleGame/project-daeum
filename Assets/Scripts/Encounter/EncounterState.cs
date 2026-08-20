namespace Daeume.Encounter
{
    /// <summary>
    /// 전투 구간(Encounter)의 상태 3종. (spec-001, spec-012)
    ///
    /// 이 값은 StageState를 대체하지 않는다.
    /// 전투 중에도 스테이지 상태는 여전히 Explore다 — spec-001이 명시적으로 요구하는 규칙이고,
    /// Test_StageLoop_EncounterDoesNotReplaceExplore가 이를 검사한다.
    /// 두 축을 섞으면 "전투 중에 회상을 시작할 수 있는가" 같은 판단이 뒤엉킨다.
    /// </summary>
    public enum EncounterState
    {
        Inactive,  // 아직 진입 전
        Active,    // 진행 중(출구 잠김, Wave 진행)
        Cleared    // 완료(영구, 재진입해도 다시 스폰하지 않는다)
    }
}
