# Blueprint API

지형 격자를 만들고 각 셀의 바닥 높이와 지형 종류를 조회·교체하는 현재 API다. 이 문서는 `YouSaidLeft.Core`의 소스와 Editor 테스트를 기준으로 작성했다. 실행 결과와 다음 작업은 [Blueprint 진행 기록](../blueprint-progress.md)을 함께 확인한다.

## 구현 범위와 설계 방향

현재 `CreateBlueprint`는 **Grass 평지 생성 → 높이 적용 → 공통 수면 아래 셀을 Water로 분류**한다. 경사 타일, 도로 연결, 출발지·목적지, 건물, 터널형 동굴, 교량·고가도로, 장식물, 입체 도로망, 종이 지도와 3D 도시 조립은 이 API에 구현되지 않았다. `ApplyTraits`는 비어 있는 private 메서드이고 생성 흐름에서 호출되지 않는다.

청사진을 3D 도시와 지도의 공통 원본으로 쓰는 결정은 [ADR-001](../decisions/ADR-001-blueprint-as-source-of-truth.md), 모듈형 타일 조립 방향은 [ADR-002](../decisions/ADR-002-grid-generation-with-modular-tiles.md), 지형 우선 방향은 [ADR-005](../decisions/ADR-005-terrain-first-generation-with-tunnel-caves.md), 입체 도로망의 후속 계약은 [ADR-006](../decisions/ADR-006-separate-terrain-and-grade-separated-road-network.md)에 있다. 이 결정들의 최종 목표와 현재 지형 API를 구분해서 읽는다.

### 현재 지형 격자와 미래 입체 도로망의 경계

- 현재 `TerrainCell[,]`은 수평 좌표마다 바닥 높이 하나를 저장한다. 터널 내부의 빈 공간이나 같은 위치의 여러 도로 높이를 표현하지 않는다.
- 미래 도로망은 지형과 별도로 도로의 높이·구간·접속점을 가지며, 겹침과 실제 연결을 구분해야 한다. 구체 타입·API·저장 형식은 아직 없다. 이 문서의 C# 시그니처는 현재 구현에만 해당한다.
- `GetNeighborCoordinates`는 도로 탐색의 연결 간선이 아니다. 이웃 좌표와 높이가 같아도 도로가 실제로 이어진다는 뜻은 아니다.
- `TerrainKind.Water`는 현재 생성기의 지형 분류다. 미래 교량·고가 구간의 통행 가능 여부를 아래 셀의 종류만으로 판정하지 않는다.
- 도로 그래프의 도달 가능성, 자동 생성의 유효성, 모듈 연결·여유 공간, 실제 차량 주행은 각각 검사한다. 그래프 연결만으로 지형 개구부나 콜라이더가 생성되지는 않는다.

다음 구현은 자동 생성기보다 작은 수동 입체 도로 연결 사례부터 시작한다. 단계와 아직 정할 계약은 [작업 현황](../blueprint-progress.md)에 있다.

| 타입 | 역할 | 소스 |
|---|---|---|
| `TerrainKind`, `TerrainCell` | 지형 종류와 셀의 절대 바닥 높이 | [TerrainCell.cs](../../Assets/Scripts/Core/Blueprint/TerrainCell.cs) |
| `Blueprint` | 크기·기준층·수면 메타데이터와 가변 셀 격자 | [Blueprint.cs](../../Assets/Scripts/Core/Blueprint/Blueprint.cs) |
| `BlueprintGenerator` | 높이차 함수를 적용하고 물을 분류 | [BlueprintGenerator.cs](../../Assets/Scripts/Core/Blueprint/BlueprintGenerator.cs) |
| `PerlinLevelSampler` | 좌표로부터 Perlin 노이즈 기반 정수 높이차 계산 | [PerlinLevelSampler.cs](../../Assets/Scripts/Core/Blueprint/PerlinLevelSampler.cs) |
| `TerrainLevelQuantizer` | 노이즈 값을 정수 높이차로 양자화 | [TerrainLevelQuantizer.cs](../../Assets/Scripts/Core/Blueprint/TerrainLevelQuantizer.cs) |

## 좌표와 높이 계약

- 저장 배열은 `TerrainCell[width, height]`이고 인덱스는 `[x, y]`다. 유효 범위는 `0 <= x < Width`, `0 <= y < Height`다.
- `Width`는 x 방향 셀 수, `Height`는 y 방향 셀 수다. `Height`와 격자 좌표 y는 지형의 수직 높이가 아니다.
- `BaseLevel`은 초기 바닥의 정수 기준층이다. 생성기는 `BaseLevel + sampleLevelOffset(x, y)`를 각 셀의 `Level`에 저장한다.
- `TerrainCell.Level`은 셀 바닥의 **절대 높이 층**이다. 생성 이후 기준층에 더할 상대 높이차가 아니다.
- `Blueprint.WaterLevel`은 격자 전체에 적용하는 **절대 수면 높이 층**이다. 물 셀도 `Level`에 수면이 아닌 바닥 높이를 유지한다.
- 층에서 Unity 거리로 바꾸는 규칙은 Core API에 없다. 디버그 도우미는 표시용 `_levelHeight`를 곱한다. 이 값은 게임의 실제 타일 규격을 확정하지 않는다.

예를 들어 `BaseLevel = 2`, 높이차 `0, 1, 2`, `WaterLevel = 3`이면 생성 결과는 다음과 같다.

| 좌표 | `Level` | `Kind` | 수면 |
|---|---|---|---|
| `(0, 0)` | 2 | `Water` | 3 |
| `(1, 0)` | 3 | `Grass` | 3 |
| `(2, 0)` | 4 | `Grass` | 3 |

**수면과 높이가 같은 셀은 Grass다.** 물 분류 조건은 `Level < WaterLevel`이며 `<=`가 아니다.

## TerrainKind와 TerrainCell

```csharp
public enum TerrainKind { Grass, Water }

public readonly struct TerrainCell
{
    public readonly int Level;
    public readonly TerrainKind Kind;

    public TerrainCell(int level, TerrainKind kind = TerrainKind.Grass);
}
```

`TerrainCell`은 읽기 전용 값 타입이다. 셀을 바꿀 때는 새 값을 만들어 `Blueprint.TrySetCell`에 전달한다. 기본 `kind`는 `Grass`이며, 현재 생성자는 `level`의 부호나 enum 값의 유효성을 검사하지 않는다.

`default(TerrainCell)`은 `Level = 0`, `Kind = Grass`다. 조회 실패 시에도 이 값이 반환되므로, `TryGetCell`의 bool 결과를 먼저 확인해야 한다.

## Blueprint

```csharp
public class Blueprint
{
    public readonly int Width;
    public readonly int Height;
    public readonly int BaseLevel;
    public readonly int? WaterLevel;

    public Blueprint(int width, int height, int baseLevel = 0, int? waterLevel = null);
    public bool TryGetCell(int x, int y, out TerrainCell cell);
    public bool TrySetCell(int x, int y, TerrainCell cell);
    public IEnumerable<Vector2Int> GetNeighborCoordinates(int x, int y);
}
```

### 생성자

`new Blueprint(...)`는 모든 셀을 `new TerrainCell(baseLevel)`로 초기화한다. `waterLevel`을 지정해도 생성자 자체는 물을 분류하지 않으며, 초기 셀은 모두 Grass다. 자동 분류가 필요하면 `BlueprintGenerator.CreateBlueprint`를 사용한다.

| 입력 | 기본값 | 현재 검사 |
|---|---|---|
| `width` | 필수 | 0 이하이면 `ArgumentOutOfRangeException`, `ParamName = "width"` |
| `height` | 필수 | 0 이하이면 `ArgumentOutOfRangeException`, `ParamName = "height"` |
| `baseLevel` | `0` | 음수이면 `ArgumentOutOfRangeException`, `ParamName = "baseLevel"` |
| `waterLevel` | `null` | 값 범위나 `baseLevel`과의 관계를 검사하지 않음 |

검사는 `width` → `height` → `baseLevel` 순서다. 여러 입력이 잘못되면 앞에서 확인한 입력의 예외가 먼저 발생한다. 양수 크기의 메모리 상한을 별도로 제한하는 로직은 없다.

네 public 필드는 생성 후 바뀌지 않는다. 셀은 `TrySetCell`로 바꿀 수 있으므로 `Blueprint` 전체가 불변 데이터인 것은 아니다. 셀을 바꿔도 `BaseLevel`이나 `WaterLevel`이 다시 계산되지 않는다.

### 셀 조회·교체

| 메서드 | 유효 좌표 | 범위 밖 좌표 |
|---|---|---|
| `TryGetCell` | 셀 값을 `out cell`에 복사하고 `true` 반환 | `cell = default`, `false` 반환 |
| `TrySetCell` | 해당 좌표의 셀 전체를 교체하고 `true` 반환 | 기존 셀을 바꾸지 않고 `false` 반환 |

두 메서드는 범위 밖 좌표에 예외를 던지지 않는다. `TrySetCell`은 좌표만 검사한다. 음수 높이, 수면과 맞지 않는 종류 등을 막거나 자동으로 물을 재분류하지 않는다.

따라서 `WaterLevel = null`인 청사진에도 호출자가 `TerrainKind.Water` 셀을 넣을 수 있다. `null`은 생성기의 자동 물 분류를 생략한다는 뜻이며, Water 셀 저장을 금지하는 조건이 아니다. 수면을 지정한 청사진을 나중에 직접 편집해도 생성 때의 `Level < WaterLevel` 관계가 자동으로 유지되지는 않는다.

### 이웃 좌표

`GetNeighborCoordinates(x, y)`는 유효 좌표의 상하좌우 중 격자 안에 있는 좌표를 반환한다. 대각선과 반대편 경계로 이어지는 좌표는 포함하지 않는다. 반환값은 셀이나 도로 연결 정보가 아닌 `Vector2Int` 좌표 모음이다.

| 예시 | 이웃 집합 |
|---|---|
| `3 × 2` 격자의 `(0, 0)` | `{ (1, 0), (0, 1) }` |
| `3 × 3` 격자의 `(1, 1)` | `{ (0, 1), (2, 1), (1, 0), (1, 2) }` |
| `1 × 1` 격자의 `(0, 0)` | 빈 집합 |

현재 구현은 `(x, y-1)` → `(x, y+1)` → `(x-1, y)` → `(x+1, y)` 순서로 후보를 확인한다. 그러나 테스트는 `Is.EquivalentTo`로 집합을 비교하며 **순회 순서를 계약으로 검증하지 않는다**. 호출자는 길찾기 우선순위 등을 반환 순서에 의존하지 않도록 하고, 순서가 필요하면 명시적으로 정한다. Core API는 이 좌표들을 북·남 같은 월드 방향으로 정의하지 않는다.

조회 시작 좌표가 범위 밖이면 메서드를 호출할 때 `ArgumentOutOfRangeException`을 던진다. x가 잘못됐으면 `ParamName = "x"`, x가 유효하고 y가 잘못됐으면 `"y"`다. 둘 다 잘못되면 x가 먼저 보고된다. 이 동작은 `TryGetCell`·`TrySetCell`의 bool 실패와 다르다.

## BlueprintGenerator

```csharp
public BlueprintGenerator();
public BlueprintGenerator(Func<int, int, int> sampleLevelOffset);
public Blueprint CreateBlueprint(
    int width, int height, int baseLevel = 0, int? waterLevel = null);
```

매개변수 없는 생성자는 모든 좌표에서 높이차 `0`을 반환하는 함수를 사용한다. 주입하는 `sampleLevelOffset`의 인자는 격자 `(x, y)`, 반환값은 **기준층에 더할 정수 높이차**다. 함수를 `null`로 전달하면 `ArgumentNullException`을 던지며 `ParamName`은 `"sampleLevelOffset"`이다.

`CreateBlueprint`는 호출마다 새 `Blueprint`를 만들고 다음 순서로 처리한다.

1. `Blueprint` 생성자로 전체 셀을 기준층의 Grass로 초기화한다. 크기·기준층의 예외는 이 생성자에서 발생한다.
2. 각 좌표에서 높이차 함수를 호출하고, `Level = BaseLevel + 높이차`인 Grass 셀을 저장한다.
3. `WaterLevel`에 값이 있으면 바닥이 수면보다 낮은 셀만 Water로 바꾼다. 바닥 `Level`은 유지한다. `WaterLevel = null`이면 이 단계를 생략한다.

생성기는 GameObject·씬·프리팹을 만들거나 읽지 않는다. 결과는 지형 데이터이며, 표시와 게임 오브젝트 배치는 소비하는 쪽의 작업이다.

현재 높이 순회는 y를 바깥 반복문, x를 안쪽 반복문으로 사용한다. 주입 함수의 호출 순서를 공개 계약으로 검증하는 테스트는 없다. 같은 좌표에서 같은 값을 반환하고 외부 상태를 바꾸지 않는 함수를 사용하면 생성 결과를 이해하고 재현하기 쉽다. 임의의 delegate는 난수 상태나 부수 효과를 가질 수 있으므로, **생성기 자체가 모든 입력 함수에 대해 순수성·재현성을 보장하는 것은 아니다**.

현재 높이차의 음수·최댓값 검사는 없으며, `BaseLevel + 높이차`의 오버플로를 막는 별도 검사도 없다. delegate에서 발생한 예외를 잡거나 다른 예외로 감싸지 않는다.

## PerlinLevelSampler

```csharp
public PerlinLevelSampler(
    float frequency, int maxLevelOffset, float originX = 0f, float originY = 0f);

public static PerlinLevelSampler FromSeed(
    int seed, float frequency, int maxLevelOffset, float originRange);

public int Sample(int x, int y);
```

| 설정 | 의미 |
|---|---|
| `frequency` | 격자 한 칸 이동할 때 Perlin 샘플 좌표에 더하는 간격 |
| `maxLevelOffset` | 양자화에 전달하는 최대 높이차 |
| `originX`, `originY` | 샘플 좌표의 원점. 생성자 기본값은 각각 `0f` |
| `seed` | `FromSeed`가 샘플 원점을 뽑는 `System.Random`의 시드 |
| `originRange` | 원점을 뽑을 때 각 `NextDouble()` 결과에 곱하는 범위 값 |

`Sample(x, y)`는 `Mathf.PerlinNoise(originX + x * frequency, originY + y * frequency)`를 호출하고, 결과를 `TerrainLevelQuantizer.ToLevelOffset`에 전달한다. 반환값은 절대 높이가 아닌 높이차다. `BaseLevel`을 더하는 책임은 생성기에 있다.

`FromSeed`는 `new System.Random(seed)`로 두 값을 뽑아 각각 x·y 원점으로 저장한다. `Sample` 호출마다 난수를 추가로 소비하지 않으며, 생성된 원점과 설정으로 좌표를 계산한다. 이 방식에서 시드는 Perlin 함수의 내부 시드 인자로 전달되는 것이 아니다.

같은 시드·설정·좌표의 반복 생성과 조회 순서 변경을 비교하는 테스트가 있다. 이는 같은 실행 환경에서 재현성을 확인하기 위한 범위다. Unity·.NET 버전이나 플랫폼이 바뀌어도 같은 결과를 얻는 이식성 계약, 모든 서로 다른 시드가 서로 다른 지형을 만든다는 보장은 현재 API에 없다. 서로 다른 시드 비교 테스트도 선택한 두 시드의 표본을 비교한다.

샘플러는 격자 크기를 알지 못하므로 좌표 범위를 검사하지 않는다. 생성자와 `FromSeed`도 `frequency`, `maxLevelOffset`, 원점과 `originRange`의 부호·유한성·상한을 검증하지 않는다. 이 입력들의 유효 범위를 정하고 검사하는 작업은 현재의 갭이다.

## TerrainLevelQuantizer

```csharp
public static int ToLevelOffset(float noiseValue, int maxLevelOffset);
```

일반적인 유한 노이즈 값과 음수가 아닌 최대 높이차에 대해, 구현은 다음 세 단계를 사용한다.

1. `Mathf.Clamp01(noiseValue)`로 값을 0~1 범위로 제한한다.
2. 제한한 값에 `maxLevelOffset + 1`을 곱하고 `Mathf.FloorToInt`로 내린다.
3. `Mathf.Clamp(..., 0, maxLevelOffset)`로 결과를 제한한다.

예를 들어 최대 높이차가 4이면 다음과 같다.

| `noiseValue` | 결과 |
|---|---|
| `-0.01f`, `0f`, `0.19f` | 0 |
| `0.2f` | 1 |
| `0.5f` | 2 |
| `0.8f`, `1f`, `1.01f` | 4 |

입력 1에서 중간 계산값이 최대 높이차보다 커질 수 있으므로 마지막 clamp가 필요하다. 이 분할 방식이 지형 전체에서 각 층의 빈도를 같게 만든다는 뜻은 아니다.

현재 음수 `maxLevelOffset`, `NaN`·무한대 입력, 아주 큰 정수의 산술 범위를 검증하는 로직이나 해당 계약을 검증하는 테스트는 없다. 일반 입력에서의 범위 설명을 이런 값까지 확장해서 해석하지 않는다.

## 최소 호출 예시

다음 예시는 좌표별 높이와 물 분류를 읽는 방법이다.

```csharp
using YouSaidLeft.Core;

var generator = new BlueprintGenerator(sampleLevelOffset: (x, y) => x);
var blueprint = generator.CreateBlueprint(
    width: 3, height: 1, baseLevel: 2, waterLevel: 3);

if (blueprint.TryGetCell(0, 0, out var cell))
{
    // cell.Level == 2, cell.Kind == TerrainKind.Water
    // blueprint.WaterLevel == 3: 바닥 높이와 수면을 따로 읽는다.
}
```

Perlin 샘플러는 인스턴스 메서드를 높이차 함수로 전달한다. 아래 값들은 호출 예시이며 게임 조정값을 확정하는 규칙이 아니다.

```csharp
var sampler = PerlinLevelSampler.FromSeed(
    seed: 12345, frequency: 0.125f, maxLevelOffset: 8, originRange: 256f);
var generator = new BlueprintGenerator(sampleLevelOffset: sampler.Sample);
var blueprint = generator.CreateBlueprint(
    width: 16, height: 16, baseLevel: 2, waterLevel: 6);
```

## BlueprintSceneDebug 표시 도우미

[BlueprintSceneDebug.cs](../../Assets/Scripts/Helper/BlueprintSceneDebug.cs)는 `YouSaidLeft.Helper`의 `MonoBehaviour`다. Inspector 설정으로 청사진을 생성하고 `OnDrawGizmos`에서 표시한다. Core API와 구분하며, 현재 확인한 내용은 **소스의 표시 로직**이다. 이 문서가 Play Mode·Game View의 실제 표시 검증을 의미하지 않는다.

| Inspector 필드 | 소스 기본값 | 용도 |
|---|---|---|
| `_width`, `_height` | 각각 `32` | 미리보기 격자 크기 |
| `_baseLevel` | `2` | 기준층 |
| `_waterLevel` | `4` | 공통 수면. nullable 필드가 아니므로 항상 값이 전달됨 |
| `_seed` | `980306` | 샘플 원점 생성용 시드 |
| `_cellSize` | `1f` | 표시할 격자 간격 |
| `_levelHeight` | `0.2f` | 한 높이 층의 표시 간격 |

미리보기의 샘플러 설정은 소스에 `frequency: 0.125f`, `maxLevelOffset: 8`, `originRange: 256f`로 적혀 있다. `OnValidate`는 캐시된 청사진을 비우고, 다음 `OnDrawGizmos`가 다시 생성한다. 크기·기준층·표시 간격이 조건에 맞지 않으면 그리기를 생략한다.

- Grass는 바닥 `Level`에 녹색 채운 큐브로 그린다.
- Water는 바닥 `Level`에 회색 와이어 큐브를, `WaterLevel`에 파란색 계열 채운 큐브를 따로 그린다.
- 표시 좌표는 로컬 `(x * cellSize, level * levelHeight, y * cellSize)`를 기준으로 한다. 격자 y는 로컬 z 방향에 대응하며, 수직 위치는 `Level`로 정한다.
- 큐브 두께는 `levelHeight * 0.1f`이고 중심을 두께의 절반만큼 내려 윗면이 해당 층에 놓이도록 계산한다. 가로·세로 크기는 `cellSize * 0.95f`다.
- `transform.localToWorldMatrix`로 표시하고, 작업 후 이전 `Gizmos.color`와 `Gizmos.matrix`를 복원한다.

이 도우미는 Gizmos를 그린다. 실제 지형 메시·프리팹·콜라이더·물 시스템을 생성하는 기능은 없다. Inspector의 최소값 표시는 Core API의 입력 검증을 대신하지 않는다.

## 테스트 소스와 현재 갭

아래는 테스트 파일에 적힌 검증 범위이며 최신 실행 결과가 아니다. 테스트 수나 통과 상태는 실행 기록으로 확인한다.

| 테스트 소스 | 확인하도록 작성된 동작 |
|---|---|
| [BlueprintGeneratorTests.cs](../../Assets/Tests/Editor/BlueprintGeneratorTests.cs) | 직사각 격자 조회, 초기 Grass·기준층, 셀 교체, 범위 밖 조회·교체, 이웃 집합·좌표 예외, 함수 주입·기본 높이차·null 예외, 절대 높이, 수면 아래만 Water, 같은 시드의 높이·종류 비교 |
| [PerlinLevelSamplerTests.cs](../../Assets/Tests/Editor/PerlinLevelSamplerTests.cs) | 좌표·설정·원점의 샘플값, 선택한 두 시드의 차이, 같은 시드의 조회 순서 변경 |
| [TerrainLevelQuantizerTests.cs](../../Assets/Tests/Editor/TerrainLevelQuantizerTests.cs) | 층 경계값, 0~1 밖의 유한 노이즈 clamp, 최대 높이차 변경 |

현재 입력 검사와 테스트 범위에는 차이가 있다. 예를 들어 `Blueprint` 생성자의 크기·기준층 예외는 소스에 있지만 해당 테스트 파일에는 그 예외를 검사하는 테스트가 없다. `waterLevel: null`의 자동 분류 생략, 수면 메타데이터만 저장하는 직접 생성, 양쪽 좌표가 동시에 잘못된 경우의 예외 우선순위 등도 위 테스트 파일에서 별도로 검증하지 않는다.

경사·입체 도로망·건물·동굴 공간·교량·고가도로, 목적지 도달 가능성과 최소 단서 보장, 직렬화·온라인 공유, 플랫폼 간 재현성은 현재 지형 API의 검증 범위에 없다. 후속 작업은 [진행 기록](../blueprint-progress.md)에서 하나의 동작으로 좁히고, 테스트로 기대 결과를 먼저 정한다.
