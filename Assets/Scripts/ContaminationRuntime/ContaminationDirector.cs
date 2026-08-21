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

        private string loadedOverlay = string.Empty;
        private bool deadEndBlocked;

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
        public event Action<string, bool> OverlayRequested;

        private void OnEnable()
        {
            GameManager.Instance?.Events.Subscribe<PlayerRestoreRequested>(OnPlayerRestoreRequested);
        }

        private void OnDisable()
        {
            GameManager.Instance?.Events.Unsubscribe<PlayerRestoreRequested>(OnPlayerRestoreRequested);
        }

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
        private void OnPlayerRestoreRequested(PlayerRestoreRequested request)
        {
            ResolveActors();
            if (!ChaseActive || pursuer == null || data == null) return;

            var offset = pursuer.position.x - request.Position.x;
            var direction = Mathf.Approximately(offset, 0f) ? 1f : Mathf.Sign(offset);
            pursuer.position = new Vector3(request.Position.x + direction * data.MaxDistance, request.Position.y, pursuer.position.z);
        }

        public void Configure(ContaminationVariantData variantData, Transform playerTransform, Transform pursuerTransform, string id = "chase-stage01-left-escape")
        {
            data = variantData;
            player = playerTransform;
            pursuer = pursuerTransform;
            chaseId = id ?? string.Empty;
        }

        /// <summary>
        /// 압박 단계를 바꾸고, 필요한 오버레이 씬 교체를 요청한다.
        /// </summary>
        /// <remarks>
        /// Collapse는 8일 슬라이스 범위 밖이라 거절한다(spec-006 Build scope). 의도된 동작이다.
        /// 이전 오버레이를 먼저 내리고 새 오버레이를 올리는 순서를 지키므로 두 오염 공간이 겹치지 않는다.
        /// (요청은 큐에 쌓여 OverlaySceneLoader가 한 번에 하나씩 처리한다.)
        /// </remarks>
        public bool SetPressure(PressureStage value)
        {
            if (data == null || value == PressureStage.Collapse) return false;
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

        public void SetSpeedAssist(ChaseSpeedAssistAdapter adapter) => speedAssist = adapter;

        public void SetDeadEndBlocked(bool blocked) => deadEndBlocked = blocked;

        /// <summary>
        /// 매 프레임 추격을 진행한다. 목표 시간에 도달하면 추격을 끝낸다.
        /// Update가 아니라 별도 함수로 분리해 두어 테스트가 시간을 직접 넣어 검증할 수 있다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!ChaseActive || data == null) return;
            var step = Mathf.Max(0f, deltaTime);
            ElapsedChaseSeconds = Mathf.Min(data.TargetChaseSeconds, ElapsedChaseSeconds + step);
            KeepDistance(step);
            PublishDirective();
            if (ElapsedChaseSeconds < data.TargetChaseSeconds) return;
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
        /// 추격자와 플레이어 사이 거리를 규칙대로 유지한다. (spec-006의 핵심 공정성 장치)
        ///
        /// - 너무 가까우면(최소 거리 미만) 물러나게 한다 → 즉사 압박이 아니라 "쫓기는 느낌"을 유지
        /// - 너무 멀어지면(최대 거리 초과) 다시 붙인다 → 멈춰 서 있는 것이 안전해지지 않게
        /// - 막다른 길에서는 최대 거리를 유지한다 → 길이 막혔다고 즉시 실패시키지 않는다
        /// </summary>
        private void KeepDistance(float deltaTime)
        {
            if (player == null || pursuer == null || deltaTime <= 0f) return;
            var offset = pursuer.position.x - player.position.x;
            var direction = Mathf.Approximately(offset, 0f) ? -1f : Mathf.Sign(offset);
            var distance = Mathf.Abs(offset);
            float targetDistance;
            if (distance > data.MaxDistance || deadEndBlocked) targetDistance = data.MaxDistance;
            else targetDistance = EffectiveMinDistance;

            if (Mathf.Approximately(distance, targetDistance)) return;

            var targetX = player.position.x + direction * targetDistance;
            var actor = pursuer.GetComponent<TraumaChaseActor>();
            if (actor != null)
            {
                actor.ApplyDirective(CreateDirective(), deltaTime, targetDistance);
                return;
            }

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
