using System.Linq;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Enemy;
using Daeume.Encounter;
using Daeume.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class CharacterAnimationMappingTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string PlayerSpritePath = "Assets/Art/Sprites/FinalDaeume/Hero/Frames/idle_00.png";
        private const string RemnantSpritePath = "Assets/Art/Sprites/FinalDaeume/Trauma/Frames/idle_00.png";
        private const string TraumaSpritePath = "Assets/Art/Sprites/FinalDaeume/Trauma/Frames/idle_00.png";

        [Test]
        public void Test_Animation_PrefabsUseCurrentSpritesAndControllers()
        {
            AssertPrefabMapping<PlayerAnimationDriver>(PlayerPrefabPath, PlayerSpritePath);
            AssertPrefabMapping<RemnantAnimationDriver>("Assets/Prefabs/Enemy/Stage01_MeleeRemnant.prefab", RemnantSpritePath);
            AssertPrefabMapping<RemnantAnimationDriver>("Assets/Prefabs/Enemy/DashRemnant.prefab", RemnantSpritePath);
            AssertPrefabMapping<RemnantAnimationDriver>("Assets/Prefabs/Enemy/RangedRemnant.prefab", RemnantSpritePath);
            AssertPrefabMapping<TraumaAnimationDriver>("Assets/Prefabs/Enemy/Stage01_Trauma.prefab", TraumaSpritePath);
        }

        [Test]
        public void Test_Animation_ControllersContainRequiredStatesAndFallbackClips()
        {
            AssertController(
                "Assets/Animations/Player/Player.controller",
                new[] { "Idle", "Move", "Airborne", "Attack", "Damaged", "Dead", "Grab" },
                "FinalDaeume/Hero/Frames");
            AssertController(
                "Assets/Animations/Enemy/Remnant.controller",
                new[] { "Idle", "Alert", "Approach", "Attack", "Hit", "Dead" },
                "FinalDaeume/Trauma/Frames");
            AssertController(
                "Assets/Animations/Enemy/Trauma.controller",
                new[] { "Idle", "Chase", "Attack" },
                "FinalDaeume/Trauma/Frames");
        }

        [Test]
        public void Test_Animation_FinalPixelArtFramesDriveHeroAndTraumaClips()
        {
            AssertImporterFolder(
                "Assets/Art/Sprites/FinalDaeume/Hero/Frames",
                24,
                64f,
                SpriteAlignment.BottomCenter);
            AssertImporterFolder(
                "Assets/Art/Sprites/FinalDaeume/Trauma/Frames",
                14,
                64f,
                SpriteAlignment.Custom);
            AssertFrameClip("Assets/Animations/Player/Player_Idle.anim", "FinalDaeume/Hero/Frames", 4, 6f, true);
            AssertFrameClip("Assets/Animations/Player/Player_Move.anim", "FinalDaeume/Hero/Frames", 6, 10f, true);
            AssertFrameClip("Assets/Animations/Player/Player_Attack.anim", "FinalDaeume/Hero/Frames", 6, 12f, false);
            AssertFrameClip("Assets/Animations/Player/Player_Airborne.anim", "FinalDaeume/Hero/Frames", 4, 8f, false);
            AssertFrameClip("Assets/Animations/Player/Player_Grab.anim", "FinalDaeume/Hero/Frames", 4, 6f, true);
            AssertFrameClip("Assets/Animations/Enemy/Trauma_Idle.anim", "FinalDaeume/Trauma/Frames", 4, 5f, true);
            AssertFrameClip("Assets/Animations/Enemy/Trauma_Chase.anim", "FinalDaeume/Trauma/Frames", 6, 8f, true);
        }

        [Test]
        public void Test_Animation_Stage01UsesMappedCharacterPrefabs()
        {
            var stage = EditorSceneManager.OpenScene("Assets/Scenes/Stage01_Base.unity", OpenSceneMode.Single);
            var trauma = stage.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<TraumaChaseActor>(true))
                .Single();
            AssertPrefabInstance<TraumaAnimationDriver>(
                trauma.gameObject,
                "Assets/Prefabs/Enemy/Stage01_Trauma.prefab");

            var encounter = stage.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EncounterController>(true))
                .Single();
            var serializedEncounter = new SerializedObject(encounter);
            var enemyPrefab = serializedEncounter.FindProperty("enemyPrefab").objectReferenceValue as MeleeRemnant;
            Assert.That(enemyPrefab, Is.Not.Null, "Stage01 encounter enemy prefab");
            Assert.That(
                AssetDatabase.GetAssetPath(enemyPrefab.gameObject),
                Is.EqualTo("Assets/Prefabs/Enemy/Stage01_MeleeRemnant.prefab"));
            Assert.That(enemyPrefab.GetComponent<Animator>(), Is.Not.Null, "Stage01 enemy Animator");
            Assert.That(enemyPrefab.GetComponent<RemnantAnimationDriver>(), Is.Not.Null, "Stage01 enemy driver");

            var persistent = EditorSceneManager.OpenScene("Assets/Scenes/Persistent.unity", OpenSceneMode.Single);
            var player = persistent.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerController>(true))
                .Single();
            AssertPrefabInstance<PlayerAnimationDriver>(player.gameObject, PlayerPrefabPath);

            var camera = persistent.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .Single();
            var pixelPerfect = camera.GetComponents<MonoBehaviour>()
                .Single(component => component.GetType().Name == "PixelPerfectCamera");
            var serializedPixelPerfect = new SerializedObject(pixelPerfect);
            Assert.That(
                serializedPixelPerfect.FindProperty("m_UpscaleRT").boolValue,
                Is.True,
                "High-resolution character art must use the native 1920x1080 reference target");
            Assert.That(
                serializedPixelPerfect.FindProperty("m_PixelSnapping").boolValue,
                Is.True,
                "Final character art must map source pixels to the 64 PPU screen grid");
            Assert.That(serializedPixelPerfect.FindProperty("m_AssetsPPU").intValue, Is.EqualTo(64));
            Assert.That(serializedPixelPerfect.FindProperty("m_RefResolutionX").intValue, Is.EqualTo(1920));
            Assert.That(serializedPixelPerfect.FindProperty("m_RefResolutionY").intValue, Is.EqualTo(1080));
        }

        [Test]
        public void Test_Animation_RuntimeCharacterGraphsExcludeLegacyPixelArt()
        {
            var runtimeRoots = new[]
            {
                "Assets/Scenes/Persistent.unity",
                "Assets/Scenes/Stage01_Base.unity",
                PlayerPrefabPath,
                "Assets/Prefabs/Enemy/Stage01_MeleeRemnant.prefab",
                "Assets/Prefabs/Enemy/DashRemnant.prefab",
                "Assets/Prefabs/Enemy/RangedRemnant.prefab",
                "Assets/Prefabs/Enemy/Stage01_Trauma.prefab",
            };
            var legacySprites = new[]
            {
                "Assets/Art/Sprites/Player/Player_Core.png",
                "Assets/Art/Sprites/Remnant/RemnantBody.png",
                "Assets/Art/Sprites/Trauma/TraumaBody.png",
                "Assets/Resources/Trauma/TraumaBody.png",
            };

            foreach (var runtimeRoot in runtimeRoots)
            {
                var dependencies = AssetDatabase.GetDependencies(runtimeRoot, true);
                Assert.That(
                    dependencies.Intersect(legacySprites),
                    Is.Empty,
                    $"{runtimeRoot} runtime dependency graph contains legacy pixel art");
            }

            foreach (var legacySprite in legacySprites)
            {
                Assert.That(
                    AssetDatabase.LoadMainAssetAtPath(legacySprite),
                    Is.Null,
                    $"Legacy pixel art must be removed: {legacySprite}");
            }

            Assert.That(
                AssetDatabase.LoadMainAssetAtPath("Assets/Art/Sprites/Player/Player_Core.asset"),
                Is.Null,
                "Legacy embedded Player_Core texture must be removed");
        }

        private static void AssertPrefabMapping<TDriver>(string prefabPath, string spritePath)
            where TDriver : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(prefab.GetComponent<TDriver>(), Is.Not.Null, $"{prefabPath} driver");

            var animator = prefab.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null, $"{prefabPath} Animator");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, $"{prefabPath} controller");

            var expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            Assert.That(expectedSprite, Is.Not.Null, spritePath);
            var mapped = prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .SingleOrDefault(renderer => renderer.sprite == expectedSprite);
            Assert.That(mapped, Is.Not.Null, $"{prefabPath} must use {spritePath}");
            Assert.That(mapped.sortingLayerName, Is.EqualTo("Character"), $"{prefabPath} sorting layer");
            Assert.That(
                mapped.transform.localScale,
                Is.EqualTo(Vector3.one),
                $"{prefabPath} must not downscale original-resolution character pixels");
        }

        private static void AssertController(
            string path,
            string[] requiredStates,
            string expectedFramePath = null)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            Assert.That(controller, Is.Not.Null, path);
            Assert.That(controller.parameters.Any(parameter =>
                parameter.name == CharacterAnimationParameters.State &&
                parameter.type == AnimatorControllerParameterType.Int), Is.True, $"{path} State parameter");

            var states = controller.layers[0].stateMachine.states;
            foreach (var requiredState in requiredStates)
            {
                var state = states.SingleOrDefault(value => value.state.name == requiredState).state;
                Assert.That(state, Is.Not.Null, $"{path} state {requiredState}");
                Assert.That(state.motion, Is.TypeOf<AnimationClip>(), $"{path} fallback clip {requiredState}");
                if (expectedFramePath != null)
                {
                    AssertClipUsesOnlyExpectedFrames((AnimationClip)state.motion, expectedFramePath);
                }
            }
        }

        private static void AssertClipUsesOnlyExpectedFrames(AnimationClip clip, string expectedFramePath)
        {
            var spriteBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .Where(binding => binding.propertyName == "m_Sprite")
                .ToArray();
            Assert.That(spriteBindings, Is.Not.Empty, $"{clip.name} sprite bindings");

            var frames = spriteBindings
                .SelectMany(binding => AnimationUtility.GetObjectReferenceCurve(clip, binding))
                .Select(keyframe => keyframe.value as Sprite)
                .ToArray();
            Assert.That(frames, Is.Not.Empty, $"{clip.name} sprite frames");
            Assert.That(frames.All(sprite =>
                sprite != null && AssetDatabase.GetAssetPath(sprite).Contains(expectedFramePath)),
                Is.True,
                $"{clip.name} must not fall back to legacy sprites");
        }

        private static void AssertPrefabInstance<TDriver>(GameObject instance, string expectedPath)
            where TDriver : Component
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            Assert.That(source, Is.Not.Null, $"{instance.name} prefab source");
            Assert.That(AssetDatabase.GetAssetPath(source), Is.EqualTo(expectedPath));
            Assert.That(instance.GetComponent<Animator>(), Is.Not.Null, $"{instance.name} Animator");
            Assert.That(instance.GetComponent<TDriver>(), Is.Not.Null, $"{instance.name} driver");
        }

        private static void AssertFrameClip(
            string clipPath,
            string expectedFramePath,
            int expectedFrameCount,
            float expectedFrameRate,
            bool expectedLoop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Assert.That(clip, Is.Not.Null, clipPath);
            Assert.That(clip.frameRate, Is.EqualTo(expectedFrameRate), $"{clipPath} frame rate");
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.EqualTo(expectedLoop), $"{clipPath} loop");

            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .SingleOrDefault(value => value.path == "Visual" && value.propertyName == "m_Sprite");
            Assert.That(binding.propertyName, Is.EqualTo("m_Sprite"), $"{clipPath} sprite binding");
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            Assert.That(frames, Has.Length.EqualTo(expectedFrameCount), $"{clipPath} frame count");
            Assert.That(frames.All(frame =>
                frame.value is Sprite sprite &&
                AssetDatabase.GetAssetPath(sprite).Contains(expectedFramePath)), Is.True, $"{clipPath} frame source");
        }

        private static void AssertImporterFolder(
            string folder,
            int expectedCount,
            float expectedPixelsPerUnit,
            SpriteAlignment expectedAlignment)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            Assert.That(guids, Has.Length.EqualTo(expectedCount), $"{folder} imported frame count");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(expectedPixelsPerUnit), $"{path} PPU");
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), $"{path} source pixel filter");
                Assert.That(importer.mipmapEnabled, Is.False, $"{path} mip maps");
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), $"{path} compression");
                Assert.That(importer.userData, Is.EqualTo("daeume-final-64ppu"), $"{path} source provenance");

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteAlignment, Is.EqualTo((int)expectedAlignment), $"{path} alignment");
                if (expectedAlignment == SpriteAlignment.Custom)
                {
                    Assert.That(settings.spritePivot.x, Is.InRange(0f, 1f), $"{path} pivot x");
                    Assert.That(settings.spritePivot.y, Is.InRange(0f, 0.1f), $"{path} bottom anchor pivot y");
                }
            }
        }
    }
}
