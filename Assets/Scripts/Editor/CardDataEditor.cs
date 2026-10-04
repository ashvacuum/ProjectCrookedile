using Crookedile.Data.Cards;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Crookedile.EditorTools
{
    /// <summary>Uses Odin's polymorphic type picker for card effects in both inspectors and the Database window.</summary>
    [CustomEditor(typeof(CardData), true)]
    [CanEditMultipleObjects]
    public class CardDataEditor : OdinEditor { }
}
