using System.Collections.Generic;

// 드래프트, 시너지, 장비 등 전투 중 적용되는 누적 보정값을 보관합니다.
// 전투 슬롯을 다시 구성할 때 CombatRuntimeController가 이 값을 읽어 최종 공격 수치를 계산합니다.
public sealed class CombatRuntimeModifierSet
{
    private readonly List<CombatRuntimeStatModifier> modifiers = new(); // 적용된 전투 보정 목록

    public IReadOnlyList<CombatRuntimeStatModifier> Modifiers => modifiers; // 외부 조회용 보정 목록

    // 새 전투 보정을 추가합니다.
    public void Add(CombatRuntimeStatModifier modifier)
    {
        if (modifier.IsEmpty)
            return;

        modifiers.Add(modifier);
    }

    // 전투 보정을 모두 제거합니다.
    public void Clear()
    {
        modifiers.Clear();
    }

    // 특정 슬롯/영웅에 적용되는 보정값만 합산합니다.
    public CombatRuntimeStatModifier GetModifier(CombatSlotType slotType, HeroRole heroRole, IReadOnlyList<string> heroTags)
    {
        CombatRuntimeStatModifier total = CombatRuntimeStatModifier.Empty();

        for (int i = 0; i < modifiers.Count; i++)
        {
            CombatRuntimeStatModifier modifier = modifiers[i];

            if (!modifier.Matches(slotType, heroRole, heroTags))
                continue;

            total = total.Add(modifier);
        }

        return total;
    }
}

// 하나의 전투 보정값입니다.
// 지금은 공격력과 공격 속도만 연결하고, 이후 사거리/투사체/스킬 충전도 같은 형태로 확장합니다.
public readonly struct CombatRuntimeStatModifier
{
    public TargetScope TargetScope { get; } // 적용 대상 범위
    public CombatSlotType SlotType { get; } // SlotByType 적용 시 대상 슬롯 타입
    public HeroRole HeroRole { get; } // HeroByRole 적용 시 대상 영웅 역할
    public string HeroTag { get; } // HeroByTag 적용 시 대상 태그
    public float AttackPercent { get; } // 공격력 비율 보정
    public float AttackSpeedPercent { get; } // 공격 속도 비율 보정
    public float RangePercent { get; } // 사거리 비율 보정
    public float SkillChargePercent { get; } // 스킬 충전 비율 보정

    public bool IsEmpty =>
        AttackPercent == 0f &&
        AttackSpeedPercent == 0f &&
        RangePercent == 0f &&
        SkillChargePercent == 0f;

    // 전투 보정값을 만듭니다.
    public CombatRuntimeStatModifier(
        TargetScope targetScope,
        CombatSlotType slotType,
        HeroRole heroRole,
        string heroTag,
        float attackPercent,
        float attackSpeedPercent,
        float rangePercent,
        float skillChargePercent)
    {
        TargetScope = targetScope;
        SlotType = slotType;
        HeroRole = heroRole;
        HeroTag = heroTag;
        AttackPercent = attackPercent;
        AttackSpeedPercent = attackSpeedPercent;
        RangePercent = rangePercent;
        SkillChargePercent = skillChargePercent;
    }

    // 빈 보정값을 만듭니다.
    public static CombatRuntimeStatModifier Empty()
    {
        return new CombatRuntimeStatModifier(TargetScope.AllSlots, default, default, string.Empty, 0f, 0f, 0f, 0f);
    }

    // 두 보정값의 수치를 합산합니다.
    public CombatRuntimeStatModifier Add(CombatRuntimeStatModifier other)
    {
        return new CombatRuntimeStatModifier(
            TargetScope,
            SlotType,
            HeroRole,
            HeroTag,
            AttackPercent + other.AttackPercent,
            AttackSpeedPercent + other.AttackSpeedPercent,
            RangePercent + other.RangePercent,
            SkillChargePercent + other.SkillChargePercent);
    }

    // 이 보정값이 특정 슬롯/영웅에 적용되는지 판단합니다.
    public bool Matches(CombatSlotType slotType, HeroRole heroRole, IReadOnlyList<string> heroTags)
    {
        switch (TargetScope)
        {
            case TargetScope.SlotByType:
                return SlotType == slotType;

            case TargetScope.HeroByRole:
                return HeroRole == heroRole;

            case TargetScope.HeroByTag:
                return HasHeroTag(heroTags);

            case TargetScope.AllSlots:
            default:
                return true;
        }
    }

    // 영웅 태그 목록에 대상 태그가 있는지 확인합니다.
    private bool HasHeroTag(IReadOnlyList<string> heroTags)
    {
        if (string.IsNullOrWhiteSpace(HeroTag) || heroTags == null)
            return false;

        for (int i = 0; i < heroTags.Count; i++)
        {
            if (heroTags[i] == HeroTag)
                return true;
        }

        return false;
    }
}
