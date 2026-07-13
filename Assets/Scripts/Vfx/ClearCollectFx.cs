using System.Collections;
using System.Collections.Generic;
using CandyCrush.Common;
using CandyCrush.Core;
using CandyCrush.Data;
using CandyCrush.View;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCrush.Vfx
{
    /// <summary>行李箱收集：从棋盘格心弧线飞到 GoalHUD（在 Canvas 上层，盖住 UI）。</summary>
    public static class CollectFx
    {
        /// <summary>
        /// 箱子从格心飞向 UI：错开起飞，到达时经 EventBus 倒数 HUD，全部到齐后 punch 图标。
        /// 使用 UI Image 挂在 Canvas 最上层，避免被 HUD 挡住。
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

            var canvas = goalHud.GetComponentInParent<Canvas>();
            if (canvas == null) yield break;
            canvas = canvas.rootCanvas;
            var canvasRt = canvas.transform as RectTransform;
            if (canvasRt == null) yield break;

            var flyLayer = EnsureFlyLayer(canvasRt);
            var worldCam = Camera.main;
            Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : (canvas.worldCamera != null ? canvas.worldCamera : worldCam);

            if (!WorldToCanvasLocal(canvasRt, uiCam, worldCam,
                    board.transform.TransformPoint(board.CellLocal(cells[0].Row, cells[0].Col)),
                    out _))
                yield break;

            Vector2 endLocal = IconToCanvasLocal(goalHud.IconRect, canvasRt, uiCam);

            float iconSize = 72f;
            if (goalHud.IconRect != null)
                iconSize = Mathf.Max(goalHud.IconRect.rect.width, goalHud.IconRect.rect.height);

            int display = remainingAfter + cells.Count;
            EventBus.Publish(new ObjectiveChangedEvent(display));

            int arrived = 0;
            int total = cells.Count;

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                var worldStart = board.transform.TransformPoint(board.CellLocal(cell.Row, cell.Col));
                if (!WorldToCanvasLocal(canvasRt, uiCam, worldCam, worldStart, out var startLocal))
                    continue;

                float delay = i * stagger;
                goalHud.StartCoroutine(FlyOneUi(
                    flyLayer, sprite, startLocal, endLocal, iconSize, flyDuration, delay,
                    () =>
                    {
                        arrived++;
                        display = Mathf.Max(remainingAfter, display - 1);
                        EventBus.Publish(new ObjectiveChangedEvent(display));
                        if (arrived >= total)
                            goalHud.StartCoroutine(goalHud.PunchIcon());
                    }));
            }

            float warmup = Mathf.Min(0.12f, stagger * cells.Count);
            float t = 0f;
            while (t < warmup)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        static RectTransform EnsureFlyLayer(RectTransform canvasRt)
        {
            const string name = "CollectFlyLayer";
            var existing = canvasRt.Find(name) as RectTransform;
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRt, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            // 独立 Canvas 强制画在最前（盖住普通 HUD）
            var overlay = go.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 500;
            go.AddComponent<GraphicRaycaster>().enabled = false;
            return rt;
        }

        static IEnumerator FlyOneUi(
            RectTransform layer,
            Sprite sprite,
            Vector2 start,
            Vector2 end,
            float iconSize,
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

            var go = new GameObject("FlyingSuitcase", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(layer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            rt.anchoredPosition = start;
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;

            Vector2 mid = Vector2.Lerp(start, end, 0.4f);
            mid.y += Mathf.Max(80f, Vector2.Distance(start, end) * 0.28f);

            float t = 0f;
            float dur = duration > 0.05f ? duration : 0.65f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2.15f);
                rt.anchoredPosition = QuadBezier(start, mid, end, ease);

                float pop = 1f + 0.16f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 1.5f));
                float shrink = Mathf.Lerp(1.15f, 0.85f, ease);
                rt.localScale = Vector3.one * (pop * shrink);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI) * 16f);
                yield return null;
            }

            rt.anchoredPosition = end;
            Object.Destroy(go);
            onArrive?.Invoke();
        }

        static bool WorldToCanvasLocal(
            RectTransform canvasRt,
            Camera uiCam,
            Camera worldCam,
            Vector3 worldPos,
            out Vector2 local)
        {
            local = Vector2.zero;
            if (worldCam == null) return false;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCam, worldPos);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, uiCam, out local);
        }

        static Vector2 IconToCanvasLocal(RectTransform icon, RectTransform canvasRt, Camera uiCam)
        {
            if (icon == null) return Vector2.zero;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCam, icon.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, uiCam, out var local);
            return local;
        }

        static Vector2 QuadBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
    }
}
