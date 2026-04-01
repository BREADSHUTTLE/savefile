using System;
using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CAPYBARA
{
    public class PageScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Header("Refs")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
    
        [Header("Config")]
        [SerializeField] private int pageCount = 3;
        [SerializeField] private float snapDuration = 0.2f;
    
        [Tooltip("이 거리 이상 드래그하면 다음/이전 페이지로 넘김 (viewport 대비 비율)")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float swipeThresholdRatio = 0.2f;
    
        [Tooltip("이 속도 이상이면 거리 작아도 넘김")]
        [SerializeField] private float flickVelocityThreshold = 800f;
    
        public event Action<int> OnPageChanged;
        
        private int currentPage = 0;
        private Vector2 dragStartPointerPos;
    
        private Coroutine snapCo;
    
        private float PageWidth => viewport.rect.width;
    
        void Reset()
        {
            scrollRect = GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                viewport = scrollRect.viewport;
                content = scrollRect.content;
            }
        }
    
        void Start()
        {
            // 페이지 수 자동 세팅 원하면 사용
            // SetPageCountAuto();

            // 시작 페이지 반영
            SnapToPage(currentPage, instant: true);
        }

        private void OnEnable()
        {
            isAnimating = false;
            SnapToPage(0,true);
        }

        public void SetPageCountAuto()
        {
            pageCount = content.childCount;
        }

        
        public void OnBeginDrag(PointerEventData eventData)
        {
            isAnimating = false;
            if (snapCo != null) StopCoroutine(snapCo);
            dragStartPointerPos = eventData.position;
        }
    
     public void OnEndDrag(PointerEventData eventData)
    {
        var delta = eventData.position - dragStartPointerPos;

        int target = currentPage;
        float threshold = PageWidth * swipeThresholdRatio;

        bool movedEnough = Mathf.Abs(delta.x) >= threshold;
        bool fastFlick = Mathf.Abs(scrollRect.velocity.x) >= flickVelocityThreshold;

        if (movedEnough || fastFlick)
        {
            if (delta.x < 0) target = currentPage + 1;       // 다음
            else if (delta.x > 0) target = currentPage - 1;  // 이전
        }
        else
        {
            target = GetNearestPage();
        }

        target = Mathf.Clamp(target, 0, pageCount - 1);
        SnapToPage(target);
    }

    private int GetNearestPage()
    {
        float x = content.anchoredPosition.x;
        int nearest = Mathf.RoundToInt(-x / PageWidth);
        return Mathf.Clamp(nearest, 0, pageCount - 1);
    }

    bool isAnimating = false;
    public void SnapToPage(int pageIndex, bool instant = false)
    {
        if (isAnimating)
            return;
        isAnimating = true;
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);

        bool changed = (currentPage != pageIndex);
        currentPage = pageIndex;

        Vector2 targetPos = new Vector2(-pageIndex * PageWidth, content.anchoredPosition.y);

        if (snapCo != null) StopCoroutine(snapCo);

        if (instant)
        {
            scrollRect.velocity = Vector2.zero;
            content.anchoredPosition = targetPos;
            if (changed) 
                OnPageChanged?.Invoke(currentPage);
            isAnimating = false;
            return;
        }

        snapCo = StartCoroutine(SmoothSnap(targetPos, snapDuration, changed));
    }


    private System.Collections.IEnumerator SmoothSnap(Vector2 target, float duration, bool fireChanged)
    {
        Vector2 start = content.anchoredPosition;
        float t = 0f;

        scrollRect.velocity = Vector2.zero;

        if (fireChanged)
            OnPageChanged?.Invoke(currentPage);
        
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            content.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        content.anchoredPosition = target;
        snapCo = null;

      

        isAnimating = false;
    }

    public int CurrentPage => currentPage;
    public int PageCount => pageCount;
    }
}
