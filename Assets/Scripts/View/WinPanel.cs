using CandyCrush.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCrush.View
{
    /// <summary>
    /// 本组件所在物体须保持 Active 才能订阅 EventBus。
    /// 隐藏时优先关 root 子节点；若 root 即自身则关掉子物体与自身 Image，不 SetActive(false)。
    /// </summary>
    public class WinPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;

        void OnEnable() => EventBus.Subscribe<LevelWinEvent>(OnLevelWin);
        void OnDisable() => EventBus.Unsubscribe<LevelWinEvent>(OnLevelWin);

        void Awake()
        {
            if (root == null) root = gameObject;
            Hide();
        }

        void OnLevelWin(LevelWinEvent _) => Show();

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (root != null && root != gameObject)
            {
                root.SetActive(true);
                return;
            }

            SetSelfVisual(true);
        }

        public void Hide()
        {
            if (root != null && root != gameObject)
            {
                root.SetActive(false);
                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);
                return;
            }

            // root == 自身：保持 Active 以便 EventBus，只关视觉
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            SetSelfVisual(false);
        }

        void SetSelfVisual(bool visible)
        {
            var img = GetComponent<Image>();
            if (img != null) img.enabled = visible;

            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(visible);
        }
    }
}
