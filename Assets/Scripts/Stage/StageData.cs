using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Daeume.Stage
{
    public enum EmotionalRole
    {
        Introduction,
        Development,
        Escalation,
        Revelation,
        Resolution
    }

    public enum StageLengthCategory
    {
        Short,
        Medium,
        Long
    }

    public enum ContaminationChannel
    {
        Visual,
        Audio,
        Spatial,
        Mechanical,
        Narrative
    }

    [CreateAssetMenu(fileName = "StageData", menuName = "Daeume/Stage Data")]
    public sealed class StageData : ScriptableObject
    {
        public const int MinimumStageId = 1;
        public const int MaximumStageId = 13;
        public const int MinimumIntensity = 0;
        public const int MaximumIntensity = 4;

        [Header("Identity")]
        [SerializeField, Range(MinimumStageId, MaximumStageId)] private int stageId = MinimumStageId;
        [SerializeField] private string location = string.Empty;

        [Header("Memory")]
        [SerializeField] private string memoryId = string.Empty;
        [SerializeField] private string memoryTitle = string.Empty;
        [SerializeField] private string memoryPresentationId = string.Empty;
        [SerializeField] private EmotionalRole emotionalRole;

        [Header("Encounter and Contamination")]
        [SerializeField] private List<string> encounterIds = new();
        [SerializeField] private string contaminationVariantId = string.Empty;
        [SerializeField] private List<ContaminationChannel> primaryContaminationChannels = new();
        [SerializeField, TextArea] private string contaminationMeaning = string.Empty;
        [SerializeField, Range(0, 4)] private int hospitalImageryDirectness;

        [Header("Chase")]
        [SerializeField] private string chaseId = string.Empty;
        [SerializeField, TextArea] private string chaseMeaning = string.Empty;
        [SerializeField, Min(0.1f)] private float targetChaseSeconds = 30f;

        [Header("Pacing")]
        [SerializeField] private StageLengthCategory lengthCategory;
        [SerializeField, Min(1f)] private float targetPlaytimeMinutes = 10f;
        [SerializeField] private List<string> clueIds = new();
        [SerializeField, Range(MinimumIntensity, MaximumIntensity)] private int explorationIntensity;
        [SerializeField, Range(MinimumIntensity, MaximumIntensity)] private int memoryIntensity;
        [SerializeField, Range(MinimumIntensity, MaximumIntensity)] private int encounterIntensity;
        [SerializeField, Range(MinimumIntensity, MaximumIntensity)] private int contaminationIntensity;
        [SerializeField, Range(MinimumIntensity, MaximumIntensity)] private int chaseIntensity;
        [SerializeField, Range(0, MaximumStageId)] private int nextStageId;

        public int StageId => stageId;
        public string Location => location;
        public string MemoryId => memoryId;
        public string MemoryTitle => memoryTitle;
        public string MemoryPresentationId => memoryPresentationId;
        public EmotionalRole EmotionalRole => emotionalRole;
        public IReadOnlyList<string> EncounterIds => encounterIds;
        public string ContaminationVariantId => contaminationVariantId;
        public IReadOnlyList<ContaminationChannel> PrimaryContaminationChannels => primaryContaminationChannels;
        public string ContaminationMeaning => contaminationMeaning;
        public int HospitalImageryDirectness => hospitalImageryDirectness;
        public string ChaseId => chaseId;
        public string ChaseMeaning => chaseMeaning;
        public float TargetChaseSeconds => targetChaseSeconds;
        public StageLengthCategory LengthCategory => lengthCategory;
        public float TargetPlaytimeMinutes => targetPlaytimeMinutes;
        public IReadOnlyList<string> ClueIds => clueIds;
        public int ExplorationIntensity => explorationIntensity;
        public int MemoryIntensity => memoryIntensity;
        public int EncounterIntensity => encounterIntensity;
        public int ContaminationIntensity => contaminationIntensity;
        public int ChaseIntensity => chaseIntensity;
        public int NextStageId => nextStageId;

        public IReadOnlyList<string> ValidateData()
        {
            var errors = new List<string>();
            RequireRange(errors, nameof(StageId), stageId, MinimumStageId, MaximumStageId);
            RequireText(errors, nameof(Location), location);
            RequireText(errors, nameof(MemoryId), memoryId);
            RequireText(errors, nameof(MemoryTitle), memoryTitle);
            RequireText(errors, nameof(MemoryPresentationId), memoryPresentationId);
            RequireIds(errors, nameof(EncounterIds), encounterIds, true);
            RequireText(errors, nameof(ContaminationVariantId), contaminationVariantId);
            if (primaryContaminationChannels == null || primaryContaminationChannels.Count == 0)
            {
                errors.Add($"{nameof(PrimaryContaminationChannels)} must contain at least one channel.");
            }
            else if (primaryContaminationChannels.Distinct().Count() != primaryContaminationChannels.Count)
            {
                errors.Add($"{nameof(PrimaryContaminationChannels)} contains duplicate channels.");
            }

            RequireText(errors, nameof(ContaminationMeaning), contaminationMeaning);
            RequireRange(errors, nameof(HospitalImageryDirectness), hospitalImageryDirectness, 0, 4);
            RequireText(errors, nameof(ChaseId), chaseId);
            RequireText(errors, nameof(ChaseMeaning), chaseMeaning);
            if (targetChaseSeconds <= 0f)
            {
                errors.Add($"{nameof(TargetChaseSeconds)} must be greater than zero.");
            }

            if (targetPlaytimeMinutes <= 0f)
            {
                errors.Add($"{nameof(TargetPlaytimeMinutes)} must be greater than zero.");
            }

            RequireIds(errors, nameof(ClueIds), clueIds, true);
            RequireRange(errors, nameof(ExplorationIntensity), explorationIntensity, MinimumIntensity, MaximumIntensity);
            RequireRange(errors, nameof(MemoryIntensity), memoryIntensity, MinimumIntensity, MaximumIntensity);
            RequireRange(errors, nameof(EncounterIntensity), encounterIntensity, MinimumIntensity, MaximumIntensity);
            RequireRange(errors, nameof(ContaminationIntensity), contaminationIntensity, MinimumIntensity, MaximumIntensity);
            RequireRange(errors, nameof(ChaseIntensity), chaseIntensity, MinimumIntensity, MaximumIntensity);
            RequireRange(errors, nameof(NextStageId), nextStageId, 0, MaximumStageId);
            if (nextStageId == stageId)
            {
                errors.Add($"{nameof(NextStageId)} cannot reference the same stage.");
            }

            return errors;
        }

        private static void RequireText(ICollection<string> errors, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{field} is required.");
            }
        }

        private static void RequireIds(ICollection<string> errors, string field, IEnumerable<string> values, bool requireAny)
        {
            var ids = values?.ToArray() ?? Array.Empty<string>();
            if (requireAny && ids.Length == 0)
            {
                errors.Add($"{field} must contain at least one id.");
                return;
            }

            if (ids.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"{field} contains an empty id.");
            }

            if (ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).Count() !=
                ids.Count(id => !string.IsNullOrWhiteSpace(id)))
            {
                errors.Add($"{field} contains duplicate ids.");
            }
        }

        private static void RequireRange(ICollection<string> errors, string field, int value, int minimum, int maximum)
        {
            if (value < minimum || value > maximum)
            {
                errors.Add($"{field} must be between {minimum} and {maximum}.");
            }
        }
    }
}
