using CandyCrush.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCrush.View
{
    /// <summary>
    /// 目标 / 胜利 UI 构建。Editor 用其生成预制体；运行时仅在预制体缺失时作兜底。
    /// </summary>
    public static class GameUiFactory
    {
        public const string GoalHudResourcePath = "Prefabs/UI/GoalHUD";
        public const string WinPanelResourcePath = "Prefabs/UI/WinPanel";

        public static Canvas EnsureOverlayCanvas()
        {
            var existing = Object.FindObjectOfType<Canvas>();
            if (existing != null)
            {
                if (existing.renderMode != RenderMode.ScreenSpaceOverlay)
                    existing.renderMode = RenderMode.ScreenSpaceOverlay;
                if (existing.GetComponent<CanvasScaler>() == null)
                {
                    var scaler = existing.gameObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                    scaler.matchWidthOrHeight = 0.5f;
                }
                if (existing.GetComponent<GraphicRaycaster>() == null)
                    existing.gameObject.AddComponent<GraphicRaycaster>();
                return existing.rootCanvas != null ? existing.rootCanvas : existing;
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scalerNew = canvasGo.AddComponent<CanvasScaler>();
            scalerNew.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scalerNew.referenceResolution = new Vector2(1080, 1920);
            scalerNew.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        public static GoalHUD CreateGoalHud(Transform canvasParent, Sprite panelSprite, Sprite iconSprite, Font font)
        {
            var goalPanel = new GameObject("GoalHUD", typeof(RectTransform));
            goalPanel.transform.SetParent(canvasParent, false);
            var goalRt = goalPanel.GetComponent<RectTransform>();
            goalRt.anchorMin = new Vector2(0.5f, 1f);
            goalRt.anchorMax = new Vector2(0.5f, 1f);
            goalRt.pivot = new Vector2(0.5f, 1f);
            goalRt.anchoredPosition = new Vector2(0f, -40f);
            goalRt.sizeDelta = new Vector2(420f, 120f);

            var goalImg = goalPanel.AddComponent<Image>();
            goalImg.sprite = panelSprite;
            goalImg.type = Image.Type.Sliced;
            goalImg.preserveAspect = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(goalPanel.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.28f, 0.5f);
            iconRt.sizeDelta = new Vector2(72f, 72f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;

            var textGo = new GameObject("Count", typeof(RectTransform));
            textGo.transform.SetParent(goalPanel.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = textRt.anchorMax = new Vector2(0.62f, 0.5f);
            textRt.sizeDelta = new Vector2(160f, 80f);
            var uiText = textGo.AddComponent<Text>();
            uiText.text = "0";
            uiText.fontSize = 64;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = new Color(0.32f, 0.2f, 0.12f, 1f);
            uiText.fontStyle = FontStyle.Bold;
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            if (font != null) uiText.font = font;

            var goalHud = goalPanel.AddComponent<GoalHUD>();
            goalHud.Bind(iconImg, uiText, font);
            return goalHud;
        }

        public static WinPanel CreateWinPanel(Transform canvasParent, Sprite suitcaseSprite, Font font)
        {
            // 宿主常驻 Active（EventBus）；Visual 子节点才是可显隐内容
            var host = new GameObject("WinPanel", typeof(RectTransform));
            host.transform.SetParent(canvasParent, false);
            var hostRt = host.GetComponent<RectTransform>();
            hostRt.anchorMin = Vector2.zero;
            hostRt.anchorMax = Vector2.one;
            hostRt.offsetMin = hostRt.offsetMax = Vector2.zero;

            var visual = new GameObject("Visual", typeof(RectTransform));
            visual.transform.SetParent(host.transform, false);
            var visualRt = visual.GetComponent<RectTransform>();
            visualRt.anchorMin = Vector2.zero;
            visualRt.anchorMax = Vector2.one;
            visualRt.offsetMin = visualRt.offsetMax = Vector2.zero;

            var winBg = visual.AddComponent<Image>();
            winBg.color = new Color(0f, 0f, 0f, 0.45f);
            winBg.raycastTarget = true;

            var greatGo = new GameObject("GreatText", typeof(RectTransform));
            greatGo.transform.SetParent(visual.transform, false);
            var greatRt = greatGo.GetComponent<RectTransform>();
            greatRt.anchorMin = greatRt.anchorMax = new Vector2(0.5f, 0.72f);
            greatRt.sizeDelta = new Vector2(600f, 140f);
            var greatText = greatGo.AddComponent<Text>();
            greatText.text = "Great";
            greatText.fontSize = 96;
            greatText.alignment = TextAnchor.MiddleCenter;
            greatText.color = new Color(1f, 0.85f, 0.2f);
            greatText.fontStyle = FontStyle.Bold;
            greatText.horizontalOverflow = HorizontalWrapMode.Overflow;
            greatText.verticalOverflow = VerticalWrapMode.Overflow;
            greatText.raycastTarget = false;
            if (font != null) greatText.font = font;

            var winSuitcase = new GameObject("WinSuitcase", typeof(RectTransform));
            winSuitcase.transform.SetParent(visual.transform, false);
            var wsRt = winSuitcase.GetComponent<RectTransform>();
            wsRt.anchorMin = wsRt.anchorMax = new Vector2(0.5f, 0.5f);
            wsRt.sizeDelta = new Vector2(160f, 160f);
            var wsImg = winSuitcase.AddComponent<Image>();
            wsImg.sprite = suitcaseSprite;
            wsImg.preserveAspect = true;
            wsImg.raycastTarget = false;

            var winPanel = host.AddComponent<WinPanel>();
            winPanel.Bind(visual);
            winPanel.Hide();
            return winPanel;
        }
    }
}
