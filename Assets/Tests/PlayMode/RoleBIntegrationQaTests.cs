using System.Collections;
using System.Collections.Generic;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class RoleBIntegrationQaTests
    {
        [UnityTest]
        public IEnumerator Test_RoleB_OverlayLoaderReconnectsWhenManagerAppearsAfterAwake()
        {
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            var loaderObject = new GameObject("LateManagerOverlayLoader");
            var loader = loaderObject.AddComponent<OverlaySceneLoader>();
            var managerObject = new GameObject("LateGameManager");
            var manager = managerObject.AddComponent<GameManager>();
            yield return null;

            manager.Events.Publish(new OverlaySceneLoadRequested(string.Empty, true));
            yield return null;
            Assert.That(loader.LastRequestWasLoad, Is.True);

            Object.Destroy(loaderObject);
            Object.Destroy(managerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_RoleB_DebugMemoryToEscapeCompletesStageOneFlow()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Stage01_Base"));
            yield return null;

            var manager = GameManager.Instance;
            var memory = Object.FindAnyObjectByType<MemoryCompletionAdapter>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            var chase = Object.FindAnyObjectByType<StageOneChaseController>();
            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            var steps = new List<SceneFlowStep>();
            manager.Events.Subscribe<FlowStepReached>(reached => steps.Add(reached.Step));

            Assert.That(manager.StageState, Is.EqualTo(StageState.Explore));
            memory.TriggerDebugMemoryComplete();
            Assert.That(manager.StageState, Is.EqualTo(StageState.Chase));
            Assert.That(director.ChaseActive, Is.True);
            Assert.That(GameObject.Find("Trauma"), Is.Not.Null);
            Assert.That(flow.CurrentData.CheckpointId, Is.EqualTo(StageOneChaseController.CheckpointId));
            Assert.That(flow.CurrentData.ContaminationVariantId, Is.EqualTo(director.VariantId));

            // 오버레이는 Stage01_Base 안의 루트 오브젝트다(#38). 씬이 아니라 활성 여부를 본다.
            var intrusionOverlay = OverlaySceneLoader.FindOverlayRoot("Stage01_Overlay_Intrusion");
            Assert.That(intrusionOverlay, Is.Not.Null, "Intrusion 오버레이 루트가 Stage01_Base 안에 없다.");
            for (var frame = 0; frame < 120 && !intrusionOverlay.activeSelf; frame++)
                yield return null;
            Assert.That(SceneManager.GetSceneByName("Stage01_Base").isLoaded, Is.True);
            Assert.That(intrusionOverlay.activeSelf, Is.True);

            GameObject.Find("Trauma").GetComponent<Collider2D>().enabled = false;
            Assert.That(chase.CompleteAtEscape(), Is.True);
            for (var frame = 0; frame < 180 && !SceneManager.GetSceneByName("Title").isLoaded; frame++)
                yield return null;
            for (var frame = 0; frame < 30 && manager.StageState != StageState.Explore; frame++)
                yield return null;

            Assert.That(steps, Is.EqualTo(new[]
            {
                SceneFlowStep.StageCleared,
                SceneFlowStep.StageClearPresentation,
                SceneFlowStep.Save,
                SceneFlowStep.FadeOut,
                SceneFlowStep.SceneLoad,
                SceneFlowStep.StageDataLoad,
                SceneFlowStep.Spawn,
                SceneFlowStep.FadeIn,
                SceneFlowStep.Explore
            }));
            Assert.That(SceneManager.GetSceneByName("Title").isLoaded, Is.True);
            Assert.That(manager.StageState, Is.EqualTo(StageState.Explore));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
