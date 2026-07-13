using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCrush.View
{
    public class GoalHUD : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI countText;

        public Image Icon => icon;
        public RectTransform IconRect => icon != null ? icon.rectTransform : transform as RectTransform;

        public void SetIcon(Sprite sprite)
        {
            if (icon != null) icon.sprite = sprite;
        }

        public void SetRemaining(int remaining)
        {
            if (countText != null)
                countText.text = remaining.ToString();
        }

        /// <summary>把 UI 箱子图标中心转到世界坐标，供飞行特效对准。</summary>
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
