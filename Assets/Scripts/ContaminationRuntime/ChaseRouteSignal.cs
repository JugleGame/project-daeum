using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>신호의 형태. 색과 별개로 "모양"을 반드시 갖게 하려고 enum으로 선언한다.</summary>
    public enum ChaseSignalShape
    {
        None,
        LeftArrow,
        Barrier,
        ExitDoor
    }

    /// <summary>
    /// 추격 경로를 알려 주는 신호(왼쪽으로 / 막힘 / 출구). (spec-006, spec-013)
    ///
    /// 핵심 규칙: 필수 정보를 색만으로 전달하지 않는다.
    /// 색약이거나 모니터 설정이 다른 사람도 같은 정보를 얻어야 하므로,
    /// 모든 신호가 색(Color) + 형태(Shape) + 기호(Symbol)를 함께 갖도록 데이터 구조 자체로 강제한다.
    /// HasNonColorCue가 그 조건을 검사하며, 테스트가 이 값을 확인한다.
    /// </summary>
    public sealed class ChaseRouteSignal : MonoBehaviour
    {
        [SerializeField] private string signalId = string.Empty;
        [SerializeField] private ChaseSignalShape shape;
        [SerializeField] private string symbol = string.Empty;
        [SerializeField] private Color color = Color.white;

        public string SignalId => signalId;
        public ChaseSignalShape Shape => shape;
        public string Symbol => symbol;
        public Color Color => color;
        public bool HasNonColorCue => shape != ChaseSignalShape.None && !string.IsNullOrWhiteSpace(symbol);

        public void Configure(string id, ChaseSignalShape signalShape, string textSymbol, Color signalColor)
        {
            signalId = id ?? string.Empty;
            shape = signalShape;
            symbol = textSymbol ?? string.Empty;
            color = signalColor;
        }
    }
}
