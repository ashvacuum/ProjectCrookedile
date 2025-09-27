using Core;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Corruption Tycoon/Card")]
public class Card : ScriptableObject
{
    [Header("Basic Info")]
    public string cardName;
    [TextArea(3, 5)]
    public string description;
    public CardType cardType;
    public Sprite cardArt;

    [Header("Costs")]
    public int energyCost = 1;
    public ResourceCost additionalCosts;

    [Header("Effects")]
    public CardEffect[] effects;

    [Header("Unlock Requirements")]
    public bool isUnlockable = false;
    public UnlockCondition unlockCondition;

    public bool CanPlay()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;

        if (rm.energy < energyCost) return false;
        if (!rm.CanAfford(additionalCosts)) return false;

        return true;
    }

    public void Play()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;

        rm.SpendEnergy(energyCost);
        rm.PayCost(additionalCosts);

        foreach (CardEffect effect in effects)
        {
            effect.Execute();
        }

        Debug.Log($"Played {cardName} ({cardType})");
    }

    public Color GetCardColor()
    {
        switch (cardType)
        {
            case CardType.Charm: return Color.magenta;
            case CardType.Defense: return Color.blue;
            case CardType.Attack: return Color.red;
            case CardType.Leverage: return Color.yellow;
            case CardType.Power: return new Color(0.5f, 0f, 0.5f);
            default: return Color.gray;
        }
    }
}

public enum CardType
{
    Charm,    // Pink - Win hearts and minds
    Defense,  // Blue - Protect yourself
    Attack,   // Red - Apply pressure
    Leverage, // Yellow - Seize opportunities (unlockable)
    Power     // Purple - Game changers (unlockable)
}