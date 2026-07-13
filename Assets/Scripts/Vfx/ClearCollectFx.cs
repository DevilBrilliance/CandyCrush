using System.Collections;
using System.Collections.Generic;
using CandyCrush.Core;
using CandyCrush.Data;
using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>行李箱收集：从棋盘格心弧线飞到 GoalHUD 箱子图标。</summary>
    public static class CollectFx
    {
        const int FlySortingOrder = 150;

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

        /// <summary>
        /// 箱子从格心飞向 UI：错开起飞，到达时倒数 HUD，全部到齐后 punch 图标。
        /// </summary>
        public static IEnumerator FlySuitcases(
            IReadOnlyList<GridPos> cells,
            BoardView board,
            GoalHUD goalHud,
            TileSpriteCatalog catalog,
            int remainingAfter,
            float flyDuration = 0.65f,
            float stagger = 0.07f)
        {
            if (cells == null || cells.Count == 0 || board == null || goalHud == null) yield break;

            var sprite = catalog != null ? catalog.GetSprite(TileType.Suitcase) : null;
            if (sprite == null && goalHud.Icon != null)
                sprite = goalHud.Icon.sprite;
            if (sprite == null) yield break;

            var cam = Camera.main;
            var target = goalHud.GetIconWorldPosition(cam);
            var root = board.transform;
            float cellSize = board.CellSize;

            // 飞行中仍显示收集前数量，每到一只减一
            int display = remainingAfter + cells.Count;
            goalHud.SetRemaining(display);

            int arrived = 0;
            int total = cells.Count;

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                var start = board.transform.TransformPoint(board.CellLocal(cell.Row, cell.Col));
                float delay = i * stagger;

                goalHud.StartCoroutine(FlyOne(
                    root, sprite, start, target, cellSize, flyDuration, delay,
                    () =>
                    {
                        arrived++;
                        display = Mathf.Max(remainingAfter, display - 1);
                        goalHud.SetRemaining(display);
                        if (arrived >= total)
                            goalHud.StartCoroutine(goalHud.PunchIcon());
                    }));
            }

            // 不阻塞整段飞行：只略等起飞错开，棋盘可继续连锁
            float warmup = Mathf.Min(0.12f, stagger * cells.Count);
            float t = 0f;
            while (t < warmup)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        static IEnumerator FlyOne(
            Transform parent,
            Sprite sprite,
            Vector3 start,
            Vector3 end,
            float cellSize,
            float duration,
            float delay,
            System.Action onArrive)
        {
            if (delay > 0f)
            {
                float d = 0f;
                while (d < delay)
                {
                    d += Time.deltaTime;
                    yield return null;
                }
            }

            var go = new GameObject("FlyingSuitcase");
            go.transform.SetParent(parent, true);
            go.transform.position = start;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = FlySortingOrder;

            float baseScale = FitScale(sprite, cellSize * 0.92f);
            go.transform.localScale = Vector3.one * baseScale;

            Vector3 mid = Vector3.Lerp(start, end, 0.4f);
            mid.y += Mathf.Max(1.1f, Vector3.Distance(start, end) * 0.32f);

            float t = 0f;
            float dur = duration > 0.05f ? duration : 0.65f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2.15f);
                go.transform.position = QuadBezier(start, mid, end, ease);

                float pop = 1f + 0.16f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 1.5f));
                float shrink = Mathf.Lerp(1f, 0.5f, ease);
                go.transform.localScale = Vector3.one * (baseScale * pop * shrink);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI) * 16f);
                yield return null;
            }

            go.transform.position = end;
            Object.Destroy(go);
            onArrive?.Invoke();
        }

        static Vector3 QuadBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        static float FitScale(Sprite sprite, float targetSize)
        {
            var size = sprite.bounds.size;
            float max = Mathf.Max(size.x, size.y);
            if (max < 0.0001f) return 1f;
            return targetSize / max;
        }
    }

    /// <summary>消除闪光占位（已由 ClearBurstFx 接管碎裂粒子）。</summary>
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
            var tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 4f);
            go.transform.localScale = Vector3.one * 0.6f;
            Object.Destroy(go, 0.2f);
        }
    }
}
