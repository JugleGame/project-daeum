using System.Linq;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    /// <summary>
    /// spec-007/spec-006/spec-005 acceptance criteria that must hold for every StageData asset
    /// currently authored in the project. 3스테이지 재구성 이후 남은 레코드는 Stage01 / Stage10 / Stage13뿐이라,
    /// 이 테스트들은 13개를 고정 개수로 세지 않고 "지금 존재하는 모든 StageData"를 순회한다 —
    /// 스테이지가 다시 늘어나도 수정 없이 통과한다.
    /// </summary>
    public sealed class StageProgressionTests
    {
        [Test]
        public void Test_Progression_AllStagesHaveRequiredNarrativeFields()
        {
            var stages = LoadAllStageData();
            Assert.That(stages, Is.Not.Empty, "Expected at least Stage 1's StageData asset to exist.");
            Assert.That(stages.Select(stage => stage.StageId), Does.Contain(1),
                "3스테이지 재구성(Stage01 -> Stage10 -> Stage13)의 시작 스테이지 레코드가 필요하다.");
            Assert.That(stages.Select(stage => stage.StageId), Does.Contain(10),
                "Issue #57 requires an authored Stage10 record.");
            Assert.That(stages.Select(stage => stage.StageId), Does.Contain(13),
                "Issue #58 requires an authored Stage13 record.");


            foreach (var stage in stages)
            {
                Assert.That(stage.ValidateData(), Is.Empty, $"Stage {stage.StageId} ({stage.name}) has invalid narrative fields.");
            }
        }

        [Test]
        public void Test_Progression_HospitalDirectnessInRange()
        {
            foreach (var stage in LoadAllStageData())
            {
                Assert.That(stage.HospitalImageryDirectness, Is.InRange(0, 4),
                    $"Stage {stage.StageId} HospitalImageryDirectness out of range.");
            }
        }

        [Test]
        public void Test_Progression_EachStageDeclaresTargetChaseSeconds()
        {
            foreach (var stage in LoadAllStageData())
            {
                Assert.That(stage.TargetChaseSeconds, Is.GreaterThan(0f),
                    $"Stage {stage.StageId} must declare a positive TargetChaseSeconds.");
            }
        }

        [Test]
        public void Test_Contamination_EachStageSelectsTwoOrThreePrimaryChannels()
        {
            foreach (var stage in LoadAllStageData())
            {
                // spec-006: Stage 1~11은 2~3개, Stage 12는 클라이맥스 예외(이번 슬라이스에는 없음).
                Assert.That(stage.PrimaryContaminationChannels.Count, Is.InRange(2, 3),
                    $"Stage {stage.StageId} must select 2-3 primary contamination channels.");
            }
        }

        [Test]
        public void Test_Chase_EachStageHasMeaningfulMechanic()
        {
            foreach (var stage in LoadAllStageData())
            {
                Assert.That(stage.ChaseId, Is.Not.Null.And.Not.Empty, $"Stage {stage.StageId} is missing a ChaseId.");
                Assert.That(stage.ChaseMeaning, Is.Not.Null.And.Not.Empty, $"Stage {stage.StageId} is missing a ChaseMeaning.");
            }

            // 고유 기믹: 서로 다른 스테이지가 같은 ChaseId를 재사용하지 않는다.
            var chaseIds = LoadAllStageData().Select(s => s.ChaseId).ToArray();
            Assert.That(chaseIds.Distinct().Count(), Is.EqualTo(chaseIds.Length));
        }

        [Test]
        public void Test_MemoryInteractable_SupportsDistinctPresentation()
        {
            var stages = LoadAllStageData();
            foreach (var stage in stages)
            {
                Assert.That(stage.MemoryPresentationId, Is.Not.Null.And.Not.Empty,
                    $"Stage {stage.StageId} is missing a MemoryPresentationId.");
            }

            // 서로 다른 Stage는 서로 다른 외형(PresentationPrefabId)을 쓴다(spec-005).
            var presentationIds = stages.Select(s => s.MemoryPresentationId).ToArray();
            Assert.That(presentationIds.Distinct().Count(), Is.EqualTo(presentationIds.Length));
        }

        private static StageData[] LoadAllStageData()
        {
            return AssetDatabase.FindAssets("t:StageData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<StageData>)
                .Where(stage => stage != null)
                .OrderBy(stage => stage.StageId)
                .ToArray();
        }
    }
}
