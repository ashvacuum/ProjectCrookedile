using System;
using System.Collections.Generic;
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

    /// <summary>Normal Debt settles at next turn start; promised Debt waits for its commitment's deadline.</summary>
    public class CelebrityState
    {
        public int Debt { get; private set; }
        public int DebtGainedThisTurn { get; private set; }
        public int TotalDebt => Debt + PromisedDebt;

        // Rule magnitudes, set by DebtRuleEffect. Each is a count so upgraded cards can carry a
        // bigger number, and a second copy of the same Policy stacks onto the first.

        /// <summary>Line of Credit: relief armed for the next Debt gain this turn.</summary>
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

        public int MediaTrainingCost { get; set; }
        public int MediaTrainingDraw { get; set; }
        public bool MediaTrainingAvailable { get; set; }
        public int NextSwayMultiplier { get; set; } = 1;
        private int _creditDiscount;
        private int _playerTurn;
        private readonly List<(int debt, int dueTurn, int minimumCost)> _promises = new();

        public int PromisedDebt
        {
            get
            {
                int total = 0;
                foreach (var promise in _promises)
                    total += promise.debt;
                return total;
            }
        }

        /// <summary>Arms only the next positive Debt gain; it expires at the next player turn.</summary>
        public void ArmCredit(int reduction, int discount)
        {
            LineOfCreditReduction = reduction;
            _creditDiscount = discount;
            _lineOfCreditUsedThisTurn = false;
        }

        /// <summary>Reserved Debt cannot settle before the player has a turn to fulfil the promise.</summary>
        public void MakePromise(int amount, int minimumCost)
        {
            int held = Mathf.Min(amount, Debt);
            if (held <= 0)
                return;

            Debt -= held;
            _promises.Add((held, _playerTurn + 1, minimumCost));
        }

        /// <summary>A successful hand play closes the draw window and can fulfil due commitments.</summary>
        public void OnCardPlayed(CardData card, int energyPaid = 0)
        {
            MediaTrainingAvailable = false;
            bool isX = card.GetCosts().Exists(cost => cost.IsXCost);
            for (int i = _promises.Count - 1; i >= 0; i--)
                if (_promises[i].dueTurn == _playerTurn
                    && (card.PrintedCost >= _promises[i].minimumCost || isX && energyPaid >= _promises[i].minimumCost))
                    _promises.RemoveAt(i);
        }

        /// <summary>Only missed commitments come due at turn end; fresh Debt keeps its normal deadline.</summary>
        public void SettlePromises(OpinionLedger ledger, BattleStats player, Func<CardData, int, int> addToHand)
        {
            int owed = 0;
            for (int i = _promises.Count - 1; i >= 0; i--)
            {
                if (_promises[i].dueTurn > _playerTurn)
                    continue;

                owed += _promises[i].debt;
                _promises.RemoveAt(i);
            }
            Collect(owed, ledger, player, addToHand);
        }

        public void ResetBattle()
        {
            Debt = 0;
            LineOfCreditReduction = FreeBorrowsPerTurn = SettlementsDelayed = DebtWaivers = 0;
            BailoutCard = null;
            BailoutCapPerTurn = 0;
            MediaTrainingCost = MediaTrainingDraw = 0;
            _playerTurn = -1;
            _promises.Clear();
            ResetTurn();
        }

        /// <summary>Per-player-turn tallies; call at the start of each player turn.</summary>
        public void ResetTurn()
        {
            DebtGainedThisTurn = BorrowsPlayedThisTurn = BorrowBonusMultiplesThisTurn = 0;
            _lineOfCreditUsedThisTurn = false;
            _bailoutTokensThisTurn = 0;
            _playerTurn++;
            LineOfCreditReduction = _creditDiscount = 0;
            NextSwayMultiplier = 1;
            MediaTrainingAvailable = MediaTrainingCost > 0;
        }

        /// <summary>True while Open Tab still has a free Borrow left this turn.</summary>
        public bool NextBorrowIsFree => BorrowsPlayedThisTurn < FreeBorrowsPerTurn;

        /// <summary>Adds Debt after Line of Credit and the per-turn cap. Returns the Debt actually added.</summary>
        public int GainDebt(int amount, DeckManager deck = null)
        {
            if (LineOfCreditReduction > 0 && !_lineOfCreditUsedThisTurn && amount > 0)
            {
                _lineOfCreditUsedThisTurn = true;
                amount -= LineOfCreditReduction;
                if (deck != null && deck.HandCount > 0 && _creditDiscount > 0)
                {
                    int index = Crookedile.Data.RunState.Current?.Rng.Next(deck.HandCount)
                        ?? UnityEngine.Random.Range(0, deck.HandCount);
                    deck.ReduceCostThisTurn(deck.Hand[index], _creditDiscount);
                }
                LineOfCreditReduction = 0;
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
            int limit = max <= 0 ? Debt + PromisedDebt : max;
            int forgiven = Mathf.Min(limit, Debt);
            Debt -= forgiven;
            int left = limit - forgiven;
            for (int i = _promises.Count - 1; i >= 0 && left > 0; i--)
            {
                var p = _promises[i];
                int n = Mathf.Min(left, p.debt);
                left -= n;
                forgiven += n;
                if (n == p.debt)
                    _promises.RemoveAt(i);
                else
                    _promises[i] = (p.debt - n, p.dueTurn, p.minimumCost);
            }
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

            Collect(owed, ledger, player, addToHand);
        }

        private void Collect(int owed, OpinionLedger ledger, BattleStats player, Func<CardData, int, int> addToHand)
        {
            if (owed <= 0)
                return;

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
