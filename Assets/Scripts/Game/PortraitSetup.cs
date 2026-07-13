using UnityEngine;

namespace CandyCrush.Game
{
    /// <summary>强制竖屏，并按竖屏比例适配正交相机与背景。</summary>
    [DefaultExecutionOrder(-100)]
    public class PortraitSetup : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] SpriteRenderer background;
        [SerializeField] float portraitOrthoSize = 8.2f;

        public void Bind(Camera cam, SpriteRenderer bg)
        {
            if (cam != null) targetCamera = cam;
            if (bg != null) background = bg;
            ApplyCamera();
        }

        void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

            if (targetCamera == null) targetCamera = Camera.main;
            ApplyCamera();
        }

        void LateUpdate()
        {
            if (targetCamera != null)
                FitBackground();
        }

        void ApplyCamera()
        {
            if (targetCamera == null) return;
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = portraitOrthoSize;
            FitBackground();
        }

        void FitBackground()
        {
            if (background == null || background.sprite == null || targetCamera == null) return;

            float worldH = targetCamera.orthographicSize * 2f;
            float worldW = worldH * targetCamera.aspect;
            var size = background.sprite.bounds.size;
            float scale = Mathf.Max(worldW / size.x, worldH / size.y);
            background.transform.localScale = new Vector3(scale, scale, 1f);
            background.transform.position = new Vector3(
                targetCamera.transform.position.x,
                targetCamera.transform.position.y,
                background.transform.position.z);
        }
    }
}
