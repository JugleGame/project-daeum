using System.Linq;
using Daeume.Core;
using Daeume.Flow;
using Daeume.Interaction;
using Daeume.Player;
using Daeume.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    public sealed class RoleALayoutTests
    {
        [Test]
        public void Test_Layout_BootOwnsOnlyFlowBootstrap()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity", OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().Single(gameObject => gameObject.name == "BootRoot");
            Assert.That(root.GetComponent<BootLoader>(), Is.Not.Null);
        }

        [Test]
        public void Test_Layout_PersistentContainsRoleASystems()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Persistent.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var systems = Find(roots, "Systems");
            var player = Find(roots, "Player");
            Assert.That(systems.GetComponent<GameManager>(), Is.Not.Null);
            Assert.That(systems.GetComponent<SceneFlowController>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerInput>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerController>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerHealth>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerCombat>(), Is.Not.Null);
            Assert.That(player.GetComponent<TraumaContactHandler>(), Is.Not.Null);
            Assert.That(player.GetComponent<InteractionTargeter>(), Is.Not.Null);
        }

        [Test]
        public void Test_Layout_InputAssetContainsRequiredRemappableActions()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Persistent.unity", OpenSceneMode.Single);
            var input = Find(scene.GetRootGameObjects(), "Player").GetComponent<PlayerInput>();
            var names = input.actions.FindActionMap("Player").actions.Select(action => action.name).ToArray();
            Assert.That(names, Is.EquivalentTo(new[] { "Move", "Jump", "Attack", "Grab", "Interact", "Pause" }));
            Assert.That(input.actions.FindAction("Move").expectedControlType, Is.EqualTo("Vector2"));
        }

        [Test]
        public void Test_PrototypeScene_HasAllFeatureStations()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/RoleAPrototype.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            Assert.That(Find(roots, "Player").GetComponent<PlayerController>(), Is.Not.Null);
            Assert.That(Find(roots, "OneWayPlatform").GetComponent<PlatformEffector2D>(), Is.Not.Null);
            Assert.That(Find(roots, "GrabbableZone").GetComponent<GrabbableSurface>(), Is.Not.Null);
            Assert.That(Find(roots, "RemnantDummy").GetComponent<PrototypeRemnant>(), Is.Not.Null);
            Assert.That(Find(roots, "InteractionDummy").GetComponent<PrototypeInteractable>(), Is.Not.Null);
            Assert.That(Find(roots, "TraumaDummy").GetComponent<TraumaContactSource>(), Is.Not.Null);
            var checkpoint = Find(roots, "CheckpointMarker");
            Assert.That(checkpoint.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(checkpoint.GetComponent<PrototypeCheckpoint>(), Is.Not.Null);
            var harness = Find(roots, "PrototypeSystems").GetComponent<PrototypeHarness>();
            Assert.That(harness, Is.Not.Null);
        }

        private static GameObject Find(GameObject[] roots, string name)
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .First(transform => transform.name == name).gameObject;
        }
    }
}
