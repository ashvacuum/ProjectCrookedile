using Core;
using UnityEngine;

[System.Serializable]
public class RelationshipPenalty
{
    [Header("Resource Penalties (Per Turn)")]
    public int approvalLossPerTurn = 0;
    public int wealthLossPerTurn = 0;
    public int heatGainPerTurn = 0;

    [Header("Cost Penalties")]
    public float budgetCostMultiplier = 1f;
    public float energyCostMultiplier = 1f;

    [Header("Special Penalties")]
    public bool randomLegalCrisis = false;
    public int legalCrisisTurnInterval = 3;
    public bool federalTaskForce = false;
    public bool hostilePress = false;
    public bool contractorBlacklist = false;

    [Header("Project Penalties")]
    public float projectCostMultiplier = 1f;
    public int projectTimeIncrease = 0;
    public int projectApprovalPenalty = 0;

    [Header("Card Penalties")]
    public bool halfCharmEffects = false;
    public bool attackCostIncrease = false;
    public bool halfPREffects = false;

    public void Apply()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;

        if (approvalLossPerTurn > 0) rm.RemoveApproval(approvalLossPerTurn);
        if (wealthLossPerTurn > 0)
        {
            rm.wealth = Mathf.Max(0, rm.wealth - wealthLossPerTurn);
        }
        if (heatGainPerTurn > 0) rm.AddHeat(heatGainPerTurn);

        if (federalTaskForce)
        {
            rm.AddHeat(5);
            Debug.Log("Federal Task Force investigating!");
        }

        if (randomLegalCrisis && GameManager.Instance.currentTurn % legalCrisisTurnInterval == 0)
        {
            TriggerLegalCrisis();
        }
    }

    private void TriggerLegalCrisis()
    {
        GameManager.Instance.resourceManager.AddHeat(15);
        Debug.Log("Legal crisis triggered by betrayal!");
    }

    public string GetPenaltyText()
    {
        string text = "";

        if (approvalLossPerTurn > 0) text += $"-{approvalLossPerTurn} Approval/turn, ";
        if (wealthLossPerTurn > 0) text += $"-{wealthLossPerTurn} Wealth/turn, ";
        if (heatGainPerTurn > 0) text += $"+{heatGainPerTurn} Heat/turn, ";

        if (budgetCostMultiplier > 1f)
        {
            int percentage = Mathf.RoundToInt((budgetCostMultiplier - 1f) * 100f);
            text += $"+{percentage}% Budget costs, ";
        }

        if (projectCostMultiplier > 1f)
        {
            int percentage = Mathf.RoundToInt((projectCostMultiplier - 1f) * 100f);
            text += $"+{percentage}% Project costs, ";
        }

        if (projectTimeIncrease > 0) text += $"+{projectTimeIncrease} weeks project time, ";
        if (projectApprovalPenalty > 0) text += $"-{projectApprovalPenalty} project approval, ";

        if (randomLegalCrisis) text += $"Legal crisis every {legalCrisisTurnInterval} turns, ";
        if (federalTaskForce) text += "Federal Task Force active, ";
        if (hostilePress) text += "Hostile press coverage, ";
        if (contractorBlacklist) text += "Contractor blacklist, ";
        if (halfCharmEffects) text += "Half Charm effects, ";
        if (attackCostIncrease) text += "Attack cards +1 energy, ";

        return text.TrimEnd(' ', ',');
    }
}