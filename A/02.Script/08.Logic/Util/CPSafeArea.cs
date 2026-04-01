using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    public class CPSafeArea : MonoBehaviour
    {
        public bool top = true;
        public bool left = true;
        public bool bottom = true;
        public bool right = true;

        private RectTransform _rect;
        private Vector2 _defaultAnchoredPosition;
        private Vector2 _defaultSizeDelta;

        public float fixedArea => -_rect.sizeDelta.y;

        private static readonly float _minLeft = 0;
        private static readonly float _minRight = 0;
        private static readonly float _minTop = 0;
        private static readonly float _minBottom = 0;

        RectTransform rtBody;

        void OnEnable()
        {
            Set().Forget();
        }

        public void AreaUpdate()
        {
            Set().Forget();
        }


        private async UniTaskVoid Set()
        {
            await UniTask.Yield();
            if (rtBody == null)
                rtBody = GetComponent<RectTransform>();

            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = anchorMin + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rtBody.anchorMin = anchorMin;
            rtBody.anchorMax = anchorMax;
        }
    }
}
