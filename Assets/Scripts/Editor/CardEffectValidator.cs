using System.Text;
using Crookedile.Data.Cards;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Read-only diagnostic: scans every <see cref="CardData"/> asset and reports how many have an
    /// empty runtime effect list. Used to confirm whether the on-disk card assets are stale relative
    /// to the current serialization (effects were stored under a former field name, so they may load
    /// empty under the current <c>_effects</c> [SerializeReference] field).
    ///
    /// Menu: Crookedile → Validate Card Effects. Logs a summary to the Console. Mutates nothing.
    /// </summary>
    public static class CardEffectValidator
    {
        [MenuItem("Crookedile/Validate Card Effects")]
        public static void Validate()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            int total = 0;
            int empty = 0;
            var sb = new StringBuilder();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card == null)
                    continue;

                total++;
                int baseCount = card.Effects?.Count ?? 0;
                int upgradedCount = card.UpgradedEffects?.Count ?? 0;
                int passiveCount = card.Passives?.Count ?? 0;

                // A Power card carries its mechanics in passives, not effects — only flag
                // cards that have neither.
                if (baseCount == 0 && passiveCount == 0)
                {
                    empty++;
                    sb.AppendLine(
                        $"  EMPTY  base=0 passives=0 upgraded={upgradedCount}  {card.name}  ({path})"
                    );
                }
            }

            Debug.Log(
                $"[CardEffectValidator] Scanned {total} CardData assets — {empty} have ZERO base effects "
                    + $"(stale/unauthored).\n{sb}"
            );
        }
    }
}
