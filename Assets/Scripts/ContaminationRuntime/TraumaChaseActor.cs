using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    [RequireComponent(typeof(TraumaContactSource), typeof(CircleCollider2D))]
    /// <summary>
    /// 트라우마 본체. 감독이 내린 지시를 "실행만" 한다. (spec-006)
    ///
    /// 이 클래스에 추격 종료 판단이나 속도 결정 로직이 없는 것이 핵심이다.
    /// 스펙은 "트라우마 액터는 director가 지시한 목표만 실행하며 스스로 추격 종료를 결정하지 않는다"를
    /// 명시적 검증 항목(Test_Chase_DirectorOwnsChaseLength)으로 두고 있다.
    /// 여기에 판단 코드를 추가하면 그 테스트가 깨지고, 스테이지별 페이싱 설계도 무너진다.
    ///
    /// 역할 분담(#12에서 정리):
    /// - <b>X축은 감독의 것</b>이다. 얼마나 붙을지·언제 끝낼지는 여기서 정하지 않는다.
    /// - <b>Y축은 액터의 것</b>이다. 중력을 받고, 벽에 막히면 타고 오르고, 플레이어가 위에 있으면 뛴다.
    ///   높이를 "어떻게" 따라잡을지는 추격 길이 설계와 무관한 이동 문제라 액터가 풀어도 스펙과 충돌하지 않는다.
    ///
    /// 왜 Y를 액터가 갖게 됐나:
    /// 예전에는 Y를 아예 건드리지 않고 자기 높이를 그대로 유지했다. 그러면 좌우 일직선으로만 움직여서,
    /// 플레이어가 발판 아래나 위로 비키기만 해도 트라우마가 영원히 제자리를 맴돌았다(추격이 성립하지 않음).
    /// 그렇다고 목표 지점에 PlayerPosition.y를 그대로 섞으면 플레이어가 점프할 때마다 같이 튀어 오른다
    /// (Test_Trauma_ChaseIgnoresPlayerVerticalMovement가 막는 예전 버그). 그래서 플레이어의 y를 따라가는 대신
    /// 지형을 딛고 스스로 오르내리게 한다 — 플레이어가 점프해도 트라우마는 땅에 남는다.
    ///
    /// [RequireComponent(TraumaContactSource)]: 공격 무효·접촉 판정 표식이 항상 함께 붙도록 강제한다.
    /// </summary>
    public sealed class TraumaChaseActor : MonoBehaviour
    {
        /// <summary>지형 검사에 쓰는 여유 두께. 0이면 바닥에 닿았는지 판정이 프레임마다 깜빡인다.</summary>
        private const float Skin = 0.02f;

        [SerializeField, Min(0f)] private float gravity = 30f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 22f;
        [SerializeField, Min(0f)] private float jumpSpeed = 13f;
        [SerializeField, Min(0f)] private float climbSpeed = 5f;

        // 플레이어가 이만큼 위에 있으면 뛴다. 너무 작게 잡으면 평지에서도 계속 튀어 오른다.
        [SerializeField, Min(0f)] private float jumpTriggerHeight = 0.6f;

        private float verticalVelocity;
        private CircleCollider2D body;

        public ChaseDirectiveIssued LastDirective { get; private set; }
        public int AppliedDirectiveCount { get; private set; }

        /// <summary>지금 지형을 딛고 서 있는지. 테스트와 연출이 읽는다.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>지금 벽을 타고 오르는 중인지.</summary>
        public bool IsClimbing { get; private set; }

        private void Awake() => body = GetComponent<CircleCollider2D>();

        /// <summary>
        /// 지시를 받아 목표 지점 쪽으로 정해진 속도만큼 이동한다.
        /// MoveTowards는 "목표를 지나치지 않고 정확히 멈추는" 이동이라 거리 유지에 적합하다.
        /// </summary>
        public void ApplyDirective(ChaseDirectiveIssued directive, float deltaTime, float targetDistance)
        {
            LastDirective = directive;
            AppliedDirectiveCount++;
            if (deltaTime <= 0f) return;

            var position = (Vector2)transform.position;
            var radius = BodyRadius();

            // ---- X: 감독이 지시한 목표 거리까지. 벽에 막히면 그 자리에 선다. ----
            // 예전에는 막힘 검사 없이 transform을 그대로 옮겨서 벽을 통과했다.
            var direction = Mathf.Approximately(directive.PursuerPosition.x, directive.PlayerPosition.x)
                ? 1f
                : Mathf.Sign(directive.PursuerPosition.x - directive.PlayerPosition.x);
            var targetX = directive.PlayerPosition.x + direction * Mathf.Max(0f, targetDistance);
            var nextX = Mathf.MoveTowards(position.x, targetX, directive.Speed * deltaTime);
            var moveX = nextX - position.x;

            var blocked = !Mathf.Approximately(moveX, 0f) &&
                          FindTerrain(position, new Vector2(Mathf.Sign(moveX), 0f), radius + Mathf.Abs(moveX) + Skin).HasValue;
            if (!blocked) position.x = nextX;

            // ---- Y: 중력 / 벽타기 / 점프 ----
            IsGrounded = verticalVelocity <= 0f && FindTerrain(position, Vector2.down, radius + Skin).HasValue;

            // 벽타기는 "플레이어가 위에 있을 때"만 한다.
            // 조건 없이 막히기만 하면 오르게 두었더니, 레벨 경계벽에 눌린 추격자가 벽을 타고
            // 화면 밖까지 끝없이 올라가 공중에 떠 버렸다(#12에서 실제로 발생).
            IsClimbing = blocked && directive.PlayerPosition.y > position.y;

            if (IsClimbing)
            {
                verticalVelocity = climbSpeed;
            }
            else if (IsGrounded)
            {
                // 평지에서는 서 있고, 플레이어가 뚜렷하게 위에 있을 때만 뛴다.
                verticalVelocity = directive.PlayerPosition.y - position.y > jumpTriggerHeight ? jumpSpeed : 0f;
            }
            else
            {
                verticalVelocity = Mathf.Max(-maxFallSpeed, verticalVelocity - gravity * deltaTime);
            }

            var moveY = verticalVelocity * deltaTime;
            if (moveY < 0f)
            {
                // 한 프레임에 바닥을 뚫고 지나가지 않도록, 이동할 거리만큼 미리 살펴 바닥에 붙여 세운다.
                var landing = FindTerrain(position, Vector2.down, radius + Mathf.Abs(moveY) + Skin);
                if (landing.HasValue)
                {
                    position.y = landing.Value.point.y + radius;
                    verticalVelocity = 0f;
                    moveY = 0f;
                    IsGrounded = true;
                }
            }

            position.y += moveY;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        private float BodyRadius()
        {
            if (body == null) body = GetComponent<CircleCollider2D>();
            var scale = transform.lossyScale;
            return body == null ? 0.5f : body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        /// <summary>
        /// 주어진 방향에서 가장 가까운 "지형"을 찾는다. 없으면 null.
        /// </summary>
        /// <remarks>
        /// 지형으로 치지 않는 것: 자기 자신, 트리거(회상 앵커·전투 트리거·잔재 몸통 등), 그리고 플레이어.
        /// 플레이어를 걸러 내지 않으면 플레이어가 벽으로 취급돼, 다가서자마자 "막혔다"고 판단해
        /// 제자리에서 벽을 타듯 올라가 버린다.
        /// </remarks>
        private RaycastHit2D? FindTerrain(Vector2 origin, Vector2 direction, float distance)
        {
            var hits = Physics2D.RaycastAll(origin, direction, distance);
            var nearest = default(RaycastHit2D);
            var found = false;

            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider == null || collider.isTrigger) continue;
                if (collider.transform == transform || collider.transform.IsChildOf(transform)) continue;
                if (collider.GetComponentInParent<PlayerController>() != null) continue;
                if (found && hit.distance >= nearest.distance) continue;

                nearest = hit;
                found = true;
            }

            return found ? nearest : null;
        }
    }
}
