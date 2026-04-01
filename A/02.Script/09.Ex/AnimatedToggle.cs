using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CAPYBARA
{
    public class AnimatedToggle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] public Toggle toggle;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private RectTransform slideArea;
        [SerializeField] private Image bgOffImage;
        [SerializeField] private Image bgOnImage;

        [Header("Handle Padding")]
        [SerializeField] private float handlePadding = 5f;

        [Header("Animation")]
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Header("Input Lock")]
        [SerializeField] private bool lockInputWhileAnimating = true;

        private Tween handleTween;
        private Tween bgOnTween;
        private Sequence seq;
        private bool locked;

        private void Awake()
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }
        
        private float GetHandleLocalX(bool isOn)
        {
            if (slideArea == null || handleRect == null) 
                return 0f;
            
            float slideWidth = slideArea.rect.width;
            float handleWidth = handleRect.rect.width;

            float minX = -slideWidth / 2f + handleWidth / 2f + handlePadding;
            float maxX = slideWidth / 2f - handleWidth / 2f - handlePadding;
            
            return isOn ? maxX : minX;
        }

        private void OnToggleChanged(bool isOn)
        {
            if (locked) 
                return;
            ApplyState(isOn, false);
        }

        public void ApplyState(bool isOn, bool instant)
        {
            handleTween?.Kill();
            bgOnTween?.Kill();
            seq?.Kill();
            toggle.SetIsOnWithoutNotify(isOn);

            float targetX = GetHandleLocalX(isOn);

            if (instant)
            {
                if (handleRect != null)
                    handleRect.anchoredPosition = new Vector2(targetX, 0);
                if (bgOnImage != null)
                    bgOnImage.fillAmount = isOn ? 1f : 0f;
                return;
            }

            if (lockInputWhileAnimating)
                SetLocked(true);

            seq = DOTween.Sequence();
            if (handleRect != null)
            {
                handleTween = handleRect.DOAnchorPosX(targetX, duration).SetEase(ease);
                seq.Join(handleTween);
            }

            if (bgOnImage != null)
            {
                bgOnTween = DOTween.To(() => bgOnImage.fillAmount,x => bgOnImage.fillAmount = x,isOn ? 1f : 0f,duration).SetEase(ease);
                seq.Join(bgOnTween);
            }

            seq.OnKill(() =>
            {
                if (lockInputWhileAnimating) 
                    SetLocked(false);
            });

            seq.OnComplete(() =>
            {
                if (lockInputWhileAnimating) 
                    SetLocked(false);
            });
        }

        private void SetLocked(bool value)
        {
            locked = value;
            toggle.interactable = !value;
        }

        private void OnDestroy()
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
            handleTween?.Kill();
            bgOnTween?.Kill();
            seq?.Kill();
        }
    }
}