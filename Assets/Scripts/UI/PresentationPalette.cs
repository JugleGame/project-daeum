using UnityEngine;

namespace Daeume.UI
{
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
