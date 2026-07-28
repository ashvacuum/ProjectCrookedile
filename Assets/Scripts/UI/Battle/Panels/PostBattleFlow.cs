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
    public class PostBattleFlow : MonoBehaviour
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

        private BattleManager _bm;
        private BattleResult _lastResult;

        private System.Action<BattleEndedEvent> _onBattleEnded;

        /// <summary>Supplies the battle context. Called by BattleUI.Initialize.</summary>
        public void Bind(BattleManager bm) => _bm = bm;

        private void OnEnable()
        {
            _onBattleEnded = OnBattleEnded;
            EventBus.Subscribe(_onBattleEnded);
            if (resultPanel != null)
                resultPanel.OnContinueClicked += OnResultContinueClicked;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe(_onBattleEnded);
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
        /// Fired by <see cref="BattleResultPanel.OnContinueClicked"/> after a victory.
        /// Generates a reward offer and opens the reward screen.
        /// On defeat (or if reward infrastructure isn't set up yet) clears the run and reloads.
        /// </summary>
        private void OnResultContinueClicked()
        {
            if (
                _lastResult == null
                || !_lastResult.isVictory
                || _cardDatabase == null
                || _rewardScreen == null
            )
            {
                // Defeat, or victory with reward infra not wired up. Only a genuine defeat
                // ends the run — otherwise a missing reward screen would silently wipe a
                // campaign that the player was actually winning.
                bool defeated = _lastResult == null || !_lastResult.isVictory;
                ReturnToCampaignOrRestart(runEnded: defeated);
                return;
            }

            var offers = _cardDatabase.GenerateRewardOffer(
                _bm != null ? _bm.PlayerOrigin : default,
                count: 3,
                typeFilter: null,
                rng: RunState.Current?.Rng
            );
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
        /// True only when the run is genuinely over (defeat). A campaign run that merely
        /// *finished an encounter* must keep its RunState — clearing it would wipe the deck,
        /// Funds, Credibility, relics, and the day counter, dropping the player onto the map
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

            // Campaign defeat is a provisional game-over: no menu screen exists yet, so the
            // map scene shows a "run ended" state and offers a fresh run.
            if (runEnded)
                RunState.Clear();

            SceneLoader.Instance?.LoadScene("campaign");
        }
    }
}
