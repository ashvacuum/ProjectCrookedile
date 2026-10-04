using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Opsive.Shared.Utility.Category("Crookedile/Boss")]
    public sealed class ChooseBossBundle : ActionNode
    {
        [Tooltip("Exact bundle name on the assigned BossData. Fails while its cooldown is active.")]
        [SerializeField]
        private string _bundleName;

        private BossBrain _brain;

        public string BundleName
        {
            get => _bundleName;
            set => _bundleName = value;
        }

        public override void OnAwake()
        {
            _brain = GetComponent<BossBrain>();
        }

        public override TaskStatus OnUpdate()
        {
            return _brain != null && _brain.Boss.TryChooseBundle(_bundleName)
                ? TaskStatus.Success
                : TaskStatus.Failure;
        }
    }
}
