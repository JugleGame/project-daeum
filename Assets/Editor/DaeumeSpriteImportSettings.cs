using UnityEditor;
using UnityEngine;

namespace Daeume.Editor
{
    public sealed class DaeumeSpriteImportSettings : AssetPostprocessor
    {
        private const float PixelsPerUnit = 32f;

private void OnPreprocessTexture()
        {
            var normalized = assetPath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/Art/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteImportMode = SpriteImportMode.Single;

            var isCharacter = normalized.Contains("/Sprites/Player/")
                || normalized.Contains("/Sprites/Remnant/")
                || normalized.Contains("/Sprites/Trauma/");

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = isCharacter
                ? (int)SpriteAlignment.BottomCenter
                : (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }
    }
}
