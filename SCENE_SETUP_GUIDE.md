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

#### Script Configuration:
- **GameManager**: Must have all manager references assigned in inspector
- **UIToolkitManager**: Requires UIDocument and VisualTreeAsset references
- **All Managers**: Will be auto-found by GameManager.InitializeGame() using FindObjectOfType()

#### Manager Dependencies:
- **GameManager**: References to all other managers
- **UIToolkitManager**: UIDocument references for each scene
- **SceneController**: Scene name strings configured
- **All Managers**: Properly configured in inspector

---

### 2. MAINMENU SCENE
**Purpose**: Title screen, new game, continue, settings
**Load Type**: Standard scene replacement

#### Required GameObjects:
```
MainMenuScene
├── MainMenuUI (UIDocument)
│   └── UXML: MainMenu.uxml
│   └── USS: MainMenu.uss
└── MainMenuCamera
    └── Clear Flags: Solid Color
    └── Background: Dark/Black
```

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

#### Required GameObjects:
```
GameScene
├── GameUI (UIDocument)
│   └── UXML: GameUI.uxml
│   └── USS: GameUI.uss
├── MainCamera
│   └── Clear Flags: Solid Color
│   └── Background: Dark Gray
└── EventSystem (if using hybrid UI)
```

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

#### Required GameObjects:
```
MapDetailScene
├── MapDetailUI (UIDocument)
│   └── UXML: MapDetail.uxml (TODO: Create)
│   └── USS: MapDetail.uss (TODO: Create)
└── MapDetailCamera (Optional)
    └── Culling Mask: MapDetail layer only
```

#### UI Requirements:
- Interactive town map
- Project placement interface
- Office expansion controls
- Close button to return to Game scene

---

### 5. ENDGAME SCENE
**Purpose**: Election results and final statistics
**Load Type**: Standard scene replacement

#### Required GameObjects:
```
EndGameScene
├── EndGameUI (UIDocument)
│   └── UXML: EndGame.uxml (TODO: Create)
│   └── USS: EndGame.uss (TODO: Create)
└── ResultsCamera
    └── Clear Flags: Solid Color
    └── Background: Contextual (victory/defeat)
```

#### UI Requirements:
- Election results display
- Final statistics summary
- Restart/Main Menu buttons
- Achievement/score display

---

### 6. LOADING SCENE (Overlay)
**Purpose**: Async loading progress display
**Load Type**: Overlay during transitions

#### Required GameObjects:
```
LoadingOverlay
└── LoadingUI (UIDocument)
    └── UXML: LoadingScreen.uxml
    └── USS: LoadingScreen.uss
```

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

This guide provides everything needed to set up the Unity scenes correctly with proper manager references and UI configuration!