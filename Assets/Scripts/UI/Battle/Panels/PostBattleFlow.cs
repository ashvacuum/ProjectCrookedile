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
                // Defeat — wipe RunState so the next scene load starts a fresh run.
                RunState.Clear();
                SceneLoader.Instance?.ReloadCurrentScene();
                return;
            }

            var offers = _cardDatabase.GenerateRewardOffer(
                _bm != null ? _bm.PlayerOrigin : default,
                count: 3
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
                // More battles remain — advance the index and reload into the next fight.
                RunState.Current.AdvanceToNextBattle();
                SceneLoader.Instance?.ReloadCurrentScene();
            }
            else
            {
                // Session complete (or no session). Clear RunState and restart for playtesting.
                GameLogger.LogInfo(
                    "PostBattleFlow",
                    "Run complete! Clearing RunState — next scene load starts fresh."
                );
                RunState.Clear();
                SceneLoader.Instance?.ReloadCurrentScene();
            }
        }
    }
}
