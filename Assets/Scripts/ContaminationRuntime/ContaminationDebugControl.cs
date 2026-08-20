using Daeume.Contamination;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 개발용 디버그 조작기. 인스펙터 우클릭 메뉴에서 압박 단계와 추격 거리를 강제로 바꾼다.
    ///
    /// [ContextMenu]: 컴포넌트 우클릭 메뉴에 항목을 추가하는 속성이다.
    /// 게임을 처음부터 진행하지 않고도 특정 상황을 재현할 수 있어 QA 시간을 크게 줄여 준다.
    ///
    /// 주의: 플레이 도중 자동으로 실행되는 경로가 없으므로 실제 게임 흐름에는 영향이 없다.
    /// 다만 출시 빌드에는 남길 이유가 없으므로, 마감 단계에서 제거 대상으로 표시해 둘 것.
    /// </summary>
    public sealed class ContaminationDebugControl : MonoBehaviour
    {
        [SerializeField] private ContaminationDirector director;
        [SerializeField] private PressureStage debugPressure = PressureStage.Stable;
        [SerializeField, Range(0f, 15f)] private float debugDistance = 5f;

        public void Configure(ContaminationDirector value) => director = value;

        [ContextMenu("Debug/Apply Pressure")]
        public void ApplyPressure() => director?.SetPressure(debugPressure);

        [ContextMenu("Debug/Apply Distance")]
        public void ApplyDistance() => director?.SetDebugDistance(debugDistance);

        public void ApplyPressure(PressureStage value)
        {
            debugPressure = value;
            ApplyPressure();
        }

        public void ApplyDistance(float value)
        {
            debugDistance = value;
            ApplyDistance();
        }
    }
}
