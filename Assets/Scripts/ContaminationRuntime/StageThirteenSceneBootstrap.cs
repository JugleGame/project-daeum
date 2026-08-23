using Daeume.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// Stage13_Base를 단독으로 열어도 수용 엔딩의 핵심 흐름을 확인할 수 있게 만드는 임시 플레이 가능 씬 구성이다.
    /// 레벨 아트와 대사 원고는 Stage 13 콘텐츠 작업에서 교체한다.
    /// </summary>
    public sealed class StageThirteenSceneBootstrap : MonoBehaviour
    {
        private const string StageSceneName = "Stage13_Base";
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject traumaPrefab;
        private StageThirteenEndingController ending;
        private Transform player;
        private Transform trauma;
        private string status = "트라우마에게 다가가세요.";

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name != StageSceneName) return;

            CreateCamera();
            CreateBackdrop();
            player = SpawnActor(playerPrefab, "Player", new Vector3(-6f, -1.4f, 0f))
                     ?? CreateFallbackActor("Player", new Vector3(-6f, -1.4f, 0f), new Color(0.35f, 0.78f, 1f));
            trauma = SpawnActor(traumaPrefab, "Trauma", new Vector3(1.5f, -1.4f, 0f))
                     ?? CreateFallbackActor("Trauma", new Vector3(1.5f, -1.4f, 0f), new Color(0.85f, 0.18f, 0.35f));

            // 프리팹이 아직 연결되지 않은 상태로도 Stage 13 규칙을 검증할 수 있게 한다.
            if (player.GetComponent<PlayerCombat>() == null) player.gameObject.AddComponent<PlayerCombat>();
            if (player.GetComponent<TraumaContactHandler>() == null) player.gameObject.AddComponent<TraumaContactHandler>();

            ending = gameObject.AddComponent<StageThirteenEndingController>();
            ending.BeginAcceptance();
        }

        private void Update()
        {
            if (player == null || ending == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                           - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            player.position += Vector3.right * (horizontal * 4f * Time.deltaTime);

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ending.RegisterRunawayLoop();
                status = ending.TraumaWaiting
                    ? "트라우마가 기다립니다. 이제 다가갈 수 있습니다."
                    : $"도주 루프 {ending.State.LoopCount}/4 — 길은 다시 벤치로 이어집니다.";
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                status = ending.TryLowerWeapon()
                    ? "무기를 내려놓았습니다. 천천히 마지막 걸음을 옮기세요."
                    : "트라우마 가까이에서만 무기를 내려놓을 수 있습니다.";
            }

            if (keyboard.enterKey.wasPressedThisFrame && ending.State.WeaponLowered)
            {
                status = ending.CompleteAfterFarewell(true, true)
                    ? "다음에 보자. 버스가 출발합니다."
                    : status;
            }
        }

        private void OnGUI()
        {
            if (ending == null) return;
            GUI.Box(new Rect(24, 24, 620, 118), "Stage 13 — 수용");
            GUI.Label(new Rect(44, 58, 580, 24), "A/D 또는 ←/→ 이동 · R 도주 루프 · E 무기 내려놓기 · Enter 작별/버스");
            GUI.Label(new Rect(44, 84, 580, 24), status);
            GUI.Label(new Rect(44, 110, 580, 24), $"루프 {ending.State.LoopCount}/4  |  무기: {(ending.State.WeaponLowered ? "내려놓음" : "들고 있음")}  |  엔딩: {(ending.State.EndingCompleted ? "완료" : "진행 중")}");
        }

        private static void CreateCamera()
        {
            if (Camera.main != null) return;
            var cameraObject = new GameObject("Stage13Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.075f);
        }

        private static void CreateBackdrop()
        {
            var platform = CreatePanel("Platform", new Vector3(0f, -2.4f, 1f), new Vector3(17f, 0.45f, 1f), new Color(0.18f, 0.22f, 0.28f));
            platform.AddComponent<BoxCollider2D>();
            CreatePanel("EmptyPath", new Vector3(4f, -1.2f, 1f), new Vector3(8f, 1.7f, 1f), new Color(0.09f, 0.12f, 0.17f));
            CreatePanel("Bench", new Vector3(-6f, -1.65f, 0.5f), new Vector3(1.4f, 0.25f, 1f), new Color(0.42f, 0.25f, 0.16f));
        }

        private static Transform SpawnActor(GameObject prefab, string objectName, Vector3 position)
        {
            if (prefab == null) return null;
            var actor = Instantiate(prefab, position, Quaternion.identity);
            actor.name = objectName;
            actor.SetActive(true);
            return actor.transform;
        }

        /// <summary>프리팹 참조가 깨졌을 때도 씬의 핵심 상호작용이 중단되지 않도록 보이는 대체 캐릭터를 만든다.</summary>
        private static Transform CreateFallbackActor(string objectName, Vector3 position, Color color)
        {
            var root = new GameObject(objectName);
            root.transform.position = position;
            CreateBodyPart(root.transform, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.62f, 0.9f, 1f), color);
            CreateBodyPart(root.transform, "Head", new Vector3(0f, 1.23f, 0f), new Vector3(0.48f, 0.48f, 1f), color * 1.15f);
            return root.transform;
        }

        private static void CreateBodyPart(Transform parent, string objectName, Vector3 localPosition, Vector3 scale, Color color)
        {
            var part = CreatePanel(objectName, parent.position + localPosition, scale, color);
            part.transform.SetParent(parent, true);
        }

        private static GameObject CreatePanel(string objectName, Vector3 position, Vector3 scale, Color color)
        {
            var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = objectName;
            panel.transform.position = position;
            panel.transform.localScale = scale;
            panel.GetComponent<Renderer>().material.color = color;
            return panel;
        }
    }
}
