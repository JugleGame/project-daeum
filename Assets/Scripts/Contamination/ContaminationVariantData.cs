using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Daeume.Contamination
{
    /// <summary>
    /// 한 스테이지의 오염 공간 설정을 담는 데이터 에셋. (spec-006)
    ///
    /// 어떤 오버레이를 쓸지, 목표 추격 시간은 몇 초인지, 추격자 속도와 거리 한계는 얼마인지를 선언한다.
    /// 이 값들이 코드가 아니라 에셋에 있는 이유는, 스테이지마다 다르게 저작해야 하고
    /// 기획자가 코드를 만지지 않고 조정할 수 있어야 하기 때문이다.
    ///
    /// [CreateAssetMenu]: 유니티 에디터의 Assets > Create 메뉴에 항목을 추가해 준다.
    /// 이 속성이 있어야 데이터 파일을 마우스로 만들 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "ContaminationVariant", menuName = "Daeume/Contamination Variant")]
    public sealed class ContaminationVariantData : ScriptableObject
    {
        [SerializeField] private string variantId = string.Empty;
        [SerializeField] private string echoOverlayName = string.Empty;
        [SerializeField] private string intrusionOverlayName = string.Empty;
        [SerializeField, Min(0.1f)] private float targetChaseSeconds = 30f;  // director가 목표로 삼는 추격 길이
        [SerializeField, Min(0.1f)] private float chaseSpeed = 6f;
        [SerializeField, Min(0.1f)] private float minDistance = 2f;          // 이보다 가까워지면 추격자를 물린다
        [SerializeField, Min(0.2f)] private float maxDistance = 7f;          // 이보다 멀어지면 다시 붙인다
        [SerializeField, Min(0f)] private float chaseLookaheadUnits = 3f;    // spec-014: 추격 카메라 선행 시야 거리
        [SerializeField] private List<string> declaredTeleportMarkerIds = new();

        public string VariantId => variantId;
        public string EchoOverlayName => echoOverlayName;
        public string IntrusionOverlayName => intrusionOverlayName;

        // 읽을 때도 한 번 더 하한을 건다.
        // 인스펙터의 Min 속성은 에디터 입력만 막을 뿐, 코드로 잘못된 값이 들어오는 것은 막지 못하기 때문이다.
        public float TargetChaseSeconds => Mathf.Max(0.1f, targetChaseSeconds);
        public float ChaseSpeed => Mathf.Max(0.1f, chaseSpeed);
        public float MinDistance => Mathf.Max(0.1f, minDistance);

        // 최대 거리는 항상 최소 거리보다 크게 강제한다. 뒤집히면 추격자가 붙었다 떨어졌다를 무한 반복한다.
        public float MaxDistance => Mathf.Max(MinDistance + 0.1f, maxDistance);

        /// <summary>
        /// 추격 카메라가 진행 방향(좌향 도주라면 왼쪽)으로 미리 보여 줘야 하는 거리. (spec-014)
        /// 이 거리보다 가까운 곳에 생존 경로 장애물을 처음 등장시키면 안 된다 — 여기서는 값만 선언하고
        /// 그 배치 규칙 준수는 레벨 저작(B)의 몫이다.
        /// </summary>
        public float ChaseLookaheadUnits => Mathf.Max(0f, chaseLookaheadUnits);

        /// <summary>
        /// 연출 목적의 순간이동이 허용된 지점 목록.
        /// </summary>
        /// <remarks>
        /// spec-006은 "director는 트라우마를 순간이동시키지 않는다. 예외는 선언된 지점뿐"이라고 규정한다.
        /// 목록을 데이터로 두면 "선언되지 않은 순간이동 0회"를 테스트로 검증할 수 있다.
        /// </remarks>
        public IReadOnlyList<string> DeclaredTeleportMarkerIds => declaredTeleportMarkerIds;

        /// <summary>테스트나 툴에서 값을 한 번에 채워 넣는다.</summary>
        public void Configure(
            string id,
            string echoOverlay,
            string intrusionOverlay,
            float chaseSeconds,
            float speed,
            float minimumDistance,
            float maximumDistance,
            IEnumerable<string> teleportMarkerIds = null,
            float lookaheadUnits = 3f)
        {
            variantId = id ?? string.Empty;
            echoOverlayName = echoOverlay ?? string.Empty;
            intrusionOverlayName = intrusionOverlay ?? string.Empty;
            targetChaseSeconds = Mathf.Max(0.1f, chaseSeconds);
            chaseSpeed = Mathf.Max(0.1f, speed);
            minDistance = Mathf.Max(0.1f, minimumDistance);
            maxDistance = Mathf.Max(minDistance + 0.1f, maximumDistance);
            chaseLookaheadUnits = Mathf.Max(0f, lookaheadUnits);
            declaredTeleportMarkerIds = teleportMarkerIds == null
                ? new List<string>()
                : new List<string>(teleportMarkerIds);
        }

        /// <summary>
        /// 데이터가 온전한지 검사하고, 문제가 있으면 이유를 문자열로 돌려준다.
        /// </summary>
        /// <remarks>
        /// 예외를 던지지 않고 결과+메시지를 돌려주는 형태라 EditMode 테스트에서 그대로 쓰기 좋다.
        /// 잘못된 데이터를 플레이 중에 발견하면 원인을 찾기 어렵기 때문에, 이런 사전 검사는 비용 대비 효과가 크다.
        /// </remarks>
        public bool ValidateData(out string error)
        {
            if (string.IsNullOrWhiteSpace(variantId)) return Fail("VariantId is required.", out error);
            if (string.IsNullOrWhiteSpace(echoOverlayName)) return Fail("Echo overlay name is required.", out error);
            if (string.IsNullOrWhiteSpace(intrusionOverlayName)) return Fail("Intrusion overlay name is required.", out error);
            if (targetChaseSeconds <= 0f) return Fail("TargetChaseSeconds must be positive.", out error);
            if (chaseSpeed <= 0f) return Fail("ChaseSpeed must be positive.", out error);
            if (minDistance <= 0f || maxDistance <= minDistance) return Fail("Distance bounds are invalid.", out error);
            if (declaredTeleportMarkerIds.Any(string.IsNullOrWhiteSpace)) return Fail("Teleport marker ids cannot be empty.", out error);
            if (declaredTeleportMarkerIds.Distinct().Count() != declaredTeleportMarkerIds.Count) return Fail("Teleport marker ids must be unique.", out error);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 압박 단계에 해당하는 오버레이 루트 오브젝트 이름. Stable에는 오버레이가 없다.
        /// </summary>
        /// <remarks>
        /// 이름이 가리키는 것은 <b>StageNN_Base 씬 안의 루트 GameObject</b>다(#38). 별도 씬 이름이 아니다.
        /// 씬으로 저작하면 스테이지당 씬이 3개로 불어나고, 기저 지형을 못 보는 채로 오버레이 좌표를
        /// 맞춰야 한다. 그래서 필드 이름에서 "Scene"을 걷어냈다 — 이름이 남아 있으면 다음 저작자가 또 씬을 만든다.
        /// </remarks>
        public string OverlayFor(PressureStage stage)
        {
            return stage switch
            {
                PressureStage.Echo => echoOverlayName,
                PressureStage.Intrusion => intrusionOverlayName,
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
