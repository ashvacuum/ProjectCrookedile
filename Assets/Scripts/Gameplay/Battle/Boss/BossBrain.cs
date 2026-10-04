using System.Threading;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using Opsive.BehaviorDesigner.Runtime;
using Opsive.BehaviorDesigner.Runtime.Components;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Debuggable("Boss", LogLevel.Info)]
    public sealed class BossBrain : MonoBehaviour
    {
        private const int MAX_PLANNING_TICKS = 64;
        private BehaviorTree _tree;
        private CancellationTokenSource _planningCancellation;

        public BattleManager Battle { get; private set; }
        public BossController Boss { get; private set; }

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
