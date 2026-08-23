using Daeume.Player;
using UnityEngine;
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
        private StageThirteenEndingController ending;
        private Transform player;
        private Transform trauma;
        private string status = "트라우마에게 다가가세요.";

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name != StageSceneName) return;

            CreateCamera();
            CreateBackdrop();
            player = CreateActor("Player", new Vector3(-6f, -1.4f, 0f), new Color(0.78f, 0.88f, 1f));
            trauma = CreateActor("Trauma", new Vector3(1.5f, -1.4f, 0f), new Color(0.12f, 0.08f, 0.16f));

            // 이 두 컴포넌트는 Stage 13 규칙이 공격과 접촉 실패를 무효화하는 실제 대상이다.
            player.gameObject.AddComponent<PlayerCombat>();
            player.gameObject.AddComponent<TraumaContactHandler>();
            ending = gameObject.AddComponent<StageThirteenEndingController>();
            ending.BeginAcceptance();
        }

        private void Update()
        {
            if (player == null || ending == null) return;

            // ContaminationRuntime 어셈블리는 Input System을 직접 참조하지 않는다.
            // 그래서 씬 확인용 입력도 기존 Input API로만 읽어 의존성 경계를 유지한다.
            var horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                           - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            player.position += Vector3.right * (horizontal * 4f * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.R))
            {
                ending.RegisterRunawayLoop();
                status = ending.TraumaWaiting
                    ? "트라우마가 기다립니다. 이제 다가갈 수 있습니다."
                    : $"도주 루프 {ending.State.LoopCount}/4 — 길은 다시 벤치로 이어집니다.";
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                status = ending.TryLowerWeapon()
                    ? "무기를 내려놓았습니다. 천천히 마지막 걸음을 옮기세요."
                    : "트라우마 가까이에서만 무기를 내려놓을 수 있습니다.";
            }

            if (Input.GetKeyDown(KeyCode.Return) && ending.State.WeaponLowered)
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
            CreatePanel("Platform", new Vector3(0f, -2.4f, 1f), new Vector3(17f, 0.45f, 1f), new Color(0.18f, 0.22f, 0.28f));
            CreatePanel("EmptyPath", new Vector3(4f, -1.2f, 1f), new Vector3(8f, 1.7f, 1f), new Color(0.09f, 0.12f, 0.17f));
            CreatePanel("Bench", new Vector3(-6f, -1.65f, 0.5f), new Vector3(1.4f, 0.25f, 1f), new Color(0.42f, 0.25f, 0.16f));
        }

        private static Transform CreateActor(string objectName, Vector3 position, Color color)
        {
            var actor = CreatePanel(objectName, position, new Vector3(0.7f, 1.5f, 1f), color);
            return actor.transform;
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
