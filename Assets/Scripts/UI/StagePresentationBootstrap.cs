using Daeume.ContaminationRuntime;
using Daeume.Memory;
using Daeume.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.UI
{
    /// <summary>
    /// StageNN_Base 씬이 열릴 때 C(연출) 담당 프리팹들을 코드로 배치해 주는 부트스트랩이다.
    ///
    /// 왜 씬에 직접 넣지 않고 코드로 배치하나:
    /// 협업 규칙상 스테이지 씬은 B만 수정할 수 있다(유니티 씬 파일은 사실상 병합이 불가능해서
    /// 두 사람이 같이 열면 작업이 통째로 날아간다). C가 만든 HUD·회상 앵커를 그 씬에 넣으려면
    /// 씬 파일을 건드려야 하는데, 그 대신 실행 시점에 코드로 얹어 소유권 규칙을 지킨다.
    ///
    /// 수정(#12): 예전에는 "Stage01_Base" 한 이름만 처리해서, Stage 02 씬에서는 HUD도 회상 앵커도
    /// 생기지 않았다. 이제는 씬 이름 규칙(StageNN_Base)에서 스테이지 번호를 뽑아
    /// 마커 ID(stageNN.memory.anchor.01)와 앵커 프리팹(Memory/StageNN_MemoryAnchor)을 계산한다.
    /// SceneFlowController.StageSceneName이 쓰는 것과 같은 규칙이라, 새 스테이지는 씬과 프리팹만
    /// 추가하면 코드 변경 없이 이어진다.
    ///
    /// [RuntimeInitializeOnLoadMethod]:
    /// 게임이 시작될 때 유니티가 자동으로 불러 주는 진입점이다. 씬에 오브젝트를 미리 놓지 않아도 실행된다.
    /// </summary>
    internal static class StagePresentationBootstrap
    {
        private const string TitleSceneName = "Title";
        private const string VisualGuideObjectName = "B1_VisualGuide";

        /// <summary>월드 디버그 라벨(B1_VisualGuide)을 게임 화면에 보일지 여부. 기본은 숨김이다.</summary>
        public static bool ShowVisualGuideDebugLabels = false;

        // static이라 씬을 다시 불러와도 값이 유지된다. HUD를 중복 생성하지 않기 위한 기억이다.
        private static GameObject hudInstance;
        private static GameObject pressureInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // 먼저 해제하고 구독한다. 도메인 재로드 설정에 따라 등록이 남아 있을 수 있어 중복을 막는다.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 수정: 이미 열려 있는 씬도 검사한다.
            // 에디터에서 스테이지 씬을 직접 열고 Play를 누르면 sceneLoaded 이벤트가 오지 않아
            // HUD도 회상 앵커도 생성되지 않았다(= 아무것도 안 보이는 상태). 그 경우를 여기서 처리한다.
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && IsStageScene(scene.name))
                {
                    ApplyToStageScene(scene);
                }
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Title이 올라와도 스테이지 씬이 아직 열려 있으면 정리하지 않는다.
            // 정상 흐름에서는 SceneFlowController.ReplaceContent가 스테이지를 먼저 내린 뒤 Title을 올리므로
            // 이 조건은 걸리지 않는다. 반대로 에디터에서 스테이지 씬을 직접 열고 Play를 누르면
            // 스테이지가 이미 열린 채로 Boot가 Title을 추가 적재해서, 방금 만든 HUD가 곧바로 파괴됐다.
            // 그 뒤에는 스테이지 씬의 sceneLoaded가 다시 오지 않아 HUD가 영원히 복구되지 않는다
            // (= 상자를 열면 이동만 잠기고 자막이 없어 멈춘 것처럼 보이는 증상).
            if (scene.name == TitleSceneName)
            {
                if (!IsStageSceneLoaded()) DespawnPersistentPresentation();
                return;
            }

            if (!IsStageScene(scene.name)) return;
            ApplyToStageScene(scene);
        }

        private static bool IsStageScene(string sceneName) => StageScenes.IsStageScene(sceneName);

        private static int StageNumber(string sceneName) => StageScenes.StageNumber(sceneName);

        /// <summary>
        /// Title로 돌아가면 DontDestroyOnLoad로 남아 있던 HUD·압박 연출을 정리한다.
        /// 다음에 스테이지에 들어갈 때 SpawnPersistentPresentation이 새로 만들 수 있도록 참조도 비운다.
        /// </summary>
        private static bool IsStageSceneLoaded() => StageScenes.AnyLoaded();

        private static void DespawnPersistentPresentation()
        {
            if (hudInstance != null) Object.Destroy(hudInstance);
            hudInstance = null;

            if (pressureInstance != null) Object.Destroy(pressureInstance);
            pressureInstance = null;
        }

        private static void ApplyToStageScene(Scene scene)
        {
            SpawnMemoryAnchor(scene);
            SpawnPersistentPresentation();
            HideVisualGuideDebugLabels(scene);
        }

        /// <summary>
        /// B1_VisualGuide는 B가 레벨 제작 중 참고용으로 심어 둔 월드 텍스트 라벨이다(START/EXPLORE 등).
        /// 게임 화면에는 나오면 안 되므로 기본적으로 꺼 둔다. 씬 파일은 B 소유라 직접 못 고치니 런타임에 끈다.
        /// </summary>
        private static void HideVisualGuideDebugLabels(Scene scene)
        {
            if (ShowVisualGuideDebugLabels) return;

            foreach (var root in scene.GetRootGameObjects())
            {
                var guide = FindDescendant(root.transform, VisualGuideObjectName);
                if (guide != null) guide.gameObject.SetActive(false);
            }
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root.name == objectName) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendant(root.GetChild(i), objectName);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>회상 앵커를 마커 위치에 배치하고 추격 컨트롤러와 연결한다.</summary>
        private static void SpawnMemoryAnchor(Scene scene)
        {
            // 이미 있으면 만들지 않는다. 씬에 직접 배치된 경우와 공존할 수 있게 하는 안전장치다.
            if (Object.FindAnyObjectByType<MemoryAnchor>() != null)
            {
                EnsureMemoryPlayback(null);
                return;
            }

            var stageId = StageNumber(scene.name);
            var marker = FindMarker($"stage{stageId:00}.memory.anchor.01");
            var prefab = Resources.Load<GameObject>($"Memory/Stage{stageId:00}_MemoryAnchor");
            if (marker == null || prefab == null) return;

            var instance = Object.Instantiate(prefab, marker.transform.position, marker.transform.rotation);

            // Instantiate로 만든 오브젝트는 "활성 씬"에 들어간다.
            // 활성 씬이 Persistent일 수도 있어, 스테이지 씬으로 명시적으로 옮긴다.
            // 그래야 스테이지를 벗어날 때 함께 정리된다.
            SceneManager.MoveGameObjectToScene(instance, scene);

            var bridge = instance.GetComponentInChildren<MemoryCompletionBridge>();
            var chase = Object.FindAnyObjectByType<StageOneChaseController>();
            if (bridge != null && chase != null) bridge.Configure(chase);

            EnsureMemoryPlayback(instance);
        }

        /// <summary>
        /// 회상 진행·건너뛰기 입력을 담당하는 MemoryPlayback이 씬에 반드시 하나 있게 한다.
        /// </summary>
        /// <remarks>
        /// 이게 없으면 회상이 첫 문장에서 멈춰 게임 전체가 진행 불가가 된다(이번 검토에서 찾은 버그).
        /// 프리팹을 다시 저장하지 않아도 되도록 실행 시점에 컴포넌트를 붙여 준다.
        /// </remarks>
        private static void EnsureMemoryPlayback(GameObject anchorInstance)
        {
            if (Object.FindAnyObjectByType<MemoryPlayback>() != null) return;

            var host = anchorInstance != null
                ? anchorInstance
                : Object.FindAnyObjectByType<MemoryAnchor>()?.gameObject;

            if (host != null) host.AddComponent<MemoryPlayback>();
        }

        /// <summary>HUD와 압박 연출은 씬을 넘나들며 유지된다(게임 내내 하나만 존재).</summary>
        private static void SpawnPersistentPresentation()
        {
            if (hudInstance == null)
            {
                var hudPrefab = Resources.Load<GameObject>("UI/Stage01_Presentation");
                if (hudPrefab != null)
                {
                    hudInstance = Object.Instantiate(hudPrefab);
                    // DontDestroyOnLoad: 씬이 바뀌어도 파괴되지 않게 한다.
                    // Title로 돌아갈 때는 OnSceneLoaded가 DespawnPersistentPresentation으로 정리한다.
                    Object.DontDestroyOnLoad(hudInstance);
                }
            }

            if (pressureInstance == null)
            {
                var pressurePrefab = Resources.Load<GameObject>("Presentation/Stage01_PressurePresentation");
                if (pressurePrefab != null)
                {
                    pressureInstance = Object.Instantiate(pressurePrefab);
                    Object.DontDestroyOnLoad(pressureInstance);
                }
            }
        }

        /// <summary>B가 씬에 심어 둔 마커를 ID로 찾는다. 좌표를 코드에 적지 않기 위한 방식이다.</summary>
        private static StageMarker FindMarker(string markerId)
        {
            foreach (var marker in Object.FindObjectsByType<StageMarker>(FindObjectsSortMode.None))
                if (marker.MarkerId == markerId) return marker;
            return null;
        }
    }
}
