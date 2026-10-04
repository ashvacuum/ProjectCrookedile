using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Boss;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    public sealed class BossController
    {
        private readonly Dictionary<string, int> _lastChosenTurn = new Dictionary<string, int>();
        private IReadOnlyList<EnemyMoveData> _intents = Array.Empty<EnemyMoveData>();
        private int _planningTurn;
        private bool _isPlanning;
        private readonly List<Dictionary<TargetType, int>> _audienceTargets =
            new List<Dictionary<TargetType, int>>();

        public BossData Data { get; }
        public BattleStats Stats { get; }
        public StatusEffectManager StatusEffects { get; }
        public IReadOnlyList<EnemyMoveData> Intents => _intents;
        public string BundleName { get; private set; }
        public int ResolvedIntentCount { get; private set; }

        public BossController(BossData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Stats = new BattleStats(0, isPlayer: false);
            StatusEffects = new StatusEffectManager(data.DisplayName, Stats);
        }

        public void BeginPlanning(int playerTurn)
        {
            _planningTurn = playerTurn;
            _isPlanning = true;
            _intents = Array.Empty<EnemyMoveData>();
            BundleName = null;
            ResolvedIntentCount = 0;
            _audienceTargets.Clear();
        }

        public bool TryChooseBundle(string name)
        {
            if (!_isPlanning || _intents.Count != 0)
                return false;

            foreach (var bundle in Data.Bundles)
            {
                if (bundle == null || bundle.Name != name || !bundle.IsValid())
                    continue;

                if (
                    _lastChosenTurn.TryGetValue(name, out int lastTurn)
                    && _planningTurn - lastTurn <= bundle.CooldownTurns
                )
                    return false;

                // The revealed list is a snapshot; selection cannot change during card play.
                var moves = new EnemyMoveData[bundle.Moves.Count];
                for (int i = 0; i < moves.Length; i++)
                    moves[i] = bundle.Moves[i];
                _intents = Array.AsReadOnly(moves);
                BundleName = name;
                _lastChosenTurn[name] = _planningTurn;
                return true;
            }

            return false;
        }

        public bool CommitPlan()
        {
            if (
                _intents.Count == 0
                && Data.FallbackBundleIndex >= 0
                && Data.FallbackBundleIndex < Data.Bundles.Count
            )
                TryChooseBundle(Data.Bundles[Data.FallbackBundleIndex]?.Name);

            _isPlanning = false;
            return _intents.Count >= 2;
        }

        public bool TryTakeNextIntent(out EnemyMoveData move)
        {
            move = null;
            if (_isPlanning || ResolvedIntentCount >= _intents.Count)
                return false;

            move = _intents[ResolvedIntentCount];
            ResolvedIntentCount++;
            return true;
        }

        public void LockAudienceTargets(IReadOnlyList<EnemyController> audience)
        {
            _audienceTargets.Clear();
            foreach (var move in _intents)
            {
                var targets = new Dictionary<TargetType, int>();
                foreach (var effect in move.Effects)
                {
                    if (effect == null || targets.ContainsKey(effect.Target))
                        continue;

                    var target = effect.Target;
                    if (target != TargetType.RandomReceptive && target != TargetType.RandomHostile)
                        continue;

                    var candidates = new List<int>();
                    for (int i = 0; i < audience.Count; i++)
                        if (
                            !audience[i].IsDefeated
                            && (
                                target == TargetType.RandomReceptive
                                    ? audience[i].Stats.IsReceptive
                                    : audience[i].Stats.IsHostile
                            )
                        )
                            candidates.Add(i);
                    targets[target] =
                        candidates.Count == 0
                            ? -1
                            : candidates[UnityEngine.Random.Range(0, candidates.Count)];
                }
                _audienceTargets.Add(targets);
            }
        }

        public IReadOnlyDictionary<TargetType, int> GetAudienceTargets(int intentIndex)
        {
            return intentIndex >= 0 && intentIndex < _audienceTargets.Count
                ? _audienceTargets[intentIndex]
                : null;
        }
    }
}
