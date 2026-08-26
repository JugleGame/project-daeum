using UnityEditor;
using UnityEngine;

namespace Daeume.Editor
{
    /// <summary>
    /// Assets/Art 아래에 들어오는 이미지의 임포트 설정을 자동으로 픽셀아트 규격에 맞춘다.
    ///
    /// AssetPostprocessor: 에셋이 프로젝트로 들어올 때 유니티가 자동으로 불러 주는 에디터 전용 훅이다.
    /// 사람이 매번 손으로 설정하면 반드시 빠뜨리는 항목이 생기므로, 규격을 코드로 강제하는 편이 안전하다.
    ///
    /// 여기서 고정하는 값들은 협업 문서(collaboration-setup)의 픽셀 규칙과 일치해야 한다.
    /// - 기본 PPU 32 (프로젝트 blockout 격자)
    /// - FinalDaeume 및 Remnant 타입 프레임 PPU 64
    /// - Point 필터 (픽셀이 뭉개지지 않게)
    /// - 압축 없음 (색이 변질되지 않게)
    /// - 밉맵 없음 (2D에서는 불필요하고 흐려지는 원인)
    /// 이 값을 바꾸면 화면 전체의 픽셀 격자가 어긋나므로 임의로 수정하지 말 것.
    /// </summary>
    public sealed class DaeumeSpriteImportSettings : AssetPostprocessor
    {
        private const float PixelsPerUnit = 32f;
        private const float FinalDaeumePixelsPerUnit = 64f;

        /// <summary>이미지를 임포트하기 직전에 호출된다(이미 임포트된 파일은 재임포트해야 적용된다).</summary>
        private void OnPreprocessTexture()
        {
            var normalized = assetPath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/Art/")
                && !normalized.StartsWith("Assets/Resources/Art/"))
            {
                return;
            }

            var isFinalDaeume = normalized.Contains("/Sprites/FinalDaeume/");
            var usesFinalCharacterPpu = isFinalDaeume
                || normalized.Contains("/Sprites/Remnant/Melee/Frames/")
                || normalized.Contains("/Sprites/Remnant/Dash/Frames/")
                || normalized.Contains("/Sprites/Remnant/Ranged/Frames/");
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = usesFinalCharacterPpu
                ? FinalDaeumePixelsPerUnit
                : PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteImportMode = SpriteImportMode.Single;
            if (usesFinalCharacterPpu)
            {
                importer.userData = "daeume-final-64ppu";
            }

            var isCharacter = normalized.Contains("/Sprites/Player/")
                || normalized.Contains("/Sprites/Remnant/")
                || normalized.Contains("/Sprites/Trauma/");
            var isFinalDaeumeHero = normalized.Contains("/Sprites/FinalDaeume/Hero/");

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (isFinalDaeumeHero)
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            }
            else if (!isFinalDaeume)
            {
                settings.spriteAlignment = isCharacter
                    ? (int)SpriteAlignment.BottomCenter
                    : (int)SpriteAlignment.Center;
            }
            importer.SetTextureSettings(settings);
        }
    }
}
