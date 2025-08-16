# HTML Background System Setup Guide

## 🚀 Quick Start

### 1. Install WebView Plugin
```
Tools → HTML Background → Setup WebView Plugin
```

### 2. Copy HTML Files  
```
Tools → HTML Background → Copy HTML Files from Source
```

### 3. Verify Setup
```
Tools → HTML Background → Check Setup Status
```

## 📋 Scene Setup

### Add Components to Your Scene:

1. **HTMLBackgroundManager** - Main background controller
   - Assign your background UI panel
   - Set UI camera reference
   - Will auto-configure stage data

2. **HTMLBackgroundPresenter** - Game integration layer
   - Links backgrounds to game progression
   - Handles automatic stage transitions

3. **HTMLBackgroundSystem** (Optional) - Event-driven management
   - Listens to game events for stage changes
   - Provides smooth transitions

## 🎮 Integration with Your Game

### Automatic Stage Changes:
- **Level milestones** → New backgrounds unlock
- **Project completion** → Stage advancement  
- **Feature unlocks** → Special stage transitions

### Interactive Click Zones:
Each HTML background has clickable areas that give game rewards:

| Stage | Clickable Objects | Rewards |
|-------|------------------|---------|
| Indie Room | Laptop, Coffee, Cat, Bookshelf | Money, XP, Productivity boosts |
| Mobile Dev | Phone, Tablet | Mobile development bonuses |
| PC Game Dev | PC, Monitor | Game development progress |
| VR Lab | VR Headset | VR technology advancement |
| AI Development | AI Server | Machine learning progress |
| Robot Lab | Robot | Robotics bonuses |
| Rocket Launch | Rocket | Space technology |
| Space Station | Control Panel | Interstellar research |
| Black Hole | Black Hole | Quantum physics |
| Time Machine | Time Machine | Temporal mechanics |

## 🛠️ Configuration

### Manual Stage Control:
```csharp
// In your code
HTMLBackgroundManager manager = FindObjectOfType<HTMLBackgroundManager>();
manager.LoadStageHTML(2); // Load PC Game Dev stage
manager.NextStage();       // Go to next stage
manager.PreviousStage();   // Go to previous stage
```

### Custom Click Zones:
```csharp
// Define clickable areas (normalized 0-1 coordinates)
var clickZone = new HTMLBackgroundManager.ClickZoneData {
    zoneName = "CustomObject",
    topLeft = new Vector2(0.3f, 0.4f),     // 30% from left, 40% from top
    bottomRight = new Vector2(0.7f, 0.8f), // 70% from left, 80% from top
    onClickEvent = new UnityEvent()
};
```

## 📁 File Structure

```
Assets/
├── Plugins/                      ← WebView plugin files
│   ├── WebViewObject.cs
│   ├── Android/
│   ├── iOS/
│   └── WebGL/
├── StreamingAssets/HTML/          ← Stage HTML files  
│   ├── stage1_indie_room.html
│   ├── stage2_mobile_dev.html
│   └── ... (all 10 stages)
└── Scripts/
    ├── Game/Managers/
    │   └── HTMLBackgroundManager.cs
    ├── Game/Presenters/
    │   └── HTMLBackgroundPresenter.cs
    ├── Game/Systems/
    │   └── HTMLBackgroundSystem.cs
    └── Editor/
        ├── HTMLAssetProcessor.cs
        └── WebViewSetupChecker.cs
```

## 🎯 Usage Examples

### Basic Setup in Scene:
1. Create empty GameObject → Add `HTMLBackgroundManager`
2. Assign your background UI panel
3. Set UI camera reference
4. Use `HTMLBackgroundSetup.cs` for initial configuration

### Advanced Integration:
1. Add `HTMLBackgroundPresenter` for automatic progression
2. Configure stage unlock levels
3. Set up project-based or level-based stage transitions

### Testing:
- Use the provided test buttons in `HTMLBackgroundSetup`
- Check console for click zone detection logs
- Verify HTML files load correctly

## 🔧 Troubleshooting

### Common Issues:

**"WebViewObject not found"**
- Run: `Tools → HTML Background → Setup WebView Plugin`

**"HTML file not found"**  
- Run: `Tools → HTML Background → Copy HTML Files from Source`
- Check that StreamingAssets/HTML folder exists

**Click zones not working**
- Verify background panel is assigned
- Check that UI camera is set correctly
- Make sure click zone coordinates are in 0-1 range

**No automatic stage changes**
- Verify `HTMLBackgroundPresenter` is in scene
- Check that `autoChangeWithStage` is enabled
- Ensure proper event subscriptions

## 💡 Tips

1. **Performance**: HTML backgrounds are rendered as textures, so they're efficient
2. **Responsive**: Click zones use normalized coordinates for different screen sizes  
3. **Extensible**: Easy to add new stages by adding HTML files and configuration
4. **Integrated**: Works seamlessly with your existing GameDevClicker progression system

Ready to use! Your HTML files will now display as interactive backgrounds that enhance gameplay with clickable zones giving real game rewards.