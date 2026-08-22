using System.Collections;
using Daeume.Audio;
using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class PressurePresentationControllerTests
    {
        [UnityTest]
        public IEnumerator Test_Presentation_ShakeDoesNotDriftOverTime()
        {
            // 실제로 겪은 버그: LateUpdate가 매 프레임 흔들림 오프셋을 그냥 더하기만 하고 지난
            // 오프셋을 빼지 않았다. StageCameraBounds가 손대지 않는 축(followVertical 꺼진 Y축)에서는
            // 아무도 리셋해 주지 않아 프레임마다 계속 쌓여, 카메라가 서서히 떠내려갔다.
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            var managerGo = new GameObject("Manager");
            managerGo.AddComponent<GameManager>();

            var cameraGo = new GameObject("Camera");
            var camera = cameraGo.AddComponent<Camera>();
            var startPosition = cameraGo.transform.localPosition;

            var controllerGo = new GameObject("PressurePresentation");
            var controller = controllerGo.AddComponent<PressurePresentationController>();
            controller.Bind(null, camera);

            try
            {
                GameManager.Instance.Events.Publish(new ContaminationPressureChanged("test-variant", PressureStage.Intrusion, string.Empty));

                for (var frame = 0; frame < 120; frame++)
                {
                    yield return new WaitForEndOfFrame();
                    var offset = (camera.transform.localPosition - startPosition).magnitude;
                    // 흔들림 최대치(기본 0.08)를 크게 넘어서면 스택형 드리프트가 재발한 것이다.
                    Assert.That(offset, Is.LessThan(0.2f), $"frame {frame}에서 카메라가 흔들림 최대치를 벗어나 떠내려갔다(offset={offset}).");
                }
            }
            finally
            {
                Object.Destroy(controllerGo);
                Object.Destroy(cameraGo);
                Object.Destroy(managerGo);
            }
        }
    }
}
