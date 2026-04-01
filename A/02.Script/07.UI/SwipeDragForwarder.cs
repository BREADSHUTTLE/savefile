using UnityEngine;
using UnityEngine.EventSystems;

namespace CAPYBARA
{
    public class SwipeDragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public SwipeToDelete target;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void SyncPosition()
        {
            if (target == null || target.contentTransform == null || _rectTransform == null)
                return;
            var p = _rectTransform.anchoredPosition;
            p.x = target.contentTransform.anchoredPosition.x;
            _rectTransform.anchoredPosition = p;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (target != null)
            {
                target.OnBeginDrag(eventData);
                SyncPosition();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target != null)
            {
                target.OnDrag(eventData);
                SyncPosition();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (target != null)
            {
                target.OnEndDrag(eventData);
                SyncPosition();
            }
        }
    }
}
