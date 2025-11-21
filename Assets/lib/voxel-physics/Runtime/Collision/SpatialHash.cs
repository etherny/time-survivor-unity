using System.Collections.Generic;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Physics
{
    /// <summary>
    /// Spatial hash grid for fast spatial queries on voxel data.
    /// Useful for collision detection, neighbor queries, and raycasting optimization.
    /// </summary>
    /// <typeparam name="T">Type of data to store (typically chunk references)</typeparam>
    public class SpatialHash<T> where T : class
    {
        private readonly Dictionary<int3, List<T>> _grid;
        private readonly float _cellSize;

        public SpatialHash(float cellSize)
        {
            _cellSize = cellSize;
            _grid = new Dictionary<int3, List<T>>();
        }

        /// <summary>
        /// Get grid cell coordinate for a world position.
        /// </summary>
        public int3 GetCellCoord(float3 worldPosition)
        {
            return (int3)math.floor(worldPosition / _cellSize);
        }

        /// <summary>
        /// Add an item at a world position.
        /// </summary>
        public void Add(float3 worldPosition, T item)
        {
            int3 cellCoord = GetCellCoord(worldPosition);

            if (!_grid.TryGetValue(cellCoord, out var cell))
            {
                cell = new List<T>();
                _grid[cellCoord] = cell;
            }

            if (!cell.Contains(item))
            {
                cell.Add(item);
            }
        }

        /// <summary>
        /// Remove an item from a world position.
        /// </summary>
        public bool Remove(float3 worldPosition, T item)
        {
            int3 cellCoord = GetCellCoord(worldPosition);

            if (_grid.TryGetValue(cellCoord, out var cell))
            {
                bool removed = cell.Remove(item);

                // Clean up empty cells
                if (cell.Count == 0)
                {
                    _grid.Remove(cellCoord);
                }

                return removed;
            }

            return false;
        }

        /// <summary>
        /// Get all items in a cell at world position.
        /// </summary>
        public List<T> GetItemsAt(float3 worldPosition)
        {
            int3 cellCoord = GetCellCoord(worldPosition);

            if (_grid.TryGetValue(cellCoord, out var cell))
            {
                return cell;
            }

            return new List<T>();
        }

        /// <summary>
        /// Get all items in a sphere around a world position.
        /// </summary>
        public List<T> GetItemsInSphere(float3 center, float radius)
        {
            var results = new List<T>();
            var visitedCells = new HashSet<int3>();

            // Calculate bounding cells
            int3 minCell = GetCellCoord(center - radius);
            int3 maxCell = GetCellCoord(center + radius);

            // Check all cells in bounding box
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        int3 cellCoord = new int3(x, y, z);

                        if (_grid.TryGetValue(cellCoord, out var cell))
                        {
                            foreach (var item in cell)
                            {
                                if (!results.Contains(item))
                                {
                                    results.Add(item);
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Get all items in an axis-aligned bounding box.
        /// </summary>
        public List<T> GetItemsInBox(float3 min, float3 max)
        {
            var results = new List<T>();

            int3 minCell = GetCellCoord(min);
            int3 maxCell = GetCellCoord(max);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        int3 cellCoord = new int3(x, y, z);

                        if (_grid.TryGetValue(cellCoord, out var cell))
                        {
                            foreach (var item in cell)
                            {
                                if (!results.Contains(item))
                                {
                                    results.Add(item);
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Clear all items from the spatial hash.
        /// </summary>
        public void Clear()
        {
            _grid.Clear();
        }

        /// <summary>
        /// Get total number of cells in use.
        /// </summary>
        public int CellCount => _grid.Count;

        /// <summary>
        /// Get total number of items stored (with duplicates if in multiple cells).
        /// </summary>
        public int ItemCount
        {
            get
            {
                int count = 0;
                foreach (var cell in _grid.Values)
                {
                    count += cell.Count;
                }
                return count;
            }
        }
    }
}
