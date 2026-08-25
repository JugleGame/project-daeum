using System.Collections;
using Daeume.ContaminationRuntime;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class Stage13AcceptanceRuntimeTests
    {
        [UnityTest]
        public IEnumerator Test_Stage13_StartsDarkWithActiveDistantTrauma()
        {
            var playerObject = new GameObject("Player");
            playerObject.transform.position = new Vector3(0f, -0.62f, 0f);
            var player = playerObject.AddComponent<PlayerController>();
            Object.DontDestroyOnLoad(playerObject);
            var cameraObject = new GameObject("Stage13TestCamera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(-1.5f, 1.875f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            Object.DontDestroyOnLoad(cameraObject);
            yield return SceneManager.LoadSceneAsync("Stage13_Base", LoadSceneMode.Single);
            yield return null;

            var trauma = Object.FindAnyObjectByType<TraumaChaseActor>(FindObjectsInactive.Include);
            var sequence = Object.FindAnyObjectByType<AcceptanceSequence>(FindObjectsInactive.Include);

            Assert.That(player, Is.Not.Null, "Stage13 Player가 없다.");
            Assert.That(trauma, Is.Not.Null, "Stage13 Trauma가 없다.");
            Assert.That(sequence, Is.Not.Null, "Stage13 AcceptanceSequence가 없다.");
            Assert.That(trauma.gameObject.activeInHierarchy, Is.True, "Stage13 시작 시 Trauma가 비활성 상태다.");
            Assert.That(Vector2.Distance(player.transform.position, trauma.transform.position), Is.GreaterThanOrEqualTo(24f));
            Assert.That(sequence.CurrentLightIntensity, Is.EqualTo(0.55f).Within(0.01f));
            Assert.That(sequence.CurrentLightColor.r, Is.EqualTo(0.16f).Within(0.01f));
            Assert.That(sequence.CurrentLightColor.g, Is.EqualTo(0.2f).Within(0.01f));
            Assert.That(sequence.CurrentLightColor.b, Is.EqualTo(0.31f).Within(0.01f));
            Assert.That(sequence.CurrentSkyColor.r, Is.EqualTo(0.16f).Within(0.01f));
            Assert.That(sequence.CurrentSkyColor.g, Is.EqualTo(0.2f).Within(0.01f));
            Assert.That(sequence.CurrentSkyColor.b, Is.EqualTo(0.31f).Within(0.01f));
            Assert.That(SampleTopCenterPixel().grayscale, Is.LessThan(0.3f),
                "Global Light를 받지 않는 Sky 때문에 실제 화면이 밝게 남아 있다.");
            Object.Destroy(playerObject);
            Object.Destroy(cameraObject);
        }

        private static Color SampleTopCenterPixel()
        {
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            var target = new RenderTexture(64, 36, 24);
            var texture = new Texture2D(64, 36, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 64, 36), 0, 0);
            texture.Apply();
            var pixel = texture.GetPixel(32, 30);
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
            return pixel;
        }

        [UnityTest]
        public IEnumerator Test_Stage13_RuntimeRulesDisableFailureAndRequireManualFinalStep()
        {
            var player = new GameObject("Player");
            var trauma = new GameObject("Trauma");
            var combat = player.AddComponent<PlayerCombat>();
            var contact = player.AddComponent<TraumaContactHandler>();
            var sequence = new GameObject("AcceptanceSequence").AddComponent<AcceptanceSequence>();
            sequence.ConfigureForTest(player.transform, trauma.transform, combat, contact);
            yield return null;

            contact.SetContactFailureEnabled(false);
            Assert.That(contact.BeginGrab(), Is.False);
            Assert.That(sequence.TryLowerWeapon(), Is.True);
            Assert.That(combat.CombatEnabled, Is.False);
            Assert.That(sequence.CanCompleteEnding(), Is.False, "무장 해제 위치에서 자동으로 엔딩이 나면 안 된다.");

            player.transform.position += Vector3.right;
            trauma.transform.position = player.transform.position;
            Assert.That(sequence.CanCompleteEnding(), Is.True);

            Object.Destroy(player);
            Object.Destroy(trauma);
            Object.Destroy(sequence.gameObject);
        }

        [UnityTest]
        public IEnumerator Test_Ending_AttackCannotResolveTrauma()
        {
            var player = new GameObject("Player");
            var trauma = new GameObject("Trauma");
            trauma.AddComponent<TraumaContactSource>();
            trauma.AddComponent<TraumaChaseActor>();
            var combat = player.AddComponent<PlayerCombat>();
            yield return null;

            Assert.That(combat.Attack(), Is.Zero);
            Object.Destroy(player);
            Object.Destroy(trauma);
        }

        [UnityTest]
        public IEnumerator Test_Ending_TraumaContactDoesNotFailStageThirteen()
        {
            var player = new GameObject("Player");
            var contact = player.AddComponent<TraumaContactHandler>();
            yield return null;
            contact.SetContactFailureEnabled(false);
            Assert.That(contact.BeginGrab(), Is.False);
            Assert.That(contact.GrabInProgress, Is.False);
            Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator Test_Ending_PlayerLowersWeaponAndWalks()
        {
            var player = new GameObject("Player");
            var trauma = new GameObject("Trauma");
            var combat = player.AddComponent<PlayerCombat>();
            var contact = player.AddComponent<TraumaContactHandler>();
            var sequence = new GameObject("AcceptanceSequence").AddComponent<AcceptanceSequence>();
            sequence.ConfigureForTest(player.transform, trauma.transform, combat, contact);
            yield return null;

            Assert.That(sequence.TryLowerWeapon(), Is.True);
            Assert.That(sequence.CanCompleteEnding(), Is.False);
            player.transform.position += Vector3.right;
            trauma.transform.position = player.transform.position;
            Assert.That(sequence.CanCompleteEnding(), Is.True);

            Object.Destroy(player);
            Object.Destroy(trauma);
            Object.Destroy(sequence.gameObject);
        }

        [UnityTest]
        public IEnumerator Test_Ending_CompletesAfterFarewell()
        {
            var sequence = new GameObject("AcceptanceSequence").AddComponent<AcceptanceSequence>();
            yield return null;
            Assert.That(sequence.FarewellKey, Is.EqualTo("ending.farewell"));
            Assert.That(sequence.CreditKey, Is.EqualTo("ending.credit"));
            Object.Destroy(sequence.gameObject);
        }

        [UnityTest]
        public IEnumerator Test_Ending_TraumaReappearsFromRightAfterLoopDelay()
        {
            var player = new GameObject("Player");
            var playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.gravityScale = 0f;
            var trauma = new GameObject("Trauma");
            var director = new GameObject("ContaminationDirector").AddComponent<ContaminationDirector>();
            var sequence = new GameObject("AcceptanceSequence").AddComponent<AcceptanceSequence>();
            sequence.ConfigureForTest(player.transform, trauma.transform, null, null, director, 0.2f);

            player.transform.position = new Vector3(-9f, 0f, 0f);
            playerBody.position = player.transform.position;
            playerBody.linearVelocity = Vector2.left;
            yield return null;

            Assert.That(player.transform.position.x, Is.EqualTo(28f).Within(0.01f));
            Assert.That(sequence.TraumaLoopRespawning, Is.True);
            Assert.That(trauma.activeSelf, Is.False);
            Assert.That(director.MovementSuppressed, Is.True);

            yield return new WaitForSeconds(0.05f);
            Assert.That(trauma.activeSelf, Is.False, "재등장 지연이 끝나기 전에 Trauma가 나타났다.");

            yield return new WaitForSeconds(0.2f);
            Assert.That(sequence.TraumaLoopRespawning, Is.False);
            Assert.That(trauma.activeSelf, Is.True);
            Assert.That(trauma.transform.position.x, Is.EqualTo(31f).Within(0.01f));
            Assert.That(director.MovementSuppressed, Is.False);

            Object.Destroy(player);
            Object.Destroy(trauma);
            Object.Destroy(director.gameObject);
            Object.Destroy(sequence.gameObject);
        }
    }
}
