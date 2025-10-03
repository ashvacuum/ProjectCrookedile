# CORRUPTION TYCOON - SCENE SETUP GUIDE

## 🎬 SCENE ARCHITECTURE

### Scene Names (as defined in SceneController.cs)
- **Bootstrap** - Core managers and initialization
- **MainMenu** - Title screen and navigation
- **Game** - Primary gameplay scene
- **MapDetail** - Detailed map view (additive)
- **EndGame** - Election results and statistics

---

## 🎯 SCENE-BY-SCENE REQUIREMENTS

### 1. BOOTSTRAP SCENE
**Purpose**: Initialize all core managers and persistent systems
**Load Type**: Always loaded first, contains DontDestroyOnLoad objects

#### Required GameObjects & Scripts:
```
Bootstrap
├── GameManager (GameObject)
│   └── Script: GameManager.cs
├── ResourceManager (GameObject)
│   └── Script: ResourceManager.cs
├── CardManager (GameObject)
│   └── Script: CardManager.cs
├── RelationshipManager (GameObject)
│   └── Script: RelationshipManager.cs
├── ProjectManager (GameObject)
│   └── Script: ProjectManager.cs
├── MapManager (GameObject)
│   └── Script: MapManager.cs
├── SeasonManager (GameObject)
│   └── Script: SeasonManager.cs
├── EnemyManager (GameObject)
│   └── Script: EnemyManager.cs
├── UIToolkitManager (GameObject)
│   └── Script: UIToolkitManager.cs
├── SceneController (GameObject)
│   └── Script: SceneController.cs
└── SaveLoadManager (GameObject)
    └── Script: SaveLoadManager.cs
```

#### Critical Script Assignments:

**GameManager.cs Configuration:**
```
[Header("Managers")] - MUST BE ASSIGNED IN INSPECTOR:
✓ resourceManager → Drag ResourceManager GameObject
✓ cardManager → Drag CardManager GameObject
✓ relationshipManager → Drag RelationshipManager GameObject
✓ projectManager → Drag ProjectManager GameObject
✓ mapManager → Drag MapManager GameObject
✓ seasonManager → Drag SeasonManager GameObject
✓ enemyManager → Drag EnemyManager GameObject
```

**UIToolkitManager.cs Configuration:**
```
[Header("UI Documents")] - MUST BE ASSIGNED IN INSPECTOR:
✓ gameUIDocument → Drag GameUI UIDocument from Game scene
✓ mainMenuUIDocument → Drag MainMenuUI UIDocument from MainMenu scene
✓ loadingUIDocument → Drag LoadingUI UIDocument (child of UIToolkitManager)

[Header("Visual Tree Assets")] - MUST BE ASSIGNED IN INSPECTOR:
✓ cardElementTemplate → CardElement.uxml asset
✓ characterElementTemplate → CharacterElement.uxml asset
✓ projectElementTemplate → ProjectElement.uxml asset
```

**SceneController.cs Configuration:**
```
[Header("Scene Names")] - VERIFY EXACT SPELLING:
✓ bootstrapScene = "Bootstrap"
✓ mainMenuScene = "MainMenu"
✓ gameScene = "Game"
✓ mapDetailScene = "MapDetail"
✓ endGameScene = "EndGame"
```

**ResourceManager.cs Configuration:**
```
[Header("Resource Limits")] - SET INITIAL VALUES:
✓ budget = 200 (starting budget)
✓ approval = 65 (starting approval)
✓ wealth = 100 (starting wealth)
✓ heat = 0 (starting heat)
✓ energy = 3 (starting energy)
✓ impunity = 0 (starting impunity)
```

**CardManager.cs Configuration:**
```
[Header("Starting Cards")] - ASSIGN STARTER DECK:
✓ starterCards → List of 14 starter Card ScriptableObjects
  ├── 6 Charm cards (Kind Words, Small Gift, etc.)
  ├── 4 Defense cards (Plausible Deniability, etc.)
  └── 4 Attack cards (Regulatory Pressure, etc.)
```

**RelationshipManager.cs Configuration:**
```
[Header("Character Database")] - ASSIGN CHARACTER DATA:
✓ _allCharacters → List of all Character ScriptableObjects
  ├── Government sector characters (Carmen, Santos, Rivera, Torres)
  ├── Business sector characters (Alex, Don Ricardo, Jennifer, Patricia)
  ├── Media sector characters (River, Carlos, Ramon)
  └── Community sector characters (Sage, Rodriguez, Sarah)
```

**ProjectManager.cs Configuration:**
```
[Header("Project Database")] - ASSIGN PROJECT DATA:
✓ availableProjects → List of all Project ScriptableObjects
  ├── Quick projects (1 week: Pothole repairs, etc.)
  ├── Standard projects (2 weeks: Road resurfacing, etc.)
  ├── Major projects (4 weeks: Highway extension, etc.)
  └── Mega projects (6-8 weeks: Airport terminal, etc.)
```

#### Manager Dependencies:
- **GameManager**: References to all other managers
- **UIToolkitManager**: UIDocument references for each scene
- **SceneController**: Scene name strings configured
- **All Managers**: Properly configured in inspector

---

### 2. MAINMENU SCENE
**Purpose**: Title screen, new game, continue, settings
**Load Type**: Standard scene replacement

#### Required GameObjects & Scripts:
```
MainMenuScene
├── MainMenuUI (GameObject)
│   └── Script: UIDocument
│   └── Source Asset: MainMenu.uxml
│   └── Panel Settings: Default Runtime Panel Settings
└── MainMenuCamera (GameObject)
    └── Script: Camera
    └── Clear Flags: Solid Color
    └── Background: Dark/Black
```

#### Script Configuration:
- **UIDocument**: Source Asset must reference MainMenu.uxml
- **Camera**: Standard camera setup for menu display
- **No custom scripts required** - UI handled by UIToolkitManager singleton

#### UIDocument Configuration:
- **Source Asset**: MainMenu.uxml
- **Panel Settings**: Default
- **Sort Order**: 0

#### UI Elements Required in UXML:
- `new-game-button` - Calls SceneController.StartNewGame()
- `continue-button` - Calls SceneController.LoadGame()
- `settings-button` - Opens settings popup
- `quit-button` - Calls Application.Quit()

---

### 3. GAME SCENE
**Purpose**: Main gameplay interface
**Load Type**: Standard scene replacement

#### Required GameObjects & Scripts:
```
GameScene
├── GameUI (GameObject)
│   └── Script: UIDocument
│   └── Source Asset: GameUI.uxml
│   └── Panel Settings: Default Runtime Panel Settings
└── MainCamera (GameObject)
    └── Script: Camera
    └── Clear Flags: Solid Color
    └── Background: Dark Gray (#2B2B2B)
```

#### Script Configuration:
- **UIDocument**: Source Asset must reference GameUI.uxml
- **Camera**: Standard camera for main game view
- **No EventSystem needed** - UI Toolkit handles input directly
- **No custom scripts required** - UI handled by UIToolkitManager singleton

#### UIDocument Configuration:
- **Source Asset**: GameUI.uxml
- **Panel Settings**: Default
- **Sort Order**: 0

#### Required Visual Tree Assets in UIToolkitManager:
- `cardElementTemplate` → CardElement.uxml
- `characterElementTemplate` → CharacterElement.uxml
- `projectElementTemplate` → ProjectElement.uxml

#### UI Elements Required in GameUI.uxml:
```
Resource Display:
├── budget-value, approval-value, wealth-value
├── heat-value, energy-value, impunity-value
├── approval-bar, heat-bar
└── turn-info, season-info

Interactive Areas:
├── card-hand (ScrollView)
├── project-list (ScrollView)
├── relationship-list (ScrollView)
└── map-container

Action Buttons:
├── projects-button
├── map-button
├── settings-button
└── end-turn-button
```

---

### 4. MAPDETAIL SCENE (Additive)
**Purpose**: Detailed map view for project management
**Load Type**: Additive loading over Game scene

#### Required GameObjects & Scripts:
```
MapDetailScene
├── MapDetailUI (GameObject)
│   └── Script: UIDocument
│   └── Source Asset: MapDetail.uxml (TODO: Create)
│   └── Panel Settings: Default Runtime Panel Settings
└── MapDetailCamera (GameObject - Optional)
    └── Script: Camera
    └── Culling Mask: MapDetail layer only
```

#### Script Configuration:
- **UIDocument**: Source Asset references MapDetail.uxml (to be created)
- **Camera**: Optional overlay camera for detailed map view
- **No custom scripts required** - UI handled by UIToolkitManager singleton

#### UI Requirements:
- Interactive town map
- Project placement interface
- Office expansion controls
- Close button to return to Game scene

---

### 5. ENDGAME SCENE
**Purpose**: Election results and final statistics
**Load Type**: Standard scene replacement

#### Required GameObjects & Scripts:
```
EndGameScene
├── EndGameUI (GameObject)
│   └── Script: UIDocument
│   └── Source Asset: EndGame.uxml (TODO: Create)
│   └── Panel Settings: Default Runtime Panel Settings
└── ResultsCamera (GameObject)
    └── Script: Camera
    └── Clear Flags: Solid Color
    └── Background: Contextual (Green for victory, Red for defeat)
```

#### Script Configuration:
- **UIDocument**: Source Asset references EndGame.uxml (to be created)
- **Camera**: Standard camera with contextual background color
- **No custom scripts required** - Results handled by GameManager and UIToolkitManager

#### UI Requirements:
- Election results display
- Final statistics summary
- Restart/Main Menu buttons
- Achievement/score display

---

### 6. LOADING OVERLAY (Built into UIToolkitManager)
**Purpose**: Async loading progress display
**Load Type**: Overlay during transitions
**Implementation**: UIDocument managed by UIToolkitManager singleton

#### Required Configuration in UIToolkitManager:
```
UIToolkitManager.loadingUIDocument → References LoadingScreen UIDocument
```

#### Loading UIDocument Setup:
- **GameObject**: LoadingUI (child of UIToolkitManager GameObject)
- **Script**: UIDocument
- **Source Asset**: LoadingScreen.uxml
- **Panel Settings**: Default Runtime Panel Settings
- **Sort Order**: 100 (appears above game UI)

#### UI Elements Required:
- `loading-progress` (ProgressBar)
- `loading-text` (Label)

---

## 🔧 MANAGER SETUP CHECKLIST

### GameManager Configuration:
```
[Header("Managers")]
✓ ResourceManager → Drag ResourceManager object
✓ CardManager → Drag CardManager object
✓ RelationshipManager → Drag RelationshipManager object
✓ ProjectManager → Drag ProjectManager object
✓ MapManager → Drag MapManager object
✓ SeasonManager → Drag SeasonManager object
✓ EnemyManager → Drag EnemyManager object
```

### UIToolkitManager Configuration:
```
[Header("UI Documents")]
✓ gameUIDocument → Drag GameUI UIDocument
✓ mainMenuUIDocument → Drag MainMenuUI UIDocument
✓ loadingUIDocument → Drag LoadingUI UIDocument

[Header("Visual Tree Assets")]
✓ cardElementTemplate → CardElement.uxml
✓ characterElementTemplate → CharacterElement.uxml
✓ projectElementTemplate → ProjectElement.uxml
```

### SceneController Configuration:
```
[Header("Scene Names")]
✓ bootstrapScene = "Bootstrap"
✓ mainMenuScene = "MainMenu"
✓ gameScene = "Game"
✓ mapDetailScene = "MapDetail"
✓ endGameScene = "EndGame"

[Header("Transition Settings")]
✓ transitionDuration = 1f
✓ useLoadingScreen = true
```

---

## 🚀 BUILD SETTINGS

### Scene Order in Build Settings:
1. **Bootstrap** (Index 0) - Always first
2. **MainMenu** (Index 1) - Default loading scene
3. **Game** (Index 2) - Core gameplay
4. **MapDetail** (Index 3) - Additive scene
5. **EndGame** (Index 4) - Results display

### Player Settings:
- **First Scene**: MainMenu (Bootstrap auto-loads first)
- **Splash Screen**: Disabled for faster loading
- **Loading Screen**: Custom via UIToolkitManager

---

## 📋 CREATION CHECKLIST

### Scene Creation Steps:
1. ✅ Create Bootstrap scene with all managers
2. ⏳ Create MainMenu scene with UIDocument
3. ⏳ Create Game scene with UIDocument
4. ⏳ Create MapDetail scene (additive)
5. ⏳ Create EndGame scene with UIDocument

### Asset Creation Steps:
1. ✅ Create all UXML files
2. ✅ Create all USS stylesheets
3. ⏳ Create ScriptableObject data files
4. ⏳ Assign all references in managers
5. ⏳ Test scene transitions

### Testing Checklist:
- [ ] Bootstrap → MainMenu transition
- [ ] MainMenu → Game transition (new game)
- [ ] MainMenu → Game transition (continue)
- [ ] Game → MapDetail transition (additive)
- [ ] MapDetail → Game transition (unload)
- [ ] Game → EndGame transition
- [ ] Save/Load functionality
- [ ] UI updates via EventBus
- [ ] Resource changes trigger UI
- [ ] Card playing works
- [ ] Project management works

---

## 🎨 VISUAL HIERARCHY

### UI Layer Structure:
```
GameUI (Sort Order: 0)
├── Resource Panel (Top)
├── Card Hand (Left)
├── Map View (Center)
├── Projects/Relations (Right)
└── Action Buttons (Bottom)

LoadingUI (Sort Order: 100)
└── Loading Overlay (Full Screen)

PopupUI (Sort Order: 200)
└── Modal Dialogs (Center)
```

---

## 🚨 CRITICAL TROUBLESHOOTING

### Common Setup Errors & Solutions:

**1. NullReferenceException on GameManager.Instance:**
```
Problem: Managers not found by GameManager.InitializeGame()
Solution: Ensure all manager GameObjects exist in Bootstrap scene
Check: Each manager has correct script attached
```

**2. UI Elements not updating:**
```
Problem: UIDocument references not assigned in UIToolkitManager
Solution: Drag UI Documents from each scene to UIToolkitManager inspector
Check: UXML files are properly assigned to UIDocument components
```

**3. Scene transitions fail:**
```
Problem: Scene names don't match Build Settings
Solution: Verify exact spelling in SceneController scene name fields
Check: All scenes added to Build Settings in correct order
```

**4. Cards/Characters/Projects not loading:**
```
Problem: ScriptableObject lists not assigned to managers
Solution: Create ScriptableObject data files and assign to manager lists
Check: Assets exist in project and are properly referenced
```

**5. EventBus errors:**
```
Problem: Namespace conflicts with new namespaces
Solution: Add proper using statements to all files
Check: All scripts compile without errors
```

### Required Namespace Usage:
```csharp
// Add these using statements as needed:
using Core;           // For GameManager, Singleton, EventBus
using UI;             // For UIToolkitManager
using Projects;       // For Project, ProjectManager
using Relationships;  // For Character, RelationshipManager
```

### Critical File Locations:
```
Scripts/Core/         → Core managers and systems
Scripts/UI/           → UIToolkitManager
Scripts/Cards/        → Card system
Scripts/Projects/     → Project system
Scripts/Relationships/ → Character system
UI/UXML/             → UI layout files
UI/Styles/           → UI styling files
```

This guide provides everything needed to set up the Unity scenes correctly with proper manager references and UI configuration!