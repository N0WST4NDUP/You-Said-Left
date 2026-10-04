# You Said Left 개발 컨벤션

이 문서는 직접 코딩하는 소규모 Unity 학습 프로젝트의 개발 규칙을 정한다. 커밋 메시지 규칙의 정본은 루트의 [.gitmessage](../.gitmessage)다.

## 1. 작업 원칙

- 동작하는 작은 단위부터 만들고, 실제 플레이를 확인하며 확장한다.
- 구현할 게임 규칙은 GDD에서 정한다. 문서의 제안과 확정된 규칙을 구분한다.
- 현재 작업에 필요한 만큼만 구조를 나눈다. 디자인 패턴, 인터페이스, DI, 어셈블리 분리는 문제를 해결할 때 도입한다.
- AI는 방향 제안, 설명, 디버깅, 리뷰를 보조한다. 게임 코드의 작성·수정이나 일괄 포맷은 명시적으로 요청한 경우에만 맡긴다.
- 저장소 밖의 이전 프로젝트 규칙을 자동으로 적용하지 않는다. 이슈 생성, 결정 번호, YAML 계약, 게이트 체계는 의무가 아니다.

## 2. 폴더와 파일

새로 만드는 프로젝트 에셋은 `Assets/YouSaidLeft/` 아래에 둔다. 아래 폴더는 실제로 필요할 때 만든다.

| 경로 | 용도 |
|---|---|
| `Scripts/` | 게임 코드. 기능이 커지면 Driving, Navigation, Traffic 등으로 구분 |
| `Editor/` | Unity Editor 전용 코드 |
| `Scenes/` | 게임 씬 |
| `Prefabs/` | 재사용할 오브젝트 |
| `Art/`, `Materials/`, `Audio/` | 시각·소리 에셋 |
| `Tests/` | 필요해진 자동 테스트 |

- 타입과 코드 식별자는 영어를 사용한다. 자체 제작 에셋 이름은 영어 PascalCase로 작성한다.
- 주요 타입 이름과 C# 파일 이름을 일치시킨다. MonoBehaviour와 ScriptableObject는 파일마다 주요 타입 하나를 둔다.
- 프로젝트 코드의 루트 네임스페이스는 `YouSaidLeft`다. 하위 네임스페이스는 기능 단위로 추가한다.
- 네임스페이스는 중괄호 블록 형식을 사용한다.
- 기존 Unity 템플릿, 패키지, 외부 에셋은 이 규칙을 맞추기 위해 일괄 이동하거나 포맷하지 않는다.
- 게임 기획과 작업 설명은 `docs/`에 둔다. `docs/decisions/`, `docs/api/`, `docs/tools/`는 각각 기록·설명·도구가 필요할 때 사용한다.

## 3. C# 스타일

| 항목 | 규칙 |
|---|---|
| 들여쓰기 | 공백 4칸 |
| 중괄호 | 새 줄에 여는 Allman 형식. 조건문·반복문에도 중괄호 사용 |
| 타입·메서드·프로퍼티·이벤트·enum 멤버 | PascalCase |
| 인터페이스 | I 접두사와 PascalCase |
| 지역 변수·매개변수 | camelCase |
| private 필드 | `_camelCase`. private static 필드도 동일 |
| 상수 | PascalCase |
| 접근 제한자 | 명시적으로 작성 |
| Inspector 노출 필드 | 기본적으로 `[SerializeField] private` |
| 텍스트 파일 | UTF-8, BOM 없음, LF 줄바꿈, 파일 끝 개행 |

- 변수 이름에 역할과 단위를 드러낸다. 예: `_maxSpeedKph`, `_brakeTorque`, `elapsedSeconds`.
- `var`는 오른쪽 표현식만으로 타입이 명확할 때 사용한다.
- 문서와 주석은 한국어를 기본으로 한다. 주석은 코드만으로 드러나지 않는 이유·제약·단위를 설명한다.
- `.editorconfig`는 포맷과 일부 명명 규칙을 지원하는 IDE에 전달한다. 명명 규칙은 suggestion 수준이며 컴파일·CI 강제 검사가 아니다.
- `.editorconfig`에 없는 명명·폴더·설계 규칙은 직접 확인한다. 현재 별도 자동 검사 도구는 없다.
- 저장소 전체에 자동 포맷을 적용하지 않는다.

## 4. Unity 코드와 에셋

- Inspector에 연결한 참조를 우선 사용한다. 반복 실행되는 경로에서 불필요한 오브젝트 검색·컴포넌트 조회·할당을 피한다.
- 입력은 Input System의 액션과 콜백 또는 적절한 프레임 갱신에서 읽는다. Rigidbody에 힘을 적용하는 등 물리 갱신은 `FixedUpdate`에서 처리한다.
- 시간 기반 계산은 해당 갱신 주기의 delta time을 사용한다.
- 이벤트 구독은 구독한 수명에 맞춰 해제한다. 정적 상태는 여러 번 Play해도 이전 실행의 값이 의도 없이 남지 않도록 관리한다.
- Inspector에 저장되는 필드의 이름·타입을 바꿀 때는 기존 씬과 프리팹의 값·참조가 유지되는지 확인한다. 필요한 경우 `FormerlySerializedAs`를 사용한다.
- Editor 전용 코드는 `Editor/`에 둔다. 런타임 코드에서 `UnityEditor`에 의존하지 않는다.
- 에셋·폴더의 이동과 이름 변경은 Unity Editor에서 한다. 해당 `.meta` 파일을 함께 커밋하며 GUID를 재생성하지 않는다.
- 씬·프리팹·직렬화 에셋·ProjectSettings는 Unity Editor에서 수정한다. YAML을 직접 손으로 고치지 않는다.
- 에셋 직렬화는 Force Text, 버전 관리는 Visible Meta Files를 사용한다.
- 씬·프리팹 충돌은 변경 의도를 확인하고 Unity Editor에서 해결한다. 확인 없이 한쪽 파일 전체를 덮어쓰지 않는다.

## 5. 패키지와 버전

- Unity 버전은 `ProjectSettings/ProjectVersion.txt`를 기준으로 맞춘다.
- 패키지는 현재 구현에 필요한 경우 추가하고, 목적·라이선스·Editor 호환성을 확인한다.
- `Packages/manifest.json`과 `Packages/packages-lock.json`을 함께 추적한다.
- 패키지 설치·업데이트는 Package Manager를 우선 사용한다. Git 의존성은 가능한 한 태그 또는 커밋으로 고정한다.
- Editor·패키지 업그레이드는 기능 구현과 구분해 검증한다. 검증되지 않은 업그레이드를 다른 작업에 끼워 넣지 않는다.
- 접속 방식이 정해지기 전에 온라인·Steam 연동 패키지를 컨벤션만으로 확정하지 않는다.

## 6. Git과 작업 단위

- 개인 학습 프로젝트는 `main`에서 작은 단위로 작업할 수 있다. 큰 기능이나 실험은 `feat/<topic>`, `fix/<topic>`, `chore/<topic>` 같은 짧은 브랜치로 구분한다.
- 하나의 목적을 가진 변경을 하나의 커밋으로 묶는다. 관련 코드·문서·설정은 함께 포함할 수 있다.
- 기존 사용자 변경을 확인한 뒤, 이번 작업에 해당하는 파일만 명시적으로 스테이징한다.
- 커밋 메시지는 [.gitmessage](../.gitmessage)를 따른다. 저장소 루트에서 `git config --local commit.template .gitmessage`로 연결한다.
- 템플릿은 메시지 작성 보조다. 현재 commit-msg 훅이나 자동 형식 검사는 없다.
- Unity 캐시, 로그, 빌드 결과, IDE 생성 파일은 기존 `.gitignore` 규칙에 따른다. 인증 정보와 개인 설정은 커밋하지 않는다.
- 바이너리 에셋은 기존 `.gitattributes`의 Git LFS 규칙을 유지한다.
- 텍스트 줄바꿈은 `.gitattributes`와 `.editorconfig`에 따른다. 전체 저장소 재정규화는 별도 작업으로 한다.
- 기본 Unity YAML 병합은 일반 텍스트 병합이다. UnityYAMLMerge를 사용할 경우 각 환경에서 실행 파일과 Git merge driver를 설정한 뒤 속성도 함께 변경한다.
- 커밋·푸시·병합은 각각 요청한 범위에서 수행한다.

## 7. 검증과 완료 기준

변경의 영향에 맞춰 확인한다. 검증을 실행하지 못했으면 그 사실과 이유를 남긴다.

| 변경 | 최소 확인 |
|---|---|
| 문서·Git·포맷 설정 | 파일 내용, 링크·경로, `git diff --check`, 커밋 범위 |
| 게임 코드 | 컴파일 오류 확인, 관련 동작을 Play Mode에서 재현 |
| 씬·프리팹·UI·카메라·운전 감각 | Game View에서 직접 동작 확인 |
| 독립적인 계산·상태 전이·버그 회귀 | 필요할 때 EditMode 또는 PlayMode 테스트 |
| 패키지·Editor·빌드 설정 | 재임포트·컴파일 확인, 영향이 있는 플레이·대상 플랫폼 빌드 |
| 온라인 기능 | 두 클라이언트에서 연결·상태 동기화·연결 종료 확인 |

- 버그 수정은 재현 절차와 기대 동작부터 확인한다.
- 게임 동작이 달라졌다면 직접 플레이로 확인한다. 숫자·로그만으로 시각·조작 결과를 검증했다고 하지 않는다.
- 테스트는 실패하기 쉬운 규칙과 회귀를 보호할 때 추가한다. 낮은 영향의 문서 수정에 형식적인 테스트를 만들지 않는다.
- Unity 검증 후 생긴 관련 없는 에셋·ProjectSettings 변경은 커밋에 섞지 않는다.
- 검증 결과는 실행한 내용과 한계가 드러나게 기록한다. 실행하지 않은 검사나 빌드를 통과했다고 쓰지 않는다.

## 8. 규칙 변경

규칙은 문제가 생겼거나 학습·구현에 도움이 되는 이유가 있을 때 변경한다. 커밋 규칙은 `.gitmessage`, 그 밖의 개발 규칙은 이 문서, 에디터 지원 설정은 `.editorconfig`와 `.gitattributes`에 반영한다. 같은 규칙을 여러 문서에 전문으로 복제하지 않는다.

## 참고 문서

- [Unity 에셋 메타데이터](https://docs.unity3d.com/6000.6/Documentation/Manual/AssetMetadata.html)
- [Unity 버전 관리 설정](https://docs.unity3d.com/6000.6/Documentation/Manual/class-VersionControlSettings.html)
- [C# 포맷 옵션](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options)
- [C# 명명 규칙과 IDE 지원](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/naming-rules)
- [Git commit.template](https://git-scm.com/docs/git-config#Documentation/git-config.txt-committemplate)
