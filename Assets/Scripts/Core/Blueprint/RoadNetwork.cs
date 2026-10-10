using System;
using System.Collections.Generic;
using UnityEngine;

namespace YouSaidLeft.Core
{
    public readonly struct RoadNode
    {
        public readonly int Id;
        public readonly Vector2Int Coordinates;
        public readonly int Level;


        public RoadNode(int id, Vector2Int coordinates, int level)
        {
            Id = id;
            Coordinates = coordinates;
            Level = level;
        }
    }

    public class RoadNetwork
    {
        private readonly List<RoadNode> _nodes;
        private readonly Dictionary<int, HashSet<int>> _connections;
        private readonly Dictionary<int, List<RoadPort>> _ports;

        public RoadNetwork()
        {
            _nodes = new();
            _connections = new();
            _ports = new();
        }

        #region Node Access Methods
        public void AddNode(RoadNode node)
        {
            if (TryGetNode(node.Id, out _))
            {
                throw new ArgumentException("중복된 Id의 노드가 있습니다.");
            }

            _nodes.Add(node);
        }

        public bool TryGetNode(int id, out RoadNode node)
        {
            node = default;
            foreach (var roadNode in _nodes)
            {
                if (roadNode.Id == id)
                {
                    node = roadNode;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Node Connection Methods
        public void ConnectBidirectional(int firstNodeId, int secondNodeId)
        {
            if (!TryGetNode(firstNodeId, out _))
            {
                throw new ArgumentException("첫 번째 노드를 찾을 수 없습니다.");
            }
            if (!TryGetNode(secondNodeId, out _))
            {
                throw new ArgumentException("두 번째 노드를 찾을 수 없습니다.");
            }

            if (!_connections.TryGetValue(firstNodeId, out var conn))
            {
                conn = new();
                _connections.Add(firstNodeId, conn);
            }
            conn.Add(secondNodeId);

            if (!_connections.TryGetValue(secondNodeId, out conn))
            {
                conn = new();
                _connections.Add(secondNodeId, conn);
            }
            conn.Add(firstNodeId);
        }

        public bool HasDirectConnection(int fromNodeId, int toNodeId)
        {
            if (!_connections.TryGetValue(fromNodeId, out var conn))
            {
                return false;
            }
            return conn.Contains(toNodeId);
        }

        // TBD: BFS로 연결 계약을 검증하고, 자동 도로 계획 단계에서 거리·주행 시간·터널이나 교량의 건설 비용 중 무엇을 최소화할지 정한 뒤 A*를 검토
        public bool HasPath(int startNodeId, int destinationNodeId)
        {
            if (!TryGetNode(startNodeId, out _) || !TryGetNode(destinationNodeId, out _))
            {
                return false;
            }

            var pendingNodeIds = new Queue<int>();
            var visitedNodeIds = new HashSet<int>() { startNodeId };

            pendingNodeIds.Enqueue(startNodeId);
            while (pendingNodeIds.Count > 0)
            {
                var nodeId = pendingNodeIds.Dequeue();
                if (nodeId == destinationNodeId)
                {
                    return true;
                }

                if (!_connections.TryGetValue(nodeId, out var conn))
                {
                    continue;
                }

                foreach (var next in conn)
                {
                    if (visitedNodeIds.Add(next))
                    {
                        pendingNodeIds.Enqueue(next);
                    }
                }
            }
            return false;
        }
        #endregion

        #region Port Access Methods
        public void AddPort(int ownerNodeId, RoadPort port)
        {
            if (!TryGetNode(ownerNodeId, out _))
            {
                throw new ArgumentException("노드를 찾을 수 없습니다.");
            }

            if (!_ports.TryGetValue(ownerNodeId, out var ports))
            {
                ports = new();
                _ports.Add(ownerNodeId, ports);
            }
            ports.Add(port);
        }

        public IReadOnlyList<RoadPort> GetPorts(int ownerNodeId)
        {
            if (!TryGetNode(ownerNodeId, out _))
            {
                throw new ArgumentException("노드를 찾을 수 없습니다.");
            }

            if (!_ports.TryGetValue(ownerNodeId, out var ports))
            {
                return Array.Empty<RoadPort>();
            }

            return ports.ToArray();
        }
        #endregion
    }
}
