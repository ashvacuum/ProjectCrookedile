using Core;
using Relationships;
using UnityEngine;

[System.Serializable]
public class UnlockCondition
{
    [Header("Relationship Requirements")]
    public string requiredCharacter = "";
    public int requiredRapport = 0;

    [Header("Resource Requirements")]
    public int requiredApproval = 0;
    public int requiredWealth = 0;
    public int requiredImpunity = 0;

    [Header("Achievement Requirements")]
    public int projectsCompleted = 0;
    public int crisesNullvived = 0;

    [Header("Special Requirements")]
    public bool requiresSpecialEvent = false;
    public string specialEventName = "";

    public bool IsMet()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;
        RelationshipManager relManager = GameManager.Instance.relationshipManager;
        ProjectManager projManager = GameManager.Instance.projectManager;

        if (!string.IsNullOrEmpty(requiredCharacter))
        {
            if (relManager.GetRapport(requiredCharacter) < requiredRapport)
                return false;
        }

        if (rm.approval < requiredApproval) return false;
        if (rm.wealth < requiredWealth) return false;
        if (rm.impunity < requiredImpunity) return false;

        if (projManager.GetCompletedProjectsCount() < projectsCompleted) return false;

        if (requiresSpecialEvent)
        {
            return false;
        }

        return true;
    }

    public string GetRequirementText()
    {
        string text = "Requires: ";

        if (!string.IsNullOrEmpty(requiredCharacter))
        {
            text += $"{requiredCharacter} rapport {requiredRapport}+, ";
        }

        if (requiredApproval > 0) text += $"Approval {requiredApproval}+, ";
        if (requiredWealth > 0) text += $"Wealth {requiredWealth}+, ";
        if (requiredImpunity > 0) text += $"Impunity {requiredImpunity}+, ";
        if (projectsCompleted > 0) text += $"{projectsCompleted} projects completed, ";

        if (requiresSpecialEvent) text += $"Special event: {specialEventName}, ";

        return text.TrimEnd(' ', ',');
    }
}