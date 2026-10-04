using System.Threading;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using Opsive.BehaviorDesigner.Runtime;
using Opsive.BehaviorDesigner.Runtime.Components;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Runs Behaviour Designer only during intent declaration so the boss commits its full move bundle before player input.</summary>
    [Debuggable("Boss", LogLevel.Info)]
    public sealed class BossBrain : MonoBehaviour
    {
        private const int MAX_PLANNING_TICKS = 64;
        private BehaviorTree _tree;
        private CancellationTokenSource _planningCancellation;

        public BattleManager Battle { get; private set; }
        public BossController Boss { get; private set; }

        /// <summary>Binds the rival to its battle and configures manual tree evaluation to prevent replanning during card play.</summary>
        public void Bind(BattleManager battle, BossController boss)
        {
            Battle = battle;
            Boss = boss;
            if (boss.Data.Behavior == null)
                return;

            _tree =
                gameObject.AddComponent<Opsive.BehaviorDesigner.Runtime.Wrappers.BehaviorTree>();
            _tree.StartWhenEnabled = false;
            _tree.UpdateMode = UpdateMode.Manual;
            _tree.EvaluationType = EvaluationType.EntireTree;
            _tree.Subgraph = boss.Data.Behavior;
        }

        /// <summary>Chooses a bundle within 64 planning ticks, attempts fallback if needed, then stops the tree and locks audience targets before revealing intents.</summary>
        public async UniTask DeclarePlan(CancellationToken cancellationToken)
        {
            _planningCancellation?.Dispose();
            _planningCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            cancellationToken = _planningCancellation.Token;
            Boss.BeginPlanning(Battle.PlayerTurnNumber);
            if (_tree != null)
            {
                _tree.StartBehavior();
                for (int i = 0; i < MAX_PLANNING_TICKS && Boss.Intents.Count == 0; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _tree.Tick();
                    if (Boss.Intents.Count == 0)
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
                _tree.StopBehavior(false);
            }

            if (!Boss.CommitPlan())
                GameLogger.LogError<BossBrain>(
                    $"{Boss.Data.DisplayName}: no valid boss plan or fallback."
                );

            Boss.LockAudienceTargets(Battle.Enemies);

            Crookedile.Core.EventBus.Publish(new BossIntentsDeclaredEvent { Boss = Boss });
        }

        /// <summary>Cancels pending declaration and stops the tree so an ended or restarted battle cannot continue planning.</summary>
        public void StopPlanning()
        {
            _planningCancellation?.Cancel();
            if (_tree != null)
                _tree.StopBehavior(false);
        }

        private void OnDestroy()
        {
            StopPlanning();
            _planningCancellation?.Dispose();
        }
    }
}
