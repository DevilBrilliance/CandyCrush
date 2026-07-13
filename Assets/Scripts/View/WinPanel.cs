using UnityEngine;

namespace CandyCrush.View
{
    public class WinPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;

        void Awake()
        {
            if (root == null) root = gameObject;
            Hide();
        }

        public void Show()
        {
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
