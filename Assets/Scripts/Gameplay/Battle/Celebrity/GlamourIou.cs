using System;
using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Celebrity (Glamour / IOU build) engine tunables. Card numbers live on the card assets; only
    /// the rules every card shares are here. Composure = Support, Sway = an Opinion push.
    /// </summary>
    public static class CelebrityRules
    {
        /// <summary>Glamour removed after each tick.</summary>
        public const int GlamourDecayPerTick = 1;

        /// <summary>Glamour stripped by each Opinion hit that leaks past Support (flat, not scaled to damage).</summary>
        public const int ScrutinyStripPerHit = 1;

        /// <summary>Unpaid Debt to Opinion damage.</summary>
        public const int DebtToOpinionRate = 1;

        /// <summary>Whether Support absorbs unpaid-Debt damage. Default: unblockable.</summary>
        public const bool UnpaidDebtBlockable = false;

        /// <summary>Max Debt gained per turn; 0 = uncapped.</summary>
        public const int MaxDebtPerTurn = 0;

        /// <summary>Card tag marking a Borrow card (Open Tab discounts the first one each turn).</summary>
        public const string BorrowTag = "borrow";
    }

    /// <summary>
    /// Player-side stacking status. Ticks after the enemy turn (BattleManager.EndTurn): pushes Opinion
    /// up by its stacks, then decays. Each Opinion hit that leaks past Support strips stacks.
    /// </summary>
    [Serializable]
    public sealed class GlamourStatus : StatusBehavior
    {
        public override string Id => "glamour";
        public override string DisplayName => "Glamour";
        public override bool IsDebuff => false;

        public override string Describe(int stacks) =>
            $"After the enemy turn, raise Opinion by {stacks}, then lose {CelebrityRules.GlamourDecayPerTick}. "
            + $"Each Opinion hit that gets past Support strips {CelebrityRules.ScrutinyStripPerHit}.";
    }

    /// <summary>
    /// Per-battle Debt and the Debt-policy flags (Line of Credit, Open Tab, Bailout, ...). Debt is
    /// settled at the start of the player's next turn and never collected when the fight ends.
    /// </summary>
    public class CelebrityState
    {
        public int Debt { get; private set; }
        public int DebtGainedThisTurn { get; private set; }

        // Rule magnitudes, set by DebtRuleEffect. Each is a count so upgraded cards can carry a
        // bigger number, and a second copy of the same Policy stacks onto the first.

        /// <summary>Line of Credit: Debt shaved off the first Debt gain each turn.</summary>
        public int LineOfCreditReduction { get; set; }

        /// <summary>Open Tab: Borrow cards per turn that cost 0.</summary>
        public int FreeBorrowsPerTurn { get; set; }
        public int BorrowsPlayedThisTurn { get; set; }

        /// <summary>Overdraft: extra multiples of Borrow energy/Debt this turn (1 = double).</summary>
        public int BorrowBonusMultiplesThisTurn { get; set; }

        /// <summary>Rain Check: upcoming settlements skipped.</summary>
        public int SettlementsDelayed { get; set; }

        /// <summary>Too Big to Fail: upcoming unpaid-Debt damage instances negated.</summary>
        public int DebtWaivers { get; set; }

        /// <summary>Bailout: Soundbite added per unpaid-Debt damage, up to the per-turn cap.</summary>
        public CardData BailoutCard { get; set; }
        public int BailoutCapPerTurn { get; set; }

        private bool _lineOfCreditUsedThisTurn;
        private int _bailoutTokensThisTurn;

        public void ResetBattle()
        {
            Debt = 0;
            LineOfCreditReduction = FreeBorrowsPerTurn = SettlementsDelayed = DebtWaivers = 0;
            BailoutCard = null;
            BailoutCapPerTurn = 0;
            ResetTurn();
        }

        /// <summary>Per-player-turn tallies; call at the start of each player turn.</summary>
        public void ResetTurn()
        {
            DebtGainedThisTurn = BorrowsPlayedThisTurn = BorrowBonusMultiplesThisTurn = 0;
            _lineOfCreditUsedThisTurn = false;
            _bailoutTokensThisTurn = 0;
        }

        /// <summary>True while Open Tab still has a free Borrow left this turn.</summary>
        public bool NextBorrowIsFree => BorrowsPlayedThisTurn < FreeBorrowsPerTurn;

        /// <summary>Adds Debt after Line of Credit and the per-turn cap. Returns the Debt actually added.</summary>
        public int GainDebt(int amount)
        {
            if (LineOfCreditReduction > 0 && !_lineOfCreditUsedThisTurn && amount > 0)
            {
                _lineOfCreditUsedThisTurn = true;
                amount -= LineOfCreditReduction;
            }

            if (CelebrityRules.MaxDebtPerTurn > 0)
                amount = Mathf.Min(amount, CelebrityRules.MaxDebtPerTurn - DebtGainedThisTurn);

            if (amount <= 0)
                return 0;
            Debt += amount;
            DebtGainedThisTurn += amount;
            return amount;
        }

        /// <summary>Cancels up to <paramref name="max"/> Debt (0 = all). Returns the amount cancelled.</summary>
        public int Forgive(int max)
        {
            int forgiven = max <= 0 ? Debt : Mathf.Min(max, Debt);
            Debt -= forgiven;
            return forgiven;
        }

        /// <summary>
        /// Start-of-player-turn settlement: Debt eats starting energy (floor 0), the rest becomes
        /// Opinion damage. Not an enemy "hit", so it never strips Glamour.
        /// </summary>
        public void Settle(
            OpinionLedger ledger,
            BattleStats player,
            Func<CardData, int, int> addToHand
        )
        {
            if (SettlementsDelayed > 0)
            {
                SettlementsDelayed--; // Rain Check: Debt stays on the books one more turn
                return;
            }
            int owed = Debt;
            if (owed <= 0)
                return;
            Debt = 0;

            int paid = Mathf.Min(owed, player.CurrentActionPoints);
            player.GainActionPoints(-paid);

            int damage = (owed - paid) * CelebrityRules.DebtToOpinionRate;
            if (damage > 0 && DebtWaivers > 0)
            {
                DebtWaivers--; // Too Big to Fail
                damage = 0;
            }
            if (damage <= 0)
                return;

            if (CelebrityRules.UnpaidDebtBlockable)
                ledger.ApplyOpinionShift(damage, true, "Debt", -1, -1, isHit: false);
            else
                ledger.DecayOpinion(damage);

            if (BailoutCard != null)
            {
                int tokens = Mathf.Min(
                    damage,
                    BailoutCapPerTurn - _bailoutTokensThisTurn
                );
                if (tokens > 0)
                {
                    _bailoutTokensThisTurn += tokens;
                    addToHand(BailoutCard, tokens);
                }
            }
        }
    }
}
