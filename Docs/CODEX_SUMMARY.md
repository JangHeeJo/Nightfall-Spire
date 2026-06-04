# Nightfall Spire Codex 작업 요약

이 문서는 Codex와 ChatGPT가 같은 기준으로 프로젝트를 검토할 수 있도록 만든 공유 요약 문서입니다.
현재 프로젝트는 기존 모바일 게임 Nightfall Spire 스타일을 참고하되, 그대로 복제하지 않고 출시 가능한 독자 Unity 모바일 게임으로 재설계하는 방향입니다.

## 기본 약속

- 답변과 작업 요약은 한국어로 작성한다.
- 사용자의 말에 무조건 동의하지 않고, 출시 기준에서 더 나은 구조가 있으면 먼저 제안한다.
- 실제 Unity 프로젝트 파일과 현재 코드 기준으로 판단한다.
- `GameRoot`, `GameContext`, 순수 Model, 씬별 Root, UI 바인딩 구조를 기본 골격으로 사용한다.
- `GameRoot`는 전역 시작점이지만 Canvas를 들고 다니지 않는다.
- 실제 UI Canvas와 Layer는 각 씬이 소유한다.
- 팝업은 씬에 미리 배치하지 않고 Prefab으로 생성하고 닫으면 파괴한다.
- 비동기 흐름은 `UniTask`를 사용한다.
- 상태와 UI 구독은 `R3`를 사용한다.
- UI 연출은 `DOTween`을 사용할 예정이다.
- Coroutine 사용은 금지한다.
- 불필요한 `MonoBehaviour` 매니저를 늘리지 않는다.
- 코드 주석은 한국어로 작성한다.
- 긴 XML 주석보다 읽기 쉬운 한 줄 주석을 선호한다.
- 스크립트 이름과 Unity 하이어라키 오브젝트 이름은 가능한 맞춘다.
- Git 작업은 작은 단위 커밋과 PR 기준으로 진행한다.
- `main`은 안정 버전으로 유지하고, 작업은 브랜치에서 진행한다.

## 현재 Git 상태

- Git 저장소 생성 완료
- GitHub 저장소 생성 및 push 완료
- `main` 보호 규칙 설정 시작
- GitHub Actions 기반 Unity CI 구성 완료
- 현재 작업 브랜치: `codex/architecture-cleanup`
- 주요 작업 커밋:
  - `Fix practice list reverse`
  - `Refactor core progress architecture`
  - `Add scene root structure`

## CI 구성 상태

GitHub Actions에 `Unity CI` 워크플로를 추가했다.

현재 CI가 확인하는 항목:

- Unity 프로젝트 기본 폴더 존재 여부
- `Library`, `Temp`, `Logs`, `UserSettings` 같은 Unity 생성 폴더 커밋 방지
- 100MB 초과 파일 커밋 방지
- `.unitypackage` 커밋 방지
- Unity EditMode 테스트
- Unity PlayMode 테스트

Android 빌드는 GitHub 무료 러너의 디스크 부족 문제로 PR에서는 제외했다.
Android 빌드는 추후 별도 수동 실행이나 전용 빌드 환경에서 다시 다루는 것이 좋다.

## 완료된 작업 1: Git/GitHub 안전장치

### 한 일

- Unity 프로젝트 루트에서 Git 저장소를 생성했다.
- Unity용 `.gitignore`를 추가했다.
- 큰 `.unitypackage` 파일이 GitHub 100MB 제한에 걸려 제외했다.
- GitHub 저장소를 만들고 프로젝트를 push했다.
- PR 기반 작업 흐름을 만들었다.
- GitHub Actions로 기본 저장소 검사와 Unity 테스트를 돌리도록 설정했다.

### 이유

Codex가 실제 프로젝트 파일을 수정하므로, 변경 내역을 되돌릴 수 있는 Git 기준점이 필요했다.
또한 `main`에 바로 반영하지 않고 PR에서 CI를 통과한 뒤 머지하는 흐름이 출시 기준에 더 안전하다.

## 완료된 작업 2: Core Progress 아키텍처 정리

커밋: `Refactor core progress architecture`

### 변경된 파일

- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/Core/GameContext.cs`
- `Assets/_Project/01_Script/Core/SaveManager.cs`
- `Assets/_Project/01_Script/Core/SceneLoadManager.cs`
- `Assets/_Project/01_Script/Core/DataTableManager.cs`
- `Assets/_Project/01_Script/Core/ServiceManager.cs`
- `Assets/_Project/01_Script/Core/GameStateMachine.cs`
- `Assets/_Project/01_Script/Model/GameProgress.cs`
- `Assets/_Project/01_Script/Model/BattleProgress.cs`
- `Assets/_Project/01_Script/Model/PopupProgress.cs`
- `Assets/_Project/01_Script/Model/RewardProgress.cs`

### 주요 변경

- `GameRoot`에서 불필요한 `Unity.VisualScripting` 의존성을 제거했다.
- `GameRoot`가 `SaveManager`, `SceneLoadManager`, `DataTableManager`, `ServiceRegistry`를 보유하도록 정리했다.
- `GameContext`에 `BattleProgress`, `RewardProgress`, `PopupProgress`를 추가했다.
- `DataTableManager`를 빈 `MonoBehaviour`에서 순수 C# 클래스로 전환했다.
- `BattleProgress`, `PopupProgress`, `RewardProgress`를 빈 `MonoBehaviour`에서 순수 C# 모델로 전환했다.
- `GameProgress`에 `PreviousState`와 `RestorePreviousState()`를 추가했다.
- 앱이 백그라운드로 갔다가 복귀할 때 이전 상태로 돌아올 수 있게 준비했다.
- `SaveManager` 저장 방식을 안전 저장 구조로 바꿨다.

### SaveManager 변경 이유

기존 방식은 `save_data.json`에 바로 저장했다.
저장 중 앱이 종료되면 저장 파일이 깨질 수 있으므로 아래 방식으로 변경했다.

```text
save_data.tmp에 먼저 저장
→ 기존 save_data.json을 save_data.backup.json으로 이동
→ save_data.tmp를 save_data.json으로 교체
```

이 방식은 출시 기준에서 저장 파일 손상 가능성을 줄인다.

### 주의점

`ServiceRegistry` 클래스는 현재 `ServiceManager.cs` 파일 안에 있다.
파일명을 바로 `ServiceRegistry.cs`로 바꾸면 Unity가 생성한 `.csproj`가 기존 `ServiceManager.cs`를 찾지 못해 로컬 빌드가 깨졌다.
Unity가 프로젝트 파일을 재생성한 뒤 파일명 정리를 다시 진행하는 것이 좋다.

## 완료된 작업 3: 연습용 Test 코드 보존

커밋: `Fix practice list reverse`

### 변경된 파일

- `Assets/_Project/01_Script/Test/Test.cs`

### 변경 내용

연습용 리스트 코드의 `Reverse()` 구현을 수정했다.
기존 코드에는 배열 인덱스와 반복 조건 문제가 있었다.

현재 핵심 조건:

```csharp
while(left < right)
```

### 이유

사용자가 연습용 코드지만 일단 보존하고 싶다고 했으므로 삭제하지 않고 수정해두었다.
추후 사용자가 직접 삭제하거나 별도 연습 폴더로 옮길 수 있다.

## 완료된 작업 4: 씬 Root 구조 추가

커밋: `Add scene root structure`

### 추가된 폴더

- `Assets/_Project/01_Script/Scene`

### 추가된 파일

- `LobbySceneRoot.cs`
- `LobbyStaticUIRoot.cs`
- `LobbyDynamicUIRoot.cs`
- `BattleSceneRoot.cs`
- `BattleStaticUIRoot.cs`
- `BattleDynamicUIRoot.cs`

### 주요 구조

`LobbySceneRoot`

- `LobbyScene` 진입점이다.
- `GameRoot.Instance.Context`가 준비될 때까지 기다린다.
- `LobbyStaticUIRoot`, `LobbyDynamicUIRoot`를 초기화한다.

`LobbyStaticUIRoot`

- 로비의 위치 고정 UI 초기화 지점이다.
- `SpireMainView`, `FightStartView`, `BottomNavigationBar` 같은 UI가 나중에 연결될 자리다.

`LobbyDynamicUIRoot`

- 로비의 동적 UI 초기화 지점이다.
- `PopupLayer`, `DimLayer`, `ToastLayer` 참조를 받을 수 있다.
- 추후 `PopupManager`가 현재 씬의 UI 레이어를 등록받을 때 사용한다.

`BattleSceneRoot`

- `BattleScene` 진입점이다.
- `GameContext`가 준비되면 `BattleProgress.BeginBattle()`을 호출한다.
- 씬이 파괴될 때 `BattleProgress.EndBattle()`을 호출한다.
- 전투 Static/Dynamic UI Root를 초기화한다.

`BattleStaticUIRoot`

- 전투 프레임처럼 위치가 거의 변하지 않는 UI 초기화 지점이다.

`BattleDynamicUIRoot`

- 전투 중 값이 자주 바뀌는 UI 초기화 지점이다.
- `PopupLayer`, `DimLayer`, `ToastLayer`, `FloatingTextLayer`, `UnitHpBarLayer` 참조를 받을 수 있다.

### 왜 추가했는지

`GameRoot`가 UI Canvas를 들고 다니지 않는 구조를 지키기 위해서다.
각 씬이 자기 UI Canvas와 Layer를 소유하고, 씬 진입점에서 `GameContext`를 UI에 전달하는 흐름을 만들기 위한 첫 단계다.

## 현재 검증 상태

로컬 검증 명령:

```text
dotnet build "Nightfall Spire.sln"
```

결과:

```text
오류 0개
경고 1개
```

남은 경고는 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고다.
이번 아키텍처 변경으로 새로 생긴 컴파일 오류는 없다.

## 현재 씬 상태

씬 파일에는 이미 아래 오브젝트 이름들이 존재한다.

`LobbyScene`

- `LobbySceneRoot`
- `Canvas_StaticUI`
- `Canvas_DynamicUI`
- `LobbyStaticUIRoot`
- `DimLayer`
- `PopupLayer`
- `ToastLayer`
- `TopCurrencyHud`

`BattleScene`

- `BattleSceneRoot`
- `Canvas_StaticUI`
- `Canvas_DynamicUI`
- `BattleStaticUIRoot`
- `DimLayer`
- `PopupLayer`
- `ToastLayer`

단, 새로 추가한 스크립트들이 실제 씬 오브젝트에 붙어 있는지는 아직 정리하지 않았다.
다음 작업에서 Unity 씬 하이어라키와 스크립트 연결을 맞추는 것이 좋다.

## 다음 작업 계획

### 1순위: 씬 Root 연결

- `LobbySceneRoot` 오브젝트에 `LobbySceneRoot.cs` 연결
- `LobbyStaticUIRoot` 오브젝트에 `LobbyStaticUIRoot.cs` 연결
- 로비 Dynamic UI Root 오브젝트 정리 또는 생성
- `PopupLayer`, `DimLayer`, `ToastLayer` Inspector 참조 연결
- `BattleSceneRoot` 오브젝트에 `BattleSceneRoot.cs` 연결
- `BattleStaticUIRoot` 오브젝트에 `BattleStaticUIRoot.cs` 연결
- 전투 Dynamic UI Root 오브젝트 정리 또는 생성
- 전투의 `PopupLayer`, `DimLayer`, `ToastLayer`, `FloatingTextLayer`, `UnitHpBarLayer` 참조 연결

### 2순위: PopupManager 구조 정리

현재 `PopupManager`는 빈 `MonoBehaviour`다.
최종 방향은 아래와 같다.

```text
GameRoot.PopupManager
→ 현재 씬의 DynamicUIRoot에서 PopupLayer / DimLayer / ToastLayer를 등록받음
→ Popup 요청 시 현재 PopupLayer 아래에 Prefab 생성
→ 닫을 때 R3 구독 정리, DOTween Close, Destroy
```

다음 작업에서는 아직 실제 팝업 Prefab 생성까지 가지 말고, 레이어 등록/해제 구조부터 만드는 것이 안전하다.

### 3순위: ScreenFadeView 추가

- 각 씬의 `Canvas_DynamicUI` 아래에 `ScreenFadeView`를 둔다.
- `GameRoot`에는 Fade Canvas를 두지 않는다.
- DOTween과 UniTask를 이용해 `FadeOutAsync`, `FadeInAsync` 구조를 만든다.

### 4순위: UI 바인딩 기초

- `CurrencyHud`가 `GameContext.CurrencyProgress`를 구독하도록 변경
- R3 구독 해제 생명주기 정리
- 로비 HUD가 `LobbyDynamicUIRoot` 초기화 시 Context를 받는 구조로 연결

### 5순위: DataTable 방향 확정

`Assets/_Project/05_Data` 아래 구조를 만든다.

추천 구조:

```text
Assets/_Project/05_Data
├── Tables
│   ├── HeroData.tsv
│   ├── EnemyData.tsv
│   ├── StageData.tsv
│   ├── WaveData.tsv
│   ├── CardData.tsv
│   ├── SkillData.tsv
│   └── RewardData.tsv
└── Generated
```

밸런스 수치 데이터는 TSV/CSV가 적합하고, Prefab/Audio/Addressables 참조는 ScriptableObject가 적합하다.

### 6순위: 정리 후보

- `Assets/_Recovery`는 Unity 자동 복구 씬처럼 보이므로 정리 후보다.
- `Assets/_Project/01_Script/Test`는 연습용 코드이므로 추후 삭제하거나 `99_Test` 아래로 옮기는 것이 좋다.
- `Unity Activation` 워크플로는 더 이상 지원되지 않는 GameCI 액션을 사용하므로 제거 후보다.

## ChatGPT에 물어볼 때 추천 질문

아래 질문을 ChatGPT에 던지면 구조 비교에 도움이 된다.

```text
이 Unity 모바일 게임 프로젝트는 GameRoot, GameContext, 순수 Progress Model, 씬별 Root, Static/Dynamic UI Root 구조를 사용하고 있습니다.
현재 작업 요약 문서를 기준으로, 출시 가능한 모바일 게임 아키텍처 관점에서 책임 분리가 적절한지 검토해주세요.
특히 PopupManager, SceneRoot, DataTableManager, SaveManager 구조에서 나중에 문제가 될 수 있는 부분을 지적해주세요.
```

## Codex가 다음 작업 때 지킬 기준

- 씬 파일을 직접 수정할 때는 Missing Script 위험을 먼저 확인한다.
- 새 스크립트 추가 시 `.meta` 파일도 함께 관리한다.
- 파일명과 클래스명은 가능한 일치시킨다.
- Unity가 생성한 `.csproj` 때문에 파일명 변경이 빌드를 깨면, Unity 재생성 이후로 미룬다.
- 변경 후 `dotnet build "Nightfall Spire.sln"`을 실행한다.
- 가능한 경우 GitHub Actions PR 체크까지 확인한다.

