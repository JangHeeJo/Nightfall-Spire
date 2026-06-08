using System;

// 전투 피해 계산을 담당합니다.
// 속성 상성이나 치명타가 붙기 전까지는 공격력과 방어력만으로 기본 피해를 계산합니다.
public sealed class CombatDamageResolver
{
    // 공격자와 대상 상태를 기준으로 피해를 적용합니다.
    public CombatDamageResult ApplyBasicAttack(CombatHeroSlotRuntimeState attacker, CombatEnemyRuntimeState target)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        int rawDamage = Math.Max(0, attacker.AttackPower);
        int finalDamage = Math.Max(1, rawDamage - target.Armor);
        int appliedDamage = target.ApplyDamage(finalDamage);

        return new CombatDamageResult(
            attacker.SlotIndex,
            target.RuntimeId,
            rawDamage,
            appliedDamage,
            !target.IsAlive);
    }
}
