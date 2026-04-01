// DOTweenAsyncExtensions.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Cysharp.Threading.Tasks.DOTween
{
    public static class DOTweenAsyncExtensions
    {
        public static UniTask ToUniTask(this Tween tween, bool ignoreTimeScale = false)
        {
            var tcs = new UniTaskCompletionSource();

            if (!tween.active || tween.IsComplete())
            {
                tcs.TrySetResult();
                return tcs.Task;
            }

            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() => tcs.TrySetCanceled());

            return tcs.Task;
        }

        public static UniTask ToUniTask(this Tween tween, CancellationToken cancellationToken, bool ignoreTimeScale = false)
        {
            var tcs = new UniTaskCompletionSource();

            cancellationToken.Register(() =>
            {
                if (tween.IsActive() && tween.IsPlaying())
                {
                    tween.Kill();
                }
                tcs.TrySetCanceled();
            });

            if (!tween.active || tween.IsComplete())
            {
                tcs.TrySetResult();
                return tcs.Task;
            }

            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() => tcs.TrySetCanceled());

            return tcs.Task;
        }
    }
}
