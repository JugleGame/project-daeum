using System.Collections;
using NUnit.Framework;
using Daeume.Player;
using Daeume.Prototype;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class SceneSmokeTests
    {
        [UnityTest]
        public IEnumerator Test_Runtime_BootPersistentTitle_NoConsoleErrors()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            const int frameLimit = 180;
            for (var frame = 0; frame < frameLimit; frame++)
            {
                if (SceneManager.GetSceneByName("Persistent").isLoaded &&
                    SceneManager.GetSceneByName("Title").isLoaded)
                {
                    LogAssert.NoUnexpectedReceived();
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Boot did not load Persistent and Title within 180 frames.");
        }

        [UnityTest]
        public IEnumerator Test_PrototypeScene_LoadsWithoutConsoleErrors()
        {
            SceneManager.LoadScene("RoleAPrototype", LoadSceneMode.Single);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<PrototypeHarness>(), Is.Not.Null);
            Assert.That(GameObject.Find("RemnantDummy"), Is.Not.Null);
            Assert.That(GameObject.Find("InteractionDummy"), Is.Not.Null);
            Assert.That(GameObject.Find("TraumaDummy"), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Test_PrototypePlayer_LandsOnGround()
        {
            SceneManager.LoadScene("RoleAPrototype", LoadSceneMode.Single);
            yield return null;

            var player = GameObject.Find("Player");
            var controller = player.GetComponent<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            for (var frame = 0; frame < 30; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.transform.position.y, Is.GreaterThan(-0.8f));
            Assert.That(Mathf.Abs(body.linearVelocity.y), Is.LessThan(0.1f));
            Assert.That(controller.IsGrounded, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
