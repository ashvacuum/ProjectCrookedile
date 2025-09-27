using Core;
using UnityEngine;

[System.Serializable]
public class RelationshipBonus
{
    [Header("Resource Bonuses (Per Turn)")]
    public int approvalPerTurn = 0;
    public int wealthPerTurn = 0;
    public int heatReductionPerTurn = 0;

    [Header("Cost Modifiers")]
    public float budgetCostMultiplier = 1f;
    public float energyCostMultiplier = 1f;
    public int energyBonus = 0;

    [Header("Special Abilities")]
    public bool immuneToInvestigations = false;
    public bool canPreviewEnemyActions = false;
    public bool doubleProjectApproval = false;
    public bool convertAttackToDefense = false;
    public bool hideWealthFromInvestigations = false;

    [Header("Project Bonuses")]
    public float projectCostMultiplier = 1f;
    public int projectTimeReduction = 0;
    public int projectApprovalBonus = 0;

    [Header("Card Effects")]
    public bool doubleCharmEffects = false;
    public bool convertCharmToPower = false;
    public bool halfPREffects = false;

    public void Apply()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;

        if (approvalPerTurn != 0) rm.AddApproval(approvalPerTurn);
        if (wealthPerTurn != 0) rm.AddWealth(wealthPerTurn);
        if (heatReductionPerTurn != 0) rm.RemoveHeat(heatReductionPerTurn);

        if (energyBonus != 0)
        {
            rm.energy = Mathf.Min(rm.energy + energyBonus, rm.maxEnergy);
        }

        if (immuneToInvestigations)
        {
            GameManager.Instance.enemyManager?.SetInvestigationImmunity(true);
        }
    }

    public string GetBonusText()
    {
        string text = "";

        if (approvalPerTurn > 0) text += $"+{approvalPerTurn} Approval/turn, ";
        if (wealthPerTurn > 0) text += $"+{wealthPerTurn} Wealth/turn, ";
        if (heatReductionPerTurn > 0) text += $"-{heatReductionPerTurn} Heat/turn, ";
        if (energyBonus > 0) text += $"+{energyBonus} Energy/turn, ";

        if (budgetCostMultiplier != 1f)
        {
            int percentage = Mathf.RoundToInt((1f - budgetCostMultiplier) * 100f);
            text += $"{percentage}% Budget cost reduction, ";
        }

        if (projectCostMultiplier != 1f)
        {
            int percentage = Mathf.RoundToInt((1f - projectCostMultiplier) * 100f);
            text += $"{percentage}% Project cost reduction, ";
        }

        if (projectTimeReduction > 0) text += $"-{projectTimeReduction} weeks project time, ";
        if (projectApprovalBonus > 0) text += $"+{projectApprovalBonus} project approval, ";

        if (immuneToInvestigations) text += "Immune to investigations, ";
        if (canPreviewEnemyActions) text += "Preview enemy actions, ";
        if (doubleProjectApproval) text += "Double project approval, ";
        if (convertAttackToDefense) text += "Use Attack cards as Defense, ";
        if (hideWealthFromInvestigations) text += "Hide wealth from investigations, ";
        if (doubleCharmEffects) text += "Double Charm card effects, ";
        if (convertCharmToPower) text += "Charm cards become Power cards, ";

        return text.TrimEnd(' ', ',');
    }
}