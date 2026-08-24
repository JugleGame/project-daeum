using System;
using System.Collections.Generic;
using Daeume.Core;

namespace Daeume.Flow
{
    /// <summary>
    /// 스테이지 클리어 시 거쳐야 하는 9단계. (spec-015가 순서를 고정한다)
    /// 순서를 코드 여기저기에 흩어 두지 않고 목록 하나로 선언해 두면
    /// "저장 전에 씬을 갈아 끼워 진행이 날아가는" 류의 사고를 구조적으로 막을 수 있다.
    /// </summary>
    public enum SceneFlowStep
    {
        StageCleared,             // 상태를 Cleared로
        StageClearPresentation,   // 클리어 연출
        Save,                     // 저장 (씬 교체보다 반드시 먼저)
        FadeOut,
        SceneLoad,
        StageDataLoad,
        Spawn,
        FadeIn,
        Explore                   // 다음 스테이지 탐색 상태로
    }

    /// <summary>어느 스테이지의 어느 체크포인트로 갈지 정리한 값.</summary>
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

    /// <summary>
    /// 씬 전환의 "규칙"만 담은 순수 C# 클래스다(MonoBehaviour 아님).
    /// 실제 씬 적재는 SceneFlowController가 하고, 이 클래스는 순서와 중복 방지만 책임진다.
    /// 덕분에 EditMode 테스트가 씬을 실제로 열지 않고 순서를 검증할 수 있다. 적합한 분리다.
    /// </summary>
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

        /// <summary>지금 씬 전환이 진행 중인가. 중복 전환을 막는 잠금 역할을 한다.</summary>
        public bool IsTransitioning { get; private set; }

        public SceneRoute NewGame() => new(1, string.Empty, true);

        public SceneRoute Continue(SaveData data)
        {
            if (data == null)
            {
                // 저장 데이터 없이 "이어하기"를 호출한 것은 명백한 프로그래밍 실수다.
                // 조용히 새 게임으로 처리하면 플레이어의 진행이 사라진 것처럼 보이므로 즉시 예외를 던진다.
                throw new ArgumentNullException(nameof(data));
            }

            return new SceneRoute(data.CurrentStageId, data.CheckpointId, false);
        }

        /// <summary>
        /// 전환을 시작한다. 이미 전환 중이면 false를 돌려준다.
        /// </summary>
        /// <remarks>
        /// 이 잠금이 spec-015의 "중복 전환 입력을 차단한다"를 담당한다.
        /// 버튼을 두 번 빠르게 누르면 씬 적재가 두 번 시작돼 오브젝트가 두 벌 생기는 사고가 난다.
        /// </remarks>
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

        /// <summary>
        /// 클리어 단계 순서를 읽기 전용으로 넘겨준다.
        /// IReadOnlyList로 돌려주므로 받는 쪽이 순서를 실수로 바꿀 수 없다. 적합한 방어다.
        /// </summary>
        public IReadOnlyList<SceneFlowStep> GetStageClearOrder() => ClearOrder;
    }
}
