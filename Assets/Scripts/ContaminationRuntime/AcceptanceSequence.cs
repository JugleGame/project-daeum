using System.Collections;
using Daeume.Contamination;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Flow;
using Daeume.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Daeume.ContaminationRuntime
{
    // ponytail: Stage13 전용 규칙을 단일 컴포넌트에 모음. 유사 엔딩 스테이지가 생기면 분리한다.
    public sealed class AcceptanceSequence : MonoBehaviour
    {
        public const int MaximumLoopCount = 4;

        [Header("Actors")]
        [SerializeField] private ContaminationDirector director;
        [SerializeField] private SceneFlowController flow;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private TraumaContactHandler traumaContact;
        [SerializeField] private Transform player;
        [SerializeField] private Transform trauma;

        [Header("Runaway loop")]
        [SerializeField] private float loopBoundaryX = -8f;
        [SerializeField] private float loopWrapX = 28f;
        [SerializeField] private float loopReturnX;
        [SerializeField, Range(20f, 30f)] private float minimumRunawaySeconds = 20f;
        [SerializeField, Min(0f)] private float traumaLoopDelaySeconds = 1.5f;
        [SerializeField, Min(0f)] private float traumaLoopSpawnOffsetX = 3f;

        [Header("Pressure reversal")]
        [SerializeField, Min(0.1f)] private float collapseDistance = 24f;
        [SerializeField, Min(0.1f)] private float intrusionDistance = 16f;
        [SerializeField, Min(0.1f)] private float echoDistance = 9f;
        [SerializeField, Min(0.1f)] private float lowerWeaponDistance = 4f;
        [SerializeField, Min(0.1f)] private float endingTouchDistance = 1.2f;
        [SerializeField, Min(0.1f)] private float minimumFinalStepDistance = 0.5f;

        [Header("Presentation")]
        [SerializeField] private Light2D globalLight;
        [SerializeField] private SpriteRenderer skyBackground;
        [SerializeField] private Color collapseLightColor = new(0.16f, 0.2f, 0.31f);
        [SerializeField] private Color stableLightColor = Color.white;
        [SerializeField, Range(0f, 2f)] private float collapseLightIntensity = 0.55f;
        [SerializeField, Range(0f, 2f)] private float stableLightIntensity = 1f;
        [SerializeField, Min(0.1f)] private float cameraHintSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float farewellSeconds = 2f;
        [SerializeField, Min(0.1f)] private float creditSeconds = 1.5f;
        [SerializeField, Min(0.1f)] private float fadeSeconds = 1f;

        private InputAction interact;
        private AudioSource directionalMusic;
        private Rigidbody2D playerBody;
        private Vector3 weaponLoweredPosition;
        private float runawaySeconds;
        private int loopCount;
        private bool weaponLowered;
        private bool endingStarted;
        private bool promptVisible;
        private Coroutine traumaLoopRespawn;
        private PressureStage pressure = PressureStage.Collapse;

        public int LoopCount => loopCount;
        public bool TraumaWaiting => loopCount >= MaximumLoopCount;
        public bool WeaponLowered => weaponLowered;
        public bool EndingStarted => endingStarted;
        public bool TraumaLoopRespawning => traumaLoopRespawn != null;
        public PressureStage Pressure => pressure;
        public float CurrentLightIntensity => globalLight == null ? -1f : globalLight.intensity;
        public Color CurrentLightColor => globalLight == null ? Color.clear : globalLight.color;
        public Color CurrentSkyColor => skyBackground == null ? Color.clear : skyBackground.color;
        public string FarewellKey => "ending.farewell";
        public string CreditKey => "ending.credit";

        private void Start()
        {
            ResolveReferences();
            var data = flow?.CurrentData;
            loopCount = Mathf.Clamp(data?.StageThirteenLoopCount ?? 0, 0, MaximumLoopCount);
            weaponLowered = data?.WeaponLowered ?? false;

            foreach (var encounter in FindObjectsByType<EncounterController>(FindObjectsInactive.Include))
                encounter.enabled = false;
            foreach (var exitLock in FindObjectsByType<EncounterExitLock>(FindObjectsInactive.Include))
                exitLock.enabled = false;

            traumaContact?.SetContactFailureEnabled(false);
            combat?.SetCombatEnabled(!weaponLowered);
            if (director != null)
            {
                director.SetPressure(PressureStage.Collapse);
                director.SetMovementSuppressed(TraumaWaiting);
            }

            ApplyHint(loopCount);
        }

        private void OnDisable()
        {
            CancelTraumaLoopRespawn();
            SetPrompt(false);
            interact?.Disable();
        }

        private void Update()
        {
            ResolveReferences();
            if (player == null || trauma == null || endingStarted) return;

            var distance = Vector2.Distance(player.position, trauma.position);
            pressure = ResolvePressure(distance, collapseDistance, intrusionDistance, echoDistance);
            director?.SetPressure(pressure);
            ApplyLight(distance);
            UpdateDirectionalMusic(distance);

            if (!weaponLowered)
            {
                UpdateRunawayLoop();
                var canLower = pressure == PressureStage.Stable && distance <= lowerWeaponDistance;
                SetPrompt(canLower);
                if (canLower && interact != null && interact.WasPressedThisFrame()) TryLowerWeapon();
                return;
            }

            SetPrompt(false);
            if (CanCompleteEnding()) StartCoroutine(CompleteEnding());
        }

        public static PressureStage ResolvePressure(float distance, float collapse, float intrusion, float echo)
        {
            if (distance >= collapse) return PressureStage.Collapse;
            if (distance >= intrusion) return PressureStage.Intrusion;
            if (distance >= echo) return PressureStage.Echo;
            return PressureStage.Stable;
        }

        public int RegisterRunawayLoop()
        {
            loopCount = Mathf.Min(MaximumLoopCount, loopCount + 1);
            runawaySeconds = 0f;
            flow?.SaveStageThirteenProgress(loopCount, weaponLowered);
            ApplyHint(loopCount);
            return loopCount;
        }

        public bool TryLowerWeapon()
        {
            ResolveReferences();
            if (weaponLowered || player == null || trauma == null ||
                Vector2.Distance(player.position, trauma.position) > lowerWeaponDistance)
                return false;

            weaponLowered = true;
            weaponLoweredPosition = player.position;
            combat?.SetCombatEnabled(false);
            flow?.SaveStageThirteenProgress(loopCount, true);
            SetPrompt(false);
            return true;
        }

        public bool CanCompleteEnding()
        {
            return weaponLowered && player != null && trauma != null &&
                   Vector2.Distance(player.position, weaponLoweredPosition) >= minimumFinalStepDistance &&
                   Vector2.Distance(player.position, trauma.position) <= endingTouchDistance;
        }

        public void ConfigureForTest(Transform playerTransform, Transform traumaTransform, PlayerCombat playerCombat,
            TraumaContactHandler contact, ContaminationDirector chaseDirector = null, float loopDelaySeconds = -1f)
        {
            player = playerTransform;
            trauma = traumaTransform;
            combat = playerCombat;
            traumaContact = contact;
            if (chaseDirector != null) director = chaseDirector;
            if (loopDelaySeconds >= 0f) traumaLoopDelaySeconds = loopDelaySeconds;
        }

        private void UpdateRunawayLoop()
        {
            playerBody ??= player.GetComponent<Rigidbody2D>();
            var movingLeft = playerBody != null
                ? playerBody.linearVelocity.x < -0.1f
                : Keyboard.current != null && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed);
            if (movingLeft)
            {
                runawaySeconds += Time.deltaTime;
                if (director != null && !director.ChaseActive) director.BeginChase();
            }
            if (player.position.x > loopBoundaryX) return;

            var position = player.position;
            position.x = runawaySeconds >= minimumRunawaySeconds ? loopReturnX : loopWrapX;
            player.position = position;
            if (playerBody != null) playerBody.position = position;
            if (runawaySeconds >= minimumRunawaySeconds) RegisterRunawayLoop();
            BeginTraumaLoopRespawn();
        }

        private void BeginTraumaLoopRespawn()
        {
            if (trauma == null || !isActiveAndEnabled) return;
            if (traumaLoopRespawn != null) StopCoroutine(traumaLoopRespawn);
            traumaLoopRespawn = StartCoroutine(RespawnTraumaAfterLoop());
        }

        private IEnumerator RespawnTraumaAfterLoop()
        {
            director?.SetMovementSuppressed(true);
            trauma.gameObject.SetActive(false);
            yield return new WaitForSeconds(traumaLoopDelaySeconds);

            if (trauma != null)
            {
                var position = trauma.position;
                position.x = loopWrapX + traumaLoopSpawnOffsetX;
                trauma.position = position;
                trauma.gameObject.SetActive(true);
            }

            director?.SetMovementSuppressed(TraumaWaiting);
            traumaLoopRespawn = null;
        }

        private void CancelTraumaLoopRespawn()
        {
            if (traumaLoopRespawn == null) return;
            StopCoroutine(traumaLoopRespawn);
            traumaLoopRespawn = null;
            if (trauma != null && !trauma.gameObject.activeSelf) trauma.gameObject.SetActive(true);
            director?.SetMovementSuppressed(TraumaWaiting);
        }

        private void ApplyHint(int stage)
        {
            if (!Application.isPlaying) return;
            SetSignal("Signal_Left_01", stage == 1);
            SetSignal("Signal_Exit_01", stage == 2);
            SetSignal("Signal_DeadEnd_01", stage >= 3);

            if (stage == 1 && isActiveAndEnabled) StartCoroutine(FrameEmptyPath());
            if (stage >= 2) StartDirectionalMusic();
            if (stage == 3)
                GameManager.Instance?.Events.Publish(new MemoryPresentationChanged(
                    "memory-stage13", "memory.stage13.title", "ending.hint.03", 0, 1, true));
            if (stage >= MaximumLoopCount) director?.SetMovementSuppressed(true);
        }

        private IEnumerator FrameEmptyPath()
        {
            var camera = Camera.main;
            if (camera == null || player == null || trauma == null) yield break;
            Behaviour follow = null;
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour.GetType().Name != "StageCameraBounds") continue;
                follow = behaviour;
                break;
            }

            if (follow != null) follow.enabled = false;
            var until = Time.time + cameraHintSeconds;
            while (Time.time < until)
            {
                var position = camera.transform.position;
                position.x = (player.position.x + trauma.position.x) * 0.5f;
                camera.transform.position = position;
                yield return null;
            }
            if (follow != null) follow.enabled = true;
        }

        private void StartDirectionalMusic()
        {
            if (directionalMusic != null) return;
            directionalMusic = gameObject.AddComponent<AudioSource>();
            directionalMusic.loop = true;
            directionalMusic.playOnAwake = false;
            directionalMusic.volume = 0.02f * AudioRuntime.BgmVolume;
            const int sampleRate = 22050;
            var samples = new float[sampleRate * 2];
            for (var index = 0; index < samples.Length; index++)
                samples[index] = Mathf.Sin(2f * Mathf.PI * 110f * index / sampleRate) * 0.04f;
            var clip = AudioClip.Create("Stage13DirectionalTone", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            directionalMusic.clip = clip;
            directionalMusic.Play();
        }

        private void UpdateDirectionalMusic(float distance)
        {
            if (directionalMusic == null || player == null || trauma == null) return;
            directionalMusic.panStereo = Mathf.Sign(trauma.position.x - player.position.x) * 0.65f;
            var approach = 1f - Mathf.InverseLerp(lowerWeaponDistance, collapseDistance, distance);
            directionalMusic.volume = Mathf.Lerp(0.02f, 0.12f, approach) * AudioRuntime.BgmVolume;
            directionalMusic.pitch = Mathf.Lerp(0.8f, 1.1f, approach);
        }

        private void ApplyLight(float distance)
        {
            var approach = 1f - Mathf.InverseLerp(lowerWeaponDistance, collapseDistance, distance);
            if (globalLight != null)
            {
                globalLight.color = Color.Lerp(collapseLightColor, stableLightColor, approach);
                globalLight.intensity = Mathf.Lerp(collapseLightIntensity, stableLightIntensity, approach);
            }
            // StageVisualBootstrap은 Sky를 의도적으로 Unlit으로 유지한다. 따라서 Global Light만으로는
            // 화면 대부분을 차지하는 하늘이 밝게 남으므로 같은 보간값을 Sprite tint에도 적용한다.
            if (skyBackground != null)
                skyBackground.color = Color.Lerp(collapseLightColor, stableLightColor, approach);
        }

        private IEnumerator CompleteEnding()
        {
            endingStarted = true;
            GameManager.Instance?.Events.Publish(new MemoryPresentationChanged(
                "memory-stage13", "memory.stage13.title", FarewellKey, 0, 1, true));
            yield return new WaitForSeconds(farewellSeconds);
            GameManager.Instance?.Events.Publish(new MemoryPresentationChanged(
                "memory-stage13", "memory.stage13.title", CreditKey, 0, 1, true));
            yield return new WaitForSeconds(creditSeconds);
            yield return FadeOut();
            GameManager.Instance?.Events.Publish(new MemoryPresentationChanged(
                "memory-stage13", "memory.stage13.title", string.Empty, 0, 0, false));
            flow?.CompleteEnding();
        }

        private IEnumerator FadeOut()
        {
            var camera = Camera.main;
            if (camera == null) yield break;
            var fade = new GameObject("Stage13Fade");
            var renderer = fade.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            renderer.sortingOrder = short.MaxValue;
            fade.transform.localScale = new Vector3(camera.orthographicSize * 2f * camera.aspect, camera.orthographicSize * 2f, 1f);
            for (var elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.deltaTime)
            {
                fade.transform.position = camera.transform.position + Vector3.forward;
                renderer.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            renderer.color = Color.black;
        }

        private void SetPrompt(bool visible)
        {
            if (promptVisible == visible) return;
            promptVisible = visible;
            GameManager.Instance?.Events.Publish(new InteractionPromptChanged(visible, "E", "prompt.ending.lower_weapon"));
        }

        private static void SetSignal(string objectName, bool active)
        {
            var signal = GameObject.Find(objectName);
            if (signal != null) signal.SetActive(active);
        }

        private void ResolveReferences()
        {
            // UnityEngine.Object의 "fake null"은 ??=에서 null로 취급되지 않는다.
            // 씬의 비어 있는 직렬화 참조도 확실히 복구하도록 Unity의 overloaded null 비교를 사용한다.
            if (director == null) director = FindAnyObjectByType<ContaminationDirector>();
            if (flow == null) flow = FindAnyObjectByType<SceneFlowController>();
            if (combat == null) combat = FindAnyObjectByType<PlayerCombat>();
            if (traumaContact == null) traumaContact = FindAnyObjectByType<TraumaContactHandler>();
            if (player == null) player = FindAnyObjectByType<PlayerController>()?.transform;
            if (trauma == null) trauma = FindAnyObjectByType<TraumaChaseActor>(FindObjectsInactive.Include)?.transform;
            if (trauma != null && !trauma.gameObject.activeSelf && !TraumaLoopRespawning) trauma.gameObject.SetActive(true);
            if (globalLight == null) globalLight = FindAnyObjectByType<Light2D>();
            if (skyBackground == null)
                skyBackground = GameObject.Find("StageSkyBackground")?.GetComponent<SpriteRenderer>();
            if (interact == null && player != null)
            {
                interact = player.GetComponent<PlayerInput>()?.actions?.FindAction("Interact");
                interact?.Enable();
            }
        }
    }
}
