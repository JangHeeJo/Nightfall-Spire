# Nightfall Spire Codex 작업 요약

이 문서는 Codex와 ChatGPT가 같은 기준으로 프로젝트를 검토할 수 있도록 만든 공유 요약 문서입니다.
현재 프로젝트는 기존 모바일 게임 Nightfall Spire 스타일을 참고하되, 그대로 복제하지 않고 출시 가능한 독자 Unity 모바일 게임으로 재설계하는 방향입니다.

## 기본 약속

- 답변과 작업 요약은 한국어로 작성한다.
- 사용자의 말에 무조건 동의하지 않고, 출시 기준에서 더 나은 구조가 있으면 먼저 제안한다.
- 실제 Unity 프로젝트 파일과 현재 코드 기준으로 판단한다.
- `GameRoot`, `GameContext`, 순수 Model, 씬별 Root, MVC UI 구조를 기본 골격으로 사용한다.
- `GameRoot`는 전역 시작점이지만 Canvas를 들고 다니지 않는다.
- 실제 UI Canvas와 Layer는 각 씬이 소유한다.
- 팝업은 씬에 미리 배치하지 않고 Prefab으로 생성하고 닫으면 파괴한다.
- 비동기 흐름은 `UniTask`를 사용한다.
- 상태 구독은 `R3`를 사용하되, UI에서는 Controller가 구독과 입력 흐름을 소유한다.
- UI 연출은 `DOTween`을 사용한다.
- Coroutine 사용은 금지한다.
- 불필요한 `MonoBehaviour` 매니저를 늘리지 않는다.
- 코드 주석은 한국어로 작성한다.
- 긴 XML 주석보다 읽기 쉬운 한 줄 주석을 선호한다.
- 스크립트 이름과 Unity 하이어라키 오브젝트 이름은 가능한 맞춘다.
- Git 작업은 작은 단위 커밋과 PR 기준으로 진행한다.
- `main`은 안정 버전으로 유지하고, 작업은 브랜치에서 진행한다.

## 현재 아키텍처 기준

- Model은 저장 데이터, 진행 상태, 테이블 Row처럼 게임 상태와 규칙 데이터를 담는다.
- View는 Unity UI 표시, 버튼 이벤트 전달, 이미지/텍스트 반영만 맡는다.
- Controller는 View 이벤트를 받아 Model/Service/Popup/Scene 흐름을 실행한다.
- Scene Root는 Unity 씬에 배치된 오브젝트 참조를 모아 Controller와 View를 조립한다.
- PopupManager는 팝업 생성, 닫기, 슬롯, 딤 처리, 생명주기만 담당한다.
- 팝업별 Controller 생성은 `PopupControllerFactory`가 담당한다.
- 팝업 View는 자기 Controller 타입을 직접 생성하지 않는다.
- 하단 탭으로 전환되는 화면은 `PopupLayerSlot.Content` 슬롯에 하나만 유지한다.
- 상세 정보나 확인창은 `PopupLayerSlot.Overlay` 슬롯에 쌓는다.

## 완료된 작업 38: 전투 유닛 공통 애니메이션 재생 규격 추가

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Scene/CombatUnitAnimationState.cs`
- `Assets/_Project/01_Script/Scene/CombatUnitAnimationPlayer.cs`
- `Assets/_Project/01_Script/Scene/UnityNightDefenseSpawnSink.cs`
- `Assembly-CSharp.csproj`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 히어로와 몬스터가 공유할 공통 애니메이션 상태 enum을 추가했다.
- 공통 애니메이션 이름을 `Idle`, `Move`, `Attack`, `Hit`, `Die`로 고정했다.
- `CombatUnitAnimationPlayer`를 추가해 Animator 상태 재생, CrossFade, MoveSpeed/AttackSpeed 파라미터 반영을 한곳에서 처리하게 했다.
- `UnityNightDefenseSpawnSink`가 Animator 파라미터를 직접 검사하고 조작하던 코드를 제거했다.
- 몬스터 표시 상태가 바뀌면 `CombatUnitAnimationPlayer`를 통해 공통 애니메이션 상태만 요청하도록 정리했다.

### 왜 이렇게 바꿨는지

몬스터와 히어로의 애니메이션 이름이 같다면 캐릭터별로 애니메이션 제어 코드를 나누면 안 된다.
전투 계산 상태와 Animator 재생 규칙을 분리해야 캐릭터가 늘어나도 구조가 무너지지 않는다.

그래서 프리팹에는 기존 Animator를 유지하고, 전투 View 계층에서 한 번 캐시한 Animator를 공통 재생기로 감싸는 방식으로 정리했다.
새 캐릭터가 추가되어도 `Idle`, `Move`, `Attack`, `Hit`, `Die` 이름만 맞추면 같은 전투 표시 흐름에 붙일 수 있다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 37: 전투 몬스터 풀링과 상태 관리 보강

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Service/CombatRuntimeContracts.cs`
- `Assets/_Project/01_Script/Service/BattleRuntimeContracts.cs`
- `Assets/_Project/01_Script/Scene/UnityNightDefenseSpawnSink.cs`
- `Assets/_Project/00_Scenes/BattleScene.unity`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `CombatEnemyRuntimeState`에 `CombatEnemyLifecycleState`를 추가했다.
- 몬스터 런타임 상태가 `Spawned`, `Moving`, `ReachedGoal`, `Defeated`로 명확히 기록되도록 했다.
- `UnityNightDefenseSpawnSink`가 더 이상 전투 중 `Instantiate/Destroy`를 반복하지 않고, `PrefabKey`별 몬스터 풀을 사용하도록 바꿨다.
- 전투 씬에서 프리팹별 사전 생성 수량 `preloadCountPerPrefab`을 3으로 저장했다.
- 몬스터가 피해를 받으면 `Hit`, 처치되면 `Defeated`, 성채에 도착하면 `ReachedGoal` 표시 상태로 바뀌도록 했다.
- 처치/성채 도착 직후 바로 비활성화하지 않고 `despawnDelaySeconds` 동안 상태를 보여준 뒤 풀로 반납하게 했다.
- 풀에서 다시 꺼낸 몬스터는 Animator를 초기화해 이전 피격/사망 상태가 남지 않게 했다.

### 왜 이렇게 바꿨는지

전투 몬스터는 웨이브마다 계속 생성되고 제거되므로 매번 생성/파괴하면 모바일 환경에서 비용과 튐 현상이 커진다.
그래서 `UnityNightDefenseSpawnSink`가 몬스터 표시 오브젝트 풀을 소유하고, 전투 런타임은 순수 상태 계산만 맡는 구조로 정리했다.

또한 몬스터가 성채로 걸어가는 흐름은 `PathProgress`만으로는 추적이 약하다.
런타임 상태와 표시 상태를 분리해 두면 이후 이동 애니메이션, 피격 이펙트, 사망 연출, 성채 타격 연출을 같은 상태 흐름 위에 붙일 수 있다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 43: 웨이브 데이터를 스폰 이벤트 순서 기준으로 재정리

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/05_Data/Tables/EnemyData.tsv`
- `Assets/_Project/05_Data/Tables/WaveData.tsv`
- `Assets/_Project/05_Data/Tables/WaveGroupData.tsv`
- `Assets/_Project/01_Script/Data/Rows/GameDataRows.cs`
- `Assets/_Project/01_Script/Service/NightDefenseSessionService.cs`
- `Assets/_Project/01_Script/Service/NightDefenseWavePlanBuilder.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 새 몬스터 프리팹 기준으로 `EnemyData.tsv`의 `PrefabKey`를 정리했다.
- `Monster_01`, `Monster_02`, `Monster_03`, `Monster_04`, `Monster_Elite_01`, `Monster_Boss_01`을 적 데이터로 등록했다.
- `WaveData.tsv`의 `WaveIndex` 의미를 한 전투 안의 스폰 이벤트 순서로 정리했다.
- `WaveGroupData.tsv`의 `MaxWaveIndex`는 스폰 이벤트 개수, `BossWaveIndex`는 보스 이벤트 위치로 쓰도록 맞췄다.
- `NightDefenseSessionService`가 `WaveGroupData.MaxWaveIndex`까지의 `WaveData` Row를 모두 모아 하나의 2분 전투 타임라인으로 캐싱하게 했다.
- `NightDefenseWavePlanBuilder`는 `WaveDataRow.WaveIndex`를 현재 런타임 웨이브 번호와 비교하지 않고, 스폰 이벤트 순서 값으로만 검증하게 했다.

### 왜 이렇게 바꿨는지

전투 안의 스폰 흐름은 고정된 “런타임 웨이브 클리어 후 다음 웨이브”가 아니라, 2분 전투 타임라인 안에서 1웨이브, 2웨이브, 보스웨이브, 3웨이브처럼 유동적으로 배치되는 구조다.

그래서 `WaveData.WaveIndex`를 런타임 분할 단위가 아니라 스폰 이벤트 순서로 해석하게 정리했다.
이제 보스 이벤트 위치는 `WaveGroupData.BossWaveIndex`와 `WaveData.IsBossWave`를 바꾸면 조정할 수 있다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 41: 밤 방어 웨이브 테이블을 2분 전투 타임라인 기준으로 정리

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/05_Data/Tables/WaveData.tsv`
- `Assets/_Project/05_Data/Tables/WaveGroupData.tsv`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 기존처럼 `WaveIndex`를 짧은 개별 웨이브 여러 개로 나누던 값을 정리했다.
- 각 `WaveGroupId`는 `WaveIndex 1` 하나만 사용하도록 맞췄다.
- 하나의 `WaveIndex 1` 안에 2분 전투 타임라인을 넣었다.
- 일반 몬스터 스폰 타이밍은 `0초`, `40초`, `80초` 세 구간으로 잡았다.
- 보스 스폰 타이밍은 `110초`로 잡았다.
- 보스 타이밍에는 보스 몬스터 1마리와 엘리트 몬스터 2마리가 고정으로 스폰되도록 Row를 분리했다.
- `WaveGroupData.tsv`의 `MaxWaveIndex`와 `BossWaveIndex`를 모두 `1`로 맞춰, 테이블에 없는 다음 웨이브를 찾다 실패하지 않게 했다.

### 왜 이렇게 바꿨는지

이 게임의 한 밤 전투는 “짧은 웨이브를 여러 번 넘기는 구조”라기보다, 하나의 2분짜리 전투 구간 안에서 스폰 이벤트가 시간표처럼 찍히는 구조가 더 맞다.

그래서 `WaveData`의 한 `WaveIndex` 안에 여러 스폰 Row를 배치했다.
이렇게 하면 전투 HUD에서는 같은 데이터를 기준으로 흰 해골 마커와 빨간 해골 마커를 그릴 수 있고, 런타임은 별도 분기 없이 같은 웨이브 플랜을 따라 스폰만 처리하면 된다.

카드 선택 시간은 기존 전투 런타임이 `WaitingForDraft` 상태에서 웨이브 Tick과 전투 Tick을 멈추도록 되어 있어, 순수 플레이타임 계산에서 제외되는 구조를 유지했다.

### 검증

- 데이터 테이블 구조를 현재 `NightDefenseSessionService`와 `NightDefenseWavePlanBuilder`가 읽을 수 있는 형태로 맞췄다.

## 완료된 작업 42: 웨이브 종료 드래프트 컬럼과 런타임 분기 제거

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/05_Data/Tables/WaveData.tsv`
- `Assets/_Project/05_Data/Tables/WaveGroupData.tsv`
- `Assets/_Project/01_Script/Data/Rows/GameDataRows.cs`
- `Assets/_Project/01_Script/Service/NightDefenseContracts.cs`
- `Assets/_Project/01_Script/Service/NightDefenseSessionService.cs`
- `Assets/_Project/01_Script/Service/NightDefenseRuntimeController.cs`
- `Assets/_Project/01_Script/Service/NightDefenseWavePlan.cs`
- `Assets/_Project/01_Script/Service/NightDefenseWavePlanBuilder.cs`
- `Assets/_Project/01_Script/Service/BattleSessionRuntimeController.cs`
- `Assets/_Project/99_Test/EditMode/Service/GameContentServiceTests.cs`
- `Assets/_Project/99_Test/EditMode/Service/NightDefenseRuntimeControllerTests.cs`
- `Assets/_Project/99_Test/EditMode/Service/NightDefenseWavePlanTests.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `WaveData.tsv`에서 `DraftAfterWave` 컬럼을 제거했다.
- `WaveGroupData.tsv`에서 `DraftIntervalWave` 컬럼을 제거했다.
- `WaveGroupData.tsv`에 `BattleDurationSec` 컬럼을 추가해 카드 선택 시간을 제외한 순수 전투 시간을 명시했다.
- `WaveDataRow`, `WaveGroupDataRow` 파서를 새 테이블 헤더에 맞췄다.
- `NightDefenseWavePlan`, `NightDefenseWaveResult`, `NightDefenseRuntimeTickResult`에서 웨이브 종료 드래프트 값을 제거했다.
- `BattleSessionRuntimeController`에서 웨이브 클리어 후 드래프트를 여는 분기를 제거했다.
- 관련 EditMode 테스트의 가짜 TSV와 기대값을 새 구조에 맞췄다.

### 왜 이렇게 바꿨는지

드래프트는 웨이브가 끝났을 때 열리는 시스템이 아니라, 전투 중 적 처치로 경험치를 얻고 레벨업했을 때 열리는 시스템이다.

그래서 웨이브 테이블은 스폰 구성과 스폰 타이밍만 담당하게 정리했다.
골드, 젬, 경험치는 이후 몬스터 처치 결과에서 직접 지급하고, 경험치가 누적되어 최대 전투 레벨 10까지 오를 때마다 전투를 멈추고 카드 드래프트를 여는 구조로 이어가야 한다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 38: 성채 공격형 몬스터 흐름과 스폰 겹침 완화

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Service/CombatRuntimeContracts.cs`
- `Assets/_Project/01_Script/Service/CombatRuntimeController.cs`
- `Assets/_Project/01_Script/Service/BattleRuntimeContracts.cs`
- `Assets/_Project/01_Script/Service/NightDefenseWavePlanBuilder.cs`
- `Assets/_Project/01_Script/Scene/UnityNightDefenseSpawnSink.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 몬스터가 성채에 도착하자마자 제거되지 않도록 전투 규칙을 바꿨다.
- 몬스터 상태를 `AttackingCastle`로 전환하고, 성채 앞에서 공격 타이머를 돌리게 했다.
- 성채에 붙은 몬스터는 살아 있는 적으로 남기 때문에 영웅 타겟팅 대상에 계속 포함된다.
- 성채 피해는 도착 순간 1회 피해가 아니라, `EnemyCastleAttackIntervalSec` 주기마다 반복 피해로 계산된다.
- 같은 라인에서 같은 시간에 스폰되는 몬스터는 `NightDefenseWavePlanBuilder`가 최소 스폰 간격을 보정한다.
- 표시 계층에서는 같은 라인의 몬스터가 완전히 겹쳐 보이지 않도록 진행도와 Y 위치를 살짝 벌린다.

### 왜 이렇게 바꿨는지

이 게임의 전투 목표는 몬스터가 성채를 부수고, 영웅이 그 웨이브를 막는 구조다.
따라서 `성채 도착 = 제거`로 처리하면 몬스터가 실제로 성채를 공격하는 전투가 성립하지 않는다.
도착 몬스터를 전장에 남기고 반복 공격 상태로 유지해야, 성채 HP와 영웅 방어가 게임의 핵심 압박으로 작동한다.

스폰도 같은 라인 같은 위치에 여러 마리가 동시에 찍히면 전투 판독성이 떨어진다.
테이블 단계에서 최소 간격을 보장하고, 표시 단계에서 약간의 위치 보정을 넣어 웨이브가 한 덩어리처럼 뭉쳐 보이는 문제를 줄였다.

### 검증

- `dotnet build "Nightfall Spire.sln" /clp:Summary /v:minimal` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 39: 몬스터 이동 속도와 라인 겹침 표시 폭 조정

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Service/CombatRuntimeController.cs`
- `Assets/_Project/01_Script/Scene/UnityNightDefenseSpawnSink.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 몬스터 이동 진행도 변환값을 `0.01`에서 `0.025`로 올렸다.
- 같은 라인에 있는 몬스터들의 Y축 표시 간격을 `0.06`에서 `0.12`로 넓혔다.

### 왜 이렇게 바꿨는지

기존 이동 속도는 전투 화면에서 몬스터가 성채로 압박해 오는 느낌이 약했다.
이동 변환값을 올려 웨이브가 더 빠르게 전진하게 만들고, Y축 표시 간격을 넓혀 같은 라인 몬스터가 뭉쳐 보이는 문제를 줄였다.

### 검증

- `dotnet build "Nightfall Spire.sln" /clp:Summary /v:minimal` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 40: 몬스터 이동속도 테이블 기준화

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Service/CombatRuntimeController.cs`
- `Assets/_Project/05_Data/Tables/EnemyData.tsv`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `EnemyData.tsv`의 모든 몬스터 `MoveSpeed` 기본값을 `0.10`으로 맞췄다.
- 전투 런타임의 이동 변환값을 `1`로 바꿔서 테이블의 `MoveSpeed`가 초당 진행도로 바로 쓰이게 했다.

### 왜 이렇게 바꿨는지

몬스터 이동속도는 코드 상수로 계속 조정하면 몬스터별 밸런싱이 어려워진다.
이제 기본 이동속도는 테이블에서 관리하고, 이후 일반 몬스터/비행 몬스터/엘리트/보스마다 `MoveSpeed`만 다르게 주면 된다.

### 검증

- `dotnet build "Nightfall Spire.sln" /clp:Summary /v:minimal` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 36: 전투 몬스터 프리팹 생성 연결

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Scene/UnityNightDefenseSpawnSink.cs`
- `Assets/_Project/00_Scenes/BattleScene.unity`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `UnityNightDefenseSpawnSink`가 `EnemyData.tsv`의 `PrefabKey`를 기준으로 실제 몬스터 프리팹을 생성하도록 연결했다.
- 전투 씬에 `EnemyLayer`를 추가해 런타임 몬스터 인스턴스가 한 곳에 모이도록 했다.
- `enemy_crawler`, `enemy_brute`는 `Monster_01` 프리팹으로, `enemy_wisp`, `enemy_night_titan`은 `Monster_02` 프리팹으로 연결했다.
- 라인별 시작 위치와 성채 도착 위치를 `lanePaths`로 인스펙터 연결해 몬스터 위치가 전투 진행률에 따라 움직이도록 했다.
- 생성된 몬스터는 `RuntimeId`로 추적하고, 전투 런타임의 갱신/제거 요청에 맞춰 위치 갱신과 삭제를 수행한다.
- 코드 주석을 깨진 인코딩 없이 한글 설명으로 다시 정리했다.

### 왜 이렇게 바꿨는지

몬스터 생성은 테이블이 원본이어야 하고, Unity 프리팹 참조는 씬 인스펙터에서 명시적으로 보여야 한다.
그래서 `EnemyData.PrefabKey`를 직접 프리팹 이름으로 강제하지 않고, `UnityNightDefenseSpawnSink`의 연결 목록을 통해 실제 프리팹을 생성하도록 만들었다.

이 방식이면 나중에 `enemy_crawler`를 다른 프리팹으로 바꾸거나, 같은 프리팹을 여러 데이터가 공유해도 테이블 구조를 흔들지 않아도 된다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 35: BattleScene 런타임 참조 누락 복구

### 변경된 파일

- `Assets/_Project/01_Script/Scene/BattleSceneRoot.cs`
- `Assets/_Project/01_Script/Core/GameFlowController.cs`
- `Assets/_Project/01_Script/Core/SaveManager.cs`
- `Assets/_Project/01_Script/Model/SaveData.cs`
- `Assets/_Project/01_Script/Service/GameContentDataSource.cs`
- `Assets/_Project/01_Script/Service/NightDefenseSessionService.cs`
- `Assets/_Project/99_Test/EditMode/Service/NightDefenseRuntimeControllerTests.cs`
- `Assets/_Project/99_Test/EditMode/Service/GameContentServiceTests.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `BattleSceneRoot`가 시작될 때 현재 BattleScene 안에서 `BattleStaticUIRoot`, `BattleDynamicUIRoot`, `NightDefenseBattleRuntime`, `UnityNightDefenseSpawnSink`를 자동으로 찾도록 했다.
- 씬 인스펙터에서 `NightDefenseBattleRuntime`이나 `UnityNightDefenseSpawnSink` 참조가 비어 있으면 `BattleSceneRoot`에 런타임 컴포넌트를 자동 생성하게 했다.
- `DontDestroyOnLoad` 쪽 오브젝트를 잘못 잡지 않도록 현재 씬에 있는 컴포넌트만 대상으로 찾는다.
- 기존 저장 데이터의 현재 방어 세션 ID가 현재 테이블에 없으면 첫 방어 세션으로 보정하게 했다.
- 오래된 저장 데이터에 전투 슬롯이나 시작 영웅 정보가 없으면 현재 버전 기준 기본 슬롯과 기본 영웅을 복구하게 했다.
- `GameFlowController`가 밤 방어 시작 실패 시 상태 전환 실패인지, 세션 데이터 실패인지, 플레이 상태 전환 실패인지 콘솔에 구체적으로 남기게 했다.
- `INightDefenseDataSource` 확장에 맞춰 EditMode 테스트의 가짜 데이터 소스도 같은 계약을 구현하게 했다.

### 왜 이렇게 바꿨는지

로비의 `Spire_Popup(Clone) > FightButton_Battle` 클릭 후 BattleScene은 정상 로드됐지만, `BattleSceneRoot`의 `battleRuntime`과 `spawnSink` 참조가 비어 있어 전투 런타임이 시작되지 않았다.
전투 진입은 핵심 플로우라 씬 참조 하나가 빠졌다고 바로 막히면 안 된다.
그래서 씬 세팅이 비어 있어도 코드에서 현재 씬 기준으로 복구하고, 표시 어댑터가 없으면 임시 런타임 어댑터를 자동 생성하도록 보강했다.

그 다음 단계에서 `밤 방어 세션 시작 실패`가 발생할 수 있었는데, 이 경우는 저장 데이터가 오래되어 `CurrentDefenseSessionId`가 현재 `DefenseSessionData.tsv`에 없는 값을 들고 있을 때 발생한다.
현재 테이블은 101번 세션부터 시작하므로, 오래된 저장값이 들어와도 첫 세션으로 복구해 테스트 전투가 막히지 않게 했다.

추가로 전투 런타임이 `Combat:NoActiveHeroSlots`로 실패하는 경우도 확인했다.
이 원인은 오래된 저장 파일에 해금된 슬롯이나 배치된 기본 영웅이 없어서 발생한다.
저장 데이터 로드 직후 현재 버전 기준으로 슬롯 0번을 해금하고 `Player_Sword`를 배치하며, 시작 영웅 4개의 기본 저장 상태를 보정하도록 만들었다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업: MVC에서 MVC 구조로 전환

### 변경 방향

- `Controller` 폴더와 Controller 계열 클래스를 제거했다.
- `Controller` 폴더를 만들고 화면 흐름 제어 클래스를 이쪽으로 모았다.
- UI 인터페이스와 ViewState는 `UI` 폴더로 옮겼다.
- View가 `PopupManager`나 `GameContext`를 직접 쥐지 않도록 줄였다.
- 팝업 View가 Controller를 직접 생성하던 구조를 제거하고 `PopupControllerFactory`로 분리했다.

### 핵심 Controller

- `BootLoadingController`: 부트 로딩 화면 표시 흐름 제어
- `CurrencyHudController`: 재화 모델 구독과 HUD 표시 제어
- `LobbyController`: 로비 버튼 입력을 명령으로 전달
- `LobbyNavigationController`: 로비 명령을 팝업/씬 전환으로 실행
- `HeroListController`: 영웅 목록 데이터 조회와 상세 팝업 요청
- `HeroDetailController`: 영웅 상세 PREV/NEXT/닫기 흐름 제어
- `BattleSceneController`: 전투 씬 진입, 밤 방어 세션 시작/정리 제어
- `PopupControllerFactory`: 팝업 View와 팝업 Controller 조립

### 검증 상태

- `Controller`, `controller`, `MVC` 문자열은 현재 코드 검색 기준으로 남아 있지 않다.
- `dotnet build "Nightfall Spire.sln"` 기준 오류 0개로 컴파일된다.
- 남은 경고는 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고이며 이번 MVC 전환으로 생긴 오류는 아니다.

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
  - `Add currency HUD MVC controller`
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

팝업은 이후 성장, 보상, 설정, 방어 결과, 오프라인 보상 등 여러 기능 UI에서 반복해서 쓰인다.
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

## 완료된 작업 8: Currency HUD MVC 샘플 추가

커밋: `Add currency HUD MVC controller`

### 변경된 파일

- `Assets/_Project/01_Script/UI/CurrencyHudView.cs`
- `Assets/_Project/01_Script/Controller/CurrencyHudController.cs`
- `Assets/_Project/01_Script/Controller.meta`
- `Assets/_Project/01_Script/Controller/CurrencyHudController.cs.meta`
- `Assets/_Project/01_Script/Scene/LobbyStaticUIRoot.cs`
- `Assets/_Project/00_Scenes/LobbyScene.unity`

### 주요 변경

- `CurrencyHudView`를 MVC View로 정리했다.
- `CurrencyHudController`를 추가해 `CurrencyProgress`와 `CurrencyHudView`를 연결하게 했다.
- `CurrencyHudController`가 `CurrencyProgress.Gold`, `CurrencyProgress.Gem`을 R3로 구독한다.
- Controller가 숫자 포맷을 담당하고 `CurrencyHudViewState`를 만들어 View에 전달한다.
- `CurrencyHudView`는 Model, R3, 숫자 포맷을 알지 않고 `Render()`로 전달받은 문자열만 표시한다.
- `LobbyStaticUIRoot`가 `GameContext`를 받은 뒤 `CurrencyHudController`를 생성한다.
- `LobbyStaticUIRoot.OnDestroy()`에서 Controller 구독을 해제한다.
- `LobbyScene`의 `TopCurrencyHud` 오브젝트에 `CurrencyHudView` 컴포넌트를 연결했다.

### 왜 이렇게 했는지

이 프로젝트는 전통 MVC를 그대로 쓰기보다 Unity에서 운용하기 쉬운 MVC 구조가 더 적합하다.
Unity View는 씬/프리팹과 Inspector 참조를 가져야 하므로, View가 Model을 직접 구독하기 시작하면 UI 코드가 점점 무거워진다.
따라서 UI는 MVC 기준으로 두고 Controller가 Model 구독, 표시 문자열 생성, View 갱신을 맡는다.

이번 변경으로 첫 UI 바인딩 기준을 아래처럼 잡았다.

```text
CurrencyProgress
= Model. 골드/젬 상태와 변경 규칙을 가진다.

CurrencyHudView
= View. Controller가 넘긴 표시 상태만 화면에 반영한다.

CurrencyHudController
= Controller. Model을 구독하고 화면 표시 상태를 만들어 View에 전달한다.
```

현재 `TopCurrencyHud`에는 아직 실제 골드/젬 텍스트 자식이 없다.
그래서 `CurrencyHudView`의 `goldText`, `gemText` 참조는 비워둔 상태다.
나중에 UI 배치 단계에서 텍스트 오브젝트를 만든 뒤 Inspector에 연결하면 된다.

## 완료된 작업 8-1: Currency HUD MVC 라이브 기준 보강

커밋: `Harden currency HUD MVC structure`

### 변경된 파일

- `Assets/_Project/01_Script/UI/CurrencyHudView.cs`
- `Assets/_Project/01_Script/Controller/ICurrencyHudView.cs`
- `Assets/_Project/01_Script/Controller/CurrencyHudViewState.cs`
- `Assets/_Project/01_Script/Controller/CurrencyHudController.cs`
- `Assets/_Project/01_Script/Scene/LobbyStaticUIRoot.cs`
- `Assets/_Project/99_Test/EditMode/Controller/CurrencyHudControllerTests.cs`

### 주요 변경

- `CurrencyHud` 이름을 `CurrencyHudView`로 바꿔 View 역할이 이름에 드러나게 했다.
- Unity 씬의 Missing Script를 막기 위해 기존 `CurrencyHud` `.meta` GUID를 `CurrencyHudView`가 유지하게 했다.
- `ICurrencyHudView`를 별도 파일로 분리해 Controller가 Unity 컴포넌트 구체 타입에 직접 묶이지 않게 했다.
- `CurrencyHudViewState`를 별도 파일로 분리해 Controller가 만든 표시 상태를 명확히 했다.
- `CurrencyHudController`는 생성자에서 바로 구독하지 않고 `Initialize()`에서 명시적으로 시작하게 했다.
- `CurrencyHudController`에 중복 초기화 방지와 Dispose 이후 갱신 차단을 추가했다.
- `LobbyStaticUIRoot`는 `FormerlySerializedAs("currencyHud")`를 사용해 기존 Inspector 직렬화 값을 `currencyHudView`로 이어받게 했다.
- `CurrencyHudControllerTests`를 추가해 초기 렌더링, Model 변경 반영, 중복 초기화 방지, Dispose 후 갱신 차단, null 의존성 거부를 검증하게 했다.

### 왜 이렇게 바꿨는지

출시용 UI 구조에서는 View, Controller, ViewState, View Interface가 한 파일에 섞이면 규모가 커질 때 책임이 흐려진다.
그래서 Currency HUD를 앞으로 다른 UI가 따라갈 기준 구조로 정리했다.

```text
CurrencyProgress
= Model. 재화 값과 변경 규칙을 가진다.

ICurrencyHudView
= Controller가 기대하는 View 계약이다.

CurrencyHudView
= Unity View. Text 참조와 Render만 담당한다.

CurrencyHudViewState
= View에 넘길 완성된 표시 상태다.

CurrencyHudController
= Controller. Model 구독, 숫자 포맷, ViewState 생성, View 갱신, 구독 해제를 담당한다.
```

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- `CurrencyHudControllerTests.cs` 컴파일 통과.
- Unity EditMode Test Runner는 현재 로컬에 Unity 프로세스가 여러 개 떠 있어 batchmode 실행이 종료 코드 127로 실패했다.
- Unity 에디터를 닫은 뒤 EditMode 테스트를 다시 실행해야 한다.

## 완료된 작업 14: 핵심 컨텐츠 서비스 뼈대 라이브 기준화

커밋: `Add core content domain services`

### 변경된 파일

- `Assets/_Project/01_Script/Service/GameContentDataSource.cs`
- `Assets/_Project/01_Script/Service/NightDefenseSessionService.cs`
- `Assets/_Project/01_Script/Service/DraftService.cs`
- `Assets/_Project/01_Script/Service/RewardService.cs`
- `Assets/_Project/01_Script/Service/DayGrowthService.cs`
- `Assets/_Project/01_Script/Core/GameContext.cs`
- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/Model/GameProgress.cs`
- `Assets/_Project/99_Test/EditMode/Service/GameContentServiceTests.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 핵심 컨텐츠 규칙을 담당할 `Service` 폴더를 추가했다.
- `GameContentDataSource`를 추가해 `DataTableManager`를 도메인 서비스용 조회 계약으로 감쌌다.
- `NightDefenseSessionService`를 추가해 방어 세션 입장 조건, 웨이브 진행, 보스 웨이브, 드래프트 웨이브 판단을 담당하게 했다.
- `DraftService`를 추가해 드래프트 풀의 포함/제외 태그와 PickCount 기준으로 카드 후보를 만들게 했다.
- `RewardService`를 추가해 보상 그룹을 보상 목록과 수령 대기 재화로 변환하게 했다.
- `DayGrowthService`를 추가해 성채 층 해금, 비용 지불, 전투 슬롯 업그레이드를 담당하게 했다.
- `GameProgress`에 `HighestClearedDefenseSessionId`를 ReactiveProperty로 노출해 낮 성장 조건이 저장 데이터와 동기화되게 했다.
- `GameContext`가 런타임에서 도메인 서비스를 보유하게 했다.
- `GameRoot`가 `DataTableManager` 로드 후 `GameContentDataSource`를 만들어 `GameContext`에 전달하게 했다.
- `GameContentServiceTests`를 추가해 밤 방어, 드래프트, 보상, 낮 성장 서비스의 핵심 계약을 검증하게 했다.

### 왜 이렇게 바꿨는지

기존 구조는 Progress 모델과 테이블 로딩은 있었지만, 실제 게임 규칙을 책임지는 계층이 없었다.
이 상태에서 UI나 전투 컨트롤러가 Progress를 직접 조작하기 시작하면 출시 단계에서 상태가 꼬일 가능성이 높다.

그래서 컨텐츠 규칙은 아래처럼 서비스로 고정한다.

```text
Table Row
→ Domain Service
→ Progress Model
→ Controller/UI 또는 전투 런타임
```

### 현재 각 서비스 책임

`NightDefenseSessionService`

- 현재 방어 세션 ID가 테이블에 있는지 확인한다.
- 필요한 성채 층 조건을 확인한다.
- 웨이브 그룹과 웨이브 Row를 확인한다.
- 검증 성공 시 `NightDefenseProgress`를 시작하고 웨이브를 진행한다.

`DraftService`

- 드래프트 풀의 Include/Exclude 태그를 기준으로 후보 카드를 필터링한다.
- `PickCount`만큼 후보를 안정적인 순서로 선택한다.
- 후보 생성 성공 시 `DraftProgress.OpenDraft()`를 호출한다.

`RewardService`

- 보상 그룹 Row를 읽어 지급 가능한 보상 목록을 만든다.
- 현재 `RewardProgress`가 표현할 수 있는 골드/젬 수령 대기 보상을 계산한다.

`DayGrowthService`

- 다음 성채 층 해금 조건과 비용을 확인한다.
- 골드/젬 비용을 차감한다.
- 전투 슬롯 업그레이드 조건과 비용을 확인한다.
- 성채 층 해금 기능 중 전투 슬롯/드래프트 풀 해금을 Progress에 반영한다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- `GameContentServiceTests.cs` 컴파일 통과.
- Unity EditMode Test Runner는 로컬 Unity 프로세스가 떠 있으면 batchmode가 실패할 수 있으므로, 에디터 종료 후 재실행해야 한다.
- `DraftService.cs.meta` GUID가 잘못된 길이로 생성되면 Unity가 해당 스크립트를 컴파일 대상에서 빠뜨릴 수 있다.
- `DraftService.cs.meta` GUID를 정상 32자리 값으로 수정했다.

## 완료된 작업 15: GameStateMachine 전이 규칙 구현

커밋: `Add game state transition rules`

### 변경된 파일

- `Assets/_Project/01_Script/Core/GameStateMachine.cs`
- `Assets/_Project/01_Script/Core/GameFlowController.cs`
- `Assets/_Project/01_Script/Model/GameProgress.cs`
- `Assets/_Project/99_Test/EditMode/Core/GameFlowControllerTests.cs`
- `Assets/_Project/99_Test/EditMode/Core/GameStateMachineTests.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 비어 있던 `GameStateMachine`에 실제 상태 전환 규칙을 추가했다.
- `GameProgress.ChangeState()`가 `GameStateMachine.CanChange()`를 통과해야 상태를 바꾸게 했다.
- `GameProgress.ChangeState()`는 상태 변경 성공 여부를 `bool`로 반환하게 했다.
- `GameProgress.RestorePreviousState()`도 복구 성공 여부를 `bool`로 반환하게 했다.
- `GameFlowController`가 상태 전환 실패 여부를 확인하고, 실패한 흐름에서는 Progress 변경을 계속하지 않게 했다.
- `AppBackground` 진입 시 이전 상태를 저장하고, 복귀 시 유효한 플레이 상태로만 돌아가게 했다.
- `GameStateMachineTests`를 추가해 부팅 흐름, 잘못된 전환 차단, 드래프트 왕복, 백그라운드 복구를 검증하게 했다.
- `GameFlowControllerTests`를 추가해 밤 방어 세션 시작, 잘못된 세션 시작 차단, 드래프트 왕복, 방어 결과 확정을 검증하게 했다.

### 왜 이렇게 바꿨는지

기존 `GameStateMachine`은 `current != next`만 확인하는 빈 껍데기였다.
이 상태에서는 UI 버튼, 전투 시스템, 팝업이 실수로 `DayPreparation -> DraftSelection` 같은 잘못된 상태 전환을 만들어도 막을 수 없다.

상태 전환은 게임 루프의 뼈대이므로, 앞으로 모든 상태 변경은 `GameProgress.ChangeState()`를 통해 `GameStateMachine` 규칙을 거치게 한다.
또한 컨트롤러는 `ChangeState()`의 반환값을 확인해서, 상태가 바뀌지 않았는데 세션 시작이나 보상 반영만 진행되는 불일치를 막는다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- `GameStateMachineTests.cs`, `GameFlowControllerTests.cs` 컴파일 통과.

## 완료된 작업 16: 밤 방어 웨이브 스폰 계획 구조 추가

커밋 예정: `Add night defense wave plan`

### 변경된 파일

- `Assets/_Project/01_Script/Core/GameFlowController.cs`
- `Assets/_Project/01_Script/Service/NightDefenseSessionService.cs`
- `Assets/_Project/01_Script/Service/NightDefenseWavePlan.cs`
- `Assets/_Project/99_Test/EditMode/Service/GameContentServiceTests.cs`
- `Assets/_Project/99_Test/EditMode/Service/NightDefenseWavePlanTests.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `NightDefenseWavePlan`을 추가해 한 웨이브의 실제 스폰 시간표를 보관하게 했다.
- `NightDefenseSpawnEvent`를 추가해 적 ID, 라인 ID, 스폰 시간, 스폰 순서를 한 이벤트로 표현하게 했다.
- `NightDefenseWavePlanBuilder`를 추가해 `WaveDataRow` 목록을 검증하고 시간순 스폰 계획으로 변환하게 했다.
- `NightDefenseSessionService.TryAdvanceNextWave()`가 웨이브 Row만 반환하지 않고 `WavePlan`도 함께 반환하게 했다.
- 유효하지 않은 웨이브 Row가 들어오면 `InvalidWaveData` 실패로 막게 했다.
- `GameFlowController.BeginLoadedNightDefenseSession()`이 바로 플레이 상태로 가지 않고 `NightDefenseReady`를 거쳐 세션 서비스를 통해 입장 조건을 검증하게 했다.
- `NightDefenseWavePlanTests`를 추가해 스폰 시간표 정렬과 잘못된 웨이브 Row 차단을 검증하게 했다.

### 왜 이렇게 바꿨는지

전투 MonoBehaviour가 `WaveDataRow`를 직접 읽어 스폰 시간을 계산하기 시작하면, 테이블 해석 규칙이 씬 코드에 흩어진다.
출시 기준에서는 웨이브 데이터 검증, 스폰 시간 계산, 정렬, 마지막 스폰 시간 계산을 순수 C# 계층에서 끝내야 한다.

그래서 흐름을 아래처럼 고정했다.

```text
WaveDataRow
→ NightDefenseWavePlanBuilder
→ NightDefenseWavePlan
→ 전투 씬 스폰 컨트롤러
```

전투 씬은 이제 어떤 테이블 컬럼을 어떻게 계산할지 몰라도 된다.
나중에 실제 적 프리팹 생성기는 `NightDefenseSpawnEvent`만 보고 시간에 맞춰 생성하면 된다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- `NightDefenseWavePlanTests.cs` 컴파일 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고 1개는 그대로 남아 있다.

## 완료된 작업 17: 밤 방어 런타임 스폰 실행 계층 추가

커밋 예정: `Add night defense runtime controller`

### 변경된 파일

- `Assets/_Project/01_Script/Service/INightDefenseSpawnSink.cs`
- `Assets/_Project/01_Script/Service/NightDefenseContracts.cs`
- `Assets/_Project/01_Script/Service/NightDefenseSpawnController.cs`
- `Assets/_Project/01_Script/Service/NightDefenseRuntimeController.cs`
- `Assets/_Project/99_Test/EditMode/Service/NightDefenseRuntimeControllerTests.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `NightDefenseSpawnRequest`를 추가해 전투 씬 스폰러에 넘길 생성 요청 값을 만들었다.
- `INightDefenseSpawnSink`를 추가해 실제 프리팹 생성 계층과 순수 런타임 계층을 분리했다.
- `NightDefenseSpawnController`를 추가해 `NightDefenseWavePlan`의 스폰 이벤트를 시간 흐름에 맞춰 실행하게 했다.
- `NightDefenseRuntimeController`를 추가해 다음 웨이브 시작, 세션 경과 시간 갱신, 스폰 Tick 실행을 묶었다.
- `NightDefenseRuntimeControllerTests`를 추가해 시간이 흐를 때 스폰 요청이 순서대로 발생하고 `NightDefenseProgress.ElapsedSeconds`가 갱신되는지 검증하게 했다.

### 왜 이렇게 바꿨는지

공개 설명 기준 Nightfall Spire 계열의 중심은 낮 성장과 밤 방어, 전투 중 로그라이트 드래프트입니다.
따라서 다음 단계가 영웅 공격이나 투사체보다 먼저 웨이브 스폰 런타임이어야 합니다.

이번 구조는 아래 흐름을 고정합니다.

```text
NightDefenseSessionService
→ NightDefenseWavePlan
→ NightDefenseRuntimeController
→ NightDefenseSpawnController
→ INightDefenseSpawnSink
→ 나중에 Unity Enemy Prefab 생성
```

이렇게 두면 전투 씬의 MonoBehaviour는 시간표 계산을 하지 않고, 순수 C# 런타임에서 발생한 스폰 요청만 처리합니다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- `NightDefenseRuntimeControllerTests.cs` 컴파일 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고 1개는 그대로 남아 있다.

## 완료된 작업 18: BattleScene 밤 방어 런타임 연결

커밋 예정: `Connect battle scene night defense runtime`

### 변경된 파일

- `Assets/_Project/01_Script/Scene/BattleSceneRoot.cs`
- `Assets/_Project/01_Script/Scene/NightDefenseBattleRuntime.cs`
- `Assets/_Project/01_Script/Scene/UnityNightDefenseSpawnSink.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `NightDefenseBattleRuntime`을 추가해 BattleScene에서 매 프레임 밤 방어 런타임 Tick을 실행하게 했다.
- `UnityNightDefenseSpawnSink`를 추가해 순수 C# 런타임에서 발생한 스폰 요청을 Unity 씬 계층이 받을 수 있게 했다.
- `BattleSceneRoot`가 밤 방어 세션 시작 성공 후 `NightDefenseBattleRuntime`과 `UnityNightDefenseSpawnSink`를 초기화하게 했다.
- 씬에 런타임 컴포넌트가 없으면 `BattleSceneRoot`가 같은 GameObject에 자동 추가해 최소 실행 연결을 보장하게 했다.

### 왜 이렇게 바꿨는지

이전 단계까지는 스폰 요청이 순수 C# 테스트 안에서만 발생했다.
이번 변경으로 BattleScene에 들어왔을 때 실제 Unity 프레임 시간 기준으로 `NightDefenseRuntimeController.Tick()`이 호출되는 연결이 생겼다.

아직 적 프리팹을 생성하지 않는 이유는 EnemyView, EnemyFactory, ObjectPool 자리가 확정되지 않았기 때문이다.
그래서 지금은 `UnityNightDefenseSpawnSink`가 요청을 로그로 받고, 다음 단계에서 실제 Enemy 생성 계층으로 교체할 수 있게 했다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- Unity 씬 파일은 직접 수정하지 않고 런타임 자동 연결 방식으로 처리했다.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고 1개는 그대로 남아 있다.

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
`LobbyScene`의 `TopCurrencyHud`에는 `CurrencyHudView`가 연결되어 있고, `LobbyStaticUIRoot`가 `CurrencyHudController`를 통해 `CurrencyProgress`와 연결한다.

## 다음 작업 계획

### 1순위: DataTable 방향 확정

- `Assets/_Project/05_Data` 아래에 테이블 원본과 생성물 폴더를 만든다.
- `DataTableManager`가 읽을 데이터 형식을 TSV/CSV/ScriptableObject 중에서 확정한다.
- 초기 테이블 스키마는 Hero, Enemy, Stage, Wave, Skill, Reward 중심으로 잡는다.

### 2순위: Currency HUD 실제 표시 오브젝트 구성

- `TopCurrencyHud` 아래에 골드/젬 텍스트 오브젝트를 추가한다.
- `CurrencyHudView.goldText`, `CurrencyHudView.gemText` Inspector 참조를 연결한다.
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

## 완료된 작업 13: MVC 데이터 테이블 로더 추가

커밋 예정: `Add MVC data table loader`

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

- ChatGPT에서 받아온 MVC TSV 테이블을 프로젝트 데이터 폴더에 반영했다.
- TSV를 런타임 Row 객체로 바꾸는 `TsvParser`, `TsvRow`, `DataTable<T>` 구조를 추가했다.
- 모든 MVC 테이블에 대응하는 Row 클래스를 추가했다.
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

## 완료된 작업 21: 콘텐츠별 계약 타입 정리

커밋 예정: `Group content contracts`

### 변경된 파일

- `Assets/_Project/01_Script/Service/DayGrowthContracts.cs`
- `Assets/_Project/01_Script/Service/DraftContracts.cs`
- `Assets/_Project/01_Script/Service/RewardContracts.cs`
- `Assets/_Project/01_Script/Service/NightDefenseContracts.cs`
- `Assets/_Project/01_Script/Service/DayGrowthService.cs`
- `Assets/_Project/01_Script/Service/DraftService.cs`
- `Assets/_Project/01_Script/Service/RewardService.cs`
- `Assets/_Project/01_Script/Service/NightDefenseSessionService.cs`
- `Assets/_Project/01_Script/Model/NightDefenseProgress.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `DayGrowthFailureReason`, `DayGrowthResult`를 `DayGrowthContracts.cs`로 분리했다.
- `DraftFailureReason`, `DraftOfferResult`를 `DraftContracts.cs`로 분리했다.
- `RewardFailureReason`, `RewardLine`, `RewardGrantResult`를 `RewardContracts.cs`로 분리했다.
- `DefenseOutcome`, `NightDefenseFailureReason`, 밤 방어 세션/런타임/스폰 요청/결과 값을 `NightDefenseContracts.cs`로 모았다.
- `NightDefenseRuntimeStartResult.cs`, `NightDefenseRuntimeTickResult.cs`, `NightDefenseSpawnTickResult.cs`, `NightDefenseSpawnEvent.cs`, `NightDefenseSpawnRequest.cs` 같은 작은 단일 값 타입 파일을 제거했다.
- 서비스 파일은 실제 규칙 실행 코드만 남기고, enum/결과 계약 타입은 콘텐츠별 Contracts 파일로 이동했다.

### 왜 이렇게 바꿨는지

작은 enum이나 결과 struct를 파일마다 쪼개면 구조가 좋아지는 것이 아니라 파일 수만 늘어난다.
반대로 서비스 구현 파일 안에 enum/result가 붙어 있으면 규칙 코드와 계약 코드가 섞인다.

그래서 기준을 콘텐츠별 Contracts 파일로 정했다.
테이블 enum은 `GameDataEnums.cs`, 팝업 계약은 `PopupContracts.cs`, 낮 성장/드래프트/보상/밤 방어 서비스 계약은 각각 자기 콘텐츠 Contracts 파일에 둔다.
실제 동작 책임이 큰 클래스만 독립 파일로 유지한다.

### 검증

- 전체 enum/readonly struct 위치를 확인했다.
- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 19: 로비 화면 단위 전투 시작 연결

커밋 예정: `Refactor lobby MVC to screen controller`

### 변경된 파일

- `Assets/_Project/01_Script/Controller/ILobbyScreenView.cs`
- `Assets/_Project/01_Script/Controller/LobbyController.cs`
- `Assets/_Project/01_Script/UI/LobbyScreen.cs`
- `Assets/_Project/01_Script/Scene/LobbyStaticUIRoot.cs`
- `Assets/_Project/99_Test/EditMode/Controller/LobbyControllerTests.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 잘못 쪼갠 `FightStartView`, `FightStartController`, `IFightStartView`를 제거했다.
- 로비 화면 전체 View인 `LobbyScreen`을 기준으로 밤 방어 시작 입력을 받게 했다.
- `LobbyController`를 추가해 로비 화면 액션이 `GameFlowController.LoadNightDefenseAsync()`로 이어지게 했다.
- `LobbyStaticUIRoot`가 부모 Canvas에 `LobbyScreen` 컴포넌트를 보장하고 `LobbyController`를 조립하게 바꿨다.
- 씬에 Button/Image 컴포넌트가 아직 없어도 `LobbyScreen`이 기존 `FightStartView` 이름 오브젝트를 찾아 최소 클릭 가능한 Button/Image를 보장하게 했다.
- `LobbyControllerTests`를 추가해 초기화, 클릭 시 로드 요청, 로드 거부 시 재활성화, Dispose 후 클릭 차단을 검증했다.

### 왜 이렇게 바꿨는지

로비에서 전투 씬으로 넘어가지 않았던 이유는 `FightStartView`라는 오브젝트는 있었지만 실제 Button, View 스크립트, Controller, `GameFlowController.LoadNightDefenseAsync()` 호출 연결이 없었기 때문이다.

처음에는 버튼 단위 View/Controller로 너무 잘게 쪼개는 잘못된 방향으로 갔다.
출시용 구조에서는 버튼 하나마다 스크립트와 Controller를 만드는 방식이 유지보수에 불리하다.

그래서 로비는 화면 단위 MVC로 정리했다.
`LobbyScreen`은 로비 화면의 주요 입력을 모으고, `LobbyController`가 그 입력을 게임 흐름으로 연결한다.
재화 HUD처럼 독립적으로 재사용되거나 상태 구독이 필요한 위젯만 별도 Controller를 유지한다.

### 검증

- `ProjectSettings/EditorBuildSettings.asset`에서 `BootScene`, `LobbyScene`, `BattleScene`이 모두 활성화되어 있는 것을 확인했다.
- `LobbyScene`에 `EventSystem`과 `GraphicRaycaster`가 있는 것을 확인했다.
- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 20: 팝업 요청/정책/결과 아키텍처 추가

커밋 예정: `Add popup request architecture`

### 변경된 파일

- `Assets/_Project/01_Script/UI/PopupContracts.cs`
- `Assets/_Project/01_Script/UI/PopupManager.cs`
- `Assets/_Project/01_Script/UI/BasePopup.cs`
- `Assets/_Project/99_Test/EditMode/UI/PopupRequestTests.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 팝업 열기 요청을 표현하는 `PopupRequest<TPopup>`를 추가했다.
- 중복 팝업 처리 기준인 `PopupOpenPolicy`를 추가했다.
- 팝업 중요도인 `PopupPriority`와 닫힘 이유인 `PopupCloseReason`을 추가했다.
- 호출자가 팝업 닫힘 결과를 받을 수 있도록 `PopupHandle`과 `PopupResult`를 추가했다.
- 작은 enum/DTO 파일을 여러 개로 늘리지 않고 `PopupContracts.cs`에 팝업 계약 타입을 모았다.
- `PopupManager.OpenAsync(prefab)` 기존 호출은 유지하면서, `PopupManager.OpenAsync(request)` 요청 기반 API를 추가했다.
- `SingleInstance`, `ReplaceTop`, `ReplaceAll`, `Stack` 정책을 `PopupManager`에 반영했다.
- `BasePopup.RequestCloseAsync()`가 닫힘 이유와 Payload를 전달할 수 있게 바꿨다.
- 씬 레이어가 해제될 때 열린 팝업 결과를 `SceneChanged`로 완료하게 했다.
- 딤 레이어는 열린 팝업 개수만 보지 않고 `UseDim`이 필요한 팝업이 있는지 기준으로 갱신하게 했다.
- 팝업 요청과 결과 타입의 기본 계약을 검증하는 EditMode 테스트를 추가했다.

### 왜 이렇게 바꿨는지

기존 구조는 팝업 프리팹을 열고 닫을 수는 있었지만, 어떤 팝업을 중복으로 막을지, 어떤 팝업을 교체할지, 닫힘 결과와 선택 Payload를 어떻게 받을지 기준이 없었다.
출시용 게임에서는 보상 결과, 드래프트 선택, 업그레이드 구매, 방어 결과처럼 서로 다른 기능 팝업이 동시에 들어올 수 있으므로 팝업 요청과 결과 계약이 먼저 필요하다.

이번 변경으로 팝업은 단순 UI 오브젝트가 아니라 `PopupRequest -> PopupManager -> BasePopup 상속 팝업 스크립트 -> PopupHandle -> PopupResult` 흐름을 갖는다.
공용 알림/확인 팝업 프리팹을 기본 전제로 두지 않고, 실제 팝업 UI마다 자기 스크립트 하나를 갖는 기준으로 간다.
Controller는 모든 팝업에 붙이지 않고, 드래프트 선택처럼 모델 상태와 선택 결과 흐름이 복잡한 경우에만 검토한다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 22: 팝업 UI 소유권 기준 정정

커밋 예정: `Clarify popup ownership model`

### 변경된 파일

- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- 팝업을 공용 알림/확인 프리팹 중심으로 만들지 않는다고 명시했다.
- `PopupManager`는 최상단 팝업 관리자이며 씬 레이어, 딤, 스택, 중복 정책, 결과 완료만 담당한다고 정리했다.
- `BasePopup`은 모든 팝업 UI 스크립트가 상속받는 생명주기 기반이라고 정리했다.
- 실제 기능은 각 팝업 UI 스크립트가 직접 소유한다고 정리했다.
- Controller는 모든 팝업에 붙이지 않고, 모델 상태와 선택 결과 흐름이 복잡한 팝업에서만 검토한다고 정리했다.

### 왜 이렇게 바꿨는지

내가 이전에 공용 알림/확인 팝업을 기본 전제로 둔 표현을 남겼는데, 이 프로젝트 기준과 맞지 않았다.
이 프로젝트의 팝업은 최상단 흐름만 `PopupManager`가 잡고, 팝업별 기능은 각 UI 스크립트가 직접 들고 가는 구조가 맞다.

그래서 문서 기준을 실제 개발 방향에 맞게 고쳤다.
앞으로 팝업을 추가할 때는 범용 팝업을 먼저 만들지 않고, 실제 콘텐츠 이름을 가진 팝업 스크립트부터 만든다.

### 검증

- 남아 있는 잘못된 팝업 소유권 기준 표현을 검색했다.
- 문서 변경만이라 런타임 동작 변경은 없다.

## 완료된 작업 23: 순수 C# 전투 런타임 뼈대 추가

커밋 예정: `Add combat runtime core`

### 변경된 파일

- `Assets/_Project/01_Script/Service/GameContentDataSource.cs`
- `Assets/_Project/01_Script/Service/CombatRuntimeContracts.cs`
- `Assets/_Project/01_Script/Service/CombatRuntimeController.cs`
- `Assets/_Project/01_Script/Service/CombatTargetingService.cs`
- `Assets/_Project/01_Script/Service/CombatDamageResolver.cs`
- `Assets/_Project/99_Test/EditMode/Service/CombatRuntimeControllerTests.cs`
- 각 새 스크립트의 `.meta`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `ICombatDataSource`를 추가해 전투 런타임이 영웅, 적, 슬롯, 슬롯 성장 테이블을 조회할 수 있게 했다.
- `GameContentDataSource`가 `ICombatDataSource`를 구현하게 했다.
- `CombatRuntimeController`를 추가해 해금된 전투 슬롯을 공격자 상태로 만들고, 스폰된 적을 런타임 상태로 등록하고, Tick마다 공격/피해/처치를 계산하게 했다.
- `CombatTargetingService`를 추가해 `Nearest`, `Farthest`, `LowestHp`, `HighestHp`, `Random` 타겟 규칙을 한곳에서 처리하게 했다.
- `CombatDamageResolver`를 추가해 기본 공격 피해 계산을 전투 컨트롤러에서 분리했다.
- `CombatRuntimeContracts.cs`에 전투 런타임 실패 이유, 영웅 슬롯 상태, 적 상태, 피해 결과, Tick 결과를 모았다.
- `CombatRuntimeControllerTests`를 추가해 슬롯 구성, 공격 피해, 적 처치 제거, 슬롯 성장 보정 적용을 검증했다.

### 왜 이렇게 만들었는지

기존 밤 방어 런타임은 웨이브 시간표를 실행하고 스폰 요청을 만드는 단계까지 있었다.
하지만 실제로 스폰된 적이 체력을 갖고, 전투 슬롯이 타겟을 잡고, 피해를 넣는 규칙 엔진은 아직 없었다.

이번 변경은 Unity 씬, 프리팹, 애니메이션을 건드리지 않고 전투 숫자 규칙만 순수 C#으로 추가한 것이다.
이 구조를 먼저 두면 이후 적 프리팹, 투사체, 스킬, 드래프트 효과를 붙이더라도 전투 규칙이 씬 오브젝트에 흩어지지 않는다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 전투 런타임 테스트 파일을 추가해 슬롯 구성, 기본 공격, 처치 제거, 슬롯 성장 보정 시나리오를 고정했다.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 24: Boot 로딩 MVC와 전투 보상/드래프트 효과 연결

커밋 예정: `Add boot loading and reward completion flow`

### 변경된 파일

- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/Core/GameContext.cs`
- `Assets/_Project/01_Script/Core/GameFlowController.cs`
- `Assets/_Project/01_Script/Controller/IBootLoadingView.cs`
- `Assets/_Project/01_Script/Controller/BootLoadingController.cs`
- `Assets/_Project/01_Script/UI/BootLoadingView.cs`
- `Assets/_Project/01_Script/Model/DraftProgress.cs`
- `Assets/_Project/01_Script/Service/GameContentDataSource.cs`
- `Assets/_Project/01_Script/Service/DraftContracts.cs`
- `Assets/_Project/01_Script/Service/DraftEffectResolver.cs`
- `Assets/_Project/01_Script/Service/CombatRuntimeModifierSet.cs`
- `Assets/_Project/01_Script/Service/CombatRuntimeContracts.cs`
- `Assets/_Project/01_Script/Service/CombatRuntimeController.cs`
- `Assets/_Project/01_Script/Service/NightDefenseContracts.cs`
- `Assets/_Project/01_Script/Service/NightDefenseCompletionService.cs`
- `Assets/_Project/99_Test/EditMode/Service/DraftEffectResolverTests.cs`
- `Assets/_Project/99_Test/EditMode/Service/GameContentServiceTests.cs`
- `Assembly-CSharp.csproj`
- 각 새 스크립트의 `.meta`

### 주요 변경

- `BootLoadingView`, `IBootLoadingView`, `BootLoadingController`를 추가해 BootScene 로딩 UI를 MVC 구조로 연결할 수 있게 했다.
- `GameRoot` 초기화 단계에서 데이터 로드, 저장 로드, 시스템 준비, 로비 씬 로드 진행률을 Boot 로딩 Controller에 보고하게 했다.
- BootScene에 로딩 View가 없어도 초기화가 조용히 진행되도록 null 허용 구조로 만들었다.
- `DraftEffectResolver`를 추가해 선택한 드래프트 카드 효과 Row를 전투 보정값으로 변환하게 했다.
- `CombatRuntimeModifierSet`을 추가해 드래프트, 시너지, 장비에서 생기는 전투 보정값을 누적 보관하게 했다.
- `CombatRuntimeController`가 슬롯 타입, 영웅 역할, 영웅 태그 기준 보정값을 읽어 공격력과 공격 속도에 반영하게 했다.
- `GameFlowController.SelectDraftCard()`가 카드 선택 전에 선택 가능 여부를 확인하고, 카드 효과 적용이 성공한 경우에만 드래프트를 닫도록 변경했다.
- `NightDefenseCompletionService`를 추가해 방어 세션의 `RewardGroupId`를 기준으로 승리 보상을 계산하게 했다.
- `GameFlowController.CompleteNightDefense(DefenseOutcome)` 경로를 추가해 실제 런타임에서는 세션 테이블 보상으로 결과를 확정할 수 있게 했다.
- 기존 테스트나 임시 호출을 위해 `CompleteNightDefense(DefenseOutcome, long, long)` 경로는 유지했다.

### 왜 이렇게 바꿨는지

BootScene은 앞으로 첫 화면 UI를 붙일 시작점이다.
씬 내부 UI 배치는 사용자가 직접 하므로, 이번 작업에서는 Unity UI 컴포넌트 참조만 받는 View와 표시 상태를 관리하는 Controller만 추가했다.
이렇게 해두면 실제 로딩 화면 디자인을 만들 때 Slider, Fill Image, TextMeshPro만 Inspector에 연결하면 된다.

드래프트 카드는 밤 방어전의 핵심이므로 선택 상태만 저장하고 끝내면 안 된다.
카드 효과를 전투 런타임 보정값으로 변환하는 계층을 추가해, 카드 선택이 실제 전투 슬롯 공격력과 공격 속도에 영향을 줄 수 있는 길을 열었다.
아직 모든 효과 타입을 구현하지 않고, 공격력/공격 속도/사거리/스킬 충전 계열처럼 현재 전투 런타임에 연결 가능한 효과부터 지원한다.

보상도 외부에서 숫자를 직접 넣는 방식만 두면 테이블 중심 구조가 무너진다.
따라서 밤 방어 결과 확정 시 현재 세션의 `RewardGroupId`를 찾아 `RewardService`로 계산하는 흐름을 추가했다.
최초 클리어 여부는 진행도 갱신 전에 판단해야 하므로 `NightDefenseCompletionService`에서 먼저 계산하고, 그 다음 `GameProgress.CompleteDefenseSession()`을 호출하는 순서로 유지했다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- `DraftEffectResolverTests.cs`로 드래프트 카드 효과가 전투 보정값과 공격력 계산에 반영되는지 검증했다.
- `GameContentServiceTests.cs`에 밤 방어 종료 보상이 세션의 RewardGroupId 기준으로 계산되는 테스트를 추가했다.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 25: BootScene 최소 UI 루트 구성

커밋 예정: `Set up boot scene loading root`

### 변경된 파일

- `Assets/_Project/00_Scenes/BootScene.unity`

### 주요 변경

- BootScene에 `Canvas_BootUI`를 추가했다.
- `Canvas_BootUI` 아래에 `BootLoadingRoot`를 추가했다.
- `BootLoadingRoot`에 `BootLoadingView` 스크립트를 연결했다.
- `Canvas_BootUI`에는 `Canvas`, `CanvasScaler`, `GraphicRaycaster`를 붙였다.
- 모바일 세로 화면 기준으로 `CanvasScaler` 기준 해상도를 `1080 x 1920`, Match 값을 `0.5`로 잡았다.
- `BootLoadingView`의 `progressSlider`, `progressFillImage`, `statusText`, `percentText`, `versionText` 참조는 비워뒀다.
- BootScene의 `GameRoot`에 붙어 있던 연습용 `Test` 스크립트 연결을 제거했다.

### 왜 이렇게 바꿨는지

BootScene은 게임 시작 시 가장 먼저 열리는 씬이다.
지금까지는 `GameRoot`만 있고 로딩 UI를 붙일 루트가 없어서, 사용자가 UI를 만들 때 어디에 어떤 스크립트를 붙여야 하는지 기준이 없었다.

이번 작업으로 아래 구조가 생겼다.

```text
BootScene
├─ Main Camera
├─ GameRoot
└─ Canvas_BootUI
   └─ BootLoadingRoot
      └─ BootLoadingView
```

사용자는 이제 `BootLoadingRoot` 아래에 Slider, Fill Image, TextMeshPro 텍스트를 배치한 뒤 `BootLoadingView`의 Inspector 참조에 연결하면 된다.
코드 쪽에서는 `GameRoot`가 BootScene 시작 시 자동으로 `BootLoadingView`를 찾아 초기화 진행률을 전달한다.

연습용 `Test` 스크립트는 코드 파일은 유지하되, 실제 BootScene 시작점인 `GameRoot`에는 붙어 있으면 안 된다.
그래서 씬 연결만 제거했다.

### 검증

- BootScene YAML에서 `Canvas_BootUI`, `BootLoadingRoot`, `BootLoadingView` 연결을 확인했다.
- BootScene에서 `Assembly-CSharp::Test` 연결이 제거된 것을 확인했다.
- `dotnet build "Nightfall Spire.sln"` 통과.
- Unity 배치 모드 검증은 실행이 즉시 종료되고 로그 파일이 남지 않아 별도 로그 검증은 하지 못했다.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 26: BootScene 로딩 화면 이미지와 로딩바 구성

커밋 예정: `Build boot loading screen UI`

### 변경된 파일

- `Assets/_Project/00_Scenes/BootScene.unity`
- `Assets/_Project/01_Script/UI/BootLoadingView.cs`
- `Assets/_Project/03_Art/BootScene/LightLoadingBg.png`
- `Assets/_Project/03_Art/BootScene/NightLoadingBg.png`
- `Assets/_Project/03_Art/BootScene/LoadingBar.prefab`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- BootScene의 `BootLoadingRoot` 아래에 `LightBackground`, `NightBackground`, `LoadingBarContainer`를 추가했다.
- `LightLoadingBg`는 기본으로 보이고, `NightLoadingBg`는 DOTween으로 천천히 나타났다 사라지도록 구성했다.
- 기본 배경 전환값은 낮/밤 각각 5초 유지, 1.2초 페이드다.
- `LoadingBar.prefab`을 `LoadingBarContainer` 아래에 런타임으로 생성하도록 했다.
- `BootLoadingView`가 로딩바 프리팹 안의 Slider를 자동으로 찾아 진행률을 연결하게 했다.
- `BootLoadingView`에 낮/밤 배경, 로딩바 생성, 참조 자동 탐색, DOTween 정리 함수 주석을 추가했다.
- BootScene 로딩 UI 기준 구조를 아키텍처 문서에 반영했다.

### 왜 이렇게 바꿨는지

BootScene은 사용자가 실제 UI를 붙일 첫 씬이다.
이번에는 단순히 빈 Root만 두는 것이 아니라, 사용자가 넣어둔 낮 배경, 밤 배경, 로딩바 프리팹을 실제 시작 화면에 바로 연결했다.

낮/밤 이미지는 둘 중 하나만 고정으로 쓰면 부트 화면이 정적이라 밋밋하다.
그래서 두 이미지를 같은 위치에 겹치고 밤 배경의 알파만 DOTween으로 조절해, 조용히 시간이 흐르는 느낌의 로딩 화면으로 만들었다.

로딩바는 프리팹 내부 구조가 외부 패키지 프리팹 인스턴스를 기반으로 되어 있어서, 씬 YAML에서 내부 Slider를 억지로 직접 참조하지 않았다.
대신 `BootLoadingView`가 프리팹을 생성한 뒤 내부 Slider를 자동으로 찾게 했다.
이렇게 하면 로딩바 프리팹 내부 구조가 조금 바뀌어도 View 연결이 바로 깨질 가능성이 줄어든다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- BootScene YAML에서 `LightBackground`, `NightBackground`, `LoadingBarContainer`, `LoadingBar.prefab` 연결을 확인했다.
- BootScene에 연습용 `Test` 스크립트 연결이 남아 있지 않은 것을 확인했다.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 27: BootScene 로딩바 프리팹 생성 오류 수정

커밋 예정: `Fix boot loading bar instantiation`

### 변경된 파일

- `Assets/_Project/01_Script/UI/BootLoadingView.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `LoadingBar.prefab` 생성 방식을 제네릭 `Instantiate<GameObject>()`에서 `UnityEngine.Object` 기반 생성으로 변경했다.
- 생성 결과가 `GameObject`로 오면 그대로 쓰고, `Component`로 오면 `component.gameObject`로 변환하도록 했다.
- 생성 결과를 해석할 수 없는 경우에는 명확한 에러 로그를 남기고 중단하도록 했다.

### 왜 이렇게 바꿨는지

사용자가 Play 실행 시 BootScene에서 `InvalidCastException`이 발생했다.
Unity Editor 로그 기준 원인은 `BootLoadingView.EnsureLoadingBarInstance()`에서 `LoadingBar.prefab`을 제네릭 방식으로 생성하는 부분이었다.

해당 로딩바 프리팹은 외부 Layer Lab 프리팹 인스턴스를 기반으로 되어 있어서, Unity가 복제 결과를 돌려주는 과정에서 제네릭 반환 타입 캐스팅이 깨질 수 있다.
따라서 Unity 오브젝트로 먼저 생성하고 실제 반환 타입을 안전하게 해석하도록 바꿨다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- Unity Editor 로그에서 기존 예외 위치가 `BootLoadingView.cs:133`의 프리팹 생성 코드였음을 확인했다.
- 기존 외부 패키지 경고 `System.Threading.Tasks.Extensions` 버전 충돌은 남아 있지만, 이번 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 28: BootScene 로딩바 런타임 프리팹 생성 제거와 고정 UI 배치

커밋 완료: `c3c9169 Fix boot loading scene UI setup`

### 변경된 파일

- `Assets/_Project/00_Scenes/BootScene.unity`
- `Assets/_Project/01_Script/UI/BootLoadingView.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `BootLoadingView`에서 `LoadingBar.prefab` 참조와 런타임 Instantiate 흐름을 제거했다.
- BootScene의 `BootLoadingView` serialized 데이터에서 `loadingBarPrefab` 참조를 제거했다.
- BootScene의 `LoadingBarContainer` 아래에 고정 UI `LoadingBarFrame`, `LoadingBarFill`, `StatusText`, `PercentText`, `VersionText`를 직접 배치했다.
- `BootLoadingView`가 `LoadingBarFill`, `StatusText`, `PercentText`, `VersionText`를 직접 참조하게 했다.
- `BootLoadingView`에 남아 있던 기본 로딩바 코드 생성 로직을 제거했다.
- 진행률은 고정 UI인 `LoadingBarFill`의 `Image.Type.Filled`와 `fillAmount`로 표시한다.

### 왜 이렇게 바꿨는지

첫 번째 수정은 `GameObject` 제네릭 생성만 피하면 될 것이라고 판단했지만, 실제 Play 결과 `LoadingBar.prefab` 생성 결과를 GameObject로 해석하지 못했다.
즉 문제는 단순 캐스팅 방식이 아니라, 현재 로딩바 프리팹이 외부 프리팹 인스턴스 기반으로 저장된 구조 자체와 런타임 생성 방식의 충돌이었다.

BootScene 로딩바는 게임 시작 안정성이 가장 중요하다.
그리고 로딩바는 팝업이 아니라 고정 UI이므로 런타임 생성 대상이 아니다.
그래서 외부 프리팹을 런타임 생성하는 흐름을 없애고, BootScene 안에 고정 UI로 직접 배치했다.
이렇게 하면 로딩바 프리팹 구조와 무관하게 BootScene이 시작되고, UI 참조도 씬에서 명확하게 확인할 수 있다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 사용자가 Unity에서 BootScene 로딩바 생성과 씬 전환을 확인했다.

## 완료된 작업 29: BootScene 진행률 연출과 고정 UI 기준 정리

커밋 완료: `c3c9169 Fix boot loading scene UI setup`

### 변경된 파일

- `Assets/_Project/00_Scenes/BootScene.unity`
- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Assets/_Project/01_Script/UI/BootLoadingView.cs`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `BootLoadingView`의 진행률 표시를 즉시 변경이 아니라 DOTween으로 부드럽게 따라가게 했다.
- 로딩 진행률이 50% 이상이 되면 `NightLoadingBg`가 1.2초 동안 페이드 인되도록 바꿨다.
- 기존 낮/밤 이미지 5초 순환 방식은 제거하고, 로딩 진행률 기반 전환으로 바꿨다.
- `GameRoot`에 최소 BootScene 표시 시간 2.6초를 추가했다.
- 실제 데이터 로드가 1초 안에 끝나도 로딩바 상승과 배경 전환이 보인 뒤 LobbyScene으로 넘어가게 했다.
- 고정 UI는 씬 배치, 팝업 UI만 인스턴스 생성한다는 UI 배치 기준을 아키텍처 문서에 추가했다.
- BootScene 로딩 텍스트가 실제로 보이도록 `StatusText`, `PercentText`, `VersionText`를 고정 UI로 추가했다.

### 왜 이렇게 바꿨는지

현재 TSV 데이터는 작아서 실제 로딩이 매우 빠르게 끝난다.
그래서 로딩 화면을 만들었어도 사용자 눈에는 바로 씬이 넘어가는 것처럼 보였다.

BootScene은 첫 인상을 주는 화면이므로, 실제 작업 시간이 짧아도 최소한의 진행 연출이 필요하다.
이번 변경으로 데이터가 빠르게 로드되어도 로딩바가 조금씩 올라가고, 절반 정도 찼을 때 낮 이미지에서 밤 이미지로 전환되는 장면을 볼 수 있게 했다.

또한 고정 UI와 팝업 UI의 생성 기준을 명확히 했다.
로딩 화면처럼 씬에 항상 존재하는 UI는 씬에 배치하고, 설정창이나 보상창처럼 필요할 때 열리는 UI만 `PopupManager`가 프리팹으로 생성한다.
이 기준을 지켜야 BootScene 로딩바처럼 고정 UI를 불필요하게 런타임 Instantiate하다가 생기는 오류를 피할 수 있다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 사용자가 Unity에서 BootScene 로딩 UI가 생성되는 것을 확인했다.

## 진행 중 작업 30: LobbyScene을 Lobby_Default 프리팹 기준으로 전환

커밋 예정: Unity에서 Lobby_Default 배치 확인 후 결정

### 변경된 파일

- `Assets/_Project/01_Script/Editor/LobbyDefaultPrefabSetupUtility.cs`
- `Assets/_Project/01_Script/Editor/LobbyDefaultPrefabSetupUtility.cs.meta`
- `Assets/_Project/00_Scenes/LobbyScene.unity`
- `Assets/_Project/01_Script/Scene/LobbyStaticUIRoot.cs`
- `Assets/_Project/01_Script/UI/LobbyScreen.cs`
- `Assets/_Project/03_Art/LobbyScene/Lobby_Default.prefab`
- `Assets/_Project/03_Art/LobbyScene/Lobby_Default.prefab.meta`
- `Docs/ARCHITECTURE_BASELINE.md`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- placeholder 고정 UI를 자동 생성하던 `LobbySceneSetupUtility`를 제거했다.
- `LobbyDefaultPrefabSetupUtility`를 추가해 LobbyScene의 고정 UI를 `Lobby_Default` 프리팹 기준으로 세팅하게 했다.
- `Lobby_Default`는 `Canvas_StaticUI > LobbyStaticUIRoot` 아래에 씬 고정 UI로 배치한다.
- 기존 placeholder 오브젝트는 Unity 세팅 유틸리티가 제거하고, `Lobby_Default` 하나를 로비 기본 UI로 사용한다.
- `LobbyScreen`은 `Lobby_Default` 안의 `Button_03_Red` 또는 `START/FIGHT` 라벨 버튼을 밤 방어 시작 버튼으로 찾는다.
- `Button_03_Red`에 Button 컴포넌트가 빠져 있어도 기존 Graphic을 유지한 채 클릭 가능한 Button을 보강한다.
- 이전 placeholder HUD인 `TopCurrencyHud`를 씬에서 제거하고, `LobbyStaticUIRoot`는 `LobbyScreen`을 직접 참조한다.
- `Lobby_Default`의 `ResourceBar_Group`을 실제 재화 View로 연결하기 전까지 `CurrencyHudController` 생성은 조용히 건너뛴다.
- 데모 문구 일부를 프로젝트 문구로 바꾼다.
  - `START` → `FIGHT`
  - `Battle 5` → `Week 1 Night 1`
  - `Hero's Arena` → `Nightfall Spire`
  - `Inventory` → `Heroes`
  - `Mission` → `Quest`
  - `AD Skip` → `Reward`

### 왜 이렇게 바꿨는지

처음 만든 placeholder 로비 뼈대는 구조를 설명하기에는 좋지만, 실제 제공된 `Lobby_Default` 프리팹보다 완성도가 낮다.
사용자가 제공한 프리팹에는 로비 레이아웃, 재화바, 좌우 버튼, 하단 탭, 시작 버튼이 이미 들어 있으므로 이것을 기준으로 쓰는 편이 낫다.

그래서 임시 placeholder 자동 생성 방식은 폐기하고, `Lobby_Default`를 우리 프로젝트 로비 고정 UI 원본으로 채택한다.
기능 스크립트는 계속 최소화해서, 버튼 하나마다 별도 View/Controller를 만들지 않고 실제 기능이 확정되는 묶음 단위로만 추가한다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- `LobbyScene.unity`에서 `TopCurrencyHud`, `GoalPanel`, `FightPanel` 같은 이전 placeholder 오브젝트가 남아 있지 않은 것을 확인했다.
- `LobbyScene.unity`에서 `Canvas_StaticUI > LobbyStaticUIRoot > Lobby_Default` 구조와 `LobbyStaticUIRoot.lobbyScreen` 참조를 확인했다.
- Unity에서 `Nightfall Spire > Setup > Lobby Default Prefab UI` 메뉴 또는 LobbyScene 재오픈으로 실제 하이어라키 확인 필요.

## 완료된 작업 31: BattleScene 전투 런타임 중심 구조 구성

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Controller/BattleSceneController.cs`
- `Assets/_Project/01_Script/Core/GameContext.cs`
- `Assets/_Project/01_Script/Model/BattleProgress.cs`
- `Assets/_Project/01_Script/Model/SaveData.cs`
- `Assets/_Project/01_Script/Scene/NightDefenseBattleRuntime.cs`
- `Assets/_Project/01_Script/Scene/UnityNightDefenseSpawnSink.cs`
- `Assets/_Project/01_Script/Service/BattleRuntimeContracts.cs`
- `Assets/_Project/01_Script/Service/BattleRuntimeContracts.cs.meta`
- `Assets/_Project/01_Script/Service/BattleSessionRuntimeController.cs`
- `Assets/_Project/01_Script/Service/BattleSessionRuntimeController.cs.meta`
- `Assets/_Project/01_Script/Service/CombatRuntimeContracts.cs`
- `Assets/_Project/01_Script/Service/CombatRuntimeController.cs`
- `Assets/_Project/99_Test/EditMode/Core/GameFlowControllerTests.cs`
- `Assets/_Project/99_Test/EditMode/Service/CombatRuntimeControllerTests.cs`

### 주요 변경

- `BattleSessionRuntimeController`를 추가해 밤 방어전의 중심 런타임을 만들었다.
- 기존 `NightDefenseRuntimeController`는 웨이브 시간표와 스폰 타이밍만 담당하도록 두고, 새 전투 세션 런타임이 웨이브 스폰을 `CombatRuntimeController`로 연결한다.
- `BattleProgress`를 호환용 bool 모델에서 성채 체력, 전투 상태, 승패 상태를 가진 런타임 모델로 승격했다.
- `CombatRuntimeController`가 적 이동 후 성채에 도달한 적을 제거하고 성채 피해로 변환하도록 바꿨다.
- 적 제거 사유를 `CombatEnemyDespawnResult`로 남기도록 해서 표시 계층이 처치와 성채 도달을 구분할 수 있게 했다.
- `IBattleCombatViewSink`를 추가해 순수 전투 계산과 Unity 표시 계층을 분리했다.
- `UnityNightDefenseSpawnSink`는 더 이상 웨이브 스폰 요청만 받지 않고, 적 생성/피해/제거/동기화 이벤트를 받는 전투 표시 Adapter가 되었다.
- `NightDefenseBattleRuntime`은 직접 웨이브를 돌리지 않고 `BattleSessionRuntimeController`를 시작하고 Tick만 전달한다.
- 기본 저장 데이터의 첫 방어 세션 ID를 실제 테이블 기준인 `101`로 수정했다.
- 전투 런타임 테스트에 성채 도달 피해 케이스를 추가했다.

### 왜 이렇게 바꿨는지

이전 구조는 전투씬에서 웨이브 스폰 로그만 찍는 수준이었다.
웨이브에서 적이 나오더라도 전투 계산 런타임으로 들어가지 않았고, 영웅 공격, 적 처치, 성채 피해, 승패 확정이 하나의 흐름으로 닫혀 있지 않았다.

라이브 수준 전투 구조에서는 씬 MonoBehaviour가 규칙을 직접 판단하면 안 된다.
씬은 Unity 생명주기와 표시 계층 연결만 맡고, 실제 규칙은 순수 C# 런타임이 맡아야 테스트와 유지보수가 가능하다.

그래서 `BattleSessionRuntimeController`를 전투 세션의 중심으로 두었다.
이 컨트롤러는 웨이브 시간표, 전투 계산, 드래프트 진입, 승리/패배 확정을 조율한다.
Unity 쪽은 `IBattleCombatViewSink` 뒤에 붙기 때문에, 이후 몬스터 프리팹 풀링이나 HP바, 피격 이펙트를 붙여도 전투 계산 코드는 그대로 유지할 수 있다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 전투 구조 변경으로 인한 컴파일 오류는 없다.

## 완료된 작업 32: BootScene 플레이 모드 자동 세팅 오류와 로비 Battle 버튼 라우팅 보강

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Editor/BootSceneSetupUtility.cs`
- `Assets/_Project/01_Script/Controller/LobbyNavigationController.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `BootSceneSetupUtility`가 플레이 모드 중 `EditorSceneManager.MarkSceneDirty`와 `SaveScene`을 호출하지 않도록 차단했다.
- 메뉴에서 Boot Loading UI 세팅을 직접 눌러도 플레이 모드 중에는 실행하지 않고 경고만 남기게 했다.
- 로비 명령 라우트가 씬 인스펙터에서 누락되어도 `BottomButton_Battle`은 기본적으로 `LoadNightDefense`로 처리되게 보강했다.

### 왜 이렇게 바꿨는지

`BootSceneSetupUtility`는 에디터에서 씬 UI를 구성하는 도구인데, `[InitializeOnLoad]`와 `delayCall` 때문에 플레이 모드 진입 중에도 실행될 수 있었다.
플레이 모드에서는 씬을 dirty 처리하거나 저장할 수 없기 때문에 `InvalidOperationException: This cannot be used during play mode.`가 발생했다.

로비 전투 진입은 핵심 플로우라 인스펙터 라우트 배열 누락만으로 막히면 안 된다.
그래서 `BottomButton_Battle`은 코드 기본 라우트로도 보장해, 씬 세팅이 비어 있어도 BattleScene 로딩으로 이어지게 했다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 35: 일반 몬스터 웨이브 물량 20~30마리 기준으로 조정

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/05_Data/Tables/WaveData.tsv`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `IsBossWave = FALSE`인 일반 몬스터 스폰 행의 `Count`를 20~30 범위로 올렸다.
- 초반 웨이브는 20~24마리, 뒤쪽 웨이브 그룹은 26~30마리까지 증가하도록 배치했다.
- 보스 웨이브의 보스 1마리 + 엘리트 2마리 구성은 그대로 유지했다.
- 스폰 간격도 기존 소규모 테스트 값에서 대량 스폰에 맞게 조금 조정했다.

### 왜 이렇게 바꿨는지

현재 전투는 2분 안에 일반 스폰 이벤트와 보스 스폰 이벤트가 진행되는 구조다.
기존 일반 몬스터 수량은 4~10마리라서 실제 모바일 디펜스 전투 밀도와 맞지 않았다.

그래서 일반 웨이브 하나가 화면 압박을 만들 수 있도록 20~30마리 기준으로 올렸다.
이후 몬스터 이동속도, 공격속도, 경험치 지급량, 카드 드래프트 타이밍과 함께 세부 밸런싱하면 된다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 36: BattleSceneRoot 런타임 참조를 인스펙터 연결 방식으로 정리

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Scene/BattleSceneRoot.cs`
- `Assets/_Project/00_Scenes/BattleScene.unity`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `BattleSceneRoot`에서 `FindObjectsByType`로 씬 컴포넌트를 찾는 보강 코드를 제거했다.
- `BattleSceneRoot`에서 `AddComponent`로 `NightDefenseBattleRuntime`, `UnityNightDefenseSpawnSink`를 자동 생성하던 코드를 제거했다.
- `BattleScene.unity`의 `BattleSceneRoot` 오브젝트에 `NightDefenseBattleRuntime`, `UnityNightDefenseSpawnSink` 컴포넌트를 직접 붙이고, `BattleSceneRoot`의 직렬화 필드에 연결했다.

### 왜 이렇게 바꿨는지

전투씬의 루트 오브젝트는 씬 구성의 조립 지점이어야 한다.
런타임에서 컴포넌트를 찾아오거나 없으면 생성하는 방식은 당장은 돌아가도, 씬 프리팹 구조가 커질수록 어떤 오브젝트가 실제 의존성인지 추적하기 어려워진다.

그래서 `BattleSceneRoot`는 인스펙터에 명시적으로 연결된 참조만 사용하도록 정리했다.
참조가 빠진 상태라면 조용히 임시 복구하지 않고, 씬 세팅 문제로 바로 드러나는 쪽이 라이브 구조에 맞다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 34: Spire_Popup 내부 FIGHT 버튼 전투 진입 연결

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/UI/SpirePopup.cs`
- `Assets/_Project/01_Script/Controller/PopupControllerFactory.cs`
- `Assets/_Project/01_Script/UI/PopupContracts.cs`
- `Assets/_Project/01_Script/UI/PopupManager.cs`
- `Assets/_Project/01_Script/Core/GameRoot.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `SpirePopup`이 자기 내부의 `FightButton_Battle` 버튼을 찾아 전투 시작 요청 이벤트를 올리도록 바꿨다.
- `SpirePopupController`를 팝업 Controller 조립 지점에 연결했다.
- `SpirePopupController`가 전투 시작 요청을 받으면 현재 로비 팝업을 정리한 뒤 `GameFlowController.LoadNightDefenseAsync()`를 호출한다.
- `PopupControllerContext`에 `GameFlowController`를 추가해 팝업 내부에서도 씬 전환을 같은 게임 흐름 Controller로 처리하게 했다.
- `GameRoot`가 `PopupManager`에 `GameFlowController`를 주입하도록 연결했다.

### 왜 이렇게 바꿨는지

`FightButton_Battle`은 `Canvas_StaticUI`가 아니라 `Canvas_DynamicUI > PopupLayer > Spire_Popup(Clone)` 내부에 생성되는 버튼이다.
그래서 Static UI의 `LobbyScreen` 버튼 캐시와 `LobbyNavigationController` 기본 라우트만으로는 이 버튼을 처리할 수 없었다.

팝업 내부 버튼은 해당 팝업 View와 Controller가 관리하는 편이 구조상 맞다.
그래서 `SpirePopup`은 버튼 클릭 사실만 알리고, 실제 전투 전환과 팝업 정리는 `SpirePopupController`가 맡도록 분리했다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.

## 완료된 작업 33: 중앙 FIGHT 버튼 전투 진입 명령 연결 보강

커밋 예정: `codex/architecture-cleanup`

### 변경된 파일

- `Assets/_Project/01_Script/Controller/LobbyNavigationController.cs`
- `Assets/_Project/01_Script/Scene/LobbyStaticUIRoot.cs`
- `Docs/CODEX_SUMMARY.md`

### 주요 변경

- `FightButton_Battle`도 전투 진입 기본 명령으로 등록했다.
- 씬 라우트 배열이 누락되거나 깨져도 `FightButton_Battle`은 `LoadNightDefense`로 처리된다.
- 전투 해금 상태를 하단 `BottomButton_Battle`과 중앙 `FightButton_Battle`에 같이 적용하도록 바꿨다.

### 왜 이렇게 바꿨는지

현재 `LobbyScene.unity`의 명령 라우트에는 중앙 FIGHT 버튼이 `FightButton_Battle`로 저장되어 있다.
하지만 코드 기본 라우트와 해금 상태 보강은 `BottomButton_Battle`만 기준으로 잡고 있었다.

전투 시작은 하단 Battle 탭과 중앙 FIGHT 버튼이 같은 게임 플로우를 타야 한다.
그래서 두 버튼 이름을 같은 전투 진입 명령으로 취급하도록 맞췄다.

### 검증

- `dotnet build "Nightfall Spire.sln"` 통과.
- 기존 `System.Threading.Tasks.Extensions` 버전 충돌 경고는 남아 있으나, 이번 수정으로 인한 컴파일 오류는 없다.
