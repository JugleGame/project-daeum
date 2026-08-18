using UnityEngine;

namespace Daeume.Stage
{
    public enum StageMarkerKind
    {
        Start,
        FallRecovery,
        RemnantSpawn,
        EncounterTrigger,
        EncounterExit,
        MemoryAnchor,
        ChaseStart,
        Escape
    }

    public sealed class StageMarker : MonoBehaviour
    {
        [SerializeField] private string markerId = string.Empty;
        [SerializeField] private StageMarkerKind kind;

        public string MarkerId => markerId;
        public StageMarkerKind Kind => kind;

        private void OnDrawGizmos()
        {
            Gizmos.color = ColorFor(kind);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.left * 0.4f, transform.position + Vector3.right * 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.4f, transform.position + Vector3.up * 0.4f);
        }

        private static Color ColorFor(StageMarkerKind markerKind)
        {
            return markerKind switch
            {
                StageMarkerKind.Start => Color.green,
                StageMarkerKind.FallRecovery => new Color(1f, 0.5f, 0f),
                StageMarkerKind.RemnantSpawn => Color.red,
                StageMarkerKind.EncounterTrigger => Color.yellow,
                StageMarkerKind.EncounterExit => Color.cyan,
                StageMarkerKind.MemoryAnchor => Color.magenta,
                StageMarkerKind.ChaseStart => new Color(0.7f, 0.2f, 1f),
                StageMarkerKind.Escape => Color.white,
                _ => Color.gray
            };
        }
    }
}
