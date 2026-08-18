using System;
using System.Collections.Generic;
using Daeume.Core;

namespace Daeume.Flow
{
    public enum SceneFlowStep
    {
        StageCleared,
        StageClearPresentation,
        Save,
        FadeOut,
        SceneLoad,
        StageDataLoad,
        Spawn,
        FadeIn,
        Explore
    }

    public readonly struct SceneRoute
    {
        public SceneRoute(int stageId, string checkpointId, bool newGame)
        {
            StageId = stageId;
            CheckpointId = checkpointId ?? string.Empty;
            NewGame = newGame;
        }

        public int StageId { get; }
        public string CheckpointId { get; }
        public bool NewGame { get; }
    }

    public sealed class SceneFlowPlan
    {
        private static readonly SceneFlowStep[] ClearOrder =
        {
            SceneFlowStep.StageCleared,
            SceneFlowStep.StageClearPresentation,
            SceneFlowStep.Save,
            SceneFlowStep.FadeOut,
            SceneFlowStep.SceneLoad,
            SceneFlowStep.StageDataLoad,
            SceneFlowStep.Spawn,
            SceneFlowStep.FadeIn,
            SceneFlowStep.Explore
        };

        public bool IsTransitioning { get; private set; }

        public SceneRoute NewGame() => new(1, string.Empty, true);

        public SceneRoute Continue(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return new SceneRoute(data.CurrentStageId, data.CheckpointId, false);
        }

        public bool TryBeginTransition()
        {
            if (IsTransitioning)
            {
                return false;
            }

            IsTransitioning = true;
            return true;
        }

        public void CompleteTransition() => IsTransitioning = false;

        public IReadOnlyList<SceneFlowStep> GetStageClearOrder() => ClearOrder;
    }
}
