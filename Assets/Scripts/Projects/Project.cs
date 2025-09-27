using Core;
using UnityEngine;

namespace Projects
{
    [CreateAssetMenu(fileName = "New Project", menuName = "Corruption Tycoon/Project")]
    public class Project : ScriptableObject
    {
        [Header("Basic Info")]
        public string projectName;
        [TextArea(3, 5)]
        public string description;
        public ProjectType projectType;
        public Sprite projectIcon;

        [Header("Timeline")]
        public int baseDuration = 2;
        public int currentProgress = 0;
        public bool isCompleted = false;
        public bool isAbandoned = false;

        [Header("Costs & Rewards")]
        public int baseBudgetCost = 300;
        public int baseApprovalGain = 20;
        public int baseHeatGenerated = 5;

        [Header("Skimming Options")]
        public bool canBeSkimmed = true;
        public SkimmingLevel currentSkimmingLevel = SkimmingLevel.Clean;

        [Header("Seasonal Effects")]
        public bool affectedByWeather = true;
        public Season idealSeason = Season.Summer;

        [Header("Requirements")]
        public UnlockCondition requirements;

        public int GetActualCost()
        {
            float multiplier = GetCostMultiplier();
            return Mathf.RoundToInt(baseBudgetCost * multiplier);
        }

        public int GetActualDuration()
        {
            int duration = baseDuration;

            if (affectedByWeather && GameManager.Instance.currentSeason == Season.Rainy)
            {
                if (projectType == ProjectType.Infrastructure || projectType == ProjectType.Construction)
                {
                    duration += 1;
                }
            }

            duration += GetDurationModifier();
            return Mathf.Max(1, duration);
        }

        public int GetActualApprovalGain()
        {
            float approval = baseApprovalGain;

            switch (currentSkimmingLevel)
            {
                case SkimmingLevel.Light:
                    approval *= 0.9f;
                    break;
                case SkimmingLevel.Moderate:
                    approval *= 0.7f;
                    break;
                case SkimmingLevel.Heavy:
                    approval *= 0.5f;
                    break;
            }

            approval += GetApprovalModifier();
            return Mathf.RoundToInt(approval);
        }

        public int GetSkimmedWealth()
        {
            switch (currentSkimmingLevel)
            {
                case SkimmingLevel.Clean:
                    return 0;
                case SkimmingLevel.Light:
                    return Mathf.RoundToInt(baseBudgetCost * 0.1f);
                case SkimmingLevel.Moderate:
                    return Mathf.RoundToInt(baseBudgetCost * 0.25f);
                case SkimmingLevel.Heavy:
                    return Mathf.RoundToInt(baseBudgetCost * 0.5f);
                default:
                    return 0;
            }
        }

        public int GetSkimmingHeat()
        {
            switch (currentSkimmingLevel)
            {
                case SkimmingLevel.Clean:
                    return 0;
                case SkimmingLevel.Light:
                    return 2;
                case SkimmingLevel.Moderate:
                    return 8;
                case SkimmingLevel.Heavy:
                    return 20;
                default:
                    return 0;
            }
        }

        public void AdvanceProgress()
        {
            if (isCompleted || isAbandoned) return;

            currentProgress++;

            if (currentProgress >= GetActualDuration())
            {
                CompleteProject();
            }
        }

        public void CompleteProject()
        {
            if (isCompleted) return;

            isCompleted = true;
            ResourceManager rm = GameManager.Instance.resourceManager;

            rm.AddApproval(GetActualApprovalGain());
            rm.AddWealth(GetSkimmedWealth());
            rm.AddHeat(baseHeatGenerated + GetSkimmingHeat());

            Debug.Log($"Completed {projectName}: +{GetActualApprovalGain()} approval, +{GetSkimmedWealth()} wealth, +{baseHeatGenerated + GetSkimmingHeat()} heat");
        }

        public void AbandonProject()
        {
            isAbandoned = true;
            ResourceManager rm = GameManager.Instance.resourceManager;

            int penalty = Mathf.RoundToInt(baseApprovalGain * 0.5f);
            rm.RemoveApproval(penalty);

            Debug.Log($"Abandoned {projectName}: -{penalty} approval penalty");
        }

        public void SetSkimmingLevel(SkimmingLevel level)
        {
            if (!canBeSkimmed && level != SkimmingLevel.Clean)
            {
                Debug.Log("This project cannot be skimmed");
                return;
            }

            currentSkimmingLevel = level;
        }

        public float GetProgressPercentage()
        {
            return (float)currentProgress / GetActualDuration();
        }

        public int GetRemainingWeeks()
        {
            return GetActualDuration() - currentProgress;
        }

        public bool CanStart()
        {
            ResourceManager rm = GameManager.Instance.resourceManager;
            return rm.budget >= GetActualCost() && requirements.IsMet();
        }

        private float GetCostMultiplier()
        {
            return GameManager.Instance.relationshipManager?.GetProjectCostMultiplier() ?? 1f;
        }

        private int GetDurationModifier()
        {
            return GameManager.Instance.relationshipManager?.GetProjectTimeModifier() ?? 0;
        }

        private int GetApprovalModifier()
        {
            return GameManager.Instance.relationshipManager?.GetProjectApprovalModifier() ?? 0;
        }
    }

    public enum ProjectType
    {
        Infrastructure,
        Construction,
        Social,
        Emergency,
        Maintenance
    }

    public enum SkimmingLevel
    {
        Clean,
        Light,
        Moderate,
        Heavy
    }
}