using UnityEngine;

// BattleScene의 Static UI 초기화 지점입니다.
// 전투 프레임처럼 전투 중 위치가 거의 변하지 않는 UI를 관리합니다.
public sealed class BattleStaticUIRoot : MonoBehaviour
{
    // 전투 Static UI Root가 씬에 정상 배치되었는지 확인합니다.
    // 실제 데이터 표현은 각 View/Controller가 필요할 때 맡고, Root는 모델 상태를 보관하지 않습니다.
    public void Initialize(GameContext gameContext)
    {
        if (gameContext == null)
            Debug.LogError("[BattleStaticUIRoot] GameContext가 없어 전투 Static UI를 초기화할 수 없습니다.");
    }
}
