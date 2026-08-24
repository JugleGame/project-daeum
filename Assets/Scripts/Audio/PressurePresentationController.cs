using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    /// 환경음은 이 값을 그대로 쓰지만, 카메라 흔들림은 추격(Intrusion)이 아닌 한 잦아든다(#38).
    /// </summary>
    // 카메라 추적(StageCameraBounds, 기본 순서 0)이 위치를 정한 뒤에 흔들림을 얹어야 한다.
    // 순서를 정하지 않으면 프레임마다 누가 먼저 쓸지 알 수 없어 흔들림이 지워지기도 한다.
    [DefaultExecutionOrder(100)]
    public sealed class PressurePresentationController : MonoBehaviour
    {
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(0f, 0.25f)] private float maximumShake = 0.08f;

        // 압박이 Echo로 오른 뒤 흔들림이 잦아드는 데 걸리는 시간. 전환은 느끼되 탐색을 방해하지 않는 길이다.
        private const float ShakeSettleSeconds = 1.5f;

        private float pressureAmount;    // 0~1로 정규화한 압박 강도. 환경음 볼륨은 계속 이 값을 쓴다.
        private float shakeAmount;       // 지금 프레임의 흔들림 강도. 압박과 달리 지속 값으로 잦아든다.
        private float shakeSustain;      // 잦아든 뒤 남는 흔들림. 추격(Intrusion)만 0이 아니다.
        private float shakeSettleRate;   // 초당 감쇠량. 어느 단계에서 시작하든 ShakeSettleSeconds에 도달한다.
        private float shakeAssist = 1f;  // 접근성 옵션(0이면 흔들림 완전 차단)
        private Vector3 lastShakeOffset;  // 지난 프레임에 더한 흔들림. 다음 프레임에 먼저 빼서 누적을 막는다.
        private Vector3 lastAppliedPosition;  // 지난 프레임에 우리가 최종적으로 써 둔 카메라 위치.
        private bool hasAppliedShake;

        public float PressureAmount => pressureAmount;

        /// <summary>지금 카메라에 실제로 적용되는 흔들림 강도. 압박과 달리 잦아든다.</summary>
        public float ShakeAmount => shakeAmount;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void OnEnable()
        {
            Connect();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            Connect();

            // 수정: 저장된 접근성 설정을 스스로 읽어 온다.
            // 예전에는 ApplyAssist를 호출해 주는 코드가 어디에도 없어서,
            // "카메라 흔들림 강도 0"으로 설정해도 실제로는 계속 흔들렸다(spec-013 필수 항목 미준수).
            ApplyAssist(FindAnyObjectByType<SceneFlowController>()?.CurrentData?.AssistSettings);
        }

        private void OnDisable()
        {
            RemoveLastAppliedShakeOffset();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameManager.Instance?.Events.Unsubscribe<ContaminationPressureChanged>(OnPressure);
        }

        /// <summary>
        /// 추적이 실행되기 전에 지난 프레임의 연출 오프셋을 제거한다.
        /// StageCameraBounds와 같은 좌표를 기준으로 계산해야 한 축만 추적하는 스테이지에서도 누적되지 않는다.
        /// </summary>
        private void Update()
        {
            EnsureCameraBinding();
            RemoveLastAppliedShakeOffset();
        }

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
        ///
        /// 지난 프레임 오프셋은 Update에서 먼저 제거한다. 그러면 StageCameraBounds의 LateUpdate가
        /// 오프셋 없는 기준 위치를 계산하고, 이 LateUpdate가 같은 월드 좌표계에서 새 오프셋만 얹는다.
        /// 한 축만 추적하는 스테이지에서도 오프셋이 누적되지 않고, 세로 추적 스테이지에서도 흔들림이
        /// 추적 결과에 의해 지워지지 않는다.
        /// </remarks>
        private void LateUpdate()
        {
            EnsureCameraBinding();
            if (targetCamera == null) return;

            shakeAmount = Mathf.MoveTowards(shakeAmount, shakeSustain, shakeSettleRate * Time.deltaTime);

            var basePosition = targetCamera.transform.position;

            lastShakeOffset = Vector3.zero;
            if (shakeAmount > 0f && shakeAssist > 0f)
            {
                var offset = Random.insideUnitCircle * maximumShake * shakeAmount * shakeAssist;
                lastShakeOffset = new Vector3(offset.x, offset.y, 0f);
            }

            lastAppliedPosition = basePosition + lastShakeOffset;
            targetCamera.transform.position = lastAppliedPosition;
            hasAppliedShake = lastShakeOffset != Vector3.zero;
        }

        /// <summary>접근성 설정을 반영한다. 강도 0이면 흔들림이 완전히 사라진다.</summary>
        public void ApplyAssist(AssistSettings settings) => shakeAssist = Mathf.Clamp01(settings?.CameraShakeStrength ?? 1f);

        public void Bind(AudioSource source, Camera camera)
        {
            RemoveLastAppliedShakeOffset();
            ambientSource = source;
            targetCamera = camera;
            ResetAppliedShakeState();
        }

        private void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            EnsureCameraBinding();
            RemoveLastAppliedShakeOffset();
            ResetAppliedShakeState();
        }

        private void EnsureCameraBinding()
        {
            if (targetCamera != null) return;
            targetCamera = Camera.main;
            ResetAppliedShakeState();
        }

        private void RemoveLastAppliedShakeOffset()
        {
            if (targetCamera != null && hasAppliedShake &&
                targetCamera.transform.position == lastAppliedPosition)
            {
                targetCamera.transform.position -= lastShakeOffset;
            }

            ResetAppliedShakeState();
        }

        private void ResetAppliedShakeState()
        {
            lastShakeOffset = Vector3.zero;
            lastAppliedPosition = targetCamera == null ? Vector3.zero : targetCamera.transform.position;
            hasAppliedShake = false;
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

            // 흔들림은 압박을 그대로 따라가지 않고, 단계가 바뀐 순간에만 그 세기로 튀었다가 잦아든다.
            //
            // 왜 나눴나(#38): 근접 잔재 하나를 처치하면 ContaminationDirector.HandleEncounterCleared가
            // 추격도 아닌데 압박을 Echo로 올린다. Echo는 0.35, 즉 최대 흔들림의 35%다. 그런데 탐색 중에
            // 압박을 되돌리는 경로가 없어서, 전투 한 번 끝낸 뒤로는 남은 탐색 내내 화면이 흔들렸다.
            // 연출이 아니라 고장으로 읽힌다. "전투 후 오염이 한 단계 번진다"는 상태 자체는 spec-006 의도라
            // 압박 값은 그대로 두고(환경음도 그대로 오른 채 유지된다) 흔들림만 잦아들게 한다.
            //
            // 추격(Intrusion)은 예외다. 추격 중에는 흔들림이 계속 있어야 압박이 전달된다.
            shakeAmount = pressureAmount;
            shakeSustain = value.Pressure == PressureStage.Intrusion ? pressureAmount : 0f;
            shakeSettleRate = (shakeAmount - shakeSustain) / ShakeSettleSeconds;

            if (value.Pressure == PressureStage.Stable)
            {
                RemoveLastAppliedShakeOffset();
            }

            // 환경음 볼륨도 압박에 따라 올린다. Lerp는 두 값 사이를 비율로 섞는 함수다.
            if (ambientSource != null) ambientSource.volume = Mathf.Lerp(0.25f, 0.8f, pressureAmount);
        }
    }
}
