using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CandyCrush.Common
{
    public enum Ease
    {
        Linear,
        SmoothStep,
        InQuad,
        OutQuad,
        InOutQuad,
        OutCubic
    }

    /// <summary>
    /// 轻量 Tween：不依赖 DOTween。可 yield return 等待，供棋盘交换/下落等使用。
    /// </summary>
    public static class Tween
    {
        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.Linear: return t;
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Ease.OutCubic:
                {
                    float u = 1f - t;
                    return 1f - u * u * u;
                }
                default: // SmoothStep
                    return t * t * (3f - 2f * t);
            }
        }

        public static IEnumerator LocalMove(Transform target, Vector3 to, float duration, Ease ease = Ease.SmoothStep)
        {
            if (target == null) yield break;
            var from = target.localPosition;
            yield return LocalMove(target, from, to, duration, ease);
        }

        public static IEnumerator LocalMove(Transform target, Vector3 from, Vector3 to, float duration, Ease ease = Ease.SmoothStep)
        {
            if (target == null) yield break;
            duration = Mathf.Max(0.0001f, duration);
            float t = 0f;
            while (t < duration)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                float u = Evaluate(ease, t / duration);
                target.localPosition = Vector3.LerpUnclamped(from, to, u);
                yield return null;
            }
            if (target != null) target.localPosition = to;
        }

        public static IEnumerator LocalScale(Transform target, Vector3 to, float duration, Ease ease = Ease.SmoothStep)
        {
            if (target == null) yield break;
            var from = target.localScale;
            yield return LocalScale(target, from, to, duration, ease);
        }

        public static IEnumerator LocalScale(Transform target, Vector3 from, Vector3 to, float duration, Ease ease = Ease.SmoothStep)
        {
            if (target == null) yield break;
            duration = Mathf.Max(0.0001f, duration);
            float t = 0f;
            while (t < duration)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                float u = Evaluate(ease, t / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, u);
                yield return null;
            }
            if (target != null) target.localScale = to;
        }

        /// <summary>多个物体同进度位移动画（同一条时间轴）。</summary>
        public static IEnumerator LocalMoveMany(
            IList<(Transform target, Vector3 from, Vector3 to)> items,
            float duration,
            Ease ease = Ease.SmoothStep)
        {
            if (items == null || items.Count == 0) yield break;
            duration = Mathf.Max(0.0001f, duration);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Evaluate(ease, t / duration);
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it.target != null)
                        it.target.localPosition = Vector3.LerpUnclamped(it.from, it.to, u);
                }
                yield return null;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.target != null) it.target.localPosition = it.to;
            }
        }

        /// <summary>多个物体同进度缩放动画。</summary>
        public static IEnumerator LocalScaleMany(
            IList<(Transform target, Vector3 from, Vector3 to)> items,
            float duration,
            Ease ease = Ease.SmoothStep)
        {
            if (items == null || items.Count == 0) yield break;
            duration = Mathf.Max(0.0001f, duration);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Evaluate(ease, t / duration);
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it.target != null)
                        it.target.localScale = Vector3.LerpUnclamped(it.from, it.to, u);
                }
                yield return null;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.target != null) it.target.localScale = it.to;
            }
        }

        /// <summary>两物体互换局部坐标（棋盘交换用）。</summary>
        public static IEnumerator LocalSwap(
            Transform a, Vector3 aFrom, Vector3 aTo,
            Transform b, Vector3 bFrom, Vector3 bTo,
            float duration,
            Ease ease = Ease.SmoothStep)
        {
            duration = Mathf.Max(0.0001f, duration);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Evaluate(ease, t / duration);
                if (a != null) a.localPosition = Vector3.LerpUnclamped(aFrom, aTo, u);
                if (b != null) b.localPosition = Vector3.LerpUnclamped(bFrom, bTo, u);
                yield return null;
            }
            if (a != null) a.localPosition = aTo;
            if (b != null) b.localPosition = bTo;
        }

        /// <summary>SpriteRenderer alpha 淡出（可选）。</summary>
        public static IEnumerator FadeSprite(SpriteRenderer sr, float toAlpha, float duration, Ease ease = Ease.Linear)
        {
            if (sr == null) yield break;
            var c0 = sr.color;
            float from = c0.a;
            duration = Mathf.Max(0.0001f, duration);
            float t = 0f;
            while (t < duration)
            {
                if (sr == null) yield break;
                t += Time.deltaTime;
                float u = Evaluate(ease, t / duration);
                var c = sr.color;
                c.a = Mathf.LerpUnclamped(from, toAlpha, u);
                sr.color = c;
                yield return null;
            }
            if (sr != null)
            {
                var c = sr.color;
                c.a = toAlpha;
                sr.color = c;
            }
        }
    }
}
