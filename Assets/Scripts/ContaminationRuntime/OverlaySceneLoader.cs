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
    /// 그래서 기저 레벨은 계속 열어 둔 채, 변화한 부분만 얹는다.
    /// 스펙도 "오버레이 적재는 기저 레벨을 닫지 않는다"를 명시적으로 요구한다.
    ///
    /// 오버레이를 담는 방법은 두 가지이고, 이 클래스가 알아서 고른다(#12):
    /// 1. <b>씬 안 루트 오브젝트</b>(권장) — 스테이지 씬에 오버레이 이름과 같은 루트를 두고 켜고 끈다.
    ///    한 씬에서 기저와 오버레이를 같이 보며 저작할 수 있고, 씬 파일 수가 스테이지당 1개로 줄며,
    ///    씬 적재/해제 비용도 사라진다. Stage 02부터 이 방식이다.
    /// 2. <b>추가(Additive) 씬</b>(구방식) — 오버레이 이름과 같은 씬을 얹는다. Stage 01이 아직 이 방식이라
    ///    폴백으로 남겨 둔다. Stage 01을 1번으로 옮기면 이 폴백은 지울 수 있다.
    ///
    /// 어느 쪽이든 기저 레벨은 닫지 않는다 — 그 계약이 이 클래스 밖으로 새지 않게 여기서만 처리한다.
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

            // 1번 방식: 스테이지 씬 안에 같은 이름의 오버레이 루트가 있으면 그것만 켜고 끈다.
            // 씬을 하나도 건드리지 않으므로 기저 레벨은 정의상 닫히지 않는다.
            var overlayRoot = FindOverlayRoot(sceneName);
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(load);
                yield break;
            }

            // 2번 방식(폴백): 같은 이름의 씬을 추가로 얹거나 내린다.
            var scene = SceneManager.GetSceneByName(sceneName);
            if (load)
            {
                // 이미 올라와 있으면 다시 올리지 않는다. 중복 적재는 같은 지형이 두 벌 생기는 사고다.
                if (!scene.isLoaded) yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                yield break;
            }

            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(sceneName);
        }

        /// <summary>
        /// 열려 있는 씬들에서 오버레이 이름과 같은 <b>루트</b> 오브젝트를 찾는다. 없으면 null.
        /// </summary>
        /// <remarks>
        /// 루트만 본다. 아무 자식 오브젝트나 이름이 겹쳤다고 오버레이로 오인하면
        /// 엉뚱한 물체가 통째로 꺼지는 사고가 난다 — 오버레이 루트는 씬 최상단에 둔다는 규약으로 막는다.
        /// GetRootGameObjects는 꺼져 있는 루트도 돌려주므로, 평소 비활성인 오버레이도 찾을 수 있다.
        /// </remarks>
        public static GameObject FindOverlayRoot(string overlayName)
        {
            if (string.IsNullOrWhiteSpace(overlayName)) return null;

            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == overlayName) return root;
                }
            }

            return null;
        }
    }
}
