using System.Collections.Generic;
using UnityEngine;

// 순수 전투 런타임 결과를 Unity 씬 표시물로 연결하는 Adapter입니다.
// 지금은 로그와 자리표시자 역할만 하고, 이후 EnemyFactory/ObjectPool/HPBar가 이 클래스 뒤에 붙습니다.
public sealed class UnityNightDefenseSpawnSink : MonoBehaviour, IBattleCombatViewSink
{
    [SerializeField] private bool logRuntimeEvents = true; // 전투 표시 계층이 붙기 전까지 런타임 이벤트를 콘솔에 남길지 여부

    // 새 전투 세션을 시작할 때 기존 적 표시물을 정리합니다.
    public void ClearBattleViews()
    {
        if (!logRuntimeEvents)
            return;

        Debug.Log("[BattleViewSink] 전투 표시물 초기화");
    }

    // 적 런타임이 생성됐을 때 Unity 표시물 생성을 요청받습니다.
    public void SpawnEnemyView(CombatEnemyRuntimeState enemy, NightDefenseSpawnRequest request)
    {
        if (!logRuntimeEvents || enemy == null)
            return;

        Debug.Log($"[BattleViewSink] Enemy Spawn Runtime:{enemy.RuntimeId} Enemy:{enemy.EnemyId} Wave:{request.WaveIndex} Lane:{request.LaneId}");
    }

    // 살아 있는 적들의 위치와 체력 표시를 갱신합니다.
    public void SyncEnemyViews(IReadOnlyList<CombatEnemyRuntimeState> enemies)
    {
        if (!logRuntimeEvents || enemies == null)
            return;

        // 매 프레임 로그를 남기면 콘솔이 터지므로 실제 표시물 연결 전에는 이 메서드를 조용히 둡니다.
    }

    // 피해 숫자나 피격 이펙트를 표시합니다.
    public void ShowDamageResults(IReadOnlyList<CombatDamageResult> damageResults)
    {
        if (!logRuntimeEvents || damageResults == null || damageResults.Count == 0)
            return;

        Debug.Log($"[BattleViewSink] Damage Count:{damageResults.Count}");
    }

    // 적 표시물을 제거하거나 풀로 반납합니다.
    public void DespawnEnemyView(CombatEnemyRuntimeState enemy, CombatEnemyDespawnReason reason)
    {
        if (!logRuntimeEvents || enemy == null)
            return;

        Debug.Log($"[BattleViewSink] Enemy Despawn Runtime:{enemy.RuntimeId} Reason:{reason}");
    }
}
