using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// ChaseRouteSignal의 Shape/Symbol/Color를 실제 화면에 보이게 하는 프레젠터.
    /// 씬에는 신호 데이터만 배치돼 있고 아무 것도 그것을 읽어 그리지 않던 공백을 메운다.
    /// </summary>
    [RequireComponent(typeof(ChaseRouteSignal))]
    public sealed class ChaseRouteSignalPresenter : MonoBehaviour
    {
        [SerializeField] private ChaseRouteSignal signal;
        [SerializeField] private SpriteRenderer doorVisual;
        [SerializeField] private SpriteRenderer signVisual;

        private static Sprite placeholderSquare;
        private static Material unlitMaterial;

        private void Awake()
        {
            if (signal == null) signal = GetComponent<ChaseRouteSignal>();
            Present();
        }

        public void Configure(ChaseRouteSignal targetSignal, SpriteRenderer door, SpriteRenderer sign)
        {
            signal = targetSignal;
            doorVisual = door;
            signVisual = sign;
        }

        public void Present()
        {
            if (signal == null) return;
            var showExitDoor = signal.Shape == ChaseSignalShape.ExitDoor;

            if (doorVisual != null)
            {
                if (doorVisual.sprite == null) doorVisual.sprite = GetPlaceholderSquare();
                ApplyUnlitMaterial(doorVisual);
                doorVisual.enabled = showExitDoor;
            }

            if (signVisual != null)
            {
                ApplyUnlitMaterial(signVisual);
                signVisual.color = signal.Color;
                signVisual.enabled = showExitDoor;
            }
        }

        // 문 스프라이트 아트는 보류 상태다(픽셀 아티팩트 반복 발생). 확정 전까지 흰 사각형을 임시로 쓴다.
        private static Sprite GetPlaceholderSquare()
        {
            if (placeholderSquare != null) return placeholderSquare;
            var texture = new Texture2D(1, 1) { name = "ChaseRouteSignal_DoorPlaceholder" };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            placeholderSquare = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return placeholderSquare;
        }

        // URP 2D 기본 머티리얼은 Lit이라 Light2D 없이는 검게 렌더링된다(2026-08-20 세션 확인 버그).
        private static void ApplyUnlitMaterial(SpriteRenderer renderer)
        {
            if (unlitMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null) unlitMaterial = new Material(shader) { name = "ChaseRouteSignal_Unlit_Runtime" };
            }
            if (unlitMaterial != null) renderer.sharedMaterial = unlitMaterial;
        }
    }
}
