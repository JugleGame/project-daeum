using System.Collections;
using NUnit.Framework;
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
    }
}
