using Crookedile.Data.Battle;
using Crookedile.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays a single active status effect as an icon pill.
    /// Placed on the <c>StatusEffectIconPrefab</c> and managed by <see cref="StatusEffectPanelUI"/>.
    ///
    /// Hovering the icon opens the shared <see cref="BattleTooltipUI"/> with the effect's
    /// name, description, and current stack count.
    ///
    /// The stack count badge (<see cref="_stackText"/>) is shown whenever stacks &gt; 1;
    /// hidden for single-stack effects to keep the icon clean.
    ///
    /// Setup: Assign <see cref="_icon"/>, <see cref="_stackText"/> (can be null), and
    /// <see cref="_iconMap"/> in the Inspector (same asset as <see cref="StatusEffectPanelUI"/>).
    /// </summary>
    public class StatusEffectIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _stackText;

        [Tooltip(
            "Same StatusEffectIconMapSO asset assigned to StatusEffectPanelUI — provides tooltip text."
        )]
        [SerializeField]
        private StatusEffectIconMapSO _iconMap;

        private StatusEffectType _type;
        private int _currentStacks;

        #region Initialisation
        /// <summary>
        /// Full initialisation — sets the icon sprite, tint color, and initial stack count.
        /// Call once when the icon is first created for an effect.
        /// </summary>
        public void Setup(StatusEffectType type, Sprite icon, Color color, int stacks)
        {
            _type = type;

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.color = color;
            }

            Refresh(stacks);
        }

        /// <summary>
        /// Updates the stored stack count and refreshes the badge text.
        /// Badge is hidden when stacks == 1 (single-stack effects read cleaner without a number).
        /// </summary>
        public void Refresh(int stacks)
        {
            _currentStacks = stacks;

            if (_stackText != null)
            {
                _stackText.gameObject.SetActive(stacks > 1);
                _stackText.text = stacks.ToString();
            }
        }

        #endregion

        #region Pointer Events
        public void OnPointerEnter(PointerEventData _)
        {
            if (_iconMap == null || BattleTooltipUI.Instance == null)
                return;

            _iconMap.TryGet(_type, out var icon, out var color, out var name, out var desc);

            // Default: fall back to the enum name when effectName is not authored in the SO
            if (string.IsNullOrEmpty(name))
                name = _type.ToString();

            // Template substitution: replace {a} with the current stack count
            if (!string.IsNullOrEmpty(desc))
                desc = desc.Replace("{a}", _currentStacks.ToString());

            string extraLine = _currentStacks > 1 ? $"Stacks: {_currentStacks}" : null;
            BattleTooltipUI.Instance.Show(name, desc, icon, color, extraLine);
        }

        public void OnPointerExit(PointerEventData _)
        {
            BattleTooltipUI.Instance?.Hide();
        }
    }
}
        #endregion
