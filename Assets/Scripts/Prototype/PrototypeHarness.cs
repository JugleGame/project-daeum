using Daeume.Core;
using Daeume.Flow;
using UnityEngine;

namespace Daeume.Prototype
{
    public sealed class PrototypeHarness : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private SceneFlowController flow;
        private GUIStyle labelStyle;

        private void Start()
        {
            if (player == null || flow == null || GameManager.Instance == null)
            {
                return;
            }

            flow.SaveChaseCheckpoint("Stage01_Chase", player.position, 3, string.Empty);
            GameManager.Instance.SetStageState(StageState.Memory);
            GameManager.Instance.SetStageState(StageState.Chase);
        }

        private void OnGUI()
        {
            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };
            GUI.Box(new Rect(12, 12, 600, 126), string.Empty);
            GUI.Label(new Rect(24, 18, 570, 28), "ROLE A 기능 프로토타입", labelStyle);
            GUI.Label(new Rect(24, 48, 570, 28), "A/D 이동 · Space 점프 · K 붙잡기 · J 공격 · E 상호작용", labelStyle);
            GUI.Label(new Rect(24, 78, 570, 28), "주황 플랫폼 → 청록 벽 → 빨강 적 → 노랑 상호작용 → 검정 Trauma", labelStyle);
            var state = GameManager.Instance == null ? "None" : GameManager.Instance.StageState.ToString();
            GUI.Label(new Rect(24, 106, 570, 28), $"StageState: {state} · Trauma 접촉 시 시작 위치로 복귀", labelStyle);
        }
    }
}
