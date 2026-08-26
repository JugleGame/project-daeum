using System;
using Daeume.Contamination;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 추격의 "길이와 압박"을 소유하는 연출 감독이다. (spec-006, 기획 결정 2번)
    ///
    /// 왜 감독이 따로 있나:
    /// 추격자(트라우마)가 스스로 속도를 정하고 추격 종료까지 판단하면, 스테이지마다 페이싱이 제각각이 되고
    /// 플레이어는 "언제 끝나는지 모르는 압박"만 받는다. 그래서 Alien: Isolation처럼
    /// 감독이 목표 시간과 거리 한계를 정하고, 추격자는 지시만 실행하게 나눴다.
    ///
    /// 감독이 정하는 것: 압박 단계(오버레이 씬), 목표 추격 시간, 최소·최대 거리 유지, 막다른 길 후퇴
    /// 감독이 하지 않는 것: 순간이동(선언된 지점 외), 실패 판정(그건 spec-003의 붙잡기 연출이 소유)
    /// </summary>
    public sealed class ContaminationDirector : MonoBehaviour
    {
        [SerializeField] private ContaminationVariantData data;
        [SerializeField] private string chaseId = "chase-stage01-left-escape";
        [SerializeField] private Transform player;
        [SerializeField] private Transform pursuer;
        [SerializeField] private ChaseSpeedAssistAdapter speedAssist;

        /// <summary>추격자가 다가서는 목표 거리. 붙잡기가 실제로 성립할 만큼 겹쳐야 한다.</summary>
        /// <remarks>
        /// 예전 값은 0.9였고, "트라우마 반지름(0.65) + 플레이어 반폭(0.25)"이라는 계산에서 나왔다.
        /// 그런데 그 합은 <b>두 콜라이더가 딱 맞닿는 접선 거리</b>다. 겹침이 0이라 유니티는
        /// OnTriggerEnter2D를 쏘지 않는다. 그래서 가만히 서 있으면 추격자가 코앞까지 와서 멈춘 채
        /// 아무 일도 일어나지 않았다(#12에서 실제로 발생). 플레이어가 스스로 뛰어들어 파고들 때만
        /// 붙잡기가 성립했다.
        ///
        /// 접선보다 확실히 안쪽으로 들어오게 잡는다. 트라우마 콜라이더는 트리거라 물리적으로 밀지
        /// 않으므로, 겹쳐도 "벽에 낀 것처럼 못 움직이는" 문제는 생기지 않는다.
        /// 중심을 완전히 겹치게(0) 두지 않는 이유는 붙잡히는 순간의 그림이 읽혀야 하기 때문이다.
        /// </remarks>
        private const float ContactDistance = 0.55f;

        /// <summary>체크포인트 복귀 직후 추격자가 다가오지 않는 시간. 달아날 틈을 만든다.</summary>
        private const float RestoreGraceSeconds = 1.5f;

        private string loadedOverlay = string.Empty;
        private bool deadEndBlocked;
        private bool timedCompletionEnabled = true;
        private float restoreGraceRemaining;

        public ContaminationVariantData Data => data;
        public PressureStage Pressure { get; private set; } = PressureStage.Stable;
        public bool ChaseActive { get; private set; }
        public float ElapsedChaseSeconds { get; private set; }
        public float RemainingChaseSeconds => data == null ? 0f : Mathf.Max(0f, data.TargetChaseSeconds - ElapsedChaseSeconds);
        public int TeleportCount { get; private set; }
        public string VariantId => data == null ? string.Empty : data.VariantId;
        public float EffectiveChaseSpeed => speedAssist == null ? data?.ChaseSpeed ?? 0f : speedAssist.ResolveSpeed(data?.ChaseSpeed ?? 0f);
        public float EffectiveMinDistance => speedAssist == null ? data?.MinDistance ?? 0f : speedAssist.ResolveApproachDistance(data?.MinDistance ?? 0f, data?.MaxDistance ?? 0f);
        public bool DeadEndBlocked => deadEndBlocked;
        public bool MovementSuppressed { get; private set; }
        public event Action<string, bool> OverlayRequested;

        private void OnEnable()
        {
            GameManager.Instance?.Events.Subscribe<PlayerRestoreRequested>(HandlePlayerRestore);
        }

        private void OnDisable()
        {
            GameManager.Instance?.Events.Unsubscribe<PlayerRestoreRequested>(HandlePlayerRestore);
        }

        /// <summary>
        /// 스테이지가 새로 열리면 지금 압박 단계가 무엇인지 알린다. (#12)
        /// </summary>
        /// <remarks>
        /// 압박 연출(카메라 흔들림·환경음)은 씬을 넘나들며 살아남는 DontDestroyOnLoad 오브젝트다.
        /// 감독은 스테이지마다 새로 생기지만 연출은 그대로라, 아무도 알려 주지 않으면 연출 쪽이
        /// 이전 스테이지의 압박 값을 계속 들고 있는다. Stage 01 추격의 흔들림이 Stage 02 탐색까지
        /// 그대로 이어지던 증상이 이것이었다.
        /// </remarks>
        private void Start() => SetPressure(Pressure);

        private void Update()
        {
            ResolveActors();
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// 체크포인트 복귀로 플레이어가 순간이동하면 추격자도 안전 거리로 함께 되돌린다. (#7)
        ///
        /// 그렇지 않으면 붙잡힌 자리에 남은 추격자가 복귀 직후 다시 닿아 Fail→복귀→Fail이 무한 반복된다.
        /// KeepDistance는 프레임당 이동 속도 제한이 있어 이 상황을 스스로 벗어나지 못하므로,
        /// 체크포인트 복귀는 "선언된 지점"으로 취급해 순간이동으로 처리한다.
        /// </summary>
        public void HandlePlayerRestore(PlayerRestoreRequested request)
        {
            ResolveActors();
            if (!ChaseActive || pursuer == null || data == null) return;

            var offset = pursuer.position.x - request.Position.x;
            var direction = Mathf.Approximately(offset, 0f) ? 1f : Mathf.Sign(offset);
            pursuer.position = new Vector3(request.Position.x + direction * data.MaxDistance, request.Position.y, pursuer.position.z);

            // 거리를 벌려 놓기만 하면 부족했다(#12). 추격자는 플레이어보다 빠르므로 복귀하자마자
            // 곧바로 다시 붙어, 탈출 경로가 추격자 너머에 있으면 지나갈 틈이 생기지 않는다
            // (복귀 → 즉사 → 복귀 무한 반복). 복귀 직후 잠깐은 다가오지 않게 해서 달아날 틈을 준다.
            restoreGraceRemaining = RestoreGraceSeconds;
        }

        public void Configure(ContaminationVariantData variantData, Transform playerTransform, Transform pursuerTransform, string id = "chase-stage01-left-escape")
        {
            data = variantData;
            player = playerTransform;
            pursuer = pursuerTransform;
            chaseId = id ?? string.Empty;
        }

        /// <summary>
        /// 압박 단계를 바꾸고, 필요한 오버레이 교체를 요청한다.
        /// </summary>
        /// <remarks>
        /// Collapse를 포함한 4단계 모두 받는다(spec-006, Issue #9). Stage13 붕괴 연출 자체는 별도 이슈(#10) 몫이라
        /// 여기서는 "거절하지 않는다"만 보장한다 — Collapse 전용 오버레이를 선언하지 않은 Variant는 그냥 빈 오버레이로 남는다.
        /// 이전 오버레이를 먼저 내리고 새 오버레이를 올리는 순서를 지키므로 두 오염 공간이 겹치지 않는다.
        /// (OverlaySceneLoader가 요청을 받은 자리에서 오버레이 루트를 켜고 끈다.)
        /// </remarks>
        public bool SetPressure(PressureStage value)
        {
            if (data == null) return false;
            var nextOverlay = data.OverlayFor(value);
            if (!string.IsNullOrEmpty(loadedOverlay) && loadedOverlay != nextOverlay) RequestOverlay(loadedOverlay, false);
            Pressure = value;
            if (!string.IsNullOrEmpty(nextOverlay) && loadedOverlay != nextOverlay) RequestOverlay(nextOverlay, true);
            loadedOverlay = nextOverlay;
            GameManager.Instance?.Events.Publish(new ContaminationPressureChanged(data.VariantId, value, nextOverlay));
            return true;
        }

        /// <summary>
        /// 추격을 시작한다. 압박을 Intrusion까지 올리고 경과 시간을 0부터 센다.
        /// </summary>
        /// <remarks>
        /// 회상 완료 경로에서는 StageOneChaseController가 먼저 Echo를 켠 뒤 이 함수를 부른다
        /// (spec-005의 "Echo 시작" 요구). 재시도 복귀처럼 회상을 건너뛰는 경로에서는 곧바로 Intrusion이 맞다.
        /// </remarks>
        public bool BeginChase()
        {
            if (data == null || ChaseActive) return false;
            SetPressure(PressureStage.Intrusion);
            ElapsedChaseSeconds = 0f;
            ChaseActive = true;
            PublishChaseState();
            return true;
        }

        public void RetryChase()
        {
            ChaseActive = false;
            ElapsedChaseSeconds = 0f;
            BeginChase();
        }

        public bool CompleteChase()
        {
            if (!ChaseActive) return false;
            ChaseActive = false;
            PublishChaseState();
            return true;
        }

        public void SetSpeedAssist(ChaseSpeedAssistAdapter adapter) => speedAssist = adapter;

        public void SetDeadEndBlocked(bool blocked) => deadEndBlocked = blocked;

        public void SetTimedCompletion(bool enabled) => timedCompletionEnabled = enabled;

        /// <summary>Stage13 네 번째 loop부터 트라우마를 제자리에서 기다리게 한다.</summary>
        public void SetMovementSuppressed(bool suppressed) => MovementSuppressed = suppressed;

        /// <summary>
        /// 매 프레임 추격을 진행한다. 목표 시간에 도달하면 추격을 끝낸다.
        /// Update가 아니라 별도 함수로 분리해 두어 테스트가 시간을 직접 넣어 검증할 수 있다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!ChaseActive || data == null) return;
            var step = Mathf.Max(0f, deltaTime);
            ElapsedChaseSeconds = Mathf.Min(data.TargetChaseSeconds, ElapsedChaseSeconds + step);
            restoreGraceRemaining = Mathf.Max(0f, restoreGraceRemaining - step);
            if (!MovementSuppressed)
            {
                KeepDistance(step);
                PublishDirective();
            }
            if (!timedCompletionEnabled || ElapsedChaseSeconds < data.TargetChaseSeconds) return;
            ChaseActive = false;
            PublishChaseState();
        }

        public void HandleEncounterCleared()
        {
            if (!ChaseActive) SetPressure(PressureStage.Echo);
        }

        public void SetDebugDistance(float distance)
        {
            ResolveActors();
            if (player == null || pursuer == null) return;
            pursuer.position = new Vector3(player.position.x - Mathf.Max(0f, distance), player.position.y, pursuer.position.z);
        }

        /// <summary>
        /// 추격자를 플레이어 쪽으로 붙인다. (spec-006)
        ///
        /// 수정: 예전에는 최소 거리를 두고 "그 이상은 다가오지 않는" 공정성 장치가 있었는데,
        /// 그러면 추격자가 절대 플레이어를 붙잡지 못해 죽음이라는 결과 자체가 나올 수 없었다.
        /// 그래서 최소 거리 유지를 없애고, 항상 접촉(ContactDistance)을 목표로 다가오게 한다.
        /// 막다른 길에서만 예외로 최대 거리를 유지한다 → 길이 막혔다고 즉시 실패시키지 않는다.
        /// </summary>
        private void KeepDistance(float deltaTime)
        {
            if (player == null || pursuer == null || deltaTime <= 0f) return;

            // 복귀 직후 유예 동안은 거리를 좁히지 않는다. 이게 없으면 추격자가 플레이어보다 빨라
            // 복귀 → 즉시 재접촉 → 복귀가 무한 반복된다.
            if (restoreGraceRemaining > 0f) return;

            var offset = pursuer.position.x - player.position.x;
            var direction = Mathf.Approximately(offset, 0f) ? -1f : Mathf.Sign(offset);
            var distance = Mathf.Abs(offset);
            var targetDistance = deadEndBlocked ? data.MaxDistance : ContactDistance;

            var actor = pursuer.GetComponent<TraumaChaseActor>();
            if (actor != null)
            {
                actor.ApplyDirective(CreateDirective(), deltaTime, targetDistance);
                return;
            }

            if (Mathf.Approximately(distance, targetDistance)) return;

            var targetX = player.position.x + direction * targetDistance;
            var position = pursuer.position;
            position.x = Mathf.MoveTowards(position.x, targetX, EffectiveChaseSpeed * deltaTime);
            pursuer.position = position;
        }

        private void PublishDirective()
        {
            if (player == null || pursuer == null) return;
            var distance = Mathf.Abs(player.position.x - pursuer.position.x);
            GameManager.Instance?.Events.Publish(CreateDirective(distance));
        }

        private ChaseDirectiveIssued CreateDirective(float? measuredDistance = null)
        {
            var distance = measuredDistance ?? Mathf.Abs(player.position.x - pursuer.position.x);
            return new ChaseDirectiveIssued(
                chaseId, player.position, pursuer.position, distance,
                EffectiveMinDistance, data.MaxDistance, EffectiveChaseSpeed, RemainingChaseSeconds);
        }

        private void PublishChaseState()
        {
            GameManager.Instance?.Events.Publish(new ChaseStateChanged(chaseId, ChaseActive, ElapsedChaseSeconds, data.TargetChaseSeconds));
        }

        private void RequestOverlay(string sceneName, bool load)
        {
            OverlayRequested?.Invoke(sceneName, load);
            GameManager.Instance?.Events.Publish(new OverlaySceneLoadRequested(sceneName, load));
        }

        /// <summary>
        /// 플레이어와 추격자를 실행 중에 이름으로 찾는다.
        /// 플레이어는 Persistent 씬에 있어 스테이지 씬에서 미리 연결해 둘 수 없기 때문이다.
        /// 이름 의존이라 오브젝트 이름을 바꾸면 끊긴다 — 바꿀 때 함께 확인해야 하는 지점이다.
        /// </summary>
        private void ResolveActors()
        {
            if (player == null)
            {
                var found = GameObject.Find("Player");
                if (found != null) player = found.transform;
            }

            if (pursuer == null)
            {
                var found = GameObject.Find("Trauma");
                if (found != null) pursuer = found.transform;
            }
        }
    }
}
