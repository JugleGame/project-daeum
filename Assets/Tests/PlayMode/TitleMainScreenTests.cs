using System.Collections;
using System.Linq;
using Daeume.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Daeume.Tests.PlayMode
{
    public sealed class TitleMainScreenTests
    {
        [UnityTest]
        public IEnumerator Test_Title_MainScreenUsesFinalArtAndAccessibleMenu()
        {
            yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var scene = SceneManager.GetActiveScene();
            var background = Find(scene, "Background").GetComponent<Image>();
            var menuSafeArea = Find(scene, "MenuSafeArea").GetComponent<RectTransform>();
            var newGame = Find(scene, "NewGameButton").GetComponent<Button>();
            var continueGame = Find(scene, "ContinueButton").GetComponent<Button>();
            var settings = Find(scene, "SettingsButton").GetComponent<Button>();
            var warning = Find(scene, "ContentWarningButton").GetComponent<Button>();
            var warningPanel = Find(scene, "ContentWarningPanel");
            var warningBody = Find(scene, "ContentWarningBody").GetComponent<Text>();
            var warningClose = Find(scene, "ContentWarningCloseButton").GetComponent<Button>();

            Assert.That(background.sprite, Is.Not.Null);
            Assert.That(background.sprite.name, Is.EqualTo("MainScreen_Background"));
            Assert.That(background.preserveAspect, Is.True);
            Assert.That(menuSafeArea.anchorMax.x, Is.LessThanOrEqualTo(0.4f));

            Canvas.ForceUpdateCanvases();
            foreach (var button in new[] { newGame, continueGame, settings, warning })
                Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f), button.name);

            Assert.That(EventSystem.current, Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(newGame.gameObject));
            Assert.That(warningPanel.activeSelf, Is.False);
            Assert.That(warningBody.text, Is.EqualTo(StringTable.Get("title.content_warning.body")));

            warning.onClick.Invoke();
            yield return null;
            Assert.That(warningPanel.activeSelf, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(warningClose.gameObject));

            warningClose.onClick.Invoke();
            yield return null;
            Assert.That(warningPanel.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(warning.gameObject));
        }

        private static GameObject Find(Scene scene, string name)
        {
            var match = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == name);
            Assert.That(match, Is.Not.Null, name);
            return match.gameObject;
        }
    }
}
