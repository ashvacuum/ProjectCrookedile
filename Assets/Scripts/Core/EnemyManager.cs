using System.Collections.Generic;
using Core;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Investigation System")]
    public bool investigationImmunity = false;
    public int investigationCooldown = 0;
    public int federalAttentionLevel = 0;

    [Header("Enemy Actions")]
    public List<EnemyAction> possibleActions = new List<EnemyAction>();
    public List<EnemyAction> scheduledActions = new List<EnemyAction>();

    [Header("Heat-Based Escalation")]
    public int[] heatThresholds = { 20, 40, 60, 80 };
    public EnemyThreatLevel currentThreatLevel = EnemyThreatLevel.LocalComplaints;

    public void ProcessEnemyActions()
    {
        UpdateThreatLevel();
        ProcessScheduledActions();

        if (!investigationImmunity)
        {
            ConsiderNewActions();
        }

        if (investigationCooldown > 0)
        {
            investigationCooldown--;
        }
    }

    private void UpdateThreatLevel()
    {
        int heat = GameManager.Instance.resourceManager.heat;
        EnemyThreatLevel newLevel = EnemyThreatLevel.LocalComplaints;

        if (heat >= heatThresholds[3]) newLevel = EnemyThreatLevel.InternationalIntervention;
        else if (heat >= heatThresholds[2]) newLevel = EnemyThreatLevel.FederalTaskForces;
        else if (heat >= heatThresholds[1]) newLevel = EnemyThreatLevel.PoliceInvestigations;
        else if (heat >= heatThresholds[0]) newLevel = EnemyThreatLevel.InvestigativeJournalism;

        if (newLevel != currentThreatLevel)
        {
            currentThreatLevel = newLevel;
            OnThreatLevelChange();
        }
    }

    private void OnThreatLevelChange()
    {
        Debug.Log($"Threat level escalated to: {currentThreatLevel}");

        switch (currentThreatLevel)
        {
            case EnemyThreatLevel.InvestigativeJournalism:
                ScheduleAction(CreateJournalismAction());
                break;
            case EnemyThreatLevel.PoliceInvestigations:
                ScheduleAction(CreatePoliceAction());
                break;
            case EnemyThreatLevel.FederalTaskForces:
                ScheduleAction(CreateFederalAction());
                break;
            case EnemyThreatLevel.InternationalIntervention:
                ScheduleAction(CreateInternationalAction());
                break;
        }
    }

    private void ProcessScheduledActions()
    {
        for (int i = scheduledActions.Count - 1; i >= 0; i--)
        {
            EnemyAction action = scheduledActions[i];
            action.turnsUntilExecution--;

            if (action.turnsUntilExecution <= 0)
            {
                ExecuteAction(action);
                scheduledActions.RemoveAt(i);
            }
        }
    }

    private void ConsiderNewActions()
    {
        if (investigationCooldown > 0) return;

        float actionChance = GetActionChance();
        if (Random.value < actionChance)
        {
            EnemyAction randomAction = GetRandomActionForThreatLevel();
            if (randomAction != null)
            {
                ScheduleAction(randomAction);
            }
        }
    }

    private float GetActionChance()
    {
        int heat = GameManager.Instance.resourceManager.heat;
        return Mathf.Clamp01(heat / 100f);
    }

    private void ScheduleAction(EnemyAction action)
    {
        if (action != null)
        {
            scheduledActions.Add(action);
            Debug.Log($"Scheduled enemy action: {action.actionName} (executes in {action.turnsUntilExecution} turns)");
        }
    }

    private void ExecuteAction(EnemyAction action)
    {
        ResourceManager rm = GameManager.Instance.resourceManager;

        rm.RemoveApproval(action.approvalDamage);
        rm.AddHeat(action.heatIncrease);

        if (action.specialEffect != null)
        {
            action.specialEffect.Invoke();
        }

        investigationCooldown = action.cooldownTurns;

        Debug.Log($"Executed enemy action: {action.actionName} (-{action.approvalDamage} approval, +{action.heatIncrease} heat)");
    }

    public void SetInvestigationImmunity(bool immunity)
    {
        investigationImmunity = immunity;
    }

    private EnemyAction CreateJournalismAction()
    {
        return new EnemyAction
        {
            actionName = "Investigative Article",
            approvalDamage = 10,
            heatIncrease = 5,
            turnsUntilExecution = 2,
            cooldownTurns = 3
        };
    }

    private EnemyAction CreatePoliceAction()
    {
        return new EnemyAction
        {
            actionName = "Police Investigation",
            approvalDamage = 15,
            heatIncrease = 10,
            turnsUntilExecution = 3,
            cooldownTurns = 5
        };
    }

    private EnemyAction CreateFederalAction()
    {
        return new EnemyAction
        {
            actionName = "Federal Task Force",
            approvalDamage = 25,
            heatIncrease = 20,
            turnsUntilExecution = 4,
            cooldownTurns = 8
        };
    }

    private EnemyAction CreateInternationalAction()
    {
        return new EnemyAction
        {
            actionName = "International Sanctions",
            approvalDamage = 40,
            heatIncrease = 30,
            turnsUntilExecution = 5,
            cooldownTurns = 10
        };
    }

    private EnemyAction GetRandomActionForThreatLevel()
    {
        switch (currentThreatLevel)
        {
            case EnemyThreatLevel.LocalComplaints:
                return null;
            case EnemyThreatLevel.InvestigativeJournalism:
                return CreateJournalismAction();
            case EnemyThreatLevel.PoliceInvestigations:
                return Random.value < 0.5f ? CreateJournalismAction() : CreatePoliceAction();
            case EnemyThreatLevel.FederalTaskForces:
                return CreateFederalAction();
            case EnemyThreatLevel.InternationalIntervention:
                return CreateInternationalAction();
            default:
                return null;
        }
    }
}

[System.Serializable]
public class EnemyAction
{
    public string actionName;
    public int approvalDamage;
    public int heatIncrease;
    public int turnsUntilExecution;
    public int cooldownTurns;
    public System.Action specialEffect;
}

public enum EnemyThreatLevel
{
    LocalComplaints,
    InvestigativeJournalism,
    PoliceInvestigations,
    FederalTaskForces,
    InternationalIntervention
}