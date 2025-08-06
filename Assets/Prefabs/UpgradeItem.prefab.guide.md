# Upgrade Item Prefab Creation Guide

## How to Create the UpgradeItem Prefab

### 1. Create the Upgrade Item GameObject
```
UpgradeItem (GameObject)
├── Background (Image)
├── Content (Horizontal Layout Group)
│   ├── Info Section (Vertical Layout Group)  
│   │   ├── Name Text (TextMeshPro)
│   │   ├── Description Text (TextMeshPro)
│   │   └── Level Text (TextMeshPro)
│   └── Button Section (Vertical Layout Group)
│       └── Upgrade Button (Button)
│           └── Button Text (TextMeshPro)
```

### 2. Component Configuration

**UpgradeItem (Root)**
- RectTransform: 
  - Width: 340, Height: 80
  - Anchor: Top Stretch
- UpgradeItemUI script component
- Background Image:
  - Color: RGBA(255, 255, 255, 25)  
  - Image Type: Sliced with rounded corners

**Content (Horizontal Layout Group)**
- Horizontal Layout Group:
  - Spacing: 10
  - Child Alignment: Middle Left
  - Control Child Size: Width ✓, Height ✓
  - Use Child Scale: ✓
  - Child Force Expand: Width ✓
- Content Size Fitter:
  - Horizontal Fit: Preferred Size

**Info Section (Vertical Layout Group)**
- Vertical Layout Group:
  - Spacing: 2
  - Child Alignment: Upper Left
  - Control Child Size: Width ✓, Height ✓
  - Use Child Scale: ✓
  - Child Force Expand: Width ✓
- Layout Element:
  - Min Width: 200
  - Preferred Width: 250

**Name Text (TextMeshPro)**
- Font Size: 16
- Font Style: Bold
- Color: White
- Text: "Upgrade Name"
- Alignment: Left

**Description Text (TextMeshPro)**  
- Font Size: 12
- Color: RGB(200, 200, 200)
- Text: "Upgrade description text"
- Alignment: Left

**Level Text (TextMeshPro)**
- Font Size: 11
- Color: RGB(255, 215, 0) // Gold
- Text: "Level 1/10"
- Alignment: Left

**Button Section (Vertical Layout Group)**
- Vertical Layout Group:
  - Child Alignment: Middle Center
- Layout Element:
  - Min Width: 80
  - Preferred Width: 80

**Upgrade Button (Button)**
- RectTransform: Width: 75, Height: 35
- Button:
  - Target Graphic: Button Background
  - Transition: Color Tint
  - Normal Color: RGB(156, 39, 176) // Purple (Experience)
  - Highlighted: RGB(123, 31, 162)
  - Pressed: RGB(100, 25, 138)
  - Disabled: RGB(102, 102, 102)
- Image:
  - Color: RGB(156, 39, 176)
  - Image Type: Sliced

**Button Text (TextMeshPro)**
- Font Size: 10
- Font Style: Bold
- Color: White
- Text: "⭐ 100"
- Alignment: Center

### 3. Script Component Assignment

**UpgradeItemUI Script References:**
- upgradeNameText → Name Text
- upgradeDescText → Description Text  
- upgradeLevelText → Level Text
- upgradeButton → Upgrade Button
- buttonText → Button Text
- backgroundImage → Background Image

### 4. Color Configuration

The UpgradeItemUI script will automatically set button colors based on currency type:
- Experience upgrades: Purple (RGB 156, 39, 176)
- Money upgrades: Orange (RGB 255, 152, 0)
- Disabled state: Gray (RGB 102, 102, 102)

### 5. Save as Prefab

1. Create the hierarchy in the scene
2. Configure all components as specified
3. Attach the UpgradeItemUI script
4. Assign all script references
5. Drag to Assets/Prefabs/ folder to save as prefab
6. Delete from scene

The prefab will be instantiated by GameViewUI when populating upgrade lists.