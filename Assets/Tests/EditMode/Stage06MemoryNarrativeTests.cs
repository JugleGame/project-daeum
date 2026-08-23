using Daeume.Core;
using Daeume.Memory;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class Stage06MemoryNarrativeTests
    {
        private const string AnchorPath = "Assets/Resources/Memory/Stage06_MemoryAnchor.prefab";

        [Test]
        public void Test_MemoryInteractable_Stage06UsesDistinctPresentationAndVerb()
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage06.asset");
            Assert.That(stage.MemoryPresentationId, Is.EqualTo("memory-presentation-stage06-shopping-bag"));
            Assert.That(ReadField("memoryId"), Is.EqualTo(stage.MemoryId));
            Assert.That(ReadField("promptKey"), Is.EqualTo("prompt.memory.stage06"));
            Assert.That(ReadField("promptKey"), Is.Not.EqualTo("prompt.memory.stage05"));
            Assert.That(StringTable.TryGet(ReadField("promptKey"), out var prompt), Is.True);
            Assert.That(prompt, Does.Contain("쇼핑백"));
        }

        [Test]
        public void Test_Memory_Stage06UsesMinimalStreetShopCaptions()
        {
            Assert.That(ReadField("titleKey"), Is.EqualTo("memory.stage06.title"));
            Assert.That(StringTable.TryGet("memory.stage06.title", out var title), Is.True);
            Assert.That(title, Does.Contain("거리"));
            foreach (var key in new[] { "memory.stage06.01", "memory.stage06.02", "memory.stage06.03" })
            {
                Assert.That(StringTable.TryGet(key, out var caption), Is.True);
                Assert.That(caption, Is.Not.Empty);
            }
        }

        private static string ReadField(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPath);
            Assert.That(prefab, Is.Not.Null, AnchorPath + " not found.");
            var property = new SerializedObject(prefab.GetComponent<MemoryAnchor>()).FindProperty(name);
            Assert.That(property, Is.Not.Null);
            return property.stringValue;
        }
    }
}