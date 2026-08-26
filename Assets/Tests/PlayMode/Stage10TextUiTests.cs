using System.Collections;
using System.Linq;
using Daeume.Core;
using Daeume.Flow;
using Daeume.Memory;
using Daeume.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Daeume.Tests.PlayMode
{
    public sealed class Stage10TextUiTests
    {
        [UnityTest]
        public IEnumerator Test_Stage10TextUi_ShowsOpeningObjectiveAndMemoryInOrder()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            flow.CurrentData.CurrentStageId = 10;
            yield return SceneManager.LoadSceneAsync("Stage10_Base", LoadSceneMode.Additive);
            yield return null;

            var hud = Object.FindAnyObjectByType<StageHudPresenter>();
            Assert.That(hud, Is.Not.Null);
            GameManager.Instance.ResetStage();
            yield return null;

            Assert.That(hud.ObjectiveLabel, Does.Contain(StringTable.Get("hud.objective.stage10.memory")));
            Assert.That(hud.ObjectiveLabel, Does.Not.Contain(StringTable.Get("hud.objective.memory")));

            var openingText = FindSceneText("OpeningText");
            Assert.That(openingText.text, Is.EqualTo(StringTable.Get("stage.opening.stage10.01")));
            yield return new WaitForSeconds(4f);
            Assert.That(openingText.text, Is.EqualTo(StringTable.Get("stage.opening.stage10.02")));
            yield return new WaitForSeconds(4f);

            GameManager.Instance.ResetStage();
            yield return null;
            Assert.That(openingText.transform.parent.gameObject.activeSelf, Is.False,
                "Stage state reset must not replay the Stage10 opening monologue.");

            var anchor = Object.FindAnyObjectByType<MemoryAnchor>();
            var panel = Object.FindAnyObjectByType<MemoryPanelPresenter>();
            Assert.That(anchor.Begin(), Is.True);
            yield return null;
            Assert.That(panel.Title, Is.EqualTo(StringTable.Get("memory.stage10.title")));
            Assert.That(panel.Body, Is.EqualTo(StringTable.Get("memory.stage10.01")));

            Assert.That(anchor.Advance(), Is.True);
            yield return null;
            Assert.That(panel.Body, Is.EqualTo(StringTable.Get("memory.stage10.02")));

            Assert.That(anchor.Advance(), Is.True);
            yield return null;
            Assert.That(panel.Body, Is.EqualTo(StringTable.Get("memory.stage10.03")));
            LogAssert.NoUnexpectedReceived();
        }

        private static Text FindSceneText(string name) => Resources.FindObjectsOfTypeAll<Text>()
            .Single(value => value.gameObject.scene.IsValid() && value.name == name);
    }
}
