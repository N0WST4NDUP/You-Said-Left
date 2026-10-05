# 저장소 검사 도구

## 개발 환경

- Unity 버전: `ProjectSettings/ProjectVersion.txt` 기준
- 검사 도구: Git, .NET SDK 10.0.400 (`global.json` 기준)
- 자체 제작 코드는 `Assets/Scripts/` 아래에 추가한다.
- 자체 제작 에셋은 `Assets/YouSaidLeft/` 아래에 필요한 만큼 추가한다.

## 저장소 검사

저장소 루트의 PowerShell에서 한 번 실행한다.

```powershell
dotnet restore tools/RepositoryChecks.Tests --locked-mode
dotnet test tools/RepositoryChecks.Tests --no-restore
.\tools\setup-hooks.ps1
```

설치 스크립트는 Git이 사용하는 훅 디렉터리에 `pre-commit`과 `commit-msg`를 설치하고 커밋 메시지 템플릿을 연결한다. 기존의 관련 없는 훅이 있으면 덮어쓰지 않고 중단한다. Git LFS 훅을 보존하며 `core.hooksPath`는 변경하지 않는다.

직접 실행할 수도 있다.

```powershell
dotnet run --project tools/RepositoryChecks --no-restore -- lint --all
dotnet run --project tools/RepositoryChecks --no-restore -- lint --staged
dotnet run --project tools/RepositoryChecks --no-restore -- commit --file <file>
dotnet run --project tools/RepositoryChecks --no-restore -- commit --range <base>..<head>
```

`<file>`, `<base>`, `<head>`는 실제 메시지 파일 경로와 Git 참조로 바꾼다. `lint --staged`는 스테이징된 내용만 검사한다. C# 검사 범위는 `Assets/Scripts/**/*.cs`이며, 검사 규칙은 [CodeRules.cs](../../tools/RepositoryChecks/CodeRules.cs)에 있다. 현재 이 범위에는 C# 파일이 0개다.

커밋 메시지 규칙은 [CommitRules.cs](../../tools/RepositoryChecks/CommitRules.cs), 작성 예시는 [.gitmessage](../../.gitmessage)를 참고한다. CI에서도 검사 도구 테스트, 전체 C# 검사, 새 커밋 메시지 검사를 실행한다. 기존 커밋은 기준 커밋 `064156e824b71f0e744ebaaa279e6b45e9cfff6c`까지 검사에서 제외한다.

조건부 컴파일은 파일당 최대 8개 심볼의 조합을 검사하며, 검사하지 못하는 코드가 있으면 실패한다. 포맷 검사에는 원본을 수정하지 않는 임시 복사본을 사용한다. 커밋 훅은 Git 설정의 메시지 정리 모드를 따른다. 명령행 `--cleanup` 재정의와 `core.commentChar=auto`는 지원하지 않으며, CI에서는 실제 저장된 메시지를 검사한다. GitHub에서 병합을 차단하려면 저장소 규칙에서 `repository-checks` 검사를 필수로 지정한다.

이 도구는 C# 구문과 저장소 규칙을 확인한다. Unity 컴파일이나 Play Mode 검증은 별도로 실행하고, 게임 동작·화면 변경은 Game View에서 직접 확인한다.
