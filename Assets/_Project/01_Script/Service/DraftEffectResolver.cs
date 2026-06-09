using System;
using System.Collections.Generic;

// 선택된 드래프트 카드 효과를 전투 런타임 보정값으로 변환합니다.
// 실제 전투 수치 반영은 CombatRuntimeController가 슬롯을 다시 구성할 때 적용합니다.
public sealed class DraftEffectResolver
{
    private readonly IDraftEffectDataSource dataSource; // 카드 효과 테이블 조회 계약
    private readonly CombatRuntimeModifierSet modifierSet; // 전투 런타임 보정값 저장소

    // 효과 변환에 필요한 데이터 조회 계약과 전투 보정 저장소를 받습니다.
    public DraftEffectResolver(IDraftEffectDataSource dataSource, CombatRuntimeModifierSet modifierSet)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.modifierSet = modifierSet ?? throw new ArgumentNullException(nameof(modifierSet));
    }

    // 선택된 카드 ID에 연결된 효과를 전투 보정값으로 적용합니다.
    public DraftEffectApplyResult ApplyCardEffects(int cardId)
    {
        IReadOnlyList<DraftCardEffectDataRow> effects = dataSource.GetDraftCardEffects(cardId);

        if (effects.Count == 0)
            return DraftEffectApplyResult.Fail(DraftFailureReason.CardEffectNotFound);

        int appliedCount = 0;

        for (int i = 0; i < effects.Count; i++)
        {
            DraftCardEffectDataRow effect = effects[i];

            if (!TryCreateModifier(effect, out CombatRuntimeStatModifier modifier))
                return DraftEffectApplyResult.Fail(DraftFailureReason.UnsupportedEffectType);

            modifierSet.Add(modifier);
            appliedCount += 1;
        }

        return DraftEffectApplyResult.Success(appliedCount);
    }

    // 카드 효과 Row 하나를 전투 보정값 하나로 변환합니다.
    private static bool TryCreateModifier(DraftCardEffectDataRow effect, out CombatRuntimeStatModifier modifier)
    {
        modifier = CombatRuntimeStatModifier.Empty();

        if (effect == null)
            return false;

        float attackPercent = 0f;
        float attackSpeedPercent = 0f;
        float rangePercent = 0f;
        float skillChargePercent = 0f;

        switch (effect.EffectType)
        {
            case EffectType.AttackPercent:
                attackPercent = effect.Value;
                break;

            case EffectType.AttackSpeedPercent:
                attackSpeedPercent = effect.Value;
                break;

            case EffectType.RangePercent:
                rangePercent = effect.Value;
                break;

            case EffectType.SkillChargePercent:
                skillChargePercent = effect.Value;
                break;

            default:
                return false;
        }

        modifier = new CombatRuntimeStatModifier(
            effect.TargetScope,
            ParseSlotType(effect.TargetTagFilter),
            ParseHeroRole(effect.TargetTagFilter),
            effect.TargetTagFilter,
            attackPercent,
            attackSpeedPercent,
            rangePercent,
            skillChargePercent);

        return true;
    }

    // TargetTagFilter가 슬롯 타입 이름이면 enum으로 변환합니다.
    private static CombatSlotType ParseSlotType(string targetTagFilter)
    {
        if (Enum.TryParse(targetTagFilter, out CombatSlotType slotType))
            return slotType;

        return default;
    }

    // TargetTagFilter가 영웅 역할 이름이면 enum으로 변환합니다.
    private static HeroRole ParseHeroRole(string targetTagFilter)
    {
        if (Enum.TryParse(targetTagFilter, out HeroRole heroRole))
            return heroRole;

        return default;
    }
}
