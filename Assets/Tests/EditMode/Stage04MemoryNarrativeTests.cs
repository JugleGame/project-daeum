using Daeume.Core;
using Daeume.Memory;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class Stage04MemoryNarrativeTests
    {
        private const string AnchorPath = "Assets/Resources/Memory/Stage04_MemoryAnchor.prefab";

        [Test]
        public void Test_MemoryInteractable_Stage04UsesDistinctPresentationAndVerb()
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage04.asset");
            Assert.That(stage.MemoryPresentationId, Is.EqualTo("memory-presentation-stage04-blanket-fort"));
            Assert.That(ReadField("memoryId"), Is.EqualTo(stage.MemoryId));
            Assert.That(ReadField("promptKey"), Is.EqualTo("prompt.memory.stage04"));
            Assert.That(ReadField("promptKey"), Is.Not.EqualTo("prompt.memory.stage03"));
            Assert.That(StringTable.TryGet(ReadField("promptKey"), out var prompt), Is.True);
            Assert.That(prompt, Is.Not.Empty);
        }

        [Test]
        public void Test_Memory_Stage04TitleIsAuthoredWithoutFinalDialogue()
        {
            var titleKey = ReadField("titleKey");
            Assert.That(titleKey, Is.EqualTo("memory.stage04.title"));
            Assert.That(StringTable.TryGet(titleKey, out var title), Is.True);
            Assert.That(title, Does.Contain("기숙사"));
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
