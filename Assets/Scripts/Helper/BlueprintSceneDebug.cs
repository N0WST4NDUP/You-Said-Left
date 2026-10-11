using UnityEngine;
using YouSaidLeft.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YouSaidLeft.Helper
{
    public enum BlueprintPreviewMode
    {
        GeneratedTerrain,
        ManualRoads,
    }

    [ExecuteAlways]
    public class BlueprintSceneDebug : MonoBehaviour
    {
        [Header("미리보기")]
        [SerializeField] private BlueprintPreviewMode _previewMode = BlueprintPreviewMode.GeneratedTerrain;
        [SerializeField] private bool _showLabels = true;

        [Header("청사진 크기")]
        [SerializeField, Min(1)] private int _width = 32;
        [SerializeField, Min(1)] private int _height = 32;
        [SerializeField, Min(0)] private int _baseLevel = 2;
        [SerializeField] private int _waterLevel = 4;
        [SerializeField] private int _seed = 980306;

        // 논리 셀과 높이 층을 미리보기의 로컬 거리로 바꿉니다.
        [SerializeField, Min(0.01f)] private float _cellSize = 1f;
        [SerializeField, Min(0.01f)] private float _levelHeight = 0.2f;

        private Blueprint _blueprint;
        private Blueprint _manualBlueprint;
#if UNITY_EDITOR
        private GUIStyle _labelStyle;
#endif

        private void OnEnable()
        {
            ClearPreviewCache();
#if UNITY_EDITOR
            SceneView.duringSceneGui += DrawSceneLabels;
#endif
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneLabels;
        }
#endif

        private void OnValidate()
        {
            ClearPreviewCache();
        }

        private void ClearPreviewCache()
        {
            _blueprint = null;
            _manualBlueprint = null;
#if UNITY_EDITOR
            _labelStyle = null;
#endif
        }

        private void OnDrawGizmos()
        {
            if (!AreSettingsValid())
            {
                return;
            }

            var blueprint = GetPreviewBlueprint();
            var previousColor = Gizmos.color;
            var previousMatrix = Gizmos.matrix;

            try
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                var manualRoads = _previewMode == BlueprintPreviewMode.ManualRoads;
                DrawTerrain(blueprint, manualRoads);
                if (manualRoads)
                {
                    DrawRoads(blueprint.Roads);
                }
            }
            finally
            {
                Gizmos.color = previousColor;
                Gizmos.matrix = previousMatrix;
            }
        }

        private bool AreSettingsValid()
        {
            if (_cellSize <= 0f || _levelHeight <= 0f)
            {
                return false;
            }

            return _previewMode == BlueprintPreviewMode.ManualRoads ||
                (_width > 0 && _height > 0 && _baseLevel >= 0);
        }

        private Blueprint GetPreviewBlueprint()
        {
            if (_previewMode == BlueprintPreviewMode.ManualRoads)
            {
                if (_manualBlueprint == null)
                {
                    _manualBlueprint = ManualRoadBlueprint.Create();
                }

                return _manualBlueprint;
            }

            if (_blueprint == null)
            {
                var sampler = PerlinLevelSampler.FromSeed(
                    _seed, frequency: 0.125f, maxLevelOffset: 8, originRange: 256f);
                var generator = new BlueprintGenerator(sampler.Sample);
                _blueprint = generator.CreateBlueprint(_width, _height, _baseLevel, _waterLevel);
            }

            return _blueprint;
        }

        private void DrawTerrain(Blueprint blueprint, bool dim)
        {
            for (var y = 0; y < blueprint.Height; y++)
            {
                for (var x = 0; x < blueprint.Width; x++)
                {
                    if (blueprint.TryGetCell(x, y, out var cell))
                    {
                        DrawTerrainCell(blueprint, x, y, cell, dim);
                    }
                }
            }
        }

        private void DrawTerrainCell(Blueprint blueprint, int x, int y, TerrainCell cell, bool dim)
        {
            var groundColor = dim ? new Color(0.12f, 0.18f, 0.12f) : Color.green;
            if (cell.Kind != TerrainKind.Water)
            {
                DrawTile(x, y, cell.Level, groundColor, false);
                return;
            }

            DrawTile(x, y, cell.Level, dim ? groundColor : Color.gray, true);
            if (blueprint.WaterLevel.HasValue)
            {
                var waterColor = dim
                    ? new Color(0.12f, 0.2f, 0.25f, 0.65f)
                    : new Color(0.35f, 0.75f, 1f, 0.65f);
                DrawTile(x, y, blueprint.WaterLevel.Value, waterColor, false);
            }
        }

        private void DrawTile(int x, int y, int level, Color color, bool wire)
        {
            var thickness = _levelHeight * 0.1f;
            var center = ToDisplayPosition(new Vector2(x, y), level) - Vector3.up * (thickness * 0.5f);
            var size = new Vector3(_cellSize * 0.95f, thickness, _cellSize * 0.95f);
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

        private void DrawRoads(RoadLayout roads)
        {
            DrawRoadConnections(roads);
            var modules = roads.GetModules();
            foreach (var module in modules)
            {
                DrawModuleFootprint(module);
                for (var index = 0; index < module.PortCount; index++)
                {
                    DrawPortArrow(module.GetPort(index));
                }
            }

            Gizmos.color = Color.white;
            foreach (var node in roads.GetNodes())
            {
                Gizmos.DrawSphere(ToDisplayPosition(node.Coordinates, node.Level), _cellSize * 0.035f);
            }
        }

        private void DrawModuleFootprint(PlacedRoadModule module)
        {
            var placement = module.Placement;
            var center = ToDisplayPosition(placement.CellCoordinates, placement.BaseLevel);
            Gizmos.color = new Color(0.65f, 0.65f, 0.65f);
            Gizmos.DrawWireCube(center, new Vector3(_cellSize, _levelHeight * 0.05f, _cellSize));
        }

        private void DrawRoadConnections(RoadLayout roads)
        {
            foreach (var connection in roads.GetConnections())
            {
                if (!roads.TryGetNode(connection.FirstNodeId, out var first) ||
                    !roads.TryGetNode(connection.SecondNodeId, out var second))
                {
                    continue;
                }

                var start = ToDisplayPosition(first.Coordinates, first.Level);
                var end = ToDisplayPosition(second.Coordinates, second.Level);
                Gizmos.color = connection.IsExternal ? Color.cyan : Color.yellow;
                if (connection.IsExternal)
                {
                    DrawConnectionRing((start + end) * 0.5f);
                }
                else
                {
                    Gizmos.DrawLine(start, end);
                }
            }
        }

        private void DrawConnectionRing(Vector3 center)
        {
            const int SegmentCount = 24;
            var radius = _cellSize * 0.1f;
            var previous = center + Vector3.right * radius;
            for (var index = 1; index <= SegmentCount; index++)
            {
                var angle = index * (Mathf.PI * 2f / SegmentCount);
                var next = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private void DrawPortArrow(ResolvedRoadPort port)
        {
            var start = ToDisplayPosition(port.ContactPoint, port.Port.BoundaryLevel);
            var direction = GetPortDirection(port.Port.Side);
            var tip = start + direction * (_cellSize * 0.24f);
            var side = Vector3.Cross(Vector3.up, direction) * (_cellSize * 0.04f);
            var back = tip - direction * (_cellSize * 0.08f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, tip);
            Gizmos.DrawLine(tip, back + side);
            Gizmos.DrawLine(tip, back - side);
        }

        private Vector3 ToDisplayPosition(Vector2 coordinates, int level)
        {
            return new Vector3(coordinates.x * _cellSize, level * _levelHeight, coordinates.y * _cellSize);
        }

        private static Vector3 GetPortDirection(RoadPortSide side)
        {
            return side switch
            {
                RoadPortSide.PositiveX => Vector3.right,
                RoadPortSide.NegativeX => Vector3.left,
                RoadPortSide.PositiveY => Vector3.forward,
                RoadPortSide.NegativeY => Vector3.back,
                _ => Vector3.zero,
            };
        }

#if UNITY_EDITOR
        private void DrawSceneLabels(SceneView sceneView)
        {
            if (_previewMode != BlueprintPreviewMode.ManualRoads || !_showLabels ||
                !sceneView.drawGizmos || !AreSettingsValid())
            {
                return;
            }

            if (GizmoUtility.TryGetGizmoInfo(GetType(), out var info) && !info.gizmoEnabled)
            {
                return;
            }

            DrawRoadLabels(GetPreviewBlueprint().Roads);
        }

        private void DrawRoadLabels(RoadLayout roads)
        {
            var previousColor = Handles.color;
            var previousMatrix = Handles.matrix;
            var previousDepthTest = Handles.zTest;
            try
            {
                Handles.matrix = transform.localToWorldMatrix;
                Handles.color = Color.white;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                if (_labelStyle == null)
                {
                    _labelStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                }

                foreach (var module in roads.GetModules())
                {
                    DrawModuleLabels(roads, module);
                }

                DrawExternalConnectionLabels(roads);
            }
            finally
            {
                Handles.color = previousColor;
                Handles.matrix = previousMatrix;
                Handles.zTest = previousDepthTest;
            }
        }

        private void DrawModuleLabels(RoadLayout roads, PlacedRoadModule module)
        {
            var placement = module.Placement;
            var modulePosition = ToDisplayPosition(placement.CellCoordinates, placement.BaseLevel);
            DrawLabel(modulePosition + Vector3.up * (_cellSize * 0.12f), $"M{module.Id}", Color.white);
            for (var index = 0; index < module.PortCount; index++)
            {
                var nodeId = module.GetNodeId(index);
                if (HasExternalConnection(roads, nodeId))
                {
                    continue;
                }

                var port = module.GetPort(index);
                var position = ToDisplayPosition(port.ContactPoint, port.Port.BoundaryLevel);
                var direction = GetPortDirection(port.Port.Side);
                var verticalOffset = direction.z != 0f ? 0.3f : 0.08f;
                var labelPosition = position + direction * (_cellSize * 0.28f) +
                    Vector3.up * (_cellSize * verticalOffset);
                DrawLabel(labelPosition, $"N{nodeId}/L{port.Port.BoundaryLevel}", Color.cyan);
            }
        }

        private static bool HasExternalConnection(RoadLayout roads, int nodeId)
        {
            foreach (var connection in roads.GetConnections())
            {
                if (connection.IsExternal &&
                    (connection.FirstNodeId == nodeId || connection.SecondNodeId == nodeId))
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawExternalConnectionLabels(RoadLayout roads)
        {
            foreach (var connection in roads.GetConnections())
            {
                if (!connection.IsExternal ||
                    !roads.TryGetNode(connection.FirstNodeId, out var first) ||
                    !roads.TryGetNode(connection.SecondNodeId, out var second))
                {
                    continue;
                }

                var start = ToDisplayPosition(first.Coordinates, first.Level);
                var end = ToDisplayPosition(second.Coordinates, second.Level);
                var labelPosition = (start + end) * 0.5f + Vector3.up * (_cellSize * 0.32f);
                DrawLabel(labelPosition,
                    $"N{connection.FirstNodeId}↔{connection.SecondNodeId}/L{first.Level}", Color.cyan);
            }
        }

        private void DrawLabel(Vector3 position, string text, Color color)
        {
            _labelStyle.normal.textColor = color;
            Handles.Label(position, text, _labelStyle);
        }
#endif
    }
}
