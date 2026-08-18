using System;
using Daeume.Contamination;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
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

        private void Update()
        {
            ResolveActors();
            Tick(Time.deltaTime);
        }

        public void Configure(ContaminationVariantData variantData, Transform playerTransform, Transform pursuerTransform, string id = "chase-stage01-left-escape")
        {
            data = variantData;
            player = playerTransform;
            pursuer = pursuerTransform;
            chaseId = id ?? string.Empty;
        }

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
