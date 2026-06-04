using UnityEngine;

// BattleScene의 Static UI 초기화 지점입니다.
// 전투 프레임처럼 전투 중 위치가 거의 변하지 않는 UI를 관리합니다.
public sealed class BattleStaticUIRoot : MonoBehaviour
{
    private GameContext context; // 전투 Static UI가 참조할 현재 게임 상태

    public void Initialize(GameContext gameContext)
    {
        context = gameContext;
    }
}
