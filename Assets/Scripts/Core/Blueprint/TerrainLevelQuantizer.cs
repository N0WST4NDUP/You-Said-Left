using UnityEngine;

namespace YouSaidLeft.Core
{
    /// <summary>
    /// 노이즈 값(0~1)을 허용된 정수 높이차로 변환하는 유틸리티 클래스입니다.
    /// </summary>
    public static class TerrainLevelQuantizer
    {
        public static int ToLevelOffset(float noiseValue, int maxLevelOffset)
        {
            var clampedNoiseValue = Mathf.Clamp01(noiseValue);
            var levelOffset = Mathf.FloorToInt(clampedNoiseValue * (maxLevelOffset + 1));
            return Mathf.Clamp(levelOffset, 0, maxLevelOffset);
        }
    }
}
