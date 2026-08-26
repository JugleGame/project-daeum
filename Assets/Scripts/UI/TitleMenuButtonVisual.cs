using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daeume.UI
{
    public sealed class TitleMenuButtonVisual : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Image focusBar;
        [SerializeField] private Text cursor;
        [SerializeField] private Text label;

        private static readonly Color ActiveBackground = new(0.886f, 0.604f, 0.31f, 0.18f);
        private static readonly Color ActiveAccent = new(0.894f, 0.639f, 0.361f, 1f);
        private static readonly Color ActiveLabel = new(1f, 0.949f, 0.851f, 1f);
        private static readonly Color InactiveLabel = new(0.933f, 0.886f, 0.812f, 0.68f);

        private void OnEnable()
        {
            Apply(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
        }

        public void OnSelect(BaseEventData eventData) => Apply(true);
        public void OnDeselect(BaseEventData eventData) => Apply(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            var selectable = GetComponent<Selectable>();
            if (selectable != null && selectable.IsInteractable())
                selectable.Select();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Apply(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
        }

        private void Apply(bool active)
        {
            if (background != null) background.color = active ? ActiveBackground : Color.clear;
            if (focusBar != null) focusBar.color = active ? ActiveAccent : Color.clear;
            if (cursor != null)
            {
                cursor.text = "›";
                cursor.color = active ? ActiveAccent : Color.clear;
            }

            if (label != null) label.color = active ? ActiveLabel : InactiveLabel;
        }
    }
}
