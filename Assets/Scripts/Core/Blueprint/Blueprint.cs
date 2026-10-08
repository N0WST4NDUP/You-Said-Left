using System.Collections.Generic;
using UnityEngine;

namespace YouSaidLeft.Core
{
    public class Blueprint
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int BaseLevel;
        public readonly int? WaterLevel;

        private readonly TerrainCell[,] _cells;

        public Blueprint(int width, int height, int baseLevel = 0, int? waterLevel = null)
        {
            if (width <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(width), "Width must be a positive integer.");
            }
            else if (height <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(height), "Height must be a positive integer.");
            }
            else if (baseLevel < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(baseLevel), "Base level must be a non-negative integer.");
            }

            Width = width;
            Height = height;
            BaseLevel = baseLevel;
            WaterLevel = waterLevel;

            _cells = new TerrainCell[width, height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    _cells[x, y] = new(baseLevel);
                }
            }
        }

        #region Cell Access Methods
        public bool TryGetCell(int x, int y, out TerrainCell cell)
        {
            if (!ValidateCoordinates(x, y))
            {
                cell = default;
                return false;
            }

            cell = _cells[x, y];
            return true;
        }

        public bool TrySetCell(int x, int y, TerrainCell cell)
        {
            if (!ValidateCoordinates(x, y))
            {
                return false;
            }

            _cells[x, y] = cell;
            return true;
        }
        #endregion

        public IEnumerable<Vector2Int> GetNeighborCoordinates(int x, int y)
        {
            if (!ValidateCoordinates(x, y))
            {
                if (x < 0 || x >= Width)
                {
                    throw new System.ArgumentOutOfRangeException(nameof(x), $"X coordinate {x} is out of bounds. Valid range: [0, {Width - 1}]");
                }
                else if (y < 0 || y >= Height)
                {
                    throw new System.ArgumentOutOfRangeException(nameof(y), $"Y coordinate {y} is out of bounds. Valid range: [0, {Height - 1}]");
                }
            }

            var neighbors = new List<Vector2Int>();

            // 상하좌우 이웃 좌표를 계산합니다.
            var potentialNeighbors = new Vector2Int[]
            {
                new Vector2Int(x, y - 1), // 위
                new Vector2Int(x, y + 1), // 아래
                new Vector2Int(x - 1, y), // 왼쪽
                new Vector2Int(x + 1, y)  // 오른쪽
            };

            foreach (var neighbor in potentialNeighbors)
            {
                if (ValidateCoordinates(neighbor.x, neighbor.y))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        private bool ValidateCoordinates(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}
