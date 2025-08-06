# Click Effect Prefab Creation Guide

## How to Create the ClickEffect Prefab

### 1. Create the Click Effect GameObject
```
ClickEffect (GameObject)
├── Effect Text (TextMeshPro)
```

### 2. Component Configuration

**ClickEffect (Root)**
- RectTransform:
  - Width: 120, Height: 60
  - Anchor: Middle Center
  - Pivot: (0.5, 0.5)
- CanvasGroup component (for fading)
  - Alpha: 1
  - Interactable: ✗
  - Blocks Raycasts: ✗

**Effect Text (TextMeshPro)**
- RectTransform: Fill parent (0, 0, 0, 0)
- TextMeshPro:
  - Font Size: 16
  - Font Style: Bold
  - Color: RGB(76, 175, 80) // Green
  - Text: "+10 ⭐"
  - Alignment: Center
  - Auto Size: Min 12, Max 20
- Shadow component:
  - Effect Color: RGBA(0, 0, 0, 76)
  - Effect Distance: (2, -2)

### 3. Animation Setup

The click effect animates by:
1. Moving upward (70 units over 1 second)
2. Fading out alpha from 1.0 to 0.0
3. Auto-destroying after animation

This is handled by the `AnimateClickEffect` coroutine in GameViewUI.

### 4. Usage

The prefab is instantiated by GameViewUI:
- On each click in the click zone
- Shows gained experience and money
- Positioned at random offset around click position
- Automatically destroyed after animation

### 5. Save as Prefab

1. Create GameObject with TextMeshPro child
2. Configure components as specified
3. Add CanvasGroup and Shadow components
4. Set all properties and colors
5. Drag to Assets/Prefabs/ folder to save as prefab
6. Delete from scene

The prefab will be instantiated by GameViewUI when clicks are performed.