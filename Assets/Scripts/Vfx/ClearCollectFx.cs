using System.Collections;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>行李箱收集：数字 punch（飞行动画可后续增强）。</summary>
    public static class CollectFx
    {
        public static IEnumerator Punch(RectTransform target, float duration = 0.2f)
        {
            if (target == null) yield break;
            var baseScale = target.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float s = 1f + Mathf.Sin(u * Mathf.PI) * 0.25f;
                target.localScale = baseScale * s;
                yield return null;
            }
            target.localScale = baseScale;
        }
    }

    /// <summary>消除闪光占位（粒子可后续替换）。</summary>
    public static class ClearFx
    {
        public static void SpawnFlash(Transform parent, Vector3 localPos, Color color)
        {
            var go = new GameObject("ClearFlash");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(color.r, color.g, color.b, 0.85f);
            sr.sortingOrder = 40;
            // 用 1x1 白贴
            var tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 4f);
            go.transform.localScale = Vector3.one * 0.6f;
            Object.Destroy(go, 0.2f);
        }
    }
}
