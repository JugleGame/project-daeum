using System.Linq;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    /// <summary>
    /// spec-007/spec-006/spec-005 acceptance criteria that must hold for every StageData asset
    /// currently authored in the project. Only Stage 1 exists in this slice (Stage 2~13 are out of
    /// scope for issue #11), so these tests iterate "all StageData assets that exist today" rather
    /// than a hardcoded count of 13 — they keep passing unmodified once later stages are added.
    /// </summary>
    public sealed class StageProgressionTests
    {
        [Test]
        public void Test_Progression_AllStagesHaveRequiredNarrativeFields()
        {
            var stages = LoadAllStageData();
            Assert.That(stages, Is.Not.Empty, "Expected at least Stage 1's StageData asset to exist.");

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
