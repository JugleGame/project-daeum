using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Daeume.UI
{
    /// <summary>
    /// 플레이어 빌드에 포함된 한글 폰트를 기존 uGUI Text와 월드 공간 TextMesh에 적용한다.
    /// OS 폰트 fallback을 기대할 수 없는 WebGL에서도 같은 글리프를 사용하기 위한 부트스트랩이다.
    /// </summary>
    public static class KoreanFontBootstrap
    {
        public const string ResourcePath = "Fonts/NanumGothic-Regular";

        private static Font cachedFont;
        private static bool missingFontReported;

        public static Font KoreanFont => cachedFont != null
            ? cachedFont
            : cachedFont = Resources.Load<Font>(ResourcePath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ApplyToLoadedText();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToLoadedText();

        public static void ApplyToLoadedText()
        {
            var font = KoreanFont;
            if (font == null)
            {
                if (!missingFontReported)
                {
                    Debug.LogError($"한글 폰트를 Resources/{ResourcePath}에서 찾을 수 없습니다.");
                    missingFontReported = true;
                }

                return;
            }

            foreach (var label in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (IsLoadedSceneObject(label.gameObject)) label.font = font;
            }

            foreach (var label in Resources.FindObjectsOfTypeAll<TextMesh>())
            {
                if (!IsLoadedSceneObject(label.gameObject)) continue;

                label.font = font;
                var renderer = label.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = font.material;
            }
        }

        /// <summary>
        /// Scene load callback 이후 생성된 prefab에도 같은 font를 즉시 적용한다.
        /// RuntimeInitializeOnLoadMethod끼리는 호출 순서가 보장되지 않으므로 생성자가 이 메서드를 호출해야 한다.
        /// </summary>
        public static void ApplyToHierarchy(GameObject root)
        {
            if (root == null) return;

            var font = KoreanFont;
            if (font == null)
            {
                if (!missingFontReported)
                {
                    Debug.LogError($"한글 폰트를 Resources/{ResourcePath}에서 찾을 수 없습니다.");
                    missingFontReported = true;
                }

                return;
            }

            foreach (var label in root.GetComponentsInChildren<Text>(true)) label.font = font;
            foreach (var label in root.GetComponentsInChildren<TextMesh>(true))
            {
                label.font = font;
                var renderer = label.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = font.material;
            }
        }

        private static bool IsLoadedSceneObject(GameObject target) =>
            target.scene.IsValid() && target.scene.isLoaded;
    }
}
