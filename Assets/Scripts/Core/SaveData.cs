using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Daeume.Core
{
    /// <summary>
    /// 접근성·보조 설정. (spec-013의 옵션 5종을 담는 그릇, spec-011이 보존 규칙을 소유)
    ///
    /// 중요한 규칙: 이 값들은 "게임 진행 상황"이 아니라 "사용자 설정"이다.
    /// 그래서 저장 슬롯과 별도 파일에 보관하고, 새 게임을 시작해도 유지된다(SaveSystem 참고).
    /// 눈이 부신 흔들림을 껐던 사람이 새 게임을 시작했다고 다시 흔들리면 안 되기 때문이다.
    ///
    /// [Serializable]: 유니티가 이 클래스를 JSON 등으로 저장할 수 있게 표시하는 속성이다.
    /// 필드가 public이어야 저장 대상이 된다(그래서 프로퍼티가 아니라 필드로 선언돼 있다).
    /// </summary>
    [Serializable]
    public sealed class AssistSettings
    {
        public float CameraShakeStrength = 1f;   // 0이면 카메라 흔들림 완전 차단 (spec-013 필수 항목)
        public int SubtitleSize = 1;             // 자막 크기 3단계: 0/1/2
        public bool ChaseSpeedAssist;            // 켜면 추격 속도와 접근 압박만 낮춘다(경로·기믹은 불변)
        public string BindingOverridesJson = string.Empty; // 키 재설정 결과를 담는 자리(Input System이 JSON으로 뱉는다)

        /// <summary>
        /// 값만 똑같은 새 객체를 만든다(깊은 복사).
        /// </summary>
        /// <remarks>
        /// 클래스는 "참조"로 전달되므로 그냥 대입하면 두 곳이 같은 객체를 가리켜,
        /// 한쪽을 고치면 다른 쪽도 바뀐다. 저장 데이터에서 이런 공유는 추적 불가능한 버그가 된다.
        /// 그래서 복사본을 만들어 넘긴다. 적합한 처리다.
        /// </remarks>
        public AssistSettings Copy()
        {
            return new AssistSettings
            {
                CameraShakeStrength = CameraShakeStrength,
                SubtitleSize = SubtitleSize,
                ChaseSpeedAssist = ChaseSpeedAssist,
                BindingOverridesJson = BindingOverridesJson ?? string.Empty
            };
        }
    }

    /// <summary>
    /// 저장 파일에 들어가는 전체 내용. (spec-011이 필드 목록을 소유)
    ///
    /// 핵심 규칙: 씬 참조나 게임오브젝트 핸들을 절대 담지 않는다. 오직 "안정 ID"(문자열)만 담는다.
    /// 게임오브젝트는 다음 실행 때 다른 것이 되므로 저장해도 의미가 없기 때문이다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int SchemaVersion = 1;   // 저장 형식 버전. 형식이 바뀌면 올려서 옛 파일을 구분한다.
        public int CurrentStageId = 1;
        public string CheckpointId = string.Empty;
        public Vector2 PlayerPosition;
        public int PlayerHealth = 1;

        // [FormerlySerializedAs]: 예전 이름으로 저장된 값을 새 이름으로 자동으로 이어 받는다.
        // spec-011의 "OpenedMemoryChest 데이터가 있으면 동일 Anchor 완료 상태로 1회 마이그레이션"을
        // 코드 없이 유니티 기능만으로 처리한 부분이다. 간결하고 적합하다.
        [FormerlySerializedAs("OpenedMemoryChest")]
        public List<string> CompletedMemoryAnchors = new();

        public List<string> CollectedMemoryFragments = new();
        public List<string> DefeatedEncounterState = new();
        public List<string> NarrativeRevealState = new();
        public bool EndingCompleted;

        // 추격 중 사망 시 "같은 오염 공간"으로 되돌리기 위해 필요한 값들 (spec-011)
        public string ContaminationVariantId = string.Empty;
        public string PressureStage = "Stable";

        public int StageThirteenLoopCount;  // Stage 13 힌트 4단계가 소비한다. bool이 아니라 정수여야 한다.
        public bool WeaponLowered;
        public AssistSettings AssistSettings = new();

        /// <summary>
        /// 저장 데이터 전체의 복사본. 리스트도 새로 만들어야 원본과 끊긴다.
        /// </summary>
        /// <remarks>
        /// new List&lt;string&gt;(원본)처럼 리스트를 다시 만들지 않으면 겉모양만 복사되고
        /// 리스트 내용은 여전히 공유된다. 여기서는 모두 새로 만들고 있어 올바르다.
        /// </remarks>
        public SaveData Copy()
        {
            return new SaveData
            {
                SchemaVersion = SchemaVersion,
                CurrentStageId = CurrentStageId,
                CheckpointId = CheckpointId ?? string.Empty,
                PlayerPosition = PlayerPosition,
                PlayerHealth = PlayerHealth,
                CompletedMemoryAnchors = new List<string>(CompletedMemoryAnchors ?? new List<string>()),
                CollectedMemoryFragments = new List<string>(CollectedMemoryFragments ?? new List<string>()),
                DefeatedEncounterState = new List<string>(DefeatedEncounterState ?? new List<string>()),
                NarrativeRevealState = new List<string>(NarrativeRevealState ?? new List<string>()),
                EndingCompleted = EndingCompleted,
                ContaminationVariantId = ContaminationVariantId ?? string.Empty,
                PressureStage = PressureStage ?? "Stable",
                StageThirteenLoopCount = StageThirteenLoopCount,
                WeaponLowered = WeaponLowered,
                AssistSettings = (AssistSettings ?? new AssistSettings()).Copy()
            };
        }
    }
}
