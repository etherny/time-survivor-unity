using UnityEngine;

namespace TimeSurvivor.Voxel.Streaming
{
    /// <summary>
    /// ScriptableObject configuration for voxel terrain streaming system.
    /// Allows designers to configure cache capacity and other streaming parameters
    /// without modifying code.
    /// </summary>
    [CreateAssetMenu(fileName = "StreamingConfiguration", menuName = "Voxel/Streaming Configuration")]
    public class StreamingConfiguration : ScriptableObject
    {
        [Header("Cache Settings")]
        [Tooltip("Maximum number of chunks to keep in memory. Higher values use more RAM but reduce loading hitches.")]
        [SerializeField]
        [Range(16, 1024)]
        private int _chunkCacheCapacity = 256;

        [Header("Statistics")]
        [Tooltip("Enable cache statistics tracking. Adds minimal overhead but useful for performance tuning.")]
        [SerializeField]
        private bool _enableStatistics = true;

        [Tooltip("Log cache statistics to console at this interval (seconds). Set to 0 to disable.")]
        [SerializeField]
        [Range(0f, 60f)]
        private float _statisticsLogInterval = 10f;

        [Header("Performance Targets")]
        [Tooltip("Target cache hit rate (0.0 to 1.0). Warning logged if actual hit rate falls below this.")]
        [SerializeField]
        [Range(0.5f, 1.0f)]
        private float _targetHitRate = 0.8f;

        /// <summary>
        /// Gets the maximum number of chunks to keep in the cache.
        /// </summary>
        public int ChunkCacheCapacity => _chunkCacheCapacity;

        /// <summary>
        /// Gets whether cache statistics tracking is enabled.
        /// </summary>
        public bool EnableStatistics => _enableStatistics;

        /// <summary>
        /// Gets the interval (in seconds) at which to log cache statistics.
        /// Returns 0 if statistics logging is disabled.
        /// </summary>
        public float StatisticsLogInterval => _statisticsLogInterval;

        /// <summary>
        /// Gets the target cache hit rate (value between 0.0 and 1.0).
        /// Performance warnings are logged if the actual hit rate falls below this value.
        /// </summary>
        public float TargetHitRate => _targetHitRate;

        /// <summary>
        /// Validates configuration values in the Unity Editor.
        /// Ensures capacity is within reasonable bounds.
        /// </summary>
        private void OnValidate()
        {
            // Ensure capacity is at least 16 (minimum viable cache size)
            if (_chunkCacheCapacity < 16)
            {
                Debug.LogWarning($"[StreamingConfiguration] Cache capacity {_chunkCacheCapacity} is too low. Setting to minimum value of 16.");
                _chunkCacheCapacity = 16;
            }

            // Ensure capacity doesn't exceed reasonable limits (1024 chunks)
            if (_chunkCacheCapacity > 1024)
            {
                Debug.LogWarning($"[StreamingConfiguration] Cache capacity {_chunkCacheCapacity} is very high and may use excessive memory.");
            }

            // Validate target hit rate
            if (_targetHitRate < 0.5f)
            {
                Debug.LogWarning($"[StreamingConfiguration] Target hit rate {_targetHitRate:P0} is very low. Consider increasing cache capacity.");
            }
        }

        /// <summary>
        /// Creates a default configuration with recommended settings.
        /// </summary>
        public static StreamingConfiguration CreateDefault()
        {
            var config = CreateInstance<StreamingConfiguration>();
            config._chunkCacheCapacity = 256;
            config._enableStatistics = true;
            config._statisticsLogInterval = 10f;
            config._targetHitRate = 0.8f;
            return config;
        }
    }
}
