using System.Collections;
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

        void Awake() => EnsureCountVisible();

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

        public Vector3 GetIconWorldPosition(Camera worldCam)
        {
            var rt = IconRect;
            if (rt == null)
                return Vector3.zero;

            if (worldCam == null) worldCam = Camera.main;

            var canvas = rt.GetComponentInParent<Canvas>();
            Camera uiCam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = canvas.worldCamera != null ? canvas.worldCamera : worldCam;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
            if (worldCam == null)
                return rt.position;

            float depth = Mathf.Abs(worldCam.transform.position.z);
            var world = worldCam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            world.z = 0f;
            return world;
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
