# RoadNetwork API

도로 노드를 ID로 저장하고, 명시한 양방향 직접 연결 및 여러 구간을 거치는 도달 가능성을 조회하는 현재 API다. 소스는 [RoadNetwork.cs](../../Assets/Scripts/Core/Blueprint/RoadNetwork.cs), 검증 코드는 [RoadNetworkTests.cs](../../Assets/Tests/Editor/RoadNetworkTests.cs)에 있다. 실행 결과와 다음 작업은 [작업 현황](../blueprint-progress.md)을 확인한다.

## 현재 구현 범위

- 같은 수평 좌표의 서로 다른 높이 노드를 서로 다른 ID로 함께 저장한다.
- 지형 `Blueprint`와 독립적인 객체이며, 도로 노드를 추가해도 지형 셀을 수정하지 않는다.
- 좌표·높이로 연결을 추론하지 않고 `ConnectBidirectional`로 지정한 직접 연결만 저장한다.
- `HasPath`는 저장된 연결을 BFS로 탐색해 도달 여부를 bool로 반환한다. 경로 목록이나 거리·시간·건설 비용의 최적 경로를 반환하지 않는다.
- 포트 판정은 아래의 별도 `RoadPortMatcher`에서 경계 높이·반대 방향과 방향에 맞는 바로 이웃 셀을 검사한다. 자동 도로 생성, 경사·실제 포트 형식·공간 검사, 3D 조립과 실제 주행은 아직 구현하지 않았다.

`HasPath`의 여러 구간 도달·고립 노드 테스트와 등록된 노드에 한정한 자기 도달 테스트의 GREEN을 확인했다. 자기 도달 테스트는 한쪽 ID만 없는 조회도 함께 검증한다. 이 테스트의 최초 RED는 AI가 실행 확인하지 않았다. 입체교차와 램프 사례는 직접 연결과 경유 경로를 구분하고 기존 내부 경로 유지까지 보강해 GREEN을 확인했다. 터널 역할의 노드를 통한 입출구 연결과 지표 도로의 분리도 양방향 GREEN을 확인했다.

양방향 연결 메서드는 이번 수동 사례의 계약이다. 게임 전체의 일방통행 정책을 확정하지 않는다. 입체 도로의 설계 방향은 [ADR-006](../decisions/ADR-006-separate-terrain-and-grade-separated-road-network.md)에 있다.

## RoadNode

```csharp
public readonly struct RoadNode
{
    public readonly int Id;
    public readonly Vector2Int Coordinates;
    public readonly int Level;

    public RoadNode(int id, Vector2Int coordinates, int level);
}
```

`Id`는 네트워크 안의 노드 식별자이고 목록 인덱스가 아니다. `Coordinates`는 수평 좌표, `Level`은 도로의 절대 높이 층이다. `Level`은 지형 바닥 높이와 별도로 저장하며, Unity 거리로 변환하는 기능은 없다.

작은 읽기 전용 값 타입으로 저장·조회 시 값이 복사된다. 같은 좌표·높이의 노드도 ID가 다르면 현재 저장 API는 허용한다. 생성자는 ID의 부호, 좌표 범위, 높이 범위를 검사하지 않는다. 이는 검사 부재에 대한 구현 설명이며, 모든 값의 조립·주행 유효성을 보장하지 않는다.

## RoadNetwork

```csharp
public class RoadNetwork
{
    public RoadNetwork();
    public void AddNode(RoadNode node);
    public bool TryGetNode(int id, out RoadNode node);
    public void ConnectBidirectional(int firstNodeId, int secondNodeId);
    public bool HasDirectConnection(int fromNodeId, int toNodeId);
    public bool HasPath(int startNodeId, int destinationNodeId);
}
```

| 메서드 | 현재 동작 |
|---|---|
| `AddNode` | ID가 새로우면 추가. 같은 ID가 이미 있으면 `ArgumentException`을 던지고 기존 노드를 유지 |
| `TryGetNode` | ID를 찾으면 `true`와 노드 값 반환. 없으면 `false`와 `default(RoadNode)` 반환 |
| `ConnectBidirectional` | 두 ID의 노드가 모두 존재하면 두 방향의 직접 연결을 추가. 기존 연결 집합은 유지 |
| `HasDirectConnection` | 출발 ID의 연결 집합에 도착 ID가 포함되어 있는지 반환. 출발 집합이 없으면 `false` |
| `HasPath` | 출발·도착 중 없는 ID가 있으면 `false`. 두 ID가 존재하면 저장된 직접 연결을 탐색해 도달 여부 반환. 등록된 노드는 자기 간선 없이 이동 0회로 자기 자신에 도달 |

`default(RoadNode)`은 ID 0, 좌표 `(0, 0)`, 높이 0이다. 조회 실패를 노드 값으로 구분하지 말고 `TryGetNode`의 bool 결과를 먼저 확인한다.

### 연결 추가와 조회

`ConnectBidirectional`은 첫 번째 노드, 두 번째 노드 순서로 존재를 검사한다. 없는 노드가 있으면 `ArgumentException`을 던지며, 두 검사가 끝나기 전에는 연결을 저장하지 않는다. 현재 저장 형식은 `Dictionary<int, HashSet<int>>`이며, 각 키의 집합에 상대 ID를 추가한다. 집합을 새로 만들 때 Dictionary에 등록하고, 이미 등록된 집합은 재사용한다.

`17 ↔ 42` 뒤 `42 ↔ 90`을 추가하면 직접 연결은 네 방향 `17 → 42`, `42 → 17`, `42 → 90`, `90 → 42`다. `17 → 90`의 직접 연결을 자동으로 추가하지 않는다. 이 경우 `HasDirectConnection(17, 90)`은 `false`, `HasPath(17, 90)`은 `true`다.

연결 추가는 좌표 인접성, 높이 차이, 지형·물, 모듈 포트나 차량 통과 조건을 검사하지 않는다. 명시한 연결이 저장되었다는 사실과 물리적으로 주행 가능한 도로인지는 구분한다.

### 도달 가능성 조회

현재 `HasPath`는 먼저 출발·도착 ID의 존재를 검사하며, 없는 ID가 있으면 `false`를 반환한다. 두 ID가 존재하면 호출마다 큐와 방문 ID 집합을 만들고, 시작 ID를 방문 집합에 기록한 뒤 큐에 넣는다. 큐에서 꺼낸 ID가 도착 ID와 같으면 `true`를 반환한다. 연결 집합이 없으면 다음 후보를 확인하며, 이웃도 방문 집합에 처음 추가된 경우에만 큐에 넣는다. 연결 데이터는 변경하지 않는다.

방문 처리를 큐에 넣는 시점으로 옮겨 각 ID를 최대 한 번 예약하도록 개선했다. 이후 존재 검사를 탐색 진입 시 수행하도록 정리했다. 지형과 물 위 도로의 분리 보관 테스트까지 추가된 시점에 63개 Editor 테스트의 GREEN을 확인했다. 다음 포트 테스트의 RED 실행에서도 기존 63개는 모두 통과했다.

자기 도달은 등록된 노드에서만 성립한다. 노드 17을 등록한 테스트에서 자기 직접 연결은 없지만 `HasPath(17, 17)`은 `true`, 등록하지 않은 `HasPath(999, 999)`는 `false`임을 확인했다. 과거 없는 동일 ID의 조회에서 `true`를 반환하던 동작은 수정되었다.

현재 `TryGetNode`는 노드 목록을 선형 검색한다. 출발·도착 ID를 탐색 전에 확인하도록 정리해, BFS에서 꺼내는 모든 ID에 대해 선형 검색을 반복하지 않는다. 저장된 연결의 양쪽 ID는 `ConnectBidirectional`에서 확인하며 노드 삭제 API는 없다. 이 불변 조건에 따라 탐색 중에는 노드 존재 검사를 반복하지 않는다.

## 검증한 동작과 남은 경계 사례

현재 RoadNetwork Editor 테스트는 다음 13개 사례를 검증한다.

- 같은 수평 좌표의 서로 다른 높이 노드 저장·조회.
- 중복 ID 거부와 기존 노드 유지.
- 정상 조회 후 같은 `out` 변수로 조회에 실패하면 이전 노드가 남지 않음.
- 명시한 양방향 직접 연결과 연결하지 않은 노드의 비연결. 두 번째 연결을 추가해도 기존 연결이 유지됨.
- 여러 구간을 통한 양방향 도달과 직접 연결 조회의 구분. 연결된 출발점에서 고립된 노드로, 고립된 출발점에서 연결된 노드로의 도달 실패.
- 자기 직접 연결 없이 등록된 노드의 자기 도달 성립, 없는 동일 ID 및 한쪽 ID만 없는 조회의 도달 거부.
- 서로 다른 높이에서 교차하는 두 도로의 각 내부 경로 성립과 도로 사이 비연결. 아래 도로 `17 ↔ 90`과 위 도로 `120 ↔ 180`은 양방향으로 도달한다. 같은 수평 위치의 `42 ↔ 150`과 서로 다른 도로 끝점 `17 ↔ 180`에는 양방향으로 도달하지 못한다. `42 ↔ 150`의 직접 연결도 양방향으로 없다.
- 램프용 중간 노드 `200`을 추가하고 `90 ↔ 200`만 연결한 상태까지는 `17 ↔ 180`의 양방향 도달이 없다. `200 ↔ 180`까지 연결하면 양방향 도달이 생기며, 교차 중심 `42 ↔ 150`도 경로로 이어진다. 연결 후에도 교차 중심의 직접 연결은 양방향으로 없으며, 기존 각 도로 내부 경로도 양방향으로 유지된다.
- 터널 앞뒤 도로 `10 ↔ 100`은 내부 연결을 하나만 추가한 상태까지는 양방향 도달이 없고, 입구·내부·출구 연결을 완성하면 양방향 도달한다. 입구 `17`과 출구 `90`의 직접 연결은 없다. 터널 내부 `42`와 지표 도로 중앙 `150` 사이에는 직접 연결과 경로가 없으며 지표 도로 내부 경로는 유지된다. 모든 조회를 양방향으로 검증한다.
- 바닥 `[3, 0, 3]`, 종류 `[Grass, Water, Grass]`, 수면 `2`인 지형과 높이 `3`인 명시적 도로를 별도로 보관한다. 도로 연결 전후에 지형 종류·바닥·수면이 유지되며, 가운데 도로의 좌표·높이와 양방향 경유 경로를 확인한다. 물을 고려하는 배치 검사나 교량의 물리적 유효성을 검증한 것은 아니다.

- 두 번째 노드가 없는 `ConnectBidirectional(17, 999)` 요청의 예외와 상태 보존.
- 첫 번째 노드가 없는 `ConnectBidirectional(999, 17)` 요청의 예외와 상태 보존.
- 양쪽 노드가 없는 `ConnectBidirectional(998, 999)` 요청의 예외와 상태 보존.

위 세 연결 입력 사례는 각각 노드 `17`, `42`와 유효한 `17 ↔ 42` 연결을 가진 새 네트워크에서 시작한다. `ArgumentException`, 기존 직접 연결의 양방향 유지, 실패한 요청의 양방향 직접 연결 부재, 없는 노드가 추가되지 않음과 기존 두 노드의 전체 값 보존을 확인한다. `HasPath`는 없는 ID를 먼저 거부하므로 부분 저장 방지는 `HasDirectConnection`으로 검사한다. 2026-10-10 현재 소스로 전체 Editor 테스트 89개 통과, 실패·스킵·미결정 0개를 확인했다. 기존 예외 처리의 회귀 GREEN이며 새 기능의 RED → GREEN 기록은 아니다.

중복 연결과 자기 연결의 별도 테스트는 아직 없다. 현재 `HashSet` 구현은 중복 추가를 한 항목으로 유지하고 자기 연결도 저장할 수 있으나, 이 경계 동작을 게임 규칙으로 확정한 것은 아니다.

## RoadPort: 논리 경계의 높이·방향·인접성 비교

소스는 [RoadPort.cs](../../Assets/Scripts/Core/Blueprint/RoadPort.cs)와 [RoadPortMatcher.cs](../../Assets/Scripts/Core/Blueprint/RoadPortMatcher.cs), 테스트는 [RoadPortTests.cs](../../Assets/Tests/Editor/RoadPortTests.cs)에 있다. 값 타입, 경계 높이의 동등 비교, X·Y축 반대 방향과 방향에 맞는 이웃 셀 판정을 구현했다. 테스트는 X·Y축 정상 접속, 높이·방향 불일치와 두 축의 잘못된 위치를 양방향으로 검사한다.

```csharp
public enum RoadPortSide { PositiveX, NegativeX, PositiveY, NegativeY }

public readonly struct RoadPort
{
    public readonly Vector2Int CellCoordinates;
    public readonly RoadPortSide Side;
    public readonly int BoundaryLevel;

    public RoadPort(Vector2Int cellCoordinates, RoadPortSide side, int boundaryLevel);
}

public static class RoadPortMatcher
{
    public static bool MatchesLogicalBoundary(RoadPort first, RoadPort second);
}
```

`CellCoordinates`는 소속 셀의 수평 격자 주소이고 실제 포트 접속점 좌표가 아니다. `Side`는 수평 격자의 증가·감소 방향이며, `PositiveY`도 수직 높이를 뜻하지 않는다. `BoundaryLevel`은 포트 끝점 주행면의 절대 경계 높이다. `RoadNode.Level`을 자동 복사하지 않으며 경사 모듈의 양끝 경계 높이는 서로 다를 수 있다. 생성자는 전달한 값을 저장하고 입력 범위를 검사하지 않는다.

현재 `MatchesLogicalBoundary`는 경계 높이가 다르면 먼저 `false`를 반환하고, 같으면 `(first.Side, second.Side)`의 튜플 `switch`로 방향 쌍을 검사한다. 네 반대 방향 쌍에서 두 번째 셀 좌표가 첫 번째 셀에 방향별 오프셋을 더한 값과 같은지 반환한다. 첫 방향에 따른 오프셋은 `PositiveX = (1, 0)`, `NegativeX = (-1, 0)`, `PositiveY = (0, 1)`, `NegativeY = (0, -1)`이다. 나머지 방향 쌍은 `false`다. 네트워크나 지형을 참조하거나 변경하지 않는다. 이 판정은 논리 경계의 높이·방향·인접성 조건이며 실제 포트 정렬·폭·단면·공간과 차량 주행은 별도 검증이다. `RoadNetwork.ConnectBidirectional`의 명시적 연결 저장 계약도 유지한다.

첫 테스트는 `(2, 5)/PositiveX`와 `(3, 5)/NegativeX`를 고정하고 경계 높이 `1/1`이면 `true`, `1/2`이면 `false`를 기대한다. 2026-10-09 뼈대 구현에서 같은 높이 사례가 실패하는 RED를 확인했다. 사용자 높이 비교 구현 후 그 시점 소스로 Editor 테스트 전체 65개 통과, 실패·스킵·미결정 0개의 GREEN을 확인했다. 정방향·역방향 조회 모두 통과했다. 이번 검증 범위는 경계 높이 비교다.

이어 두 셀 주소와 경계 높이 `2/2`, 첫 방향 `PositiveX`를 고정하고 두 번째 방향만 바꾸는 네 사례를 추가했다. `NegativeX`는 `true`, 나머지 세 방향은 `false`를 기대한다. 그 시점 높이 비교만 하는 소스로 Editor 테스트 전체 69개 중 66개 통과·3개 실패, 스킵·미결정 0개의 RED를 확인했다. 잘못된 방향의 세 사례에서도 `true`를 반환한 것이 실패 원인이다. 사용자 방향 판정 구현 후 전체 69개 통과의 GREEN을 확인했다. 이후 조기 반환과 튜플 `switch`로 정리한 그 시점 소스도 전체 69개 통과, 실패·스킵·미결정 0개였다.

Y축 회귀 테스트의 다섯 사례를 추가한 그 시점 소스로 Editor 테스트 전체 74개 통과, 실패·스킵·미결정 0개를 확인했다. 당시 포트의 열한 사례 모두 정방향·역방향 조회가 통과했다. Y축의 정상 접속, 같은 방향·직각 방향 및 다른 높이의 거부를 보강했다.

X축 인접성의 여섯 사례를 추가한 그 시점 소스로 Editor 테스트 전체 80개 중 75개 통과·5개 실패, 스킵·미결정 0개의 RED를 확인했다. 같은 높이의 `PositiveX/NegativeX` 쌍에서 잘못된 위치의 다섯 사례도 `true`가 반환돼 실패했다. 사용자 좌표 판정 구현 후 그 시점 소스로 전체 80개 통과, 실패·스킵·미결정 0개의 GREEN을 확인했다.

Y축 위치 회귀의 여섯 사례도 추가한 그 시점 소스로 Editor 테스트 전체 86개 통과, 실패·스킵·미결정 0개를 확인했다. 포트의 23개 사례 모두 정방향·역방향 조회가 통과했다. 같은 셀, 두 칸 거리, 뒤쪽 이웃, 다른 축의 이웃과 대각선인 잘못된 위치를 두 축 모두에서 거부한다. 이 결과는 논리 경계 조건의 검증이며 실제 모듈의 형식·기하·공간과 차량 주행은 포함하지 않는다.
