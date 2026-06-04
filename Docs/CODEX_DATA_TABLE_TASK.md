# Codex 구현 지시문: Nightfall Spire MVP DataTable 적용

이 압축 파일의 TSV 테이블과 enum 초안을 프로젝트에 적용해주세요.

## 적용 목표

일반 StageData 중심 구조가 아니라 DefenseSessionData 중심의 낮/밤 방어전 구조를 반영합니다.

## 해야 할 일

1. `Assets/_Project/05_Data/Tables` 폴더를 추가하고 TSV 파일들을 넣습니다.
2. `Assets/_Project/05_Data/Generated` 폴더를 추가합니다.
3. `Assets/_Project/01_Script/Data/Enums/GameDataEnums.cs`를 추가합니다.
4. `DataTableManager`는 순수 C# 구조로 유지합니다.
5. 아래 구조를 구현합니다.

```text
ITableRow
DataTable<T>
TsvParser
DataTableManager.LoadAllAsync()
```

## 우선 Row 클래스

- CurrencyDataRow
- HeroDataRow
- CombatSlotDataRow
- CombatSlotUpgradeDataRow
- DefenseSessionDataRow
- WaveGroupDataRow
- WaveDataRow
- EnemyDataRow
- DraftPoolDataRow
- DraftCardDataRow
- DraftCardEffectDataRow
- SynergyDataRow
- SkillDataRow
- AttackPatternDataRow
- ProjectileDataRow
- RewardDataRow
- MaterialDataRow

후순위:

- MiningNodeDataRow
- CraftRecipeDataRow
- GearDataRow

## 주의

- Coroutine 사용 금지
- UniTask 기반으로 작성
- 코드 주석은 한국어 한 줄 주석 중심
- Prefab/Icon/Effect는 TSV에 직접 참조하지 말고 Key만 사용
- StageData를 전투 중심으로 만들지 말 것
- 전투 중심 데이터는 DefenseSessionData
