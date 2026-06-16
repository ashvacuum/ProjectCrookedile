namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Comparison operator used by numeric passive conditions.
    /// </summary>
    public enum ComparisonType
    {
        AtLeast, // value >= threshold
        AtMost, // value <= threshold
        Equals, // value == threshold
    }
}
