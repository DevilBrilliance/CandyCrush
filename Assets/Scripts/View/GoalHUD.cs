using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCrush.View
{
    public class GoalHUD : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI countText;

        public void SetIcon(Sprite sprite)
        {
            if (icon != null) icon.sprite = sprite;
        }

        public void SetRemaining(int remaining)
        {
            if (countText != null)
                countText.text = remaining.ToString();
        }
    }
}
