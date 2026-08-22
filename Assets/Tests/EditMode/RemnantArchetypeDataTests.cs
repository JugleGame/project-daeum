using System;
using System.Linq;
using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    /// <summary>Issue #9: 돌진형/원거리형 archetype, VisualTraitTags, 압박 단계 데이터 검사.</summary>
    public sealed class RemnantArchetypeDataTests
    {
        [Test]
        public void Test_Remnant_StageElevenAllContainProtagonistTrait()
        {
            RemnantDataBase[] data =
            {
                ScriptableObject.CreateInstance<MeleeRemnantData>(),
                ScriptableObject.CreateInstance<DashRemnantData>(),
                ScriptableObject.CreateInstance<RangedRemnantData>()
            };

            try
            {
                foreach (var instance in data)
                {
                    Configure(instance, stageNumber: 11, tags: VisualTraitTag.HumanoidSilhouette);
                    Assert.That(instance.ValidateData().Any(e => e.Contains("protagonist trait")),
                        Is.True, instance.GetType().Name);

                    Configure(instance, stageNumber: 11, tags: VisualTraitTag.HumanoidSilhouette | VisualTraitTag.ProtagonistHand);
                    Assert.That(instance.ValidateData().Any(e => e.Contains("protagonist trait")),
                        Is.False, instance.GetType().Name);
                }
            }
            finally
            {
                foreach (var instance in data) UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Test_Remnant_PressureChangesDeclaredValuesOnly()
        {
            Assert.That(Enum.GetValues(typeof(RemnantArchetype)).Length, Is.EqualTo(3));

            var melee = ScriptableObject.CreateInstance<MeleeRemnantData>();
            var dash = ScriptableObject.CreateInstance<DashRemnantData>();
            var ranged = ScriptableObject.CreateInstance<RangedRemnantData>();

            try
            {
                RemnantDataBase[] data = { melee, dash, ranged };
                var archetypes = data.Select(d => d.Archetype).Distinct().ToArray();
                Assert.That(archetypes, Is.EquivalentTo(new[] { RemnantArchetype.Melee, RemnantArchetype.Dash, RemnantArchetype.Ranged }));

                foreach (var instance in data)
                {
                    var stable = instance.GetProfile(PressureStage.Stable);
                    var intrusion = instance.GetProfile(PressureStage.Intrusion);

                    // 압박이 올라도 archetype 자체(3종)는 그대로이고, 선언된 수치만 달라져야 한다.
                    Assert.That(data.Select(d => d.Archetype).Distinct().Count(), Is.EqualTo(3), instance.GetType().Name);
                    Assert.That(intrusion.TelegraphMultiplier, Is.Not.EqualTo(stable.TelegraphMultiplier), instance.GetType().Name);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(melee);
                UnityEngine.Object.DestroyImmediate(dash);
                UnityEngine.Object.DestroyImmediate(ranged);
            }
        }

        [Test]
        public void Test_Remnant_TelegraphNeverReachesZero()
        {
            RemnantDataBase[] data =
            {
                ScriptableObject.CreateInstance<MeleeRemnantData>(),
                ScriptableObject.CreateInstance<DashRemnantData>(),
                ScriptableObject.CreateInstance<RangedRemnantData>()
            };

            try
            {
                foreach (var instance in data)
                {
                    Assert.That(instance.ValidateData(), Is.Empty, instance.GetType().Name);
                    foreach (var stage in new[] { PressureStage.Stable, PressureStage.Echo, PressureStage.Intrusion })
                    {
                        var telegraph = instance.AttackTelegraphSeconds * instance.GetProfile(stage).TelegraphMultiplier;
                        Assert.That(telegraph, Is.GreaterThan(0f), $"{instance.GetType().Name}/{stage}");
                        Assert.That(telegraph, Is.GreaterThanOrEqualTo(0.05f), $"{instance.GetType().Name}/{stage}");
                    }
                }
            }
            finally
            {
                foreach (var instance in data) UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Test_Contamination_FourPressureStagesDeclared()
        {
            Assert.That(Enum.GetValues(typeof(PressureStage)).Length, Is.EqualTo(4));
            Assert.That(Enum.GetNames(typeof(PressureStage)),
                Is.EquivalentTo(new[] { "Stable", "Echo", "Intrusion", "Collapse" }));

            var variantData = ScriptableObject.CreateInstance<ContaminationVariantData>();
            variantData.Configure("variant-test", "Overlay_Echo", "Overlay_Intrusion", 10f, 5f, 2f, 6f);
            var root = new GameObject("FourStageTestRoot");
            var player = new GameObject("Player").transform;
            player.SetParent(root.transform);
            var pursuer = new GameObject("Trauma").transform;
            pursuer.SetParent(root.transform);
            var director = root.AddComponent<ContaminationDirector>();
            director.Configure(variantData, player, pursuer);

            try
            {
                // 4단계 모두 director가 유효한 Variant 참조로 실제 전환 가능해야 한다(더는 Collapse만 거절하지 않는다).
                Assert.That(director.SetPressure(PressureStage.Stable), Is.True);
                Assert.That(director.SetPressure(PressureStage.Echo), Is.True);
                Assert.That(director.SetPressure(PressureStage.Intrusion), Is.True);
                Assert.That(director.SetPressure(PressureStage.Collapse), Is.True);
                Assert.That(director.Pressure, Is.EqualTo(PressureStage.Collapse));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(variantData);
            }
        }

        private static void Configure(RemnantDataBase instance, int stageNumber, VisualTraitTag tags) =>
            instance.SetStageIdentity(stageNumber, tags);
    }
}
