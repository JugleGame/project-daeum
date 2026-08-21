using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.UI
{
    /// <summary>
    /// 스테이지가 열릴 때 플레이어 스프라이트·카메라 설정·지형 색을 코드로 맞춰 주는 임시 연출 부트스트랩이다.
    ///
    /// 왜 코드로 하나: B가 소유한 Stage01_Base 씬을 C가 열 수 없기 때문이다(씬 소유권 규칙).
    /// 아트가 확정되기 전 단계에서 최소한의 시각적 통일감을 주기 위한 장치다.
    ///
    /// 검토 메모: 이 방식은 "임시"라는 점이 중요하다. 실제 스프라이트와 타일이 들어오면
    /// 색 보정(EnsureValue) 같은 처리는 아트 자체가 담당해야 하며, 이 스크립트는 축소되거나 사라져야 한다.
    /// 지금처럼 런타임에 모든 SpriteRenderer를 훑어 색을 덮어쓰면, 나중에 의도적으로 어둡게 그린 아트까지
    /// 밝게 바뀌어 아티스트의 의도를 지운다.
    /// </summary>
    public sealed class StageVisualBootstrap : MonoBehaviour
    {
        [SerializeField] private Color cameraBackground = new(0.5f, 0.5f, 0.5f, 1f);
        private Material unlitMaterial;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void Start() => ApplyToStage(SceneManager.GetActiveScene());
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (unlitMaterial != null) Destroy(unlitMaterial);
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToStage(scene);

        // 추격 중 압박 단계가 바뀔 때마다 오염 오버레이 씬이 추가로 로드/언로드된다(ContaminationDirector).
        // 그 오버레이 씬도 sceneUnloaded를 발생시키므로, Stage01_Base가 실제로 남아 있는지 다시 확인해야 한다.
        private void OnSceneUnloaded(Scene scene) => SetPlayerPhysicsActive(SceneManager.GetSceneByName("Stage01_Base").isLoaded);

        private void ApplyToStage(Scene scene)
        {
            // Player는 Persistent 씬에 계속 존재하고 Rigidbody2D도 항상 살아 있어서,
            // 실제 스테이지(바닥)가 없는 Title/Boot 단계에서도 중력으로 계속 낙하했다.
            // 스테이지에 들어와 있을 때만 Dynamic으로 켜고, 그 외에는 Kinematic으로 묶어 둔다.
            //
            // 주의: 방금 로드된 scene.name과 "Stage01_Base"를 직접 비교하면 안 된다.
            // 추격 중 압박 단계가 바뀔 때마다 오염 오버레이 씬(Stage01_Overlay_Echo 등)이 추가로
            // 로드되는데, 그때도 이 콜백이 호출된다. scene.name만 비교하면 오버레이 로드 때마다
            // Player가 Kinematic으로 바뀌어 버리고, 그 순간 공중에 떠 있었다면(예: 점프 중)
            // 중력을 안 받는 Kinematic 상태로 남아 속도만으로 하늘로 계속 올라가 버린다.
            // 그래서 "방금 뭐가 로드됐나"가 아니라 "지금 Stage01_Base가 실제로 떠 있나"를 확인한다.
            SetPlayerPhysicsActive(SceneManager.GetSceneByName("Stage01_Base").isLoaded);

            if (scene.name != "Stage01_Base") return;

            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null && unlitMaterial == null)
                unlitMaterial = new Material(shader) { name = "Stage_Unlit_Runtime" };

            var camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 4.21875f;
                camera.backgroundColor = cameraBackground;
            }

            foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.sortingLayerName == "Terrain")
                {
                    renderer.color = EnsureValue(renderer.color, 0.5f);
                    if (unlitMaterial != null) renderer.sharedMaterial = unlitMaterial;
                }
                else if (renderer.sortingLayerName == "Background")
                {
                    renderer.color = EnsureValue(renderer.color, 0.22f);
                    if (unlitMaterial != null) renderer.sharedMaterial = unlitMaterial;
                }
            }
        }

        private static void SetPlayerPhysicsActive(bool active)
        {
            var player = GameObject.Find("Player");
            var body = player != null ? player.GetComponent<Rigidbody2D>() : null;
            if (body != null) body.bodyType = active ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        }

        /// <summary>
        /// 색이 너무 어두우면 최소 밝기까지만 올린다(색조·채도는 유지).
        /// RGB를 직접 만지면 색감이 틀어지므로, HSV(색상·채도·명도)로 바꿔 명도만 조정한다.
        /// </summary>
        private static Color EnsureValue(Color color, float minimum)
        {
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);
            var result = Color.HSVToRGB(hue, saturation, Mathf.Max(value, minimum));
            result.a = color.a;
            return result;
        }
    }
}
