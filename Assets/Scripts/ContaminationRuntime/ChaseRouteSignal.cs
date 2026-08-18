using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public enum ChaseSignalShape
    {
        None,
        LeftArrow,
        Barrier,
        ExitDoor
    }

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
