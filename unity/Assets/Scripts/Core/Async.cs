using System;
using UnityEngine;

namespace Politicmon.Core
{
    /// <summary>
    /// Small helpers around Unity 6 Awaitable so game flow (dialogs, battles) reads top to bottom.
    /// </summary>
    public static class Async
    {
        /// <summary>Starts an async flow without awaiting it, logging any exception.</summary>
        public static async void Run(Func<Awaitable> flow)
        {
            try
            {
                await flow();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static Awaitable Seconds(float seconds) => Awaitable.WaitForSecondsAsync(seconds);

        public static Awaitable NextFrame() => Awaitable.NextFrameAsync();

        /// <summary>Calls step(t) with t going from 0 to 1 over the duration, one call per frame.</summary>
        public static async Awaitable Tween(float duration, Action<float> step)
        {
            float t = 0f;
            while (t < duration)
            {
                step(Mathf.Clamp01(t / duration));
                await Awaitable.NextFrameAsync();
                t += Time.deltaTime;
            }
            step(1f);
        }

        public static Awaitable MoveTo(RectTransform rect, Vector2 target, float duration)
        {
            Vector2 from = rect.anchoredPosition;
            return Tween(duration, t => rect.anchoredPosition = Vector2.Lerp(from, target, t));
        }
    }
}
