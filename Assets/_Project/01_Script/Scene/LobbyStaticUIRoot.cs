using UnityEngine;

// LobbyScene의 Static UI 초기화 지점입니다.
// 위치가 고정된 로비 UI들은 이 Root를 통해 GameContext를 전달받습니다.
public sealed class LobbyStaticUIRoot : MonoBehaviour
{
    private GameContext context; // 로비 UI가 참조할 현재 게임 상태

    public void Initialize(GameContext gameContext)
    {
        context = gameContext;
    }
}
