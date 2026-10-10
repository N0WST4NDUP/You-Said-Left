# API 문서

현재 저장소에 구현된 API의 입력, 반환값과 경계 동작을 정리한다. 설계 결정과 앞으로 만들 기능은 [ADR 색인](../decisions/README.md), 다음 작업과 검증 상태는 [Blueprint 진행 기록](../blueprint-progress.md)에서 확인한다.

## 문서 목록

| API | 구현 범위 | 문서 |
|---|---|---|
| Blueprint | 지형 격자, 정수 높이 층, 공통 수면, 높이차 샘플링과 물 분류 | [Blueprint API](blueprint.md) |
| RoadNetwork | ID별 도로 노드·포트 저장·조회, 명시적 양방향 직접 연결, BFS 도달 여부와 논리 포트 경계 비교 | [RoadNetwork API](road-network.md) |

## Blueprint부터 읽기

1. [ADR-001](../decisions/ADR-001-blueprint-as-source-of-truth.md)의 청사진 원본 원칙, [ADR-005](../decisions/ADR-005-terrain-first-generation-with-tunnel-caves.md)의 지형 우선 방향, [ADR-006](../decisions/ADR-006-separate-terrain-and-grade-separated-road-network.md)의 입체 도로 연결·공간 계약, [ADR-007](../decisions/ADR-007-node-module-placement-and-port-contracts.md)의 노드·모듈 배치·포트 역할과 좌표 계약을 읽는다.
2. [Blueprint API](blueprint.md)에서 좌표, 절대 높이와 수면 계약을 확인한다.
3. [진행 기록](../blueprint-progress.md)에서 구현된 범위, 미정 사항과 다음 작은 작업을 확인한다.
4. 관련 소스와 Editor 테스트를 다시 읽고, [이슈 단위 작업 흐름](../development-workflow.md)에 따라 범위·제외 범위·검증 조건을 정한다. **RED 확인 → 사용자 최소 구현 → GREEN 확인 → 필요한 리팩터링·AI 리뷰**를 한 동작씩 반복한다. 기존 구현의 회귀 검증은 바로 GREEN일 수 있다. 검사 도구와 Unity 검증의 차이는 [저장소 검사 안내](../tools/repository-checks.md)를 따른다.
5. 현재 합의한 독립 범위의 완료 조건과 필요한 검증·문서 갱신을 마치면, 상위 이슈가 열려 있어도 다음 기능으로 넘어가기 전에 완료 범위·검증 결과·포함 파일과 메시지를 정리해 커밋을 추천한다. 실제 커밋과 다음 작업은 사용자 지시를 따른다.

API 문서는 호출 방법을 설명하고, ADR은 결정의 근거를 설명한다. 현재 Blueprint API에는 모듈 배치·회전, 도로·건물·동굴 공간·교량·고가도로 데이터가 없다. 별도 RoadNetwork API에는 도로 노드·포트와 직접 연결의 최소 저장 계약, BFS 도달 여부 조회와 논리 포트 경계 비교가 구현되어 있다. ADR-007에서 채택한 모듈 배치와 로컬 포트 정의, 접속점 좌표 계산, 포트와 접속 노드의 대응 및 모듈 내부·외부 연결 구성은 아직 구현되지 않았다. 경로 목록·비용 최적화, 건물 배치, 공간 검사, 종이 지도와 3D 도시 조립도 후속 범위다.

## 갱신 원칙

- 실제 [소스](../../Assets/Scripts/Core/Blueprint/)를 기준으로 작성하고, 구현 사실과 향후 방향을 구분한다.
- 시그니처, 기본값, 예외, 좌표 또는 데이터 의미를 바꾸면 해당 API 문서와 관련 테스트를 함께 확인한다.
- 테스트 파일에 검증 코드가 있다는 사실과 테스트가 실제로 통과했다는 결과를 구분한다. 실행 결과 없이 통과 수를 기록하지 않는다.
- 씬 표시와 게임 동작은 소스나 저장소 검사만으로 검증됐다고 쓰지 않는다. Play Mode와 Game View 확인 여부를 따로 기록한다.
