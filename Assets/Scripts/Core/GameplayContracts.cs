using UnityEngine;

namespace Daeume.Core
{
    // 이 파일은 "역할 간 공용 계약"을 모아 둔 곳이다.
    // A(시스템)/B(추격·레벨)/C(연출)가 서로의 코드를 읽지 않고도 붙일 수 있게,
    // 주고받는 데이터의 모양만 여기에 정의한다. 이 파일을 바꾸면 세 사람 모두에게 영향이 간다.
    //
    // 대부분 readonly struct다.
    // - struct: 클래스와 달리 값 자체가 복사돼 전달된다. 작은 메시지에 적합하고 쓰레기(GC) 부담이 적다.
    // - readonly: 만든 뒤 값을 바꿀 수 없다. 이벤트를 받은 쪽이 내용을 몰래 고쳐 다른 구독자에게
    //   영향을 주는 사고를 원천 차단한다. 이벤트 메시지로는 아주 적합한 선택이다.

    /// <summary>피해를 받을 수 있는 대상의 종류. 같은 공격이라도 대상에 따라 규칙이 다르다.</summary>
    public enum DamageTargetKind
    {
        Player,
        Remnant,  // 잔재: 처치 가능한 일반 적
        Trauma    // 트라우마: spec-003에 따라 공격이 절대 통하지 않는 추격자
    }

    /// <summary>공격 한 번의 요청 내용(피해량과 때린 주체).</summary>
    public readonly struct DamageRequest
    {
        public DamageRequest(int amount, GameObject source = null)
        {
            // 음수 피해(= 회복)를 공격 경로로 흘려보내는 실수를 입구에서 막는다. 적합한 방어다.
            Amount = Mathf.Max(0, amount);
            Source = source;
        }

        public int Amount { get; }
        public GameObject Source { get; }
    }

    /// <summary>공격 처리 결과. 실제로 피해가 들어갔는지와 최종 피해량을 돌려준다.</summary>
    /// <remarks>
    /// 무적 시간이거나 트라우마처럼 무효 대상이면 Applied=false가 된다.
    /// 호출한 쪽은 이 값을 보고 타격 이펙트를 낼지 말지 결정한다.
    /// </remarks>
    public readonly struct DamageResult
    {
        public DamageResult(bool applied, int amount)
        {
            Applied = applied;
            Amount = amount;
        }

        public bool Applied { get; }
        public int Amount { get; }
    }

    /// <summary>
    /// "때릴 수 있는 것"이 지켜야 할 약속(인터페이스).
    /// 플레이어든 잔재든 이 약속만 따르면 공격 코드는 상대가 무엇인지 몰라도 된다.
    /// </summary>
    public interface IDamageable
    {
        DamageTargetKind TargetKind { get; }
        DamageResult ApplyDamage(DamageRequest request);
    }

    /// <summary>체력이 바뀔 때 발행. HUD(C)가 구독해 숫자를 갱신한다.</summary>
    public readonly struct PlayerHealthChanged
    {
        public PlayerHealthChanged(int current, int maximum)
        {
            Current = current;
            Maximum = maximum;
        }

        public int Current { get; }
        public int Maximum { get; }
    }

    /// <summary>
    /// 플레이어가 잔재를 실제로 "맞혔을 때" 발행한다. (spec-003)
    /// 빗나간 헛스윙으로는 발행하지 않는다 — Stage 11의 비선공 통과 판정이 이 값을 쓰기 때문이다.
    /// </summary>
    public readonly struct PlayerAggressionChanged
    {
        public PlayerAggressionChanged(string encounterId)
        {
            EncounterId = encounterId ?? string.Empty;
        }

        public string EncounterId { get; }
    }

    /// <summary>
    /// Encounter가 Cleared 상태가 될 때 발행한다. (spec-003)
    /// Daeume.Player가 Daeume.Encounter를 직접 참조하면 asmdef 순환 참조(Encounter가 이미 Player를 참조)가
    /// 생기므로, 공용 계약인 여기(Daeume.Core)를 거쳐 PlayerCombat이 선공 여부를 스스로 초기화한다.
    /// </summary>
    public readonly struct EncounterCleared
    {
        public EncounterCleared(string encounterId)
        {
            EncounterId = encounterId ?? string.Empty;
        }

        public string EncounterId { get; }
    }

    /// <summary>추격 중 사망 후 체크포인트 복귀를 요청하는 메시지. (spec-011)</summary>
    public readonly struct ChaseCheckpointRestoreRequested
    {
        public ChaseCheckpointRestoreRequested(string checkpointId)
        {
            CheckpointId = checkpointId ?? string.Empty;
        }

        public string CheckpointId { get; }
    }

    /// <summary>
    /// 상호작용 프롬프트 표시 요청. (spec-010, spec-013)
    /// 문장을 직접 담지 않고 "입력 액션 이름 + 문자열 키"만 담는 것이 핵심이다.
    /// 덕분에 키를 재설정(리매핑)하면 프롬프트 표시도 자동으로 따라간다.
    /// </summary>
    public readonly struct InteractionPromptChanged
    {
        public InteractionPromptChanged(bool visible, string actionName, string stringTableKey)
        {
            Visible = visible;
            // null 대신 빈 문자열로 통일한다. 받는 쪽이 매번 null 검사를 하지 않아도 된다.
            ActionName = actionName ?? string.Empty;
            StringTableKey = stringTableKey ?? string.Empty;
        }

        public bool Visible { get; }
        public string ActionName { get; }
        public string StringTableKey { get; }
    }

    /// <summary>트라우마에게 붙잡히는 연출이 시작됨. 연출 길이를 함께 전달한다. (spec-003)</summary>
    public readonly struct TraumaGrabStarted
    {
        public TraumaGrabStarted(float durationSeconds)
        {
            DurationSeconds = durationSeconds;
        }

        public float DurationSeconds { get; }
    }

    /// <summary>저장 데이터로부터 플레이어 위치·체력을 복원해 달라는 요청. 씬 흐름(A)이 발행한다.</summary>
    public readonly struct PlayerRestoreRequested
    {
        public PlayerRestoreRequested(Vector2 position, int health)
        {
            Position = position;
            Health = health;
        }

        public Vector2 Position { get; }
        public int Health { get; }
    }

    /// <summary>
    /// 회상 자막 한 줄이 바뀔 때마다 발행한다. (spec-005)
    /// 문장 자체가 아니라 문자열 키(LineKey)를 보낸다 — 원고를 코드·씬에 담지 않기 위해서다.
    /// LineIndex/LineCount는 "3문장 중 2번째" 같은 진행 표시에 쓸 수 있다.
    /// </summary>
    public readonly struct MemoryPresentationChanged
    {
        public MemoryPresentationChanged(string memoryId, string titleKey, string lineKey, int lineIndex, int lineCount, bool visible)
        {
            MemoryId = memoryId ?? string.Empty;
            TitleKey = titleKey ?? string.Empty;
            LineKey = lineKey ?? string.Empty;
            LineIndex = Mathf.Max(0, lineIndex);
            LineCount = Mathf.Max(0, lineCount);
            Visible = visible;
        }

        public string MemoryId { get; }
        public string TitleKey { get; }
        public string LineKey { get; }
        public int LineIndex { get; }
        public int LineCount { get; }
        public bool Visible { get; }
    }

    /// <summary>
    /// 회상이 끝났음을 알린다. 이 메시지가 오염 전환과 추격 시작의 방아쇠다. (spec-005 → spec-006)
    /// 정상 종료와 건너뛰기 모두 같은 메시지를 발행해야 한다("두 종료 경로가 같은 흐름을 요청한다").
    /// </summary>
    public readonly struct MemoryCompleted
    {
        public MemoryCompleted(string memoryId, string narrativeFlag)
        {
            MemoryId = memoryId ?? string.Empty;
            NarrativeFlag = narrativeFlag ?? string.Empty;
        }

        public string MemoryId { get; }
        public string NarrativeFlag { get; }
    }
}
