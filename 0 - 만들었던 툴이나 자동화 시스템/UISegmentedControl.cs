using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace CAPYBARA.Bundles
{
    public class UISegmentedControl : MonoBehaviour
    {
        [SerializeField] private CPButton button;
        [SerializeField] private TMP_Text text;
        [SerializeField] private bool isOn = false;

        [Header("Toggle Objects")]
        [SerializeField] private GameObject[] objOn;
        [SerializeField] private GameObject[] objOff;

        [Header("Scale Animation")]
        [SerializeField] private bool useScaleAnimation = false;
        [SerializeField] private Transform[] scaleTargets;
        [SerializeField] private Vector3 scaleOn = Vector3.one;
        [SerializeField] private Vector3 scaleOff = new Vector3(0.85f, 0.85f, 1f);
        [SerializeField] private float scaleDuration = 0.2f;
        [SerializeField] private Ease scaleEase = Ease.OutBack;

        public UnityEvent<bool> onValueChanged = new UnityEvent<bool>();

        public bool IsOn => isOn;
        public CPButton Button => button;

        private Tweener[] scaleTweens;

        public void SetIsOn(bool value, bool notify = true)
        {
            bool changed = isOn != value;
            isOn = value;
            ApplyVisual(changed);

            if (notify && changed)
                onValueChanged.Invoke(isOn);
        }

        public void SetText(string txt, Color color)
        {
            text.text = txt;
            text.color = color;
        }

        private void ApplyVisual(bool animate = true)
        {
            if (button != null)
            {
                if (isOn)
                    button.Select();
                else
                    button.UnSelect();
            }

            if (objOn != null)
            {
                foreach (var obj in objOn)
                {
                    if (obj != null)
                        obj.SetActive(isOn);
                }
            }

            if (objOff != null)
            {
                foreach (var obj in objOff)
                {
                    if (obj != null)
                        obj.SetActive(!isOn);
                }
            }

            if (useScaleAnimation)
            {
                if (animate)
                    AnimateScale();
                else
                    ApplyScaleImmediate();
            }
        }

        private void AnimateScale()
        {
            if (scaleTargets == null || scaleTargets.Length == 0)
                return;

            KillScaleTweens();

            Vector3 targetScale = isOn ? scaleOn : scaleOff;
            scaleTweens = new Tweener[scaleTargets.Length];

            for (int i = 0; i < scaleTargets.Length; i++)
            {
                if (scaleTargets[i] != null)
                    scaleTweens[i] = scaleTargets[i].DOScale(targetScale, scaleDuration).SetEase(scaleEase).SetUpdate(true);
            }
        }

        private void ApplyScaleImmediate()
        {
            if (scaleTargets == null)
                return;

            Vector3 targetScale = isOn ? scaleOn : scaleOff;

            foreach (var target in scaleTargets)
            {
                if (target != null)
                    target.localScale = targetScale;
            }
        }

        private void KillScaleTweens()
        {
            if (scaleTweens == null)
                return;

            foreach (var tween in scaleTweens)
                tween?.Kill();
        }

        private void OnDisable()
        {
            KillScaleTweens();
        }
    }
}
