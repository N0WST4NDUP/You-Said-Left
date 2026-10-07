namespace YouSaidLeft.Core
{
    public class Blueprint
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int BaseLevel;

        private readonly TerrainCell[,] _cells;

        public Blueprint(int width, int height, int baseLevel = 0)
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

        private bool ValidateCoordinates(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}
