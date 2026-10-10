# 설계 결정 기록 (ADR)

You Said Left의 기술·설계 결정과 그 근거를 기록한다. 각 문서는 하나의 결정을 다룬다.

## 색인

| 번호 | 제목 | 상태 | 결정일 |
|---|---|---|---|
| [ADR-001](ADR-001-blueprint-as-source-of-truth.md) | 청사진 데이터를 원본으로 3D 도시와 종이 지도를 만든다 | `Accepted` | 2026-10-05 |
| [ADR-002](ADR-002-grid-generation-with-modular-tiles.md) | 랜덤 도시를 격자 위에서 생성하고 Low Poly Epic City 타일로 조립한다 | `Accepted` | 2026-10-05 |
| [ADR-003](ADR-003-trait-selection-before-random-levels.md) | 랜덤 레벨 출발 전에 특성을 고르고, 특성 점수 합이 0 이하일 때만 출발한다 | `Proposed` | — |
| [ADR-004](ADR-004-roles-by-boarding-seat.md) | 역할은 시스템이 배정하지 않고 탑승한 좌석으로 정한다 | `Proposed` | — |
| [ADR-005](ADR-005-terrain-first-generation-with-tunnel-caves.md) | 지형을 먼저 생성하고 동굴을 터널 모듈로 표현한다 | `Accepted` | 2026-10-08 |
| [ADR-006](ADR-006-separate-terrain-and-grade-separated-road-network.md) | 지형과 입체 도로망을 분리하고 주행 가능한 연결을 먼저 정의한다 | `Accepted` | 2026-10-08 |
| [ADR-007](ADR-007-node-module-placement-and-port-contracts.md) | 도로 노드를 접속점으로 두고 모듈 배치와 경계 포트를 분리한다 | `Accepted` | 2026-10-11 |

새 ADR을 추가하면 이 표에 한 줄을 추가한다. 번호 오름차순으로 정렬한다.

현재 구현과 진행 상황은 [Blueprint API](../api/blueprint.md)와 [청사진 작업 현황](../blueprint-progress.md)을 참고한다. ADR-001·002의 구현 전 설명은 결정 당시의 기록이며, 현재 코드 상태를 의미하지 않는다. ADR-005는 지형 생성 순서와 동굴 표현을, ADR-006은 입체 도로의 연결·공간 계약을 보완한다. ADR-007은 그래프 점·모듈 배치·경계 포트의 관계와 좌표 원본을 구체화한다. 채택한 설계와 현재 구현을 구분해 읽는다.

ADR-002·005·006의 과거 에셋 조사 제한은 사용자의 철회 이후 현재 지침으로 재적용하지 않는다. 변경 경위는 [ADR-007의 변경 범위](ADR-007-node-module-placement-and-port-contracts.md#변경-범위), 현재 작업 권한은 [AGENTS.md](../../AGENTS.md)를 따른다. 기존 Accepted 본문은 결정 당시 기록으로 보존한다.

## 작성 규칙

- **파일명**: `ADR-001-kebab-case-title.md` 형식. 번호는 3자리 순번이고 재사용하지 않는다.
- **제목**: 주제가 아니라 결정을 서술한다. 예: "차량 입력" 대신 "차량 입력을 Input System 액션으로 처리"
- **본문 구조**: 요구사항/문제 → 판단 근거 → 설계 → 결과
- **근거**: 직접 확인한 사실, 측정 결과, 출처가 있는 자료만 쓴다. 모르는 부분은 비워 두고 갭으로 표시한다.

## 상태

| 상태 | 의미 |
|---|---|
| `Proposed` | 검토 중. 결과 섹션에는 측정 계획만 쓴다 |
| `Accepted` | 결정 확정. 구현 전이거나 진행 중 |
| `Validated` | 구현과 검증 완료. 실측 결과를 기록한다 |
| `Rejected` | 검토했으나 채택하지 않음 |
| `Superseded by ADR-NNN` | 이후 결정으로 대체됨 |

`Accepted` 이후의 ADR은 내용을 고치지 않는다. 결정이 바뀌면 새 ADR을 작성하고, 기존 문서의 상태를 `Superseded by ADR-NNN`으로 바꾼다. 오타와 링크 수정은 예외다.
