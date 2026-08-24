using UnityEngine;

namespace Daeume.Stage
{
    /// <summary>
    /// URP 2D Renderer에서 Skybox 대신 스테이지 전용 하늘 스프라이트를
    /// 카메라 뒤에 고정하고 현재 화면을 빈틈없이 덮도록 맞춘다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StageSkyBackground : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float overscan = 1.02f;
        [SerializeField] private float worldZ = 50f;

        private Camera targetCamera;
        private SpriteRenderer spriteRenderer;

        private void OnEnable() => RefreshNow();
        private void LateUpdate() => RefreshNow();

        public void RefreshNow()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null)
                return;

            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
                targetCamera = Camera.main;

            if (targetCamera == null || !targetCamera.orthographic)
                return;

            var spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            var cameraHeight = targetCamera.orthographicSize * 2f;
            var cameraWidth = cameraHeight * targetCamera.aspect;
            var uniformScale = Mathf.Max(
                cameraWidth / spriteSize.x,
                cameraHeight / spriteSize.y) * overscan;

            transform.position = new Vector3(
                targetCamera.transform.position.x,
                targetCamera.transform.position.y,
                worldZ);
            transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }
    }
}
