using System.Linq;
using Daeume.Core;
using Daeume.Memory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Daeume.Tests.EditMode
{
    public sealed class Stage10TextUiTests
    {
        private const string AnchorPath = "Assets/Resources/Memory/Stage10_MemoryAnchor.prefab";
        private const string PresentationPath = "Assets/Resources/UI/Stage01_Presentation.prefab";

        [Test]
        public void Test_Stage10TextUi_AllRequiredKeysResolve()
        {
            var keys = new[]
            {
                "stage.opening.stage10.01",
                "stage.opening.stage10.02",
                "hud.objective.stage10.memory",
                "prompt.memory.stage10",
                "memory.stage10.title",
                "memory.stage10.01",
                "memory.stage10.02",
                "memory.stage10.03"
            };

            foreach (var key in keys)
            {
                Assert.That(StringTable.TryGet(key, out var value), Is.True, $"StringTable에 '{key}'가 없다.");
                Assert.That(value, Is.Not.Null.And.Not.Empty, $"'{key}' 문구가 비어 있다.");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPath);
            var anchor = prefab.GetComponent<MemoryAnchor>();
            var serialized = new SerializedObject(anchor);
            Assert.That(serialized.FindProperty("promptKey").stringValue, Is.EqualTo("prompt.memory.stage10"));
            Assert.That(serialized.FindProperty("titleKey").stringValue, Is.EqualTo("memory.stage10.title"));

            var lines = serialized.FindProperty("lineKeys");
            Assert.That(Enumerable.Range(0, lines.arraySize)
                .Select(index => lines.GetArrayElementAtIndex(index).stringValue),
                Is.EqualTo(new[] { "memory.stage10.01", "memory.stage10.02", "memory.stage10.03" }));
        }

        [Test]
        public void Test_Stage10TextUi_SubtitleSizesStayInsideSafeArea()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPath);
            var instance = Object.Instantiate(prefab);

            try
            {
                var canvas = (RectTransform)instance.transform;
                canvas.localScale = Vector3.one;
                canvas.sizeDelta = new Vector2(1920f, 1080f);

                SetText(instance, "OpeningText", StringTable.Get("stage.opening.stage10.02"));
                SetText(instance, "ObjectiveText", StringTable.Get("hud.objective.stage10.memory"));
                SetText(instance, "PromptText", $"[E] {StringTable.Get("prompt.memory.stage10")}");
                SetText(instance, "TitleText", StringTable.Get("memory.stage10.title"));
                SetText(instance, "BodyText", StringTable.Get("memory.stage10.03"));

                foreach (Transform child in canvas) child.gameObject.SetActive(true);

                foreach (var tier in new[] { 0, 1, 2 })
                {
                    var scale = SubtitleScale.Resolve(tier);
                    SetFontSize(instance, "OpeningText", 32, scale);
                    SetFontSize(instance, "ObjectiveText", 22, scale);
                    SetFontSize(instance, "PromptText", 22, scale);
                    SetFontSize(instance, "TitleText", 28, scale);
                    SetFontSize(instance, "BodyText", 22, scale);
                    Canvas.ForceUpdateCanvases();

                    foreach (var name in new[] { "OpeningText", "ObjectiveText", "PromptText", "TitleText", "BodyText" })
                    {
                        AssertFits(canvas, FindText(instance, name), tier);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Text FindText(GameObject root, string name) =>
            root.GetComponentsInChildren<Text>(true).Single(value => value.name == name);

        private static void SetText(GameObject root, string name, string value) => FindText(root, name).text = value;

        private static void SetFontSize(GameObject root, string name, int baseSize, float scale) =>
            FindText(root, name).fontSize = Mathf.RoundToInt(baseSize * scale);

        private static void AssertFits(RectTransform canvas, Text text, int tier)
        {
            Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 0.1f),
                $"{text.name} clips vertically at subtitle tier {tier}.");

            var corners = new Vector3[4];
            text.rectTransform.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Assert.That(canvas.rect.Contains(canvas.InverseTransformPoint(corner)), Is.True,
                    $"{text.name} leaves the 1920x1080 safe area at subtitle tier {tier}.");
            }
        }
    }
}
