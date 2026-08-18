using UnityEngine;

namespace Daeume.Prototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PrototypeVisual : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;
        private Texture2D texture;
        private Sprite sprite;

        public void SetColor(Color value)
        {
            color = value;
            GetComponent<SpriteRenderer>().color = color;
        }

        private void Awake()
        {
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                name = $"{name}_PrototypeTexture"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
        }

        private void OnDestroy()
        {
            Destroy(sprite);
            Destroy(texture);
        }
    }
}
