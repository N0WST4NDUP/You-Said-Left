# API 문서

현재 저장소에 구현된 API의 입력, 반환값과 경계 동작을 정리한다. 설계 결정과 앞으로 만들 기능은 [ADR 색인](../decisions/README.md), 다음 작업과 검증 상태는 [Blueprint 진행 기록](../blueprint-progress.md)에서 확인한다.

## 문서 목록

| API | 구현 범위 | 문서 |
|---|---|---|
| Blueprint | 지형 격자, 정수 높이 층, 공통 수면, 높이차 샘플링·물 분류와 별도 `Roads` 보관 | [Blueprint API](blueprint.md) |
| RoadNetwork / RoadPort | ID별 도로 노드·포트 저장·조회, 명시적 양방향 직접 연결, BFS 도달 여부, 논리 포트 경계 비교와 한 셀 무회전 포트 해석 | [RoadNetwork API](road-network.md) |
| RoadLayout / ManualRoadBlueprint | 한 셀 모듈 배치, 포트에서 경계 노드 유도, 명시적 내부·외부 접속과 고정 수동 청사진 | [RoadLayout API](road-layout.md) |

## Blueprint부터 읽기

1. [ADR-001](../decisions/ADR-001-blueprint-as-source-of-truth.md)의 청사진 원본 원칙, [ADR-005](../decisions/ADR-005-terrain-first-generation-with-tunnel-caves.md)의 지형 우선 방향, [ADR-006](../decisions/ADR-006-separate-terrain-and-grade-separated-road-network.md)의 입체 도로 연결·공간 계약, [ADR-007](../decisions/ADR-007-node-module-placement-and-port-contracts.md)의 노드·모듈 배치·포트 역할과 좌표 계약을 읽는다.
2. [Blueprint API](blueprint.md)에서 지형 좌표·절대 높이·수면을, [RoadLayout API](road-layout.md)에서 별도 도로 배치·경계 노드·접속 계약을 확인한다.
3. [진행 기록](../blueprint-progress.md)에서 구현된 범위, 미정 사항과 다음 작은 작업을 확인한다.
4. 관련 소스와 Editor 테스트를 다시 읽고, [이슈 단위 작업 흐름](../development-workflow.md)에 따라 범위·제외 범위·검증 조건을 정한다. **RED 확인 → 사용자 최소 구현 → GREEN 확인 → 필요한 리팩터링·AI 리뷰**를 한 동작씩 반복한다. 기존 구현의 회귀 검증은 바로 GREEN일 수 있다. 검사 도구와 Unity 검증의 차이는 [저장소 검사 안내](../tools/repository-checks.md)를 따른다.
5. 현재 합의한 독립 범위의 완료 조건과 필요한 검증·문서 갱신을 마치면, 상위 이슈가 열려 있어도 다음 기능으로 넘어가기 전에 완료 범위·검증 결과·포함 파일과 메시지를 정리해 커밋을 추천한다. 실제 커밋과 다음 작업은 사용자 지시를 따른다.

API 문서는 호출 방법을 설명하고, ADR은 결정의 근거를 설명한다. `Blueprint`는 지형 격자와 별도 `RoadLayout`인 `Roads`를 보유한다. #9에서 구현한 한 셀 무회전 포트 해석에 이어, 수동 배치의 포트마다 별도 경계 노드를 유도하고 요청한 내부 연결과 검사한 외부 접속을 구성한다. 접속점과 `RoadNode.Coordinates`는 한 셀을 `1`로 놓은 논리 좌표의 `Vector2`이고, 소속 셀 주소는 `Vector2Int`다. `ManualRoadBlueprint`의 평지·램프·입체교차와 Scene 미리보기에서 같은 데이터를 읽는다.

현재 배치는 경계 포트만 가진 최소 모델이다. 내부 교차점, 프리팹 선택, 회전·멀티셀, 도로 형식·폭·여유 공간, 자동 생성·실제 조립·차량 주행·지도는 후속 범위다. 최신 실행 기록은 [진행 기록](../blueprint-progress.md#검증-기록과-한계)에 있으며, 기존 단계별 통과 수는 각 시점의 기록으로 보존한다.

## 갱신 원칙

- 실제 [소스](../../Assets/Scripts/Core/Blueprint/)를 기준으로 작성하고, 구현 사실과 향후 방향을 구분한다.
- 시그니처, 기본값, 예외, 좌표 또는 데이터 의미를 바꾸면 해당 API 문서와 관련 테스트를 함께 확인한다.
- 테스트 파일에 검증 코드가 있다는 사실과 테스트가 실제로 통과했다는 결과를 구분한다. 실행 결과 없이 통과 수를 기록하지 않는다.
- 씬 표시와 게임 동작은 소스나 저장소 검사만으로 검증됐다고 쓰지 않는다. Play Mode와 Game View 확인 여부를 따로 기록한다.
