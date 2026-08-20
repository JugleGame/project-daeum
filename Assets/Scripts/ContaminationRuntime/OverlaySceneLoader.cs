using System.Collections;
using System.Collections.Generic;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 오염 공간(Overlay) 씬을 기저 씬 위에 덧씌우거나 걷어 내는 담당자다. (spec-006, spec-015)
    ///
    /// 왜 "덧씌우기"인가:
    /// 오염된 공간을 따로 만들어 통째로 교체하면 같은 지형을 두 벌 관리해야 한다.
    /// 그래서 기저 씬(Stage01_Base)은 계속 열어 둔 채, 변화한 부분만 담은 씬을 추가(Additive)로 얹는다.
    /// 스펙도 "오버레이 적재는 기저 레벨을 닫지 않는다"를 명시적으로 요구한다.
    ///
    /// 이 스크립트는 씬 API를 직접 부르는 유일한 창구다.
    /// 다른 시스템은 이벤트(OverlaySceneLoadRequested)만 발행하고 씬을 직접 건드리지 않는다(spec-015).
    /// </summary>
    public sealed class OverlaySceneLoader : MonoBehaviour
    {
        private GameManager subscribedManager;

        // 처리 대기 중인 요청 줄(큐)과, 지금 처리 중인지 여부.
        private readonly Queue<(string sceneName, bool load)> pending = new();
        private bool processing;

        public string LastRequestedScene { get; private set; } = string.Empty;
        public bool LastRequestWasLoad { get; private set; }

        // Awake/OnEnable/Start 세 곳에서 구독을 시도한다.
        // Persistent와 Stage 씬을 함께 열었을 때 GameManager가 나중에 만들어지는 순서가 실제로 존재했고,
        // 그때 구독이 누락돼 오버레이가 뜨지 않는 버그가 있었다. 세 시점 모두 시도해 그 구멍을 막는다.
        private void Awake() => EnsureSubscribed();
        private void OnEnable() => EnsureSubscribed();
        private void Start() => EnsureSubscribed();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void EnsureSubscribed()
        {
            var manager = GameManager.Instance;
            // 이미 같은 매니저에 붙어 있으면 중복 구독하지 않는다(이벤트가 두 번 처리되는 것을 방지).
            if (manager == null || manager == subscribedManager) return;
            Unsubscribe();
            subscribedManager = manager;
            subscribedManager.Events.Subscribe<OverlaySceneLoadRequested>(HandleRequest);
        }

        private void Unsubscribe()
        {
            if (subscribedManager == null) return;
            subscribedManager.Events.Unsubscribe<OverlaySceneLoadRequested>(HandleRequest);
            subscribedManager = null;
        }

        /// <summary>요청을 줄 세워 두고, 앞의 작업이 끝나면 순서대로 처리한다.</summary>
        /// <remarks>
        /// 수정 이유(중요):
        /// 예전에는 요청이 올 때마다 코루틴을 따로 시작했다. 그러면 같은 프레임에
        /// "Echo 적재 → Echo 해제 → Intrusion 적재"처럼 요청이 몰릴 때 서로 뒤엉킨다.
        /// 아직 적재가 끝나지 않은 씬을 해제하려 해서 "Scene to unload is invalid" 오류가 나거나,
        /// 해제가 늦게 끝나 방금 올린 씬이 도로 내려가는 사고가 생긴다.
        /// 회상 완료 시 압박이 Stable → Echo → Intrusion으로 연속 변하므로 이 상황이 실제로 발생한다.
        /// 한 번에 하나씩만 처리하도록 줄을 세워 순서를 보장한다.
        /// </remarks>
        public void HandleRequest(OverlaySceneLoadRequested request)
        {
            pending.Enqueue((request.SceneName, request.Load));
            if (!processing)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            processing = true;
            while (pending.Count > 0)
            {
                var (sceneName, load) = pending.Dequeue();
                yield return ApplyRequest(sceneName, load);
            }

            processing = false;
        }

        /// <summary>
        /// 실제 적재/해제를 수행한다. 테스트에서는 이 함수를 직접 호출해 완료 시점을 기다린다.
        /// </summary>
        public IEnumerator ApplyRequest(string sceneName, bool load)
        {
            LastRequestedScene = sceneName ?? string.Empty;
            LastRequestWasLoad = load;
            if (string.IsNullOrWhiteSpace(sceneName)) yield break;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (load)
            {
                // 이미 올라와 있으면 다시 올리지 않는다. 중복 적재는 같은 지형이 두 벌 생기는 사고다.
                if (!scene.isLoaded) yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                yield break;
            }

            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
