using System.Collections;
using CandyCrush.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCrush.View
{
    public class GoalHUD : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Text countText;
        [SerializeField] Font sourceFont;

        public Image Icon => icon;
        public RectTransform IconRect => icon != null ? icon.rectTransform : transform as RectTransform;

        void OnEnable() => EventBus.Subscribe<ObjectiveChangedEvent>(OnObjectiveChanged);
        void OnDisable() => EventBus.Unsubscribe<ObjectiveChangedEvent>(OnObjectiveChanged);

        void Awake() => EnsureCountVisible();

        void OnObjectiveChanged(ObjectiveChangedEvent evt) => SetRemaining(evt.Remaining);

        public void Bind(Image iconImage, Text count, Font font)
        {
            icon = iconImage;
            countText = count;
            sourceFont = font;
            EnsureCountVisible();
        }

        void EnsureCountVisible()
        {
            if (countText == null) return;

            if (countText.font == null)
            {
                var src = sourceFont;
                if (src == null)
                    src = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (src != null)
                    countText.font = src;
            }

            countText.fontStyle = FontStyle.Bold;
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = new Color(0.32f, 0.2f, 0.12f, 1f);
            countText.horizontalOverflow = HorizontalWrapMode.Overflow;
            countText.verticalOverflow = VerticalWrapMode.Overflow;
            if (string.IsNullOrEmpty(countText.text))
                countText.text = "0";
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon != null) icon.sprite = sprite;
        }

        public void SetRemaining(int remaining)
        {
            EnsureCountVisible();
            if (countText != null)
                countText.text = remaining.ToString();
        }

        public IEnumerator PunchIcon(float duration = 0.2f)
        {
            var target = IconRect;
            if (target == null) yield break;
            var baseScale = target.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float s = 1f + Mathf.Sin(u * Mathf.PI) * 0.28f;
                target.localScale = baseScale * s;
                yield return null;
            }
            target.localScale = baseScale;
        }
    }
}
