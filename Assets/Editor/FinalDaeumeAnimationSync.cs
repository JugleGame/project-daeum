using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Daeume.Editor
{
    /// <summary>
    /// FinalDaeume의 개별 PNG와 기존 AnimationClip의 sprite curve를 동기화한다.
    /// 프레임 파일을 교체해도 Animator Controller와 clip GUID는 유지한다.
    /// </summary>
    public static class FinalDaeumeAnimationSync
    {
        private const string HeroFrames = "Assets/Art/Sprites/FinalDaeume/Hero/Frames";
        private const string TraumaFrames = "Assets/Art/Sprites/FinalDaeume/Trauma/Frames";
        private const string VisualPath = "Visual";

        [MenuItem("Daeume/Assets/Sync FinalDaeume Player Animations")]
        public static void SyncPlayerAnimations()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var heroSanitizedPixelCount = SanitizeFrameEdges(HeroFrames);
            var traumaSanitizedPixelCount = SanitizeFrameEdges(TraumaFrames);
            var sanitizedPixelCount = heroSanitizedPixelCount + traumaSanitizedPixelCount;
            if (sanitizedPixelCount > 0)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            ReimportFrames(HeroFrames);
            ReimportFrames(TraumaFrames);

            SyncClip("Assets/Animations/Player/Player_Idle.anim", "idle", 5, 6f, true);
            SyncClip("Assets/Animations/Player/Player_Move.anim", "move", 8, 10f, true);
            SyncClip("Assets/Animations/Player/Player_Attack.anim", "attack", 8, 12f, false);
            SyncClip("Assets/Animations/Player/Player_Airborne.anim", "jump", 6, 8f, false);
            SyncClip("Assets/Animations/Player/Player_Grab.anim", "grab", 4, 6f, true);

            AssetDatabase.SaveAssets();
            Debug.Log($"FinalDaeume animation clips synchronized: hero={heroSanitizedPixelCount}, trauma={traumaSanitizedPixelCount} bright edge pixels sanitized.");
        }

        private static int SanitizeFrameEdges(string folder)
        {
            var sanitizedPixelCount = 0;
            foreach (var path in Directory.GetFiles(folder, "*.png"))
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    throw new InvalidOperationException($"Unable to decode animation frame: {path}");
                }

                var width = texture.width;
                var height = texture.height;
                var pixels = texture.GetPixels32();
                var noisy = new bool[pixels.Length];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var index = (y * width) + x;
                        noisy[index] = pixels[index].a > 0
                            && IsBrightNeutral(pixels[index])
                            && TouchesTransparency(pixels, width, height, x, y);
                    }
                }

                var changed = false;
                for (var index = 0; index < pixels.Length; index++)
                {
                    if (!noisy[index]) continue;
                    var replacement = FindDarkestOpaqueNeighbor(pixels, noisy, width, height, index);
                    if (!replacement.HasValue) continue;
                    var alpha = pixels[index].a;
                    pixels[index] = replacement.Value;
                    pixels[index].a = alpha;
                    sanitizedPixelCount++;
                    changed = true;
                }

                if (changed)
                {
                    texture.SetPixels32(pixels);
                    texture.Apply(false, false);
                    File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
                }

                UnityEngine.Object.DestroyImmediate(texture);
            }

            return sanitizedPixelCount;
        }

        private static bool IsBrightNeutral(Color32 pixel)
        {
            var maximum = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
            var minimum = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
            var average = (pixel.r + pixel.g + pixel.b) / 3f;
            return maximum - minimum <= 28 && average >= 72f;
        }

        private static bool TouchesTransparency(Color32[] pixels, int width, int height, int x, int y)
        {
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if (offsetX == 0 && offsetY == 0) continue;
                    var sampleX = x + offsetX;
                    var sampleY = y + offsetY;
                    if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height) continue;
                    if (pixels[(sampleY * width) + sampleX].a == 0) return true;
                }
            }

            return false;
        }

        private static Color32? FindDarkestOpaqueNeighbor(
            Color32[] pixels,
            bool[] noisy,
            int width,
            int height,
            int sourceIndex)
        {
            var sourceX = sourceIndex % width;
            var sourceY = sourceIndex / width;
            Color32? best = null;
            var bestBrightness = int.MaxValue;
            for (var radius = 1; radius <= 4; radius++)
            {
                for (var offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    for (var offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        var x = sourceX + offsetX;
                        var y = sourceY + offsetY;
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;
                        var index = (y * width) + x;
                        var candidate = pixels[index];
                        if (candidate.a == 0 || noisy[index]) continue;
                        var brightness = candidate.r + candidate.g + candidate.b;
                        if (brightness >= bestBrightness) continue;
                        best = candidate;
                        bestBrightness = brightness;
                    }
                }

                if (best.HasValue && bestBrightness < 216) break;
            }

            return best;
        }

        private static void ReimportFrames(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                AssetDatabase.ImportAsset(
                    AssetDatabase.GUIDToAssetPath(guid),
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        }

        private static void SyncClip(
            string clipPath,
            string framePrefix,
            int frameCount,
            float framesPerSecond,
            bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"AnimationClip not found: {clipPath}");
            }

            var frames = new ObjectReferenceKeyframe[frameCount];
            for (var index = 0; index < frameCount; index++)
            {
                var framePath = $"{HeroFrames}/{framePrefix}_{index:00}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                if (sprite == null)
                {
                    throw new InvalidOperationException($"Sprite not found: {framePath}");
                }

                frames[index] = new ObjectReferenceKeyframe
                {
                    time = index / framesPerSecond,
                    value = sprite,
                };
            }

            var binding = EditorCurveBinding.PPtrCurve(VisualPath, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            clip.frameRate = framesPerSecond;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }
    }
}
