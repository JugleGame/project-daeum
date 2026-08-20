using UnityEngine;

namespace Daeume.UI
{
    /// <summary>
    /// UI·연출에서 쓰는 색을 한곳에 모아 둔 데이터 에셋이다.
    ///
    /// 왜 필요한가: 색을 각 스크립트에 직접 적어 두면 톤을 바꿀 때 전부 찾아 고쳐야 하고,
    /// 미묘하게 다른 색이 섞여 화면이 지저분해진다. 팔레트 하나를 공유하면 그 문제가 사라진다.
    ///
    /// 주의: 색만으로 필수 정보를 전달해서는 안 된다(spec-013).
    /// 이 팔레트는 "분위기"를 통일하기 위한 것이고, 경고·잠금 같은 필수 신호는
    /// 반드시 형태나 기호를 함께 써야 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Daeume/Presentation Palette")]
    public sealed class PresentationPalette : ScriptableObject
    {
        [SerializeField] private Color background = new(0.055f, 0.067f, 0.086f, 1f);
        [SerializeField] private Color foreground = new(0.88f, 0.85f, 0.76f, 1f);
        [SerializeField] private Color memory = new(0.39f, 0.72f, 0.69f, 1f);
        [SerializeField] private Color danger = new(0.82f, 0.25f, 0.29f, 1f);
        [SerializeField] private Color focus = new(1f, 0.78f, 0.28f, 1f);
        public Color Background => background;
        public Color Foreground => foreground;
        public Color Memory => memory;
        public Color Danger => danger;
        public Color Focus => focus;
    }
}
