using UnityEngine;

namespace YouSaidLeft.Core
{
    public class PerlinLevelSampler
    {
        private readonly float _frequency;
        private readonly int _maxLevelOffset;
        private readonly float _originX;
        private readonly float _originY;

        public PerlinLevelSampler(float frequency, int maxLevelOffset, float originX = 0f, float originY = 0f)
        {
            _frequency = frequency;
            _maxLevelOffset = maxLevelOffset;
            _originX = originX;
            _originY = originY;
        }

        public static PerlinLevelSampler FromSeed(int seed, float frequency, int maxLevelOffset, float originRange)
        {
            var random = new System.Random(seed);
            var originX = (float)(random.NextDouble() * originRange);
            var originY = (float)(random.NextDouble() * originRange);
            return new PerlinLevelSampler(frequency, maxLevelOffset, originX, originY);
        }

        public int Sample(int x, int y)
        {
            var noiseValue = Mathf.PerlinNoise(
                _originX + x * _frequency,
                _originY + y * _frequency
            );
            return TerrainLevelQuantizer.ToLevelOffset(noiseValue, _maxLevelOffset);
        }
    }
}
