using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Daeume.Core
{
    public enum SaveLoadStatus
    {
        Loaded,
        FirstRun,
        RecoveredCorrupt,
        RecoveredUnsupportedVersion
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

    public interface ISaveStore
    {
        bool Exists { get; }
        string Read();
        void Write(string json);
        void Delete();
    }

    public sealed class FileSaveStore : ISaveStore
    {
        private readonly string path;

        public FileSaveStore(string path) => this.path = path;
        public bool Exists => File.Exists(path);
        public string Read() => File.ReadAllText(path);

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

    public sealed class SaveSystem
    {
        public const int CurrentSchemaVersion = 1;
        private readonly ISaveStore store;
        private readonly ISaveStore settingsStore;
        private AssistSettings assistSettings = new();

        public SaveSystem(ISaveStore store, ISaveStore settingsStore = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.settingsStore = settingsStore;
        }

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
                    return NewResult(SaveLoadStatus.RecoveredCorrupt, maxHealth);
                }

                if (data.SchemaVersion != CurrentSchemaVersion)
                {
                    return NewResult(SaveLoadStatus.RecoveredUnsupportedVersion, maxHealth);
                }

                Normalize(data, maxHealth);
                if (settingsStore != null && settingsStore.Exists)
                {
                    data.AssistSettings = assistSettings.Copy();
                }
                else
                {
                    assistSettings = data.AssistSettings.Copy();
                }
                return new SaveLoadResult(SaveLoadStatus.Loaded, data);
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is IOException || exception is UnauthorizedAccessException)
            {
                return NewResult(SaveLoadStatus.RecoveredCorrupt, maxHealth);
            }
        }

        public void Save(SaveData data, int maxHealth)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var copy = data.Copy();
            copy.SchemaVersion = CurrentSchemaVersion;
            Normalize(copy, maxHealth);
            assistSettings = copy.AssistSettings.Copy();
            settingsStore?.Write(JsonUtility.ToJson(assistSettings, true));
            store.Write(JsonUtility.ToJson(copy, true));
        }

        public SaveData CreateNewGame(int maxHealth)
        {
            var data = CreateDefault(maxHealth);
            data.AssistSettings = assistSettings.Copy();
            return data;
        }

        public void DeleteProgress() => store.Delete();

        public static int ResolveRespawnHealth(SaveData data, int maxHealth, bool deathRestore, int checkpointRespawnHealth)
        {
            if (deathRestore)
            {
                var declared = checkpointRespawnHealth <= 0 ? maxHealth : checkpointRespawnHealth;
                return Mathf.Clamp(declared, 1, maxHealth);
            }

            return Mathf.Clamp(data?.PlayerHealth ?? maxHealth, 1, maxHealth);
        }

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

        private static void Normalize(SaveData data, int maxHealth)
        {
            data.CurrentStageId = Mathf.Max(1, data.CurrentStageId);
            data.PlayerHealth = Mathf.Clamp(data.PlayerHealth, 1, Mathf.Max(1, maxHealth));
            data.CheckpointId ??= string.Empty;
            data.CompletedMemoryAnchors ??= new List<string>();
            data.CollectedMemoryFragments ??= new List<string>();
            data.DefeatedEncounterState ??= new List<string>();
            data.NarrativeRevealState ??= new List<string>();
            data.ContaminationVariantId ??= string.Empty;
            data.PressureStage ??= "Stable";
            data.AssistSettings ??= new AssistSettings();
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
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is IOException || exception is UnauthorizedAccessException)
            {
                assistSettings = new AssistSettings();
            }
        }
    }
}
