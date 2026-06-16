using UnityEngine;
using UnityEngine.EventSystems;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Attach this to the HP bar area of an enemy slot (or any transparent Image overlay
    /// positioned over the bar). On pointer-enter the enemy name label fades in;
    /// on pointer-exit it fades back out.
    ///
    /// Inspector setup:
    ///   1. Add this component to the HP bar Image (or a transparent overlay Image on top of it).
    ///   2. Ensure the Image has <b>Raycast Target = true</b>.
    ///   3. Assign the parent <see cref="EnemySlotUI"/> to the <c>_slot</c> field.
    /// </summary>
    public class HPBarHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip(
            "The EnemySlotUI that owns this HP bar. "
                + "The trigger calls ShowNameLabel / HideNameLabel on it."
        )]
        [SerializeField]
        private EnemySlotUI _slot;

        public void OnPointerEnter(PointerEventData eventData) => _slot?.ShowNameLabel();

        public void OnPointerExit(PointerEventData eventData) => _slot?.HideNameLabel();
    }
}
