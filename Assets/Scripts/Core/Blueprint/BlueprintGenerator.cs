namespace YouSaidLeft.Core
{
    public class BlueprintGenerator
    {
        public Blueprint CreateBlueprint(int width, int height, int baseLevel = 0)
        {
            // 0. 기본 평지 청사진
            var blueprint = new Blueprint(width, height, baseLevel);

            return blueprint;
        }

        private void ApplyTraits(Blueprint blueprint)
        {
            // TODO: 특성 시스템 구현 후, 이 메서드를 사용하여 청사진에 특성을 적용합니다.
            // TBD: 아직 시스템 로직을 어떻게 할지 확정 안되어서, 이 메서드는 임시로 비워둡니다.
        }
    }
}
