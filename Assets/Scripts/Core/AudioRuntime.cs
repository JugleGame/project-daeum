using UnityEngine;

namespace Daeume.Core
{
    /// <summary>제공된 2D BGM·효과음을 공통으로 재생한다.</summary>
    public static class AudioRuntime
    {
        private static AudioSource sfxSource;
        private static AudioSource titleSource;

        public static float BgmVolume { get; private set; } = 1f;
        public static float SfxVolume { get; private set; } = 1f;
        public static event System.Action VolumeChanged;

        public static void ApplySettings(AssistSettings settings)
        {
            BgmVolume = Mathf.Clamp01(settings?.BgmVolume ?? 1f);
            SfxVolume = Mathf.Clamp01(settings?.SfxVolume ?? 1f);
            if (titleSource != null) titleSource.volume = BgmVolume;
            if (sfxSource != null) sfxSource.volume = SfxVolume;
            VolumeChanged?.Invoke();
        }

        public static void PlaySfx(string cue)
        {
            var clip = Resources.Load<AudioClip>("Audio/Sfx/" + cue);
            if (clip == null || Object.FindAnyObjectByType<AudioListener>() == null) return;
            if (sfxSource == null)
            {
                var host = new GameObject("SfxAudioSource");
                Object.DontDestroyOnLoad(host);
                sfxSource = host.AddComponent<AudioSource>();
                sfxSource.spatialBlend = 0f;
                sfxSource.volume = SfxVolume;
            }

            sfxSource.PlayOneShot(clip);
        }

        public static void PlayTitleMusic()
        {
            if (titleSource != null && titleSource.isPlaying) return;
            var clip = Resources.Load<AudioClip>("Audio/Bgm/Title");
            if (clip == null) return;
            var host = new GameObject("TitleMusicSource");
            Object.DontDestroyOnLoad(host);
            titleSource = host.AddComponent<AudioSource>();
            titleSource.spatialBlend = 0f;
            titleSource.loop = true;
            titleSource.volume = BgmVolume;
            titleSource.clip = clip;
            titleSource.Play();
        }

        public static void StopTitleMusic()
        {
            if (titleSource == null) return;
            Object.Destroy(titleSource.gameObject);
            titleSource = null;
        }
    }
}
