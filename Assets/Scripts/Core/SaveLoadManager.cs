using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Core;
using Projects;
using Relationships;

public class SaveLoadManager : Singleton<SaveLoadManager>
{
    [Header("Save Settings")]
    public string saveFileName = "corruption_save.json";
    public bool autoSaveEnabled = true;
    public int autoSaveInterval = 3; // turns

    private string savePath;
    private int turnsSinceLastSave = 0;

    protected override void OnSingletonAwake()
    {
        InitializeSaveSystem();
    }

    private void InitializeSaveSystem()
    {
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        Debug.Log($"Save path: {savePath}");
    }

    public void SaveGame()
    {
        if (GameManager.Instance == null) return;

        GameSaveData saveData = CreateSaveData();
        string jsonData = JsonUtility.ToJson(saveData, true);

        try
        {
            File.WriteAllText(savePath, jsonData);
            Debug.Log("Game saved successfully");
            turnsSinceLastSave = 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    public bool LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No save file found");
            return false;
        }

        try
        {
            string jsonData = File.ReadAllText(savePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);

            ApplySaveData(saveData);
            Debug.Log("Game loaded successfully");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return false;
        }
    }

    public bool HasSaveFile()
    {
        return File.Exists(savePath);
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save file deleted");
        }
    }

    public void ProcessAutoSave()
    {
        if (!autoSaveEnabled) return;

        turnsSinceLastSave++;
        if (turnsSinceLastSave >= autoSaveInterval)
        {
            SaveGame();
        }
    }

    private GameSaveData CreateSaveData()
    {
        GameSaveData saveData = new GameSaveData();

        // Game state
        saveData.currentTurn = GameManager.Instance.currentTurn;
        saveData.currentSeason = (int)GameManager.Instance.currentSeason;
        saveData.gameWon = GameManager.Instance.gameWon;
        saveData.gameLost = GameManager.Instance.gameLost;

        // Resources
        ResourceManager rm = GameManager.Instance.resourceManager;
        saveData.budget = rm.budget;
        saveData.approval = rm.approval;
        saveData.wealth = rm.wealth;
        saveData.heat = rm.heat;
        saveData.energy = rm.energy;
        saveData.impunity = rm.impunity;
        saveData.passiveWealthIncome = rm.passiveWealthIncome;

        // Office/Map
        MapManager mm = GameManager.Instance.mapManager;
        if (mm != null)
        {
            saveData.officeLevel = mm.officeLevel;
            saveData.currentGhostEmployees = mm.currentGhostEmployees;
        }

        // Relationships
        RelationshipManager relm = GameManager.Instance.relationshipManager;
        if (relm != null)
        {
            saveData.characterRapport = new Dictionary<string, int>();
            saveData.betrayedCharacters = new List<string>();

            foreach (Character character in relm._unlockedCharacters)
            {
                saveData.characterRapport[character.characterName] = character.currentRapport;
                if (character.isBetrayed)
                {
                    saveData.betrayedCharacters.Add(character.characterName);
                }
            }
        }

        // Projects
        ProjectManager pm = GameManager.Instance.projectManager;
        if (pm != null)
        {
            saveData.activeProjects = new List<ProjectSaveData>();
            foreach (Project project in pm.GetActiveProjects())
            {
                saveData.activeProjects.Add(new ProjectSaveData
                {
                    projectName = project.projectName,
                    currentProgress = project.currentProgress,
                    skimmingLevel = (int)project.currentSkimmingLevel
                });
            }

            saveData.completedProjectNames = new List<string>();
            foreach (Project project in pm.GetCompletedProjects())
            {
                saveData.completedProjectNames.Add(project.projectName);
            }
        }

        return saveData;
    }

    private void ApplySaveData(GameSaveData saveData)
    {
        // Game state
        GameManager.Instance.currentTurn = saveData.currentTurn;
        GameManager.Instance.currentSeason = (Season)saveData.currentSeason;
        GameManager.Instance.gameWon = saveData.gameWon;
        GameManager.Instance.gameLost = saveData.gameLost;

        // Resources
        ResourceManager rm = GameManager.Instance.resourceManager;
        rm.budget = saveData.budget;
        rm.approval = saveData.approval;
        rm.wealth = saveData.wealth;
        rm.heat = saveData.heat;
        rm.energy = saveData.energy;
        rm.impunity = saveData.impunity;
        rm.passiveWealthIncome = saveData.passiveWealthIncome;

        // Office/Map
        MapManager mm = GameManager.Instance.mapManager;
        if (mm != null)
        {
            mm.officeLevel = saveData.officeLevel;
            mm.currentGhostEmployees = saveData.currentGhostEmployees;
        }

        // Relationships
        RelationshipManager relm = GameManager.Instance.relationshipManager;
        if (relm != null && saveData.characterRapport != null)
        {
            foreach (var kvp in saveData.characterRapport)
            {
                Character character = relm.GetCharacter(kvp.Key);
                if (character != null)
                {
                    character.currentRapport = kvp.Value;
                }
            }

            if (saveData.betrayedCharacters != null)
            {
                foreach (string characterName in saveData.betrayedCharacters)
                {
                    Character character = relm.GetCharacter(characterName);
                    if (character != null)
                    {
                        character.isBetrayed = true;
                    }
                }
            }
        }

        // Projects would need more complex restoration logic
        // For now, just restore basic state
    }
}

[System.Serializable]
public class GameSaveData
{
    public int currentTurn;
    public int currentSeason;
    public bool gameWon;
    public bool gameLost;

    public int budget;
    public int approval;
    public int wealth;
    public int heat;
    public int energy;
    public int impunity;
    public int passiveWealthIncome;

    public int officeLevel;
    public int currentGhostEmployees;

    public Dictionary<string, int> characterRapport;
    public List<string> betrayedCharacters;

    public List<ProjectSaveData> activeProjects;
    public List<string> completedProjectNames;
}

[System.Serializable]
public class ProjectSaveData
{
    public string projectName;
    public int currentProgress;
    public int skimmingLevel;
}