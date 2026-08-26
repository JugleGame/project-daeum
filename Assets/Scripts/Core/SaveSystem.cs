using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Daeume.Core
{
    /// <summary>저장 파일을 읽은 결과 상태. 실패를 "예외"가 아니라 "결과값"으로 다룬다.</summary>
    /// <remarks>
    /// spec-011은 "저장 없음은 Stage 1, 손상·미지원 버전은 명시적 복구 결과를 반환한다"를 요구한다.
    /// 즉 파일이 깨졌다고 게임이 멈추면 안 되고, 무슨 일이 있었는지 알 수 있어야 한다.
    /// </remarks>
    public enum SaveLoadStatus
    {
        Loaded,                        // 정상적으로 불러옴
        FirstRun,                      // 저장 파일이 아예 없음(첫 실행)
        RecoveredCorrupt,              // 파일이 깨져 새 데이터로 복구함
        RecoveredUnsupportedVersion    // 형식 버전이 달라 새 데이터로 복구함
    }

    public readonly struct SaveLoadResult
    {
        public SaveLoadResult(SaveLoadStatus status, SaveData data)
        {
            Status = status;
            Data = data;
        }

        public SaveLoadStatus Status { get; }
        public SaveData Data { get; }
    }

    /// <summary>
    /// "저장 공간"의 약속. 실제 파일일 수도, 테스트용 메모리일 수도 있다.
    /// </summary>
    /// <remarks>
    /// 이렇게 인터페이스로 한 겹 떼어 놓으면 테스트가 진짜 파일을 만들지 않고도 저장 로직을 검증할 수 있다.
    /// SaveSystemTests가 이 구조 덕분에 빠르게 돈다. 적합한 설계다.
    /// </remarks>
    public interface ISaveStore
    {
        bool Exists { get; }
        string Read();
        void Write(string json);
        void Delete();
    }

    /// <summary>실제 디스크 파일에 읽고 쓰는 구현.</summary>
    public sealed class FileSaveStore : ISaveStore
    {
        private readonly string path;

        public FileSaveStore(string path) => this.path = path;
        public bool Exists => File.Exists(path);
        public string Read() => File.ReadAllText(path);

        /// <summary>
        /// 임시 파일에 먼저 쓴 뒤 이름을 바꾸는 방식으로 저장한다.
        /// </summary>
        /// <remarks>
        /// 저장 도중 게임이 강제 종료되면 파일이 반쯤 쓰인 상태로 남아 다음 실행에서 세이브가 날아간다.
        /// 임시 파일 → 교체 순서로 하면 "완전한 옛 파일"이나 "완전한 새 파일" 둘 중 하나만 존재한다.
        /// 세이브 손실을 막는 중요한 처리이며, 지우면 안 된다.
        ///
        /// 검토 메모: File.Delete 후 File.Move 사이에서 종료되면 파일이 잠깐 없어질 수 있다.
        /// 그 경우에도 다음 실행은 FirstRun으로 안전하게 시작하므로 크래시는 없다.
        /// 완전한 원자적 교체가 필요해지면 File.Replace로 바꾸면 된다.
        /// </remarks>
        public void Write(string json)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporaryPath, path);
        }

        public void Delete()
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// 저장/불러오기 규칙 전체를 담당한다. (spec-011)
    ///
    /// 진행 상황(store)과 사용자 설정(settingsStore)을 서로 다른 파일에 나눠 저장하는 것이 핵심이다.
    /// 그래야 "새 게임을 시작해도 접근성 설정은 유지된다"는 요구를 만족할 수 있다.
    /// </summary>
    public sealed class SaveSystem
    {
        public const int CurrentSchemaVersion = 1;
        private readonly ISaveStore store;          // 진행 상황
        private readonly ISaveStore settingsStore;  // 사용자 설정(선택 사항)
        private AssistSettings assistSettings = new();

        public SaveSystem(ISaveStore store, ISaveStore settingsStore = null)
        {
            // 진행 저장소가 없으면 이 클래스는 존재 의미가 없으므로 생성 시점에 바로 막는다.
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.settingsStore = settingsStore;
        }

        /// <summary>저장 파일을 불러온다. 어떤 상황에서도 사용 가능한 SaveData를 돌려준다.</summary>
        public SaveLoadResult Load(int maxHealth)
        {
            LoadAssistSettings();
            if (!store.Exists)
            {
                return NewResult(SaveLoadStatus.FirstRun, maxHealth);
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(store.Read());
                if (data == null)
                {
                    // 내용이 비었거나 JSON이 아니면 null이 온다.
                    return NewResult(SaveLoadStatus.RecoveredCorrupt, maxHealth);
                }

                if (data.SchemaVersion != CurrentSchemaVersion)
                {
                    // 형식이 다르면 억지로 읽지 않는다. 잘못 읽어 이상한 상태로 진행하는 것보다 낫다.
                    return NewResult(SaveLoadStatus.RecoveredUnsupportedVersion, maxHealth);
                }

                Normalize(data, maxHealth);

                // 설정 파일이 따로 있으면 그쪽이 우선이다(설정은 슬롯과 무관하게 하나로 관리).
                if (settingsStore != null && settingsStore.Exists)
                {
                    data.AssistSettings = assistSettings.Copy();
                }
                else
                {
                    // 설정 파일이 아직 없던 옛 저장본이면, 저장 데이터 안의 설정을 이어받는다.
                    assistSettings = data.AssistSettings.Copy();
                }

                return new SaveLoadResult(SaveLoadStatus.Loaded, data);
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is IOException || exception is UnauthorizedAccessException)
            {
                // 예상 가능한 실패(형식 오류·파일 접근 실패)만 골라 잡는다.
                // 모든 예외를 잡으면 코드 버그까지 조용히 삼켜 원인을 못 찾게 된다. 적합한 범위 지정이다.
                return NewResult(SaveLoadStatus.RecoveredCorrupt, maxHealth);
            }
        }

        /// <summary>현재 상태를 저장한다. 설정은 별도 파일로 함께 저장한다.</summary>
        public void Save(SaveData data, int maxHealth)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // 복사본을 만들어 저장한다. 저장 중에 게임 쪽에서 값이 바뀌어도 파일 내용이 뒤섞이지 않는다.
            var copy = data.Copy();
            copy.SchemaVersion = CurrentSchemaVersion;
            Normalize(copy, maxHealth);
            assistSettings = copy.AssistSettings.Copy();
            settingsStore?.Write(JsonUtility.ToJson(assistSettings, true));
            store.Write(JsonUtility.ToJson(copy, true));
        }

        /// <summary>새 게임용 데이터. 진행은 초기화하되 사용자 설정은 그대로 물려준다(spec-011).</summary>
        public SaveData CreateNewGame(int maxHealth)
        {
            var data = CreateDefault(maxHealth);
            data.AssistSettings = assistSettings.Copy();
            return data;
        }

        public void DeleteProgress() => store.Delete();

        /// <summary>
        /// 부활 시 체력을 결정한다. (spec-011)
        /// - 사망 후 복귀: 체크포인트가 선언한 RespawnHealth(기본 최대 체력). 반복 실패로 진행 불가가 되는 것을 막는다.
        /// - 일반 재실행: 저장된 체력 그대로, 단 1 미만으로 떨어지지 않게 보정한다.
        /// </summary>
        public static int ResolveRespawnHealth(SaveData data, int maxHealth, bool deathRestore, int checkpointRespawnHealth)
        {
            if (deathRestore)
            {
                var declared = checkpointRespawnHealth <= 0 ? maxHealth : checkpointRespawnHealth;
                return Mathf.Clamp(declared, 1, maxHealth);
            }

            // 체력 0으로 복원하면 되살아나자마자 다시 죽는 무한 루프가 된다. 최소 1을 보장한다.
            return Mathf.Clamp(data?.PlayerHealth ?? maxHealth, 1, maxHealth);
        }

        /// <summary>같은 ID를 두 번 넣지 않는 리스트 추가. 기억 조각 중복 지급을 막는다(spec-011).</summary>
        public static void AddUnique(List<string> stableIds, string stableId)
        {
            if (stableIds == null || string.IsNullOrWhiteSpace(stableId) || stableIds.Contains(stableId))
            {
                return;
            }

            stableIds.Add(stableId);
        }

        private SaveLoadResult NewResult(SaveLoadStatus status, int maxHealth)
        {
            return new SaveLoadResult(status, CreateNewGame(maxHealth));
        }

        private static SaveData CreateDefault(int maxHealth)
        {
            return new SaveData
            {
                SchemaVersion = CurrentSchemaVersion,
                CurrentStageId = 1,
                PlayerHealth = Mathf.Max(1, maxHealth)
            };
        }

        /// <summary>
        /// 값이 이상하거나 비어 있는 항목을 안전한 기본값으로 맞춘다.
        /// 손상된 파일이나 옛 파일을 읽어도 이후 코드가 null 검사를 하지 않아도 되게 하는 방어선이다.
        /// </summary>
        private static void Normalize(SaveData data, int maxHealth)
        {
            data.CurrentStageId = Mathf.Max(1, data.CurrentStageId);
            data.PlayerHealth = Mathf.Clamp(data.PlayerHealth, 1, Mathf.Max(1, maxHealth));
            data.CheckpointId ??= string.Empty;                     // ??= 는 "null일 때만 대입"이다.
            data.CompletedMemoryAnchors ??= new List<string>();
            data.CollectedMemoryFragments ??= new List<string>();
            data.DefeatedEncounterState ??= new List<string>();
            data.NarrativeRevealState ??= new List<string>();
            data.ContaminationVariantId ??= string.Empty;
            data.PressureStage ??= "Stable";
            data.AssistSettings ??= new AssistSettings();
            NormalizeAudioVolumes(data.AssistSettings);
        }

        private void LoadAssistSettings()
        {
            if (settingsStore == null || !settingsStore.Exists)
            {
                return;
            }

            try
            {
                assistSettings = JsonUtility.FromJson<AssistSettings>(settingsStore.Read()) ?? new AssistSettings();
                NormalizeAudioVolumes(assistSettings);
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is IOException || exception is UnauthorizedAccessException)
            {
                // 설정 파일이 깨졌다고 게임을 막을 이유는 없다. 기본 설정으로 계속 진행한다.
                assistSettings = new AssistSettings();
            }
        }

        private static void NormalizeAudioVolumes(AssistSettings settings)
        {
            if (settings.AudioVolumeSchemaVersion == 0)
            {
                settings.BgmVolume = 1f;
                settings.SfxVolume = 1f;
            }
            else
            {
                settings.BgmVolume = Mathf.Clamp01(settings.BgmVolume);
                settings.SfxVolume = Mathf.Clamp01(settings.SfxVolume);
            }

            settings.AudioVolumeSchemaVersion = 1;
        }
    }
}
