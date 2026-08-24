using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    /// <summary>Issue #9 QA 확장: 트라우마 프리팹의 콜라이더가 비균일 스케일로 뒤틀리던 버그 회귀 테스트.</summary>
    public sealed class TraumaPrefabTests
    {
        private const string PrefabPath = "Assets/Prefabs/Enemy/Stage01_Trauma.prefab";

        [Test]
        public void Test_Trauma_PrefabColliderMatchesVisualScale()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            // CircleCollider2D는 원본 오브젝트에 있다. 그 오브젝트의 스케일이 균일(1,1,1)해야
            // radius가 시각과 다르게 어느 한쪽으로 부풀지 않는다.
            var collider = prefab.GetComponent<CircleCollider2D>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));

            // 실제 크기 표현은 자식 Visual이 전담한다.
            var visual = prefab.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.localScale.x, Is.GreaterThan(1f));
            Assert.That(visual.localScale.y, Is.GreaterThan(1f));

            Assert.That(visual.gameObject.activeSelf, Is.True);
            var renderer = visual.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(renderer.sprite),
                Is.EqualTo("Assets/Art/Sprites/Trauma/TraumaBody.png"));

        }
    }
}
