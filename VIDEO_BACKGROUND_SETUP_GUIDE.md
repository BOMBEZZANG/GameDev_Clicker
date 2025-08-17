# Video Background Setup Guide for Unity

## Overview
This guide explains how to set up the MP4 video background system in Unity for the Developer Clicker game.

## Setup Steps

### 1. Create the Video Background Manager GameObject

1. **In Unity Hierarchy:**
   - Right-click in the Hierarchy window
   - Select `Create Empty`
   - Name it exactly: `VideoBackgroundManager`

### 2. Add the VideoBackgroundManager Component

1. **Select the VideoBackgroundManager GameObject**
2. **In the Inspector:**
   - Click `Add Component`
   - Search for `VideoBackgroundManager`
   - Click to add it

### 3. Create UI Elements for Video Display

1. **Create a Canvas (if not already present):**
   - Right-click in Hierarchy → UI → Canvas
   - Set Canvas Scaler UI Scale Mode to `Scale With Screen Size`
   - Set Reference Resolution to your target resolution (e.g., 1920x1080)

2. **Create the Background Panel:**
   - Right-click on Canvas → UI → Panel
   - Rename it to `BackgroundPanel`
   - Set anchors to stretch full screen (Alt+Shift click on bottom-right preset)
   - Set Left, Top, Right, Bottom to 0
   - Remove the Image component or set its color alpha to 0

3. **Create the Video Display:**
   - Right-click on BackgroundPanel → UI → Raw Image
   - Rename it to `VideoDisplay`
   - Set anchors to stretch full screen
   - Set Left, Top, Right, Bottom to 0

### 4. Configure the VideoBackgroundManager Component

1. **In the VideoBackgroundManager Inspector:**

   **Video Settings:**
   - `Background Image`: Drag the `VideoDisplay` RawImage here
   - `Video Player`: Leave empty (it will be auto-created)

   **Stage Configuration:**
   - The component will auto-populate with 10 stages
   - Each stage is already configured with the correct MP4 filename
   - You can expand each stage to verify:
     - Stage 1: stage1_indie_room.mp4
     - Stage 2: stage2_mobile_dev.mp4
     - Stage 3: stage3_PC_Game_dev.mp4
     - Stage 4: stage4_VR_lab.mp4
     - Stage 5: stage5_Ai.mp4
     - Stage 6: stage6_lobot.mp4
     - Stage 7: stage7_rocket.mp4
     - Stage 8: stage8_space.mp4
     - Stage 9: stage9_blackhole.mp4
     - Stage 10: stage10_Timemachine.mp4

### 5. Create Click Zones (Optional)

For each stage that needs clickable areas:

1. **Create Click Zone Buttons:**
   - Right-click on BackgroundPanel → UI → Button
   - Name it descriptively (e.g., `ClickZone_Computer`)
   - Position and size it over the clickable area
   - Set the Button's Image alpha to 0 (invisible)
   - Remove or make transparent the Text child object

2. **Configure in VideoBackgroundManager:**
   - Expand the relevant stage in Stage Data
   - Add elements to `Stage Click Zones`
   - For each zone:
     - `Zone Name`: Descriptive name
     - `Zone Transform`: Drag the button's RectTransform
     - `Zone Button`: Drag the button component
     - `On Click Event`: Configure what happens on click

### 6. Connect to GamePresenter

1. **In the GamePresenter GameObject:**
   - Find the GamePresenter component
   - In the `Video Background Manager Object` field
   - Drag the VideoBackgroundManager GameObject

### 7. File Structure Verification

Ensure your MP4 files are in the correct location:
```
Assets/
  GameData/
    BG_Images/
      mp4/
        stage1_indie_room.mp4
        stage2_mobile_dev.mp4
        stage3_PC_Game_dev.mp4
        stage4_VR_lab.mp4
        stage5_Ai.mp4
        stage6_lobot.mp4
        stage7_rocket.mp4
        stage8_space.mp4
        stage9_blackhole.mp4
        stage10_Timemachine.mp4
```

## Testing

1. **Enter Play Mode**
2. **Check Console for:**
   - "Stage data auto-populated with default values" (first time)
   - "Loading video from: file:///..." message
   - "Video prepared successfully" message

3. **If you see errors:**
   - Verify MP4 files exist in the correct path
   - Check file names match exactly (case-sensitive)
   - Ensure VideoDisplay RawImage is assigned
   - Check that Background Image field is set in Inspector

## Troubleshooting

### Video Not Playing
- Ensure MP4 files are H.264 encoded
- Check Unity's VideoPlayer supports your codec
- Verify file paths are correct
- Check Console for specific error messages

### Black Screen
- Verify Background Image (RawImage) is assigned
- Check RenderTexture is being created
- Ensure VideoDisplay is not behind other UI elements

### Performance Issues
- Consider reducing video resolution
- Use lower bitrate encoding
- Enable hardware acceleration in Player Settings

## Stage Switching

The video will automatically change when the game stage changes. You can also manually test stage switching:

1. **In Play Mode:**
   - Select VideoBackgroundManager in Hierarchy
   - In Inspector, find the public methods
   - Use `LoadStageByNumber` with values 1-10
   - Or use `NextStage`/`PreviousStage` buttons

## Notes

- Videos loop automatically
- Videos pause when the application loses focus
- The system creates a RenderTexture at 1920x1080 resolution
- You can adjust this in the SetupVideoPlayer() method if needed