using System;
using Sirenix.OdinInspector;

namespace Crookedile.Data.Campaign
{
    [Serializable]
    [InfoBox(
        "@$value == null ? \"(no passive chosen)\" : $value.EditorSafeDescription()",
        InfoMessageType.None
    )]
    public abstract class OverworldPassive
    {
        public struct VisitModifiers
        {
            public int TravelTimeReductionPercent;
            public int EncounterTimeReductionPercent;
        }

        /// <summary>Contributes to a visit preview without mutating the ally, encounter, or run.</summary>
        public abstract void ModifyVisit(EncounterData encounter, ref VisitModifiers modifiers);

        public abstract string GetDescription();

        public string EditorSafeDescription()
        {
            try
            {
                return GetDescription();
            }
            catch (Exception exception)
            {
                return $"(description error: {exception.GetType().Name})";
            }
        }
    }
}
