using System;
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
            ReimportFrames(HeroFrames);
            ReimportFrames(TraumaFrames);

            SyncClip("Assets/Animations/Player/Player_Idle.anim", "idle", 4, 6f, true);
            SyncClip("Assets/Animations/Player/Player_Move.anim", "move", 6, 10f, true);
            SyncClip("Assets/Animations/Player/Player_Attack.anim", "attack", 6, 12f, false);
            SyncClip("Assets/Animations/Player/Player_Airborne.anim", "jump", 4, 8f, false);
            SyncClip("Assets/Animations/Player/Player_Grab.anim", "grab", 4, 6f, true);

            AssetDatabase.SaveAssets();
            Debug.Log("FinalDaeume player animation clips synchronized: 24 frames at 64 PPU.");
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
