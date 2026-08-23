using Daeume.Core;
using Daeume.Memory;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    /// <summary>
    /// 이슈 #12: 저작한 Stage 2 서사가 실제로 화면까지 도달하는지 확인한다.
    ///
    /// StageProgressionTests는 StageData(에셋)만 검사한다. 화면에 나오는 자막은
    /// MemoryAnchor 프리팹의 문자열 키 → StringTable 경로에서 나오므로, StageData만 채워도
    /// 인게임 텍스트는 전혀 바뀌지 않는다(#11에서 실제로 그랬다). 여기서는 그 경로를 검사한다.
    /// </summary>
    public sealed class Stage02MemoryNarrativeTests
    {
        private const string AnchorPrefabPath = "Assets/Resources/Memory/Stage02_MemoryAnchor.prefab";
        private const string StageDataPath = "Assets/Data/Stages/Stage02.asset";

        [Test]
        public void Test_Memory_Stage02AnchorIdMatchesStageData()
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(StageDataPath);
            Assert.That(stage, Is.Not.Null, $"{StageDataPath} not found.");

            Assert.That(ReadAnchorField("memoryId"), Is.EqualTo(stage.MemoryId),
                "MemoryAnchor 프리팹의 memoryId가 StageData.MemoryId와 다르면 저장·조각 지급이 서로 다른 ID로 갈린다.");
        }

        [Test]
        public void Test_Memory_Stage02SubtitlesResolveToAuthoredText()
        {
            foreach (var key in new[] { ReadAnchorField("titleKey"), "memory.stage02.01", "memory.stage02.02", "memory.stage02.03" })
            {
                AssertAuthored(key);
            }
        }

        [Test]
        public void Test_MemoryInteractable_Stage02UsesDedicatedPrompt()
        {
            var promptKey = ReadAnchorField("promptKey");

            // spec-005: Stage마다 고유한 InteractionVerb를 쓴다. 공용 prompt.memory에 머물면 안 되고,
            // Stage 1의 동사를 그대로 재사용해도 안 된다.
            Assert.That(promptKey, Is.Not.EqualTo("prompt.memory"));
            Assert.That(promptKey, Is.Not.EqualTo("prompt.memory.stage01"));
            AssertAuthored(promptKey);
        }

        /// <summary>Stage 1과 Stage 2가 같은 문장을 돌려쓰고 있지 않은지 확인한다.</summary>
        [Test]
        public void Test_Memory_Stage02TextIsDistinctFromStage01()
        {
            for (var line = 1; line <= 3; line++)
            {
                Assert.That(StringTable.Get($"memory.stage02.{line:00}"),
                    Is.Not.EqualTo(StringTable.Get($"memory.stage01.{line:00}")));
            }
        }

        /// <summary>키가 문자열 테이블에 실제로 저작되어 있는지 확인한다(빈 문자열·"[키]" 폴백 모두 실패).</summary>
        private static void AssertAuthored(string key)
        {
            Assert.That(key, Is.Not.Null.And.Not.Empty, "문자열 키가 비어 있다.");
            Assert.That(StringTable.TryGet(key, out var text), Is.True, $"StringTable에 '{key}'가 없다.");
            Assert.That(text, Is.Not.Null.And.Not.Empty, $"'{key}'의 문장이 비어 있어 화면에 아무것도 나오지 않는다.");
        }

        /// <summary>
        /// MemoryAnchor의 저작 필드는 private [SerializeField]다. 프리팹이 실제로 들고 있는 값을
        /// 봐야 의미가 있으므로 SerializedObject로 직렬화된 값을 그대로 읽는다.
        /// </summary>
        private static string ReadAnchorField(string fieldName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"{AnchorPrefabPath} not found.");

            var anchor = prefab.GetComponent<MemoryAnchor>();
            Assert.That(anchor, Is.Not.Null, "MemoryAnchor component missing on the Stage 2 anchor prefab.");

            var property = new SerializedObject(anchor).FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, $"MemoryAnchor.{fieldName} not found.");
            return property.stringValue;
        }
    }
}
