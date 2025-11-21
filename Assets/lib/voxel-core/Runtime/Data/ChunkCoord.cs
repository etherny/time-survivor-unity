using System;
using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Core
{
    /// <summary>
    /// Represents a chunk coordinate in 3D space.
    /// Immutable struct for thread-safe operations in Jobs.
    /// </summary>
    public struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public readonly int3 Value;

        public ChunkCoord(int x, int y, int z)
        {
            Value = new int3(x, y, z);
        }

        public ChunkCoord(int3 coord)
        {
            Value = coord;
        }

        public int X => Value.x;
        public int Y => Value.y;
        public int Z => Value.z;

        public bool Equals(ChunkCoord other)
        {
            return Value.x == other.Value.x && Value.y == other.Value.y && Value.z == other.Value.z;
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            // FNV-1a hash for better distribution
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash ^ Value.x) * 16777619;
                hash = (hash ^ Value.y) * 16777619;
                hash = (hash ^ Value.z) * 16777619;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"ChunkCoord({Value.x}, {Value.y}, {Value.z})";
        }

        public static bool operator ==(ChunkCoord left, ChunkCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkCoord left, ChunkCoord right)
        {
            return !left.Equals(right);
        }

        public static ChunkCoord operator +(ChunkCoord left, int3 offset)
        {
            return new ChunkCoord(left.Value + offset);
        }

        public static ChunkCoord operator -(ChunkCoord left, int3 offset)
        {
            return new ChunkCoord(left.Value - offset);
        }
    }
}
