using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    public sealed class BossPanel : BattlePanel
    {
        [Tooltip(
            "Podium content, hidden in ordinary fights. Keep the subscribing component outside this root."
        )]
        [SerializeField]
        private GameObject _content;

        [Tooltip("Rival's name and current bundle.")]
        [SerializeField]
        private TMP_Text _name;

        [Tooltip("Rival's portrait, with no health or hostility controls.")]
        [SerializeField]
        private Image _portrait;

        [Tooltip("Three ordered intent displays. Unused slots are hidden.")]
        [SerializeField]
        private EnemyIntentDisplay[] _intents;

        [Tooltip("Execution order and locked audience targets for each move.")]
        [SerializeField]
        private TMP_Text[] _orderLabels;

        public RectTransform Anchor =>
            _content != null ? _content.transform as RectTransform : null;

        private void OnEnable()
        {
            On<BattleStartedEvent>(OnBattleStarted);
            On<BossIntentsDeclaredEvent>(OnDeclared);
            On<BossActingEvent>(OnActing);
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            if (_content == null)
                return;

            _content.SetActive(evt.Setup.boss != null);
            if (evt.Setup.boss == null)
                return;

            _name.text = $"BOSS DEBATE — {evt.Setup.boss.DisplayName}";
            _portrait.sprite = evt.Setup.boss.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            foreach (var intent in _intents)
                intent.ShowIntent(null);
        }

        private void OnDeclared(BossIntentsDeclaredEvent evt)
        {
            if (Battle == null || evt.Boss != Battle.Boss)
                return;

            _name.text = $"{evt.Boss.Data.DisplayName} — {evt.Boss.BundleName}";
            for (int i = 0; i < _intents.Length; i++)
            {
                bool visible = i < evt.Boss.Intents.Count;
                _intents[i].gameObject.SetActive(visible);
                _orderLabels[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                _intents[i]
                    .ShowIntent(
                        evt.Boss.Intents[i],
                        evt.Boss.StatusEffects,
                        Battle.PlayerStatusEffects
                    );
                string label = $"{i + 1}.";
                var targets = evt.Boss.GetAudienceTargets(i);
                if (targets != null)
                    foreach (var target in targets.Values)
                        label +=
                            target >= 0 && target < Battle.Enemies.Count
                                ? $" → {Battle.Enemies[target].EnemyData.EnemyName}"
                                : " → no eligible audience";
                _orderLabels[i].text = label;
            }
        }

        private void OnActing(BossActingEvent evt)
        {
            if (Battle == null || evt.Boss != Battle.Boss || evt.IntentIndex >= _intents.Length)
                return;

            _orderLabels[evt.IntentIndex].text += " — acting";
            _intents[evt.IntentIndex].ShowIntent(null);
        }
    }
}
