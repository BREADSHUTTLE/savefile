using DG.Tweening;
using UnityEngine;

namespace CAPYBARA.Bundles
{
    public class CPTabIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UISegmentedControlGroup toggleGroup;
        [SerializeField] private RectTransform indicator;
        [SerializeField] private RectTransform sizeTarget;  // 비어있으면 indicator 사용
        [SerializeField] private RectTransform[] tabTargets;

        [Header("Animation Settings")]
        [SerializeField] private float moveDuration = 0.25f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;
        
        [Header("Size Matching")]
        [SerializeField] private bool matchWidth = true;
        [SerializeField] private bool matchHeight = false;
        [SerializeField] private float sizeDuration = 0.2f;
        [SerializeField] private Ease sizeEase = Ease.OutQuad;

        private RectTransform SizeTarget => sizeTarget != null ? sizeTarget : indicator;

        private Tweener moveTween;
        private Tweener sizeTween;

        private void OnEnable()
        {
            if (toggleGroup != null)
                toggleGroup.onIndexChanged += OnTabChanged;

            if (toggleGroup != null && tabTargets != null && tabTargets.Length > 0)
            {
                int currentIndex = toggleGroup.CurrentIndex;
                if (currentIndex >= 0 && currentIndex < tabTargets.Length)
                    SetPositionImmediate(currentIndex);
            }
        }

        private void OnDisable()
        {
            if (toggleGroup != null)
                toggleGroup.onIndexChanged -= OnTabChanged;

            KillTweens();
        }

        private void OnTabChanged(int index)
        {
            if (indicator == null || tabTargets == null || index < 0 || index >= tabTargets.Length)
                return;

            RectTransform target = tabTargets[index];
            if (target == null)
                return;

            MoveToTarget(target);
        }

        private void MoveToTarget(RectTransform target)
        {
            KillTweens();

            Vector2 targetPos = GetTargetAnchoredPosition(target);
            moveTween = indicator.DOAnchorPos(targetPos, moveDuration).SetEase(moveEase).SetUpdate(true);

            if (matchWidth || matchHeight)
            {
                Vector2 targetSize = GetTargetSize(target);
                sizeTween = SizeTarget.DOSizeDelta(targetSize, sizeDuration).SetEase(sizeEase).SetUpdate(true);
            }
        }

        private void SetPositionImmediate(int index)
        {
            if (indicator == null || tabTargets == null || index < 0 || index >= tabTargets.Length)
                return;

            RectTransform target = tabTargets[index];
            if (target == null)
                return;

            indicator.anchoredPosition = GetTargetAnchoredPosition(target);

            if (matchWidth || matchHeight)
                SizeTarget.sizeDelta = GetTargetSize(target);
        }

        private Vector2 GetTargetSize(RectTransform target)
        {
            float width = matchWidth ? target.rect.width : SizeTarget.sizeDelta.x;
            float height = matchHeight ? target.rect.height : SizeTarget.sizeDelta.y;
            return new Vector2(width, height);
        }

        private Vector2 GetTargetAnchoredPosition(RectTransform target)
        {
            return new Vector2(target.anchoredPosition.x, indicator.anchoredPosition.y);
        }

        private void KillTweens()
        {
            moveTween?.Kill();
            sizeTween?.Kill();
        }

        public void SetPositionImmediateByIndex(int index)
        {
            SetPositionImmediate(index);
        }

        public void MoveToIndex(int index)
        {
            OnTabChanged(index);
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Setup Tab Targets")]
        private void AutoSetupTabTargets()
        {
            if (toggleGroup == null)
            {
                Debug.LogWarning("CPTabIndicator: ToggleGroup이 설정되지 않았습니다.");
                return;
            }

            var togglesField = typeof(UISegmentedControlGroup).GetField("toggles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (togglesField != null)
            {
                var toggles = togglesField.GetValue(toggleGroup) as System.Collections.Generic.List<UISegmentedControl>;
                if (toggles != null)
                {
                    tabTargets = new RectTransform[toggles.Count];
                    for (int i = 0; i < toggles.Count; i++)
                    {
                        if (toggles[i] != null)
                            tabTargets[i] = toggles[i].GetComponent<RectTransform>();
                    }
                    Debug.Log($"CPTabIndicator: {toggles.Count}개의 탭 타겟이 자동 설정되었습니다.");
                }
            }
        }
#endif
    }
}
