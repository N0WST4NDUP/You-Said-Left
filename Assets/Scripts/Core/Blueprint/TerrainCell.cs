namespace YouSaidLeft.Core
{
    public enum TerrainKind
    {
        Grass,
        Water,
    }

    public readonly struct TerrainCell
    {
        public readonly int Level;
        public readonly TerrainKind Kind;

        public TerrainCell(int level, TerrainKind kind = TerrainKind.Grass)
        {
            Level = level;
            Kind = kind;
        }
    }
}
