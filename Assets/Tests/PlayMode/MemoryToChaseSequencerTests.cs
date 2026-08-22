using System.Collections;
using System.Collections.Generic;
using Daeume.Audio;
using Daeume.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class MemoryToChaseSequencerTests
    {
        [UnityTest]
        public IEnumerator Test_Presentation_MemoryToChaseCueOrder()
        {
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            var root = new GameObject("MemoryToChaseSequencerTestRoot");
            try
            {
                root.AddComponent<GameManager>();
                root.AddComponent<MemoryToChaseSequencer>();

                var steps = new List<MemoryToChaseCueStep>();
                GameManager.Instance.Events.Subscribe<MemoryToChaseCueStepChanged>(value => steps.Add(value.Step));

                GameManager.Instance.Events.Publish(new MemoryCompleted("test-memory", string.Empty));

                // BriefSilence은 실제 시간(WaitForSeconds)을 기다려야 하므로 프레임 수가 아니라
                // 경과 시간 기준으로 대기한다. 테스트 러너는 프레임을 실시간보다 훨씬 빠르게 돌릴 수 있어
                // 고정 프레임 수만으로는 그 시간이 지났다고 보장할 수 없다.
                var deadline = Time.time + 2f;
                while (Time.time < deadline && steps.Count < 6)
                {
                    yield return null;
                }

                Assert.That(steps, Is.EqualTo(new[]
                {
                    MemoryToChaseCueStep.LastLine,
                    MemoryToChaseCueStep.BriefSilence,
                    MemoryToChaseCueStep.AmbientStop,
                    MemoryToChaseCueStep.MonsterStinger,
                    MemoryToChaseCueStep.Reveal,
                    MemoryToChaseCueStep.ChaseBgmStart
                }));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
