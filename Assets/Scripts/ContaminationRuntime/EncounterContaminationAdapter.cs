using Daeume.Core;
using Daeume.Encounter;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 전투(Encounter) 결과를 오염 감독에게 전달하는 연결부다.
    ///
    /// Encounter 쪽은 오염을 몰라야 하고 감독은 전투를 몰라야 하므로,
    /// 이벤트를 구독해 옮겨 주는 얇은 어댑터를 따로 뒀다. 모듈 간 결합을 늘리지 않는 좋은 방식이다.
    ///
    /// 검토 메모: 구독을 Awake에서만 하고 있어, GameManager가 나중에 생성되는 순서에서는 놓칠 수 있다.
    /// 같은 문제를 겪은 OverlaySceneLoader는 Awake/OnEnable/Start 세 곳에서 재시도하도록 고쳤다.
    /// 이 어댑터도 동일한 보완이 필요해지면 그 방식을 그대로 적용하면 된다.
    /// </summary>
    public sealed class EncounterContaminationAdapter : MonoBehaviour
    {
        [SerializeField] private ContaminationDirector director;

        private void Awake()
        {
            GameManager.Instance?.Events.Subscribe<EncounterStateChanged>(HandleEncounterStateChanged);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.Events.Unsubscribe<EncounterStateChanged>(HandleEncounterStateChanged);
        }

        public void Configure(ContaminationDirector value) => director = value;

        public void HandleEncounterStateChanged(EncounterStateChanged message)
        {
            if (message.State == EncounterState.Cleared) director?.HandleEncounterCleared();
        }
    }
}
