using System.Linq;
using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Encounter;
using Daeume.Player;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    /// <summary>
    /// 이슈 #12: Stage 02 블록아웃 씬이 저작 규약대로 서 있는지 검사한다.
    ///
    /// 씬은 손으로 열어 보지 않으면 깨진 것을 알기 어렵고, 마커 ID 하나가 어긋나면
    /// 회상 앵커나 전투가 조용히 생성되지 않는다(증상이 "아무 일도 안 일어남"이라 추적이 오래 걸린다).
    /// </summary>
    public sealed class Stage02LayoutTests
    {
        private const string ScenePath = "Assets/Scenes/Stage02_Base.unity";

        [Test]
        public void Test_Stage02_ContainsDataCameraBoundsAndUniqueRequiredMarkers()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Find(scene, "Stage02BaseRoot");
            var definition = root.GetComponent<StageDefinition>();
            var bounds = root.GetComponent<StageCameraBounds>();
            var markers = root.GetComponentsInChildren<StageMarker>(true);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Data, Is.Not.Null);
            Assert.That(definition.Data.StageId, Is.EqualTo(2));
            Assert.That(bounds, Is.Not.Null);
            Assert.That(bounds.Minimum.x, Is.LessThan(bounds.Maximum.x));

            Assert.That(markers.Select(marker => marker.MarkerId), Has.None.Empty);
            Assert.That(markers.Select(marker => marker.MarkerId).Distinct().Count(), Is.EqualTo(markers.Length));

            // 마커 ID는 stage02.* 네임스페이스를 쓴다. Stage 1 것을 그대로 복사해 두면
            // StagePresentationBootstrap이 회상 앵커를 엉뚱한 지점에 놓는다.
            Assert.That(markers.Select(marker => marker.MarkerId),
                Is.All.Matches<string>(id => id.StartsWith("stage02.")));

            var kinds = markers.Select(marker => marker.Kind).ToArray();
            foreach (var required in new[]
                     {
                         StageMarkerKind.Start, StageMarkerKind.FallRecovery, StageMarkerKind.EncounterTrigger,
                         StageMarkerKind.EncounterExit, StageMarkerKind.MemoryAnchor, StageMarkerKind.ChaseStart,
                         StageMarkerKind.Escape
                     })
            {
                Assert.That(kinds, Does.Contain(required), $"Stage02 is missing a {required} marker.");
            }

            // 교실·복도·계단 3구간 전투를 위해 트리거 3개와 잔재 스폰 6개가 필요하다.
            Assert.That(kinds.Count(kind => kind == StageMarkerKind.EncounterTrigger), Is.EqualTo(3));
            Assert.That(kinds.Count(kind => kind == StageMarkerKind.RemnantSpawn), Is.EqualTo(6));
        }

        [Test]
        public void Test_Stage02_TeachesMeleeCombatAcrossClassroomCorridorAndStairs()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controllers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EncounterController>(true))
                .ToArray();

            Assert.That(controllers.Length, Is.EqualTo(3), "Stage02는 전투 구간 3개(교실·복도·계단)로 저작된다.");
            Assert.That(controllers.Select(controller => controller.Data.EncounterId).OrderBy(id => id),
                Is.EqualTo(new[] { "stage02.encounter.01", "stage02.encounter.02", "stage02.encounter.03" }));

            foreach (var controller in controllers)
            {
                var data = controller.Data;

                // spec-004 슬라이스 범위: Stage 2는 근접형 1종만 쓴다(변주는 Stage 3부터).
                Assert.That(data.EnemyType, Is.EqualTo(EncounterEnemyType.MeleeRemnant));
                Assert.That(data.ClearCondition, Is.EqualTo(EncounterClearCondition.DefeatAll));

                // 첫 실전 전투라 회피 선택지를 주지 않는다 — 출구를 잠가 순수 습득 구간으로 만든다.
                Assert.That(data.LockExit, Is.True, $"{data.EncounterId} must lock its exit.");

                // 스폰 지점이 데이터 선언과 씬 배치 양쪽에 다 있어야 실제로 적이 나온다.
                Assert.That(data.SpawnMarkerIds, Is.Not.Empty);
                var serialized = new SerializedObject(controller);
                Assert.That(serialized.FindProperty("spawnPoints").arraySize, Is.EqualTo(data.SpawnMarkerIds.Count),
                    $"{data.EncounterId}의 씬 스폰 지점 수가 데이터 선언과 다르다.");
                Assert.That(serialized.FindProperty("exitLock").objectReferenceValue, Is.Not.Null,
                    $"{data.EncounterId} has no EncounterExitLock wired.");
            }

            // 계단은 계단참마다 1기씩, 오르면서 3웨이브로 처치한다.
            var stairs = controllers.Single(controller => controller.Data.EncounterId == "stage02.encounter.03");
            Assert.That(stairs.Data.WaveCount, Is.EqualTo(3));
            Assert.That(stairs.Data.SpawnCount, Is.EqualTo(1));
        }

        [Test]
        public void Test_Stage02_BlockoutReusesGrabAndOneWayVerbsOnAClimbableRoute()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var grabZone = Find(scene, "GrabWall_Zone");
            Assert.That(grabZone.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(grabZone.GetComponent<GrabbableSurface>(), Is.Not.Null);

            var oneWay = Find(scene, "OneWayPlatform");
            Assert.That(oneWay.GetComponent<BoxCollider2D>().usedByEffector, Is.True);
            Assert.That(oneWay.GetComponent<PlatformEffector2D>().useOneWay, Is.True);

            // 계단은 위로 갈수록 높아져야 한다. 순서가 뒤집히면 오를 수 없는 지형이 된다.
            var previousTop = float.NegativeInfinity;
            foreach (var name in new[] { "Stair_01", "Stair_02", "Stair_03" })
            {
                var stair = Find(scene, name);
                var top = stair.transform.position.y + stair.transform.lossyScale.y * 0.5f;
                Assert.That(top, Is.GreaterThan(previousTop), $"{name} must rise above the previous step.");
                previousTop = top;
            }

            // Zone D(마지막 교실)는 계단 꼭대기보다 높다.
            var upper = Find(scene, "Ground_UpperClassroom");
            Assert.That(upper.transform.position.y + upper.transform.lossyScale.y * 0.5f, Is.GreaterThan(previousTop));

            // Stage 3 이후 TerrainHazard로 재사용할 사물함 줄은 이번 스테이지에서는 비활성이다.
            Assert.That(Find(scene, "Lockers_Reserve").activeSelf, Is.False);
        }

        [Test]
        public void Test_Stage02_UsesItsOwnContaminationVariantAndChase()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var director = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ContaminationDirector>(true))
                .Single();

            Assert.That(director.Data, Is.Not.Null);
            Assert.That(director.Data.VariantId, Is.EqualTo("Stage02_Overlay_Intrusion"));
            Assert.That(director.Data.EchoOverlayScene, Is.EqualTo("Stage02_Overlay_Echo"));
            Assert.That(director.Data.IntrusionOverlayScene, Is.EqualTo("Stage02_Overlay_Intrusion"));
            Assert.That(director.Data.ValidateData(out var error), Is.True, error);

            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage02.asset");
            Assert.That(director.Data.VariantId, Is.EqualTo(stage.ContaminationVariantId),
                "씬의 오염 Variant와 StageData 선언이 어긋나면 재시도 시 다른 공간이 나온다.");
        }

        /// <summary>
        /// 씬 이름 규칙(Stage02_Base)과 빌드 설정 등록이 있어야 SceneFlowController가 Stage 1 → 2를 잇는다.
        /// 등록을 빠뜨리면 클리어 후 조용히 타이틀로 돌아가므로, 여기서 미리 잡는다.
        /// </summary>
        [Test]
        public void Test_Stage02_SceneIsRegisteredInBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path == ScenePath && entry.enabled), Is.True,
                $"{ScenePath} is not registered (enabled) in the build settings.");

            // Stage 02는 오버레이를 별도 씬으로 두지 않는다(#12). 씬이 늘면 적재 비용도 늘고
            // 기저 지형을 못 본 채 오버레이 좌표를 맞춰야 해서 저작이 어려워진다.
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path.Contains("Stage02_Overlay_")), Is.False,
                "Stage 02 오버레이는 Stage02_Base 안의 루트 오브젝트로 저작한다.");
        }

        /// <summary>
        /// 오버레이는 Stage02_Base 안의 루트 오브젝트다. 이름은 ContaminationVariantData가 선언한 값과
        /// 정확히 같아야 OverlaySceneLoader가 찾아서 켜고 끌 수 있다.
        /// </summary>
        [Test]
        public void Test_Contamination_Stage02OverlaysLiveInsideTheBaseSceneAndStartDisabled()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var variant = AssetDatabase.LoadAssetAtPath<ContaminationVariantData>(
                "Assets/Data/Contamination/Stage02_ContaminationVariant.asset");

            foreach (var overlayName in new[] { variant.EchoOverlayScene, variant.IntrusionOverlayScene })
            {
                var root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == overlayName);
                Assert.That(root, Is.Not.Null, $"'{overlayName}' 루트가 Stage02_Base에 없다.");

                // 탐색 중에 켜져 있으면 오염되지 않은 공간에 오염 지형이 미리 보인다.
                Assert.That(root.activeSelf, Is.False, $"'{overlayName}' must start disabled.");

                // 오버레이는 기저 공간에 더하기만 한다 — 실제로 지형을 들고 있어야 의미가 있다.
                Assert.That(root.GetComponentsInChildren<Collider2D>(true), Is.Not.Empty);
            }
        }

        private static GameObject Find(Scene scene, string name)
        {
            var found = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == name);
            Assert.That(found, Is.Not.Null, $"Missing Stage02 object: {name}");
            return found.gameObject;
        }
    }
}
