using Daeume.Core;

namespace Daeume.Audio
{
    /// <summary>
    /// StageState(+Encounter 진행 여부)를 오디오 큐 5종 중 하나로 매핑한다. (spec-014)
    ///
    /// 이 클래스는 소리를 재생하지 않는다. "무슨 상태면 무슨 큐인가"만 결정한다 — 실제 재생은
    /// AudioSource를 들고 있는 프레젠터의 몫이다. 판단과 재생을 나눠 두면 재생 장치(AudioSource) 없이도
    /// EditMode 테스트로 매핑 규칙을 검증할 수 있다.
    /// </summary>
    public static class AudioCueResolver
    {
        /// <summary>
        /// Failed 상태에는 대응하는 큐가 없다(spec-014의 5종에 포함되지 않음).
        /// null을 돌려주면 호출자는 재생 중이던 큐를 그대로 유지해야 한다 — 실패 연출이
        /// 짧게 화면을 덮었다가 같은 자리로 복귀하는 흐름과 맞다.
        /// </summary>
        public static AudioCueId? Resolve(StageState state, bool encounterActive)
        {
            return state switch
            {
                StageState.Explore => encounterActive ? AudioCueId.EncounterCombat : AudioCueId.ExploreAmbient,
                StageState.Memory => AudioCueId.Memory,
                StageState.Chase => AudioCueId.Chase,
                StageState.Cleared => AudioCueId.Cleared,
                _ => null
            };
        }
    }
}
