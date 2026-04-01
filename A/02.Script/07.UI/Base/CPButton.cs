using CAPYBARA.Core;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CAPYBARA.Bundles
{
    public class CPButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Button Animation")]
        [SerializeField] private float downSize = 0.97f;
        [SerializeField] private float upSize = 1f;

        [Header("Button Color")]
        [SerializeField] private Image targetGraphic = null;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private GameObject[] additionalTargets = null;
        
        [Header("On/Off Image")]
        [SerializeField] private Image onImage = null;
        [SerializeField] private Image offImage = null;
        
        [Header("Press Reveal")]
        [SerializeField] private Image pressRevealImage  = null;
        [SerializeField] private float revealDuration = 0.2f;
        private Tweener revealTween;

        [Header("Sound")]
        [SerializeField] private AudioSourceKey touchSound = AudioSourceKey.None;
        [SerializeField] private AudioSource clickAudio = null;

        [Header("Events")]
        public UnityEvent onClick = new UnityEvent();
        public UnityEvent onClickDown;
        public UnityEvent onClickUp;
        public UnityAction<Vector2> onTouch;

        [Header("Interactable")]
        [SerializeField] private bool _interactable = true;
        
        [Header("Dim")]
        [SerializeField] private Image dimImage = null;
        [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.5f);

        public bool interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                UpdateDimImage();
            }
        }

        private Vector2 downPosition;
        private bool isDown = false;
        private Coroutine animCoroutine;

        private void Awake()
        {
            if (pressRevealImage != null)
            {
                var c = pressRevealImage.color;
                c.a = 0f;
                pressRevealImage.color = c;
            }
            
            UpdateDimImage();
        }
        
        private void OnEnable()
        {
            UpdateDimImage();
        }
        
        private Vector2 GetSafeInputMousePos()
        {
            return Input.mousePosition;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (isDown || !interactable)
                return;
            
            downPosition = GetSafeInputMousePos();
            isDown = true;
            
            if (clickAudio != null)
                clickAudio.Play();

            if (touchSound != AudioSourceKey.None)
                AudioManager.Instance.Play(touchSound);

            onClickDown?.Invoke();
            if (pressRevealImage != null)
            {
                revealTween?.Kill();
                revealTween = pressRevealImage.DOFade(1f, revealDuration);
            }
            ApplyColor(pressedColor);
            StopAnimCoroutine();
            animCoroutine = StartCoroutine(IeAnim(downSize));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDown)
                return;

            isDown = false;

            onClickUp?.Invoke();
            
            if (pressRevealImage != null)
            {
                revealTween?.Kill();
                revealTween = pressRevealImage.DOFade(0f, revealDuration);
            }
            ApplyColor(normalColor);
            StopAnimCoroutine();
            animCoroutine = StartCoroutine(IeAnim(upSize));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable)
                return;
            
            onClick?.Invoke();
            onTouch?.Invoke(eventData.position);
        }

        public void Select()
        {
            ApplyStateSprite(true);
        }

        public void UnSelect()
        {
            ApplyStateSprite(false);
        }

        private void ApplyStateSprite(bool isActive)
        {
            if (onImage != null)
                onImage.gameObject.SetActive(isActive);

            if (offImage != null)
                offImage.gameObject.SetActive(!isActive);
        }

        private void PointerExit()
        {
            if (!isDown)
                return;

            isDown = false;

            onClickUp?.Invoke();
            if (pressRevealImage != null)
            {
                revealTween?.Kill();
                revealTween = pressRevealImage.DOFade(0f, revealDuration);
            }
            ApplyColor(normalColor);
            StopAnimCoroutine();
            animCoroutine = StartCoroutine(IeAnim(upSize));
        }

        private void ApplyColor(Color color)
        {
            if (targetGraphic != null)
                targetGraphic.color = color;

            if (additionalTargets != null)
            {
                for (int i = 0; i < additionalTargets.Length; i++)
                {
                    if (additionalTargets[i] != null)
                    {
                        var graphic = additionalTargets[i].GetComponent<Graphic>();
                        if (graphic != null)
                            graphic.color = color;
                    }
                }
            }
        }

        private void StopAnimCoroutine()
        {
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
        }

        private void OnDisable()
        {
            StopAnimCoroutine();
            revealTween?.Kill();
            if (pressRevealImage != null)
            {
                var c = pressRevealImage.color;
                c.a = 0f;
                pressRevealImage.color = c;
            }

            transform.localScale = Vector3.one;
            ApplyScaleToAdditionalTargets(Vector3.one);
            ApplyColor(normalColor);
            isDown = false;
        }

        private IEnumerator IeAnim(float targetSize)
        {
            // 목표 크기로 애니메이션
            while (Mathf.Abs(transform.localScale.x - targetSize) > 0.01f)
            {
                Vector3 newScale = Vector3.one * Mathf.Lerp(transform.localScale.x, targetSize, 0.45f);
                transform.localScale = newScale;
                ApplyScaleToAdditionalTargets(newScale);
                yield return null;
            }
            Vector3 finalScale = Vector3.one * targetSize;
            transform.localScale = finalScale;
            ApplyScaleToAdditionalTargets(finalScale);

            // upSize보다 큰 경우 upSize로 복귀
            if (targetSize > upSize * 1.01f)
            {
                while (Mathf.Abs(transform.localScale.x - upSize) > 0.01f)
                {
                    Vector3 newScale = Vector3.one * Mathf.Lerp(transform.localScale.x, upSize, 0.45f);
                    transform.localScale = newScale;
                    ApplyScaleToAdditionalTargets(newScale);
                    yield return null;
                }
                Vector3 upScale = Vector3.one * upSize;
                transform.localScale = upScale;
                ApplyScaleToAdditionalTargets(upScale);
            }

            // 드래그 감지 (Down 상태일 때만)
            while (isDown)
            {
                if (Vector2.SqrMagnitude(GetSafeInputMousePos() - downPosition) > 100000f)
                {
                    PointerExit();
                    yield break;
                }
                yield return null;
            }
        }

        private void ApplyScaleToAdditionalTargets(Vector3 scale)
        {
            if (additionalTargets != null)
            {
                for (int i = 0; i < additionalTargets.Length; i++)
                {
                    if (additionalTargets[i] != null)
                        additionalTargets[i].transform.localScale = scale;
                }
            }
        }

        private void UpdateDimImage()
        {
            if (dimImage == null)
                return;

            dimImage.gameObject.SetActive(!_interactable);
            dimImage.color = dimColor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateDimImage();
        }
#endif
    }
}
