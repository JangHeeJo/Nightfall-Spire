using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// BattleScene에서 순수 C# 전투 세션 런타임을 매 프레임 실행하는 Unity 연결 계층입니다.
public sealed class NightDefenseBattleRuntime : MonoBehaviour
{
    private const string DraftPopupAssetPath = "Assets/_Project/03_Art/LobbyScene/Popup_UI/DraftSelection_Popup.prefab"; // 에디터에서 씬 참조가 비었을 때 복구할 프리팹 경로

    [SerializeField] private bool enableDraftSelections; // 카드 드래프트 UI가 완성되기 전에는 꺼두고 전투 1사이클만 바로 확인합니다.
    [SerializeField] private DraftSelectionPopup draftSelectionPopupPrefab; // 전투 중 카드 선택에 사용할 팝업 프리팹

    private BattleSessionRuntimeController sessionRuntime; // 웨이브, 전투 계산, 승패를 묶은 실제 전투 런타임
    private GameContext gameContext; // 드래프트 진행 상태와 카드 테이블 조회용 Context
    private GameFlowController gameFlowController; // 카드 선택 확정과 게임 상태 전환 담당
    private PopupHandle draftPopupHandle; // 현재 열려 있는 드래프트 팝업 핸들
    private bool isOpeningDraftPopup; // 비동기 팝업 열기 중복 방지 플래그
    private bool isTicking; // Update에서 런타임 Tick을 돌릴지 여부

    // BattleSceneController가 GameContext와 표시 계층을 준비한 뒤 전투 런타임을 시작합니다.
    public bool Initialize(GameContext gameContext, GameFlowController gameFlowController, IBattleCombatViewSink viewSink)
    {
        this.gameContext = gameContext;
        this.gameFlowController = gameFlowController;
        sessionRuntime = new BattleSessionRuntimeController(gameContext, gameFlowController, viewSink, enableDraftSelections);
        BattleRuntimeStartResult startResult = sessionRuntime.Start();

        if (!startResult.IsSuccess)
        {
            Debug.LogError($"[NightDefenseBattleRuntime] 전투 런타임 시작 실패: {startResult.FailureReason}, Combat:{startResult.CombatFailureReason}, Wave:{startResult.NightDefenseFailureReason}");
            isTicking = false;
            return false;
        }

        isTicking = true;
        Debug.Log($"[NightDefenseBattleRuntime] 전투 런타임 시작 완료 Draft:{enableDraftSelections}");
        return true;
    }

    // Unity 프레임 시간에 맞춰 밤 방어 세션 런타임을 진행합니다.
    private void Update()
    {
        if (!isTicking || sessionRuntime == null)
            return;

        TryOpenDraftPopup();

        BattleRuntimeTickResult result = sessionRuntime.Tick(Time.deltaTime);
        TryOpenDraftPopup();

        if (result.State == BattleRuntimeState.Victory
            || result.State == BattleRuntimeState.Defeat
            || result.State == BattleRuntimeState.Abandoned)
        {
            isTicking = false;
        }
    }

    // GameFlow가 드래프트 상태로 들어가면 실제 팝업을 한 번만 엽니다.
    private void TryOpenDraftPopup()
    {
        if (!enableDraftSelections || isOpeningDraftPopup || draftPopupHandle != null)
            return;

        if (gameContext?.DraftProgress?.IsDraftOpen.Value != true)
            return;

        if (gameContext.GameProgress.CurrentState.Value != GameState.DraftSelection)
            return;

        OpenDraftPopupAsync().Forget();
    }

    // PopupManager를 통해 드래프트 팝업을 열고 현재 후보 카드 Row를 주입합니다.
    private async UniTaskVoid OpenDraftPopupAsync()
    {
        isOpeningDraftPopup = true;

        await UniTask.WaitUntil(() => GameRoot.Instance?.PopupManager?.HasActiveLayers == true);

        DraftSelectionPopup popupPrefab = ResolveDraftPopupPrefab();

        if (popupPrefab == null || GameRoot.Instance?.PopupManager == null)
        {
            Debug.LogError($"[NightDefenseBattleRuntime] 드래프트 팝업 프리팹을 찾지 못했습니다. Inspector 참조 또는 경로를 확인하세요. Path:{DraftPopupAssetPath}");
            isOpeningDraftPopup = false;
            return;
        }

        List<DraftCardDataRow> offeredCards = BuildOfferedDraftCards();
        PopupRequest<DraftSelectionPopup> request = new PopupRequest<DraftSelectionPopup>(
            "BattleDraftSelection",
            popupPrefab,
            PopupOpenPolicy.SingleInstance,
            PopupLayerSlot.Overlay,
            PopupPriority.Important,
            useDim: true,
            configureBeforeOpen: popup =>
            {
                popup.BindCards(offeredCards);
                popup.SetCardsInteractable(true);
                popup.CardSelected += HandleDraftCardSelected;
            });

        draftPopupHandle = await GameRoot.Instance.PopupManager.OpenAsync(request);

        if (draftPopupHandle == null)
        {
            isOpeningDraftPopup = false;
            return;
        }

        await draftPopupHandle.ResultTask;

        if (draftPopupHandle?.Popup is DraftSelectionPopup popup)
            popup.CardSelected -= HandleDraftCardSelected;

        draftPopupHandle = null;
        isOpeningDraftPopup = false;
    }

    // 씬 직렬화 참조가 비어 있어도 에디터 플레이 중에는 에셋 경로로 프리팹을 복구합니다.
    private DraftSelectionPopup ResolveDraftPopupPrefab()
    {
        if (draftSelectionPopupPrefab != null)
            return draftSelectionPopupPrefab;

#if UNITY_EDITOR
        draftSelectionPopupPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<DraftSelectionPopup>(DraftPopupAssetPath);
#endif
        return draftSelectionPopupPrefab;
    }

    // 현재 DraftProgress의 카드 ID 순서를 유지하면서 테이블 Row 목록으로 변환합니다.
    private List<DraftCardDataRow> BuildOfferedDraftCards()
    {
        List<DraftCardDataRow> cards = new List<DraftCardDataRow>();
        IReadOnlyList<int> offeredCardIds = gameContext.DraftProgress.OfferedCardIds;
        IReadOnlyList<DraftCardDataRow> allCards = gameContext.ContentDataSource.GetDraftCards();

        for (int i = 0; i < offeredCardIds.Count; i++)
        {
            if (TryFindDraftCard(allCards, offeredCardIds[i], out DraftCardDataRow card))
                cards.Add(card);
        }

        return cards;
    }

    // 카드 ID 하나에 맞는 DraftCardData Row를 찾습니다.
    private static bool TryFindDraftCard(IReadOnlyList<DraftCardDataRow> cards, int cardId, out DraftCardDataRow card)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].CardId != cardId)
                continue;

            card = cards[i];
            return true;
        }

        card = null;
        return false;
    }

    // 카드 버튼 선택을 GameFlow에 확정하고, 성공한 경우 팝업을 닫아 전투 재개를 허용합니다.
    private void HandleDraftCardSelected(int cardId)
    {
        if (draftPopupHandle?.Popup is DraftSelectionPopup popup)
            popup.SetCardsInteractable(false);

        bool selected = gameFlowController != null && gameFlowController.SelectDraftCard(cardId);

        if (!selected)
        {
            if (draftPopupHandle?.Popup is DraftSelectionPopup retryPopup)
                retryPopup.SetCardsInteractable(true);

            return;
        }

        CloseDraftPopupAsync(cardId).Forget();
    }

    // 선택이 끝난 드래프트 팝업을 닫습니다.
    private async UniTaskVoid CloseDraftPopupAsync(int selectedCardId)
    {
        if (draftPopupHandle == null || GameRoot.Instance?.PopupManager == null)
            return;

        await GameRoot.Instance.PopupManager.CloseAsync(draftPopupHandle, PopupCloseReason.Confirmed, selectedCardId);
    }

    // 씬이 내려갈 때 진행 중인 런타임을 중단 상태로 정리합니다.
    private void OnDestroy()
    {
        if (draftPopupHandle?.Popup is DraftSelectionPopup popup)
            popup.CardSelected -= HandleDraftCardSelected;

        sessionRuntime?.StopAsAbandoned();
        sessionRuntime = null;
        gameContext = null;
        gameFlowController = null;
        draftPopupHandle = null;
        isTicking = false;
    }
}
