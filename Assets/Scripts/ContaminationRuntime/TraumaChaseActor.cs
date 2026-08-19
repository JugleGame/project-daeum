using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    [RequireComponent(typeof(TraumaContactSource), typeof(CircleCollider2D))]
    public sealed class TraumaChaseActor : MonoBehaviour
    {
        public ChaseDirectiveIssued LastDirective { get; private set; }
        public int AppliedDirectiveCount { get; private set; }

        public void ApplyDirective(ChaseDirectiveIssued directive, float deltaTime, float targetDistance)
        {
            LastDirective = directive;
            AppliedDirectiveCount++;
            if (deltaTime <= 0f) return;

            var direction = Mathf.Approximately(directive.PursuerPosition.x, directive.PlayerPosition.x)
                ? 1f
                : Mathf.Sign(directive.PursuerPosition.x - directive.PlayerPosition.x);
            var target = directive.PlayerPosition + Vector2.right * direction * Mathf.Max(0f, targetDistance);
            transform.position = Vector2.MoveTowards(transform.position, target, directive.Speed * deltaTime);
        }
    }
}
