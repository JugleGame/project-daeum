using System.Collections;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class StageThirteenAcceptanceRulesTests
    {
        [UnityTest]
        public IEnumerator Test_Ending_AttackAndTraumaContactAreDisabledDuringAcceptance()
        {
            var player = new GameObject("Stage13Player");
            var combat = player.AddComponent<PlayerCombat>();
            var contact = player.AddComponent<TraumaContactHandler>();
            yield return null;

            combat.SetCombatEnabled(false);
            contact.SetContactFailureEnabled(false);

            Assert.That(combat.Attack(), Is.EqualTo(0));
            Assert.That(contact.BeginGrab(), Is.False);

            Object.Destroy(player);
        }
    }
}
