using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>마지막 회상 뒤 트라우마를 캐릭터 모습으로 드러낸다.</summary>
    public sealed class TraumaRevealVisual : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private TraumaAppearancePulse pulse;

        public void Reveal()
        {
            if (characterSprite == null || target == null) return;
            if (animator != null) animator.enabled = false;
            target.sprite = characterSprite;
            pulse?.Play();
        }
    }
}
