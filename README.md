# You Said Left

한 명은 운전하고, 한 명은 지도를 읽으며 목적지까지 도착하는 2인 협동 운전 게임.

Unity 학습을 목적으로 게임 코드는 직접 작성한다. AI는 설계 방향, 설명, 디버깅, 리뷰를 보조한다. Steam 출시를 목표로 하며, 협동 접속 방식과 세부 게임 규칙은 GDD에서 정한다.

## 개발 환경

- Unity Editor 버전: `ProjectSettings/ProjectVersion.txt` 기준
- 현재 초기 구성: `6000.6.0f1`, Universal Render Pipeline
- 버전 관리: Git, Git LFS
- 의존성 버전: `Packages/manifest.json`과 `Packages/packages-lock.json`

Unity Hub에서 이 저장소의 루트 폴더를 프로젝트로 추가하고, 프로젝트에 기록된 Editor 버전으로 연다.

새 작업 환경에서는 Git LFS를 설치한 뒤, 저장소 루트에서 실행한다.

```shell
git lfs install --local
git lfs pull
git config --local commit.template .gitmessage
```

Git의 로컬 설정과 훅은 커밋으로 공유되지 않으므로 작업 환경마다 설정한다.

## 개발 규칙

- [개발 컨벤션](docs/DEVELOPMENT.md)
- [커밋 메시지 템플릿과 규칙](.gitmessage)
- [AI 작업 지침](AGENTS.md)

게임 기획 문서는 `docs/`에 작성한다.
