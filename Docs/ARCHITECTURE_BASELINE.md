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
```

### GameProgress

게임 전체 상태와 낮/밤 루프 진행을 관리합니다.
현재는 다음 밤 방어 세션 ID와 완료한 낮/밤 루프 수를 보유합니다.

주요 값:

- `CurrentDefenseSessionId`
- `CompletedDayCount`
- `CurrentState`
- `PreviousState`

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

### 2순위: 밤 방어 런타임

- 웨이브 진행기
- 적 스폰 컨트롤러
- 방어 세션 타이머
- 보스 웨이브 판단
- 방어 성공/실패 처리

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
