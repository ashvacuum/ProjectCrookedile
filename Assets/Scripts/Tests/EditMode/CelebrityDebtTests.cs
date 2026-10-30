using Crookedile.Gameplay;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;

namespace Crookedile.Tests
{
    /// <summary>The Celebrity's Debt rules: settlement, Line of Credit, delayed settlement and the waiver.</summary>
    public class CelebrityDebtTests
    {
        private OpinionLedger _ledger;
        private CelebrityState _state;
        private BattleStats _player;

        [SetUp]
        public void SetUp()
        {
            _ledger = new OpinionLedger(100, 50, () => { });
            _state = new CelebrityState();
            _player = new BattleStats(3);
        }

        private void Settle() => _state.Settle(_ledger, _player, (_, __) => 0);

        [Test]
        public void Settle_PaysFromEnergyFirst_ThenTheMeter()
        {
            _state.GainDebt(5);

            Settle();

            Assert.AreEqual(0, _player.CurrentActionPoints, "3 energy absorbs 3 of the Debt");
            Assert.AreEqual(48, _ledger.CurrentOpinion, "the unpaid 2 hits the meter");
            Assert.AreEqual(0, _state.Debt);
        }

        [Test]
        public void LineOfCredit_StripsOneFromTheFirstDebtGainEachTurn()
        {
            _state.LineOfCreditReduction = 1;

            _state.GainDebt(2);
            _state.GainDebt(2);

            Assert.AreEqual(3, _state.Debt);
        }

        [Test]
        public void RainCheck_DelaysOneSettlement()
        {
            _state.GainDebt(3);
            _state.SettlementsDelayed = 1;

            Settle();

            Assert.AreEqual(3, _state.Debt);
            Assert.AreEqual(3, _player.CurrentActionPoints);
        }

        [Test]
        public void Waiver_AbsorbsTheUnpaidDamage()
        {
            _state.DebtWaivers = 1;
            _state.GainDebt(7);

            Settle();

            Assert.AreEqual(0, _player.CurrentActionPoints, "energy still pays first");
            Assert.AreEqual(
                50,
                _ledger.CurrentOpinion,
                "the waiver eats the 4 that would hit the meter"
            );
            Assert.AreEqual(0, _state.DebtWaivers);
        }

        [Test]
        public void Collateral_SupportPaysAfterEnergy_BeforeTheMeter()
        {
            _state.SupportPaysDebt = true;
            _ledger.GainSupport(2);
            _state.GainDebt(6);

            _state.HoldSupportForDebt(_ledger);
            Settle();

            Assert.AreEqual(0, _player.CurrentActionPoints, "energy pays 3 first");
            Assert.AreEqual(0, _ledger.CurrentSupport, "Support pays the next 2");
            Assert.AreEqual(49, _ledger.CurrentOpinion, "only the last 1 hits the meter");
        }

        [Test]
        public void Collateral_LeavesEnergyFirst()
        {
            _state.SupportPaysDebt = true;
            _ledger.GainSupport(4);
            _state.GainDebt(2);

            _state.HoldSupportForDebt(_ledger);
            Settle();

            Assert.AreEqual(1, _player.CurrentActionPoints, "energy covers the whole bill");
            Assert.AreEqual(50, _ledger.CurrentOpinion);
        }

        [Test]
        public void Collateral_MissedPromiseSpendsCurrentSupportAfterEnergy()
        {
            _state.SupportPaysDebt = true;
            _state.GainDebt(5);
            _state.MakePromise(5, 2);
            _state.ResetTurn(); // the promise's turn
            _ledger.GainSupport(4);

            _state.SettlePromises(_ledger, _player, (_, __) => 0);

            Assert.AreEqual(0, _player.CurrentActionPoints, "energy pays 3 first");
            Assert.AreEqual(2, _ledger.CurrentSupport, "Support pays the other 2");
            Assert.AreEqual(50, _ledger.CurrentOpinion);
        }

        [Test]
        public void Collateral_SkipsADelayedSettlement()
        {
            _state.SupportPaysDebt = true;
            _state.SettlementsDelayed = 1;
            _ledger.GainSupport(2);
            _state.GainDebt(3);

            _state.HoldSupportForDebt(_ledger);

            Assert.AreEqual(2, _ledger.CurrentSupport);
            Assert.AreEqual(3, _state.Debt);
        }
    }
}
