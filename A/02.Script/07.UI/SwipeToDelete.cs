using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CAPYBARA
{
    public class SwipeToDelete : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, 
                                 IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("콘텐츠 설정")]
        public RectTransform contentTransform;
        public GameObject deleteButtonArea;
        public GameObject clickTarget;
        [NonSerialized] public List<GameObject> priorityClickTargets = new List<GameObject>();

        [Header("스와이프 설정")]
        public float openOffset = -300f;
        public float swipeThreshold = 100f;
        public float dragStartThreshold = 20f;
        public float snapDuration = 0.2f;

        private Vector2 dragStartPos;
        private float dragStartX;
        private bool isDragging;
        private bool isOpen;
        private bool isHorizontalDrag;
        private bool wasDragged;
        private bool isVerticalDrag;
        private bool isAnimating;
        private GameObject currentPressedObject;
        private Image cachedImage;
        [NonSerialized] public ScrollRect parentScrollRect;
        private Coroutine snapCoroutine;
        private CanvasGroup deleteButtonCanvasGroup;

        public Action onDeleteButtonShown;
        public Action onDeleteButtonHidden;

        private void Awake()
        {
            cachedImage = GetComponent<Image>();

            if (deleteButtonArea != null)
            {
                deleteButtonCanvasGroup = deleteButtonArea.GetComponent<CanvasGroup>();
                if (deleteButtonCanvasGroup == null)
                    deleteButtonCanvasGroup = deleteButtonArea.AddComponent<CanvasGroup>();
                
                deleteButtonCanvasGroup.alpha = 0f;
                deleteButtonCanvasGroup.blocksRaycasts = false;
                deleteButtonArea.SetActive(true);
            }
        }

        private void OnEnable()
        {
            parentScrollRect = GetComponentInParent<ScrollRect>();
            ResetState();
        }

        private void OnDisable()
        {
            if (snapCoroutine != null)
            {
                StopCoroutine(snapCoroutine);
                snapCoroutine = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            wasDragged = false;
            currentPressedObject = PassEventToObjectBehind<IPointerDownHandler>(eventData, (handler, data) => handler.OnPointerDown(data));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (wasDragged)
            {
                if (currentPressedObject != null)
                {
                    var handler = currentPressedObject.GetComponent<IPointerUpHandler>();
                    if (handler != null)
                        handler.OnPointerUp(eventData);
                }
                currentPressedObject = null;
                return;
            }

            PassEventToObjectBehind<IPointerUpHandler>(eventData, (handler, data) => handler.OnPointerUp(data));
            currentPressedObject = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (wasDragged)
            {
                wasDragged = false;
                return;
            }

            PassEventToObjectBehind<IPointerClickHandler>(eventData, (handler, data) => handler.OnPointerClick(data));
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (parentScrollRect != null)
                parentScrollRect.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isAnimating)
                return;
            
            dragStartPos = eventData.position;
            dragStartX = contentTransform != null ? contentTransform.anchoredPosition.x : 0f;
            isDragging = true;
            isHorizontalDrag = false;
            isVerticalDrag = false;
            
            if (parentScrollRect != null)
                parentScrollRect.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isAnimating)
                return;
            
            if (!isDragging)
            {
                if (isVerticalDrag && parentScrollRect != null)
                    parentScrollRect.OnDrag(eventData);
                return;
            }

            Vector2 delta = eventData.position - dragStartPos;

            if (!isHorizontalDrag && !isVerticalDrag)
            {
                if (Mathf.Abs(delta.x) < dragStartThreshold && Mathf.Abs(delta.y) < dragStartThreshold)
                {
                    if (parentScrollRect != null)
                        parentScrollRect.OnDrag(eventData);
                    return;
                }

                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                {
                    isDragging = false;
                    isVerticalDrag = true;
                    
                    if (parentScrollRect != null)
                        parentScrollRect.OnDrag(eventData);
                    return;
                }

                isHorizontalDrag = true;
                wasDragged = true;

                if (currentPressedObject != null)
                {
                    var upHandler = currentPressedObject.GetComponent<IPointerUpHandler>();
                    if (upHandler != null)
                        upHandler.OnPointerUp(eventData);
                    currentPressedObject = null;
                }
            }

            if (isHorizontalDrag && contentTransform != null)
            {
                float targetX = dragStartX + delta.x;

                targetX = Mathf.Clamp(targetX, openOffset, 0f);
                
                Vector2 pos = contentTransform.anchoredPosition;
                pos.x = targetX;
                contentTransform.anchoredPosition = pos;

                UpdateDeleteButtonAlpha(targetX);
            }
        }
        
        private void UpdateDeleteButtonAlpha(float currentX)
        {
            if (deleteButtonCanvasGroup == null)
                return;

            float progress = Mathf.Abs(currentX) / Mathf.Abs(openOffset);
            progress = Mathf.Clamp01(progress);
            
            deleteButtonCanvasGroup.alpha = progress;
            deleteButtonCanvasGroup.blocksRaycasts = progress > 0.1f;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isAnimating)
                return;

            if (isVerticalDrag && parentScrollRect != null)
            {
                parentScrollRect.OnEndDrag(eventData);
                isVerticalDrag = false;
                return;
            }
            
            if (!isDragging || !isHorizontalDrag)
            {
                isDragging = false;
                if (parentScrollRect != null)
                    parentScrollRect.OnEndDrag(eventData);
                return;
            }

            isDragging = false;
            
            if (contentTransform == null)
                return;
            
            float currentX = contentTransform.anchoredPosition.x;
            float threshold = openOffset * 0.5f;
            
            if (currentX < threshold)
                SnapTo(openOffset, true);
            else
                SnapTo(0f, false);
        }

        private void SnapTo(float targetX, bool opening)
        {
            if (snapCoroutine != null)
                StopCoroutine(snapCoroutine);
            
            snapCoroutine = StartCoroutine(SnapAnimation(targetX, opening));
        }

        private IEnumerator SnapAnimation(float targetX, bool opening)
        {
            isAnimating = true;
            
            if (contentTransform == null)
            {
                isAnimating = false;
                yield break;
            }
            
            float startX = contentTransform.anchoredPosition.x;
            float startAlpha = deleteButtonCanvasGroup != null ? deleteButtonCanvasGroup.alpha : 0f;
            float targetAlpha = opening ? 1f : 0f;
            float elapsed = 0f;
            
            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / snapDuration;

                t = 1f - Mathf.Pow(1f - t, 3f);
                
                float newX = Mathf.Lerp(startX, targetX, t);
                Vector2 pos = contentTransform.anchoredPosition;
                pos.x = newX;
                contentTransform.anchoredPosition = pos;

                if (deleteButtonCanvasGroup != null)
                    deleteButtonCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                yield return null;
            }

            Vector2 finalPos = contentTransform.anchoredPosition;
            finalPos.x = targetX;
            contentTransform.anchoredPosition = finalPos;

            if (deleteButtonCanvasGroup != null)
            {
                deleteButtonCanvasGroup.alpha = targetAlpha;
                deleteButtonCanvasGroup.blocksRaycasts = opening;
            }
            
            isAnimating = false;

            if (opening)
            {
                isOpen = true;
                onDeleteButtonShown?.Invoke();
            }
            else
            {
                isOpen = false;
                onDeleteButtonHidden?.Invoke();
            }
            
            snapCoroutine = null;
        }

        private GameObject PassEventToObjectBehind<T>(PointerEventData eventData, Action<T, PointerEventData> action) where T : class
        {
            if (priorityClickTargets.Count > 0 && !isOpen)
            {
                if (cachedImage != null)
                    cachedImage.raycastTarget = false;

                List<RaycastResult> hitResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, hitResults);

                if (cachedImage != null)
                    cachedImage.raycastTarget = true;

                foreach (var result in hitResults)
                {
                    if (!priorityClickTargets.Contains(result.gameObject))
                        continue;
                    var handler = result.gameObject.GetComponent<T>();
                    if (handler != null)
                    {
                        action(handler, eventData);
                        return result.gameObject;
                    }
                }
            }

            if (clickTarget != null && !isOpen)
            {
                var targetHandler = clickTarget.GetComponent<T>();
                if (targetHandler != null)
                {
                    action(targetHandler, eventData);
                    return clickTarget;
                }
            }

            // 열린 상태: 루트 Graphic이 버튼 영역을 가리므로, 자신을 잠시 끄고 아래 UI로 포워딩
            if (isOpen)
                return PassThroughRaycast<T>(eventData, action);

            return null;
        }

        private GameObject PassThroughRaycast<T>(PointerEventData eventData, Action<T, PointerEventData> action) where T : class
        {
            if (cachedImage != null)
                cachedImage.raycastTarget = false;

            var hitResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hitResults);

            if (cachedImage != null)
                cachedImage.raycastTarget = true;

            foreach (var result in hitResults)
            {
                if (result.gameObject == gameObject)
                    continue;

                var handler = result.gameObject.GetComponent<T>();
                if (handler == null)
                    handler = result.gameObject.GetComponentInParent<T>();
                if (handler != null)
                {
                    action(handler, eventData);
                    return result.gameObject;
                }
            }

            return null;
        }

        public void ShowDeleteButton()
        {
            if (contentTransform != null)
            {
                SnapTo(openOffset, true);
            }
            else
            {
                isOpen = true;
                onDeleteButtonShown?.Invoke();
            }
        }

        public void HideDeleteButton()
        {
            if (contentTransform != null)
            {
                SnapTo(0f, false);
            }
            else
            {
                isOpen = false;
                onDeleteButtonHidden?.Invoke();
            }
        }

        public void ResetState()
        {
            if (snapCoroutine != null)
            {
                StopCoroutine(snapCoroutine);
                snapCoroutine = null;
            }
            
            isOpen = false;
            wasDragged = false;
            isVerticalDrag = false;
            isAnimating = false;
            currentPressedObject = null;

            if (contentTransform != null)
            {
                Vector2 pos = contentTransform.anchoredPosition;
                pos.x = 0f;
                contentTransform.anchoredPosition = pos;
            }

            if (deleteButtonCanvasGroup != null)
            {
                deleteButtonCanvasGroup.alpha = 0f;
                deleteButtonCanvasGroup.blocksRaycasts = false;
            }
        }

        public void SetOpenOffset(float offset)
        {
            openOffset = offset;
        }

        public bool IsOpen => isOpen;
    }
}
