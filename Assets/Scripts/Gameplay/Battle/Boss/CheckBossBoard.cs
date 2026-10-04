using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    public enum BossBoardMetric
    {
        OpinionPercent = 0,
        ReceptiveAudience = 1,
        HostileAudience = 2,
        PlayerTurn = 3,
        Support = 4,
        Denial = 5,
    }

    [Opsive.Shared.Utility.Category("Crookedile/Boss")]
    public sealed class CheckBossBoard : ConditionalNode
    {
        [Tooltip("Public battle state to test. Audience counts never include the rival.")]
        [SerializeField]
        private BossBoardMetric _metric;

        [Tooltip(
            "Pass if the value is at least this threshold; otherwise test whether it is below."
        )]
        [SerializeField]
        private bool _atLeast = true;

        [Tooltip(
            "Threshold in percent, audience members, turns, or shield units according to Metric."
        )]
        [SerializeField]
        private float _threshold;

        private BossBrain _brain;

        public BossBoardMetric Metric
        {
            get => _metric;
            set => _metric = value;
        }
        public float Threshold
        {
            get => _threshold;
            set => _threshold = value;
        }

        public override void OnAwake()
        {
            _brain = GetComponent<BossBrain>();
        }

        public override TaskStatus OnUpdate()
        {
            if (_brain == null)
                return TaskStatus.Failure;

            var battle = _brain.Battle;
            float value = 0;
            switch (_metric)
            {
                case BossBoardMetric.OpinionPercent:
                    value = battle.OpinionPercentage * 100f;
                    break;
                case BossBoardMetric.PlayerTurn:
                    value = battle.PlayerTurnNumber;
                    break;
                case BossBoardMetric.Support:
                    value = battle.CurrentSupport;
                    break;
                case BossBoardMetric.Denial:
                    value = battle.CurrentDenial;
                    break;
                default:
                    foreach (var enemy in battle.Enemies)
                        if (
                            !enemy.IsDefeated
                            && (
                                _metric == BossBoardMetric.ReceptiveAudience
                                    ? enemy.Stats.IsReceptive
                                    : enemy.Stats.IsHostile
                            )
                        )
                            value++;
                    break;
            }

            bool passes = _atLeast ? value >= _threshold : value < _threshold;
            return passes ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
