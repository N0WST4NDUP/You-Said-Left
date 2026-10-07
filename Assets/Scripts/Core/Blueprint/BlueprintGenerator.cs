using System;

namespace YouSaidLeft.Core
{
    public class BlueprintGenerator
    {
        private readonly Func<int, int, int> _sampleLevelOffset;

        public BlueprintGenerator() : this(sampleLevelOffset: (x, y) => 0) { }
        public BlueprintGenerator(Func<int, int, int> sampleLevelOffset)
        {
            _sampleLevelOffset = sampleLevelOffset ?? throw new ArgumentNullException(nameof(sampleLevelOffset));
        }

        public Blueprint CreateBlueprint(int width, int height, int baseLevel = 0)
        {
            // 0. 기본 평지 청사진
            var blueprint = new Blueprint(width, height, baseLevel);

            // 1. 높이 계산 결과를 모든 셀에 절대 높이로 적용합니다.
            ApplyElevation(blueprint);

            return blueprint;
        }

        private void ApplyElevation(Blueprint blueprint)
        {
            for (var y = 0; y < blueprint.Height; y++)
            {
                for (var x = 0; x < blueprint.Width; x++)
                {
                    var levelOffset = _sampleLevelOffset(x, y);
                    var level = blueprint.BaseLevel + levelOffset;
                    var cell = new TerrainCell(level);
                    blueprint.TrySetCell(x, y, cell);
                }
            }
        }

        private void ApplyTraits(Blueprint blueprint)
        {
            // TODO: 특성 시스템 구현 후, 이 메서드를 사용하여 청사진에 특성을 적용합니다.
            // TBD: 아직 시스템 로직을 어떻게 할지 확정 안되어서, 이 메서드는 임시로 비워둡니다.
        }
    }
}
