using Daeume.Core;
using Daeume.Encounter;
using UnityEngine;

namespace Daeume.Audio
{
    /// <summary>현재 재생 중인 오디오 큐가 바뀔 때 발행한다. 다른 연출(카메라·UI)이 맞춰 반응할 수 있게 한다.</summary>
    public readonly struct AudioCueChanged
    {
        public AudioCueChanged(AudioCueId cue) => Cue = cue;
        public AudioCueId Cue { get; }
    }

    /// <summary>
    /// 상태별 오디오 큐 5종을 실제로 재생한다. (spec-014)
    ///
    /// 판단(AudioCueResolver)과 재생(이 클래스)을 나눠 둔 이유는 AudioCueResolverTests.cs 참고.
    /// 클립을 아직 배정하지 않은 큐는 조용히 넘어간다 — 이 프로젝트는 블록아웃 단계라 실음원이
    /// 없는 큐가 있을 수 있고, 나중에 클립만 채워 넣으면 이 스크립트를 고칠 필요가 없다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioCuePresenter : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip exploreAmbientClip;
        [SerializeField] private AudioClip encounterCombatClip;
        [SerializeField] private AudioClip memoryClip;
        [SerializeField] private AudioClip chaseClip;
        [SerializeField] private AudioClip clearedClip;

        private bool encounterActive;
        private float baseVolume = 1f;

        public AudioCueId? CurrentCue { get; private set; }

        private void Awake()
        {
            if (musicSource == null) musicSource = GetComponent<AudioSource>();
            if (musicSource != null) baseVolume = musicSource.volume;
        }

        private void OnEnable()
        {
            Connect();
            AudioRuntime.VolumeChanged += ApplyVolume;
            ApplyVolume();
        }
        private void Start() => Connect();
        private void OnDisable()
        {
            Disconnect();
            AudioRuntime.VolumeChanged -= ApplyVolume;
        }

        private void ApplyVolume()
        {
            if (musicSource != null) musicSource.volume = baseVolume * AudioRuntime.BgmVolume;
        }

        private void Connect()
        {
            if (GameManager.Instance == null) return;
            Disconnect();
            GameManager.Instance.Events.Subscribe<StageStateChanged>(OnStageStateChanged);
            GameManager.Instance.Events.Subscribe<EncounterStateChanged>(OnEncounterStateChanged);
        }

        private void Disconnect()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Events.Unsubscribe<StageStateChanged>(OnStageStateChanged);
            GameManager.Instance.Events.Unsubscribe<EncounterStateChanged>(OnEncounterStateChanged);
        }

        private void OnEncounterStateChanged(EncounterStateChanged value)
        {
            encounterActive = value.State == EncounterState.Active;
            Resolve(GameManager.Instance?.StageState ?? StageState.Explore);
        }

        private void OnStageStateChanged(StageStateChanged value) => Resolve(value.State);

        private void Resolve(StageState state)
        {
            var next = AudioCueResolver.Resolve(state, encounterActive);
            if (next == null || next == CurrentCue) return;

            CurrentCue = next;
            ApplyClip(next.Value);
            GameManager.Instance?.Events.Publish(new AudioCueChanged(next.Value));
        }

        private void ApplyClip(AudioCueId cue)
        {
            var clip = cue switch
            {
                AudioCueId.ExploreAmbient => exploreAmbientClip,
                AudioCueId.EncounterCombat => encounterCombatClip,
                AudioCueId.Memory => memoryClip,
                AudioCueId.Chase => chaseClip,
                AudioCueId.Cleared => clearedClip,
                _ => null
            };

            if (musicSource == null) return;
            if (clip == null)
            {
                musicSource.Stop();
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }
}
