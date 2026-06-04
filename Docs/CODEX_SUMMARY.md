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
- UI 연출은 `DOTween`을 사용한다.
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
  - `Add Codex project summary`
  - `Connect scene root components`
  - `Add popup layer registration`
  - `Add popup lifecycle foundation`
  - `Add currency HUD binder`
  - `Add screen fade foundation`
  - `Add DOTween package and UI tweens`

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

## 완료된 작업 5: 씬 Root 컴포넌트 연결

커밋: `Connect scene root components`

### 변경된 파일

- `Assets/_Project/00_Scenes/LobbyScene.unity`
- `Assets/_Project/00_Scenes/BattleScene.unity`

### LobbyScene 연결 내용

- `LobbySceneRoot` 오브젝트에 `LobbySceneRoot.cs`를 연결했다.
- `LobbyStaticUIRoot` 오브젝트에 `LobbyStaticUIRoot.cs`를 연결했다.
- `Canvas_DynamicUI` 오브젝트에 `LobbyDynamicUIRoot.cs`를 연결했다.
- `DimLayer` 오브젝트에 `CanvasGroup`을 추가했다.
- `LobbySceneRoot`의 `staticUIRoot`, `dynamicUIRoot` 참조를 연결했다.
- `LobbyDynamicUIRoot`의 `popupLayer`, `dimLayer`, `toastLayer` 참조를 연결했다.

### BattleScene 연결 내용

- `BattleSceneRoot` 오브젝트에 `BattleSceneRoot.cs`를 연결했다.
- `BattleStaticUIRoot` 오브젝트에 `BattleStaticUIRoot.cs`를 연결했다.
- `Canvas_DynamicUI` 오브젝트에 `BattleDynamicUIRoot.cs`를 연결했다.
- `DimLayer` 오브젝트에 `CanvasGroup`을 추가했다.
- `BattleSceneRoot`의 `staticUIRoot`, `dynamicUIRoot` 참조를 연결했다.
- `BattleDynamicUIRoot`의 `popupLayer`, `dimLayer`, `toastLayer` 참조를 연결했다.
- `floatingTextLayer`, `unitHpBarLayer`는 아직 씬에 명확한 오브젝트가 없어 `None` 상태로 두었다.

### 왜 이렇게 했는지

현재 씬에는 별도 `LobbyDynamicUIRoot`, `BattleDynamicUIRoot` 오브젝트가 없었다.
그래서 새 GameObject를 만들기보다 기존 `Canvas_DynamicUI`에 Dynamic UI Root 스크립트를 붙였다.
이 방식은 하이어라키 변경 폭이 작고, 현재 씬 구조를 크게 흔들지 않는다.

`DimLayer`는 팝업 뒤 어둡게 처리하거나 입력 차단을 제어해야 하므로 `CanvasGroup`이 필요하다.
따라서 `DimLayer`에 `CanvasGroup`을 추가했다.

## 완료된 작업 6: PopupManager 레이어 등록 구조

커밋: `Add popup layer registration`

### 변경된 파일

- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/UI/PopupManager.cs`
- `Assets/_Project/01_Script/Scene/LobbyDynamicUIRoot.cs`
- `Assets/_Project/01_Script/Scene/BattleDynamicUIRoot.cs`

### 주요 변경

- `PopupManager`를 빈 `MonoBehaviour`에서 순수 C# 관리 클래스로 변경했다.
- `GameRoot`가 `PopupManager`를 생성하고 전역에서 접근할 수 있게 했다.
- `LobbyDynamicUIRoot`가 로비 씬의 `PopupLayer`, `DimLayer`, `ToastLayer`를 등록한다.
- `BattleDynamicUIRoot`가 전투 씬의 `PopupLayer`, `DimLayer`, `ToastLayer`를 등록한다.
- 씬이 파괴될 때 Dynamic UI Root가 등록한 레이어를 해제한다.
- 레이어 등록 시 `DimLayer`의 `alpha`, `blocksRaycasts`, `interactable`을 초기 상태로 되돌린다.

### 왜 이렇게 했는지

팝업은 전역 요청으로 열릴 수 있지만, 실제 표시 위치는 현재 씬의 Canvas 아래에 있어야 한다.
따라서 `GameRoot`가 Canvas를 직접 들고 다니지 않고, 현재 씬의 Dynamic UI Root가 자기 레이어를 `PopupManager`에 등록하는 구조로 잡았다.

이번 단계에서는 실제 팝업 Prefab 생성까지 가지 않았다.
먼저 레이어 등록/해제 책임을 고정해두면, 다음 단계에서 `BasePopup`, 닫기 연출, 딤 처리, 중복 팝업 정책을 붙일 때 구조가 흔들리지 않는다.

## 완료된 작업 7: Popup 생명주기 기초 구현

커밋: `Add popup lifecycle foundation`

### 변경된 파일

- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/UI/BasePopup.cs`
- `Assets/_Project/01_Script/UI/PopupManager.cs`
- `Assets/_Project/01_Script/UI/PopupTweenManager.cs`
- `Assets/_Project/01_Script/Model/CurrencyProgress.cs`

### 주요 변경

- `BasePopup`에 공통 초기화, 열기, 닫기, 닫기 요청 생명주기를 추가했다.
- 팝업 프리팹에 `CanvasGroup`이 없으면 자동으로 추가하게 했다.
- `PopupManager.OpenAsync()`를 추가해 현재 씬의 `PopupLayer` 아래에 팝업 프리팹을 생성할 수 있게 했다.
- `PopupManager.CloseAsync()`, `CloseTopAsync()`, `CloseAllAsync()`를 추가했다.
- 팝업이 하나 이상 열려 있으면 `DimLayer`를 켜고 뒤쪽 UI 입력을 막도록 했다.
- 팝업이 모두 닫히면 `DimLayer`를 다시 숨기고 입력 차단을 해제하도록 했다.
- `PopupProgress.OpenPopupCount`와 실제 열린 팝업 수가 함께 움직이도록 연결했다.
- `PopupTweenManager`를 순수 C# 클래스로 정리하고, DOTween을 붙이기 전까지는 즉시 표시/숨김만 담당하게 했다.
- `CurrencyProgress.cs`의 깨진 한국어 주석을 UTF-8 기준으로 복구했다.

### 왜 이렇게 했는지

팝업은 이후 성장, 보상, 설정, 확인/취소, 경고, 오프라인 보상 등 여러 곳에서 반복해서 쓰인다.
그래서 개별 팝업마다 생성/닫기/딤 처리 규칙을 따로 만들면 유지보수가 어려워진다.

이번 변경으로 팝업 공통 흐름은 아래처럼 고정했다.

```text
PopupManager.OpenAsync(prefab)
→ 현재 씬 PopupLayer 아래에 생성
→ BasePopup.Initialize()
→ DimLayer 표시
→ BasePopup.OpenAsync()

BasePopup.RequestCloseAsync()
→ PopupManager.CloseAsync()
→ BasePopup.CloseAsync()
→ PopupProgress 갱신
→ 필요하면 DimLayer 숨김
→ Destroy
```

DOTween은 아직 `Packages/manifest.json`에 없으므로 바로 의존성을 추가하지 않았다.
나중에 DOTween을 설치하면 `PopupTweenManager` 내부만 교체해서 열기/닫기 연출을 붙이면 된다.

## 완료된 작업 8: Currency HUD Binder 샘플 추가

커밋: `Add currency HUD binder`

### 변경된 파일

- `Assets/_Project/01_Script/UI/CurrencyHud.cs`
- `Assets/_Project/01_Script/Presenter/CurrencyHudBinder.cs`
- `Assets/_Project/01_Script/Presenter.meta`
- `Assets/_Project/01_Script/Presenter/CurrencyHudBinder.cs.meta`
- `Assets/_Project/01_Script/Scene/LobbyStaticUIRoot.cs`
- `Assets/_Project/00_Scenes/LobbyScene.unity`

### 주요 변경

- `CurrencyHud`를 재화 표시만 담당하는 View로 정리했다.
- `CurrencyHudBinder`를 추가해 `CurrencyProgress`와 `CurrencyHud`를 연결하게 했다.
- `CurrencyHudBinder`가 `CurrencyProgress.Gold`, `CurrencyProgress.Gem`을 R3로 구독한다.
- 값이 바뀌면 Binder가 `CurrencyHud.SetGold()`, `CurrencyHud.SetGem()`을 호출한다.
- `LobbyStaticUIRoot`가 `GameContext`를 받은 뒤 `CurrencyHudBinder`를 생성한다.
- `LobbyStaticUIRoot.OnDestroy()`에서 Binder 구독을 해제한다.
- `LobbyScene`의 `TopCurrencyHud` 오브젝트에 `CurrencyHud` 컴포넌트를 연결했다.

### 왜 이렇게 했는지

이 프로젝트는 전통 MVC를 그대로 쓰기보다 Unity식 MV(P) + R3 Binder 구조가 더 적합하다.
Unity View는 씬/프리팹과 Inspector 참조를 가져야 하므로, View가 Model을 직접 구독하기 시작하면 UI 코드가 점점 무거워진다.

이번 변경으로 첫 UI 바인딩 기준을 아래처럼 잡았다.

```text
CurrencyProgress
= Model. 골드/젬 상태와 변경 규칙을 가진다.

CurrencyHud
= View. 골드/젬 표시 함수만 가진다.

CurrencyHudBinder
= Binder. Model을 구독하고 View 표시 함수를 호출한다.
```

현재 `TopCurrencyHud`에는 아직 실제 골드/젬 텍스트 자식이 없다.
그래서 `CurrencyHud`의 `goldText`, `gemText` 참조는 비워둔 상태다.
나중에 UI 배치 단계에서 텍스트 오브젝트를 만든 뒤 Inspector에 연결하면 된다.

## 완료된 작업 9: ScreenFade 기초 구조 추가

커밋: `Add screen fade foundation`

### 변경된 파일

- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/UI/ScreenFadeManager.cs`
- `Assets/_Project/01_Script/UI/ScreenFadeManager.cs.meta`
- `Assets/_Project/01_Script/UI/ScreenFadeView.cs`
- `Assets/_Project/01_Script/UI/ScreenFadeView.cs.meta`
- `Assets/_Project/01_Script/Scene/LobbyDynamicUIRoot.cs`
- `Assets/_Project/01_Script/Scene/BattleDynamicUIRoot.cs`
- `Assets/_Project/00_Scenes/LobbyScene.unity`
- `Assets/_Project/00_Scenes/BattleScene.unity`

### 주요 변경

- `ScreenFadeManager`를 추가해 현재 씬의 `ScreenFadeView` 참조를 전역에서 사용할 수 있게 했다.
- `GameRoot`가 `ScreenFadeManager`를 생성하도록 했다.
- `ScreenFadeView`를 추가해 `CanvasGroup` 기반 페이드 알파와 입력 차단을 제어하게 했다.
- `ScreenFadeView`가 DOTween 기반으로 `FadeToAsync()`를 처리할 수 있는 기반을 만들었다.
- `ScreenFadeManager.FadeOutAsync()`, `FadeInAsync()`를 추가했다.
- `LobbyDynamicUIRoot`, `BattleDynamicUIRoot`가 현재 씬의 `ScreenFadeView`를 등록/해제하도록 했다.
- `LobbyScene`의 기존 `ScreenFade` 오브젝트에 `CanvasGroup`과 `ScreenFadeView`를 연결했다.
- `BattleScene`의 `Canvas_DynamicUI` 아래에 `ScreenFade` 오브젝트를 추가하고 `CanvasGroup`, `ScreenFadeView`를 연결했다.

### 왜 이렇게 했는지

화면 페이드는 씬 전환, 로딩, 전투 진입, 튜토리얼 차단 등 여러 시스템에서 공통으로 요청할 수 있다.
하지만 실제 페이드 오브젝트는 현재 씬의 Canvas 아래에 있어야 하므로 `GameRoot`가 Canvas를 직접 소유하면 안 된다.

따라서 팝업과 같은 원칙으로 구조를 잡았다.

```text
GameRoot.ScreenFadeManager
→ 현재 씬 DynamicUIRoot가 ScreenFadeView 등록
→ 전역에서 FadeOutAsync / FadeInAsync 요청
→ 실제 표시는 현재 씬 Canvas_DynamicUI 아래 ScreenFadeView가 처리
```

현재 `ScreenFade` 오브젝트는 `CanvasGroup`과 스크립트만 갖고 있다.
실제 검은 화면을 보이게 하려면 다음 UI 배치 단계에서 `Image` 또는 전용 그래픽 오브젝트를 추가해야 한다.

## 완료된 작업 10: DOTween 로컬 패키지 설치와 UI 연출 전환

커밋: `Add DOTween package and UI tweens`

### 변경된 파일

- `Packages/manifest.json`
- `Packages/com.demigiant.dotween`
- `Assets/_Project/01_Script/UI/PopupTweenManager.cs`
- `Assets/_Project/01_Script/UI/ScreenFadeView.cs`

### 주요 변경

- `Packages/com.demigiant.dotween` 로컬 패키지를 추가했다.
- `Packages/manifest.json`에 `com.demigiant.dotween` 의존성을 추가했다.
- `PopupTweenManager`의 팝업 열기/닫기 연출을 DOTween 기반으로 바꿨다.
- `ScreenFadeView`의 화면 페이드 보간을 DOTween 기반으로 바꿨다.
- DOTween 완료/중단 콜백을 `UniTask`로 기다릴 수 있도록 변환 함수를 추가했다.

### 왜 이렇게 했는지

팝업, 페이드, 전투 데미지 텍스트, HUD 숫자 변화처럼 화면 연출은 앞으로 계속 늘어난다.
이를 직접 `Time.deltaTime`으로 구현하면 연출마다 반복 코드가 생기고, 일시정지 중에도 돌아야 하는 UI 연출 처리가 복잡해진다.

그래서 UI 연출은 DOTween으로 통일했다.
다만 DOTween DLL을 그대로 참조하면 현재 로컬 C# 프로젝트 타깃과 맞지 않아 `dotnet build`가 깨졌다.
그래서 DLL 방식이 아니라 DOTween 소스를 `Packages/com.demigiant.dotween` 로컬 패키지에 넣는 방식으로 정리했다.

이 방식은 Unity Package Manager가 프로젝트 의존성으로 인식할 수 있고, 로컬 컴파일도 통과한다.

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
- `ScreenFade`
- `TopCurrencyHud`

`BattleScene`

- `BattleSceneRoot`
- `Canvas_StaticUI`
- `Canvas_DynamicUI`
- `BattleStaticUIRoot`
- `DimLayer`
- `PopupLayer`
- `ToastLayer`
- `ScreenFade`

현재는 `LobbyScene`과 `BattleScene`의 SceneRoot, StaticUIRoot, DynamicUIRoot 연결이 완료되었다.
각 씬의 Dynamic UI Root는 현재 씬의 `PopupLayer`, `DimLayer`, `ToastLayer`를 `PopupManager`에 등록한다.
각 씬의 Dynamic UI Root는 현재 씬의 `ScreenFade`를 `ScreenFadeManager`에 등록한다.
`LobbyScene`의 `TopCurrencyHud`에는 `CurrencyHud` View가 연결되어 있고, `LobbyStaticUIRoot`가 `CurrencyHudBinder`를 통해 `CurrencyProgress`와 연결한다.

## 다음 작업 계획

### 1순위: DataTable 방향 확정

- `Assets/_Project/05_Data` 아래에 테이블 원본과 생성물 폴더를 만든다.
- `DataTableManager`가 읽을 데이터 형식을 TSV/CSV/ScriptableObject 중에서 확정한다.
- 초기 테이블 스키마는 Hero, Enemy, Stage, Wave, Skill, Reward 중심으로 잡는다.

### 2순위: Currency HUD 실제 표시 오브젝트 구성

- `TopCurrencyHud` 아래에 골드/젬 텍스트 오브젝트를 추가한다.
- `CurrencyHud.goldText`, `CurrencyHud.gemText` Inspector 참조를 연결한다.
- 모바일 해상도 기준으로 상단 HUD 위치와 크기를 정리한다.

### 3순위: Battle Dynamic UI 보조 레이어

- 전투 `FloatingTextLayer`, `UnitHpBarLayer` 실제 오브젝트 추가 또는 기존 오브젝트 지정
- Unity 에디터에서 Inspector 연결 상태 확인
- GitHub Actions로 PlayMode 테스트 확인

### 4순위: ScreenFade 실제 그래픽 구성

- `ScreenFade` 아래에 전체 화면 검은 Image를 추가한다.
- `ScreenFadeView`가 해당 그래픽을 통해 실제 화면 암전을 보여주게 한다.
- 모바일 해상도 기준으로 Safe Area와 전체 화면 덮임 여부를 확인한다.

### 5순위: DataTable 후보 구조

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


## 완료된 작업 11: 낮/밤 방어전 중심 아키텍처 재정렬

커밋: `Realign architecture around night defense loop`

### 변경된 파일

- `Docs/ARCHITECTURE_BASELINE.md`
- `Assets/_Project/01_Script/Core/GameContext.cs`
- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/Core/GameState.cs`
- `Assets/_Project/01_Script/Model/SaveData.cs`
- `Assets/_Project/01_Script/Model/GameProgress.cs`
- `Assets/_Project/01_Script/Model/DayProgress.cs`
- `Assets/_Project/01_Script/Model/CombatSlotProgress.cs`
- `Assets/_Project/01_Script/Model/NightDefenseProgress.cs`
- `Assets/_Project/01_Script/Model/DraftProgress.cs`
- `Assets/_Project/01_Script/Model/RewardProgress.cs`
- `Assets/_Project/01_Script/Model/BattleProgress.cs`
- `Assets/_Project/01_Script/Scene/BattleSceneRoot.cs`

### 주요 변경

- 프로젝트 기준 문서 `Docs/ARCHITECTURE_BASELINE.md`를 추가했다.
- `GameContext`의 중심 모델을 일반 Battle이 아니라 낮/밤 방어전 루프 기준으로 재정렬했다.
- `DayProgress`를 추가해 낮 준비 단계의 스파이어, 성채 층, 채굴, 마법 도서관 해금 상태를 관리하게 했다.
- `CombatSlotProgress`를 추가해 영웅 개별 레벨보다 전투 슬롯 성장과 배치를 우선 구조로 잡았다.
- `NightDefenseProgress`를 추가해 밤 방어 세션, 웨이브, 보스 웨이브, 경과 시간을 관리하게 했다.
- `DraftProgress`를 추가해 전투 중 1-of-3 로그라이트 카드 선택 상태를 관리하게 했다.
- `SaveData`를 `CurrentStageId`, `BattleSlot` 중심에서 `CurrentDefenseSessionId`, `DayCycle`, `CombatSlot` 중심으로 바꿨다.
- `GameState`를 `BattlePlaying` 중심에서 `DayPreparation`, `NightDefensePlaying`, `DraftSelection`, `NightDefenseResult` 중심으로 바꿨다.
- `BattleSceneRoot`가 `BattleProgress` 대신 `NightDefenseProgress`를 시작/종료하게 바꿨다.

### 왜 이렇게 바꿨는지

기존 구조는 UI, 씬 전환, 팝업 같은 공통 기반은 괜찮았지만 게임 핵심이 일반 자동전투 RPG처럼 보이는 문제가 있었다.
Nightfall Spire 계열은 단순 스테이지 전투보다 낮 준비와 밤 방어 세션, 전투 슬롯 성장, 로그라이트 카드 선택이 중심이다.

따라서 코드의 1급 모델 이름부터 `DayProgress`, `CombatSlotProgress`, `NightDefenseProgress`, `DraftProgress`로 바꿔야 이후 데이터 테이블과 전투 시스템이 엉뚱한 방향으로 가지 않는다.

### 현재 남겨둔 것

`BattleProgress`, `BattleScene`, `BattleStaticUIRoot`, `BattleDynamicUIRoot` 이름은 아직 남겨뒀다.
Unity 씬 연결과 `.meta` 리스크를 줄이기 위해 이번 작업에서는 내부 모델 중심만 바로잡았다.
나중에 씬 파일까지 안정적으로 다룰 때 `NightDefenseSceneRoot` 계열로 이름을 정리하는 것이 좋다.

## 완료된 작업 12: GameFlowController 추가

커밋: `Add game cycle director`

### 변경된 파일

- `Assets/_Project/01_Script/Core/GameFlowController.cs`
- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/Model/NightDefenseProgress.cs`
- `Assets/_Project/01_Script/Scene/BattleSceneRoot.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 낮 준비, 밤 방어전 시작, 드래프트 선택, 방어 결과, 낮 복귀 흐름을 관리하는 `GameFlowController`를 추가했다.
- `GameRoot`가 `GameFlowController`를 생성하고 전역에서 접근할 수 있게 했다.
- `BattleSceneRoot`가 직접 `NightDefenseProgress`를 시작하지 않고 `GameFlowController.BeginLoadedNightDefenseSession()`을 호출하게 바꿨다.
- `NightDefenseProgress`에 `DefenseOutcome`과 `LastOutcome`을 추가해 밤 방어 성공/실패/중단 결과를 보관하게 했다.
- 기준 문서에 `GameFlowController`를 상태 전환의 기본 진입점으로 명시했다.

### 왜 이렇게 바꿨는지

이전 수정은 모델 이름은 바로잡았지만, 상태 전환을 누가 책임지는지 부족했다.
게임 루프가 복잡해질수록 UI 버튼, 전투 컨트롤러, 팝업이 각각 Progress를 직접 바꾸면 상태가 꼬인다.

그래서 낮/밤 루프의 흐름은 `GameFlowController`가 지휘하고, Progress 모델은 현재 값을 담는 역할에 집중하게 분리했다.

## 완료된 작업 13: MVP 데이터 테이블 로더 추가

커밋 예정: `Add MVP data table loader`

### 변경된 파일

- `Assets/_Project/05_Data/Tables/*.tsv`
- `Assets/_Project/01_Script/Data/Enums/GameDataEnums.cs`
- `Assets/_Project/01_Script/Data/Core/ITableRow.cs`
- `Assets/_Project/01_Script/Data/Core/DataTable.cs`
- `Assets/_Project/01_Script/Data/Core/TsvParser.cs`
- `Assets/_Project/01_Script/Data/Core/TsvRow.cs`
- `Assets/_Project/01_Script/Data/Core/IntPair.cs`
- `Assets/_Project/01_Script/Data/Rows/GameDataRows.cs`
- `Assets/_Project/01_Script/Core/DataTableManager.cs`
- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Docs/DATA_TABLE_ARCHITECTURE.md`
- `Docs/CODEX_DATA_TABLE_TASK.md`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- ChatGPT에서 받아온 MVP TSV 테이블을 프로젝트 데이터 폴더에 반영했다.
- TSV를 런타임 Row 객체로 바꾸는 `TsvParser`, `TsvRow`, `DataTable<T>` 구조를 추가했다.
- 모든 MVP 테이블에 대응하는 Row 클래스를 추가했다.
- `DataTableManager`가 게임 시작 시 모든 TSV를 읽고 조회 인덱스를 구성하게 바꿨다.
- 밤 방어전 핵심 연결인 세션, 웨이브, 드래프트 카드 효과, 보상, 슬롯 업그레이드 조회 함수를 추가했다.
- `GameRoot` 초기화 흐름에서 저장 데이터를 읽기 전에 데이터 테이블을 먼저 로드하게 했다.

### 왜 이렇게 바꿨는지

기존 구조는 게임 진행 모델과 UI/씬 전환의 뼈대는 있었지만, 실제 Nightfall Spire식 루프를 움직일 기준 데이터가 없었다.
이번 작업으로 `DefenseSessionData -> WaveGroupData -> WaveData -> EnemyData`, `DraftCardData -> DraftCardEffectData`, `RewardData` 흐름을 코드에서 바로 조회할 수 있게 했다.

이제 다음 전투 런타임은 하드코딩된 웨이브나 카드가 아니라 테이블 기준으로 붙일 수 있다.
테이블 파일은 밸런스와 콘텐츠 담당이 바꾸고, 런타임 코드는 `DataTableManager`를 통해 같은 기준을 읽는 구조로 간다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.
