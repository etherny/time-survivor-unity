using System;
using TimeSurvivor.Voxel.Core;
using UnityEngine;

namespace TimeSurvivor.Voxel.Streaming
{
    /// <summary>
    /// Represents a chunk load request with priority for the streaming system.
    /// Used in priority queue to determine load order based on distance to player.
    /// Implements IComparable for SortedSet ordering.
    /// </summary>
    public struct ChunkLoadRequest : IComparable<ChunkLoadRequest>, IEquatable<ChunkLoadRequest>
    {
        /// <summary>
        /// The chunk coordinate to load.
        /// </summary>
        public ChunkCoord Coordinate { get; private set; }

        /// <summary>
        /// Distance from player to chunk center (in meters).
        /// Used for priority ordering - closer chunks load first.
        /// </summary>
        public float DistanceToPlayer { get; private set; }

        /// <summary>
        /// Timestamp when this request was created (for tie-breaking).
        /// </summary>
        public float RequestTime { get; private set; }

        /// <summary>
        /// Creates a new chunk load request.
        /// </summary>
        /// <param name="coord">Chunk coordinate to load.</param>
        /// <param name="distanceToPlayer">Distance from player in meters.</param>
        public ChunkLoadRequest(ChunkCoord coord, float distanceToPlayer)
        {
            Coordinate = coord;
            DistanceToPlayer = distanceToPlayer;
            RequestTime = Time.time;
        }

        /// <summary>
        /// Compares this request with another for priority ordering.
        /// Lower distance = higher priority (loads first).
        /// If distances are equal, earlier request time wins.
        /// If times are equal, use coordinate hash for deterministic ordering.
        /// </summary>
        /// <param name="other">Other request to compare with.</param>
        /// <returns>-1 if this has higher priority, 1 if lower, 0 if equal.</returns>
        public int CompareTo(ChunkLoadRequest other)
        {
            // Primary: Distance (ascending - closer chunks first)
            int distanceComparison = DistanceToPlayer.CompareTo(other.DistanceToPlayer);
            if (distanceComparison != 0)
                return distanceComparison;

            // Secondary: Request time (ascending - older requests first)
            int timeComparison = RequestTime.CompareTo(other.RequestTime);
            if (timeComparison != 0)
                return timeComparison;

            // Tertiary: Coordinate hash (for deterministic ordering of simultaneous requests)
            return Coordinate.GetHashCode().CompareTo(other.Coordinate.GetHashCode());
        }

        /// <summary>
        /// Checks equality based on chunk coordinate only.
        /// Two requests for the same chunk are considered equal regardless of distance/time.
        /// </summary>
        /// <param name="other">Other request to compare with.</param>
        /// <returns>True if coordinates match.</returns>
        public bool Equals(ChunkLoadRequest other)
        {
            return Coordinate.Equals(other.Coordinate);
        }

        /// <summary>
        /// Checks equality with object.
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>True if obj is ChunkLoadRequest with same coordinate.</returns>
        public override bool Equals(object obj)
        {
            return obj is ChunkLoadRequest other && Equals(other);
        }

        /// <summary>
        /// Returns hash code based on coordinate.
        /// </summary>
        /// <returns>Coordinate hash code.</returns>
        public override int GetHashCode()
        {
            return Coordinate.GetHashCode();
        }

        /// <summary>
        /// Returns string representation for debugging.
        /// </summary>
        /// <returns>String showing coordinate and distance.</returns>
        public override string ToString()
        {
            return $"LoadRequest({Coordinate}, dist={DistanceToPlayer:F1}m, time={RequestTime:F2})";
        }

        public static bool operator ==(ChunkLoadRequest left, ChunkLoadRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkLoadRequest left, ChunkLoadRequest right)
        {
            return !left.Equals(right);
        }
    }
}
