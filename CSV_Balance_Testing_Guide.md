# CSV Balance Testing Guide

## Setup Instructions

### 1. Scene Setup
1. Open your main game scene in Unity
2. Create an empty GameObject and name it "BalanceTestController"
3. Add the `BalanceTestController` component to this GameObject
4. Add the `BalanceIntegrationSystem` component to the same GameObject (it will be added automatically if not present)

### 2. UI Setup
Create the following UI elements in your scene:

#### Canvas Structure:
```
Canvas
├── Panel (Background)
│   ├── StatusText (TextMeshProUGUI)
│   ├── PlayerDataText (TextMeshProUGUI)
│   ├── UpgradesText (TextMeshProUGUI)
│   ├── ProjectsText (TextMeshProUGUI)
│   └── ButtonPanel
│       ├── LoadDataButton (Button with TextMeshProUGUI)
│       ├── TestClickButton (Button with TextMeshProUGUI)
│       └── AutoIncomeButton (Button with TextMeshProUGUI)
```

### 3. Component Configuration
1. Select the BalanceTestController GameObject
2. In the Inspector, assign the UI elements:
   - Status Text → StatusText
   - Player Data Text → PlayerDataText
   - Upgrades Text → UpgradesText
   - Projects Text → ProjectsText
   - Load Data Button → LoadDataButton
   - Test Click Button → TestClickButton
   - Auto Income Button → AutoIncomeButton

### 4. Test Settings
In the BalanceTestController component:
- Test Player Level: 1 (starting level)
- Test Player Exp: 0 (starting experience)
- Test Player Money: 1000 (starting money for testing)
- Test Stage: 1 (starting stage)
- Run Simulation: Toggle this for keyboard shortcuts
- Simulation Speed: 1 (adjust for faster/slower testing)

## Testing Workflow

### Basic Testing:
1. **Play the scene** in Unity Editor
2. **Click "Load Data"** - This loads all CSV files
3. **Check the console** for any loading errors
4. **View loaded data** in the Status Text area
5. **Test clicking** with the "Test Click" button
6. **Toggle auto income** with the "Start Auto" button

### Advanced Testing:
Enable "Run Simulation" in the inspector and use:
- **U key** - Purchase a random available upgrade
- **P key** - Start a random available project
- **R key** - Reset all test data to initial values

## CSV File Locations

All balance CSV files are located in:
`Assets/GameData/CSV/Balancing/`

- **Upgrades.csv** - All upgrade definitions
- **Levels.csv** - Level progression data
- **Projects.csv** - Project definitions and rewards
- **Stage.csv** - Stage unlock requirements
- **DualCurrency.csv** - Currency conversion rates
- **ExpectedTime.csv** - Time progression targets

## Modifying Balance Data

### To modify upgrade costs:
1. Open `Upgrades.csv`
2. Edit the `base_price` column for initial cost
3. Edit the `price_multiplier` column for scaling (1.15 = 15% increase per level)
4. Save the file
5. Stop and restart play mode in Unity

### To modify level requirements:
1. Open `Levels.csv`
2. Edit the `required_exp` column
3. Edit the `money_multiplier` column for income scaling
4. Save and restart

### To modify project rewards:
1. Open `Projects.csv`
2. Edit the `base_reward` column
3. Edit the `completion_time` for duration
4. Save and restart

## Common Issues and Solutions

### Issue: "No game data available from SaveManager"
**Solution**: This is normal on first run. The system creates default data automatically.

### Issue: CSV files not loading
**Solutions**:
1. Check file path exists: `Assets/GameData/CSV/Balancing/`
2. Ensure CSV files have correct encoding (UTF-8)
3. Check console for specific loading errors

### Issue: Upgrades not appearing
**Solutions**:
1. Check `unlock_condition` in Upgrades.csv
2. Verify player level meets requirements
3. Check stage requirements

### Issue: Values not updating
**Solution**: CSV data is loaded once at startup. Stop and restart play mode after CSV changes.

## Balance Testing Checklist

- [ ] All CSV files load without errors
- [ ] Click values increase with upgrades
- [ ] Auto income works correctly
- [ ] Upgrade prices scale appropriately
- [ ] Projects complete and give rewards
- [ ] Level progression feels balanced
- [ ] Stage unlocks work at correct levels
- [ ] Money system unlocks at level 10
- [ ] All upgrade effects apply correctly
- [ ] Save/Load maintains progression

## Performance Metrics to Monitor

1. **Early Game (Levels 1-10)**
   - Time to first upgrade
   - Time to unlock money system
   - Click value progression

2. **Mid Game (Levels 11-30)**
   - Balance between click and auto income
   - Project completion rates
   - Upgrade purchase frequency

3. **Late Game (Levels 30+)**
   - Exponential growth control
   - Stage progression pacing
   - Resource accumulation rates

## Debugging Commands

In the Console window, you can check:
```
CSVLoader.Instance.LoadedBalanceData.Upgrades.Count
BalanceManager.Instance.GetUpgrade("skill_01")
BalanceManager.Instance.CalculateUpgradePrice("skill_01", 5)
```

## Notes

- The system supports hot-reloading during development
- All number formatting is handled automatically (K, M, B suffixes)
- The test controller is for development only - remove before release
- Save data is stored separately from CSV balance data
- CSV changes don't affect existing save files automatically