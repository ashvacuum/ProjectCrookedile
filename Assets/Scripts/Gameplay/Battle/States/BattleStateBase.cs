using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Shared base for the battle's FSM states — holds the owning BattleManager.
    /// States live in their own files (Battle/States/) and reach the manager through
    /// its public API plus the internal state-machine surface (same assembly).
    /// </summary>
    internal abstract class BattleStateBase : State
    {
        protected readonly BattleManager _manager;

        protected BattleStateBase(BattleManager manager) => _manager = manager;
    }
}
