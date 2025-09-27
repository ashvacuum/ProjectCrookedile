using Core;
using Relationships;
using UnityEngine;

[System.Serializable]
public class CardEffect
{
    [Header("Effect Type")]
    public EffectType effectType;

    [Header("Resource Changes")]
    public int approvalChange = 0;
    public int wealthChange = 0;
    public int heatChange = 0;
    public int impunityChange = 0;

    [Header("Relationship Effects")]
    public string targetCharacter = "";
    public int rapportChange = 0;

    [Header("Project Effects")]
    public ProjectEffectType projectEffect = ProjectEffectType.None;
    public int projectEffectValue = 0;

    [Header("Conditional Effects")]
    public ConditionType condition = ConditionType.None;
    public int conditionValue = 0;

    [Header("Description")]
    public string effectDescription = "";

    public void Execute()
    {
        if (!CheckCondition()) return;

        ResourceManager rm = GameManager.Instance.resourceManager;

        switch (effectType)
        {
            case EffectType.ResourceChange:
                ExecuteResourceChange(rm);
                break;

            case EffectType.RelationshipChange:
                ExecuteRelationshipChange();
                break;

            case EffectType.ProjectEffect:
                ExecuteProjectEffect();
                break;

            case EffectType.SpecialAction:
                ExecuteSpecialAction();
                break;
        }
    }

    private bool CheckCondition()
    {
        if (condition == ConditionType.None) return true;

        ResourceManager rm = GameManager.Instance.resourceManager;

        switch (condition)
        {
            case ConditionType.MinApproval:
                return rm.approval >= conditionValue;
            case ConditionType.MinWealth:
                return rm.wealth >= conditionValue;
            case ConditionType.MinHeat:
                return rm.heat >= conditionValue;
            case ConditionType.MinImpunity:
                return rm.impunity >= conditionValue;
            case ConditionType.MaxApproval:
                return rm.approval <= conditionValue;
            case ConditionType.MaxHeat:
                return rm.heat <= conditionValue;
            default:
                return true;
        }
    }

    private void ExecuteResourceChange(ResourceManager rm)
    {
        if (approvalChange != 0) rm.AddApproval(approvalChange);
        if (wealthChange != 0) rm.AddWealth(wealthChange);
        if (heatChange != 0) rm.AddHeat(heatChange);
        if (impunityChange != 0) rm.AddImpunity(impunityChange);
    }

    private void ExecuteRelationshipChange()
    {
        if (!string.IsNullOrEmpty(targetCharacter))
        {
            RelationshipManager relManager = GameManager.Instance.relationshipManager;
            relManager?.ChangeRapport(targetCharacter, rapportChange);
        }
    }

    private void ExecuteProjectEffect()
    {
        ProjectManager projManager = GameManager.Instance.projectManager;

        switch (projectEffect)
        {
            case ProjectEffectType.ReduceCost:
                projManager?.ModifyProjectCost(-projectEffectValue);
                break;
            case ProjectEffectType.ReduceTime:
                projManager?.ModifyProjectTime(-projectEffectValue);
                break;
            case ProjectEffectType.IncreaseApproval:
                projManager?.ModifyProjectApproval(projectEffectValue);
                break;
        }
    }

    private void ExecuteSpecialAction()
    {
        Debug.Log($"Special Action: {effectDescription}");
    }
}

public enum EffectType
{
    ResourceChange,
    RelationshipChange,
    ProjectEffect,
    SpecialAction
}

public enum ProjectEffectType
{
    None,
    ReduceCost,
    ReduceTime,
    IncreaseApproval
}

public enum ConditionType
{
    None,
    MinApproval,
    MinWealth,
    MinHeat,
    MinImpunity,
    MaxApproval,
    MaxHeat
}