using System.IO;
using System.Linq;
using Daeume.Contamination;
using NUnit.Framework;
using UnityEditor;

namespace Daeume.Tests.EditMode
{
    /// <summary>
    /// 오버레이는 StageNN_Base 안의 루트 오브젝트로만 저작한다(#38). 그 규칙을 스테이지 수와 무관하게 지킨다.
    /// </summary>
    /// <remarks>
    /// 왜 테스트로 막는가: 스테이지 13개 × 씬 3개 = 39씬이 되면 적재/해제 비용이 그만큼 늘고,
    /// 오버레이 좌표를 기저 지형을 못 보는 채로 맞춰야 한다(Stage 02를 실제로 눈감고 배치했다).
    /// 문서로만 알리면 다음 저작자가 또 씬을 만든다. 여기서 이름 하나로 걸러 낸다.
    ///
    /// 에셋을 훑는 방식이라 스테이지가 늘어도 이 테스트는 고칠 필요가 없다.
    /// </remarks>
    public sealed class ContaminationOverlayGuardTests
    {
        [Test]
        public void Test_Contamination_OverlayNamesAreNotSceneNames()
        {
            var variants = AssetDatabase.FindAssets($"t:{nameof(ContaminationVariantData)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ContaminationVariantData>)
                .Where(variant => variant != null)
                .ToArray();

            // 에셋을 하나도 못 찾으면 테스트가 조용히 아무것도 검사하지 않는다. 그 상태를 실패로 만든다.
            Assert.That(variants, Is.Not.Empty, "ContaminationVariantData 에셋을 하나도 찾지 못했다.");

            var buildSceneNames = EditorBuildSettings.scenes
                .Select(entry => Path.GetFileNameWithoutExtension(entry.path))
                .ToArray();

            foreach (var variant in variants)
            {
                foreach (var overlayName in new[] { variant.EchoOverlayName, variant.IntrusionOverlayName })
                {
                    Assert.That(buildSceneNames, Has.None.EqualTo(overlayName),
                        $"'{variant.name}'의 오버레이 '{overlayName}'과 같은 이름의 씬이 빌드 설정에 등록돼 있다. " +
                        "오버레이는 StageNN_Base 안의 루트 오브젝트로 저작한다.");
                }
            }
        }

        [Test]
        public void Test_Contamination_NoOverlaySceneFilesExist()
        {
            // 빌드 설정에서 빼고 파일만 남겨 두는 경우도 막는다 — 남아 있으면 다음 저작자가 그것을 본보기로 삼는다.
            var overlayScenes = AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path).Contains("_Overlay_"))
                .ToArray();

            Assert.That(overlayScenes, Is.Empty,
                $"오버레이 씬 파일이 남아 있다: {string.Join(", ", overlayScenes)}");
        }
    }
}
