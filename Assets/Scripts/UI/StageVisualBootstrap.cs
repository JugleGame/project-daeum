using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.UI
{
    public sealed class StageVisualBootstrap : MonoBehaviour
    {
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Color cameraBackground = new(0.035f, 0.045f, 0.065f, 1f);
        private Material unlitMaterial;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start() => ApplyToStage(SceneManager.GetActiveScene());
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (unlitMaterial != null) Destroy(unlitMaterial);
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToStage(scene);

        public void Configure(Sprite sprite) => playerSprite = sprite;

        private void ApplyToStage(Scene scene)
        {
            if (scene.name != "Stage01_Base") return;

            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null && unlitMaterial == null)
                unlitMaterial = new Material(shader) { name = "Stage_Unlit_Runtime" };

            var player = GameObject.Find("Player");
            if (player != null)
            {
                var renderer = player.GetComponent<SpriteRenderer>();
                if (renderer == null) renderer = player.AddComponent<SpriteRenderer>();
                renderer.sprite = playerSprite;
                renderer.color = Color.white;
                if (unlitMaterial != null) renderer.sharedMaterial = unlitMaterial;
                renderer.sortingLayerName = "Character";
                renderer.sortingOrder = 10;
            }

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

        private static Color EnsureValue(Color color, float minimum)
        {
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);
            var result = Color.HSVToRGB(hue, saturation, Mathf.Max(value, minimum));
            result.a = color.a;
            return result;
        }
    }
}
