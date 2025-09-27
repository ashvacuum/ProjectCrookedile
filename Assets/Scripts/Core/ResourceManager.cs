using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Primary Resources")]
    public int budget = 200;
    public int approval = 65;
    public int wealth = 100;
    public int heat = 0;
    public int energy = 3;
    public int impunity = 0;

    [Header("Resource Limits")]
    public int maxBudget = 999;
    public int maxApproval = 100;
    public int maxWealth = 9999;
    public int maxHeat = 100;
    public int maxEnergy = 5;
    public int maxImpunity = 100;

    [Header("Per Turn Income")]
    public int budgetPerTurn = 200;
    public int passiveWealthIncome = 0;

    public void ProcessTurnStart()
    {
        budget = Mathf.Min(budget + budgetPerTurn, maxBudget);
        wealth += passiveWealthIncome;
        energy = Mathf.Min(energy + 3, maxEnergy);

        ClampAllResources();
    }

    public void ProcessTurnEnd()
    {
        heat = Mathf.Max(0, heat - 2);
        ClampAllResources();
    }

    public bool SpendBudget(int amount)
    {
        if (budget >= amount)
        {
            budget -= amount;
            return true;
        }
        return false;
    }

    public bool SpendWealth(int amount)
    {
        if (wealth >= amount)
        {
            wealth -= amount;
            return true;
        }
        return false;
    }

    public void SpendEnergy(int amount)
    {
        energy = Mathf.Max(0, energy - amount);
    }

    public void AddApproval(int amount)
    {
        int oldValue = approval;
        approval = Mathf.Clamp(approval + amount, 0, maxApproval);

        if (approval != oldValue)
        {
            EventBus.Publish(new ResourceChangedEvent("Approval", oldValue, approval));
        }
    }

    public void AddWealth(int amount)
    {
        int oldValue = wealth;
        wealth = Mathf.Min(wealth + amount, maxWealth);

        if (wealth != oldValue)
        {
            EventBus.Publish(new ResourceChangedEvent("Wealth", oldValue, wealth));
        }
    }

    public void AddHeat(int amount)
    {
        int oldValue = heat;
        heat = Mathf.Min(heat + amount, maxHeat);

        if (heat != oldValue)
        {
            EventBus.Publish(new ResourceChangedEvent("Heat", oldValue, heat));
        }
    }

    public void AddImpunity(int amount)
    {
        int oldValue = impunity;
        impunity = Mathf.Min(impunity + amount, maxImpunity);

        if (impunity != oldValue)
        {
            EventBus.Publish(new ResourceChangedEvent("Impunity", oldValue, impunity));
        }
    }

    public void RemoveApproval(int amount)
    {
        int oldValue = approval;
        approval = Mathf.Max(0, approval - amount);

        if (approval != oldValue)
        {
            EventBus.Publish(new ResourceChangedEvent("Approval", oldValue, approval));
        }
    }

    public void RemoveHeat(int amount)
    {
        int oldValue = heat;
        heat = Mathf.Max(0, heat - amount);

        if (heat != oldValue)
        {
            EventBus.Publish(new ResourceChangedEvent("Heat", oldValue, heat));
        }
    }

    public void SetPassiveWealthIncome(int amount)
    {
        passiveWealthIncome = amount;
    }

    public bool CanAfford(ResourceCost cost)
    {
        return budget >= cost.budget &&
               wealth >= cost.wealth &&
               energy >= cost.energy;
    }

    public void PayCost(ResourceCost cost)
    {
        budget -= cost.budget;
        wealth -= cost.wealth;
        energy -= cost.energy;
        ClampAllResources();
    }

    public ImpunityTier GetImpunityTier()
    {
        if (impunity >= 95) return ImpunityTier.ShadowEmperor;
        if (impunity >= 80) return ImpunityTier.CriminalPartner;
        if (impunity >= 60) return ImpunityTier.CorruptAuthority;
        if (impunity >= 40) return ImpunityTier.ConnectedOfficial;
        return ImpunityTier.CleanPolitician;
    }

    private void ClampAllResources()
    {
        budget = Mathf.Clamp(budget, 0, maxBudget);
        approval = Mathf.Clamp(approval, 0, maxApproval);
        wealth = Mathf.Clamp(wealth, 0, maxWealth);
        heat = Mathf.Clamp(heat, 0, maxHeat);
        energy = Mathf.Clamp(energy, 0, maxEnergy);
        impunity = Mathf.Clamp(impunity, 0, maxImpunity);
    }
}

[System.Serializable]
public class ResourceCost
{
    public int budget = 0;
    public int wealth = 0;
    public int energy = 0;

    public ResourceCost(int b = 0, int w = 0, int e = 0)
    {
        budget = b;
        wealth = w;
        energy = e;
    }
}

public enum ImpunityTier
{
    CleanPolitician,
    ConnectedOfficial,
    CorruptAuthority,
    CriminalPartner,
    ShadowEmperor
}