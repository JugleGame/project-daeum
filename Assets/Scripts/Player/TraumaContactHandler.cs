using System.Collections;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Player
{
    /// <summary>
    /// 트라우마에 닿았을 때의 "붙잡히는 연출 → 실패 → 체크포인트 복귀"를 담당한다. (spec-003, spec-011)
    ///
    /// 왜 즉사가 아니라 연출인가:
    /// 기획 결정 1번은 "보이고 읽히는 실패"다. 화면 밖에서 갑자기 죽으면 플레이어는 왜 죽었는지 모른다.
    /// 그래서 반드시 화면 안에서 시작하고, 정해진 길이의 연출을 보여 준 뒤 실패로 넘긴다.
    /// 또 트라우마 접촉은 체력을 깎지 않는다 — 추격에 체력 관리를 끌어들이지 않기 위한 결정이다.
    /// </summary>
    public sealed class TraumaContactHandler : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float traumaGrabSeconds = 1f;   // spec-003의 TraumaGrabSeconds
        [SerializeField] private string chaseCheckpointId = "Stage01_Chase";
        [SerializeField] private PlayerController controller;

        public bool GrabInProgress { get; private set; }
        public float TraumaGrabSeconds => traumaGrabSeconds;
        public bool ContactFailureEnabled { get; private set; } = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var source = other.GetComponentInParent<TraumaContactSource>();

            // IsOnScreen 검사가 핵심이다.
            // 화면 밖에서 몰래 따라온 트라우마에 닿아 실패하면 "보이지 않는 즉사"가 되어 기획이 금지한 경험이 된다.
            if (ContactFailureEnabled && source != null && IsOnScreen(source.transform, Camera.main))
            {
                BeginGrab();
            }
        }

        /// <summary>붙잡기 연출을 시작한다. 이미 진행 중이면 무시한다.</summary>
        public bool BeginGrab()
        {
            if (!ContactFailureEnabled)
            {
                return false;
            }

            if (GrabInProgress)
            {
                // 콜라이더가 여러 번 겹치면 이 함수가 연속 호출된다.
                // 이 가드가 없으면 연출이 여러 겹으로 겹쳐 실패가 두 번 발생한다.
                return false;
            }

            StartCoroutine(GrabSequence());
            return true;
        }

        /// <summary>Stage 13에서는 접촉해도 붙잡기·실패를 발생시키지 않는다.</summary>
        public void SetContactFailureEnabled(bool enabled) => ContactFailureEnabled = enabled;

        /// <summary>
        /// 코루틴: "시간이 흐르는 동안 기다렸다가 이어서 실행"하는 유니티 문법이다.
        /// yield return new WaitForSeconds(n)이 n초 대기를 뜻한다.
        /// </summary>
        private IEnumerator GrabSequence()
        {
            GrabInProgress = true;

            // 연출 시작을 알린다. 오디오·카메라(C)가 이 신호로 효과음과 압박 연출을 낸다.
            GameManager.Instance?.Events.Publish(new TraumaGrabStarted(traumaGrabSeconds));

            if (controller != null)
            {
                // 연출 중 플레이어 입력은 무시한다(spec-003). 붙잡힌 뒤 도망치는 조작이 통하면 안 된다.
                controller.InputEnabled = false;
            }

            // 재시도마다 같은 길이여야 한다. Time.deltaTime 누적이 아니라 고정 대기라서 결정적이다. 적합하다.
            yield return new WaitForSeconds(traumaGrabSeconds);

            GameManager.Instance?.Fail(StageFailureCause.TraumaGrabCompleted);
            GameManager.Instance?.Events.Publish(new ChaseCheckpointRestoreRequested(chaseCheckpointId));

            // 수정: 조작 잠금을 여기서 반드시 풀어 준다.
            // 원래는 체크포인트 복귀(PlayerRestoreRequested)가 대신 풀어 주기를 기대했는데,
            // 체크포인트 ID가 맞지 않아 복귀가 취소되면 플레이어가 영원히 조작 불능으로 남는다.
            // 복귀 경로가 다시 켜 주더라도 중복으로 켜는 것은 아무 부작용이 없으므로 여기서 보장한다.
            if (controller != null)
            {
                controller.InputEnabled = true;
            }

            GrabInProgress = false;
        }

        /// <summary>대상이 카메라 화면 안에 있는지 검사한다.</summary>
        /// <remarks>
        /// WorldToViewportPoint는 월드 좌표를 "화면 비율 좌표"로 바꾼다.
        /// x, y가 0~1 사이면 화면 안, z가 양수면 카메라 앞쪽이라는 뜻이다.
        /// z 검사를 빼면 카메라 뒤에 있는 대상도 화면 안으로 잘못 판정된다.
        /// </remarks>
        public static bool IsOnScreen(Transform source, Camera camera)
        {
            if (source == null || camera == null)
            {
                return false;
            }

            var point = camera.WorldToViewportPoint(source.position);
            return point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
        }
    }
}
