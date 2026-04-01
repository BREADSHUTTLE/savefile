using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace CAPYBARA
{
    [RequireComponent(typeof(TMP_InputField))]
    public class ScrollableInputField : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public float decelerationRate = 0.135f;
        public float scrollSensitivity = 1f;

        private TMP_InputField inputField;
        private RectTransform textViewport;
        private GameObject touchBlocker;
        private Color originalSelectionColor;
        private Color originalCaretColor;

        private float velocity;
        private bool isInertiaScrolling;
        private float savedScrollY = -1f;

        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();

            if (scrollRect == null)
                scrollRect = GetComponentInParent<ScrollRect>();

            if (inputField != null)
            {
                inputField.readOnly = true;
                textViewport = inputField.textViewport;

                originalSelectionColor = inputField.selectionColor;
                originalCaretColor = inputField.customCaretColor ? inputField.caretColor : (inputField.textComponent != null ? inputField.textComponent.color : Color.white);

                HideSelection();
            }

            CreateTouchBlocker();
        }

        private void HideSelection()
        {
            var c = originalSelectionColor;
            c.a = 0f;
            inputField.selectionColor = c;
            inputField.customCaretColor = true;
            inputField.caretColor = new Color(originalCaretColor.r, originalCaretColor.g, originalCaretColor.b, 0f);
        }

        private void RestoreColors()
        {
            inputField.selectionColor = originalSelectionColor;
            inputField.customCaretColor = true;
            inputField.caretColor = originalCaretColor;
        }

        private void CreateTouchBlocker()
        {
            touchBlocker = new GameObject("TouchBlocker");
            touchBlocker.transform.SetParent(transform, false);

            var rt = touchBlocker.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = touchBlocker.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            var handler = touchBlocker.AddComponent<ScrollableInputFieldBlocker>();
            handler.owner = this;

            touchBlocker.transform.SetAsLastSibling();
        }

        public void ActivateInput()
        {
            if (touchBlocker != null)
                touchBlocker.SetActive(false);

            savedScrollY = -1f;
            RestoreColors();
            inputField.readOnly = false;
            inputField.ActivateInputField();

            ClearSelectionNextFrame().Forget();
        }

        private async UniTaskVoid ClearSelectionNextFrame()
        {
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());

            if (inputField == null || !inputField.isFocused)
                return;

            int pos = inputField.caretPosition;
            inputField.selectionAnchorPosition = pos;
            inputField.selectionFocusPosition = pos;
            inputField.selectionStringAnchorPosition = inputField.stringPosition;
            inputField.selectionStringFocusPosition = inputField.stringPosition;
        }

        public ScrollRect GetParentScrollRect() => scrollRect;

        public bool HasOverflowingText()
        {
            if (inputField == null || inputField.textComponent == null)
                return false;

            float textH = inputField.textComponent.preferredHeight;
            float viewH = textViewport != null ? textViewport.rect.height : inputField.textComponent.rectTransform.rect.height;
            return textH > viewH + 1f;
        }

        public void ApplyScrollDelta(float delta)
        {
            if (inputField == null || inputField.textComponent == null)
                return;

            var textRT = inputField.textComponent.rectTransform;
            float textH = inputField.textComponent.preferredHeight;
            float viewH = textViewport != null ? textViewport.rect.height : textRT.rect.height;
            float maxScroll = Mathf.Max(0f, textH - viewH);

            if (maxScroll <= 0f)
                return;

            Vector2 pos = textRT.anchoredPosition;
            pos.y = Mathf.Clamp(pos.y + delta, 0f, maxScroll);
            textRT.anchoredPosition = pos;
            savedScrollY = pos.y;
        }

        public void StartInertia(float startVelocity)
        {
            velocity = startVelocity;
            isInertiaScrolling = true;
        }

        public void StopInertia()
        {
            velocity = 0f;
            isInertiaScrolling = false;
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases += EnforceScrollPosition;
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= EnforceScrollPosition;
        }

        private void EnforceScrollPosition()
        {
            if (inputField == null || !inputField.readOnly)
                return;

            if (savedScrollY < 0f || inputField.textComponent == null)
                return;

            var textRT = inputField.textComponent.rectTransform;
            if (Mathf.Abs(textRT.anchoredPosition.y - savedScrollY) > 0.1f)
            {
                var pos = textRT.anchoredPosition;
                pos.y = savedScrollY;
                textRT.anchoredPosition = pos;
            }
        }

        private void LateUpdate()
        {
            if (inputField == null)
                return;

            if (!inputField.isFocused && !inputField.readOnly)
            {
                if (inputField.textComponent != null)
                    savedScrollY = inputField.textComponent.rectTransform.anchoredPosition.y;

                inputField.readOnly = true;
                HideSelection();

                if (touchBlocker != null && !touchBlocker.activeSelf)
                    touchBlocker.SetActive(true);
            }

            if (isInertiaScrolling)
            {
                velocity *= Mathf.Pow(decelerationRate, Time.unscaledDeltaTime);

                if (Mathf.Abs(velocity) < 1f)
                {
                    velocity = 0f;
                    isInertiaScrolling = false;
                }
                else
                {
                    ApplyScrollDelta(velocity * Time.unscaledDeltaTime);
                }
            }
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= EnforceScrollPosition;
            velocity = 0f;
            isInertiaScrolling = false;

            if (inputField != null)
            {
                inputField.readOnly = true;
                HideSelection();
            }

            if (touchBlocker != null && !touchBlocker.activeSelf)
                touchBlocker.SetActive(true);
        }
    }

    public class ScrollableInputFieldBlocker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler, IPointerClickHandler
    {
        [HideInInspector] public ScrollableInputField owner;

        private bool isDragging;
        private bool useInternalScroll;
        private int tapCount;
        private float lastTapTime;
        private float velocity;

        private const float DoubleTapInterval = 0.4f;

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            isDragging = false;
            useInternalScroll = owner != null && owner.HasOverflowingText();
            velocity = 0f;

            if (owner != null)
            {
                owner.StopInertia();
                var sr = owner.GetParentScrollRect();
                if (sr != null)
                    sr.OnInitializePotentialDrag(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            velocity = 0f;

            if (owner == null)
                return;

            if (!useInternalScroll)
            {
                var sr = owner.GetParentScrollRect();
                if (sr != null)
                    sr.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner == null)
                return;

            if (useInternalScroll)
            {
                float deltaY = eventData.delta.y * owner.scrollSensitivity;
                owner.ApplyScrollDelta(-deltaY);
                velocity = -deltaY / Time.unscaledDeltaTime;
            }
            else
            {
                var sr = owner.GetParentScrollRect();
                if (sr != null)
                    sr.OnDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null)
            {
                if (useInternalScroll)
                {
                    owner.StartInertia(velocity);
                }
                else
                {
                    var sr = owner.GetParentScrollRect();
                    if (sr != null)
                        sr.OnEndDrag(eventData);
                }
            }

            isDragging = false;
            velocity = 0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging || owner == null)
                return;

            float now = Time.unscaledTime;
            if (now - lastTapTime <= DoubleTapInterval)
            {
                tapCount = 0;
                lastTapTime = 0f;
                owner.ActivateInput();
            }
            else
            {
                tapCount = 1;
                lastTapTime = now;
            }
        }
    }
}
