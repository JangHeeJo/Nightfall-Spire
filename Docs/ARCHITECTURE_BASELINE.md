# Nightfall Spire 아키텍처 기준

이 문서는 현재 프로젝트가 일반 자동전투 RPG로 흐르지 않도록 잡아두는 기준 문서입니다.
레퍼런스 게임을 그대로 복제하지 않고, 공개 설명에서 확인되는 장르 구조를 바탕으로 독자 게임 구조를 설계합니다.

## 레퍼런스에서 확인한 핵심

공개 설명 기준으로 Nightfall Spire 계열의 중심은 아래 흐름입니다.

```text
낮 준비
→ 스파이어/성채/층/전투 슬롯/방어 모듈/마법 도서관/채굴/제작 성장
→ 영웅과 슬롯 배치
→ 밤 방어전 시작
→ 웨이브와 보스 방어
→ 전투 중 1-of-3 로그라이트 카드 선택
→ 카드 누적과 시너지로 화력 변화
→ 보상 획득
→ 다시 낮 성장으로 재투자
```

참고한 공개 자료:

- Google Play: Build by day, defend by night / tower defense / roguelite drafting / base building / mining
- QooApp, APKPure, FileHippo: 전투 슬롯 업그레이드, 새 영웅 즉시 전력화, 성채 층, 마법 도서관, 방어 모듈, 전투 중 로그라이트 카드

## 설계 원칙

- 일반 `Stage -> Battle -> Reward` 구조로 단순화하지 않는다.
- 중심 단위는 `DefenseSession`이다.
- 낮 성장과 밤 방어를 코드와 데이터에서 분리한다.
- 영웅 개별 레벨보다 `CombatSlot` 성장을 우선 축으로 둔다.
- 로그라이트 드래프트는 부가 기능이 아니라 전투의 핵심 시스템으로 둔다.
- UI와 씬은 게임 루프를 보여주는 계층이며, 게임 규칙의 중심이 아니다.
- UI 구조는 MVP를 기준으로 둔다. View는 Unity 표시와 입력 이벤트만 맡고, Presenter가 Model 구독과 View 갱신을 담당한다.
- 테이블이 붙기 전에도 런타임 모델 이름이 최종 게임 구조를 드러내야 한다.

## 현재 코드 기준 구조

```text
GameRoot
→ GameContext
  → GameProgress
  → CurrencyProgress
  → DayProgress
  → CombatSlotProgress
  → NightDefenseProgress
  → DraftProgress
  → RewardProgress
  → PopupProgress
  → NightDefenseSessionService
  → DraftService
  → RewardService
  → DayGrowthService
```

### GameProgress

게임 전체 상태와 낮/밤 루프 진행을 관리합니다.
현재는 다음 밤 방어 세션 ID와 완료한 낮/밤 루프 수를 보유합니다.
상태 변경은 `GameStateMachine` 규칙을 통과해야만 반영됩니다.

주요 값:

- `CurrentDefenseSessionId`
- `CompletedDayCount`
- `CurrentState`
- `PreviousState`

### GameStateMachine

게임 전체 상태 전환 규칙을 검증합니다.
아래처럼 큰 흐름을 벗어나는 전환은 막습니다.

```text
None
→ Boot
→ DayPreparationLoading
→ DayPreparation
→ NightDefenseLoading
→ NightDefenseReady 또는 NightDefensePlaying
→ DraftSelection
→ NightDefensePlaying
→ NightDefenseResult
→ DayPreparationLoading
```

`AppBackground`는 현재 상태를 `PreviousState`에 저장하고, 복귀 시 이전 플레이 흐름 상태로만 돌아가게 합니다.
UI, 서비스, 씬 루트가 직접 상태를 바꾸더라도 `GameProgress.ChangeState()`를 통해 이 규칙을 거치도록 합니다.
`ChangeState()`는 성공 여부를 반환하므로, 흐름 제어 계층은 상태 변경 실패 시 다음 처리를 진행하지 않아야 합니다.

### DayProgress

낮 준비 단계의 성장 상태를 관리합니다.
스파이어, 성채 층, 채굴 진행, 마법 도서관 해금 같은 값이 여기에 들어갑니다.

주요 값:

- `SpireLevel`
- `CitadelFloorCount`
- `MiningDepth`
- `MagicLibraryUnlocked`

### CombatSlotProgress

전투 슬롯 성장과 영웅 배치를 관리합니다.
이 구조는 새 영웅이 들어와도 슬롯 성장에 의해 즉시 전력화될 수 있게 하기 위한 핵심 모델입니다.

주요 동작:

- 슬롯 해금
- 슬롯 업그레이드
- 슬롯에 영웅 배치
- 해금된 슬롯 수 조회

### NightDefenseProgress

밤 방어전 한 판의 런타임 상태를 관리합니다.
씬 이름은 아직 `BattleScene`이지만, 내부 모델은 `NightDefenseProgress`를 기준으로 움직입니다.

주요 값:

- `IsDefenseActive`
- `CurrentDefenseSessionId`
- `CurrentWaveIndex`
- `IsBossWave`
- `ElapsedSeconds`

### DraftProgress

전투 중 1-of-3 로그라이트 카드 선택 상태를 관리합니다.
실제 카드 효과 적용은 이후 `DraftEffectResolver` 또는 전투 시스템에서 처리합니다.

주요 값:

- `IsDraftOpen`
- `OfferedCardIds`
- `SelectedCardIds`

## 기존 구조에서 바로잡은 점

기존에는 `BattleProgress`가 전투 진입 여부만 들고 있었고, 구조적으로 일반 전투 씬처럼 보였습니다.
이제 `BattleProgress`는 호환용으로만 남기고, 실제 중심 상태는 `NightDefenseProgress`로 옮겼습니다.

기존에는 `CurrentStageId`가 진행 기준이었지만, 이 게임에는 단순 스테이지보다 밤 방어 세션이 더 적합합니다.
그래서 저장/런타임 진행 기준을 `CurrentDefenseSessionId`로 바꿨습니다.

기존에는 `BattleSlot`이라는 저장 데이터가 있었지만 의미가 약했습니다.
이제 `CombatSlot`으로 이름을 바꾸고, 영웅 개별 성장보다 슬롯 성장 중심이라는 설계 의도를 코드에 드러냈습니다.

## 다음에 붙일 시스템

### 1순위: 데이터 테이블

- `DefenseSessionData`
- `WaveGroupData`
- `WaveData`
- `EnemyData`
- `DraftPoolData`
- `DraftCardData`
- `DraftCardEffectData`
- `CombatSlotUpgradeData`
- `SpireUpgradeData`
- `CitadelFloorData`
- `BuildingModuleData`
- `MiningNodeData`
- `RewardData`

현재 데이터 테이블은 `Assets/_Project/05_Data/Tables`의 TSV를 기준으로 로드합니다.
`DataTableManager`가 앱 시작 시 모든 테이블을 읽고, 런타임 로직은 TSV 파일을 직접 읽지 않고 Row 객체와 조회 함수만 사용합니다.

핵심 조회:

- `DefenseSessionData`로 현재 밤 방어 세션의 웨이브 그룹, 드래프트 풀, 보상 그룹을 찾습니다.
- `GetWaveRows(waveGroupId, waveIndex)`로 해당 웨이브의 스폰 구성을 찾습니다.
- `GetCombatSlotUpgrade(upgradeGroupId, level)`로 슬롯 성장 수치와 비용을 찾습니다.
- `GetRewardRows(rewardGroupId)`로 방어 결과 보상 목록을 찾습니다.
- `GetDraftCardEffects(cardId)`로 선택한 카드의 적용 효과를 찾습니다.

이 구조가 필요한 이유는 밤 방어전, 드래프트, 낮 성장 시스템이 서로 같은 테이블 기준을 봐야 하기 때문입니다.
전투 시스템이나 UI가 TSV를 직접 파싱하면 같은 데이터가 여러 방식으로 해석될 수 있으므로, 테이블 파싱은 `DataTableManager` 한 곳에 묶습니다.

### 1-1순위: 도메인 서비스

테이블을 읽는 것만으로는 게임 규칙이 고정되지 않습니다.
따라서 Progress 모델을 직접 조작하지 않고, 도메인 서비스가 테이블 조건을 검증한 뒤 Progress에 결과를 반영하는 구조를 기본으로 둡니다.

- `NightDefenseSessionService`: 방어 세션 입장 조건, 웨이브 진행, 보스/드래프트 웨이브 판단, 스폰 계획 생성을 담당합니다.
- `DraftService`: 드래프트 풀의 포함/제외 태그, PickCount, 이미 선택한 고유 카드 조건을 기준으로 후보를 만듭니다.
- `RewardService`: 보상 그룹을 계산하고 현재 RewardProgress가 표현할 수 있는 재화 보상을 수령 대기 상태로 반영합니다.
- `DayGrowthService`: 성채 층 해금, 전투 슬롯 업그레이드, 비용 지불, 해금 기능 반영을 담당합니다.

서비스는 `DataTableManager`에 직접 강하게 묶이지 않고 조회 인터페이스를 통해 데이터를 받습니다.
실제 런타임에서는 `GameContentDataSource`가 `DataTableManager`를 감싸고, 테스트에서는 fake data source가 같은 계약을 구현합니다.

### 2순위: 밤 방어 런타임

- 웨이브 진행기
- `NightDefenseWavePlan` 기반 적 스폰 컨트롤러
- 전투 슬롯 공격 루프
- 타겟 선택과 피해 적용
- 방어 세션 타이머
- 보스 웨이브 판단
- 방어 성공/실패 처리

`WaveDataRow`는 테이블 원본이며, 실제 스폰러가 직접 해석하지 않습니다.
`NightDefenseSessionService.TryAdvanceNextWave()`가 웨이브 Row를 검증하고 `NightDefenseWavePlan`으로 변환합니다.
전투 씬의 스폰 컨트롤러는 `NightDefenseSpawnEvent` 목록만 보고 적 생성 시간, 라인, 적 ID를 처리해야 합니다.

```text
WaveDataRow
→ NightDefenseWavePlanBuilder
→ NightDefenseWavePlan
→ NightDefenseRuntimeController
→ NightDefenseSpawnController
```

이 구조가 필요한 이유는 웨이브 테이블 해석 규칙을 전투 MonoBehaviour 안에 흩뿌리지 않기 위해서입니다.
테이블 값 검증, 시간표 정렬, 마지막 스폰 시간 계산은 순수 C#에서 끝내고, Unity 씬은 프리팹 생성과 위치 배치만 맡습니다.

`NightDefenseRuntimeController`는 세션 서비스에서 다음 웨이브 계획을 받아오고, 세션 경과 시간과 웨이브 스폰 실행을 함께 진행합니다.
`NightDefenseSpawnController`는 `NightDefenseWavePlan`의 `NightDefenseSpawnEvent`를 시간에 맞춰 `NightDefenseSpawnRequest`로 바꿉니다.
실제 Unity 적 프리팹 생성기는 `INightDefenseSpawnSink`를 구현해 스폰 요청을 받는 방식으로 붙입니다.
`BattleSceneRoot`는 씬 진입 후 밤 방어 세션이 시작되면 `NightDefenseBattleRuntime`을 초기화합니다.
현재 `UnityNightDefenseSpawnSink`는 실제 적 생성 전 단계이므로 스폰 요청을 로그로만 받으며, 이후 `EnemyFactory`와 오브젝트 풀로 교체합니다.

전투 숫자 규칙은 `CombatRuntimeController`에서 처리합니다.
이 클래스는 Unity 오브젝트를 만들지 않고, 테이블에서 읽은 영웅/적/슬롯 성장 값과 현재 `CombatSlotProgress`만 보고 공격 가능한 슬롯, 살아 있는 적, 피해 결과를 계산합니다.

```text
CombatSlotProgress
→ ICombatDataSource
→ CombatRuntimeController
→ CombatTargetingService
→ CombatDamageResolver
→ CombatRuntimeTickResult
```

`CombatTargetingService`는 `TargetingType`에 맞춰 적을 고르고, `CombatDamageResolver`는 공격력과 방어력 기준으로 실제 피해를 적용합니다.
이 구조가 필요한 이유는 전투 규칙을 MonoBehaviour, 프리팹, 애니메이션 이벤트 안에 섞지 않기 위해서입니다.
이후 투사체, 스킬, 드래프트 효과, 속성 상성은 이 전투 런타임 위에 별도 Resolver로 붙입니다.

### 3순위: 드래프트 런타임

- 카드 후보 추첨
- 1-of-3 UI 연결
- 카드 효과 적용기
- 카드 태그/시너지 판정

### 4순위: 낮 성장 런타임

- 스파이어 업그레이드
- 전투 슬롯 업그레이드 비용 처리
- 성채 층/시설 해금
- 채굴 보상
- 제작/장비 구조

## 주의할 점

- `BattleScene` 파일명과 `BattleStaticUIRoot` 같은 기존 씬 이름은 당장 바꾸지 않는다.
  씬 파일 연결과 Unity 메타 리스크가 있으므로, 먼저 내부 모델과 데이터 구조를 바로잡고 나중에 이름 변경을 진행한다.
- 레퍼런스의 고유 캐릭터, 카드명, 수치, 아트는 복제하지 않는다.
- 구조는 참고하되, 플레이 감각과 콘텐츠는 독자적으로 설계한다.

## GameFlowController 기준

`GameFlowController`는 낮/밤 루프의 상태 전환 진입점입니다.
앞으로 UI 버튼, 전투 컨트롤러, 결과 팝업은 `GameProgress`, `NightDefenseProgress`, `DraftProgress`를 직접 조합해서 상태를 바꾸지 않고 이 클래스를 통해 요청합니다.
각 요청은 상태 변경 성공 여부를 반환해야 하며, 실패한 상태 전환 뒤에 Progress 값을 계속 바꾸지 않는 것을 원칙으로 합니다.

주요 책임:

- 낮 준비 상태 진입
- 밤 방어전 씬 로드 요청
- 씬 로드 후 밤 방어 세션 시작
- 전투 중 드래프트 선택지 열기
- 드래프트 카드 선택 확정
- 밤 방어 성공/실패 결과 확정
- 결과 확인 후 낮 준비 화면 복귀

이 구조가 필요한 이유는 게임의 핵심 루프가 단순 전투 시작/종료가 아니기 때문입니다.
밤 방어전은 웨이브, 드래프트, 보상, 다음 낮 성장으로 이어지는 흐름을 반드시 함께 관리해야 합니다.
따라서 개별 UI나 전투 시스템이 Progress를 직접 건드리기 시작하면 출시 단계에서 상태 꼬임이 생길 가능성이 큽니다.

밤 방어 씬 로드가 끝나면 바로 `NightDefensePlaying`으로 뛰지 않고 `NightDefenseReady`를 거칩니다.
이 단계에서 `NightDefenseSessionService`가 현재 세션 입장 조건과 테이블 연결을 검증하고, 성공한 경우에만 실제 플레이 상태로 진입합니다.

## Lobby Screen MVP

로비 화면 입력은 버튼 단위가 아니라 화면 단위 MVP 구조를 기준으로 연결합니다.

```text
LobbyScreen
→ LobbyPresenter
→ GameFlowController.LoadNightDefenseAsync()
→ SceneLoadManager.LoadBattleSceneAsync()
→ BattleSceneRoot
→ GameFlowController.BeginLoadedNightDefenseSession()
```

`LobbyScreen`은 로비 화면 전체 View입니다.
개별 버튼마다 View/Presenter 클래스를 만들지 않고, 밤 방어 시작, 하단 탭, 업그레이드 진입 같은 로비 화면 액션을 화면 단위 이벤트로 모읍니다.
`LobbyPresenter`는 중복 클릭 방지, 로드 중 비활성화, 로드 거부 시 재활성화처럼 로비 화면 액션의 흐름을 담당합니다.
`LobbyStaticUIRoot`는 `LobbyScreen`과 `LobbyPresenter`를 조립만 하고, 상태 전환 판단은 `GameFlowController`에 맡깁니다.

재화 HUD처럼 여러 화면에서 재사용되거나 독립 상태 구독이 필요한 위젯은 별도 Presenter를 둘 수 있습니다.
하지만 단순 버튼 하나를 이유로 `ButtonNameView`, `ButtonNamePresenter`를 계속 추가하지 않습니다.

현재 로비 씬의 `FightStartView` 이름 오브젝트는 `LobbyScreen`이 밤 방어 시작 버튼으로 찾아 사용합니다.
이후 실제 로비 UI 디자인이 확정되면 프리팹에 Button, Image, TextMeshPro 라벨을 명시적으로 구성하고 `LobbyScreen`의 화면 단위 계약을 유지합니다.

## Popup Architecture

팝업은 단순 생성/닫기 관리자가 아니라 요청, 정책, 결과가 있는 최상단 UI 흐름으로 다룹니다.
단, 공용 알림/확인 팝업 프리팹을 기본 전제로 두지 않습니다.
실제 팝업 UI는 각 팝업 프리팹이 자기 스크립트를 하나씩 갖고, 필요한 입력과 표시 기능을 그 스크립트 안에서 담당합니다.

```text
PopupRequest
→ PopupManager
→ BasePopup
→ 각 팝업 UI 스크립트
→ PopupHandle.ResultTask
→ PopupResult
```

`PopupRequest`는 어떤 팝업 프리팹을 어떤 정책으로 열지 명시합니다.
`PopupOpenPolicy`는 같은 팝업이 이미 열려 있을 때 쌓을지, 재사용할지, 위 팝업만 교체할지, 전체를 교체할지 결정합니다.
`PopupHandle`은 실제 열린 팝업 인스턴스와 닫힘 결과 대기 태스크를 보관합니다.
`PopupResult`는 확인, 취소, 배경 닫기, 교체, 씬 전환 같은 닫힘 이유와 선택 결과 Payload를 전달합니다.
이 계약 타입들은 `PopupContracts.cs` 한 파일에 모아둡니다.
값 몇 개짜리 enum이나 작은 DTO를 파일마다 쪼개지 않고, 실제 동작을 가진 `PopupManager`, `BasePopup`, `PopupTweenManager`만 별도 파일로 유지합니다.

기준은 다음과 같습니다.

- `PopupManager`는 최상단 팝업 관리자이며, 씬 레이어, 딤, 스택, 중복 정책, 결과 완료만 담당합니다.
- `BasePopup`은 모든 팝업 UI 스크립트가 상속받는 공통 생명주기입니다.
- 각 팝업 UI는 실제 기획 기능 이름을 가진 자기 스크립트 하나를 갖고 자기 기능을 처리합니다.
- 공용 알림/확인 팝업을 먼저 만들지 않습니다. 필요하면 실제 기획 UI 이름을 가진 팝업으로 만듭니다.
- 팝업마다 무조건 Presenter를 만들지 않습니다. 팝업 자체 스크립트로 충분하면 `BasePopup` 상속 스크립트 하나에서 처리합니다.
- 드래프트 선택처럼 모델 상태, 선택 결과, 카드 효과 적용 흐름이 복잡한 팝업만 Presenter 분리를 검토합니다.
- 씬 전환으로 레이어가 해제되면 열린 팝업 결과는 `SceneChanged`로 완료합니다.
- 팝업이 하나라도 딤을 요구하면 DimLayer가 뒤쪽 UI 입력을 막고, 딤이 필요 없는 토스트성 팝업은 PopupRequest에서 `UseDim = false`로 엽니다.

## Contract File Rule

작은 enum, result, request, event 값 타입은 파일을 무작정 늘리지 않고 콘텐츠별 Contracts 파일에 모읍니다.

- 데이터 테이블 enum: `GameDataEnums.cs`
- 팝업 요청/정책/결과: `PopupContracts.cs`
- 낮 성장 결과/실패 이유: `DayGrowthContracts.cs`
- 드래프트 결과/실패 이유: `DraftContracts.cs`
- 보상 결과/실패 이유: `RewardContracts.cs`
- 밤 방어 세션/런타임/스폰 계약: `NightDefenseContracts.cs`

독립 파일로 남기는 기준은 실제 동작 책임이 큰 클래스이거나 파일 하나가 읽기 어려울 정도로 커지는 경우입니다.
예를 들어 `PopupManager`, `NightDefenseRuntimeController`, `NightDefenseWavePlan`, `GameStateMachine`은 독립 파일로 유지합니다.
반대로 값 몇 개짜리 enum, 단순 Result/Request struct는 관련 콘텐츠 Contracts 파일에 함께 둡니다.

## Boot Loading MVP

BootScene의 로딩 UI는 MVP 기준으로 붙입니다.
씬 배치와 실제 UI 디자인은 Unity에서 직접 작업하고, 코드는 아래 책임만 갖습니다.

```text
GameRoot.InitializeAsync()
→ BootLoadingPresenter
→ IBootLoadingView
→ BootLoadingView
```

`BootLoadingView`는 Slider, Image Fill, TextMeshPro 참조를 받아 화면에 표시만 합니다.
`BootLoadingPresenter`는 진행률 범위 보정, 상태 문구, 버전 텍스트 전달만 담당합니다.
`GameRoot`는 데이터 로드, 저장 로드, 시스템 준비, 로비 씬 로드 같은 실제 초기화 단계가 끝날 때마다 Presenter에 진행률을 보고합니다.

BootScene에 로딩 View가 없어도 게임 시작은 실패하지 않아야 합니다.
따라서 `GameRoot`는 `BootLoadingView`를 찾지 못하면 로딩 UI 없이 초기화 흐름을 계속 진행합니다.

로딩 배경은 `LightLoadingBg`와 `NightLoadingBg`를 같은 위치에 겹쳐 두고, `BootLoadingView`가 DOTween으로 밤 배경 알파를 서서히 올렸다 내리는 방식으로 순환시킵니다.
기본값은 5초 유지, 1.2초 전환입니다.
로딩바는 `LoadingBar.prefab`을 `LoadingBarContainer` 아래에 런타임으로 생성하고, 내부 Slider를 자동으로 찾아 진행률을 반영합니다.

현재 BootScene 기준 하이어라키는 아래처럼 둡니다.

```text
BootScene
├─ Main Camera
├─ GameRoot
└─ Canvas_BootUI
   └─ BootLoadingRoot
      ├─ LightBackground
      ├─ NightBackground
      └─ LoadingBarContainer
```

`BootLoadingRoot`에는 `BootLoadingView`가 붙어 있습니다.
실제 UI 작업자는 이 오브젝트 아래 배경 이미지와 로딩바 위치를 조정하고, 상태 텍스트, 퍼센트 텍스트, 버전 텍스트가 필요할 때만 추가로 배치한 뒤 Inspector에 연결합니다.
BootScene의 `GameRoot`에는 실제 부트스트랩 컴포넌트만 유지하고, 연습용 스크립트나 임시 테스트 컴포넌트는 붙이지 않습니다.

## Draft Effect Runtime

드래프트 선택은 `DraftProgress`에 선택한 카드 ID만 저장하는 것으로 끝나지 않습니다.
카드 효과는 `DraftEffectResolver`가 `DraftCardEffectDataRow`를 읽어 `CombatRuntimeModifierSet`에 전투 보정값으로 저장합니다.

```text
DraftProgress.CanSelectCard()
→ DraftEffectResolver.ApplyCardEffects()
→ CombatRuntimeModifierSet
→ CombatRuntimeController.RebuildHeroSlots()
```

`CombatRuntimeModifierSet`은 드래프트, 시너지, 장비, 시설 효과처럼 전투 중 슬롯/영웅에 누적되는 보정값을 한곳에 모읍니다.
`CombatRuntimeController`는 슬롯을 다시 구성할 때 슬롯 타입, 영웅 역할, 영웅 태그 기준으로 적용 가능한 보정만 합산합니다.

현재 연결된 효과는 공격력, 공격 속도, 사거리, 스킬 충전 비율입니다.
아직 전투 런타임에 실제 동작이 없는 투사체 수, 체인, 상태 이상 같은 효과는 무리하게 하드코딩하지 않고 별도 Resolver가 생긴 뒤 연결합니다.

## Night Defense Completion

밤 방어 결과 확정은 테이블의 세션 보상 그룹을 기준으로 처리합니다.

```text
GameFlowController.CompleteNightDefense(outcome)
→ NightDefenseCompletionService
→ DefenseSessionData.RewardGroupId
→ RewardService.BuildReward()
→ RewardProgress.SetPendingReward()
→ GameProgress.CompleteDefenseSession()
```

최초 클리어 보상 판단은 `GameProgress.CompleteDefenseSession()`보다 먼저 해야 합니다.
그래서 `NightDefenseCompletionService`가 현재 최고 클리어 세션을 먼저 보고 보상을 계산한 뒤, `GameFlowController`가 진행도와 낮/밤 루프 완료를 반영합니다.

테스트나 임시 개발용으로 이미 계산된 보상 숫자를 넘기는 `CompleteNightDefense(outcome, gold, gem)` 경로는 유지합니다.
하지만 실제 런타임에서는 세션 테이블을 읽는 `CompleteNightDefense(outcome)` 경로를 기본으로 사용합니다.
