using System.Collections.Generic;
using System.Linq;
using Core;
using Projects;
using UnityEngine;

public class ProjectManager : MonoBehaviour
{
    [Header("Project Database")]
    public List<Project> availableProjects = new List<Project>();
    public List<Project> activeProjects = new List<Project>();
    public List<Project> completedProjects = new List<Project>();

    [Header("Project Limits")]
    public int maxActiveProjects = 3;

    [Header("Project Modifiers")]
    public float globalCostMultiplier = 1f;
    public int globalTimeModifier = 0;
    public int globalApprovalModifier = 0;

    public void UpdateProjects()
    {
        for (int i = activeProjects.Count - 1; i >= 0; i--)
        {
            Project project = activeProjects[i];
            project.AdvanceProgress();

            if (project.isCompleted)
            {
                activeProjects.RemoveAt(i);
                completedProjects.Add(project);
            }
            else if (project.isAbandoned)
            {
                activeProjects.RemoveAt(i);
            }
        }
    }

    public bool StartProject(Project project)
    {
        if (activeProjects.Count >= maxActiveProjects)
        {
            Debug.Log("Cannot start project: Maximum active projects reached");
            return false;
        }

        if (!project.CanStart())
        {
            Debug.Log("Cannot start project: Requirements not met or insufficient budget");
            return false;
        }

        ResourceManager rm = GameManager.Instance.resourceManager;
        if (!rm.SpendBudget(project.GetActualCost()))
        {
            return false;
        }

        Project projectInstance = Instantiate(project);
        projectInstance.currentProgress = 0;
        projectInstance.isCompleted = false;
        projectInstance.isAbandoned = false;

        activeProjects.Add(projectInstance);

        Debug.Log($"Started project: {project.projectName} (Cost: {project.GetActualCost()}, Duration: {project.GetActualDuration()} weeks)");
        return true;
    }

    public void AbandonProject(Project project)
    {
        if (activeProjects.Contains(project))
        {
            project.AbandonProject();
            activeProjects.Remove(project);
        }
    }

    public void SetProjectSkimming(Project project, SkimmingLevel level)
    {
        project.SetSkimmingLevel(level);
        Debug.Log($"Set {project.projectName} skimming to {level}");
    }

    public List<Project> GetAvailableProjects()
    {
        return availableProjects.Where(p => p.CanStart()).ToList();
    }

    public List<Project> GetActiveProjects()
    {
        return activeProjects;
    }

    public List<Project> GetCompletedProjects()
    {
        return completedProjects;
    }

    public int GetCompletedProjectsCount()
    {
        return completedProjects.Count;
    }

    public int GetCompletedProjectsApproval()
    {
        return completedProjects.Sum(p => p.GetActualApprovalGain());
    }

    public void ProcessProjectEffects()
    {
        foreach (Project project in activeProjects)
        {
            ResourceManager rm = GameManager.Instance.resourceManager;
            rm.AddHeat(1);
        }
    }

    public void ModifyProjectCost(int amount)
    {
        foreach (Project project in activeProjects)
        {
            globalCostMultiplier += amount * 0.01f;
        }
    }

    public void ModifyProjectTime(int weeks)
    {
        globalTimeModifier += weeks;
    }

    public void ModifyProjectApproval(int amount)
    {
        globalApprovalModifier += amount;
    }

    public float GetProjectCostMultiplier()
    {
        float multiplier = globalCostMultiplier;

        foreach (Character character in GameManager.Instance.relationshipManager.GetAlliedCharacters())
        {
            multiplier *= character.allyBonus.projectCostMultiplier;
        }

        foreach (Character character in GameManager.Instance.relationshipManager.GetBetrayedCharacters())
        {
            multiplier *= character.betrayalPenalty.projectCostMultiplier;
        }

        return multiplier;
    }

    public int GetProjectTimeModifier()
    {
        int modifier = globalTimeModifier;

        foreach (Character character in GameManager.Instance.relationshipManager.GetAlliedCharacters())
        {
            modifier += character.allyBonus.projectTimeReduction;
        }

        foreach (Character character in GameManager.Instance.relationshipManager.GetBetrayedCharacters())
        {
            modifier += character.betrayalPenalty.projectTimeIncrease;
        }

        return modifier;
    }

    public int GetProjectApprovalModifier()
    {
        int modifier = globalApprovalModifier;

        foreach (Character character in GameManager.Instance.relationshipManager.GetAlliedCharacters())
        {
            modifier += character.allyBonus.projectApprovalBonus;
        }

        foreach (Character character in GameManager.Instance.relationshipManager.GetBetrayedCharacters())
        {
            modifier -= character.betrayalPenalty.projectApprovalPenalty;
        }

        return modifier;
    }

    public string GetProjectSummary()
    {
        return $"Active: {activeProjects.Count}/{maxActiveProjects}, Completed: {completedProjects.Count}";
    }

    public Project GetProjectByName(string projectName)
    {
        return availableProjects.FirstOrDefault(p => p.projectName == projectName) ??
               activeProjects.FirstOrDefault(p => p.projectName == projectName) ??
               completedProjects.FirstOrDefault(p => p.projectName == projectName);
    }
}