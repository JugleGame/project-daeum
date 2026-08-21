using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;

namespace Daeume.Audio
{
    /// <summary>
    /// 오염 압박 단계를 소리와 카메라 흔들림으로 표현한다. (spec-014)
    ///
    /// 중요한 원칙: 이 스크립트는 "트라우마와의 거리"를 직접 계산하지 않는다.
    /// 거리와 압박 값은 B의 ContaminationDirector가 소유하고, 여기서는 이벤트로 받은 값을 쓰기만 한다.
    /// (spec-014의 Test_Presentation_TraumaDistanceComesFromDirector가 이 규칙을 검사한다.)
    ///
    /// 압박 → 강도 매핑: Stable 0 / Echo 0.35 / Intrusion 1.0
    /// </summary>
    public sealed class PressurePresentationController : MonoBehaviour
    {
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(0f, 0.25f)] private float maximumShake = 0.08f;

        private float pressureAmount;    // 0~1로 정규화한 압박 강도
        private float shakeAssist = 1f;  // 접근성 옵션(0이면 흔들림 완전 차단)

        public float PressureAmount => pressureAmount;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void OnEnable() => Connect();

        private void Start()
        {
            Connect();

            // 수정: 저장된 접근성 설정을 스스로 읽어 온다.
            // 예전에는 ApplyAssist를 호출해 주는 코드가 어디에도 없어서,
            // "카메라 흔들림 강도 0"으로 설정해도 실제로는 계속 흔들렸다(spec-013 필수 항목 미준수).
            ApplyAssist(FindAnyObjectByType<SceneFlowController>()?.CurrentData?.AssistSettings);
        }

        private void OnDisable() => GameManager.Instance?.Events.Unsubscribe<ContaminationPressureChanged>(OnPressure);

        /// <summary>
        /// LateUpdate에서 흔든다. 카메라 추적(StageCameraBounds)이 LateUpdate에서 위치를 정하므로,
        /// 그 뒤에 오프셋을 "더해야" 흔들림이 추적 결과 위에 얹힌다.
        /// </summary>
        /// <remarks>
        /// 예전에는 Awake 시점에 캐시해 둔 위치를 기준으로 매 프레임 덮어썼다. 압박이 켜져 있는 동안
        /// 내내 그 캐시된 좌표로 되돌아가 버려서, 추격 중 카메라가 압박이 시작된 순간의 위치(대개
        /// 회상을 막 끝낸 탈출구 근처)에 고정된 것처럼 보이고 플레이어를 따라가지 않는 버그가 있었다.
        /// 고정 기준점 없이 "이번 프레임 위치"에 오프셋만 더하면, StageCameraBounds가 매 프레임 다시
        /// 계산해 주는 추적 위치를 지우지 않는다.
        /// </remarks>
        private void LateUpdate()
        {
            if (targetCamera == null || pressureAmount <= 0f || shakeAssist <= 0f) return;

            var offset = Random.insideUnitCircle * maximumShake * pressureAmount * shakeAssist;
            targetCamera.transform.localPosition += new Vector3(offset.x, offset.y, 0f);
        }

        /// <summary>접근성 설정을 반영한다. 강도 0이면 흔들림이 완전히 사라진다.</summary>
        public void ApplyAssist(AssistSettings settings) => shakeAssist = Mathf.Clamp01(settings?.CameraShakeStrength ?? 1f);

        public void Bind(AudioSource source, Camera camera)
        {
            ambientSource = source;
            targetCamera = camera;
        }

        private void Connect()
        {
            if (GameManager.Instance == null) return;

            // 중복 구독 방지: 먼저 해제한 뒤 구독한다.
            // OnEnable과 Start 두 곳에서 호출되므로 이 처리가 없으면 이벤트가 두 번 처리된다.
            GameManager.Instance.Events.Unsubscribe<ContaminationPressureChanged>(OnPressure);
            GameManager.Instance.Events.Subscribe<ContaminationPressureChanged>(OnPressure);
        }

        private void OnPressure(ContaminationPressureChanged value)
        {
            pressureAmount = value.Pressure switch
            {
                PressureStage.Stable => 0f,
                PressureStage.Echo => 0.35f,
                PressureStage.Intrusion => 1f,
                _ => 0f   // Collapse는 슬라이스 범위 밖이라 0으로 둔다.
            };

            // 환경음 볼륨도 압박에 따라 올린다. Lerp는 두 값 사이를 비율로 섞는 함수다.
            if (ambientSource != null) ambientSource.volume = Mathf.Lerp(0.25f, 0.8f, pressureAmount);
        }
    }
}
