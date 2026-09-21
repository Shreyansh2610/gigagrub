using System.Collections.Generic;
using UnityEngine;

namespace GigaGrub.Systems
{
    /// <summary>
    /// High-performance, zero-allocation 2D Spatial Hash Grid for fast spatial queries
    /// (e.g. food detection, creature neighbor queries) in the playable arena.
    /// </summary>
    /// <typeparam name="T">Type of object stored in the grid</typeparam>
    public class SpatialGrid2D<T> where T : class
    {
        private readonly float cellSize;
        private readonly float invCellSize;
        private readonly Vector2 minBounds;
        private readonly int cols;
        private readonly int rows;
        private readonly List<T>[] cells;
        private readonly int totalCells;

        public float CellSize => cellSize;
        public int Columns => cols;
        public int Rows => rows;

        public SpatialGrid2D(Vector2 arenaMin, Vector2 arenaMax, float cellSize = 10f, int initialCellCapacity = 16)
        {
            this.cellSize = Mathf.Max(1f, cellSize);
            this.invCellSize = 1f / this.cellSize;
            this.minBounds = arenaMin;

            float width = arenaMax.x - arenaMin.x;
            float height = arenaMax.y - arenaMin.y;

            this.cols = Mathf.Max(1, Mathf.CeilToInt(width * invCellSize));
            this.rows = Mathf.Max(1, Mathf.CeilToInt(height * invCellSize));
            this.totalCells = cols * rows;

            cells = new List<T>[totalCells];
            for (int i = 0; i < totalCells; i++)
            {
                cells[i] = new List<T>(initialCellCapacity);
            }
        }

        private int GetCellIndex(Vector2 position)
        {
            int col = Mathf.FloorToInt((position.x - minBounds.x) * invCellSize);
            int row = Mathf.FloorToInt((position.y - minBounds.y) * invCellSize);

            col = Mathf.Clamp(col, 0, cols - 1);
            row = Mathf.Clamp(row, 0, rows - 1);

            return row * cols + col;
        }

        private void GetCellCoords(Vector2 position, out int col, out int row)
        {
            col = Mathf.FloorToInt((position.x - minBounds.x) * invCellSize);
            row = Mathf.FloorToInt((position.y - minBounds.y) * invCellSize);

            col = Mathf.Clamp(col, 0, cols - 1);
            row = Mathf.Clamp(row, 0, rows - 1);
        }

        public void Insert(T item, Vector2 position)
        {
            if (item == null) return;
            int idx = GetCellIndex(position);
            cells[idx].Add(item);
        }

        public bool Remove(T item, Vector2 position)
        {
            if (item == null) return false;
            int idx = GetCellIndex(position);
            return cells[idx].Remove(item);
        }

        public void Move(T item, Vector2 oldPos, Vector2 newPos)
        {
            if (item == null) return;
            int oldIdx = GetCellIndex(oldPos);
            int newIdx = GetCellIndex(newPos);

            if (oldIdx != newIdx)
            {
                cells[oldIdx].Remove(item);
                cells[newIdx].Add(item);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < totalCells; i++)
            {
                cells[i].Clear();
            }
        }

        /// <summary>
        /// Queries all items within a radius of the center point without heap allocation.
        /// Fills the caller-provided results list.
        /// </summary>
        public void QueryRadius(Vector2 center, float radius, List<T> results)
        {
            results.Clear();

            int minCol, minRow, maxCol, maxRow;
            GetCellCoords(new Vector2(center.x - radius, center.y - radius), out minCol, out minRow);
            GetCellCoords(new Vector2(center.x + radius, center.y + radius), out maxCol, out maxRow);

            for (int r = minRow; r <= maxRow; r++)
            {
                int rowOffset = r * cols;
                for (int c = minCol; c <= maxCol; c++)
                {
                    List<T> cellList = cells[rowOffset + c];
                    int count = cellList.Count;
                    for (int i = 0; i < count; i++)
                    {
                        T item = cellList[i];
                        if (item != null)
                        {
                            results.Add(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Direct access to items in a specific cell by column and row.
        /// </summary>
        public IReadOnlyList<T> GetCellItems(int col, int row)
        {
            col = Mathf.Clamp(col, 0, cols - 1);
            row = Mathf.Clamp(row, 0, rows - 1);
            return cells[row * cols + col];
        }
    }
}
