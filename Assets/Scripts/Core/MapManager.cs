using Core;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Office Expansion")]
    public int officeLevel = 1;
    public int maxOfficeLevel = 4;
    public int[] officeUpgradeCosts = { 1000, 2500, 5000 };
    public int[] officeWealthCapacity = { 500, 1000, 2500, 9999 };
    public int[] ghostEmployeeSlots = { 0, 3, 6, 10 };

    [Header("Ghost Employees")]
    public int currentGhostEmployees = 0;
    public int wealthPerGhost = 50;
    public int heatPerGhost = 1;

    [Header("Map Visualization")]
    public bool showApprovalIndicators = true;
    public bool showHeatVisualization = true;
    public bool showProjectProgress = true;

    public void UpgradeOffice()
    {
        if (officeLevel >= maxOfficeLevel) return;

        int upgradeCost = officeUpgradeCosts[officeLevel - 1];
        ResourceManager rm = GameManager.Instance.resourceManager;

        if (rm.SpendWealth(upgradeCost))
        {
            officeLevel++;
            Debug.Log($"Office upgraded to level {officeLevel}");
            OnOfficeUpgraded();
        }
    }

    private void OnOfficeUpgraded()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;
        rm.maxWealth = officeWealthCapacity[officeLevel - 1];

        Debug.Log($"New wealth capacity: {rm.maxWealth}, Ghost employee slots: {ghostEmployeeSlots[officeLevel - 1]}");
    }

    public void HireGhostEmployee()
    {
        if (currentGhostEmployees >= ghostEmployeeSlots[officeLevel - 1]) return;

        ResourceManager rm = GameManager.Instance.resourceManager;
        if (rm.SpendWealth(200))
        {
            currentGhostEmployees++;
            UpdatePassiveIncome();
            Debug.Log($"Hired ghost employee {currentGhostEmployees}/{ghostEmployeeSlots[officeLevel - 1]}");
        }
    }

    public void FireGhostEmployee()
    {
        if (currentGhostEmployees > 0)
        {
            currentGhostEmployees--;
            UpdatePassiveIncome();
            Debug.Log($"Fired ghost employee {currentGhostEmployees}/{ghostEmployeeSlots[officeLevel - 1]}");
        }
    }

    private void UpdatePassiveIncome()
    {
        ResourceManager rm = GameManager.Instance.resourceManager;
        rm.SetPassiveWealthIncome(currentGhostEmployees * wealthPerGhost);
    }

    public void ProcessMapEffects()
    {
        if (currentGhostEmployees > 0)
        {
            ResourceManager rm = GameManager.Instance.resourceManager;
            rm.AddHeat(currentGhostEmployees * heatPerGhost);
        }
    }

    public bool CanUpgradeOffice()
    {
        if (officeLevel >= maxOfficeLevel) return false;

        ResourceManager rm = GameManager.Instance.resourceManager;
        return rm.wealth >= officeUpgradeCosts[officeLevel - 1];
    }

    public bool CanHireGhost()
    {
        if (currentGhostEmployees >= ghostEmployeeSlots[officeLevel - 1]) return false;

        ResourceManager rm = GameManager.Instance.resourceManager;
        return rm.wealth >= 200;
    }

    public int GetNextUpgradeCost()
    {
        if (officeLevel >= maxOfficeLevel) return 0;
        return officeUpgradeCosts[officeLevel - 1];
    }

    public int GetAvailableGhostSlots()
    {
        return ghostEmployeeSlots[officeLevel - 1] - currentGhostEmployees;
    }

    public string GetOfficeStatus()
    {
        return $"Office Level {officeLevel}/{maxOfficeLevel}, Ghosts: {currentGhostEmployees}/{ghostEmployeeSlots[officeLevel - 1]}";
    }
}