# RoadLayout API

`Blueprint.Roads`에 한 셀 무회전 도로 모듈을 수동 배치하고, 해석한 포트별 경계 노드와 명시적 내부·외부 연결을 구성한다. 소스는 [RoadLayout.cs](../../Assets/Scripts/Core/Blueprint/RoadLayout.cs), 고정 사례는 [ManualRoadBlueprint.cs](../../Assets/Scripts/Core/Blueprint/ManualRoadBlueprint.cs)다. [ADR-007](../decisions/ADR-007-node-module-placement-and-port-contracts.md)의 역할 분리를 경계 포트만 있는 최소 모델로 구현했다.

지형과 도로는 같은 `Blueprint`가 보관하는 별도 데이터다. 도로 등록·연결은 지형 바닥·종류·수면을 변경하지 않는다. 자동 접속·노드 병합·도로 생성·프리팹 조립은 하지 않는다. 포트의 값·좌표·입력 검증은 [RoadNetwork API](road-network.md#한-셀-배치의-무회전-포트-해석)를 함께 읽는다.

## 공개 API

```csharp
public sealed class RoadLayout
{
    public RoadLayout(int width, int height);
    public PlacedRoadModule AddModule(int moduleId, RoadModulePlacement placement,
        params RoadPortDefinition[] definitions);
    public void ConnectInternal(int moduleId, int firstPortIndex, int secondPortIndex);
    public bool TryConnectExternal(int firstModuleId, int firstPortIndex,
        int secondModuleId, int secondPortIndex);
    public IReadOnlyList<PlacedRoadModule> GetModules();
    public IReadOnlyList<RoadNode> GetNodes();
    public IReadOnlyList<RoadConnection> GetConnections();
    public bool TryGetNode(int nodeId, out RoadNode node);
    public bool HasPath(int startNodeId, int destinationNodeId);
}

public sealed class PlacedRoadModule
{
    public readonly int Id;
    public readonly RoadModulePlacement Placement;
    public int PortCount { get; }
    public RoadPortDefinition GetDefinition(int portIndex);
    public ResolvedRoadPort GetPort(int portIndex);
    public int GetNodeId(int portIndex);
}

public readonly struct RoadConnection
{
    public readonly int FirstNodeId;
    public readonly int SecondNodeId;
    public readonly bool IsExternal;
    public RoadConnection(int firstNodeId, int secondNodeId, bool isExternal);
}
```

`RoadLayout`의 크기는 배치 셀 범위 검사에 사용한다. `Blueprint` 생성자는 자신의 크기로 빈 `RoadLayout`을 만들어 읽기 전용 `Roads` 프로퍼티에 둔다. `PlacedRoadModule`은 `AddModule`로만 얻으며 공개 API에서 배치·정의·해석한 포트·노드 ID를 변경할 수 없다. `Id`는 배치 식별자이며 프리팹이나 공통 모듈 정의의 ID가 아니다.

## 배치와 경계 노드

`AddModule`은 다음 순서로 등록한다.

1. 중복 모듈 ID, 소속 셀 범위와 정의 배열을 검사한다.
2. 전달받은 정의 배열을 복사한다. 이후 호출자가 원본 배열의 항목을 교체해도 저장한 정의는 바뀌지 않는다.
3. 모든 정의를 `ResolveUnrotated`로 해석하고 필요한 노드 ID를 계산한다. 이 단계가 끝나기 전에는 노드·포트·모듈을 저장하지 않는다.
4. 포트마다 별도 `RoadNode`를 만든다. `Coordinates`는 포트의 `ContactPoint`, `Level`은 `BoundaryLevel`이다. 노드 ID 아래에 해당 `RoadPort`도 저장한다.
5. 모듈을 등록하고 다음 노드 ID를 갱신한다. 내부·외부 연결은 추가하지 않는다.

노드 ID는 레이아웃 안에서 0부터 할당하며 모듈 ID나 포트 인덱스와 별개다. 같은 점·높이의 포트도 별도 노드 ID를 유지한다. 같은 셀에 여러 모듈 배치를 둘 수 있으며 물리 공간 충돌을 검사하지 않는다. 각 포트의 상대 높이로 끝점 높이를 유도하므로 램프 양끝은 다른 높이를 가질 수 있다.

현재 한 배치는 하나 이상의 경계 포트를 필요로 한다. 내부 교차점·경유점이나 허용 내부 통행을 담은 별도 모듈 정의 타입은 아직 없다. 배치 셀만 범위 검사하며 로컬 접속점을 반 칸, 셀 테두리나 청사진 안으로 제한하지 않는다.

## 명시적 내부·외부 연결

| 요청 | 검사와 효과 |
|---|---|
| `ConnectInternal` | 지정한 같은 모듈의 두 포트 인덱스를 확인하고 대응 노드 사이 양방향 간선 추가. 다른 끝점 높이 허용 |
| `TryConnectExternal` | 두 모듈·포트 인덱스를 확인한 뒤, 서로 다른 모듈이고 접속점이 정확히 같으며 논리 경계가 호환되면 양방향 외부 간선 추가 후 `true` |
| 외부 접속 불일치 | 같은 모듈, 접속점 불일치 또는 Matcher 실패이면 `false`. 기존 노드·모듈·간선·경로 유지 |
| 이미 있는 직접 연결 | 새 표시 간선을 추가하지 않음. 정방향·역방향 반복 요청도 스냅샷에는 한 항목으로 유지 |

내부 연결은 호출자가 명시한 포트쌍을 저장하는 계약이다. 프리팹 형상이나 별도 내부 통행 whitelist를 검사하지 않는다. 연결을 요청하기 전에는 같은 모듈의 포트도 서로 도달하지 못한다.

외부 접속은 `ContactPoint.Equals`로 `Vector2` 성분을 정확히 비교한다. Unity의 근사 비교 `==`나 허용 오차 판정을 쓰지 않는다. 이어 `MatchesLogicalBoundary`가 같은 끝점 높이·반대 방향·바로 이웃 소속 셀을 검사한다. 이 조건이 맞아도 `TryConnectExternal`을 호출하기 전에는 간선이 생기지 않는다. 도로 형식·폭·단면·경사·차량 여유 공간은 검사하지 않는다.

`RoadConnection`은 양방향 접속을 표시하는 한 항목이다. `FirstNodeId`·`SecondNodeId`는 최초로 추가한 요청의 순서이며, 순서가 통행 방향을 뜻하지 않는다. `IsExternal = false`는 모듈 내부 연결, `true`는 서로 다른 배치의 접속이다. 외부 접속의 두 노드는 같은 점·높이를 공유하므로 길이 0인 관계일 수 있다. 미리보기는 이를 청록색 링으로 표시한다. 도로 길이나 비용·지도 선분으로 해석하는 규칙은 아직 없다.

## 조회와 스냅샷

| 조회 | 반환 계약 |
|---|---|
| `GetModules` | 조회 시점의 모듈 참조를 새 배열에 복사. 모듈의 공개 값은 읽기 전용이며 배열 항목 변경은 내부 Dictionary를 바꾸지 않음 |
| `GetNodes` | 전체 노드 값의 배열 복사. 이후 등록이나 반환 배열 항목 변경과 독립 |
| `GetConnections` | 전체 연결 값의 배열 복사. 이후 연결이나 반환 배열 항목 변경과 독립 |
| `TryGetNode` | ID가 있으면 `true`와 값, 없으면 `false`와 `default` |
| `HasPath` | 저장한 양방향 간선으로 도달 여부 조회. 없는 ID는 `false`, 존재하는 ID의 자기 도달은 간선 없이 `true` |

비어 있지 않은 전체 조회는 호출마다 배열을 할당한다. 모듈 조회 순회 순서나 노드 ID 할당 순서를 자동 생성·지도 정책으로 확정하지 않는다. 삭제·이동·갱신·캐시·직렬화 API는 아직 없다.

## 예외와 상태 보존

| 입력 | 현재 결과 |
|---|---|
| 레이아웃 `width` / `height`가 0 이하 | 해당 매개변수의 `ArgumentOutOfRangeException` |
| 등록한 `moduleId` 재사용 | `ArgumentException`, 매개변수 `moduleId` |
| 소속 셀이 레이아웃 수평 범위 밖 | `ArgumentOutOfRangeException`, 매개변수 `placement` |
| 정의 배열 `null` / 빈 배열 | 각각 `ArgumentNullException` / `ArgumentException`, 매개변수 `definitions` |
| 로컬 NaN·무한대·미정의 방향 | Resolver의 `ArgumentException`, 매개변수 `definition` |
| 끝점 높이 합 또는 다음 노드 ID 계산의 정수 오버플로 | `OverflowException` |
| 없는 모듈에 내부·외부 연결 요청 | `ArgumentException`, 매개변수 `moduleId` |
| 음수 또는 `PortCount` 이상인 포트 인덱스 조회·연결 | `ArgumentOutOfRangeException`, 매개변수 `portIndex` |

`AddModule`은 검증·해석 후 저장하므로 뒤쪽 정의가 실패해도 앞쪽 포트의 노드나 모듈을 부분 등록하지 않고 다음 노드 ID를 소비하지 않는다. 연결 요청도 필요한 모듈·인덱스를 확인한 뒤 간선을 저장한다.

`RoadModulePlacement` 값 보관과 Resolver는 음수 셀 주소를 허용하지만 `RoadLayout.AddModule`은 소속 셀을 자신의 범위 안으로 제한한다. 도로 기준 높이·오프셋의 음수는 레이아웃에서도 별도 금지하지 않는다. 모듈 ID의 부호, 중복 로컬 정의·접속점, 같은 포트를 내부 연결하는 요청은 별도 금지 규칙을 두지 않으며 게임 정책으로 검증한 것은 아니다.

## ManualRoadBlueprint

`ManualRoadBlueprint.Create()`는 호출마다 새 `Blueprint(7, 6)`을 만들고 다음 고정 도로를 구성한다. 지형은 모두 높이 0의 Grass이며 수면은 `null`이다. 이는 실제 프리팹 규격의 데이터가 아닌 논리 연결 사례다.

| 모듈 ID | 소속 셀 | 기준 높이 | 포트와 내부 통행 |
|---|---|---|---|
| 1, 2 | `(1,1)`, `(2,1)` | 각각 1 | X 직선. 각 양끝 오프셋 0, 내부 양방향 연결 |
| 3 | `(1,3)` | 1 | X 직선, 양끝 높이 1 |
| 4 | `(2,3)` | 1 | X 램프. `-X` 오프셋 0 / `+X` 오프셋 1 → 끝점 높이 1 / 2 |
| 5 | `(3,3)` | 2 | X 직선, 양끝 높이 2 |
| 6 | `(5,4)` | 1 | 아래 X 직선 |
| 7 | `(5,4)` | 3 | 위 Y 직선. 모듈 6과 외부 접속 없음 |

모듈마다 경계 포트 2개와 내부 간선 1개가 있다. 외부 접속은 `1:포트1 ↔ 2:포트0`, `3:포트1 ↔ 4:포트0`, `4:포트1 ↔ 5:포트0`만 요청한다. 결과는 모듈 7개·경계 노드 14개·고유 양방향 간선 10개(내부 7, 외부 3)다. 평지 두 모듈과 램프 세 모듈은 각각 양방향으로 이어지며, 같은 셀의 아래·위 도로는 각 내부 경로를 유지하면서 서로 도달하지 못한다.

필수 외부 접속이 호환되지 않으면 factory는 `InvalidOperationException`을 던진다. 생성 결과는 가변 `Blueprint`/`RoadLayout`이며 이후 호출자가 공개 API로 추가 구성할 수 있다. Scene 미리보기는 이 factory의 데이터를 그대로 읽는다. [표시 도우미](blueprint.md#blueprintscenedebug-표시-도우미)와 [진행 기록](../blueprint-progress.md)을 함께 확인한다.

## 검증과 남은 경계

2026-10-11 수동 배치·연결과 최종 미리보기 라벨 수정 후 Unity 6000.6.0f1 Editor의 전체 144개 테스트가 통과했고 실패·스킵·미결정은 0개였다. #9 완료 시점의 124개에 `RoadNode` 소수 좌표 1개, [RoadLayoutTests](../../Assets/Tests/Editor/RoadLayoutTests.cs) 15개와 [ManualRoadBlueprintTests](../../Assets/Tests/Editor/ManualRoadBlueprintTests.cs) 4개를 추가했다. 저장소 C# 검사도 14개 소스에서 구문·규칙 위반 0개였다. Scene View의 도로 기호·ID·높이 라벨과 Play Mode의 Game View Gizmos도 실제 화면 캡처로 확인했으며 Console 오류·경고는 0개였다.

- 정의 원본 배열의 외부 수정 격리, 포트별 접속점·끝점 높이와 서로 다른 노드 ID, 노드·모듈·간선 스냅샷을 확인했다.
- 명시 전 내부 비연결, 내부 연결의 양방향 도달, 외부 접속 후 별도 ID 유지·양방향 도달을 확인했다.
- 없는 모듈·잘못된 인덱스의 연결 예외와 상태 보존, 뒤쪽 정의의 NaN·높이 오버플로 후 부분 등록 부재와 다음 노드 ID 보존을 확인했다.
- 높이·방향·셀 인접성·접속점 불일치로 외부 접속을 거부하며, `0.000001f` 차이도 정확 비교로 거부하고 기존 내부 경로·간선을 유지했다.
- 수동 평지·램프의 접속점·높이·양방향 경로, 입체교차 층 사이 비연결과 도로 등록 후 지형·수면 보존을 확인했다.

레이아웃 크기·중복 ID·범위 밖 배치·null/빈 정의의 예외는 구현돼 있으나 이번 RoadLayout 테스트에 독립 사례는 없다. 같은 모듈의 외부 접속 거부와 내부 자기 연결도 별도 테스트가 없다. 현재 경계 포트만 있는 모델이며 내부 교차점·일방통행·차로·프리팹 선택·회전·멀티셀·도로 형식·공간·자동 생성·실제 차량 주행은 검증 범위 밖이다. 미리보기 화면 확인과 물리적 조립·주행 검증은 [진행 기록](../blueprint-progress.md#검증-기록과-한계)에 구분한다.
