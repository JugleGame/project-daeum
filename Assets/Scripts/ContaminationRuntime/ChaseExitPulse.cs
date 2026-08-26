using Daeume.Core;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 추격이 시작되면 탈출 통로를 맥동시켜 "저기로 도망쳐라"를 알린다.
    /// </summary>
    /// <remarks>
    /// 왜 필요한가: 추격이 시작되는 순간 플레이어는 지금까지 나아가던 오른쪽의 반대인 왼쪽 끝으로
    /// 달려야 한다. 화면에 보이지도 않는 목적지를 글자 없이 알리려면, 회상 지점과 같은 맥동을
    /// 탈출구에도 붙여 "빛나는 것 = 목표"라는 규칙을 한 번 더 쓰는 것이 가장 짧다.
    ///
    /// 추격 전에는 맥동하지 않는다. 아직 갈 수 없는 곳을 미리 가리키면 탐색 동선을 망친다.
    /// </remarks>
    public sealed class ChaseExitPulse : SpritePulse
    {
        private bool chasing;

        protected override bool ShouldPulse => chasing;

        // GameManager가 이 오브젝트보다 늦게 만들어지는 순서가 실제로 있어, 두 곳에서 구독을 시도한다
        // (StageHudPresenter와 같은 이유다).
        private void OnEnable() => Connect();
        private void Start() => Connect();

        protected override void OnDisable()
        {
            base.OnDisable();
            GameManager.Instance?.Events.Unsubscribe<ChaseStateChanged>(OnChase);
        }

        private void Connect()
        {
            if (GameManager.Instance == null) return;

            // 중복 구독을 막기 위해 먼저 해제한다.
            GameManager.Instance.Events.Unsubscribe<ChaseStateChanged>(OnChase);
            GameManager.Instance.Events.Subscribe<ChaseStateChanged>(OnChase);
        }

        private void OnChase(ChaseStateChanged value) => chasing = value.Active;
    }
}
