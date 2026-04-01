using DG.Tweening;
using UnityEngine;

namespace CAPYBARA
{
    public class CurvedMover : MonoBehaviour
    {
        [Header("경로 설정")]
        public Vector3 startPosition;
        public Vector3 endPosition;
        [Tooltip("곡선 제어점 1 (시작점 쪽 접선)")]
        public Vector3 controlPoint1;
        [Tooltip("곡선 제어점 2 (끝점 쪽 접선)")]
        public Vector3 controlPoint2;

        [Header("애니메이션 설정")]
        public float duration = 1f;
        public Ease easeType = Ease.InOutSine;
        public int loops = 1;
        public LoopType loopType = LoopType.Restart;

        private Tween _activeTween;

        public void Play()
        {
            gameObject.SetActive(true);
            Stop();
            transform.position = startPosition;
            _activeTween = transform
                .DOMove(endPosition,duration)
                //.DOPath(new[] { controlPoint1, controlPoint2, endPosition }, duration, PathType.CubicBezier)
                .SetEase(easeType)
                .SetLoops(loops, loopType);
        }

        public void Stop()
        {
            _activeTween?.Kill();
            transform.position = startPosition;
        }

        /// <summary>t(0~1) 위치의 베지어 좌표 반환. 에디터 미리보기에서도 사용.</summary>
        public Vector3 EvaluatePath(float t)
        {
            float u = 1f - t;
            return u * u * u * startPosition
                 + 3f * u * u * t * controlPoint1
                 + 3f * u * t * t * controlPoint2
                 + t * t * t * endPosition;
        }

        private void OnDestroy() => _activeTween?.Kill();
    }
}
