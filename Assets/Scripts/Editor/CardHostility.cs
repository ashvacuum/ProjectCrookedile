using System.Collections;
using System.Reflection;
using System.Text;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;

namespace Crookedile.Editor
{
    public enum HostilityKind
    {
        None,
        Aggravates,
        Pacifies,
        Mixed,
    }

    /// <summary>
    /// Totals the hostility a card raises (aggravates) and lowers (pacifies) on enemies, for the
    /// Card Database window and the cards CSV. Walks the base effect list and passives through
    /// nested effects (guards, delays, granted passives) by reflection, so a new wrapper effect
    /// needs no change here. Amounts that read from the battle context are flagged
    /// <see cref="Scaled"/> instead of guessed.
    /// </summary>
    public sealed class CardHostility
    {
        private const BindingFlags Fields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public int Raised { get; private set; }
        public int Lowered { get; private set; }
        public bool Scaled { get; private set; }
        private bool _raises,
            _lowers;

        public HostilityKind Kind =>
            _raises && _lowers ? HostilityKind.Mixed
            : _raises ? HostilityKind.Aggravates
            : _lowers ? HostilityKind.Pacifies
            : HostilityKind.None;

        public static CardHostility Of(CardData card)
        {
            var result = new CardHostility();
            result.Walk(card.Effects);
            result.Walk(card.Passives);
            return result;
        }

        /// <summary>"Aggravates +10", "Pacifies -3", "Mixed +5/-3"; a trailing * marks a scaled amount.</summary>
        public string Label()
        {
            if (Kind == HostilityKind.None)
                return "";
            var sb = new StringBuilder(Kind.ToString());
            if (Raised > 0 || Lowered > 0)
            {
                sb.Append(' ');
                if (Raised > 0)
                    sb.Append('+').Append(Raised);
                if (Raised > 0 && Lowered > 0)
                    sb.Append('/');
                if (Lowered > 0)
                    sb.Append('-').Append(Lowered);
            }
            if (Scaled)
                sb.Append('*');
            return sb.ToString();
        }

        private void Walk(object node)
        {
            switch (node)
            {
                case null:
                case string:
                    return;
                case IEnumerable list:
                    foreach (var item in list)
                        Walk(item);
                    return;
                case ReduceHostilityEffect:
                    Add(node, lower: true);
                    break;
                case RaiseTargetHostilityEffect:
                case RaiseAllOpponentsHostilityEffect:
                    Add(node, lower: false);
                    break;
                case ShiftHostilityEffect:
                    Add(node, lower: ReadInt(node, "_amount") < 0);
                    break;
            }

            if (node is not (BattleEffect or BattlePassive))
                return;
            foreach (var field in node.GetType().GetFields(Fields))
                if (
                    typeof(IEnumerable).IsAssignableFrom(field.FieldType)
                    || typeof(BattleEffect).IsAssignableFrom(field.FieldType)
                    || typeof(BattlePassive).IsAssignableFrom(field.FieldType)
                )
                    Walk(field.GetValue(node));
        }

        private void Add(object effect, bool lower)
        {
            int amount = System.Math.Abs(ReadInt(effect, "_amount"));
            var source = effect.GetType().GetField("_amountSource", Fields)?.GetValue(effect);
            if (source != null && source.ToString() != "FixedAmount")
            {
                Scaled = true;
                amount = 0;
            }

            if (lower)
            {
                _lowers = true;
                Lowered += amount;
            }
            else
            {
                _raises = true;
                Raised += amount;
            }
        }

        private static int ReadInt(object effect, string field) =>
            effect.GetType().GetField(field, Fields)?.GetValue(effect) is int value ? value : 0;
    }
}
