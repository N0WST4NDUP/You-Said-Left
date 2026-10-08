# API 문서

현재 저장소에 구현된 API의 입력, 반환값과 경계 동작을 정리한다. 설계 결정과 앞으로 만들 기능은 [ADR 색인](../decisions/README.md), 다음 작업과 검증 상태는 [Blueprint 진행 기록](../blueprint-progress.md)에서 확인한다.

## 문서 목록

| API | 구현 범위 | 문서 |
|---|---|---|
| Blueprint | 지형 격자, 정수 높이 층, 공통 수면, 높이차 샘플링과 물 분류 | [Blueprint API](blueprint.md) |
| RoadNetwork | ID별 도로 노드 저장·조회, 명시적 양방향 직접 연결, 여러 구간의 도달 가능성 조회 | [RoadNetwork API](road-network.md) |

## Blueprint부터 읽기

1. [ADR-001](../decisions/ADR-001-blueprint-as-source-of-truth.md)의 청사진 원본 원칙, [ADR-005](../decisions/ADR-005-terrain-first-generation-with-tunnel-caves.md)의 지형 우선 방향, [ADR-006](../decisions/ADR-006-separate-terrain-and-grade-separated-road-network.md)의 입체 도로 연결·공간 계약을 읽는다.
2. [Blueprint API](blueprint.md)에서 좌표, 절대 높이와 수면 계약을 확인한다.
3. [진행 기록](../blueprint-progress.md)에서 구현된 범위, 미정 사항과 다음 작은 작업을 확인한다.
4. 관련 소스와 Editor 테스트를 다시 읽고, **RED 테스트 → 사용자 직접 구현 → GREEN 확인 → AI 리뷰** 순서로 한 가지 동작씩 진행한다. 검사 도구와 Unity 검증의 차이는 [저장소 검사 안내](../tools/repository-checks.md)를 따른다.

API 문서는 호출 방법을 설명하고, ADR은 결정의 근거를 설명한다. 현재 Blueprint API에는 경사 모양·회전, 도로·건물·동굴 공간·교량·고가도로 데이터가 없다. 별도 RoadNetwork API에는 도로 노드와 직접 연결의 최소 저장 계약, 여러 구간을 통한 도달 여부 조회가 구현되어 있다. 경로 목록·비용 최적화, 건물 배치, 공간 검사, 종이 지도와 3D 도시 조립은 아직 구현되지 않았다.

## 갱신 원칙

- 실제 [소스](../../Assets/Scripts/Core/Blueprint/)를 기준으로 작성하고, 구현 사실과 향후 방향을 구분한다.
- 시그니처, 기본값, 예외, 좌표 또는 데이터 의미를 바꾸면 해당 API 문서와 관련 테스트를 함께 확인한다.
- 테스트 파일에 검증 코드가 있다는 사실과 테스트가 실제로 통과했다는 결과를 구분한다. 실행 결과 없이 통과 수를 기록하지 않는다.
- 씬 표시와 게임 동작은 소스나 저장소 검사만으로 검증됐다고 쓰지 않는다. Play Mode와 Game View 확인 여부를 따로 기록한다.
- 유료 에셋 파일을 AI 입력으로 사용하지 않는다. 에셋 관련 원칙은 [ADR-002](../decisions/ADR-002-grid-generation-with-modular-tiles.md)를 따른다.
