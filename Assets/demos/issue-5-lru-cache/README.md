# Demo: LRU Cache for Chunk Management

## Description

This demo showcases the **Least Recently Used (LRU) Cache** implementation for efficient chunk management in voxel-based terrain systems. The demonstration provides an interactive visualization of cache behavior, including:

- Real-time LRU ordering visualization with color gradients
- Cache statistics tracking (hit rate, hits, misses, evictions)
- Configurable cache capacity (5-50 chunks)
- Automatic and manual chunk access simulation
- Eviction callbacks with logging

The demo uses a lightweight `MockChunk` class to simulate chunk caching without the overhead of actual mesh generation, allowing you to focus on understanding the cache behavior and performance characteristics.

## Prérequis

- **Unity Version**: 6000.2.12f1 or later
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Packages Required**:
  - Unity UI (com.unity.ugui) - Built-in
  - Unity Mathematics (com.unity.mathematics)
- **Configuration**:
  - Ensure URP is properly configured in your project
  - No special project settings required

## Installation

### Step 1: Verify Package Dependencies

The demo requires the `voxel-streaming` package which contains the LRU Cache implementation.

1. Navigate to `Assets/lib/voxel-streaming/`
2. Verify the following files exist:
   - `Runtime/Cache/LRUCache.cs`
   - `Runtime/Cache/ChunkCache.cs`
   - `Runtime/Cache/CacheStatistics.cs`
   - `Runtime/Cache/IEvictionHandler.cs`

### Step 2: Verify Demo Files

Ensure all demo files are present in `Assets/demos/issue-5-lru-cache/`:

```
Assets/demos/issue-5-lru-cache/
├── Scenes/
│   └── DemoScene.unity
├── Scripts/
│   └── DemoController.cs
├── Prefabs/
│   └── ChunkCacheItem.prefab
└── README.md (this file)
```

### Step 3: Setup UI in Unity Editor (Automatic - One Click)

The demo includes an automated setup tool that creates the complete UI hierarchy with one click:

1. Open `Assets/demos/issue-5-lru-cache/Scenes/DemoScene.unity`
2. In Unity menu bar, go to: **Tools → LRU Cache Demo → Setup Demo Scene**
3. Click "OK" in the confirmation dialog
4. The tool will automatically:
   - Create the complete Canvas with proper settings
   - Create Stats Panel (upper left) with all text components and hit rate bar
   - Create Control Panel (lower left) with sliders, buttons, and toggles
   - Create Cache Visualization Panel (right side) with scroll view
   - Assign ALL references to the DemoController automatically
   - Save the scene

**Note**: If you run the setup tool on an already-configured scene, it will ask if you want to recreate the UI. This is useful if you want to reset to the default UI layout.

**Manual Setup (Alternative)**: If you prefer to create the UI manually or need to customize it, see the detailed instructions in the [Manual Setup Guide](#manual-setup-guide) at the end of this document.

## Utilisation

### Step 1: Ouvrir la scène

1. Navigate to `Assets/demos/issue-5-lru-cache/Scenes/DemoScene.unity`
2. Double-click to open the scene in Unity Editor

### Step 2: Configuration de la scène

1. **Verify DemoController Setup**:
   - Select the `DemoController` GameObject in the Hierarchy
   - In the Inspector, verify all serialized fields are assigned:
     - All Text UI references (countText, hitRateText, etc.)
     - All Button references (simulateButton, clearButton, resetStatsButton)
     - All Slider references (capacitySlider, hitRateBar, autoSimulateSpeedSlider)
     - Toggle reference (autoSimulateToggle)
     - Content parent reference (cacheContentParent)
     - Prefab reference (chunkCacheItemPrefab)

2. **Adjust Configuration** (optional):
   - `Min Capacity`: Minimum cache capacity (default: 5)
   - `Max Capacity`: Maximum cache capacity (default: 50)
   - `Default Capacity`: Initial cache capacity (default: 20)
   - `Chunk Range Min/Max`: Range for random chunk coordinates (default: -10 to 10)
   - `Recent Color`: Color for most recently used items (default: Green)
   - `Old Color`: Color for least recently used items (default: Red)

### Step 3: Lancer la démonstration

1. **Press Play** in Unity Editor
2. The demo will start with:
   - Cache capacity set to 20 chunks
   - Empty cache (Count: 0/20)
   - Statistics at zero

3. **Manual Simulation**:
   - Click "Simulate Random Access" button
   - Each click generates a random ChunkCoord
   - Cache attempts to retrieve the chunk (TryGet)
   - On cache miss: creates new MockChunk and adds to cache
   - On cache hit: increments access count
   - Watch the visualization panel update in real-time

4. **Automatic Simulation**:
   - Enable "Auto Simulate" toggle
   - Adjust speed with the speed slider (0.1 to 2.0 seconds)
   - Operations/sec displayed automatically
   - Cache will continuously simulate random accesses
   - Disable toggle to stop

5. **Adjust Cache Capacity**:
   - Use the Capacity Slider (5-50)
   - Cache will reinitialize with new capacity
   - All data and statistics are cleared
   - Current capacity shown as "Count: X/Capacity"

6. **Clear Cache**:
   - Click "Clear Cache" button
   - Removes all cached chunks
   - Statistics remain unchanged
   - Visualization panel clears

7. **Reset Statistics**:
   - Click "Reset Statistics" button
   - Resets Hits, Misses, Evictions to 0
   - Cache contents remain unchanged
   - Hit Rate resets to 0%

### Controls Summary

| Control | Action |
|---------|--------|
| **Capacity Slider** | Change cache capacity (5-50) |
| **Simulate Button** | Perform single random access |
| **Clear Cache** | Remove all cached chunks |
| **Reset Statistics** | Reset performance counters |
| **Auto Simulate Toggle** | Enable/disable automatic simulation |
| **Speed Slider** | Adjust auto-simulation speed |

### Understanding the Visualization

**Cache Visualization Panel** (right side):
- Each item represents a cached chunk
- **Ordered from top to bottom**: Most recent → Least recent
- **Color gradient**:
  - Green: Recently accessed (safe from eviction)
  - Yellow-Orange: Mid-range
  - Red: Least recently used (next to be evicted)
- **Text format**: "Chunk (X, Y, Z)" showing coordinates

**Statistics Panel** (upper left):
- **Count**: Current items / Capacity
- **Hit Rate**: Percentage of successful cache retrievals
- **Hits**: Number of cache hits (found in cache)
- **Misses**: Number of cache misses (not in cache)
- **Evictions**: Number of items removed due to capacity
- **Hit Rate Bar**: Visual representation of hit rate (0-100%)

## Validation

### Ce que vous devriez voir:

#### Initial State
- Count: 0/20 (empty cache)
- Hit Rate: 0%
- All statistics at 0
- Empty visualization panel

#### After Manual Simulations
- First simulation: Cache MISS (new chunk added)
- Count increases: 1/20, 2/20, etc.
- Visualization shows chunks in LRU order
- Color gradient from green (top) to red (bottom)
- Console logs cache operations

#### Cache Behavior
- **Cache Miss**: New chunk created, added to cache (green at top)
- **Cache Hit**: Existing chunk moved to top, color updates
- **Eviction**: When capacity reached, red item (bottom) evicted
- **LRU Ordering**: Recently accessed items move to top (green)

#### Statistics Progression
- Misses increase when accessing new chunks
- Hits increase when re-accessing cached chunks
- Evictions increase when cache is full
- Hit Rate improves with repeated access patterns
- Hit Rate Bar fills proportionally

#### Auto-Simulation
- Continuous cache operations
- Statistics update rapidly
- Visualization refreshes each operation
- Console logs each access/eviction
- Performance remains smooth (60 FPS)

### Expected Console Output

```
[LRU Cache Demo] Cache initialized with capacity: 20
[LRU Cache Demo] Cache MISS: Added Chunk (5, 0, 3) [Accessed: 1]
[LRU Cache Demo] Cache MISS: Added Chunk (-2, 1, 7) [Accessed: 1]
[LRU Cache Demo] Cache HIT: Chunk (5, 0, 3) [Accessed: 2]
[LRU Cache Demo] Evicted: Chunk (-8, 2, -5) [Accessed: 1]
[LRU Cache Demo] Cache MISS with EVICTION: Added Chunk (9, -1, 4) [Accessed: 1]
[LRU Cache Demo] Cache cleared
[LRU Cache Demo] Statistics reset
```

### Performance Expectations

- **Frame Rate**: Consistent 60 FPS or higher
- **Memory**: Minimal allocations per operation
- **Response Time**: Instant UI updates
- **Auto-Simulation**: Smooth at 2 ops/sec (max speed)

## Problèmes connus

### Limitations
- MockChunk is a simplified representation (no actual mesh data)
- Chunk coordinates are randomly generated (no spatial coherence)
- Demo is single-threaded (no Job System usage shown)
- UI updates every frame during auto-simulation (minor overhead)

### Known Issues
- **First Simulation**: Always results in cache miss (expected behavior)
- **High Speed Auto-Simulation**: Console logs may slow down Unity Editor (use Console collapse/clear)
- **Scene Setup Required**: UI must be manually configured in Unity Editor (cannot be fully automated in .unity file)

### Workarounds
- If performance drops during auto-simulation: reduce speed or disable console logging
- If UI references are missing: follow Setup UI instructions carefully
- If prefab is missing: ensure ChunkCacheItem.prefab exists in Prefabs folder

## Notes techniques

### Architecture

**DemoController.cs** (340 lines):
- MonoBehaviour orchestrating the demo
- Implements cache operations with MockChunk
- Manages UI updates and visualization
- Uses DemoEvictionHandler for logging

**MockChunk Class**:
- Lightweight chunk representation
- Tracks ChunkCoord and AccessCount
- No mesh data (performance optimization for demo)

**DemoEvictionHandler**:
- Implements IEvictionHandler<ChunkCoord, MockChunk>
- Logs evictions to Unity Console
- Demonstrates eviction callback pattern

### Performance Considerations

**O(1) Operations**:
- TryGet: Dictionary lookup + LinkedList move
- Put: Dictionary insert + LinkedList add
- Evict: LinkedList remove + Dictionary remove

**Memory**:
- ChunkCache<MockChunk>: O(capacity)
- Visualization items: O(capacity) GameObjects
- No per-frame allocations in cache operations

**Threading**:
- LRUCache is thread-safe (uses lock)
- Demo runs on main thread only
- UI updates are immediate (no async)

### Integration with Voxel Engine

This demo uses the production LRU Cache from `voxel-streaming` package:
- **Package**: `TimeSurvivor.Voxel.Streaming`
- **Classes**: `LRUCache<TKey, TValue>`, `ChunkCache<TChunk>`
- **Interfaces**: `IEvictionHandler<TKey, TValue>`
- **Data**: `CacheStatistics`, `ChunkCoord` (from voxel-core)

To integrate into your voxel terrain system:
1. Replace MockChunk with your actual chunk type (e.g., TerrainChunk)
2. Implement IEvictionHandler for chunk cleanup/disposal
3. Use ChunkCache in your ChunkManager or streaming system
4. Monitor CacheStatistics for performance optimization
5. Tune capacity based on memory budget and access patterns

### Profiler Markers

LRUCache includes profiler markers for performance monitoring:
- `LRUCache.Get`: TryGet operation timing
- `LRUCache.Put`: Put operation timing
- `LRUCache.Evict`: Eviction timing

Use Unity Profiler to analyze cache performance in your game.

### Code Quality

- **SOLID Principles**: Single Responsibility (DemoController, MockChunk, DemoEvictionHandler)
- **Clean Code**: Descriptive names, XML documentation, regions for organization
- **Error Handling**: Null checks, reference validation in Awake
- **Resource Management**: Proper cleanup in OnDestroy, event unsubscription
- **Performance**: Cached references, object pooling for visualization items

## Additional Resources

- **Source Code**: `Assets/demos/issue-5-lru-cache/Scripts/DemoController.cs`
- **LRU Cache Implementation**: `Assets/lib/voxel-streaming/Runtime/Cache/LRUCache.cs`
- **Chunk Cache Wrapper**: `Assets/lib/voxel-streaming/Runtime/Cache/ChunkCache.cs`
- **Architecture Decision Record**: See project documentation for ADR on LRU Cache design

## Support

If you encounter issues:
1. Verify all UI references are assigned in DemoController Inspector
2. Check Console for error messages
3. Ensure voxel-streaming package is properly installed
4. Verify Unity version compatibility (6000.2.12f1+)

For questions or bug reports, refer to the project issue tracker (Issue #5: LRU Cache Implementation).

---

## Manual Setup Guide

If you prefer to manually create the UI or need to customize it, follow these detailed instructions:

### Manual Canvas Setup

1. **Create Canvas**:
   - Right-click in Hierarchy → UI → Canvas
   - Set Canvas Scaler to "Scale With Screen Size"
   - Reference Resolution: 1920x1080

2. **Create Stats Panel** (Upper Left):
   - Create Panel: Right-click Canvas → UI → Panel
   - Name: "StatsPanel"
   - RectTransform: Anchor to top-left
     - Position: X=150, Y=-100
     - Width: 280, Height: 180
   - Add Text components (children of StatsPanel):
     - `CountText` - "Count: 0/20"
     - `HitRateText` - "Hit Rate: 0%"
     - `HitsText` - "Hits: 0"
     - `MissesText` - "Misses: 0"
     - `EvictionsText` - "Evictions: 0"
   - Add Slider: `HitRateBar` (non-interactable, visual only)

3. **Create Control Panel** (Lower Left):
   - Create Panel: Right-click Canvas → UI → Panel
   - Name: "ControlPanel"
   - RectTransform: Anchor to bottom-left
     - Position: X=150, Y=150
     - Width: 280, Height: 280
   - Add UI elements (children of ControlPanel):
     - `CapacitySlider` - Slider (Min: 5, Max: 50, Value: 20)
     - `CapacityValueText` - Text showing slider value
     - `SimulateButton` - Button ("Simulate Random Access")
     - `ClearButton` - Button ("Clear Cache")
     - `ResetStatsButton` - Button ("Reset Statistics")
     - `AutoSimulateToggle` - Toggle ("Auto Simulate")
     - `AutoSimulateSpeedSlider` - Slider (Min: 0.1, Max: 2.0, Value: 0.5)
     - `AutoSimulateSpeedText` - Text showing operations/sec

4. **Create Cache Visualization Panel** (Right Side):
   - Create Panel: Right-click Canvas → UI → Panel
   - Name: "VisualizationPanel"
   - RectTransform: Anchor to right side
     - Position: X=-200, Y=0
     - Width: 380, Height: 900
   - Add ScrollView (child of VisualizationPanel):
     - Add Vertical Layout Group component
     - Content Size Fitter: Vertical Fit = Preferred Size
     - Note the Content Transform - this will be assigned to DemoController

5. **Assign References to DemoController**:
   - Select the `DemoController` GameObject in Hierarchy
   - In Inspector, drag and drop all UI elements to their corresponding fields:
     - Stats Panel: countText, hitRateText, hitsText, missesText, evictionsText, hitRateBar
     - Control Panel: capacitySlider, capacityValueText, simulateButton, clearButton, resetStatsButton, autoSimulateToggle, autoSimulateSpeedSlider, autoSimulateSpeedText
     - Visualization: cacheContentParent (Content transform from ScrollView)
     - Prefab: chunkCacheItemPrefab (drag from Prefabs folder)
