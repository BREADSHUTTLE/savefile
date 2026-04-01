using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using System.Runtime.InteropServices;
using CAPYBARA;

namespace CAPYBARA.Bundles
{
    [ExecuteAlways]
    public class CPFixedAspectScaler : MonoBehaviour
    {
        public RectTransform scalerWrapper;
        public CanvasScaler canvasScaler;
        public RectTransform canvasRect;

        public static bool portraitMode=false;
        private const float targetAspect = 19.5f / 9f;

        private void Awake()
        {
            UpdateScaler();
            TouchScreenKeyboard.hideInput = true;
        }

        void Update()
        {
            if (!portraitMode)
            {
                UpdateScaler();    
            }
            
        }
        void UpdateScaler()
        {
            if (scalerWrapper == null || scalerWrapper.parent == null || canvasScaler == null) return;

            RectTransform parentRect = scalerWrapper.parent.GetComponent<RectTransform>();
            Vector2 parentSize = parentRect.rect.size;
            float parentWidth = parentSize.x;
            float parentHeight = parentSize.y;
            float parentAspect = parentWidth / parentHeight;

            float targetWidth, targetHeight;

            if (parentAspect >= targetAspect)
            {
                targetHeight = parentHeight;
                targetWidth = targetHeight * parentAspect;
                canvasScaler.matchWidthOrHeight = 1f;
            }
            else
            {
                targetWidth = parentWidth;
                targetHeight = targetWidth / targetAspect;
                canvasScaler.matchWidthOrHeight = 0f;
            }

            scalerWrapper.sizeDelta = new Vector2(targetWidth, targetHeight);
            scalerWrapper.anchorMin = new Vector2(0.5f, 0.5f);
            scalerWrapper.anchorMax = new Vector2(0.5f, 0.5f);
            scalerWrapper.pivot = new Vector2(0.5f, 0.5f);
            scalerWrapper.anchoredPosition = Vector2.zero;
        }
    }
}
