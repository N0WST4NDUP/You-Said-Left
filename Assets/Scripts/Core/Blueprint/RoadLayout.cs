using System;
using System.Collections.Generic;
using UnityEngine;

namespace YouSaidLeft.Core
{
    public readonly struct RoadConnection
    {
        public readonly int FirstNodeId;
        public readonly int SecondNodeId;
        public readonly bool IsExternal;

        public RoadConnection(int firstNodeId, int secondNodeId, bool isExternal)
        {
            FirstNodeId = firstNodeId;
            SecondNodeId = secondNodeId;
            IsExternal = isExternal;
        }
    }

    public sealed class PlacedRoadModule
    {
        public readonly int Id;
        public readonly RoadModulePlacement Placement;

        private readonly RoadPortDefinition[] _definitions;
        private readonly ResolvedRoadPort[] _ports;
        private readonly int[] _nodeIds;

        public int PortCount => _ports.Length;

        internal PlacedRoadModule(int id, RoadModulePlacement placement,
            RoadPortDefinition[] definitions, ResolvedRoadPort[] ports, int[] nodeIds)
        {
            Id = id;
            Placement = placement;
            _definitions = definitions;
            _ports = ports;
            _nodeIds = nodeIds;
        }

        public RoadPortDefinition GetDefinition(int portIndex) => _definitions[ValidatePortIndex(portIndex)];
        public ResolvedRoadPort GetPort(int portIndex) => _ports[ValidatePortIndex(portIndex)];
        public int GetNodeId(int portIndex) => _nodeIds[ValidatePortIndex(portIndex)];

        private int ValidatePortIndex(int portIndex)
        {
            if (portIndex < 0 || portIndex >= PortCount)
            {
                throw new ArgumentOutOfRangeException(nameof(portIndex));
            }
            return portIndex;
        }
    }

    public sealed class RoadLayout
    {
        private readonly int _width;
        private readonly int _height;
        private readonly RoadNetwork _network = new();
        private readonly Dictionary<int, PlacedRoadModule> _modules = new();
        private readonly List<RoadConnection> _connections = new();
        private int _nextNodeId;

        public RoadLayout(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }
            _width = width;
            _height = height;
        }

        public IReadOnlyList<RoadNode> GetNodes() => _network.GetNodes();

        public IReadOnlyList<PlacedRoadModule> GetModules() => new List<PlacedRoadModule>(_modules.Values).ToArray();

        public IReadOnlyList<RoadConnection> GetConnections() => _connections.ToArray();

        public bool TryGetNode(int nodeId, out RoadNode node) => _network.TryGetNode(nodeId, out node);

        public bool HasPath(int startNodeId, int destinationNodeId) => _network.HasPath(startNodeId, destinationNodeId);

        public void ConnectInternal(int moduleId, int firstPortIndex, int secondPortIndex)
        {
            var module = GetModule(moduleId);
            var firstNodeId = module.GetNodeId(firstPortIndex);
            var secondNodeId = module.GetNodeId(secondPortIndex);
            AddConnection(firstNodeId, secondNodeId, false);
        }

        public bool TryConnectExternal(int firstModuleId, int firstPortIndex, int secondModuleId, int secondPortIndex)
        {
            var first = GetModule(firstModuleId);
            var second = GetModule(secondModuleId);
            var firstPort = first.GetPort(firstPortIndex);
            var secondPort = second.GetPort(secondPortIndex);
            if (firstModuleId == secondModuleId ||
                !firstPort.ContactPoint.Equals(secondPort.ContactPoint) ||
                !RoadPortMatcher.MatchesLogicalBoundary(firstPort.Port, secondPort.Port))
            {
                return false;
            }
            AddConnection(first.GetNodeId(firstPortIndex), second.GetNodeId(secondPortIndex), true);
            return true;
        }

        private PlacedRoadModule GetModule(int moduleId)
        {
            if (!_modules.TryGetValue(moduleId, out var module))
            {
                throw new ArgumentException("모듈을 찾을 수 없습니다.", nameof(moduleId));
            }
            return module;
        }

        private void AddConnection(int firstNodeId, int secondNodeId, bool isExternal)
        {
            if (_network.HasDirectConnection(firstNodeId, secondNodeId))
            {
                return;
            }
            _network.ConnectBidirectional(firstNodeId, secondNodeId);
            _connections.Add(new RoadConnection(firstNodeId, secondNodeId, isExternal));
        }


        public PlacedRoadModule AddModule(int moduleId, RoadModulePlacement placement,
            params RoadPortDefinition[] definitions)
        {
            ValidateModule(moduleId, placement, definitions);
            var copiedDefinitions = (RoadPortDefinition[])definitions.Clone();
            var ports = new ResolvedRoadPort[definitions.Length];
            var nodeIds = new int[definitions.Length];
            var nextNodeId = checked(_nextNodeId + definitions.Length);

            // 모든 포트를 해석한 뒤에만 모듈과 노드를 저장합니다.
            for (var i = 0; i < definitions.Length; i++)
            {
                ports[i] = RoadPortResolver.ResolveUnrotated(placement, copiedDefinitions[i]);
                nodeIds[i] = _nextNodeId + i;
            }

            var module = new PlacedRoadModule(moduleId, placement, copiedDefinitions, ports, nodeIds);
            for (var i = 0; i < ports.Length; i++)
            {
                _network.AddNode(new RoadNode(nodeIds[i], ports[i].ContactPoint, ports[i].Port.BoundaryLevel));
                _network.AddPort(nodeIds[i], ports[i].Port);
            }
            _modules.Add(moduleId, module);
            _nextNodeId = nextNodeId;
            return module;
        }

        private void ValidateModule(int moduleId, RoadModulePlacement placement, RoadPortDefinition[] definitions)
        {
            if (_modules.ContainsKey(moduleId))
            {
                throw new ArgumentException("중복된 모듈 ID입니다.", nameof(moduleId));
            }
            var cell = placement.CellCoordinates;
            if (cell.x < 0 || cell.x >= _width || cell.y < 0 || cell.y >= _height)
            {
                throw new ArgumentOutOfRangeException(nameof(placement), "배치 셀이 청사진 범위 밖입니다.");
            }
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }
            if (definitions.Length == 0)
            {
                throw new ArgumentException("경계 포트가 필요합니다.", nameof(definitions));
            }
        }
    }
}
