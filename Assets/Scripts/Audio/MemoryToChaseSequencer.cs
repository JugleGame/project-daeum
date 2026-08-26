using System.Collections;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Audio
{
    /// <summary>Memory→Chase 6단계 중 하나에 들어섰을 때 발행한다. (spec-014)</summary>
    public readonly struct MemoryToChaseCueStepChanged
    {
        public MemoryToChaseCueStepChanged(MemoryToChaseCueStep step) => Step = step;
        public MemoryToChaseCueStep Step { get; }
    }

    /// <summary>spec-014가 정한 Memory→Chase 6단계 순서.</summary>
    public enum MemoryToChaseCueStep
    {
        LastLine,        // 마지막 대사
        BriefSilence,    // 짧은 무음
        AmbientStop,      // 환경음 정지
        MonsterStinger,   // 괴물 효과음
        Reveal,           // 등장
        ChaseBgmStart     // Chase BGM
    }

    /// <summary>
    /// Memory 완료 신호를 받아 spec-014의 6단계 순서를 진행한다. (Memory→Chase 오디오 시퀀스)
    ///
    /// 왜 여기서 추격 시작 타이밍을 건드리지 않나:
    /// 추격 시작(BeginChaseFromMemory)은 spec-006에 따라 MemoryCompletionBridge → B의
    /// StageOneChaseController가 소유한다. 이 시퀀서는 그 순서를 따라가며 "지금 몇 단계인지"만
    /// 알리는 프레젠테이션 전용 관찰자다 — 게임플레이 타이밍을 대신 정하면 역할 경계를 넘게 된다.
    ///
    /// 클립을 아직 배정하지 않아도 단계 신호(MemoryToChaseCueStepChanged)는 정상적으로 순서대로 발행된다.
    /// </summary>
    public sealed class MemoryToChaseSequencer : MonoBehaviour
    {
        [SerializeField] private AudioSource stingerSource;
        [SerializeField] private AudioClip monsterStingerClip;
        [SerializeField, Min(0f)] private float briefSilenceSeconds = 0.3f;

        private Coroutine running;

        private void OnEnable() => Connect();
        private void Start() => Connect();
        private void OnDisable() => GameManager.Instance?.Events.Unsubscribe<MemoryCompleted>(OnMemoryCompleted);

        private void Connect()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Events.Unsubscribe<MemoryCompleted>(OnMemoryCompleted);
            GameManager.Instance.Events.Subscribe<MemoryCompleted>(OnMemoryCompleted);
        }

        private void OnMemoryCompleted(MemoryCompleted message)
        {
            // 이미 진행 중이면 다시 시작하지 않는다. 회상은 스테이지당 한 번만 끝나므로
            // 중복은 실제로는 안 생기지만, 방어적으로 막아 둔다.
            if (running != null) return;
            running = StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            Publish(MemoryToChaseCueStep.LastLine);

            yield return new WaitForSeconds(briefSilenceSeconds);
            Publish(MemoryToChaseCueStep.BriefSilence);

            Publish(MemoryToChaseCueStep.AmbientStop);

            if (stingerSource != null && monsterStingerClip != null)
            {
                stingerSource.PlayOneShot(monsterStingerClip, AudioRuntime.SfxVolume);
            }
            Publish(MemoryToChaseCueStep.MonsterStinger);

            Publish(MemoryToChaseCueStep.Reveal);

            // Chase BGM 자체는 AudioCuePresenter가 StageStateChanged(Chase)를 보고 재생한다.
            // 여기서는 시퀀스가 그 단계에 도달했다는 것만 알린다.
            Publish(MemoryToChaseCueStep.ChaseBgmStart);

            running = null;
        }

        private static void Publish(MemoryToChaseCueStep step)
        {
            GameManager.Instance?.Events.Publish(new MemoryToChaseCueStepChanged(step));
        }
    }
}
