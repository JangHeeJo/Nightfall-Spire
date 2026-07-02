# MacBook Codex Handoff

작성일: 2026-07-02

이 파일은 맥북 Codex에서 내 Unity 프로젝트 2개를 GitHub 기준으로 바로 확인하고 이어받기 위한 최소 인수인계 문서다.

## GitHub 다운로드 준비 상태

### Nightfall Spire

- 다운로드 가능: 예
- GitHub 주소: `https://github.com/JangHeeJo/Nightfall-Spire.git`
- 받을 브랜치: `codex/architecture-cleanup`
- 맥북에서 할 일:
  - GitHub Desktop 또는 `git clone`으로 저장소를 받는다.
  - `codex/architecture-cleanup` 브랜치로 전환한다.
  - Unity Hub에서 받은 폴더를 연다.

### PrisonLife

- 다운로드 가능: 아직 아님
- 이유: 현재 로컬 저장소에는 GitHub remote가 없다.
- 현재 로컬 프로젝트 경로: `D:\Project\M_1\PrisonLife`
- 준비된 상태: 업로드용 커밋은 로컬 복사본에 만들어둠.
- 남은 일: GitHub에 빈 저장소 `JangHeeJo/PrisonLife`를 만든 뒤 remote로 연결해 push하면 맥북에서 받을 수 있다.

## 공통 작업 규칙

- 답변은 한국어로 한다.
- Ponytail 모드 기준으로 작업한다: 가장 작은 올바른 변경, 새 시스템보다 기존 구조 재사용.
- Unity/C#/표준 기능, 이미 설치된 패키지, 기존 서비스/테이블/프리팹을 먼저 확인한다.
- 사용자 변경사항을 되돌리지 않는다.
- 비슷한 코드를 병렬로 추가하지 말고, 공유 원인을 찾아 한 곳에서 고친다.
- Unity 씬/프리팹 변경은 직렬화 참조를 특히 조심한다.

## 프로젝트 1: PrisonLife

- 성격: Unity 모바일 방치형/경영 게임. Google Play 내부 테스트를 목표로 보상형 광고, Unity IAP, 저장/로드, 언락 진행, 포트폴리오용 릴리즈 QA 구조가 들어가 있다.
- 실제 Windows 프로젝트 경로: `D:\Project\M_1\PrisonLife`
- 솔루션: `D:\Project\M_1\PrisonLife\PrisonLife.sln`
- 현재 열린 보조/전달 폴더: `C:\Users\wkd84\OneDrive\문서\PrisonLife`
- 현재 브랜치: `master`
- Git remote: 현재 `git remote -v` 출력 없음. 맥북에서 바로 clone하려면 먼저 GitHub remote를 연결하고 push하거나, 프로젝트 폴더를 직접 옮겨야 한다.
- 현재 변경 상태: IAP, 광고 보상, 저장, 골드 HUD 관련 파일에 미커밋 변경 있음.

중요 파일:

- `D:\Project\M_1\PrisonLife\AGENTS.md`
- `D:\Project\M_1\PrisonLife\Docs\PORTFOLIO_REFACTOR_SUMMARY.md`
- `D:\Project\M_1\PrisonLife\Docs\RELEASE_ADMOB_CHECKLIST.md`
- `D:\Project\M_1\PrisonLife\Packages\manifest.json`

핵심 구조:

- Monetization: `Assets/Scripts/IAP/*`, `Assets/Scripts/AdMobRewardedAdService.cs`
- Gold boost: `Assets/Scripts/IAP/GoldMultiplierProvider.cs`, `Assets/GoldHudView.cs`
- Save: `Assets/Scripts/Save/SaveManager.cs`, `Assets/Scripts/Save/SaveData.cs`
- Unlock: `Assets/Scripts/Unlock/*`

검증 기준:

- 작은 코드 변경은 `dotnet build "PrisonLife.sln"`로 우선 확인한다.
- Unity/Android/광고/IAP 변경은 Unity Editor와 Google Play 내부 테스트 기준으로 별도 확인이 필요하다.

## 프로젝트 2: Nightfall Spire

- 성격: Unity 모바일 성채 방어/로그라이트/성장 게임. 기존 Nightfall Spire 계열 느낌은 참고하되, 독자 출시 가능한 구조로 재설계 중이다.
- 실제 Windows 프로젝트 경로: `D:\Project\M_1\Nightfall Spire`
- 솔루션: `D:\Project\M_1\Nightfall Spire\Nightfall Spire.sln`
- GitHub remote: `https://github.com/JangHeeJo/Nightfall-Spire.git`
- 현재 브랜치: `codex/architecture-cleanup`
- 맥북 연결 기준: 위 GitHub 저장소를 clone하고 `codex/architecture-cleanup` 브랜치를 checkout한다.
- 현재 변경 상태: 씬, 프리팹, 세이브/전투/영웅 서비스, 스킬 테이블, 다수 아트 에셋 변경 및 추가/삭제가 있음. 작업 전 `git status`로 반드시 확인한다.

중요 파일:

- `D:\Project\M_1\Nightfall Spire\Docs\CODEX_SUMMARY.md`
- `D:\Project\M_1\Nightfall Spire\Docs\DATA_TABLE_ARCHITECTURE.md`
- `D:\Project\M_1\Nightfall Spire\Docs\CODEX_DATA_TABLE_TASK.md`
- `D:\Project\M_1\Nightfall Spire\Packages\manifest.json`

핵심 작업 기준:

- `GameRoot`, `GameContext`, 순수 Model, 씬별 Root, MVC UI 구조를 기본 골격으로 쓴다.
- Canvas는 각 씬이 소유한다. `GameRoot`가 Canvas를 들고 다니지 않는다.
- 팝업은 Prefab으로 생성하고 닫으면 파괴한다.
- 비동기는 UniTask, 상태 구독은 R3, UI 연출은 DOTween을 쓴다.
- Coroutine은 쓰지 않는다.
- 불필요한 MonoBehaviour 매니저를 늘리지 않는다.
- 테이블 기반 데이터는 `Assets/_Project/05_Data/Tables/*`를 우선 확인한다.

최근 작업 맥락:

- 완료된 작업 40: 성채 층 슬롯 기반 전투 타겟 구조 적용.
- 기본 전투 슬롯, 애니메이션 상태 매핑, 전투 유닛 공통 애니메이션, Spire 팝업 전투 진입 등이 정리되어 있다.
- 자세한 누적 내역은 `Docs\CODEX_SUMMARY.md`를 먼저 읽으면 된다.

검증 기준:

- `dotnet build "Nightfall Spire.sln"` 통과가 기본 확인선이다.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 알려진 경고다. 새 컴파일 오류가 생겼는지만 우선 본다.

## 맥북 Codex에서 시작 순서

1. Nightfall Spire는 GitHub에서 clone: `https://github.com/JangHeeJo/Nightfall-Spire.git`
2. 브랜치 checkout: `codex/architecture-cleanup`
3. PrisonLife는 GitHub에 빈 저장소 `JangHeeJo/PrisonLife`를 만든 뒤 Windows 로컬 저장소를 push해야 clone 가능하다.
4. 각 프로젝트 루트의 `AGENTS.md` 또는 이 파일의 공통 작업 규칙을 먼저 읽는다.
5. Unity 프로젝트는 `Library`, `Temp`, `obj`, `.vs`, `Logs` 같은 생성 폴더를 신뢰하지 말고 Unity가 다시 만들게 둔다.
6. 작업 시작 전 항상 `git status`로 사용자 변경사항을 확인한다.
