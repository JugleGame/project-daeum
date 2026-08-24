using Daeume.Core;
using Daeume.Memory;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class Stage05MemoryNarrativeTests
    {
        private const string AnchorPath = "Assets/Resources/Memory/Stage05_MemoryAnchor.prefab";

        [Test]
        public void Test_MemoryInteractable_Stage05UsesDistinctPresentationAndVerb()
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage05.asset");
            Assert.That(stage.MemoryPresentationId, Is.EqualTo("memory-presentation-stage05-project-board"));
            Assert.That(ReadField("memoryId"), Is.EqualTo(stage.MemoryId));
            Assert.That(ReadField("promptKey"), Is.EqualTo("prompt.memory.stage05"));
            Assert.That(ReadField("promptKey"), Is.Not.EqualTo("prompt.memory.stage04"));
            Assert.That(StringTable.TryGet(ReadField("promptKey"), out var prompt), Is.True);
            Assert.That(prompt, Does.Contain("프로젝트 보드"));
        }

        [Test]
        public void Test_Memory_Stage05UsesMinimalProjectRoomCaptions()
        {
            Assert.That(ReadField("titleKey"), Is.EqualTo("memory.stage05.title"));
            Assert.That(StringTable.TryGet("memory.stage05.title", out var title), Is.True);
            Assert.That(title, Does.Contain("프로젝트실"));
            foreach (var key in new[] { "memory.stage05.01", "memory.stage05.02", "memory.stage05.03" })
            {
                Assert.That(StringTable.TryGet(key, out var caption), Is.True);
                Assert.That(caption, Is.Not.Empty);
            }
        }

        private static string ReadField(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPath);
            Assert.That(prefab, Is.Not.Null, $"{AnchorPath} not found.");
            var property = new SerializedObject(prefab.GetComponent<MemoryAnchor>()).FindProperty(name);
            Assert.That(property, Is.Not.Null);
            return property.stringValue;
        }
    }
}
