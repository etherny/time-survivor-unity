using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Streaming;

namespace TimeSurvivor.Demos.LRUCache
{
    /// <summary>
    /// Interactive demo controller for visualizing LRU Cache behavior.
    /// Simulates chunk caching with configurable capacity, random access patterns,
    /// and real-time statistics visualization.
    /// </summary>
    public class DemoController : MonoBehaviour
    {
        #region Serialized Fields - UI References

        [Header("Stats Panel")]
        [SerializeField] private Text countText;
        [SerializeField] private Text hitRateText;
        [SerializeField] private Text hitsText;
        [SerializeField] private Text missesText;
        [SerializeField] private Text evictionsText;
        [SerializeField] private Slider hitRateBar;

        [Header("Control Panel")]
        [SerializeField] private Slider capacitySlider;
        [SerializeField] private Text capacityValueText;
        [SerializeField] private Button simulateButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button resetStatsButton;
        [SerializeField] private Toggle autoSimulateToggle;
        [SerializeField] private Slider autoSimulateSpeedSlider;
        [SerializeField] private Text autoSimulateSpeedText;

        [Header("Cache Visualization")]
        [SerializeField] private Transform cacheContentParent;
        [SerializeField] private GameObject chunkCacheItemPrefab;

        [Header("Configuration")]
        [SerializeField] private int minCapacity = 5;
        [SerializeField] private int maxCapacity = 50;
        [SerializeField] private int defaultCapacity = 20;
        [SerializeField] private int chunkRangeMin = -10;
        [SerializeField] private int chunkRangeMax = 10;
        [SerializeField] private Color recentColor = Color.green;
        [SerializeField] private Color oldColor = Color.red;

        #endregion

        #region Private Fields

        private ChunkCache<MockChunk> _chunkCache;
        private DemoEvictionHandler _evictionHandler;
        private List<GameObject> _visualizationItems = new List<GameObject>();
        private float _autoSimulateTimer;
        private float _autoSimulateInterval = 0.5f;
        private System.Random _random;

        #endregion

        #region Inner Classes

        /// <summary>
        /// Mock chunk class for demonstration purposes.
        /// Represents a lightweight chunk without actual mesh data.
        /// </summary>
        private class MockChunk
        {
            public ChunkCoord Coord { get; set; }
            public int AccessCount { get; set; }

            public MockChunk(ChunkCoord coord)
            {
                Coord = coord;
                AccessCount = 0;
            }

            public override string ToString()
            {
                return $"Chunk ({Coord.X}, {Coord.Y}, {Coord.Z}) [Accessed: {AccessCount}]";
            }
        }

        /// <summary>
        /// Eviction handler implementation for logging and tracking evictions.
        /// </summary>
        private class DemoEvictionHandler : IEvictionHandler<ChunkCoord, MockChunk>
        {
            public void OnEvict(ChunkCoord key, MockChunk value)
            {
                Debug.Log($"[LRU Cache Demo] Evicted: {value}");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            _random = new System.Random();
            _evictionHandler = new DemoEvictionHandler();
        }

        private void Start()
        {
            InitializeUI();
            InitializeCache(defaultCapacity);
            UpdateAllUI();
        }

        private void Update()
        {
            if (autoSimulateToggle != null && autoSimulateToggle.isOn)
            {
                _autoSimulateTimer += Time.deltaTime;
                if (_autoSimulateTimer >= _autoSimulateInterval)
                {
                    _autoSimulateTimer = 0f;
                    SimulateRandomAccess();
                }
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Validates that all required UI references are assigned.
        /// Logs errors for missing references.
        /// </summary>
        private void ValidateReferences()
        {
            if (countText == null) Debug.LogError("[DemoController] countText is not assigned!");
            if (hitRateText == null) Debug.LogError("[DemoController] hitRateText is not assigned!");
            if (hitsText == null) Debug.LogError("[DemoController] hitsText is not assigned!");
            if (missesText == null) Debug.LogError("[DemoController] missesText is not assigned!");
            if (evictionsText == null) Debug.LogError("[DemoController] evictionsText is not assigned!");
            if (hitRateBar == null) Debug.LogError("[DemoController] hitRateBar is not assigned!");
            if (capacitySlider == null) Debug.LogError("[DemoController] capacitySlider is not assigned!");
            if (capacityValueText == null) Debug.LogError("[DemoController] capacityValueText is not assigned!");
            if (simulateButton == null) Debug.LogError("[DemoController] simulateButton is not assigned!");
            if (clearButton == null) Debug.LogError("[DemoController] clearButton is not assigned!");
            if (resetStatsButton == null) Debug.LogError("[DemoController] resetStatsButton is not assigned!");
            if (autoSimulateToggle == null) Debug.LogError("[DemoController] autoSimulateToggle is not assigned!");
            if (autoSimulateSpeedSlider == null) Debug.LogError("[DemoController] autoSimulateSpeedSlider is not assigned!");
            if (autoSimulateSpeedText == null) Debug.LogError("[DemoController] autoSimulateSpeedText is not assigned!");
            if (cacheContentParent == null) Debug.LogError("[DemoController] cacheContentParent is not assigned!");
            if (chunkCacheItemPrefab == null) Debug.LogError("[DemoController] chunkCacheItemPrefab is not assigned!");
        }

        /// <summary>
        /// Initializes UI controls with default values and registers event listeners.
        /// </summary>
        private void InitializeUI()
        {
            // Setup capacity slider
            if (capacitySlider != null)
            {
                capacitySlider.minValue = minCapacity;
                capacitySlider.maxValue = maxCapacity;
                capacitySlider.value = defaultCapacity;
                capacitySlider.wholeNumbers = true;
                capacitySlider.onValueChanged.AddListener(OnCapacityChanged);
            }

            // Setup buttons
            if (simulateButton != null)
            {
                simulateButton.onClick.AddListener(OnSimulateClicked);
            }

            if (clearButton != null)
            {
                clearButton.onClick.AddListener(OnClearClicked);
            }

            if (resetStatsButton != null)
            {
                resetStatsButton.onClick.AddListener(OnResetStatsClicked);
            }

            // Setup auto-simulate speed slider
            if (autoSimulateSpeedSlider != null)
            {
                autoSimulateSpeedSlider.minValue = 0.1f;
                autoSimulateSpeedSlider.maxValue = 2f;
                autoSimulateSpeedSlider.value = 0.5f;
                autoSimulateSpeedSlider.onValueChanged.AddListener(OnAutoSimulateSpeedChanged);
            }

            // Setup hit rate bar (non-interactive)
            if (hitRateBar != null)
            {
                hitRateBar.minValue = 0f;
                hitRateBar.maxValue = 1f;
                hitRateBar.interactable = false;
            }
        }

        /// <summary>
        /// Initializes or re-initializes the chunk cache with the specified capacity.
        /// Clears existing cache and resets statistics.
        /// </summary>
        /// <param name="capacity">Maximum number of chunks to cache</param>
        private void InitializeCache(int capacity)
        {
            _chunkCache = new ChunkCache<MockChunk>(capacity, _evictionHandler);
            Debug.Log($"[LRU Cache Demo] Cache initialized with capacity: {capacity}");
        }

        #endregion

        #region Cache Operations

        /// <summary>
        /// Simulates a random chunk access operation.
        /// Generates a random ChunkCoord, attempts to retrieve from cache.
        /// On cache miss, creates a new MockChunk and adds it to the cache.
        /// Updates UI after each operation.
        /// </summary>
        private void SimulateRandomAccess()
        {
            // Generate random chunk coordinate
            int x = _random.Next(chunkRangeMin, chunkRangeMax + 1);
            int y = _random.Next(chunkRangeMin, chunkRangeMax + 1);
            int z = _random.Next(chunkRangeMin, chunkRangeMax + 1);
            ChunkCoord coord = new ChunkCoord(x, y, z);

            // Try to get from cache
            if (_chunkCache.TryGetChunk(coord, out MockChunk chunk))
            {
                // Cache hit - increment access count
                chunk.AccessCount++;
                Debug.Log($"[LRU Cache Demo] Cache HIT: {chunk}");
            }
            else
            {
                // Cache miss - create new chunk
                chunk = new MockChunk(coord) { AccessCount = 1 };
                var evicted = _chunkCache.PutChunk(coord, chunk);

                if (evicted != null)
                {
                    Debug.Log($"[LRU Cache Demo] Cache MISS with EVICTION: Added {chunk}");
                }
                else
                {
                    Debug.Log($"[LRU Cache Demo] Cache MISS: Added {chunk}");
                }
            }

            UpdateAllUI();
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Updates all UI elements (statistics and visualization).
        /// </summary>
        private void UpdateAllUI()
        {
            UpdateStatsUI();
            UpdateCacheVisualization();
        }

        /// <summary>
        /// Updates the statistics panel with current cache metrics.
        /// Displays count, hit rate, hits, misses, and evictions.
        /// </summary>
        private void UpdateStatsUI()
        {
            if (_chunkCache == null) return;

            var stats = _chunkCache.Statistics;

            if (countText != null)
            {
                countText.text = $"Count: {_chunkCache.Count}/{_chunkCache.Capacity}";
            }

            if (hitRateText != null)
            {
                hitRateText.text = $"Hit Rate: {stats.HitRate:P1}";
            }

            if (hitsText != null)
            {
                hitsText.text = $"Hits: {stats.Hits}";
            }

            if (missesText != null)
            {
                missesText.text = $"Misses: {stats.Misses}";
            }

            if (evictionsText != null)
            {
                evictionsText.text = $"Evictions: {stats.Evictions}";
            }

            if (hitRateBar != null)
            {
                hitRateBar.value = stats.HitRate;
            }
        }

        /// <summary>
        /// Updates the cache visualization panel.
        /// Displays all cached chunks in LRU order with color gradient.
        /// Green = most recently used, Red = least recently used.
        /// </summary>
        private void UpdateCacheVisualization()
        {
            if (_chunkCache == null || cacheContentParent == null || chunkCacheItemPrefab == null)
                return;

            // Clear existing visualization items
            foreach (var item in _visualizationItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _visualizationItems.Clear();

            // Get ordered keys (most recent first)
            var orderedKeys = _chunkCache.OrderedKeys;
            int index = 0;
            int totalCount = _chunkCache.Count;

            foreach (var coord in orderedKeys)
            {
                // Instantiate prefab
                GameObject itemObj = Instantiate(chunkCacheItemPrefab, cacheContentParent);
                _visualizationItems.Add(itemObj);

                // Calculate color gradient (green to red)
                float t = totalCount > 1 ? (float)index / (totalCount - 1) : 0f;
                Color itemColor = Color.Lerp(recentColor, oldColor, t);

                // Update UI components
                Image backgroundImage = itemObj.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    backgroundImage.color = itemColor;
                }

                Text labelText = itemObj.GetComponentInChildren<Text>();
                if (labelText != null)
                {
                    labelText.text = $"Chunk ({coord.X}, {coord.Y}, {coord.Z})";
                }

                index++;
            }
        }

        #endregion

        #region UI Callbacks

        /// <summary>
        /// Called when the capacity slider value changes.
        /// Re-initializes the cache with the new capacity.
        /// </summary>
        /// <param name="newCapacity">The new cache capacity</param>
        private void OnCapacityChanged(float newCapacity)
        {
            int capacity = Mathf.RoundToInt(newCapacity);

            if (capacityValueText != null)
            {
                capacityValueText.text = capacity.ToString();
            }

            InitializeCache(capacity);
            UpdateAllUI();
        }

        /// <summary>
        /// Called when the Simulate button is clicked.
        /// Performs a single random access simulation.
        /// </summary>
        private void OnSimulateClicked()
        {
            SimulateRandomAccess();
        }

        /// <summary>
        /// Called when the Clear button is clicked.
        /// Clears all cached chunks.
        /// </summary>
        private void OnClearClicked()
        {
            if (_chunkCache != null)
            {
                _chunkCache.Clear();
                Debug.Log("[LRU Cache Demo] Cache cleared");
                UpdateAllUI();
            }
        }

        /// <summary>
        /// Called when the Reset Stats button is clicked.
        /// Resets cache statistics (hits, misses, evictions) to zero.
        /// </summary>
        private void OnResetStatsClicked()
        {
            if (_chunkCache != null)
            {
                _chunkCache.ResetStatistics();
                Debug.Log("[LRU Cache Demo] Statistics reset");
                UpdateAllUI();
            }
        }

        /// <summary>
        /// Called when the auto-simulate speed slider value changes.
        /// Adjusts the interval between automatic simulations.
        /// </summary>
        /// <param name="newSpeed">New speed value (lower = faster)</param>
        private void OnAutoSimulateSpeedChanged(float newSpeed)
        {
            _autoSimulateInterval = newSpeed;

            if (autoSimulateSpeedText != null)
            {
                autoSimulateSpeedText.text = $"{(1f / newSpeed):F1} ops/sec";
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Unregister event listeners
            if (capacitySlider != null)
            {
                capacitySlider.onValueChanged.RemoveListener(OnCapacityChanged);
            }

            if (simulateButton != null)
            {
                simulateButton.onClick.RemoveListener(OnSimulateClicked);
            }

            if (clearButton != null)
            {
                clearButton.onClick.RemoveListener(OnClearClicked);
            }

            if (resetStatsButton != null)
            {
                resetStatsButton.onClick.RemoveListener(OnResetStatsClicked);
            }

            if (autoSimulateSpeedSlider != null)
            {
                autoSimulateSpeedSlider.onValueChanged.RemoveListener(OnAutoSimulateSpeedChanged);
            }

            // Clear visualization items
            foreach (var item in _visualizationItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _visualizationItems.Clear();
        }

        #endregion
    }
}
