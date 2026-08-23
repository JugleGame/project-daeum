using Daeume.Core;
using Daeume.Memory;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    /// <summary>Issue #13: Stage 03의 전용 회상 외형·동사·문자열 키를 검증한다.</summary>
    public sealed class Stage03MemoryNarrativeTests
    {
        private const string AnchorPrefabPath = "Assets/Resources/Memory/Stage03_MemoryAnchor.prefab";
        private const string StageDataPath = "Assets/Data/Stages/Stage03.asset";

        [Test]
        public void Test_Memory_Stage03AnchorIdMatchesStageData()
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(StageDataPath);
            Assert.That(stage, Is.Not.Null, $"{StageDataPath} not found.");
            Assert.That(ReadAnchorField("memoryId"), Is.EqualTo(stage.MemoryId));
            Assert.That(stage.MemoryPresentationId, Is.EqualTo("memory-presentation-stage03-club-speaker"));
        }

        [Test]
        public void Test_Memory_Stage03SubtitlesResolveToAuthoredText()
        {
            foreach (var key in new[]
                     {
                         ReadAnchorField("titleKey"), "memory.stage03.01", "memory.stage03.02", "memory.stage03.03"
                     })
            {
                AssertAuthored(key);
            }
        }

        [Test]
        public void Test_MemoryInteractable_Stage03UsesDedicatedPrompt()
        {
            var promptKey = ReadAnchorField("promptKey");
            Assert.That(promptKey, Is.EqualTo("prompt.memory.stage03"));
            Assert.That(promptKey, Is.Not.EqualTo("prompt.memory.stage01"));
            Assert.That(promptKey, Is.Not.EqualTo("prompt.memory.stage02"));
            AssertAuthored(promptKey);
        }

        [Test]
        public void Test_Memory_Stage03TextIsDistinctFromEarlierStages()
        {
            for (var line = 1; line <= 3; line++)
            {
                var stage03 = StringTable.Get($"memory.stage03.{line:00}");
                Assert.That(stage03, Is.Not.EqualTo(StringTable.Get($"memory.stage01.{line:00}")));
                Assert.That(stage03, Is.Not.EqualTo(StringTable.Get($"memory.stage02.{line:00}")));
            }
        }

        private static void AssertAuthored(string key)
        {
            Assert.That(key, Is.Not.Null.And.Not.Empty);
            Assert.That(StringTable.TryGet(key, out var text), Is.True, $"StringTable에 '{key}'가 없다.");
            Assert.That(text, Is.Not.Null.And.Not.Empty);
        }

        private static string ReadAnchorField(string fieldName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"{AnchorPrefabPath} not found.");
            var anchor = prefab.GetComponent<MemoryAnchor>();
            Assert.That(anchor, Is.Not.Null);
            var property = new SerializedObject(anchor).FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, $"MemoryAnchor.{fieldName} not found.");
            return property.stringValue;
        }
    }
}
