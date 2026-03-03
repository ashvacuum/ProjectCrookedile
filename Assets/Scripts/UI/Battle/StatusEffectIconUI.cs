using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays a single active status effect as an icon pill: [icon] [stack count].
    /// Placed on the <c>StatusEffectIconPrefab</c> and managed by <see cref="StatusEffectPanelUI"/>.
    ///
    /// Setup: Assign <see cref="_icon"/> and <see cref="_stackText"/> in the Inspector.
    /// The stack count text is hidden when stacks ≤ 1 so permanent and single-stack
    /// effects display cleanly without a redundant "1" label.
    /// </summary>
    public class StatusEffectIconUI : MonoBehaviour
    {
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _stackText;

        /// <summary>
        /// Full initialisation — sets the icon sprite, tint color, and initial stack count.
        /// Call once when the icon is first created for an effect.
        /// </summary>
        public void Setup(Sprite icon, Color color, int stacks)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.color  = color;
            }
            Refresh(stacks);
        }

        /// <summary>
        /// Updates only the stack count display. Call each time the effect stacks change.
        /// </summary>
        public void Refresh(int stacks)
        {
            if (_stackText == null) return;
            bool showCount = stacks > 1;
            _stackText.gameObject.SetActive(showCount);
            if (showCount) _stackText.text = stacks.ToString();
        }
    }
}
