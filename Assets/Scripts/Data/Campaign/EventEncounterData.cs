using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// One choice the player can pick in an event: a button label, the outcomes it applies,
    /// and the text shown afterwards so the choice reads as a consequence rather than a
    /// silent stat change.
    /// </summary>
    [Serializable]
    public class EventOption
    {
        [Tooltip("Button text, e.g. \"Take the envelope\".")]
        [SerializeField]
        private string _label;

        [Tooltip("Shown after this option is chosen, before returning to the map. Optional.")]
        [TextArea(2, 4)]
        [SerializeField]
        private string _resultText;

        [Tooltip("Everything this choice does to the run. Applied in order, all of them.")]
        [SerializeReference]
        [SerializeField]
        private List<RunOutcome> _outcomes = new List<RunOutcome>();

        public string Label => _label;
        public string ResultText => _resultText;
        public IReadOnlyList<RunOutcome> Outcomes => _outcomes;

        /// <summary>
        /// Applies every outcome to <paramref name="state"/>. Null entries are skipped —
        /// a freshly-added inspector row is null until a type is picked, and a half-authored
        /// event must not throw at runtime.
        /// </summary>
        public void Apply(RunState state)
        {
            if (state == null)
                return;
            foreach (var outcome in _outcomes)
                outcome?.Apply(state);
        }

        /// <summary>Joined outcome descriptions, for option subtext or a tooltip.</summary>
        public string DescribeOutcomes()
        {
            var parts = new List<string>();
            foreach (var outcome in _outcomes)
                if (outcome != null)
                    parts.Add(outcome.GetDescription());
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// A campaign-map location that opens a dialogue panel instead of loading a battle:
    /// body text plus a list of <see cref="EventOption"/>s, each mutating the run.
    ///
    /// Deliberately has no battle handoff and no requirement gating yet — see
    /// docs/campaign-build-checklist.md M2 for where those slot in. Being a plain
    /// <see cref="EncounterData"/> means it drops into an encounter pool unchanged
    /// whenever randomised/per-day encounter selection gets built.
    ///
    /// Create via: Assets → Create → Crookedile → Campaign → Event Encounter
    /// </summary>
    [CreateAssetMenu(
        menuName = "Crookedile/Campaign/Event Encounter",
        fileName = "New Event Encounter"
    )]
    public class EventEncounterData : EncounterData
    {
        [Tooltip(
            "The dialogue shown in the event panel. This is the scene text — the inherited "
                + "Blurb is the short line shown on the map before the player commits."
        )]
        [TextArea(4, 10)]
        [SerializeField]
        private string _body;

        [Tooltip("Choices offered. At least one, or the player can't leave the panel.")]
        [SerializeField]
        private List<EventOption> _options = new List<EventOption>();

        public string Body => _body;
        public IReadOnlyList<EventOption> Options => _options;
    }
}
