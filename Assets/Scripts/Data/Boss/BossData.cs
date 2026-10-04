using System.Collections.Generic;
using Crookedile.Data.Enemy;
using Opsive.BehaviorDesigner.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Boss
{
    [CreateAssetMenu(fileName = "New Boss", menuName = "Crookedile/Boss")]
    public sealed class BossData : ScriptableObject
    {
        [Tooltip("Rival's name shown on the debate podium.")]
        [SerializeField]
        private string _displayName;

        [Tooltip("Rival's portrait. The rival has no HP or audience slot.")]
        [SerializeField]
        private Sprite _portrait;

        [Tooltip("Behavior Designer subtree that selects a bundle during intent declaration only.")]
        [InlineButton(nameof(OpenBehavior), "Open graph")]
        [SerializeField]
        private Subtree _behavior;

        [Tooltip("Plans available to this rival. Moves and their effects are edited inline.")]
        [ListDrawerSettings(ListElementLabelName = "Name")]
        [SerializeField]
        private List<BossMoveBundle> _bundles = new List<BossMoveBundle>();

        [Tooltip(
            "Always-available bundle used if the tree fails to select a plan. Must have no cooldown."
        )]
        [Min(0)]
        [SerializeField]
        private int _fallbackBundleIndex;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public Sprite Portrait => _portrait;
        public Subtree Behavior => _behavior;
        public IReadOnlyList<BossMoveBundle> Bundles => _bundles;
        public int FallbackBundleIndex => _fallbackBundleIndex;

        private void OpenBehavior()
        {
#if UNITY_EDITOR
            if (_behavior != null)
                UnityEditor.AssetDatabase.OpenAsset(_behavior);
#endif
        }

        public IEnumerable<string> GetConfigurationIssues()
        {
            if (_behavior == null)
                yield return "No Behaviour Designer planning subtree.";

            if (_bundles == null || _bundles.Count == 0)
            {
                yield return "No move bundles.";
                yield break;
            }

            var names = new HashSet<string>();
            foreach (var bundle in _bundles)
            {
                if (bundle == null || !bundle.IsValid())
                {
                    yield return "Every bundle must contain exactly two or three non-null moves.";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(bundle.Name) || !names.Add(bundle.Name))
                    yield return "Bundle names must be non-empty and unique.";

                foreach (var move in bundle.Moves)
                {
                    if (move.Condition != EnemyMoveCondition.None)
                        yield return $"{move.name}: choose conditional bundles in the tree; move conditions are not evaluated for bosses.";

                    foreach (var effect in move.Effects)
                    {
                        if (effect == null)
                            yield return $"{move.name}: empty effect.";
                        else if (effect is Crookedile.Gameplay.Battle.DelayedEffect)
                            yield return $"{move.name}: delayed player effects are unsupported; use a future planning bundle.";
                        else if (
                            effect.Target == TargetType.Adjacent
                            || effect.Target == TargetType.AdjacentAllies
                        )
                            yield return $"{move.name}: a rival has no row adjacency.";
                    }
                }
            }

            if (
                _fallbackBundleIndex < 0
                || _fallbackBundleIndex >= _bundles.Count
                || _bundles[_fallbackBundleIndex] == null
                || !_bundles[_fallbackBundleIndex].IsValid()
                || _bundles[_fallbackBundleIndex].CooldownTurns != 0
            )
                yield return "Fallback must be a valid bundle with no cooldown.";
        }
    }
}
