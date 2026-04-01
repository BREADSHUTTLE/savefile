using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace CAPYBARA
{
    public class AnimatedSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private UISlicedFill fillRect;
        [SerializeField] private RectTransform slideArea;

        [Header("Handle Padding")]
        [SerializeField] private float handlePadding = 5f;

        [Header("Value")]
        [SerializeField] [Range(0f, 1f)] private float value = 1f;
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 1f;

        public event Action<float> onValueChanged;

        public float Value
        {
            get => Mathf.Lerp(minValue, maxValue, value);
            set
            {
                float normalized = Mathf.InverseLerp(minValue, maxValue, value);
                SetValue(normalized);
            }
        }

        public float NormalizedValue
        {
            get => value;
            set => SetValue(value);
        }

        private void Start()
        {
            UpdateVisuals();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateValueFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateValueFromPointer(eventData);
        }

        private void UpdateValueFromPointer(PointerEventData eventData)
        {
            if (slideArea == null) 
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(slideArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            float normalizedValue = Mathf.Clamp01((localPoint.x - slideArea.rect.xMin) / slideArea.rect.width);
            
            SetValue(normalizedValue);
        }

        private void SetValue(float newValue)
        {
            newValue = Mathf.Clamp01(newValue);
            
            if (Mathf.Approximately(value, newValue)) 
                return;
            
            value = newValue;
            UpdateVisuals();
            onValueChanged?.Invoke(Value);
        }

        private void UpdateVisuals()
        {
            if (fillRect != null)
                fillRect.FillAmount = value;

            if (handleRect != null && slideArea != null)
            {
                float slideWidth = slideArea.rect.width;
                float handleWidth = handleRect.rect.width;
                
                float minX = -slideWidth / 2f + handleWidth / 2f + handlePadding;
                float maxX = slideWidth / 2f - handleWidth / 2f - handlePadding;
                
                float targetX = Mathf.Lerp(minX, maxX, value);
                handleRect.anchoredPosition = new Vector2(targetX, handleRect.anchoredPosition.y);
            }
        }

        public void SetValueWithoutNotify(float newValue)
        {
            value = Mathf.Clamp01(Mathf.InverseLerp(minValue, maxValue, newValue));
            UpdateVisuals();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateVisuals();
        }
#endif
    }
}
