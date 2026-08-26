using Daeume.Core;
using UnityEngine;

namespace Daeume.UI
{
    /// <summary>
    /// 접근성·보조 설정 값을 보관하고 범위를 보정한다. (spec-013)
    ///
    /// ■ 검토 결과: 현재 이 클래스는 "값 그릇"까지만 구현돼 있고, 실제 옵션 화면이 없다.
    /// spec-013은 접근성 옵션 5종(컨트롤 리매핑 / 흔들림 강도 0 / 자막 크기 3단계 / 추격 속도 저하)을
    /// "잘라낼 수 없는 항목"으로 못 박고 있으며, design-decisions 5번도 같은 결론이다.
    /// 남은 작업은 다음 세 가지다.
    ///   1) 타이틀에서 열 수 있는 옵션 화면(UI) 제작
    ///   2) 변경값을 SaveSystem의 설정 파일에 저장 — 진행 슬롯과 별개로 보존되어야 한다
    ///   3) 소비처 연결: 흔들림은 PressurePresentationController, 속도는 ChaseSpeedAssistAdapter,
    ///      자막 크기는 StageHudPresenter/MemoryPanelPresenter가 읽어 가야 한다
    ///      (흔들림·속도 두 개는 이번 수정에서 저장값을 스스로 읽도록 연결해 두었다).
    ///
    /// 옵션 화면에는 옵션 사용을 평가하는 문구를 넣지 않는다("쉬움 모드" 같은 표현 금지) — 스펙 명시 사항.
    /// </summary>
    public sealed class AssistSettingsPresenter : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float cameraShakeStrength = 0.5f;
        [SerializeField, Range(0, 2)] private int subtitleSize = 1;
        [SerializeField] private bool chaseSpeedAssist;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        private string bindingOverridesJson = string.Empty;

        /// <summary>현재 값을 저장 가능한 형태로 만들어 돌려준다.</summary>
        public AssistSettings Current => new()
        {
            CameraShakeStrength = cameraShakeStrength,
            SubtitleSize = subtitleSize,
            ChaseSpeedAssist = chaseSpeedAssist,
            BindingOverridesJson = bindingOverridesJson,
            AudioVolumeSchemaVersion = 1,
            BgmVolume = bgmVolume,
            SfxVolume = sfxVolume
        };

        /// <summary>저장된 설정을 화면 값에 반영한다. 범위를 벗어난 값은 안전하게 잘라 낸다.</summary>
        public void Apply(AssistSettings settings)
        {
            settings ??= new AssistSettings();
            cameraShakeStrength = Mathf.Clamp01(settings.CameraShakeStrength);
            subtitleSize = Mathf.Clamp(settings.SubtitleSize, 0, 2);
            chaseSpeedAssist = settings.ChaseSpeedAssist;
            bindingOverridesJson = settings.BindingOverridesJson ?? string.Empty;
            bgmVolume = Mathf.Clamp01(settings.BgmVolume);
            sfxVolume = Mathf.Clamp01(settings.SfxVolume);
        }

        // 아래 네 함수는 옵션 화면의 슬라이더·토글·리매핑이 호출할 진입점이다.
        // 값 보정을 이 안에서 하므로, UI 쪽에서 범위를 다시 검사할 필요가 없다.
        public void SetCameraShake(float value) => cameraShakeStrength = Mathf.Clamp01(value);
        public void SetSubtitleSize(int value) => subtitleSize = Mathf.Clamp(value, 0, 2);
        public void SetChaseAssist(bool value) => chaseSpeedAssist = value;
        public void SetBindingOverrides(string json) => bindingOverridesJson = json ?? string.Empty;
        public void SetBgmVolume(float value) => bgmVolume = Mathf.Clamp01(value);
        public void SetSfxVolume(float value) => sfxVolume = Mathf.Clamp01(value);
    }
}
