namespace Daeume.Core
{
    public enum StageFailureCause
    {
        HealthDepleted,
        TraumaGrabCompleted
    }

    public readonly struct StageStateChanged
    {
        public StageStateChanged(StageState state) => State = state;
        public StageState State { get; }
    }

    public readonly struct StageFailed
    {
        public StageFailed(StageFailureCause cause) => Cause = cause;
        public StageFailureCause Cause { get; }
    }

    public sealed class StageLoop
    {
        public StageState State { get; private set; } = StageState.Explore;

        public bool TryTransition(StageState next)
        {
            if (next == StageState.Failed || !IsAllowed(State, next))
            {
                return false;
            }

            State = next;
            return true;
        }

        public bool TryFail(StageFailureCause cause)
        {
            if (cause != StageFailureCause.HealthDepleted &&
                cause != StageFailureCause.TraumaGrabCompleted)
            {
                return false;
            }

            State = StageState.Failed;
            return true;
        }

        public bool CanClearAtExit => State == StageState.Chase;

        public bool TryClearAtExit()
        {
            return CanClearAtExit && TryTransition(StageState.Cleared);
        }

        public void Reset(StageState state = StageState.Explore) => State = state;

        private static bool IsAllowed(StageState current, StageState next)
        {
            return current switch
            {
                StageState.Explore => next == StageState.Memory,
                StageState.Memory => next == StageState.Chase,
                StageState.Chase => next == StageState.Cleared,
                _ => false
            };
        }
    }
}
