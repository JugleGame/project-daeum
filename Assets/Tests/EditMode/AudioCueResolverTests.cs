using Daeume.Audio;
using Daeume.Core;
using NUnit.Framework;

namespace Daeume.Tests.EditMode
{
    public sealed class AudioCueResolverTests
    {
        [Test]
        public void Test_AudioCue_ExploreMapsToAmbientOrCombat()
        {
            Assert.That(AudioCueResolver.Resolve(StageState.Explore, false), Is.EqualTo(AudioCueId.ExploreAmbient));
            Assert.That(AudioCueResolver.Resolve(StageState.Explore, true), Is.EqualTo(AudioCueId.EncounterCombat));
        }

        [Test]
        public void Test_AudioCue_MemoryChaseClearedMapDirectly()
        {
            Assert.That(AudioCueResolver.Resolve(StageState.Memory, false), Is.EqualTo(AudioCueId.Memory));
            Assert.That(AudioCueResolver.Resolve(StageState.Chase, false), Is.EqualTo(AudioCueId.Chase));
            Assert.That(AudioCueResolver.Resolve(StageState.Cleared, false), Is.EqualTo(AudioCueId.Cleared));
        }

        [Test]
        public void Test_AudioCue_FailedHasNoCue()
        {
            Assert.That(AudioCueResolver.Resolve(StageState.Failed, false), Is.Null);
        }
    }
}
