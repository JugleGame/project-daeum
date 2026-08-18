using System.Collections;
using Daeume.Core;
using Daeume.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class InteractionFunctionalTests
    {
        [UnityTest]
        public IEnumerator Test_Interaction_ClosestValidTargetSelected()
        {
            var actor = CreateActor(out var targeter);
            var far = CreateTarget("Far", new Vector2(1f, 0f), "B");
            var near = CreateTarget("Near", new Vector2(0.25f, 0f), "A");
            Physics2D.SyncTransforms();
            Assert.That(targeter.RefreshTarget(), Is.SameAs(near));
            Cleanup(actor, far.gameObject, near.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Interaction_PromptOnlyInRange()
        {
            var manager = new GameObject("Manager").AddComponent<GameManager>();
            var actor = CreateActor(out var targeter);
            var target = CreateTarget("Target", new Vector2(0.25f, 0f), "A");
            var visible = false;
            manager.Events.Subscribe<InteractionPromptChanged>(message => visible = message.Visible);
            Physics2D.SyncTransforms();
            targeter.RefreshTarget();
            Assert.That(visible, Is.True);
            target.transform.position = Vector2.right * 10f;
            Physics2D.SyncTransforms();
            targeter.RefreshTarget();
            Assert.That(visible, Is.False);
            Cleanup(actor, target.gameObject, manager.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Interaction_DisabledDuringMemoryOrFailure()
        {
            var manager = new GameObject("Manager").AddComponent<GameManager>();
            var actor = CreateActor(out var targeter);
            var target = CreateTarget("Target", new Vector2(0.25f, 0f), "A");
            Physics2D.SyncTransforms();
            targeter.RefreshTarget();
            manager.SetStageState(StageState.Memory);
            Assert.That(targeter.TryInteract(), Is.False);
            Assert.That(target.InvokeCount, Is.Zero);
            Cleanup(actor, target.gameObject, manager.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Interaction_InvokesOnce()
        {
            var actor = CreateActor(out var targeter);
            var target = CreateTarget("Target", new Vector2(0.25f, 0f), "A");
            Physics2D.SyncTransforms();
            targeter.RefreshTarget();
            Assert.That(targeter.TryInteract(), Is.True);
            Assert.That(target.InvokeCount, Is.EqualTo(1));
            Cleanup(actor, target.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Interaction_PromptCarriesActionAndKey()
        {
            var manager = new GameObject("Manager").AddComponent<GameManager>();
            var actor = CreateActor(out var targeter);
            var target = CreateTarget("Target", new Vector2(0.25f, 0f), "A");
            InteractionPromptChanged received = default;
            manager.Events.Subscribe<InteractionPromptChanged>(message => received = message);
            Physics2D.SyncTransforms();
            targeter.RefreshTarget();
            Assert.That(received.ActionName, Is.EqualTo("Interact"));
            Assert.That(received.StringTableKey, Is.EqualTo("prompt.memory"));
            Cleanup(actor, target.gameObject, manager.gameObject);
            yield return null;
        }

        private static GameObject CreateActor(out InteractionTargeter targeter)
        {
            var actor = new GameObject("Actor");
            targeter = actor.AddComponent<InteractionTargeter>();
            return actor;
        }

        private static FakeInteractable CreateTarget(string name, Vector2 position, string stableId)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            gameObject.AddComponent<CircleCollider2D>();
            var target = gameObject.AddComponent<FakeInteractable>();
            target.Id = stableId;
            return target;
        }

        private static void Cleanup(params GameObject[] objects)
        {
            foreach (var gameObject in objects)
            {
                Object.Destroy(gameObject);
            }
        }

        private sealed class FakeInteractable : MonoBehaviour, IInteractable
        {
            public string Id { get; set; }
            public int InvokeCount { get; private set; }
            public string StableId => Id;
            public bool CanInteract(GameObject interactor) => true;
            public InteractionPrompt GetPrompt() => new("Interact", "prompt.memory");
            public void Interact(GameObject interactor) => InvokeCount++;
        }
    }
}
