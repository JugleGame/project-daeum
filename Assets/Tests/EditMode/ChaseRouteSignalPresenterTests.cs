using Daeume.ContaminationRuntime;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class ChaseRouteSignalPresenterTests
    {
        [Test]
        public void Test_ChaseRouteSignalPresenter_ExitDoor_ShowsDoorAndSign()
        {
            var root = new GameObject("Signal");
            try
            {
                var signal = root.AddComponent<ChaseRouteSignal>();
                signal.Configure("test-exit", ChaseSignalShape.ExitDoor, "◫ EXIT", Color.yellow);

                var door = new GameObject("DoorVisual").AddComponent<SpriteRenderer>();
                door.transform.SetParent(root.transform);
                door.enabled = false;

                var sign = new GameObject("SignVisual").AddComponent<SpriteRenderer>();
                sign.transform.SetParent(root.transform);
                sign.enabled = false;

                var text = root.AddComponent<TextMesh>();
                text.text = "stale";
                text.color = Color.black;

                var presenter = root.AddComponent<ChaseRouteSignalPresenter>();
                presenter.Configure(signal, door, sign, text);

                presenter.Present();

                Assert.That(door.enabled, Is.True);
                Assert.That(sign.enabled, Is.True);
                Assert.That(sign.color, Is.EqualTo(Color.yellow));

                // 씬에 박혀 있던 TextMesh 문구·색이 ChaseRouteSignal 데이터와 따로 놀던 버그(이슈 #7)의 회귀 방지.
                Assert.That(text.text, Is.EqualTo("◫ EXIT"));
                Assert.That(text.color, Is.EqualTo(Color.yellow));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
