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

        [UnityTest]
        public IEnumerator Test_Presentation_ExploreShakeSettlesAfterEncounterCleared()
        {
            // 실제로 겪은 버그(#38): 근접 잔재 하나를 처치하면 추격도 아닌데 압박이 Echo(0.35)로 오르고,
            // 탐색 중에 되돌리는 경로가 없어 남은 탐색 내내 최대 흔들림의 35%로 화면이 흔들렸다.
            // 압박 자체는 spec-006 의도라 그대로 두고, 흔들림만 잦아드는지 여기서 고정한다.
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            var managerGo = new GameObject("Manager");
            managerGo.AddComponent<GameManager>();

            var cameraGo = new GameObject("Camera");
            var camera = cameraGo.AddComponent<Camera>();

            var controllerGo = new GameObject("PressurePresentation");
            var controller = controllerGo.AddComponent<PressurePresentationController>();
            controller.Bind(null, camera);

            try
            {
                GameManager.Instance.Events.Publish(new ContaminationPressureChanged("test-variant", PressureStage.Echo, string.Empty));
                yield return new WaitForEndOfFrame();
                Assert.That(controller.ShakeAmount, Is.GreaterThan(0f), "전환 순간에는 흔들려야 압박이 전달된다.");

                // 감쇠 시간(1.5초)보다 넉넉히 기다린다.
                yield return new WaitForSeconds(2.5f);

                Assert.That(controller.ShakeAmount, Is.EqualTo(0f),
                    "탐색 상태(Echo)에서 흔들림이 잦아들지 않으면 남은 탐색 내내 화면이 흔들린다.");
                Assert.That(controller.PressureAmount, Is.GreaterThan(0f),
                    "압박 자체는 유지된다 — 잦아드는 것은 흔들림뿐이다.");

                // 실제로 카메라가 멈췄는지도 본다. ShakeAmount만 보면 적용 경로가 끊겨도 통과한다.
                yield return new WaitForEndOfFrame();
                var settled = camera.transform.localPosition;
                for (var frame = 0; frame < 30; frame++)
                {
                    yield return new WaitForEndOfFrame();
                    Assert.That(camera.transform.localPosition, Is.EqualTo(settled),
                        $"frame {frame}에서 카메라가 아직 흔들리고 있다.");
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
