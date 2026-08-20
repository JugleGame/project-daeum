using Daeume.Core;
using Daeume.Flow;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 접근성 옵션 "추격 속도 저하"를 추격 수치에 반영한다. (spec-006, spec-013)
    ///
    /// 이 옵션은 잘라낼 수 없는 기준선 항목이다(design-decisions 5번).
    /// 다만 규칙이 엄격하다: 속도와 접근 압박만 낮추고,
    /// 경로·기믹·신호 타이밍·탈출점은 절대 바꾸지 않는다.
    /// 그래서 이 어댑터는 "속도"와 "목표 접근 거리" 두 값만 변환하는 아주 좁은 역할만 맡는다.
    /// 이 좁음이 곧 스펙 준수를 구조적으로 보장한다 — 여기에 다른 기능을 추가하면 안 된다.
    /// </summary>
    public sealed class ChaseSpeedAssistAdapter : MonoBehaviour
    {
        [SerializeField] private bool enabledForDebug;
        [SerializeField, Range(0.1f, 1f)] private float speedMultiplier = 0.75f;   // 추격 속도 배수
        [SerializeField, Range(0f, 1f)] private float approachPressure = 0.5f;     // 0에 가까울수록 멀리서 따라온다

        public bool Enabled => enabledForDebug;
        public float SpeedMultiplier => speedMultiplier;
        public float ApproachPressure => approachPressure;

        private void Start()
        {
            // 저장된 사용자 설정을 읽어 온다. 설정은 저장 슬롯과 별개로 보존되므로
            // 새 게임을 시작해도 이전에 켜 둔 어시스트가 유지된다(spec-011).
            Apply(FindAnyObjectByType<SceneFlowController>()?.CurrentData?.AssistSettings);
        }

        public void Apply(AssistSettings settings)
        {
            // 설정이 없으면 현재 값을 유지한다. null을 기본값으로 덮어쓰면
            // 아직 설정이 로드되지 않은 순간에 사용자의 선택이 지워질 수 있다.
            if (settings != null) enabledForDebug = settings.ChaseSpeedAssist;
        }

        public void Configure(bool enabled, float speedScale = 0.75f, float pressure = 0.5f)
        {
            enabledForDebug = enabled;
            speedMultiplier = Mathf.Clamp(speedScale, 0.1f, 1f);
            approachPressure = Mathf.Clamp01(pressure);
        }

        /// <summary>어시스트가 켜져 있으면 속도를 배수만큼 낮춘다.</summary>
        public float ResolveSpeed(float baseSpeed)
        {
            return Mathf.Max(0f, baseSpeed) * (enabledForDebug ? speedMultiplier : 1f);
        }

        /// <summary>
        /// 어시스트가 켜져 있으면 추격자가 유지할 목표 거리를 "최대 거리 쪽으로" 당긴다.
        /// Lerp(최대, 최소, 압박): 압박이 0이면 최대 거리(가장 여유), 1이면 원래 최소 거리와 같아진다.
        /// 거리 값만 바뀔 뿐 경로와 신호는 그대로이므로 스펙 제약을 지킨다.
        /// </summary>
        public float ResolveApproachDistance(float minimumDistance, float maximumDistance)
        {
            if (!enabledForDebug) return minimumDistance;
            return Mathf.Lerp(maximumDistance, minimumDistance, approachPressure);
        }
    }
}
