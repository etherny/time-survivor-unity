# Voxel Streaming Package

## Overview

The `voxel-streaming` package provides a high-performance, thread-safe LRU (Least Recently Used) cache system designed for managing terrain chunks in voxel-based games. It includes statistics tracking, profiler integration, and configurable eviction handling.

## Features

- **Thread-Safe O(1) Operations**: All cache operations (Get, Put, Remove, Contains) execute in constant time with full thread safety
- **Automatic LRU Eviction**: When cache reaches capacity, automatically evicts least recently used items
- **Performance Profiling**: Built-in Unity Profiler markers for monitoring cache performance
- **Statistics Tracking**: Real-time cache hit/miss tracking and performance metrics
- **Eviction Callbacks**: Optional handler interface for cleanup when items are evicted
- **ScriptableObject Configuration**: Designer-friendly cache configuration without code changes
- **Chunk Specialization**: Domain-specific `ChunkCache` wrapper for voxel terrain chunks

## Architecture

### Core Components

#### `LRUCache<TKey, TValue>`
Generic thread-safe LRU cache implementation with O(1) operations.

**Key Features:**
- Dictionary + Doubly-Linked List for O(1) access and eviction
- Thread-safe with fine-grained locking
- Unity Profiler markers: `LRUCache.Get`, `LRUCache.Put`, `LRUCache.Evict`
- Statistics tracking (hits, misses, evictions, hit rate)
- Optional eviction handler callbacks

**Performance Targets:**
- Get: < 0.05ms
- Put: < 0.1ms
- Evict: < 0.05ms
- Hit Rate: ≥ 80% (with realistic access patterns)

#### `ChunkCache<TChunk>`
Specialized wrapper for managing terrain chunks with `ChunkCoord` keys.

**Benefits:**
- Domain-specific API (`TryGetChunk`, `PutChunk`, `ContainsChunk`)
- Type safety for chunk operations
- Cleaner code in chunk management systems

#### `CacheStatistics`
Performance metrics struct for monitoring cache efficiency.

**Metrics:**
- `Hits`: Number of successful cache lookups
- `Misses`: Number of failed cache lookups
- `Evictions`: Number of items evicted
- `HitRate`: Percentage of successful lookups (0.0 to 1.0)
- `TotalAccesses`: Total Get operations

#### `IEvictionHandler<TKey, TValue>`
Interface for handling eviction callbacks.

**Use Cases:**
- Saving chunk data to disk before eviction
- Cleaning up Unity GameObjects/Resources
- Logging eviction events for debugging
- Updating UI or state machines

#### `StreamingConfiguration`
ScriptableObject for configuring cache behavior.

**Settings:**
- `ChunkCacheCapacity`: Maximum chunks in memory (16-1024)
- `EnableStatistics`: Toggle statistics tracking
- `StatisticsLogInterval`: Auto-logging interval (seconds)
- `TargetHitRate`: Performance warning threshold

## Installation

This package is part of the TimeSurvivor voxel engine located at:
```
Assets/lib/voxel-streaming/
```

### Dependencies
- `TimeSurvivor.Voxel.Core` (for ChunkCoord)
- `Unity.Mathematics`
- `Unity.Collections`
- `Unity.Profiling.Core`

### Assembly Definition
Add `TimeSurvivor.Voxel.Streaming` to your assembly references.

## Usage Examples

### Basic LRU Cache

```csharp
using TimeSurvivor.Voxel.Streaming;

// Create cache with capacity of 100 items
var cache = new LRUCache<int, string>(100);

// Add items
cache.Put(1, "value_one");
cache.Put(2, "value_two");

// Retrieve items
if (cache.TryGet(1, out string value))
{
    Debug.Log($"Cache hit: {value}");
}

// Check existence
if (cache.Contains(2))
{
    Debug.Log("Key 2 exists in cache");
}

// View statistics
var stats = cache.Statistics;
Debug.Log($"Hit Rate: {stats.HitRate:P2}");
```

### Chunk Cache with Eviction Handler

```csharp
using TimeSurvivor.Voxel.Streaming;
using TimeSurvivor.Voxel.Core;
using UnityEngine;

// Implement eviction handler
public class ChunkEvictionHandler : IEvictionHandler<ChunkCoord, TerrainChunk>
{
    public void OnEvict(ChunkCoord coord, TerrainChunk chunk)
    {
        // Save chunk data to disk
        SaveChunkToDisk(coord, chunk);

        // Destroy GameObject
        if (chunk.GameObject != null)
        {
            Object.Destroy(chunk.GameObject);
        }

        Debug.Log($"Evicted chunk at {coord}");
    }

    private void SaveChunkToDisk(ChunkCoord coord, TerrainChunk chunk)
    {
        // Implementation here
    }
}

// Create chunk cache with eviction handling
var evictionHandler = new ChunkEvictionHandler();
var chunkCache = new ChunkCache<TerrainChunk>(256, evictionHandler);

// Use chunk cache
var coord = new ChunkCoord(0, 0, 0);
chunkCache.PutChunk(coord, myTerrainChunk);

if (chunkCache.TryGetChunk(coord, out var chunk))
{
    Debug.Log($"Found chunk at {coord}");
}
```

### Using StreamingConfiguration

```csharp
using TimeSurvivor.Voxel.Streaming;
using UnityEngine;

public class ChunkStreamer : MonoBehaviour
{
    [SerializeField] private StreamingConfiguration _config;

    private ChunkCache<TerrainChunk> _cache;

    private void Awake()
    {
        // Create cache from configuration
        _cache = new ChunkCache<TerrainChunk>(
            _config.ChunkCacheCapacity,
            new ChunkEvictionHandler()
        );

        if (_config.EnableStatistics && _config.StatisticsLogInterval > 0)
        {
            InvokeRepeating(nameof(LogStatistics),
                _config.StatisticsLogInterval,
                _config.StatisticsLogInterval);
        }
    }

    private void LogStatistics()
    {
        var stats = _cache.Statistics;
        Debug.Log($"Cache Stats: {stats}");

        if (stats.HitRate < _config.TargetHitRate)
        {
            Debug.LogWarning($"Cache hit rate {stats.HitRate:P2} is below target {_config.TargetHitRate:P2}. " +
                           $"Consider increasing cache capacity.");
        }
    }
}
```

### Thread-Safe Usage

```csharp
using System.Threading.Tasks;
using TimeSurvivor.Voxel.Streaming;

// Cache is thread-safe, can be accessed from multiple threads
var cache = new LRUCache<int, string>(100);

// Producer thread
Task.Run(() =>
{
    for (int i = 0; i < 1000; i++)
    {
        cache.Put(i, $"value_{i}");
    }
});

// Consumer thread
Task.Run(() =>
{
    for (int i = 0; i < 1000; i++)
    {
        if (cache.TryGet(i, out var value))
        {
            ProcessValue(value);
        }
    }
});
```

## Performance Considerations

### Cache Capacity Sizing
- **Too Small**: High eviction rate, poor hit rate, frequent loading
- **Too Large**: Excessive memory usage, potential performance issues
- **Recommended**: Start with 256 chunks, monitor hit rate
- **Formula**: `Capacity ≈ (ViewDistance / ChunkSize)³ × 1.5`

### Memory Usage
- Each cached chunk includes voxel data, mesh, and GameObject
- Typical chunk: ~100KB-500KB depending on voxel count and mesh complexity
- Example: 256 chunks × 250KB = ~64MB

### Profiling
Use Unity Profiler to monitor:
- `LRUCache.Get` - Should be < 0.05ms
- `LRUCache.Put` - Should be < 0.1ms
- `LRUCache.Evict` - Should be < 0.05ms

### Optimization Tips
1. **Monitor Hit Rate**: Aim for ≥80% to minimize loading overhead
2. **Tune Capacity**: Increase if hit rate is too low
3. **Use Statistics**: Track performance in real scenarios
4. **Profile Eviction**: Ensure cleanup in `OnEvict` is fast
5. **Avoid Blocking**: Don't perform expensive I/O in `OnEvict`

## Testing

The package includes comprehensive unit tests covering:
- LRU eviction behavior
- O(1) time complexity verification
- Hit rate targets (≥80%)
- Thread safety
- Eviction handler callbacks
- Statistics tracking
- ChunkCache wrapper functionality

Run tests via Unity Test Runner or:
```bash
make test
```

## API Reference

### LRUCache<TKey, TValue>

#### Constructor
```csharp
public LRUCache(int capacity, IEvictionHandler<TKey, TValue> evictionHandler = null)
```

#### Methods
- `bool TryGet(TKey key, out TValue value)` - O(1) lookup
- `TValue Put(TKey key, TValue value)` - O(1) insert/update
- `bool Contains(TKey key)` - O(1) existence check
- `bool Remove(TKey key)` - O(1) removal
- `void Clear()` - Remove all items
- `void ResetStatistics()` - Clear statistics counters

#### Properties
- `int Count` - Current item count
- `int Capacity` - Maximum capacity
- `CacheStatistics Statistics` - Performance metrics
- `IEnumerable<TKey> Keys` - All cached keys
- `IEnumerable<TValue> Values` - All cached values (ordered by recency)

### ChunkCache<TChunk>

#### Methods
- `bool TryGetChunk(ChunkCoord coord, out TChunk chunk)`
- `TChunk PutChunk(ChunkCoord coord, TChunk chunk)`
- `bool ContainsChunk(ChunkCoord coord)`
- `bool RemoveChunk(ChunkCoord coord)`
- `void Clear()`
- `void ResetStatistics()`

#### Properties
- `int Count`
- `int Capacity`
- `CacheStatistics Statistics`

## License

Part of the TimeSurvivor game project.
