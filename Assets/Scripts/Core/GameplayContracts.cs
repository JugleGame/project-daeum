using UnityEngine;

namespace Daeume.Core
{
    public enum DamageTargetKind
    {
        Player,
        Remnant,
        Trauma
    }

    public readonly struct DamageRequest
    {
        public DamageRequest(int amount, GameObject source = null)
        {
            Amount = Mathf.Max(0, amount);
            Source = source;
        }

        public int Amount { get; }
        public GameObject Source { get; }
    }

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

    public interface IDamageable
    {
        DamageTargetKind TargetKind { get; }
        DamageResult ApplyDamage(DamageRequest request);
    }

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

    public readonly struct PlayerAggressionChanged
    {
        public PlayerAggressionChanged(string encounterId)
        {
            EncounterId = encounterId ?? string.Empty;
        }

        public string EncounterId { get; }
    }

    public readonly struct ChaseCheckpointRestoreRequested
    {
        public ChaseCheckpointRestoreRequested(string checkpointId)
        {
            CheckpointId = checkpointId ?? string.Empty;
        }

        public string CheckpointId { get; }
    }

    public readonly struct InteractionPromptChanged
    {
        public InteractionPromptChanged(bool visible, string actionName, string stringTableKey)
        {
            Visible = visible;
            ActionName = actionName ?? string.Empty;
            StringTableKey = stringTableKey ?? string.Empty;
        }

        public bool Visible { get; }
        public string ActionName { get; }
        public string StringTableKey { get; }
    }

    public readonly struct TraumaGrabStarted
    {
        public TraumaGrabStarted(float durationSeconds)
        {
            DurationSeconds = durationSeconds;
        }

        public float DurationSeconds { get; }
    }

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
}
