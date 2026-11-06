using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns what happens AFTER a battle: recording the victory in RunState, generating
    /// the reward offer on Continue, applying the picked card, advancing the session,
    /// and reloading the scene. Extracted from BattleUI — this is run progression, not
    /// battle UI, and it's the piece that grows when the real metagame lands.
    /// </summary>
    public class PostBattleFlow : BattlePanel
    {
        [Header("Post-battle")]
        [Tooltip("Result panel whose Continue click starts the post-battle flow.")]
        [SerializeField]
        private BattleResultPanel resultPanel;

        [Tooltip("CardDatabase ScriptableObject used to generate post-battle card offers.")]
        [SerializeField]
        private CardDatabase _cardDatabase;

        [Tooltip(
            "Reward screen overlay panel (starts inactive). Shown after a victory Continue click."
        )]
        [SerializeField]
        private CardPickerPanel _rewardScreen;

        private BattleResult _lastResult;

        private void OnEnable()
        {
            On<BattleEndedEvent>(OnBattleEnded);
            if (resultPanel != null)
                resultPanel.OnContinueClicked += OnResultContinueClicked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (resultPanel != null)
                resultPanel.OnContinueClicked -= OnResultContinueClicked;
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            _lastResult = evt.Result;

            // Victory: update RunState so the next battle knows this one was won.
            if (evt.Result.isVictory)
                RunState.Current?.RecordBattleVictory();
        }

        /// <summary>
        /// Fired by <see cref="BattleResultPanel.OnContinueClicked"/>. A victory opens the reward
        /// screen. A defeat costs Credibility: at 0, or in a boss debate, the run ends; otherwise a campaign run gets the
        /// smaller consolation offer (<see cref="CardDatabase.DefeatRewards"/>) and goes back to the map.
        /// </summary>
        private void OnResultContinueClicked()
        {
            var run = RunState.Current;
            if (_lastResult == null || !_lastResult.isVictory)
            {
                bool survived =
                    run?.RecordBattleDefeat(
                        _lastResult?.finalOpinion ?? 0,
                        _lastResult?.winLine ?? 0
                    ) == true;
                // Losing a boss debate (the election) ends the run whatever Credibility is left.
                if (Battle?.Boss != null)
                    survived = false;
                if (
                    survived
                    && run.IsCampaignRun
                    && _cardDatabase?.DefeatRewards != null
                    && _rewardScreen != null
                )
                {
                    // Not reported to NoteRewardOffer: a consolation offer shouldn't feed the rare pity timer.
                    var consolation = _cardDatabase.GenerateRewardOffer(
                        Battle != null ? Battle.PlayerOrigin : default,
                        rng: run.Rng,
                        config: _cardDatabase.DefeatRewards
                    );
                    _rewardScreen.OpenSingle(
                        "Salvage Something",
                        consolation,
                        "Take Card",
                        picked =>
                        {
                            if (picked != null)
                                run.AddCardToDeck(picked);
                            ReturnToCampaignOrRestart(runEnded: false);
                        }
                    );
                    return;
                }
                ReturnToCampaignOrRestart(runEnded: !survived);
                return;
            }

            // Winning a boss debate sends you into the next week at full Credibility.
            if (Battle?.Boss != null)
                run?.RestoreCredibility();

            // Victory with reward infra not wired up: never end the run over a missing screen,
            // or it would silently wipe a campaign the player was actually winning.
            if (_cardDatabase == null || _rewardScreen == null)
            {
                ReturnToCampaignOrRestart(runEnded: false);
                return;
            }

            // A dominant win draws from the better pool; a maxed meter guarantees a Rare (4 Rares in a boss fight).
            int maxOpinion = Battle?.Opinion?.MaxOpinion ?? 100;
            var offers = _cardDatabase.GenerateRewardOffer(
                Battle != null ? Battle.PlayerOrigin : default,
                rng: run?.Rng,
                cardsSinceRare: run?.CardsSinceRare ?? 0,
                config: _cardDatabase.RewardsForWin(
                    _lastResult.finalOpinion,
                    maxOpinion,
                    bossFight: Battle?.Boss != null
                ),
                guaranteeRare: _lastResult.finalOpinion >= maxOpinion
            );
            run?.NoteRewardOffer(offers);
            _rewardScreen.OpenSingle("Choose a Card", offers, "Take Card", OnRewardChosen);
        }

        /// <summary>
        /// Callback from <see cref="CardPickerPanel"/> once the player picks a card (or skips).
        /// Adds the card to <see cref="RunState.Current"/>, advances the session battle index,
        /// and reloads the scene. Clears RunState when the session is fully complete.
        /// </summary>
        private void OnRewardChosen(CardData picked)
        {
            if (picked != null)
                RunState.Current?.AddCardToDeck(picked);

            if (RunState.Current?.HasNextBattle == true)
            {
                // More rounds remain in THIS encounter — advance the index and reload into
                // the next fight. Unchanged whether this is a campaign or test run.
                RunState.Current.AdvanceToNextBattle();
                SceneLoader.Instance?.ReloadCurrentScene();
                return;
            }

            // Encounter fully complete — the player won, so the run continues.
            ReturnToCampaignOrRestart(runEnded: false);
        }

        /// <summary>
        /// Sends the player back where they came from: the campaign map for a campaign run,
        /// or a fresh reload of the current (test) scene otherwise.
        /// </summary>
        /// <param name="runEnded">
        /// True only when the run is genuinely over (a defeat that took Credibility to 0). A campaign
        /// run that merely *finished an encounter*, won or lost, must keep its RunState — clearing it would wipe the deck,
        /// Funds, Credibility, allies, and the day counter, dropping the player onto the map
        /// with a brand new run. Non-campaign (test) runs clear either way, unchanged.
        /// </param>
        private void ReturnToCampaignOrRestart(bool runEnded)
        {
            if (RunState.Current?.IsCampaignRun != true)
            {
                // Test-harness path, byte-for-byte as before.
                RunState.Clear();
                SceneLoader.Instance?.ReloadCurrentScene();
                return;
            }

            // Out of Credibility ends the run: the profile records it, the run save is deleted,
            // and the map scene shows its "run ended" state with a fresh run on offer.
            if (runEnded)
                Crookedile.Data.Save.SaveSystem.EndRun(victory: false);

            SceneLoader.Instance?.LoadScene("campaign");
        }
    }
}
