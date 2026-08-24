using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 오염 공간(Overlay)을 기저 지형 위에 덧씌우거나 걷어 내는 담당자다. (spec-006, spec-015)
    ///
    /// 왜 "덧씌우기"인가:
    /// 오염된 공간을 따로 만들어 통째로 교체하면 같은 지형을 두 벌 관리해야 한다.
    /// 그래서 기저 레벨은 그대로 둔 채, 변화한 부분만 얹는다.
    /// 스펙도 "오버레이 적재는 기저 레벨을 닫지 않는다"를 명시적으로 요구한다.
    ///
    /// 오버레이는 <b>StageNN_Base 씬 안의 루트 GameObject</b>다. 그것을 켜고 끄는 것이 전부다(#38).
    /// 예전에는 오버레이를 별도 씬 파일로 두고 additive로 얹었고 이 클래스에 그 폴백이 남아 있었다.
    /// 지웠다. 이유는 세 가지다:
    /// 1. 스테이지당 씬 3개 × 13스테이지 = 39씬. 적재/해제 비용이 그만큼 늘어난다.
    /// 2. 오버레이 좌표를 기저 지형을 못 보는 상태에서 맞춰야 한다(Stage 02를 실제로 눈감고 배치했다).
    /// 3. 실제 내용물은 발판 몇 개뿐인데 씬 파일 + meta가 따라붙는다.
    /// 폴백을 남겨 두면 다음 저작자가 "되니까" 또 씬을 만든다. 경로를 하나만 남겨 그 여지를 없앤다.
    ///
    /// 씬을 하나도 건드리지 않으므로 "기저 레벨을 닫지 않는다"는 계약이 구조적으로 보장된다.
    /// </summary>
    public sealed class OverlaySceneLoader : MonoBehaviour
    {
        private GameManager subscribedManager;

        public string LastRequestedOverlay { get; private set; } = string.Empty;
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

        /// <summary>
        /// 요청을 그 자리에서 처리한다.
        /// </summary>
        /// <remarks>
        /// 예전에는 요청을 큐에 세우고 코루틴으로 하나씩 처리했다. 씬 적재가 프레임을 넘겨 끝나기 때문에
        /// "Echo 적재 → Echo 해제 → Intrusion 적재"처럼 같은 프레임에 요청이 몰리면 서로 뒤엉켰다
        /// (회상 완료 시 압박이 Stable → Echo → Intrusion으로 연속 변해 실제로 발생했다).
        /// 이제 처리가 SetActive 한 번이라 동기적으로 끝난다 — 뒤엉킬 틈 자체가 없어 큐를 지웠다.
        /// </remarks>
        public void HandleRequest(OverlaySceneLoadRequested request) => ApplyRequest(request.SceneName, request.Load);

        /// <summary>오버레이 루트를 켜거나 끈다. 이름이 비었거나 루트가 없으면 아무 일도 하지 않는다.</summary>
        public void ApplyRequest(string overlayName, bool load)
        {
            LastRequestedOverlay = overlayName ?? string.Empty;
            LastRequestWasLoad = load;

            var overlayRoot = FindOverlayRoot(overlayName);
            if (overlayRoot != null) overlayRoot.SetActive(load);
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
