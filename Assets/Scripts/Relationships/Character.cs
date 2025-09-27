using Core;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Corruption Tycoon/Character")]
public class Character : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;
    public CharacterSector sector;
    [TextArea(3, 5)]
    public string description;
    public Sprite portrait;

    [Header("Relationship Mechanics")]
    public int currentRapport = 0;
    public int maxRapport = 100;
    public bool isBetrayed = false;

    [Header("Bonus Effects (Active when allied)")]
    public string bonusName;
    [TextArea(2, 4)]
    public string bonusDescription;
    public RelationshipBonus allyBonus;

    [Header("Penalty Effects (Active when betrayed)")]
    public string penaltyName;
    [TextArea(2, 4)]
    public string penaltyDescription;
    public RelationshipPenalty betrayalPenalty;

    [Header("Interaction Requirements")]
    public ResourceCost interactionCost;
    public int rapportPerInteraction = 10;

    [Header("Unlock Requirements")]
    public bool requiresUnlock = false;
    public UnlockCondition unlockCondition;

    public bool IsAllied()
    {
        return currentRapport >= 75 && !isBetrayed;
    }

    public bool IsHostile()
    {
        return currentRapport <= 25 || isBetrayed;
    }

    public bool IsNeutral()
    {
        return !IsAllied() && !IsHostile();
    }

    public void ChangeRapport(int amount)
    {
        if (isBetrayed) return;

        currentRapport = Mathf.Clamp(currentRapport + amount, 0, maxRapport);
        Debug.Log($"{characterName} rapport: {currentRapport}/{maxRapport}");
    }

    public void Betray()
    {
        isBetrayed = true;
        Debug.Log($"You betrayed {characterName}! They are now permanently hostile.");
    }

    public void Interact()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;

        if (rm.CanAfford(interactionCost))
        {
            rm.PayCost(interactionCost);
            ChangeRapport(rapportPerInteraction);
        }
    }

    public RelationshipStatus GetStatus()
    {
        if (isBetrayed) return RelationshipStatus.Betrayed;
        if (IsAllied()) return RelationshipStatus.Allied;
        if (IsHostile()) return RelationshipStatus.Hostile;
        return RelationshipStatus.Neutral;
    }
}

public enum CharacterSector
{
    Government,
    Business,
    Media,
    Community,
    Criminal
}

public enum RelationshipStatus
{
    Hostile,
    Neutral,
    Allied,
    Betrayed
}