using System;
using System.Collections.Generic;

// 영웅의 TargetingType과 성채 슬롯 공격 구간에 맞춰 공격 대상을 고릅니다.
// 영웅은 자기 층 슬롯이 맡은 진행도 구간 안의 몬스터만 공격합니다.
public sealed class CombatTargetingService
{
    // 살아 있는 적 목록에서 공격 대상 하나를 고릅니다.
    public CombatEnemyRuntimeState SelectTarget(CombatHeroSlotRuntimeState attacker, IReadOnlyList<CombatEnemyRuntimeState> enemies)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));

        if (enemies == null)
            throw new ArgumentNullException(nameof(enemies));

        CombatEnemyRuntimeState selected = null;

        for (int i = 0; i < enemies.Count; i++)
        {
            CombatEnemyRuntimeState enemy = enemies[i];

            if (enemy == null || !enemy.IsAlive)
                continue;

            if (!attacker.CanTarget(enemy))
                continue;

            if (selected == null || IsBetterTarget(attacker.TargetingType, enemy, selected))
                selected = enemy;
        }

        return selected;
    }

    // 타겟 규칙별로 candidate가 current보다 우선인지 판단합니다.
    private bool IsBetterTarget(TargetingType targetingType, CombatEnemyRuntimeState candidate, CombatEnemyRuntimeState current)
    {
        switch (targetingType)
        {
            case TargetingType.Farthest:
                return candidate.PathProgress < current.PathProgress ||
                       (Math.Abs(candidate.PathProgress - current.PathProgress) < 0.0001f && candidate.SpawnOrder < current.SpawnOrder);

            case TargetingType.LowestHp:
                return candidate.CurrentHp < current.CurrentHp ||
                       (candidate.CurrentHp == current.CurrentHp && candidate.SpawnOrder < current.SpawnOrder);

            case TargetingType.HighestHp:
                return candidate.CurrentHp > current.CurrentHp ||
                       (candidate.CurrentHp == current.CurrentHp && candidate.SpawnOrder < current.SpawnOrder);

            case TargetingType.Random:
                return candidate.RuntimeId < current.RuntimeId;

            case TargetingType.Nearest:
            default:
                return candidate.PathProgress > current.PathProgress ||
                       (Math.Abs(candidate.PathProgress - current.PathProgress) < 0.0001f && candidate.SpawnOrder < current.SpawnOrder);
        }
    }
}
