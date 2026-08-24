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
        [SerializeField] private TextMesh symbolText;

        private static Sprite placeholderSquare;
        private static Material unlitMaterial;

        private void Awake()
        {
            if (signal == null) signal = GetComponent<ChaseRouteSignal>();
            if (symbolText == null) symbolText = GetComponent<TextMesh>();
            Present();
        }

        public void Configure(ChaseRouteSignal targetSignal, SpriteRenderer door, SpriteRenderer sign, TextMesh text = null)
        {
            signal = targetSignal;
            doorVisual = door;
            signVisual = sign;
            if (text != null) symbolText = text;
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

            // 예전에는 씬에 박힌 TextMesh 문구·색이 ChaseRouteSignal 데이터와 따로 놀았다
            // (기호가 두 곳에 중복돼 하나만 고치면 어긋나고, 글자 색은 spec-013이 요구하는
            // "색 신호"를 전혀 반영하지 않았다). 여기서 signal 데이터를 유일한 출처로 만든다.
            if (symbolText != null)
            {
                symbolText.text = signal.Symbol;
                symbolText.color = signal.Color;
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
