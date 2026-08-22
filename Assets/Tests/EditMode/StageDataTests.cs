using System.Linq;
using Daeume.Contamination;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class StageDataTests
    {
        private const string StageOnePath = "Assets/Data/Stages/Stage01.asset";

        [Test]
        public void Test_StageData_SchemaSupportsThirteenStagesAndFiveIntensities()
        {
            Assert.That(StageData.MinimumStageId, Is.EqualTo(1));
            Assert.That(StageData.MaximumStageId, Is.EqualTo(13));
            Assert.That(StageData.MinimumIntensity, Is.EqualTo(0));
            Assert.That(StageData.MaximumIntensity, Is.EqualTo(4));

            var data = LoadStageOne();
            var intensities = new[]
            {
                data.ExplorationIntensity,
                data.MemoryIntensity,
                data.EncounterIntensity,
                data.ContaminationIntensity,
                data.ChaseIntensity
            };
            Assert.That(intensities, Has.Length.EqualTo(5));
            Assert.That(intensities, Has.All.InRange(StageData.MinimumIntensity, StageData.MaximumIntensity));
        }

        [Test]
        public void Test_StageData_StageOneRecordHasValidRequiredReferences()
        {
            var data = LoadStageOne();

            Assert.That(data.ValidateData(), Is.Empty);
            Assert.That(data.StageId, Is.EqualTo(1));
            Assert.That(data.NextStageId, Is.EqualTo(2));
            Assert.That(data.HospitalImageryDirectness, Is.InRange(0, 4));
            Assert.That(data.EncounterIds, Is.Not.Empty);
            Assert.That(data.EncounterIds.Distinct().Count(), Is.EqualTo(data.EncounterIds.Count));
            Assert.That(data.PrimaryContaminationChannels, Is.Not.Empty);
            Assert.That(data.ContaminationVariantId, Is.EqualTo("Stage01_Overlay_Intrusion"));
            Assert.That(data.ChaseId, Is.EqualTo("chase-stage01-left-escape"));
        }

        [Test]
        public void Test_Stage07_WinterDormitoryDataAndVariantAreValid()
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage07/Stage07.asset");
            var variant = AssetDatabase.LoadAssetAtPath<ContaminationVariantData>("Assets/Data/Contamination/Stage07/Stage07_ContaminationVariant.asset");
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.ValidateData(), Is.Empty);
            Assert.That(stage.HospitalImageryDirectness, Is.EqualTo(2));
            Assert.That(variant, Is.Not.Null);
            Assert.That(variant.ValidateData(out var error), Is.True, error);
        }

        [Test]
        public void Test_Contamination_VariantDeclaresStableEchoIntrusionSlice()
        {
            var data = ScriptableObject.CreateInstance<ContaminationVariantData>();
            data.Configure(
                "Stage01_Overlay_Intrusion",
                "Stage01_Overlay_Echo",
                "Stage01_Overlay_Intrusion",
                45f,
                6f,
                2f,
                7f);

            Assert.That(data.ValidateData(out var error), Is.True, error);
            Assert.That(data.OverlayFor(PressureStage.Stable), Is.Empty);
            Assert.That(data.OverlayFor(PressureStage.Echo), Is.EqualTo("Stage01_Overlay_Echo"));
            Assert.That(data.OverlayFor(PressureStage.Intrusion), Is.EqualTo("Stage01_Overlay_Intrusion"));
            Assert.That(data.OverlayFor(PressureStage.Collapse), Is.Empty);
            Object.DestroyImmediate(data);
        }

        private static StageData LoadStageOne()
        {
            var data = AssetDatabase.LoadAssetAtPath<StageData>(StageOnePath);
            Assert.That(data, Is.Not.Null, $"Missing Stage 1 data at {StageOnePath}.");
            return data;
        }
    }
}
