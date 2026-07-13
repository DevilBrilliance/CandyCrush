using CandyCrush.Common;
using UnityEngine;

namespace CandyCrush.View
{
    /// <summary>
    /// 宿主保持 Active 以订阅 EventBus；通过 root（通常为 Visual 子节点）显隐内容。
    /// </summary>
    public class WinPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;

        void OnEnable() => EventBus.Subscribe<LevelWinEvent>(OnLevelWin);
        void OnDisable() => EventBus.Unsubscribe<LevelWinEvent>(OnLevelWin);

        void Awake()
        {
            if (root == null && transform.childCount > 0)
                root = transform.GetChild(0).gameObject;
            Hide();
        }

        public void Bind(GameObject visualRoot)
        {
            root = visualRoot;
            Hide();
        }

        void OnLevelWin(LevelWinEvent _) => Show();

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            if (root != null)
                root.SetActive(true);
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            if (root != null)
                root.SetActive(false);
        }
    }
}
