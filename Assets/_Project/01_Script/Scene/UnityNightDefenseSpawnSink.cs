using UnityEngine;

// 밤 방어 런타임에서 발생한 스폰 요청을 Unity 씬으로 전달하는 임시 수신자입니다.
// 실제 Enemy 프리팹 생성기는 이후 이 클래스 뒤에 EnemyFactory와 ObjectPool을 붙여 확장합니다.
public sealed class UnityNightDefenseSpawnSink : MonoBehaviour, INightDefenseSpawnSink
{
    [SerializeField] private bool logSpawnRequests = true; // 실제 프리팹 생성 전까지 스폰 요청을 콘솔에 남길지 여부

    // 런타임 스폰 컨트롤러가 발생시킨 적 생성 요청을 받습니다.
    public void Spawn(NightDefenseSpawnRequest request)
    {
        if (!logSpawnRequests)
            return;

        Debug.Log($"[NightDefenseSpawn] Wave:{request.WaveIndex} Enemy:{request.EnemyId} Lane:{request.LaneId} Time:{request.SpawnTimeSec:0.00}");
    }
}
