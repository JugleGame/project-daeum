using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    /// <summary>Issue #53: 재사용 가능한 원거리 배경 프리팹의 임포트 및 배치 규격 회귀 테스트.</summary>
    public sealed class BackgroundPropPrefabTests
    {
        private readonly struct BackgroundPropSpec
        {
            public BackgroundPropSpec(string group, string name)
            {
                Group = group;
                Name = name;
            }

            public string Group { get; }
            public string Name { get; }

            public string SpritePath =>
                $"Assets/Art/Sprites/Stage01/BackgroundProps/{Group}/{Name}.png";

            public string PrefabPath =>
                $"Assets/Prefabs/Stage/Stage01/BackgroundProps/{Group}/{Name}.prefab";
        }

        private static readonly BackgroundPropSpec[] Specs =
        {
            new BackgroundPropSpec("Houses", "house-small-single"),
            new BackgroundPropSpec("Houses", "house-narrow-two-story"),
            new BackgroundPropSpec("Houses", "house-wide-low"),
            new BackgroundPropSpec("Houses", "house-distant-cluster"),
            new BackgroundPropSpec("Trees", "tree-small-round"),
            new BackgroundPropSpec("Trees", "tree-medium-wide"),
            new BackgroundPropSpec("Trees", "tree-tall-narrow"),
            new BackgroundPropSpec("Trees", "tree-asymmetric"),
            new BackgroundPropSpec("Trees", "tree-distant-cluster"),
        };

        [Test]
        public void Test_BackgroundProps_ImportedAndPrefabbedForFarLayer()
        {
            foreach (var spec in Specs)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.SpritePath);
                Assert.That(sprite, Is.Not.Null, spec.SpritePath);

                var importer = AssetImporter.GetAtPath(spec.SpritePath) as TextureImporter;
                Assert.That(importer, Is.Not.Null, spec.SpritePath);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), spec.SpritePath);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), spec.SpritePath);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), spec.SpritePath);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), spec.SpritePath);
                Assert.That(importer.mipmapEnabled, Is.False, spec.SpritePath);
                Assert.That(importer.alphaIsTransparency, Is.True, spec.SpritePath);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
                Assert.That(prefab, Is.Not.Null, spec.PrefabPath);
                Assert.That(prefab.GetComponentsInChildren<Collider2D>(true), Is.Empty, spec.PrefabPath);

                var visual = prefab.transform.Find("Visual");
                Assert.That(visual, Is.Not.Null, spec.PrefabPath);

                var renderer = visual.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null, spec.PrefabPath);
                Assert.That(renderer.sprite, Is.SameAs(sprite), spec.PrefabPath);
                Assert.That(renderer.sortingLayerName, Is.EqualTo("Far"), spec.PrefabPath);
                Assert.That(renderer.sortingOrder, Is.Zero, spec.PrefabPath);
                Assert.That(renderer.sharedMaterial, Is.Not.Null, spec.PrefabPath);

                // 배경 소품도 스테이지 조명을 받아야 한다. Unlit이면 Global Light 2D가 만든
                // 저녁 톤을 무시하고 혼자 밝게 떠서 앞쪽 소품·타일맵과 따로 논다.
                Assert.That(renderer.sharedMaterial.name, Is.EqualTo("Stage01_Map_SpriteLit"), spec.PrefabPath);

                Assert.That(visual.localPosition.x, Is.EqualTo(0f).Within(0.0001f), spec.PrefabPath);
                Assert.That(visual.localPosition.y, Is.EqualTo(sprite.bounds.extents.y).Within(0.0001f), spec.PrefabPath);
                Assert.That(visual.localPosition.z, Is.EqualTo(0f).Within(0.0001f), spec.PrefabPath);
                Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), spec.PrefabPath);
            }
        }
    }
}
