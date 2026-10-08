using UnityEngine;
using YouSaidLeft.Core;

namespace YouSaidLeft.Helper
{
    public class BlueprintSceneDebug : MonoBehaviour
    {
        [Header("청사진 크기")]
        [SerializeField, Min(1)] private int _width = 32;
        [SerializeField, Min(1)] private int _height = 32;
        [SerializeField, Min(0)] private int _baseLevel = 2;
        [SerializeField] private int _waterLevel = 4;
        [SerializeField] private int _seed = 980306;

        // 미리보기에 사용할 표시 크기입니다.
        [SerializeField, Min(0.01f)] private float _cellSize = 1f;
        [SerializeField, Min(0.01f)] private float _levelHeight = 0.2f;

        private Blueprint _blueprint;

        private void OnValidate()
        {
            // Inspector 설정이 바뀌면 다음 그리기에서 다시 생성합니다.
            _blueprint = null;
        }

        private void OnDrawGizmos()
        {
            if (_width <= 0 || _height <= 0 || _baseLevel < 0 || _cellSize <= 0f || _levelHeight <= 0f)
            {
                return;
            }

            if (_blueprint == null)
            {
                var sampler = PerlinLevelSampler.FromSeed(
                    _seed,
                    frequency: 0.125f,
                    maxLevelOffset: 8,
                    originRange: 256f);

                var generator = new BlueprintGenerator(sampler.Sample);

                _blueprint = generator.CreateBlueprint(
                    _width, _height, _baseLevel, _waterLevel);
            }

            var previousColor = Gizmos.color;
            var previousMatrix = Gizmos.matrix;

            try
            {
                Gizmos.matrix = transform.localToWorldMatrix;

                for (var y = 0; y < _blueprint.Height; y++)
                {
                    for (var x = 0; x < _blueprint.Width; x++)
                    {
                        if (!_blueprint.TryGetCell(x, y, out var cell))
                        {
                            continue;
                        }

                        if (cell.Kind == TerrainKind.Water)
                        {
                            DrawTile(x, y, cell.Level, Color.gray, true);

                            if (_blueprint.WaterLevel.HasValue)
                            {
                                DrawTile(
                                    x, y,
                                    _blueprint.WaterLevel.Value,
                                    new Color(0.35f, 0.75f, 1f, 0.65f),
                                    false);
                            }
                        }
                        else
                        {
                            DrawTile(x, y, cell.Level, Color.green, false);
                        }
                    }
                }
            }
            finally
            {
                Gizmos.color = previousColor;
                Gizmos.matrix = previousMatrix;
            }
        }

        private void DrawTile(int x, int y, int level, Color color, bool wire)
        {
            var thickness = _levelHeight * 0.1f;

            var center = new Vector3(
                x * _cellSize,
                level * _levelHeight - thickness * 0.5f,
                y * _cellSize);

            var size = new Vector3(
                _cellSize * 0.95f,
                thickness,
                _cellSize * 0.95f);

            Gizmos.color = color;

            if (wire)
            {
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                Gizmos.DrawCube(center, size);
            }
        }
    }
}

