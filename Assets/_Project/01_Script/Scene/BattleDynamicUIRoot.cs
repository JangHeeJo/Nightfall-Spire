using UnityEngine;

// BattleScene의 Dynamic UI 초기화 지점입니다.
// 스테이지 진행도, 웨이브, 보스 HP, 쿨타임, 데미지 텍스트, 팝업 레이어를 관리합니다.
public sealed class BattleDynamicUIRoot : MonoBehaviour
{
    [SerializeField] private Transform popupLayer; // 팝업이 생성될 부모 레이어
    [SerializeField] private CanvasGroup dimLayer; // 팝업 뒤 딤 처리 레이어
    [SerializeField] private Transform toastLayer; // 토스트 메시지 부모 레이어
    [SerializeField] private Transform floatingTextLayer; // 데미지/회복 텍스트 부모 레이어
    [SerializeField] private Transform unitHpBarLayer; // 유닛 머리 위 HP바 부모 레이어

    private GameContext context; // 전투 Dynamic UI가 참조할 현재 게임 상태

    public Transform PopupLayer => popupLayer;
    public CanvasGroup DimLayer => dimLayer;
    public Transform ToastLayer => toastLayer;
    public Transform FloatingTextLayer => floatingTextLayer;
    public Transform UnitHpBarLayer => unitHpBarLayer;

    public void Initialize(GameContext gameContext)
    {
        context = gameContext;
    }
}
