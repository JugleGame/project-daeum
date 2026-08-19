using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Daeume.Contamination
{
    [CreateAssetMenu(fileName = "ContaminationVariant", menuName = "Daeume/Contamination Variant")]
    public sealed class ContaminationVariantData : ScriptableObject
    {
        [SerializeField] private string variantId = string.Empty;
        [SerializeField] private string echoOverlayScene = string.Empty;
        [SerializeField] private string intrusionOverlayScene = string.Empty;
        [SerializeField, Min(0.1f)] private float targetChaseSeconds = 30f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 6f;
        [SerializeField, Min(0.1f)] private float minDistance = 2f;
        [SerializeField, Min(0.2f)] private float maxDistance = 7f;
        [SerializeField] private List<string> declaredTeleportMarkerIds = new();

        public string VariantId => variantId;
        public string EchoOverlayScene => echoOverlayScene;
        public string IntrusionOverlayScene => intrusionOverlayScene;
        public float TargetChaseSeconds => Mathf.Max(0.1f, targetChaseSeconds);
        public float ChaseSpeed => Mathf.Max(0.1f, chaseSpeed);
        public float MinDistance => Mathf.Max(0.1f, minDistance);
        public float MaxDistance => Mathf.Max(MinDistance + 0.1f, maxDistance);
        public IReadOnlyList<string> DeclaredTeleportMarkerIds => declaredTeleportMarkerIds;

        public void Configure(
            string id,
            string echoScene,
            string intrusionScene,
            float chaseSeconds,
            float speed,
            float minimumDistance,
            float maximumDistance,
            IEnumerable<string> teleportMarkerIds = null)
        {
            variantId = id ?? string.Empty;
            echoOverlayScene = echoScene ?? string.Empty;
            intrusionOverlayScene = intrusionScene ?? string.Empty;
            targetChaseSeconds = Mathf.Max(0.1f, chaseSeconds);
            chaseSpeed = Mathf.Max(0.1f, speed);
            minDistance = Mathf.Max(0.1f, minimumDistance);
            maxDistance = Mathf.Max(minDistance + 0.1f, maximumDistance);
            declaredTeleportMarkerIds = teleportMarkerIds == null
                ? new List<string>()
                : new List<string>(teleportMarkerIds);
        }

        public bool ValidateData(out string error)
        {
            if (string.IsNullOrWhiteSpace(variantId)) return Fail("VariantId is required.", out error);
            if (string.IsNullOrWhiteSpace(echoOverlayScene)) return Fail("Echo overlay scene is required.", out error);
            if (string.IsNullOrWhiteSpace(intrusionOverlayScene)) return Fail("Intrusion overlay scene is required.", out error);
            if (targetChaseSeconds <= 0f) return Fail("TargetChaseSeconds must be positive.", out error);
            if (chaseSpeed <= 0f) return Fail("ChaseSpeed must be positive.", out error);
            if (minDistance <= 0f || maxDistance <= minDistance) return Fail("Distance bounds are invalid.", out error);
            if (declaredTeleportMarkerIds.Any(string.IsNullOrWhiteSpace)) return Fail("Teleport marker ids cannot be empty.", out error);
            if (declaredTeleportMarkerIds.Distinct().Count() != declaredTeleportMarkerIds.Count) return Fail("Teleport marker ids must be unique.", out error);
            error = string.Empty;
            return true;
        }

        public string OverlayFor(PressureStage stage)
        {
            return stage switch
            {
                PressureStage.Echo => echoOverlayScene,
                PressureStage.Intrusion => intrusionOverlayScene,
                _ => string.Empty
            };
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
