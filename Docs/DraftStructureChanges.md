# Draft Structure Changes

## Changed Scripts

- `Assets/_Project/01_Script/Service/DraftService.cs`
  - Split draft openings into `TryOpenOpeningHeroRecruitOffer` and `TryOpenLevelUpOffer`.
  - Filters cards by `DraftCardType`.
  - Tracks hero recruit cards separately from effect-based cards.
  - Treats the opening 2 hero picks as battle-start picks only, not as the total hero recruit limit.
  - Allows only the next hero upgrade step for recruited heroes.

- `Assets/_Project/01_Script/Model/DraftProgress.cs`
  - Tracks recruited heroes for the current battle.
  - Tracks selected card counts for unique and stack limits.
  - Tracks hero upgrade steps by hero and upgrade key.
  - Tracks opening hero recruit count.

- `Assets/_Project/01_Script/Core/GameFlowController.cs`
  - Keeps an already-open draft deck type intact.
  - Skips effect-table application for hero recruit cards.

- `Assets/_Project/01_Script/Service/BattleSessionRuntimeController.cs`
  - Opens the opening hero recruit draft before the first wave.
  - Opens the second opening recruit draft after the first recruit selection.
  - Uses wave clear as the temporary level-up draft trigger.

- `Assets/_Project/01_Script/Data/Enums/GameDataEnums.cs`
  - Adds `DraftDeckType`.
  - Adds `DraftCardType`.
  - Adds `CardGrade.Legendary`.

- `Assets/_Project/01_Script/Data/Rows/GameDataRows.cs`
  - Extends `DraftCardDataRow` with card type, target hero, and upgrade step columns.

- `Assets/_Project/01_Script/UI/DraftSelectionPopup.cs`
  - Adds the draft popup View root.
  - Keeps the dim background reference for blocking the battle screen behind the popup.
  - Exposes 2-card opening draft and 3-card level-up draft slots through the same prefab.

- `Assets/_Project/01_Script/UI/DraftCardSlotView.cs`
  - Adds a lightweight card slot View.
  - Keeps placeholder image/text roots so final UI art can be attached later.

## Changed Data

- `Assets/_Project/05_Data/Tables/DraftCardData.tsv`
  - Adds `CardType`, `TargetHeroId`, `UpgradeKey`, `UpgradeStep`, and `MaxStep`.
  - Renames old stat cards to `GlobalStatBuff`.
  - Adds hero recruit cards for sword, archer, magician, and axe.

- `Assets/_Project/03_Art/LobbyScene/Popup_UI/DraftSelection_Popup.prefab`
  - Adds the draft popup prefab structure.
  - Includes a full-screen semi-transparent dim background panel.
  - Includes 3 card slot placeholders for level-up drafts; opening drafts use the first 2 slots.
  - Uses mobile-safe anchors and pivots instead of fixed 1280x720 positioning.
  - Applies `SafeAreaFitter` to the popup content so notches, home bars, and rounded corners do not cover card choices.
  - Keeps the dim background full-screen while the selectable content stays inside the safe area.

## UI Layout Rule

- Battle popup UI is mobile-first.
- Full-screen dim or touch-block panels stretch to the entire Canvas.
- Actual selectable content stays inside a Safe Area root.
- Draft card choices stay in a horizontal row on mobile, matching the reference gameplay flow.
- Opening drafts center 2 horizontal cards; level-up drafts show 3 horizontal cards.
- Each card keeps an upright vertical card ratio; only the card group is arranged horizontally.
- Card slots and inner placeholder elements use proportional RectTransform anchors so they scale across phone and tablet aspect ratios.
- Draft popup size and position are controlled by prefab RectTransform values, not runtime layout code.
- Art can be replaced later without changing the responsive popup structure.

## Current Draft Rules

- Opening recruit draft:
  - 2 cards shown.
  - 1 card selected.
  - Runs 2 times at battle start.
  - This limits only the opening picks, not the total number of heroes in the battle.
  - Only unlocked and unrecruited heroes can appear.

- Level-up draft:
  - 3 cards shown.
  - Can include unrecruited unlocked heroes after the opening 2 picks.
  - Hero recruit cards stop only after every unlocked hero has been recruited for the current battle.
  - Can include global stat buffs.
  - Can include recruited hero upgrades.
  - Hero upgrades only appear in the next valid step order.
