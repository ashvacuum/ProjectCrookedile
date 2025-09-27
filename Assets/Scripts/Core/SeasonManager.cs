using Core;
using UnityEngine;

public class SeasonManager : MonoBehaviour
{
    [Header("Season Effects")]
    public SeasonData currentSeasonData;
    public SeasonData[] seasonDataArray = new SeasonData[3];

    private void Start()
    {
        InitializeSeasons();
    }

    private void InitializeSeasons()
    {
        seasonDataArray[0] = CreateSummerData();
        seasonDataArray[1] = CreateRainyData();
        seasonDataArray[2] = CreateChristmasData();

        currentSeasonData = seasonDataArray[0];
    }

    public void OnSeasonChange(Season newSeason)
    {
        currentSeasonData = seasonDataArray[(int)newSeason];
        ApplySeasonEffects();

        Debug.Log($"Season changed to {newSeason}: {currentSeasonData.description}");
    }

    public void ProcessSeasonEffects()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;

        if (currentSeasonData.approvalPerTurn != 0)
        {
            rm.AddApproval(currentSeasonData.approvalPerTurn);
        }

        if (currentSeasonData.heatPerTurn != 0)
        {
            rm.AddHeat(currentSeasonData.heatPerTurn);
        }

        if (currentSeasonData.budgetModifier != 0)
        {
            rm.budget += currentSeasonData.budgetModifier;
        }
    }

    private void ApplySeasonEffects()
    {
        ProjectManager pm = GameManager.Instance.projectManager;

        if (currentSeasonData.projectDelayWeeks > 0)
        {
            pm.ModifyProjectTime(currentSeasonData.projectDelayWeeks);
        }
    }

    private SeasonData CreateSummerData()
    {
        return new SeasonData
        {
            seasonName = "Summer (Tag-init)",
            description = "Peak construction season with intense heat",
            approvalPerTurn = 0,
            heatPerTurn = 0,
            budgetModifier = 0,
            projectDelayWeeks = 0,
            specialEvents = new string[] { "Power Outage", "Water Shortage" }
        };
    }

    private SeasonData CreateRainyData()
    {
        return new SeasonData
        {
            seasonName = "Rainy Season (Tag-ulan)",
            description = "Construction delays but emergency funding opportunities",
            approvalPerTurn = -1,
            heatPerTurn = 0,
            budgetModifier = 50,
            projectDelayWeeks = 1,
            specialEvents = new string[] { "Flooding", "Landslide", "Emergency Relief" }
        };
    }

    private SeasonData CreateChristmasData()
    {
        return new SeasonData
        {
            seasonName = "Christmas Season (Tag-lamig)",
            description = "Gift-giving season and campaign finale",
            approvalPerTurn = 2,
            heatPerTurn = -1,
            budgetModifier = 100,
            projectDelayWeeks = 0,
            specialEvents = new string[] { "Christmas Bonus", "Holiday Event", "Year-end Audit" }
        };
    }
}

[System.Serializable]
public class SeasonData
{
    public string seasonName;
    public string description;
    public int approvalPerTurn;
    public int heatPerTurn;
    public int budgetModifier;
    public int projectDelayWeeks;
    public string[] specialEvents;
}