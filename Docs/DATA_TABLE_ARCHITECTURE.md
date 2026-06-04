# Nightfall Spire MVP DataTable 설계 요약

## 핵심 방향

일반 자동전투 RPG의 `StageData -> Battle -> Reward` 구조가 아닙니다.
중심 데이터는 `DefenseSessionData`입니다.

```text
낮 준비
→ Spire/Citadel/CombatSlot/Module/Mining/Crafting 성장
→ 밤 방어전 시작
→ Wave 방어
→ 몇 웨이브마다 1-of-3 Draft 선택
→ 카드 누적과 Synergy로 전투 화력 변화
→ 보상 획득
→ 다시 낮 성장
```

## StageData는 없음

초기 MVP에는 `StageData.tsv`를 만들지 않습니다.
전투 기준은 `DefenseSessionData`입니다.
나중에 월드맵/챕터 UI가 필요하면 `ChapterData` 또는 `WorldNodeData`를 별도로 추가합니다.

## TSV 파싱 규칙

- 첫 줄은 Header
- 탭으로 컬럼 분리
- 빈 줄 무시
- `#` 시작 줄 무시
- enum은 문자열로 파싱
- list는 `|` 로 분리
- pairlist는 `|` 로 분리 후 `:` 로 key/value 분리

## Unity Object 참조 규칙

TSV에는 Unity Object를 직접 참조하지 않습니다.
`PrefabKey`, `IconKey`, `EffectKey`, `VisualKey`, `PositionKey` 같은 string Key만 둡니다.
실제 Prefab/Icon/Effect는 별도 Registry ScriptableObject에서 매핑하는 것을 추천합니다.

## DataTableManager 복합 조회

- CombatSlotUpgradeData: UpgradeGroupId + Level
- WaveData: WaveGroupId / WaveGroupId + WaveIndex
- RewardData: RewardGroupId
- DraftCardEffectData: CardId
- SynergyData: RequiredCardTagList 조건
