using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed partial class AetheriaGame : MonoBehaviour
{
    private const string DungeonMapBuildLabel = "아에테리아 원정 지도";
    private const string DungeonMapSpritePath = "WebVersion/assets/world-map-v2.png";
    private const string DungeonClosedScrollSpritePath = "WebVersion/assets/closed-bound-scroll.png";
    private const float DungeonMapBaseWidth = 1920f;
    private const float DungeonMapBaseHeight = 1080f;
    private const float DungeonMapRegionZoom = 2.35f;
    private const float DungeonMapMinZoom = 1f;
    private const float DungeonMapMaxZoom = 4.5f;
    private const float DungeonMapPinRevealZoom = DungeonMapRegionZoom;
    private const float DungeonMapWheelStep = 0.22f;
    private const float DungeonMapOpenAnimationSeconds = 1.15f;
    private const float DungeonMapPlateAlpha = 0.30f;
    private const int SaveSlotCount = 7;
    private const int CurrentSaveVersion = 2;
    private const int NamedRarity = 12;
    private const int MaxRecipeIngredientRarity = 10;
    private const float BaseTurnManaRegen = 0.075f;
    private const float MonsterTurnManaRegen = 0.15f;
    private const float ManaRegenCap = 0.15f;
    private const float GearEnhancementGrowth = 0.15f;
    private const float GearEnhancementSuccessChance = 0.8f;
    private const float BaseExperienceRewardMultiplier = 1.35f;
    private const float BossExperienceRewardMultiplier = 1.15f;
    private const float UnderRecommendedLevelXpBonusPerLevel = 0.07f;
    private const float UnderRecommendedLevelXpBonusCap = 0.55f;
    private const int PhysicalSkillMpCostPerLevel = 6;
    private const int MagicSkillMpCostPerLevel = 8;
    private const float PhysicalSkillMpCostBaseGrowth = 0.035f;
    private const float MagicSkillMpCostBaseGrowth = 0.06f;
    private const float PhysicalAccessoryMaxMpSkillCostRate = 0.04f;
    private const float MagicAccessoryMaxMpSkillCostRate = 0.08f;
    private const float AccessoryMaxMpSkillCostLevelGrowth = 0.02f;
    private const bool LogStatusFailure = true;
    private const int MaxVisibleCombatLogLines = 14;
    private const int MaxStoredBattleLogLines = 90;
    private const int MaxInventoryItems = 180;
    private const int MaxSkillLevel = 30;
    private const int MaxGearEnhancementLevel = 30;
    private const int MaxDungeonFloors = 30;
    private const int MaxGeneralLogLines = 12;
    private const float UiScrollbarWidth = 12f;
    private const int UiButtonDefaultTextSize = 24;
    private static AetheriaGame instance;
    private static readonly int[] CraftingRecipeCosts = { 40, 80, 150, 280, 520, 900, 1500, 2400, 3600, 5200, 7600 };
    private static readonly int[] RaritySellValues = { 8, 14, 24, 50, 120, 260, 520, 1000, 1600, 2600, 4200, 6500, 9000 };
    private static readonly float[] RarityCostMultipliers = { 1f, 1.15f, 1.3f, 1.5f, 2f, 2.8f, 3.8f, 5.2f, 6.2f, 7.4f, 8.8f, 10.5f, 12f };

    private static int RoundToGameInt(float value)
    {
        return Mathf.FloorToInt(value + 0.5f);
    }

    private static float RoundToGameHundredth(float value)
    {
        return RoundToGameInt(value * 100f) / 100f;
    }

    private readonly Color pageColor = Rgb(232, 242, 249);
    private readonly Color panelColor = Rgba(255, 252, 244, 244);
    private readonly Color panelAltColor = Rgba(221, 238, 249, 246);
    private readonly Color goldColor = Rgb(157, 96, 18);
    private readonly Color goodColor = Rgb(9, 130, 101);
    private readonly Color dangerColor = Rgb(190, 47, 61);
    private readonly Color manaColor = Rgb(0, 108, 151);
    private readonly Color textColor = Rgb(20, 39, 61);
    private readonly Color mutedColor = Rgb(48, 65, 82);
    private readonly Color neonPurple = Rgb(112, 69, 163);
    private readonly Color buttonTextColor = Rgb(16, 32, 57);
    private readonly Color disabledButtonTextColor = Rgb(91, 105, 121);
    private Sprite mapCircleSprite;
    private Sprite mapRoundedRectSprite;
    private List<DungeonMapRegion> cachedDungeonMapRegions;
    private readonly Dictionary<string, Sprite> streamingSpriteCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Sprite> portraitSpriteCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, List<SkillState>> skillCacheByClass = new Dictionary<string, List<SkillState>>();
    private readonly Dictionary<int, AetheriaRarityData> rarityCache = new Dictionary<int, AetheriaRarityData>();
    private List<DungeonData> cachedDungeons;
    private List<HeroClass> cachedHeroClasses;

    private Canvas canvas;
    private RectTransform root;
    private Font uiFont;
    private PlayerState player;
    private bool playerDataValidated;
    private EnemyState currentEnemy;
    private DungeonData currentDungeon;
    private readonly List<string> battleLog = new List<string>();
    private readonly List<StatusEffect> playerStatusEffects = new List<StatusEffect>();
    private readonly List<StatusEffect> enemyStatusEffects = new List<StatusEffect>();
    private int activeSlot;
    private int selectedInventoryIndex = -1;
    private string selectedHeroClassName = "";
    private bool dungeonMapZoomed;
    private bool dungeonMapScrollOpened;
    private bool dungeonMapScrollOpening;
    private bool dungeonMapBuildLogged;
    private bool guideReturnToMainMenu;
    private string selectedDungeonRegionKey = "green";
    private float dungeonMapZoom = 1f;
    private Vector2 dungeonMapPan = Vector2.zero;
    private Coroutine dungeonMapOpenCoroutine;
    private int currentDungeonFloor;
    private int enemyManaRegenTurnCounter;
    private bool actionLocked;
    private bool currentEnemyIsBoss;
    private AetheriaScreen currentScreen;
    private InventorySortMode inventorySortMode = InventorySortMode.Power;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindFirstObjectByType<AetheriaGame>() != null)
        {
            return;
        }

        var game = new GameObject("Aetheria Game");
        game.AddComponent<AetheriaGame>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAetheriaAudio();
        LoadGameDatabase();
        uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans KR", "Malgun Gothic", "맑은 고딕", "Arial" }, 20);
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        CreateCanvas();
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        ReleaseAetheriaAudio();
        ReleaseCombatPresentationResources();
        ReleaseRuntimeSkillIconResources();
        ReleaseDungeonRegionVisuals();
        ReleaseGeneratedSprites();
        instance = null;
    }

    private void CreateCanvas()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
#if ENABLE_INPUT_SYSTEM
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#else
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
            DontDestroyOnLoad(eventSystem);
        }

        var canvasObject = new GameObject("Aetheria Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        root = AddPanel("Root", canvas.transform, pageColor);
        Stretch(root, 0, 0, 0, 0);
    }

    private void ShowMainMenu()
    {
        currentScreen = AetheriaScreen.MainMenu;
        ResetCombatState(true);
        selectedHeroClassName = "";
        activeSlot = Mathf.Clamp(activeSlot, 0, SaveSlotCount - 1);
        ClearRoot();

        var frame = AddPanel("Main Menu", root, pageColor);
        Stretch(frame, 0, 0, 0, 0);
        AddVertical(frame, 16, TextAnchor.MiddleCenter, new RectOffset(120, 120, 42, 42));

        var titleCard = AddReadabilityPlate(frame, "Main Menu Title", Rgb(9, 25, 47));
        ConstrainLayoutSize(titleCard, 960, 178);
        AddVertical(titleCard, 0, TextAnchor.MiddleCenter, new RectOffset(28, 28, 10, 10));
        AddAetheriaCrestWatermark(titleCard, "Aetheria Main Menu Crest", new Vector2(0.40f, 0.03f), new Vector2(0.60f, 0.97f), 0.18f);
        AddText(titleCard, "AETHERIA", 76, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 92);
        AddText(titleCard, "빛의 원정", 30, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 42);
        AddText(titleCard, "영웅을 선택하고 아에테리아의 원정을 이어가세요", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 30);

        var slotPanel = AddPanel("Save Slots", frame, Color.clear);
        AddLayoutSize(slotPanel, -1, 430);
        AddVertical(slotPanel, 12, TextAnchor.UpperCenter, new RectOffset(12, 12, 8, 8));
        AddReadabilityTextBlock(slotPanel, "Adventure Records", "모험 기록", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 48, Rgb(12, 30, 52));

        var slots = AddPanel("Slot Grid", slotPanel, new Color(0, 0, 0, 0));
        AddLayoutSize(slots, -1, 318);
        var slotGrid = slots.gameObject.AddComponent<GridLayoutGroup>();
        slotGrid.cellSize = new Vector2(390, 146);
        slotGrid.spacing = new Vector2(12, 12);
        slotGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        slotGrid.constraintCount = 4;
        slotGrid.childAlignment = TextAnchor.UpperCenter;

        for (var i = 0; i < SaveSlotCount; i++)
        {
            var slot = i;
            var selected = slot == activeSlot;
            var label = HasSave(slot)
                ? "원정 일지 " + (slot + 1) + "\n" + SavePreview(slot)
                : "봉인된 일지 " + (slot + 1) + "\n새로운 원정을 기록할 수 있습니다";
            var button = AddObjectActionButton(slots, label, () =>
            {
                activeSlot = slot;
                ShowMainMenu();
            }, selected ? goldColor : manaColor, VisualActionRole.Journal, "save_journal", 390, 146);
            button.gameObject.name = selected ? "Selected Adventure Journal" : "Adventure Journal";
        }

        var buttons = AddRow("Menu Buttons", frame, 12, TextAnchor.MiddleCenter);
        ConstrainLayoutSize(buttons, 1420, 92);
        ConfigureNonExpandingRow(buttons);
        var loadButton = AddObjectActionButton(buttons, HasSave(activeSlot) ? "원정 계속하기" : "저장 기록 없음", LoadSelectedSaveSlot, goodColor, VisualActionRole.Confirm, "continue", 360, 88);
        loadButton.interactable = HasSave(activeSlot);
        var canStartAdventure = !HasSave(activeSlot) || HasEmptySaveSlot();
        var newAdventureLabel = !HasSave(activeSlot) ? "새 원정 시작" : (HasEmptySaveSlot() ? "빈 일지에 새 원정" : "빈 일지 없음");
        var newAdventureButton = AddObjectActionButton(buttons, newAdventureLabel, () =>
        {
            ShowClassSelect(HasSave(activeSlot) ? FirstEmptySaveSlot() : activeSlot);
        }, manaColor, VisualActionRole.Portal, "new_adventure", 360, 88);
        newAdventureButton.interactable = canStartAdventure;
        var resetButton = AddObjectActionButton(buttons, "기록 삭제 " + (activeSlot + 1), () =>
        {
            ShowDeleteSaveConfirm(activeSlot);
        }, dangerColor, VisualActionRole.Cancel, "delete_record", 310, 88);
        resetButton.interactable = HasSave(activeSlot);
        AddObjectActionButton(buttons, "플레이 방법", () => ShowHowToPlay(true), goldColor, VisualActionRole.Guide, "guide", 310, 88);

        var mainMenuHint = AddReadabilityTextBlock(frame, "Main Menu Hint", "처음이라면 ‘플레이 방법’에서 전투 흐름을 먼저 확인하세요.", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 46, Rgb(9, 25, 47));
        ConstrainLayoutSize(mainMenuHint, 1080, 46);
        ApplyVisualRefreshToScreen(frame);
    }

    private void LoadSelectedSaveSlot()
    {
        activeSlot = Mathf.Clamp(activeSlot, 0, SaveSlotCount - 1);
        if (!HasSave(activeSlot))
        {
            ShowMainMenu();
            return;
        }

        if (LoadGame(activeSlot))
        {
            if (!player.hasSeenGuide)
            {
                ShowHowToPlay(false);
            }
            else
            {
                ShowTown("슬롯 " + (activeSlot + 1) + "의 기록을 불러왔습니다.");
            }
        }
        else
        {
            ShowMainMenu();
        }
    }

    private void ShowClassSelect(int slot)
    {
        currentScreen = AetheriaScreen.ClassSelect;
        activeSlot = slot;
        var heroClasses = HeroClasses();
        if (string.IsNullOrEmpty(selectedHeroClassName) && heroClasses.Count > 0)
        {
            selectedHeroClassName = heroClasses[0].name;
        }
        ClearRoot();
        var selectedHeroClass = heroClasses.Find(entry => entry.name == selectedHeroClassName);
        var selectedAccent = selectedHeroClass != null ? ClassAccentColor(selectedHeroClass.name) : goldColor;

        var page = AddPanel("Class Select", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 12, TextAnchor.UpperCenter, new RectOffset(44, 44, 24, 24));
        AddReadabilityTextBlock(page, "Hero Select Header", "영웅 선택  ·  각 영웅은 고유한 전투 방식과 UI를 가집니다", 34, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 64, Rgb(9, 25, 47));

        var body = AddRow("Hero Selection Stage", page, 22, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 842);

        var showcase = AddPanel("Selected Hero Showcase", body, Color.clear);
        AddLayoutSize(showcase, 700, -1);
        AddVertical(showcase, 8, TextAnchor.UpperCenter, new RectOffset(20, 20, 10, 10));
        if (selectedHeroClass != null)
        {
            AddReadabilityTextBlock(showcase, "Selected Hero Name", selectedHeroClass.name + "  ·  " + ClassRoleLabel(selectedHeroClass.name), 32, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 54, Rgb(9, 25, 47));
            AddGroundingShadow(showcase, 380, 52, new Vector2(0f, 168f), 0.22f);
            AddCharacterArt(showcase, selectedHeroClass.portraitName, "idle", 440, 500);
            AddReadabilityTextBlock(showcase, "Selected Hero Description", selectedHeroClass.description, 21, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 92, Rgb(9, 25, 47));
            var statRow = AddRow("Selected Hero Stat Badges", showcase, 8, TextAnchor.MiddleCenter);
            AddLayoutSize(statRow, -1, 48);
            AddStatusChip(statRow, "HP " + selectedHeroClass.hp, VisualStatusTone.Good, null, 118, 42);
            AddStatusChip(statRow, "MP " + selectedHeroClass.mp, VisualStatusTone.Mana, null, 118, 42);
            AddStatusChip(statRow, "공격 " + selectedHeroClass.attack, VisualStatusTone.Warning, null, 128, 42);
            AddStatusChip(statRow, "마력 " + selectedHeroClass.magic, VisualStatusTone.Information, null, 128, 42);
            AddStatusChip(statRow, "속도 " + RoundToGameInt(BaseSpeedForClass(selectedHeroClass.name)), VisualStatusTone.Neutral, null, 128, 42);
        }

        var roster = AddPanel("Hero Roster", body, Color.clear);
        AddLayoutSize(roster, -1, -1);
        AddVertical(roster, 10, TextAnchor.UpperCenter, new RectOffset(8, 8, 6, 6));
        AddReadabilityTextBlock(roster, "Hero Roster Header", "원정대 명단", 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 48, Rgb(9, 25, 47));

        var grid = AddPanel("Class Grid", roster, new Color(0, 0, 0, 0));
        AddLayoutSize(grid, -1, 720);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(250, 344);
        layout.spacing = new Vector2(10, 10);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.childAlignment = TextAnchor.UpperCenter;

        foreach (var heroClass in heroClasses)
        {
            var localHeroClass = heroClass;
            var accent = ClassAccentColor(heroClass.name);
            var selected = selectedHeroClassName == heroClass.name;
            var card = AddPanel(heroClass.name, grid, Color.white);
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = card.GetComponent<Image>();
            cardButton.onClick.AddListener(() =>
            {
                PlayUiClickSound();
                selectedHeroClassName = localHeroClass.name;
                ShowClassSelect(activeSlot);
            });
            ApplyCharacterThemeCardButton(cardButton, heroClass.portraitName, selected);

            var contentObject = new GameObject("Class Card Content", typeof(RectTransform));
            contentObject.transform.SetParent(card, false);
            var content = contentObject.GetComponent<RectTransform>();
            Stretch(content, 22, 18, 22, 18);
            var contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 5;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;

            AddCharacterArt(content, heroClass.portraitName, "idle", 176, 176);
            AddText(content, heroClass.name, 24, FontStyle.Bold, CharacterThemeHeading(heroClass.portraitName, accent), TextAnchor.MiddleCenter, 38);
            AddText(content, ClassRoleLabel(heroClass.name), 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 32);
            AddText(content, "HP " + heroClass.hp + "   MP " + heroClass.mp + "\n공격 " + heroClass.attack + "   마력 " + heroClass.magic + "   속도 " + RoundToGameInt(BaseSpeedForClass(heroClass.name)), 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 58);
        }

        var buttons = AddRow("Class Select Actions", page, 16, TextAnchor.MiddleCenter);
        AddLayoutSize(buttons, -1, 78);
        var startButton = AddObjectActionButton(buttons, selectedHeroClass != null ? selectedHeroClass.name + "으로 원정 시작" : "영웅을 먼저 선택하세요", StartSelectedHeroClass, selectedAccent, VisualActionRole.Confirm, "start_expedition", 760, 74);
        startButton.interactable = selectedHeroClass != null;
        AddObjectActionButton(buttons, "메인 메뉴로", ShowMainMenu, panelAltColor, VisualActionRole.Back, "back", 400, 74);
        ApplyVisualRefreshToScreen(page);
    }

    private string ClassRoleLabel(string className)
    {
        switch (className)
        {
            case "성기사": return "방패 · 보호 · 기절";
            case "원소술사": return "원소 마법 · 광역 제어";
            case "그림자 자객": return "치명타 · 독 · 흡혈";
            case "빛의 사제": return "회복 · 성역 · 약화";
            case "폭렬술사": return "폭발 · 화상 · 파쇄";
            case "정령술사": return "정령 교대 · 속성 대응";
            case "바람 궁수": return "조준 · 속박 · 연사";
            case "무투가": return "연계 · 기절 · 강화";
            default: return "균형형 원정 영웅";
        }
    }

    private void StartSelectedHeroClass()
    {
        var selectedHeroClass = HeroClasses().Find(entry => entry.name == selectedHeroClassName);
        if (selectedHeroClass == null)
        {
            ShowClassSelect(activeSlot);
            return;
        }

        player = CreatePlayer(selectedHeroClass);
        MarkPlayerDataDirty();
        selectedHeroClassName = "";
        SaveGame();
        ShowHowToPlay(false);
    }

    private void ShowHowToPlay(bool returnToMainMenu)
    {
        guideReturnToMainMenu = returnToMainMenu || player == null;
        currentScreen = AetheriaScreen.Guide;

        ClearRoot();
        var page = AddPanel("How To Play", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 14, TextAnchor.UpperCenter, new RectOffset(42, 42, 26, 26));

        var header = AddReadabilityPlate(page, "Guide Header", Rgb(9, 25, 47));
        ConstrainLayoutSize(header, 1720, 118);
        AddVertical(header, 2, TextAnchor.MiddleCenter, new RectOffset(24, 24, 10, 10));
        AddAetheriaCrestWatermark(header, "Aetheria Guide Crest", new Vector2(0.45f, 0.03f), new Vector2(0.55f, 0.97f), 0.16f);
        AddText(header, "처음 3분 플레이 방법", 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 58);
        AddText(header, "마을  →  지도  →  전투  →  성장", 23, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 34);

        var flow = AddRow("Guide Flow", page, 14, TextAnchor.UpperCenter);
        ConstrainLayoutSize(flow, 1720, 334);
        ConfigureNonExpandingRow(flow);
        AddGuideFlowRoute(flow);
        AddGuideStep(flow, "1  마을", "‘던전 탐험’을 누릅니다.\n체력이 부족하면 여관에서 먼저 회복하세요.", goodColor, "UI/VisualRefresh/Objects/inn");
        AddGuideStep(flow, "2  지도", "지역 문장을 고르거나 오른쪽 아래 ‘추천 전투 시작’으로 바로 출발합니다.", goldColor, "UI/VisualRefresh/Objects/portal");
        AddGuideStep(flow, "3  전투", "적의 다음 행동과 MP를 확인한 뒤 사용할 카드를 고르세요.\n마우스 또는 패드로 선택하며 단축키는 보조 기능입니다.", manaColor, "UI/VisualRefresh/Routes/combat");
        AddGuideStep(flow, "4  성장", "승리 후 마을에서 장비를 착용·강화하고 다음 던전에 도전합니다.", neonPurple, "UI/VisualRefresh/Growth/equipment_armor");

        var rules = AddReadabilityPlate(page, "Guide Rules", Rgb(9, 25, 47));
        ConstrainLayoutSize(rules, 1720, 424);
        AddVertical(rules, 8, TextAnchor.UpperLeft, new RectOffset(30, 30, 18, 18));
        AddText(rules, "전투에서 이것만 기억하세요", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 42);
        AddMessageBanner(rules, "목표: 적 HP를 0으로 만들기  ·  스킬은 MP를 사용  ·  적 턴에는 잠시 기다리기", goodColor, 70);
        AddText(rules,
            "• 카드를 마우스로 클릭하거나 패드로 선택할 수 있습니다. 단축키는 선택 사항입니다.\n"
            + "• 스킬 버튼이 회색으로 비활성화되면 MP가 부족하거나 침묵 상태입니다. 기본 공격은 MP를 쓰지 않습니다.\n"
            + "• 일반 층을 돌파하면 다음 층으로 이어지고, 마지막 층의 보스를 쓰러뜨리면 다음 던전이 열립니다.\n"
            + "• 위험하면 ESC 또는 ‘던전 나가기’로 마을에 돌아갈 수 있습니다.",
            21, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 175);
        AddText(rules, "추천 첫 행동: 마을에서 ‘던전으로 출발’ → ‘추천 전투 시작’", 24, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 44);

        var buttons = AddRow("Guide Actions", page, 14, TextAnchor.MiddleCenter);
        ConstrainLayoutSize(buttons, 1720, 78);
        ConfigureNonExpandingRow(buttons);
        AddObjectActionButton(buttons, guideReturnToMainMenu ? "확인 · 메인 메뉴로" : "확인 · 마을에서 시작", () =>
        {
            if (player != null && !guideReturnToMainMenu)
            {
                player.hasSeenGuide = true;
                SaveGame();
            }

            if (guideReturnToMainMenu)
            {
                ShowMainMenu();
            }
            else
            {
                ShowTown("가이드 확인 완료. 먼저 던전 탐험을 선택해 보세요.");
            }
        }, goodColor, VisualActionRole.Confirm, "guide_confirm", 820, 74);
        ApplyVisualRefreshToScreen(page);
    }

    private void AddGuideStep(Transform parent, string title, string description, Color accent, string visualPath)
    {
        var card = AddReadabilityPlate(parent, "Guide Step " + title, Rgb(9, 25, 47));
        ConstrainLayoutSize(card, 398, 320);
        AddSideAccent(card, accent);
        AddVertical(card, 7, TextAnchor.UpperCenter, new RectOffset(18, 18, 16, 16));
        var visualSprite = LoadGeneratedSprite(visualPath, Vector4.zero);
        if (visualSprite != null)
        {
            var visual = AddFlatPanel("Guide Step Visual " + title, card, Color.white);
            AddLayoutSize(visual, 76, 76);
            var visualImage = visual.GetComponent<Image>();
            visualImage.sprite = visualSprite;
            visualImage.preserveAspect = true;
            visualImage.raycastTarget = false;
        }
        else
        {
            AddIconBadge(card, null, title.Substring(0, 1), accent, 64);
        }
        AddText(card, title, 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 40);
        AddDivider(card, new Color(accent.r, accent.g, accent.b, 0.80f));
        AddText(card, description, 20, FontStyle.Bold, Color.white, TextAnchor.UpperLeft, 154);
    }

    private void ShowDeleteSaveConfirm(int slot)
    {
        slot = Mathf.Clamp(slot, 0, SaveSlotCount - 1);
        activeSlot = slot;
        currentScreen = AetheriaScreen.MainMenu;
        ClearRoot();

        var page = AddPanel("Delete Save Confirm", root, pageColor);
        Stretch(page, 0, 0, 0, 0);

        var panel = AddReadabilityPlate(page, "Delete Save Confirm", Rgb(67, 16, 24));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = panel.anchorMin;
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(760f, 430f);
        AddSideAccent(panel, dangerColor);
        AddVertical(panel, 14, TextAnchor.MiddleCenter, new RectOffset(42, 42, 30, 30));

        AddDeleteRecordVisual(panel, 64);
        AddText(panel, "저장 기록을 삭제할까요?", 40, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 58);
        AddText(panel, "원정 일지 " + (slot + 1) + " · 삭제한 기록은 복구할 수 없습니다.", 21, FontStyle.Bold, dangerColor, TextAnchor.MiddleCenter, 42);
        AddText(panel, HasSave(slot) ? SavePreview(slot) : "선택한 슬롯에는 저장 데이터가 없습니다.", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 76);

        var buttons = AddRow("Delete Save Buttons", panel, 14, TextAnchor.MiddleCenter);
        AddLayoutSize(buttons, -1, 74);
        var deleteButton = AddObjectActionButton(buttons, "기록 삭제", () =>
        {
            DeleteSave(slot);
            ShowMainMenu();
        }, dangerColor, VisualActionRole.Cancel, "delete_record", 300, 70);
        deleteButton.interactable = HasSave(slot);
        AddObjectActionButton(buttons, "취소", ShowMainMenu, panelAltColor, VisualActionRole.Back, "back", 300, 70);
        ApplyVisualRefreshToScreen(page);
    }

    private void ShowTown(string message)
    {
        EnsurePlayerData();
        currentScreen = AetheriaScreen.Town;
        ResetCombatState(true);
        selectedInventoryIndex = -1;
        ClearRoot();

        AddGeneralLog(message);
        SaveGame();
        var dungeonTotal = Dungeons().Count;
        var unlockedDungeonLabel = dungeonTotal > 0 ? Mathf.Clamp(player.stage, 1, dungeonTotal) + "/" + dungeonTotal : player.stage.ToString();
        var townCharacterState = !string.IsNullOrEmpty(message)
            && (message.Contains("휴식") || message.Contains("회복") || message.Contains("여관"))
            ? "rest"
            : "idle";

        var page = AddPanel("Town", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 10, TextAnchor.UpperCenter, new RectOffset(24, 24, 16, 16));

        var hud = AddReadabilityPlate(page, "Town HUD", Rgb(9, 25, 47));
        AddLayoutSize(hud, -1, 190);
        var hudLayout = hud.gameObject.AddComponent<HorizontalLayoutGroup>();
        hudLayout.spacing = 14;
        hudLayout.padding = new RectOffset(18, 18, 10, 10);
        hudLayout.childAlignment = TextAnchor.MiddleLeft;
        hudLayout.childControlWidth = true;
        hudLayout.childControlHeight = true;
        hudLayout.childForceExpandWidth = false;
        hudLayout.childForceExpandHeight = false;

        var hudStats = AddPanel("HUD Stats", hud, new Color(0, 0, 0, 0));
        AddLayoutSize(hudStats, 790, 164);
        AddVertical(hudStats, 2, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
        AddText(hudStats, "Lv." + player.level + "  " + player.heroName + "  ·  " + player.heroClass, 28, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 32);
        AddText(hudStats, "원정 " + unlockedDungeonLabel + "   골드 " + player.gold + " G   전투력 " + EquippedPowerTotal(player), 18, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 22);
        AddBar(hudStats, player.hp, MaxHp(), dangerColor, "HP");
        AddBar(hudStats, player.mp, MaxMp(), manaColor, "MP");
        AddBar(hudStats, player.xp, XpToNext(), goodColor, "XP");

        var topActions = AddRow("Town Quick Actions", hud, 8, TextAnchor.MiddleRight);
        AddLayoutSize(topActions, -1, 70);
        var saveButton = AddObjectActionButton(topActions, "저장", () =>
        {
            SaveGame();
            ShowTown("슬롯 " + (activeSlot + 1) + "에 저장했습니다.");
        }, neonPurple, VisualActionRole.Journal, "save", 180, 66);
        var restButton = AddObjectActionButton(topActions, "여관 25 G", Rest, manaColor, VisualActionRole.Inn, "inn", 190, 66);
        var guideButton = AddObjectActionButton(topActions, "방법", () => ShowHowToPlay(false), goodColor, VisualActionRole.Guide, "guide", 170, 66);
        var menuButton = AddObjectActionButton(topActions, "메뉴", () =>
        {
            SaveGame();
            ShowMainMenu();
        }, panelAltColor, VisualActionRole.Menu, "menu", 170, 66);

        var body = AddRow("Town Command Deck", page, 18, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 842);

        var heroShowcase = AddPanel("Hero Showcase", body, Color.clear);
        AddLayoutSize(heroShowcase, 430, -1);
        AddVertical(heroShowcase, 8, TextAnchor.UpperCenter, new RectOffset(10, 10, 12, 12));
        AddReadabilityTextBlock(heroShowcase, "Hero Caption", townCharacterState == "rest" ? "회복 완료" : player.heroName + " · " + player.heroClass, 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 48, Rgb(9, 25, 47));
        AddGroundingShadow(heroShowcase, 340, 48, new Vector2(0f, 172f), 0.22f);
        AddCharacterArt(heroShowcase, player.portraitName, townCharacterState, 400, 650);
        AddReadabilityTextBlock(heroShowcase, "Hero Combat Style", "HP " + player.hp + "/" + MaxHp() + "   MP " + player.mp + "/" + MaxMp() + "\n" + (BasicAttackUsesMagic() ? "마법 공격형" : "물리 공격형"), 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 68, Rgb(9, 25, 47));

        var commandBoard = AddPanel("Adventure Command Board", body, Color.clear);
        AddLayoutSize(commandBoard, 880, -1);
        AddVertical(commandBoard, 10, TextAnchor.UpperCenter, new RectOffset(10, 10, 10, 10));
        AddReadabilityTextBlock(commandBoard, "Town Command Header", "아에테리아 원정본부\n던전 탐험 → 전투 → 장비 정비 → 더 강한 던전", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 84, Rgb(9, 25, 47));
        AddReadabilityTextBlock(commandBoard, "Town Message", string.IsNullOrEmpty(message) ? "다음 행동을 선택하세요." : message, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 56, Rgb(9, 25, 47));

        var actionGrid = AddPanel("Town Action Grid", commandBoard, new Color(0, 0, 0, 0));
        AddLayoutSize(actionGrid, -1, 500);
        var actionLayout = actionGrid.gameObject.AddComponent<GridLayoutGroup>();
        actionLayout.cellSize = new Vector2(420, 156);
        actionLayout.spacing = new Vector2(12, 12);
        actionLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        actionLayout.constraintCount = 2;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;

        AddObjectActionButton(actionGrid, "던전 탐험\n추천 원정지 선택", EnterDungeonSelectFromTown, goodColor, VisualActionRole.Portal, "portal", 420, 156);
        AddObjectActionButton(actionGrid, "가방 · 장비\n전투 세팅", () => ShowInventory("가방과 착용 장비를 정비합니다."), panelAltColor, VisualActionRole.Satchel, "satchel", 420, 156);
        AddObjectActionButton(actionGrid, "대장간\n장비 강화", () => ShowEnhancement("착용 장비를 부위별로 강화합니다."), goldColor, VisualActionRole.Anvil, "anvil", 420, 156);
        AddObjectActionButton(actionGrid, "스킬 수련\n기술 강화", () => ShowSkillTraining("골드로 직업 스킬을 강화합니다."), manaColor, VisualActionRole.Tome, "skill_tome", 420, 156);
        AddObjectActionButton(actionGrid, "장비 조합\n상위 등급 제작", () => ShowCrafting("같은 등급 장비 3개를 다음 등급으로 조합합니다."), neonPurple, VisualActionRole.Alchemy, "alchemy", 420, 156);
        AddObjectActionButton(actionGrid, "여관 휴식\nHP · MP 회복", Rest, dangerColor, VisualActionRole.Inn, "inn", 420, 156);

        AddReadabilityTextBlock(commandBoard, "Town Compact Stats", StatLine() + "\n가방 " + InventoryCountLabel() + "   최근 기록 " + player.generalLogs.Count, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 96, Rgb(9, 25, 47));

        var fieldJournal = AddReadabilityPlate(body, "Field Journal", Rgb(9, 25, 47));
        AddLayoutSize(fieldJournal, -1, -1);
        AddVertical(fieldJournal, 12, TextAnchor.UpperLeft, new RectOffset(20, 20, 20, 20));
        AddText(fieldJournal, "원정 기록", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 42);
        AddDivider(fieldJournal, Rgba(111, 211, 255, 135));
        AddText(fieldJournal, "진행도\n해금 던전 " + unlockedDungeonLabel + "\n보유 골드 " + player.gold + " G\n장비 전투력 " + EquippedPowerTotal(player), 19, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 120);
        AddDivider(fieldJournal, Rgba(111, 211, 255, 95));
        AddText(fieldJournal, "착용 장비", 22, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 34);
        AddText(fieldJournal,
            EquipmentLine("무기", player.weapon) + "\n"
            + EquipmentLine("방어구", player.armor) + "\n"
            + EquipmentLine("장신구 1", player.charm) + "\n"
            + EquipmentLine("장신구 2", player.charm2) + "\n"
            + EquipmentLine("장신구 3", player.charm3) + "\n"
            + EquipmentLine("장신구 4", player.charm4),
            18, FontStyle.Bold, textColor, TextAnchor.UpperLeft, 190);
        AddDivider(fieldJournal, Rgba(111, 211, 255, 95));
        AddText(fieldJournal, "최근 소식", 22, FontStyle.Bold, neonPurple, TextAnchor.MiddleLeft, 34);
        AddText(fieldJournal, RecentLogText(), 18, FontStyle.Bold, Color.white, TextAnchor.UpperLeft, 190);
        ApplyVisualRefreshToScreen(page);
    }

    private void ShowTownLegacy(string message)
    {
        currentScreen = AetheriaScreen.Town;
        ResetCombatState(true);
        selectedInventoryIndex = -1;
        ClearRoot();

        var page = AddPanel("Town", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 16, TextAnchor.UpperCenter, new RectOffset(32, 32, 24, 24));

        var hud = AddPanel("Town HUD", page, panelColor);
        AddLayoutSize(hud, -1, 190);
        var hudLayout = hud.gameObject.AddComponent<HorizontalLayoutGroup>();
        hudLayout.spacing = 18;
        hudLayout.padding = new RectOffset(18, 18, 16, 16);
        hudLayout.childAlignment = TextAnchor.MiddleLeft;
        hudLayout.childControlWidth = true;
        hudLayout.childControlHeight = true;
        hudLayout.childForceExpandWidth = false;
        hudLayout.childForceExpandHeight = true;

        var portraitPanel = AddPanel("HUD Avatar", hud, panelColor);
        AddLayoutSize(portraitPanel, 118, 118);
        AddPortrait(portraitPanel, player.portraitName, 112);

        var hudStats = AddPanel("HUD Stats", hud, new Color(0, 0, 0, 0));
        AddLayoutSize(hudStats, 620, -1);
        AddVertical(hudStats, 5, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
        AddText(hudStats, "Lv." + player.level + "  " + player.heroName + " / " + player.heroClass, 30, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 38);
        var dungeonTotal = Dungeons().Count;
        var unlockedDungeonLabel = dungeonTotal > 0 ? Mathf.Clamp(player.stage, 1, dungeonTotal) + "/" + dungeonTotal : player.stage.ToString();
        AddText(hudStats, "해금 던전 " + unlockedDungeonLabel + "   골드 " + player.gold + " G", 20, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 28);
        AddBar(hudStats, player.hp, MaxHp(), dangerColor, "HP");
        AddBar(hudStats, player.mp, MaxMp(), manaColor, "MP");
        AddBar(hudStats, player.xp, XpToNext(), goodColor, "XP");

        var topActions = AddPanel("Top Actions", hud, new Color(0, 0, 0, 0));
        AddLayoutSize(topActions, -1, -1);
        var topGrid = topActions.gameObject.AddComponent<GridLayoutGroup>();
        topGrid.cellSize = new Vector2(190, 50);
        topGrid.spacing = new Vector2(10, 10);
        topGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        topGrid.constraintCount = 3;
        topGrid.childAlignment = TextAnchor.MiddleRight;
        AddButton(topActions, "마을 저장", () =>
        {
            SaveGame();
            ShowTown("슬롯 " + (activeSlot + 1) + "에 저장했습니다.");
        }, neonPurple);
        AddButton(topActions, "여관 휴식 (25 G)", Rest, manaColor);
        AddButton(topActions, "메인 메뉴", () =>
        {
            SaveGame();
            ShowMainMenu();
        }, panelAltColor);

        var body = AddRow("Town Content", page, 16, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 780);

        var sidebar = AddPanel("Town Sidebar", body, panelAltColor);
        AddLayoutSize(sidebar, 260, -1);
        AddVertical(sidebar, 12, TextAnchor.UpperCenter, new RectOffset(14, 14, 18, 18));
        AddText(sidebar, "AETHERIA", 25, FontStyle.Bold, manaColor, TextAnchor.MiddleCenter, 42);
        AddButton(sidebar, "영웅 정보", () => ShowTown("영웅 정보를 확인했습니다."), neonPurple);
        AddButton(sidebar, "장비", () => ShowInventory("장비와 가방을 열었습니다."), panelAltColor);
        AddButton(sidebar, "스킬 강화", () => ShowSkillTraining("스킬을 강화해 위력과 상태이상 확률을 올립니다."), manaColor);
        AddButton(sidebar, "차원 포탈", () =>
        {
            EnterDungeonSelectFromTown();
        }, goodColor);
        AddButton(sidebar, "아이템 조합", () => ShowCrafting("같은 등급 장비 3개를 다음 등급 장비로 조합합니다."), panelAltColor);
        AddButton(sidebar, "심연의 대장간", () => ShowEnhancement("착용 중인 장비를 부위별로 강화하세요."), goldColor);

        var content = AddPanel("Town Panel Content", body, panelColor);
        AddLayoutSize(content, -1, -1);
        AddVertical(content, 14, TextAnchor.UpperLeft, new RectOffset(22, 22, 20, 20));
        AddText(content, "아에테리아 마을", 34, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 46);
        AddText(content, "마을에서 영웅 정보, 착용 장비, 스킬, 가방을 정비하고 차원 포탈로 진입합니다.", 20, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 34);

        var statusAndGear = AddRow("Status And Gear", content, 14, TextAnchor.UpperCenter);
        AddLayoutSize(statusAndGear, -1, 350);

        var stats = AddPanel("Hero Stats Card", statusAndGear, panelColor);
        AddLayoutSize(stats, 520, -1);
        AddVertical(stats, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));
        AddText(stats, "영웅 능력치", 25, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 34);
        AddText(stats, StatLine(), 19, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 52);
        AddText(stats, "체력 " + player.hp + "/" + MaxHp() + "\n마나 " + player.mp + "/" + MaxMp() + "\n기본 공격 타입 " + (BasicAttackUsesMagic() ? "마법" : "물리") + "\n상태 피해는 지속 피해와 마나 연소량에 반영됩니다.", 20, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 160);

        var equipment = AddPanel("Equipment Card", statusAndGear, Rgba(244, 237, 251, 248));
        AddLayoutSize(equipment, -1, -1);
        AddVertical(equipment, 7, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));
        AddText(equipment, "착용 중인 장비", 25, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 34);
        AddText(equipment, EquipmentLine("무기", player.weapon), 19, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 30);
        AddText(equipment, EquipmentLine("방어구", player.armor), 19, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 30);
        AddText(equipment, EquipmentLine("액세서리 1", player.charm), 18, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 29);
        AddText(equipment, EquipmentLine("액세서리 2", player.charm2), 18, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 29);
        AddText(equipment, EquipmentLine("액세서리 3", player.charm3), 18, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 29);
        AddText(equipment, EquipmentLine("액세서리 4", player.charm4), 18, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 29);
        AddButton(equipment, "부위별 강화", () => ShowEnhancement("착용 중인 장비를 부위별로 강화하세요."), goldColor);

        var lower = AddRow("Lower Town Panels", content, 14, TextAnchor.UpperCenter);
        AddLayoutSize(lower, -1, 265);

        var skills = AddPanel("Skill Section", lower, panelColor);
        AddLayoutSize(skills, 560, -1);
        AddVertical(skills, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));
        AddText(skills, "스킬 강화", 24, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 34);
        foreach (var skill in ScaledSkillsForPlayer())
        {
            AddText(skills, skill.name + "  MP " + skill.mpCost + "  위력 " + RoundToGameInt(skill.multiplier * 100) + "%", 18, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 27);
        }

        var log = AddPanel("Town Event Log", lower, panelAltColor);
        AddLayoutSize(log, -1, -1);
        AddVertical(log, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));
        AddText(log, "영웅 소식", 24, FontStyle.Bold, neonPurple, TextAnchor.MiddleLeft, 34);
        AddGeneralLog(message);
        SaveGame();
        AddText(log, message + "\n\n" + RecentLogText() + "\n\n" + BuildStatusText(), 18, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 200);
    }

    private void ResetCombatState(bool clearDungeon)
    {
        CancelCombatPresentation();
        actionLocked = false;
        combatPresentationPhase = CombatPresentationPhase.PlayerChoice;
        currentEnemy = null;
        enemyManaRegenTurnCounter = 0;
        playerStatusEffects.Clear();
        enemyStatusEffects.Clear();
        if (!clearDungeon)
        {
            return;
        }

        currentDungeon = null;
        currentDungeonFloor = 0;
        currentEnemyIsBoss = false;
    }

    private void ShowInventory(string message)
    {
        currentScreen = AetheriaScreen.Inventory;
        EnsurePlayerData();
        ClampSelectedInventoryIndex();
        SaveGame();
        ClearRoot();

        var page = AddPanel("Inventory", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 14, TextAnchor.UpperCenter, new RectOffset(38, 38, 26, 26));

        var top = AddRow("Inventory Top", page, 14, TextAnchor.MiddleLeft);
        AddLayoutSize(top, -1, 72);
        AddText(top, "원정 가방", 40, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 310);
        AddText(top, "보유 " + InventoryCountLabel() + "   ·   " + player.gold + " G", 22, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 460);
        AddButton(top, "마을로", () => ShowTown("마을로 돌아왔습니다."), panelAltColor);

        var sortRow = AddRow("Inventory Sort Row", page, 8, TextAnchor.MiddleLeft);
        AddLayoutSize(sortRow, -1, 50);
        AddText(sortRow, "장비 정렬", 19, FontStyle.Bold, mutedColor, TextAnchor.MiddleLeft, 110);
        AddInventorySortButton(sortRow, "전투력", InventorySortMode.Power);
        AddInventorySortButton(sortRow, "희귀도", InventorySortMode.Rarity);
        AddInventorySortButton(sortRow, "부위", InventorySortMode.Type);

        var body = AddRow("Inventory Body", page, 16, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 818);
        body.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var listColumn = AddPanel("Inventory List Column", body, Color.clear);
        AddLayoutSize(listColumn, 650, -1);
        AddVertical(listColumn, 8, TextAnchor.UpperLeft, new RectOffset(0, 0, 0, 0));
        AddText(listColumn, "가방 장비", 28, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 40);
        var list = AddScrollList("Item List", listColumn, Color.clear, -1, -1, 8, new RectOffset(12, 16, 10, 12));

        if (player.inventory.Count == 0)
        {
            AddText(list, "가방이 비어 있습니다.\n던전을 탐험해 장비를 획득하세요.", 22, FontStyle.Bold, mutedColor, TextAnchor.MiddleCenter, 120);
        }
        else
        {
            var orderedItems = SortedInventoryEntries();
            for (var i = 0; i < orderedItems.Count; i++)
            {
                AddInventoryItemRow(list, orderedItems[i].index, orderedItems[i].item);
            }
        }

        var detailColumn = AddPanel("Inventory Comparison Column", body, Color.clear);
        AddLayoutSize(detailColumn, 690, -1);
        AddVertical(detailColumn, 8, TextAnchor.UpperLeft, new RectOffset(0, 0, 0, 0));
        AddText(detailColumn, "선택 장비 비교", 28, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 40);
        var detail = AddScrollList("Item Detail", detailColumn, Color.clear, -1, -1, 10, new RectOffset(12, 16, 10, 12));
        AddMessageBanner(detail, message, Rgb(178, 148, 102), 52);

        ItemState selectedItem = null;
        if (selectedInventoryIndex >= 0 && selectedInventoryIndex < player.inventory.Count)
        {
            selectedItem = player.inventory[selectedInventoryIndex];
            AddSelectedInventoryComparison(detail, selectedItem);
        }
        else
        {
            var prompt = AddPanel("Inventory Selection Read Plate", detail, Color.clear);
            AddLayoutSize(prompt, -1, 150);
            AddGrowthReadPlate(prompt, Rgb(245, 248, 250));
            AddVertical(prompt, 8, TextAnchor.MiddleCenter, new RectOffset(24, 24, 22, 22));
            AddText(prompt, "장비를 선택하세요", 26, FontStyle.Bold, manaColor, TextAnchor.MiddleCenter, 38);
            AddText(prompt, "종류 · 희귀도 · 전투력 배지를 확인한 뒤\n현재 장비와 변화를 비교할 수 있습니다.", 19, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 64);
        }

        var equipped = AddPanel("Equipped Six Slots", body, Color.clear);
        AddLayoutSize(equipped, 450, -1);
        AddVertical(equipped, 8, TextAnchor.UpperLeft, new RectOffset(0, 0, 0, 0));
        AddText(equipped, "장착 장비  6슬롯", 28, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 40);
        AddEquipmentSlotCard(equipped, "무기", "Weapon", player.weapon, selectedItem);
        AddEquipmentSlotCard(equipped, "방어구", "Armor", player.armor, selectedItem);
        AddEquipmentSlotCard(equipped, "장신구 1", "Charm1", player.charm, selectedItem);
        AddEquipmentSlotCard(equipped, "장신구 2", "Charm2", player.charm2, selectedItem);
        AddEquipmentSlotCard(equipped, "장신구 3", "Charm3", player.charm3, selectedItem);
        AddEquipmentSlotCard(equipped, "장신구 4", "Charm4", player.charm4, selectedItem);
    }

    private void AddInventoryItemRow(Transform parent, int index, ItemState item)
    {
        if (item == null)
        {
            return;
        }

        var row = AddPanel("Inventory Item Row", parent, Color.clear);
        AddLayoutSize(row, -1, 70);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.padding = new RectOffset(0, 0, 3, 3);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var enhanceLabel = item.level > 0 ? "  +" + item.level : "";
        var selectButton = AddButton(row, item.name + enhanceLabel, () =>
        {
            selectedInventoryIndex = index;
            ShowInventory(item.name + "을(를) 선택했습니다.");
        }, selectedInventoryIndex == index ? goldColor : panelAltColor);
        AddLayoutSize(selectButton.GetComponent<RectTransform>(), 278, 64);
        AddGrowthBadge(row, TypeLabel(item.type), Rgb(74, 112, 138), 78, 56);
        AddGrowthBadge(row, RarityLabel(item.rarity), RarityColor(item.rarity), 88, 56);
        AddGrowthBadge(row, "전투력\n+" + item.power, goldColor, 112, 56);
    }

    private void AddSelectedInventoryComparison(Transform parent, ItemState item)
    {
        if (item == null)
        {
            return;
        }

        var header = AddPanel("Selected Item Read Plate", parent, Color.clear);
        AddLayoutSize(header, -1, 112);
        AddGrowthReadPlate(header, Rgb(245, 248, 250));
        AddVertical(header, 6, TextAnchor.UpperLeft, new RectOffset(16, 16, 12, 12));
        AddText(header, item.name + (item.level > 0 ? "  +" + item.level : ""), 27, FontStyle.Bold, RarityColor(item.rarity), TextAnchor.MiddleLeft, 38);
        var badgeRow = AddRow("Selected Item Badges", header, 8, TextAnchor.MiddleLeft);
        AddLayoutSize(badgeRow, -1, 42);
        badgeRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        AddGrowthBadge(badgeRow, TypeLabel(item.type), Rgb(74, 112, 138), 110, 36);
        AddGrowthBadge(badgeRow, RarityLabel(item.rarity), RarityColor(item.rarity), 110, 36);
        AddGrowthBadge(badgeRow, "전투력 +" + item.power, goldColor, 150, 36);

        var comparisonTarget = EquippedItemForReplacement(item.type, item.type == "Charm" ? 1 : 0);
        var comparison = AddPanel("Power Comparison Read Plate", parent, Color.clear);
        AddLayoutSize(comparison, -1, 146);
        AddGrowthReadPlate(comparison, Rgb(245, 248, 250));
        AddVertical(comparison, 6, TextAnchor.UpperLeft, new RectOffset(16, 16, 12, 12));
        var currentPower = comparisonTarget != null ? comparisonTarget.power : 0;
        var powerDelta = item.power - currentPower;
        var arrowColor = powerDelta > 0 ? goodColor : powerDelta < 0 ? dangerColor : mutedColor;
        AddText(comparison, "전투력   +" + currentPower + "   →   +" + item.power + "   " + GrowthArrow(powerDelta), 25, FontStyle.Bold, arrowColor, TextAnchor.MiddleLeft, 40);
        AddText(comparison, GearStatsDescription(item), 18, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 64);

        AddColoredGearDeltaPanel(parent, item, comparisonTarget);

        if (item.type == "Charm")
        {
            AddText(parent, "장착할 장신구 슬롯", 20, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 30);
            var slotGrid = AddPanel("Accessory Equip Slot Grid", parent, Color.clear);
            AddLayoutSize(slotGrid, -1, 146);
            var layout = slotGrid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(300, 64);
            layout.spacing = new Vector2(8, 8);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.UpperCenter;
            AddButton(slotGrid, AccessorySlotButtonLabel(item, 1, player.charm), () => EquipItemToSlot(selectedInventoryIndex, 1), goodColor);
            AddButton(slotGrid, AccessorySlotButtonLabel(item, 2, player.charm2), () => EquipItemToSlot(selectedInventoryIndex, 2), goodColor);
            AddButton(slotGrid, AccessorySlotButtonLabel(item, 3, player.charm3), () => EquipItemToSlot(selectedInventoryIndex, 3), goodColor);
            AddButton(slotGrid, AccessorySlotButtonLabel(item, 4, player.charm4), () => EquipItemToSlot(selectedInventoryIndex, 4), goodColor);
        }
        else
        {
            var equipButton = AddButton(parent, "장착   " + GrowthArrow(item.power - currentPower), () => EquipItem(selectedInventoryIndex), goodColor);
            AddLayoutSize(equipButton.GetComponent<RectTransform>(), -1, 58);
        }

        var actionRow = AddRow("Selected Item Actions", parent, 10, TextAnchor.MiddleLeft);
        AddLayoutSize(actionRow, -1, 58);
        AddButton(actionRow, "대장간에서 강화", () => ShowEnhancement("착용 중인 장비를 선택해 강화할 수 있습니다."), goldColor);
        AddButton(actionRow, "판매  " + SellValue(item) + " G", () =>
        {
            var sellMessage = SellItem(selectedInventoryIndex);
            selectedInventoryIndex = -1;
            ShowInventory(sellMessage);
        }, dangerColor);
    }

    private void AddEquipmentSlotCard(Transform parent, string slotLabel, string slotKey, ItemState item, ItemState selectedItem)
    {
        var rarity = item != null ? RarityLabel(item.rarity) : "빈 슬롯";
        var itemName = item != null ? item.name + (item.level > 0 ? " +" + item.level : "") : "장비 없음";
        var power = item != null ? "전투력 +" + item.power : "가방에서 장착 가능";
        var comparison = GrowthEquipmentComparison(selectedItem, item, slotKey);
        var label = slotLabel + "  ·  " + rarity + "\n" + itemName + "\n" + power + comparison + (item != null ? "   ·   클릭하여 해제" : "");
        var button = AddButton(parent, label, () => UnequipItem(slotKey), item != null ? RarityColor(item.rarity) : mutedColor);
        AddLayoutSize(button.GetComponent<RectTransform>(), -1, 112);
        button.interactable = item != null;
    }

    private string GrowthEquipmentComparison(ItemState selectedItem, ItemState equippedItem, string slotKey)
    {
        if (selectedItem == null)
        {
            return "";
        }

        var slotType = slotKey == "Weapon" ? "Weapon" : slotKey == "Armor" ? "Armor" : "Charm";
        if (selectedItem.type != slotType)
        {
            return "";
        }

        var equippedPower = equippedItem != null ? equippedItem.power : 0;
        return "   " + GrowthArrow(selectedItem.power - equippedPower);
    }

    private string GrowthArrow(int delta)
    {
        if (delta > 0) return "▲ +" + delta;
        if (delta < 0) return "▼ " + delta;
        return "→ 0";
    }

    private void AddItemComparisonPanel(Transform parent, string title, string value, Color titleColor)
    {
        var panel = AddPanel("Item Comparison " + title, parent, panelAltColor);
        AddSideAccent(panel, titleColor);
        AddLayoutSize(panel, -1, -1);
        AddVertical(panel, 8, TextAnchor.UpperLeft, new RectOffset(16, 16, 14, 14));
        AddText(panel, title, 22, FontStyle.Bold, titleColor, TextAnchor.MiddleLeft, 34);
        AddText(panel, value, 18, FontStyle.Bold, textColor, TextAnchor.UpperLeft, 326);
    }

    private void AddInventorySortButton(Transform parent, string label, InventorySortMode mode)
    {
        var active = inventorySortMode == mode;
        var button = AddButton(parent, active ? label + " ✓" : label, () =>
        {
            inventorySortMode = mode;
            selectedInventoryIndex = -1;
            ShowInventory(label + " 기준으로 가방을 정렬했습니다.");
        }, active ? goldColor : panelAltColor);
        AddLayoutSize(button.GetComponent<RectTransform>(), 170, 50);
    }

    private List<InventoryEntry> SortedInventoryEntries()
    {
        var entries = new List<InventoryEntry>();
        for (var i = 0; i < player.inventory.Count; i++)
        {
            var item = player.inventory[i];
            if (item != null)
            {
                entries.Add(new InventoryEntry { index = i, item = item });
            }
        }

        entries.Sort(CompareInventoryEntries);
        return entries;
    }

    private int CompareInventoryEntries(InventoryEntry left, InventoryEntry right)
    {
        if (left.item == null && right.item == null) return 0;
        if (left.item == null) return 1;
        if (right.item == null) return -1;

        var result = 0;
        if (inventorySortMode == InventorySortMode.Rarity)
        {
            result = right.item.rarity.CompareTo(left.item.rarity);
            if (result == 0) result = right.item.power.CompareTo(left.item.power);
        }
        else if (inventorySortMode == InventorySortMode.Type)
        {
            result = GearTypeSortOrder(left.item.type).CompareTo(GearTypeSortOrder(right.item.type));
            if (result == 0) result = right.item.rarity.CompareTo(left.item.rarity);
            if (result == 0) result = right.item.power.CompareTo(left.item.power);
        }
        else
        {
            result = right.item.power.CompareTo(left.item.power);
            if (result == 0) result = right.item.rarity.CompareTo(left.item.rarity);
        }

        if (result == 0)
        {
            result = string.Compare(left.item.name, right.item.name, StringComparison.Ordinal);
        }

        return result != 0 ? result : left.index.CompareTo(right.index);
    }

    private int GearTypeSortOrder(string type)
    {
        if (type == "Weapon") return 0;
        if (type == "Armor") return 1;
        return 2;
    }

    private void AddColoredGearDeltaPanel(Transform parent, ItemState replacement, ItemState equipped)
    {
        if (replacement == null)
        {
            return;
        }

        var panel = AddPanel("Colored Gear Delta", parent, panelAltColor);
        AddLayoutSize(panel, -1, 82);
        var layout = panel.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(112, 30);
        layout.spacing = new Vector2(8, 6);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new RectOffset(14, 14, 7, 7);

        AddDeltaChip(panel, "전투력", replacement.power, equipped != null ? equipped.power : 0, false);
        AddDeltaChip(panel, "체력", replacement.maxHp, equipped != null ? equipped.maxHp : 0, false);
        AddDeltaChip(panel, "공격", replacement.attack, equipped != null ? equipped.attack : 0, false);
        AddDeltaChip(panel, "마력", replacement.magic, equipped != null ? equipped.magic : 0, false);
        AddDeltaChip(panel, "방어", replacement.defense, equipped != null ? equipped.defense : 0, false);
        AddDeltaChip(panel, "속도", replacement.speed, equipped != null ? equipped.speed : 0, false);
        AddDeltaChip(panel, "치명", replacement.critRate, equipped != null ? equipped.critRate : 0f, true);
        AddDeltaChip(panel, "회피", replacement.evasion, equipped != null ? equipped.evasion : 0f, true);
        AddDeltaChip(panel, "피감", replacement.damageReduction, equipped != null ? equipped.damageReduction : 0f, true);
        AddDeltaChip(panel, "발견", replacement.itemFind, equipped != null ? equipped.itemFind : 0f, true);
    }

    private void AddDeltaChip(Transform parent, string label, int replacement, int equipped, bool percent)
    {
        AddDeltaChip(parent, label, replacement - equipped, percent);
    }

    private void AddDeltaChip(Transform parent, string label, float replacement, float equipped, bool percent)
    {
        AddDeltaChip(parent, label, RoundToGameInt((replacement - equipped) * 100f), percent);
    }

    private void AddDeltaChip(Transform parent, string label, int delta, bool percent)
    {
        if (delta == 0)
        {
            return;
        }

        var chipColor = delta > 0 ? Rgba(6, 78, 59, 220) : Rgba(95, 21, 35, 220);
        var chip = AddPanel("Delta " + label, parent, chipColor);
        var chipTextColor = delta > 0 ? goodColor : dangerColor;
        AddSideAccent(chip, chipTextColor);
        AddText(chip, label + " " + SignedInt(delta) + (percent ? "%" : ""), 15, FontStyle.Bold, chipTextColor, TextAnchor.MiddleCenter, 28);
    }

    private void ShowEnhancement(string message)
    {
        currentScreen = AetheriaScreen.Enhancement;
        EnsurePlayerData();
        SaveGame();
        ClearRoot();

        var page = AddPanel("Enhancement", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 14, TextAnchor.UpperCenter, new RectOffset(38, 38, 26, 26));

        var top = AddRow("Enhancement Top", page, 14, TextAnchor.MiddleLeft);
        AddLayoutSize(top, -1, 72);
        AddText(top, "심연의 대장간", 40, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 370);
        AddText(top, "보유 골드  " + player.gold + " G   ·   실패해도 장비는 유지됩니다.", 21, FontStyle.Bold, mutedColor, TextAnchor.MiddleLeft, 650);
        AddButton(top, "마을로", () => ShowTown("강화소를 나왔습니다."), panelAltColor);

        AddMessageBanner(page, message, goldColor, 52);

        var body = AddRow("Enhancement Forge Body", page, 16, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 826);
        body.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var forgeStage = AddPanel("Forge Anvil Read Plate", body, Color.clear);
        AddLayoutSize(forgeStage, 430, -1);
        AddGrowthReadPlate(forgeStage, Rgb(244, 239, 224));
        AddVertical(forgeStage, 8, TextAnchor.UpperCenter, new RectOffset(22, 22, 20, 20));
        AddText(forgeStage, "강화할 장비를 고르세요", 27, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 42);
        AddText(forgeStage, "모루에 올린 장비의 전투력을\n한 단계 끌어올립니다.", 19, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 58);
        AddForgeAnvilGraphic(forgeStage);
        AddGrowthProgressBar(forgeStage, GearEnhancementSuccessChance, goodColor, "기본 성공률  " + RoundToGameInt(GearEnhancementSuccessChance * 100f) + "%", 34);
        AddText(forgeStage, "현재 장착한 6개 부위만 강화 가능\n강화 단계와 희귀도가 높을수록 비용 증가", 18, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 62);

        var grid = AddPanel("Enhancement Equipment Grid", body, Color.clear);
        AddLayoutSize(grid, -1, -1);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(650, 252);
        layout.spacing = new Vector2(16, 16);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;
        layout.childAlignment = TextAnchor.UpperCenter;

        AddEnhancementCard(grid, "무기", player.weapon, "공격 성장");
        AddEnhancementCard(grid, "방어구", player.armor, "체력 · 방어 성장");
        AddEnhancementCard(grid, "장신구 1", player.charm, "마나 · 마력 성장");
        AddEnhancementCard(grid, "장신구 2", player.charm2, "마나 · 마력 성장");
        AddEnhancementCard(grid, "장신구 3", player.charm3, "마나 · 마력 성장");
        AddEnhancementCard(grid, "장신구 4", player.charm4, "마나 · 마력 성장");
    }

    private void ShowSkillTraining(string message)
    {
        currentScreen = AetheriaScreen.SkillTraining;
        EnsurePlayerData();
        SaveGame();
        ClearRoot();

        var page = AddPanel("Skill Training", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 14, TextAnchor.UpperCenter, new RectOffset(38, 38, 24, 24));

        var accent = ActiveCharacterAccent(manaColor);
        var top = AddRow("Skill Training Top", page, 14, TextAnchor.MiddleLeft);
        AddLayoutSize(top, -1, 72);
        AddText(top, player.heroClass + "  전용 스킬", 38, FontStyle.Bold, accent, TextAnchor.MiddleLeft, 520);
        AddText(top, "보유 골드  " + player.gold + " G", 22, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 320);
        AddButton(top, "마을로", () => ShowTown("훈련장을 나왔습니다."), panelAltColor);

        AddMessageBanner(page, message, accent, 52);

        var body = AddRow("Skill Training Body", page, 18, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 830);
        body.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var heroStage = AddPanel("Skill Character Read Plate", body, Color.clear);
        AddLayoutSize(heroStage, 390, -1);
        AddGrowthReadPlate(heroStage, Rgb(239, 246, 250));
        AddVertical(heroStage, 8, TextAnchor.UpperCenter, new RectOffset(18, 18, 18, 18));
        AddText(heroStage, "기술 수련", 27, FontStyle.Bold, accent, TextAnchor.MiddleCenter, 38);
        AddText(heroStage, player.heroName + " · " + player.heroClass, 19, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 30);
        AddCharacterArt(heroStage, player.portraitName, "skill", 340, 520);
        AddText(heroStage, "각 기술은 최대 Lv." + MaxSkillLevel + "\n현재 효과와 다음 효과를 비교하세요.", 18, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 58);

        var grid = AddPanel("Fixed Skill Card Grid", body, Color.clear);
        AddLayoutSize(grid, -1, -1);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(345, 730);
        layout.spacing = new Vector2(12, 12);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.childAlignment = TextAnchor.MiddleCenter;

        foreach (var baseSkill in SkillsForClass(player.heroClass))
        {
            AddSkillTrainingCard(grid, baseSkill);
        }
    }

    private void AddSkillTrainingCard(Transform parent, SkillState baseSkill)
    {
        var level = SkillLevel(baseSkill.name);
        var skill = ScaleSkill(baseSkill, level);
        var capped = level >= MaxSkillLevel;
        var nextSkill = ScaleSkill(baseSkill, capped ? level : level + 1);
        var cost = SkillUpgradeCost(level);
        var accent = ActiveCharacterAccent(manaColor);
        var card = AddPanel("Fixed Skill Card " + baseSkill.name, parent, Color.clear);
        AddGrowthReadPlate(card, Rgb(238, 245, 249));
        AddSideAccent(card, accent);
        AddVertical(card, 6, TextAnchor.UpperLeft, new RectOffset(14, 14, 14, 14));

        var skillHeader = AddPanel("Skill Card Icon Header", card, Color.clear);
        AddLayoutSize(skillHeader, -1, 108);
        var skillHeaderLayout = skillHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
        skillHeaderLayout.spacing = 10;
        skillHeaderLayout.padding = new RectOffset(0, 0, 4, 4);
        skillHeaderLayout.childAlignment = TextAnchor.MiddleLeft;
        skillHeaderLayout.childControlWidth = true;
        skillHeaderLayout.childControlHeight = true;
        skillHeaderLayout.childForceExpandWidth = false;
        skillHeaderLayout.childForceExpandHeight = false;
        AddSkillIconVisual(skillHeader, baseSkill.name, 96, 96);

        var skillHeaderCopy = AddPanel("Skill Card Header Copy", skillHeader, Color.clear);
        AddLayoutSize(skillHeaderCopy, -1, 96);
        AddVertical(skillHeaderCopy, 4, TextAnchor.MiddleLeft, new RectOffset(0, 0, 4, 4));
        AddText(skillHeaderCopy, baseSkill.name, 24, FontStyle.Bold, accent, TextAnchor.MiddleLeft, 42);
        var badgeRow = AddRow("Skill Card Badges", skillHeaderCopy, 8, TextAnchor.MiddleLeft);
        AddLayoutSize(badgeRow, -1, 38);
        badgeRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        AddGrowthBadge(badgeRow, "Lv." + level, accent, 86, 36);
        AddGrowthBadge(badgeRow, skill.magic ? "마력형" : "공격형", skill.magic ? manaColor : goodColor, 94, 36);

        AddGrowthProgressBar(card, (float)level / MaxSkillLevel, accent, "수련 단계  " + level + " / " + MaxSkillLevel, 28);
        AddText(card, "소모 MP  " + skill.mpCost, 18, FontStyle.Bold, mutedColor, TextAnchor.MiddleLeft, 26);

        var powerPreview = AddPanel("Skill Power Preview Read Plate", card, Color.clear);
        AddLayoutSize(powerPreview, -1, 80);
        AddGrowthReadPlate(powerPreview, Rgb(250, 250, 246));
        AddVertical(powerPreview, 2, TextAnchor.MiddleCenter, new RectOffset(8, 8, 6, 6));
        AddText(powerPreview, "현재 위력  " + RoundToGameInt(skill.multiplier * 100) + "%", 20, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 28);
        AddText(powerPreview, capped ? "최대 단계 도달" : "다음 위력  →  " + RoundToGameInt(nextSkill.multiplier * 100) + "%", 18, FontStyle.Bold, capped ? goldColor : goodColor, TextAnchor.MiddleCenter, 28);

        AddText(card, "현재 효과", 18, FontStyle.Bold, accent, TextAnchor.MiddleLeft, 26);
        AddText(card, SkillEffectLine(skill), 18, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 88);
        AddText(card, "다음 효과", 18, FontStyle.Bold, capped ? goldColor : goodColor, TextAnchor.MiddleLeft, 26);
        AddText(card, capped ? "현재 효과가 최종 단계입니다." : SkillEffectLine(nextSkill), 18, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 88);
        AddText(card, capped ? "더 이상 강화할 수 없습니다." : SkillTrainingEffectLine(skill, nextSkill), 17, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 100);

        var button = AddButton(card, capped ? "최대 레벨" : "강화   " + cost + " G", () => UpgradeSkill(baseSkill.name), goldColor);
        AddLayoutSize(button.GetComponent<RectTransform>(), -1, 56);
        button.interactable = !capped && player.gold >= cost;
    }

    private void ShowCrafting(string message)
    {
        currentScreen = AetheriaScreen.Crafting;
        EnsurePlayerData();
        SaveGame();
        ClearRoot();

        var page = AddPanel("Crafting", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 14, TextAnchor.UpperCenter, new RectOffset(38, 38, 24, 24));

        var top = AddRow("Crafting Top", page, 14, TextAnchor.MiddleLeft);
        AddLayoutSize(top, -1, 72);
        AddText(top, "연금 조합진", 40, FontStyle.Bold, neonPurple, TextAnchor.MiddleLeft, 350);
        AddText(top, "보유 골드  " + player.gold + " G   ·   같은 등급 장비 3개를 상위 등급 1개로", 21, FontStyle.Bold, mutedColor, TextAnchor.MiddleLeft, 760);
        AddButton(top, "마을로", () => ShowTown("조합소를 나왔습니다."), panelAltColor);

        AddMessageBanner(page, message, neonPurple, 52);

        var body = AddRow("Crafting Body", page, 16, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 830);
        body.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var ingredientColumn = AddPanel("Crafting Ingredient Column", body, Color.clear);
        AddLayoutSize(ingredientColumn, 420, -1);
        AddVertical(ingredientColumn, 8, TextAnchor.UpperLeft, new RectOffset(0, 0, 0, 0));
        AddText(ingredientColumn, "조합 가능 재료", 28, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 40);
        var ingredientList = AddScrollList("Crafting Ingredient Summary", ingredientColumn, Color.clear, -1, -1, 7, new RectOffset(12, 16, 10, 12));
        AddText(ingredientList, "네임드 장비는 재료에서 제외됩니다.", 18, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 38);
        for (var rarity = 0; rarity <= MaxRecipeIngredientRarity; rarity++)
        {
            AddCraftingIngredientSummary(ingredientList, rarity, CountCraftIngredients(rarity));
        }

        var recipeColumn = AddPanel("Crafting Recipe Column", body, Color.clear);
        AddLayoutSize(recipeColumn, -1, -1);
        AddVertical(recipeColumn, 8, TextAnchor.UpperLeft, new RectOffset(0, 0, 0, 0));
        AddText(recipeColumn, "재료 3개   →   합성진   →   결과 장비", 28, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 40);
        var recipeList = AddScrollList("Crafting Recipe Flow", recipeColumn, Color.clear, -1, -1, 10, new RectOffset(12, 18, 10, 12));
        for (var rarity = 0; rarity <= MaxRecipeIngredientRarity; rarity++)
        {
            AddCraftingRecipeFlow(recipeList, rarity);
        }
    }

    private void AddEnhancementCard(Transform parent, string slotLabel, ItemState item, string role)
    {
        var accent = item == null ? mutedColor : RarityColor(item.rarity);
        var card = AddPanel("Enhancement Card " + slotLabel, parent, Color.clear);
        AddGrowthReadPlate(card, Rgb(246, 243, 234));
        AddSideAccent(card, accent);
        AddVertical(card, 3, TextAnchor.UpperLeft, new RectOffset(10, 10, 10, 10));

        var heading = AddRow("Enhancement Card Heading", card, 8, TextAnchor.MiddleLeft);
        AddLayoutSize(heading, -1, 32);
        heading.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        AddText(heading, slotLabel, 22, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 30);
        AddGrowthBadge(heading, item == null ? "빈 슬롯" : RarityLabel(item.rarity), accent, 104, 28);

        var cost = item == null ? 0 : EnhancementCost(item);
        var nextPower = item == null ? 0 : PreviewEnhancedPower(item);
        AddText(card, item == null ? "장착된 장비 없음" : item.name + (item.level > 0 ? "  +" + item.level : ""), 19, FontStyle.Bold, accent, TextAnchor.MiddleLeft, 28);

        if (item == null)
        {
            AddText(card, role + "\n가방에서 이 부위 장비를 먼저 착용하세요.", 18, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 76);
        }
        else
        {
            var capped = item.level >= MaxGearEnhancementLevel;
            AddText(card, capped ? "전투력  +" + item.power + "   ·   최대 강화" : "전투력  +" + item.power + "   →   +" + nextPower + "   ▲ +" + (nextPower - item.power), 21, FontStyle.Bold, capped ? goldColor : goodColor, TextAnchor.MiddleLeft, 34);
            AddText(card, role + "   ·   " + CompactGearStatsDescription(item), 17, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 32);
            AddGrowthProgressBar(card, GearEnhancementSuccessChance, goodColor, "성공률 " + RoundToGameInt(GearEnhancementSuccessChance * 100f) + "%   ·   실패 시 유지", 26);
        }

        var button = AddButton(card, item != null && item.level >= MaxGearEnhancementLevel ? "최대 강화" : item == null ? "장비를 먼저 장착" : "강화   " + cost + " G", () => EnhanceEquippedItem(slotLabel, item), goldColor);
        AddLayoutSize(button.GetComponent<RectTransform>(), -1, 48);
        button.interactable = item != null && item.level < MaxGearEnhancementLevel && player.gold >= cost;
    }

    private void AddCraftingIngredientSummary(Transform parent, int rarity, int count)
    {
        var row = AddPanel("Crafting Ingredient " + rarity, parent, Color.clear);
        AddLayoutSize(row, -1, 52);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        AddGrowthBadge(row, RarityLabel(rarity), RarityColor(rarity), 170, 44);
        AddGrowthBadge(row, count + "개 보유", count >= 3 ? goodColor : mutedColor, 150, 44);
    }

    private void AddCraftingRecipeFlow(Transform parent, int rarity)
    {
        var count = CountCraftIngredients(rarity);
        var cost = RecipeCraftCost(rarity);
        var resultRarity = rarity + 1;
        var card = AddPanel("Crafting Recipe " + rarity, parent, Color.clear);
        AddLayoutSize(card, -1, 154);
        AddGrowthReadPlate(card, Rgb(242, 239, 248));
        AddVertical(card, 5, TextAnchor.UpperLeft, new RectOffset(12, 12, 9, 9));
        AddText(card, RarityLabel(rarity) + " 조합   ·   보유 " + count + "/3", 20, FontStyle.Bold, RarityColor(rarity), TextAnchor.MiddleLeft, 30);

        var flow = AddPanel("Crafting Material Flow", card, Color.clear);
        AddLayoutSize(flow, -1, 96);
        var layout = flow.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 7;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        AddGrowthBadge(flow, "재료 1\n" + RarityLabel(rarity), RarityColor(rarity), 108, 74);
        AddGrowthFlowText(flow, "+", 28);
        AddGrowthBadge(flow, "재료 2\n" + RarityLabel(rarity), RarityColor(rarity), 108, 74);
        AddGrowthFlowText(flow, "+", 28);
        AddGrowthBadge(flow, "재료 3\n" + RarityLabel(rarity), RarityColor(rarity), 108, 74);
        AddGrowthFlowText(flow, "→", 34);
        AddGrowthBadge(flow, "합성진\n" + cost + " G", neonPurple, 132, 82);
        AddGrowthFlowText(flow, "→", 34);
        AddGrowthBadge(flow, "결과\n" + RarityLabel(resultRarity), RarityColor(resultRarity), 142, 82);
        var button = AddButton(flow, "조합 실행\n" + count + "/3", () => CraftByRecipe(rarity), RarityColor(resultRarity));
        AddLayoutSize(button.GetComponent<RectTransform>(), 180, 76);
        button.interactable = count >= 3 && player.gold >= cost;
    }

    private void AddForgeAnvilGraphic(Transform parent)
    {
        var art = AddPanel("Forge Anvil Graphic", parent, Color.clear);
        AddLayoutSize(art, 360, 390);
        var generatedAnvil = LoadRuntimeObjectSprite("UI/VisualRefresh/Objects/anvil");
        if (generatedAnvil != null)
        {
            AddGroundingShadow(art, 270f, 52f, new Vector2(0f, -145f), 0.22f);
            var visual = AddFlatPanel("Generated Anvil Artwork", art, Color.white);
            visual.anchorMin = new Vector2(0.08f, 0.08f);
            visual.anchorMax = new Vector2(0.92f, 0.96f);
            visual.offsetMin = Vector2.zero;
            visual.offsetMax = Vector2.zero;
            visual.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var image = visual.GetComponent<Image>();
            image.sprite = generatedAnvil;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return;
        }

        AddForgeAnvilPart(art, "Anvil Top", new Vector2(0f, 74f), new Vector2(282f, 58f), Rgb(80, 96, 108));
        AddForgeAnvilPart(art, "Anvil Horn", new Vector2(-158f, 72f), new Vector2(78f, 36f), Rgb(98, 111, 121));
        AddForgeAnvilPart(art, "Anvil Highlight", new Vector2(18f, 101f), new Vector2(240f, 8f), goldColor);
        AddForgeAnvilPart(art, "Anvil Neck", new Vector2(0f, 10f), new Vector2(122f, 92f), Rgb(65, 78, 89));
        AddForgeAnvilPart(art, "Anvil Foot", new Vector2(0f, -70f), new Vector2(238f, 54f), Rgb(53, 65, 75));
        AddForgeAnvilPart(art, "Forge Spark Left", new Vector2(-112f, 146f), new Vector2(12f, 28f), goldColor);
        AddForgeAnvilPart(art, "Forge Spark Center", new Vector2(-48f, 172f), new Vector2(10f, 34f), goldColor);
        AddForgeAnvilPart(art, "Forge Spark Right", new Vector2(92f, 154f), new Vector2(14f, 24f), goldColor);
        var label = AddText(art, "모  루", 24, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 42);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 8f);
        labelRect.sizeDelta = new Vector2(0f, 42f);
        labelRect.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    private void AddForgeAnvilPart(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var part = AddFlatPanel(name, parent, color);
        part.anchorMin = new Vector2(0.5f, 0.5f);
        part.anchorMax = new Vector2(0.5f, 0.5f);
        part.pivot = new Vector2(0.5f, 0.5f);
        part.anchoredPosition = position;
        part.sizeDelta = size;
        part.GetComponent<Image>().raycastTarget = false;
        part.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private void AddGrowthReadPlate(RectTransform target, Color tint)
    {
        if (target == null)
        {
            return;
        }

        var plate = AddFlatPanel(target.name + " Backplate", target, new Color(tint.r, tint.g, tint.b, 0.30f));
        plate.SetAsFirstSibling();
        Stretch(plate, 0, 0, 0, 0);
        plate.GetComponent<Image>().raycastTarget = false;
        plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private RectTransform AddGrowthBadge(Transform parent, string label, Color accent, float width, float height)
    {
        var surface = Color.Lerp(Rgb(247, 250, 252), new Color(accent.r, accent.g, accent.b, 1f), 0.28f);
        surface.a = 0.30f;
        var badge = AddFlatPanel("Growth Badge " + label, parent, surface);
        badge.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(badge, width, height);
        var text = AddText(badge, label, 18, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, height);
        Stretch(text.GetComponent<RectTransform>(), 6, 3, 6, 3);
        return badge;
    }

    private void AddGrowthProgressBar(Transform parent, float ratio, Color accent, string label, float height)
    {
        var frame = AddFlatPanel("Growth Progress", parent, new Color(0.08f, 0.11f, 0.14f, 0.30f));
        frame.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(frame, -1, height);
        var fill = AddFlatPanel("Growth Progress Fill", frame, new Color(accent.r, accent.g, accent.b, 0.88f));
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().raycastTarget = false;
        var text = AddText(frame, label, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, height);
        Stretch(text.GetComponent<RectTransform>(), 4, 2, 4, 2);
    }

    private void AddGrowthFlowText(Transform parent, string value, float width)
    {
        var text = AddText(parent, value, 26, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 80);
        var layout = text.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
    }

    private void ShowDungeonSelect(bool saveBeforeDraw = false)
    {
        if (player == null)
        {
            ResetCombatState(true);
            ShowMainMenu();
            return;
        }

        currentScreen = AetheriaScreen.DungeonSelect;
        if (saveBeforeDraw)
        {
            SaveGame();
        }

        ClearRoot();
        if (!dungeonMapBuildLogged)
        {
            Debug.Log("[Aetheria] " + DungeonMapBuildLabel + " 화면 생성.");
            dungeonMapBuildLogged = true;
        }

        var portalDungeons = Dungeons();
        var currentDungeonLabel = portalDungeons.Count > 0 ? Mathf.Clamp(player.stage, 1, portalDungeons.Count) + "/" + portalDungeons.Count : player.stage.ToString();
        if (string.IsNullOrEmpty(selectedDungeonRegionKey))
        {
            selectedDungeonRegionKey = DungeonRegionKey(NextRecommendedDungeon(portalDungeons));
        }

        var portalPage = AddPanel("Dungeon Select Full Map", root, pageColor);
        Stretch(portalPage, 0, 0, 0, 0);

        if (!dungeonMapScrollOpened)
        {
            if (dungeonMapScrollOpening)
            {
                AddDungeonScrollOpeningView(portalPage);
                AddDungeonMapChrome(portalPage);
                AddDungeonMapTopOverlay(portalPage, currentDungeonLabel, false);
                AddDungeonMapStatusOverlay(portalPage, "지도 펼치는 중", "두루마리 축이 양옆으로 열리면 던전 지도가 나타납니다.");
            }
            else
            {
                AddClosedDungeonScrollView(portalPage);
                AddDungeonMapChrome(portalPage);
                AddDungeonMapTopOverlay(portalPage, currentDungeonLabel, false);
                AddDungeonMapStatusOverlay(portalPage, "닫힌 던전 지도", "두루마리를 클릭하면 지도가 펼쳐집니다.");
            }

            return;
        }

        var mapPanel = AddPanel("Dungeon World Map Panel", portalPage, Rgba(220, 237, 246, 250));
        Stretch(mapPanel, 0, 0, 0, 0);
        mapPanel.gameObject.AddComponent<RectMask2D>();

        var mapImage = AddPanel("Dungeon World Map Image", mapPanel, Rgb(3, 9, 18));
        ConfigureDungeonMapImage(mapImage);
        TryApplyStreamingSprite(mapImage, DungeonMapSpritePath);
        ApplyDungeonMapView(mapImage);
        ConfigureDungeonMapInput(mapPanel, mapImage);

        var daylightWash = AddFlatPanel("Map Daylight Wash", mapImage, new Color(1f, 0.99f, 0.92f, 0.06f));
        Stretch(daylightWash, 0, 0, 0, 0);
        daylightWash.GetComponent<Image>().raycastTarget = false;

        AddDungeonRegionLighting(mapImage);
        AddDungeonRegionButtons(mapImage, portalDungeons);
        if (DungeonMapPinsVisible())
        {
            AddDungeonPins(mapImage, portalDungeons);
        }

        AddDungeonMapChrome(portalPage);
        AddDungeonMapTopOverlay(portalPage, currentDungeonLabel, true);
        AddDungeonMapLegendOverlay(portalPage, portalDungeons);
        var pinsVisible = DungeonMapPinsVisible();
        var statusTitle = pinsVisible ? "경로의 원정 핀을 선택하세요" : dungeonMapZoomed ? "조금 더 확대하면 원정 경로가 나타납니다" : "지역 문장을 선택해 원정 경로를 펼치세요";
        var statusDetail = pinsVisible ? (FindDungeonMapRegion(selectedDungeonRegionKey)?.name ?? "미지의 대륙") + "  ·  드래그 이동  ·  휠 확대/축소" : dungeonMapZoomed ? "드래그 이동  ·  휠 확대/축소" : "현재·추천·잠금 상태는 왼쪽 위 범례에서 확인할 수 있습니다";
        AddDungeonMapStatusOverlay(portalPage, statusTitle, statusDetail);
        AddDungeonQuickStartOverlay(portalPage, portalDungeons);
    }

    private void EnterDungeonSelectFromTown()
    {
        ResetDungeonMapView();
        dungeonMapScrollOpened = true;
        dungeonMapScrollOpening = false;
        if (dungeonMapOpenCoroutine != null)
        {
            StopCoroutine(dungeonMapOpenCoroutine);
            dungeonMapOpenCoroutine = null;
        }

        ShowDungeonSelect(true);
    }

    private void OpenDungeonScroll()
    {
        if (dungeonMapScrollOpened || dungeonMapScrollOpening)
        {
            return;
        }

        ResetDungeonMapView();
        dungeonMapScrollOpening = true;
        ShowDungeonSelect();
    }

    private void AddClosedDungeonScrollView(RectTransform portalPage)
    {
        var closedImage = AddCoverImage("Closed Dungeon Scroll", portalPage, DungeonClosedScrollSpritePath, Rgb(6, 3, 1));
        var image = closedImage.GetComponent<Image>();
        image.raycastTarget = true;

        var button = closedImage.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(OpenDungeonScroll);
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Rgba(255, 246, 214, 255);
        colors.pressedColor = Rgba(220, 170, 88, 255);
        colors.selectedColor = Color.white;
        button.colors = colors;
    }

    private void AddDungeonScrollOpeningView(RectTransform portalPage)
    {
        Canvas.ForceUpdateCanvases();
        var targetSize = RectViewportSize(portalPage);

        var mapImage = AddCoverImage("Dungeon Opening World Map", portalPage, DungeonMapSpritePath, Rgb(3, 9, 18));
        mapImage.GetComponent<Image>().raycastTarget = false;
        mapImage.SetAsFirstSibling();

        var leftLeaf = AddDungeonScrollLeaf(portalPage, "Left Dungeon Scroll Leaf", -1f, targetSize);
        var rightLeaf = AddDungeonScrollLeaf(portalPage, "Right Dungeon Scroll Leaf", 1f, targetSize);
        if (dungeonMapOpenCoroutine != null)
        {
            StopCoroutine(dungeonMapOpenCoroutine);
        }

        dungeonMapOpenCoroutine = StartCoroutine(AnimateDungeonScrollOpen(leftLeaf, rightLeaf, targetSize));
    }

    private RectTransform AddDungeonScrollLeaf(RectTransform portalPage, string name, float direction, Vector2 viewportSize)
    {
        var leaf = AddPanel(name + " Mask", portalPage, new Color(0, 0, 0, 0));
        leaf.GetComponent<Image>().raycastTarget = false;
        leaf.anchorMin = new Vector2(0.5f, 0.5f);
        leaf.anchorMax = leaf.anchorMin;
        leaf.pivot = new Vector2(0.5f, 0.5f);
        leaf.sizeDelta = new Vector2(viewportSize.x * 0.5f + 4f, viewportSize.y + 4f);
        leaf.anchoredPosition = new Vector2(direction * viewportSize.x * 0.25f, 0f);
        leaf.gameObject.AddComponent<RectMask2D>();

        var leafImage = AddPanel(name + " Image", leaf, Rgb(6, 3, 1));
        ConfigureCoverImage(leafImage);
        TryApplyStreamingSprite(leafImage, DungeonClosedScrollSpritePath);
        leafImage.sizeDelta = CoverSizeForViewport(viewportSize, SpriteAspect(leafImage, DungeonMapBaseWidth / DungeonMapBaseHeight));
        leafImage.anchoredPosition = -leaf.anchoredPosition;
        leafImage.GetComponent<Image>().raycastTarget = false;
        var layout = leafImage.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.ignoreLayout = true;
        }

        var innerShadow = AddFlatPanel(name + " Inner Shadow", leaf, Rgba(0, 0, 0, 120));
        innerShadow.GetComponent<Image>().raycastTarget = false;
        innerShadow.anchorMin = new Vector2(direction < 0f ? 1f : 0f, 0f);
        innerShadow.anchorMax = new Vector2(direction < 0f ? 1f : 0f, 1f);
        innerShadow.pivot = new Vector2(direction < 0f ? 1f : 0f, 0.5f);
        innerShadow.anchoredPosition = Vector2.zero;
        innerShadow.sizeDelta = new Vector2(44f, 0f);

        var goldEdge = AddFlatPanel(name + " Gold Edge", leaf, Rgba(245, 158, 11, 150));
        goldEdge.GetComponent<Image>().raycastTarget = false;
        goldEdge.anchorMin = new Vector2(direction < 0f ? 1f : 0f, 0f);
        goldEdge.anchorMax = new Vector2(direction < 0f ? 1f : 0f, 1f);
        goldEdge.pivot = new Vector2(direction < 0f ? 1f : 0f, 0.5f);
        goldEdge.anchoredPosition = Vector2.zero;
        goldEdge.sizeDelta = new Vector2(4f, 0f);

        return leaf;
    }

    private IEnumerator AnimateDungeonScrollOpen(RectTransform leftLeaf, RectTransform rightLeaf, Vector2 targetSize)
    {
        var leftStart = new Vector2(-targetSize.x * 0.25f, 0f);
        var rightStart = new Vector2(targetSize.x * 0.25f, 0f);
        var leftEnd = new Vector2(-targetSize.x * 0.78f, 0f);
        var rightEnd = new Vector2(targetSize.x * 0.78f, 0f);
        var elapsed = 0f;
        while (elapsed < DungeonMapOpenAnimationSeconds)
        {
            if (currentScreen != AetheriaScreen.DungeonSelect || leftLeaf == null || rightLeaf == null)
            {
                dungeonMapScrollOpening = false;
                dungeonMapOpenCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / DungeonMapOpenAnimationSeconds);
            t = 1f - Mathf.Pow(1f - t, 3f);
            var lift = Mathf.Sin(t * Mathf.PI) * targetSize.x * 0.018f;
            leftLeaf.anchoredPosition = Vector2.Lerp(leftStart, leftEnd, t) + Vector2.left * lift;
            rightLeaf.anchoredPosition = Vector2.Lerp(rightStart, rightEnd, t) + Vector2.right * lift;
            yield return null;
        }

        if (currentScreen != AetheriaScreen.DungeonSelect)
        {
            dungeonMapScrollOpening = false;
            dungeonMapOpenCoroutine = null;
            yield break;
        }

        dungeonMapScrollOpened = true;
        dungeonMapScrollOpening = false;
        dungeonMapOpenCoroutine = null;
        ShowDungeonSelect();
    }

    private void AddDungeonMapChrome(RectTransform portalPage)
    {
        AddMapEdgeBand(portalPage, "Map Shadow Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 126f), Rgba(24, 62, 84, 34));
        AddMapEdgeBand(portalPage, "Map Shadow Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 126f), Rgba(24, 62, 84, 38));
        AddMapEdgeBand(portalPage, "Map Shadow Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(110f, 0f), Rgba(24, 62, 84, 30));
        AddMapEdgeBand(portalPage, "Map Shadow Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(110f, 0f), Rgba(24, 62, 84, 30));

        AddMapEdgeBand(portalPage, "Map Gold Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(0f, 3f), Rgba(245, 158, 11, 90));
        AddMapEdgeBand(portalPage, "Map Gold Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(0f, 3f), Rgba(245, 158, 11, 82));
        AddMapEdgeBand(portalPage, "Map Gold Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(3f, 0f), Rgba(245, 158, 11, 70));
        AddMapEdgeBand(portalPage, "Map Gold Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-7f, 0f), new Vector2(3f, 0f), Rgba(245, 158, 11, 70));
    }

    private void AddDungeonRegionLighting(RectTransform mapParent)
    {
        if (mapParent == null)
        {
            return;
        }

        foreach (var region in DungeonMapRegions())
        {
            var selected = region.key == selectedDungeonRegionKey;
            if (dungeonMapZoomed && !selected)
            {
                continue;
            }

            var light = AddFlatPanel("Region Light " + region.key, mapParent, new Color(region.color.r, region.color.g, region.color.b, selected ? 0.13f : 0.07f));
            light.anchorMin = new Vector2(0.5f, 0.5f);
            light.anchorMax = light.anchorMin;
            light.pivot = new Vector2(0.5f, 0.5f);
            light.anchoredPosition = DungeonMapLocalPoint(mapParent, region.center);
            light.sizeDelta = selected && dungeonMapZoomed ? new Vector2(430f, 310f) : new Vector2(300f, 210f);
            light.GetComponent<Image>().sprite = MapCircleSprite();
            light.GetComponent<Image>().raycastTarget = false;
            light.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }
    }

    private RectTransform AddMapEdgeBand(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        var band = AddFlatPanel(name, parent, color);
        band.GetComponent<Image>().raycastTarget = false;
        band.anchorMin = anchorMin;
        band.anchorMax = anchorMax;
        band.pivot = pivot;
        band.anchoredPosition = anchoredPosition;
        band.sizeDelta = sizeDelta;
        return band;
    }

    private void AddDungeonMapTopOverlay(RectTransform portalPage, string currentDungeonLabel, bool includeResetButton)
    {
        var portalTop = AddRow("Dungeon Map Top Overlay", portalPage, 8, TextAnchor.MiddleLeft);
        var portalTopImage = portalTop.GetComponent<Image>();
        portalTopImage.sprite = MapRoundedRectSprite();
        portalTopImage.color = new Color(1f, 252f / 255f, 244f / 255f, DungeonMapPlateAlpha);
        portalTopImage.raycastTarget = false;
        portalTop.anchorMin = new Vector2(1f, 1f);
        portalTop.anchorMax = new Vector2(1f, 1f);
        portalTop.pivot = new Vector2(1f, 1f);
        portalTop.anchoredPosition = new Vector2(-14f, -14f);
        portalTop.sizeDelta = new Vector2(includeResetButton ? 600f : 390f, 72f);
        var portalTopLayout = portalTop.GetComponent<HorizontalLayoutGroup>();
        portalTopLayout.padding = new RectOffset(10, 10, 7, 7);
        portalTopLayout.childForceExpandWidth = false;
        portalTopLayout.childForceExpandHeight = true;
        AddText(portalTop, "원정 지도", 25, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 42);
        AddText(portalTop, currentDungeonLabel, 16, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 42);

        if (includeResetButton)
        {
            var resetButton = AddButton(portalTop, "전체 보기", () =>
            {
                ResetDungeonMapView();
                ShowDungeonSelect();
            }, panelAltColor);
            AddLayoutSize(resetButton.GetComponent<RectTransform>(), 180, 60);
        }

        var townButton = AddButton(portalTop, "마을로", () => ShowTown("마을로 돌아왔습니다."), panelAltColor);
        AddLayoutSize(townButton.GetComponent<RectTransform>(), 130, 60);
    }

    private void AddDungeonMapLegendOverlay(RectTransform portalPage, List<DungeonData> dungeons)
    {
        var legend = AddFlatPanel("Dungeon Map Legend", portalPage, new Color(0.96f, 0.98f, 1f, DungeonMapPlateAlpha));
        legend.GetComponent<Image>().sprite = MapRoundedRectSprite();
        legend.GetComponent<Image>().raycastTarget = false;
        legend.anchorMin = new Vector2(0f, 1f);
        legend.anchorMax = new Vector2(0f, 1f);
        legend.pivot = new Vector2(0f, 1f);
        legend.anchoredPosition = new Vector2(14f, -14f);
        legend.sizeDelta = new Vector2(820f, 72f);
        var layout = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(14, 14, 7, 7);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var legendTitle = AddText(legend, "원정 경로", 21, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 54);
        var legendTitleLayout = legendTitle.GetComponent<LayoutElement>();
        legendTitleLayout.preferredWidth = 112f;
        legendTitleLayout.flexibleWidth = 0f;
        AddDungeonLegendItem(legend, "현", "현재", goldColor);
        AddDungeonLegendItem(legend, "추", "추천", manaColor);
        AddDungeonLegendItem(legend, "완", "완료", goodColor);
        AddDungeonLegendItem(legend, "잠", "잠금", mutedColor);

        var recommended = NextRecommendedDungeon(dungeons);
        if (recommended != null)
        {
            AddText(legend, "추천  " + recommended.number + ". " + recommended.name, 18, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 54);
        }
    }

    private void AddDungeonLegendItem(Transform parent, string symbol, string label, Color color)
    {
        var item = AddFlatPanel("Map Legend " + label, parent, Color.clear);
        AddLayoutSize(item, 88, 54);
        item.GetComponent<Image>().raycastTarget = false;
        var layout = item.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var icon = AddFlatPanel("Map Legend Icon " + label, item, new Color(color.r, color.g, color.b, 0.94f));
        AddLayoutSize(icon, 34, 34);
        icon.GetComponent<Image>().sprite = MapCircleSprite();
        icon.GetComponent<Image>().raycastTarget = false;
        var iconText = AddText(icon, symbol, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 34);
        Stretch(iconText.GetComponent<RectTransform>(), 0, 0, 0, 0);
        AddText(item, label, 18, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 42);
    }

    private void AddDungeonMapStatusOverlay(RectTransform portalPage, string title, string detail)
    {
        var statusPanel = AddFlatPanel("Dungeon Map Status Overlay", portalPage, new Color(0.98f, 0.98f, 0.95f, DungeonMapPlateAlpha));
        statusPanel.GetComponent<Image>().sprite = MapRoundedRectSprite();
        statusPanel.GetComponent<Image>().raycastTarget = false;
        statusPanel.anchorMin = new Vector2(0f, 0f);
        statusPanel.anchorMax = new Vector2(0f, 0f);
        statusPanel.pivot = new Vector2(0f, 0f);
        statusPanel.anchoredPosition = new Vector2(14f, 14f);
        statusPanel.sizeDelta = new Vector2(720f, 78f);
        AddVertical(statusPanel, 1, TextAnchor.MiddleCenter, new RectOffset(14, 14, 7, 7));
        AddText(statusPanel, title, 20, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 26);
        AddText(statusPanel, detail, 18, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 26);
    }

    private void AddDungeonQuickStartOverlay(RectTransform portalPage, List<DungeonData> dungeons)
    {
        var recommended = NextRecommendedDungeon(dungeons);
        if (recommended == null)
        {
            return;
        }

        var quickStart = AddFlatPanel("Dungeon Quick Start", portalPage, new Color(1f, 0.98f, 0.92f, DungeonMapPlateAlpha));
        quickStart.GetComponent<Image>().sprite = MapRoundedRectSprite();
        AddSideAccent(quickStart, manaColor);
        quickStart.anchorMin = new Vector2(1f, 0f);
        quickStart.anchorMax = new Vector2(1f, 0f);
        quickStart.pivot = new Vector2(1f, 0f);
        quickStart.anchoredPosition = new Vector2(-14f, 14f);
        quickStart.sizeDelta = new Vector2(520f, 182f);
        AddVertical(quickStart, 5, TextAnchor.MiddleCenter, new RectOffset(18, 18, 12, 12));

        AddText(quickStart, "나침반이 가리키는 추천 경로", 20, FontStyle.Bold, manaColor, TextAnchor.MiddleCenter, 28);
        AddText(quickStart, recommended.number + ". " + recommended.name + "  ·  권장 Lv." + recommended.recommendedLevel, 22, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 34);
        AddText(quickStart, "빠른 시작: 추천 던전으로 바로 출발할 수 있어요.", 18, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 24);
        var enter = AddButton(quickStart, "추천 전투 시작", () => StartCombat(recommended, false), goodColor);
        AddLayoutSize(enter.GetComponent<RectTransform>(), -1, 56);
        enter.interactable = IsDungeonUnlocked(recommended) && HasDungeonEncounter(recommended);
    }

    private bool HasDungeonEncounter(DungeonData dungeon)
    {
        if (dungeon == null)
        {
            return false;
        }

        return dungeon.boss != null && dungeon.monsters != null && dungeon.monsters.Count > 0;
    }

    private RectTransform AddCoverImage(string name, RectTransform parent, string spritePath, Color fallbackColor)
    {
        var imageRect = AddPanel(name, parent, fallbackColor);
        ConfigureCoverImage(imageRect);
        TryApplyStreamingSprite(imageRect, spritePath);
        FitCoverImageToViewport(imageRect);

        var layout = imageRect.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.ignoreLayout = true;
        }

        return imageRect;
    }

    private void ConfigureCoverImage(RectTransform imageRect)
    {
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.localScale = Vector3.one;
    }

    private void FitCoverImageToViewport(RectTransform imageRect)
    {
        if (imageRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var viewportSize = RectViewportSize(imageRect.parent as RectTransform);
        var aspect = SpriteAspect(imageRect, DungeonMapBaseWidth / DungeonMapBaseHeight);
        imageRect.sizeDelta = CoverSizeForViewport(viewportSize, aspect);
    }

    private Vector2 CoverSizeForViewport(Vector2 viewportSize, float aspect)
    {
        aspect = Mathf.Max(0.01f, aspect);
        var targetWidth = viewportSize.x;
        var targetHeight = targetWidth / aspect;
        if (targetHeight < viewportSize.y)
        {
            targetHeight = viewportSize.y;
            targetWidth = targetHeight * aspect;
        }

        return new Vector2(targetWidth, targetHeight);
    }

    private Vector2 RectViewportSize(RectTransform viewport)
    {
        Canvas.ForceUpdateCanvases();
        var width = viewport != null && viewport.rect.width > 1f ? viewport.rect.width : 1920f;
        var height = viewport != null && viewport.rect.height > 1f ? viewport.rect.height : 1080f;
        return new Vector2(width, height);
    }

    private float SpriteAspect(RectTransform imageRect, float fallbackAspect)
    {
        var image = imageRect != null ? imageRect.GetComponent<Image>() : null;
        if (image == null || image.sprite == null)
        {
            return fallbackAspect;
        }

        var spriteRect = image.sprite.rect;
        return spriteRect.width > 1f && spriteRect.height > 1f ? spriteRect.width / spriteRect.height : fallbackAspect;
    }

    private void ConfigureDungeonMapImage(RectTransform mapImage)
    {
        mapImage.anchorMin = new Vector2(0.5f, 0.5f);
        mapImage.anchorMax = new Vector2(0.5f, 0.5f);
        mapImage.pivot = new Vector2(0.5f, 0.5f);
        mapImage.anchoredPosition = Vector2.zero;
        mapImage.sizeDelta = CoveredDungeonMapSize(mapImage);

        var layout = mapImage.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.ignoreLayout = true;
        }
    }

    private Vector2 CoveredDungeonMapSize(RectTransform mapImage)
    {
        Canvas.ForceUpdateCanvases();
        var viewport = mapImage != null ? mapImage.parent as RectTransform : null;
        var viewportWidth = viewport != null && viewport.rect.width > 1f ? viewport.rect.width : 1920f;
        var viewportHeight = viewport != null && viewport.rect.height > 1f ? viewport.rect.height : 1080f;
        var mapAspect = DungeonMapBaseWidth / DungeonMapBaseHeight;
        var viewportAspect = viewportWidth / viewportHeight;
        var scale = viewportAspect > mapAspect ? viewportWidth / DungeonMapBaseWidth : viewportHeight / DungeonMapBaseHeight;
        scale = Mathf.Max(1f, scale);
        return new Vector2(DungeonMapBaseWidth * scale, DungeonMapBaseHeight * scale);
    }

    private void FitDungeonMapToViewport(RectTransform mapImage)
    {
        if (mapImage == null)
        {
            return;
        }

        var targetSize = CoveredDungeonMapSize(mapImage);
        if ((mapImage.sizeDelta - targetSize).sqrMagnitude > 1f)
        {
            mapImage.sizeDelta = targetSize;
        }
    }

    private void ConfigureDungeonMapInput(RectTransform viewport, RectTransform mapImage)
    {
        var input = viewport.gameObject.AddComponent<DungeonMapInputHandler>();
        input.Configure(this, viewport, mapImage);
    }

    private Vector2 ClampDungeonMapPosition(RectTransform mapImage, Vector2 position, float zoom)
    {
        var viewport = mapImage.parent as RectTransform;
        var viewportWidth = viewport != null && viewport.rect.width > 1f ? viewport.rect.width : 1920f;
        var viewportHeight = viewport != null && viewport.rect.height > 1f ? viewport.rect.height : 1080f;
        var contentSize = DungeonMapContentSize(mapImage);
        var maxX = Mathf.Max(0f, (contentSize.x * zoom - viewportWidth) * 0.5f);
        var maxY = Mathf.Max(0f, (contentSize.y * zoom - viewportHeight) * 0.5f);
        return new Vector2(
            Mathf.Clamp(position.x, -maxX, maxX),
            Mathf.Clamp(position.y, -maxY, maxY));
    }

    private Vector2 DungeonMapContentSize(RectTransform mapImage)
    {
        var rectSize = mapImage != null ? mapImage.rect.size : Vector2.zero;
        if (rectSize.x <= 1f || rectSize.y <= 1f)
        {
            rectSize = mapImage != null ? mapImage.sizeDelta : new Vector2(DungeonMapBaseWidth, DungeonMapBaseHeight);
        }

        if (rectSize.x <= 1f || rectSize.y <= 1f)
        {
            rectSize = new Vector2(DungeonMapBaseWidth, DungeonMapBaseHeight);
        }

        var image = mapImage != null ? mapImage.GetComponent<Image>() : null;
        if (image == null || image.sprite == null || !image.preserveAspect)
        {
            return rectSize;
        }

        var spriteRect = image.sprite.rect;
        if (spriteRect.width <= 1f || spriteRect.height <= 1f)
        {
            return rectSize;
        }

        var spriteAspect = spriteRect.width / spriteRect.height;
        var rectAspect = rectSize.x / rectSize.y;
        return spriteAspect > rectAspect
            ? new Vector2(rectSize.x, rectSize.x / spriteAspect)
            : new Vector2(rectSize.y * spriteAspect, rectSize.y);
    }

    private Vector2 DungeonMapLocalPoint(RectTransform mapImage, Vector2 percent)
    {
        var contentSize = DungeonMapContentSize(mapImage);
        return new Vector2(
            (percent.x / 100f - 0.5f) * contentSize.x,
            (0.5f - percent.y / 100f) * contentSize.y);
    }

    private void AddDungeonRegionButtons(RectTransform mapParent, List<DungeonData> dungeons)
    {
        if (dungeonMapZoomed)
        {
            return;
        }

        var recommended = NextRecommendedDungeon(dungeons);
        foreach (var region in DungeonMapRegions())
        {
            var localRegion = region;
            var regionDungeons = DungeonsInRegion(dungeons, localRegion.key);
            var containsRecommended = recommended != null && DungeonRegionKey(recommended) == localRegion.key;
            var selected = selectedDungeonRegionKey == localRegion.key;
            var button = AddMapRegionButton(mapParent, localRegion, regionDungeons.Count, selected, containsRecommended, () =>
            {
                FocusDungeonMapRegion(localRegion, mapParent);
                ShowDungeonSelect();
            });
            button.interactable = regionDungeons.Count > 0;
        }
    }

    private void ApplyDungeonMapView(RectTransform mapImage)
    {
        FitDungeonMapToViewport(mapImage);

        if (!dungeonMapZoomed || dungeonMapZoom <= DungeonMapMinZoom + 0.001f)
        {
            ResetDungeonMapView();
            mapImage.localScale = Vector3.one;
            mapImage.anchoredPosition = Vector2.zero;
            return;
        }

        Canvas.ForceUpdateCanvases();
        dungeonMapZoom = Mathf.Clamp(dungeonMapZoom, DungeonMapMinZoom, DungeonMapMaxZoom);
        dungeonMapPan = ClampDungeonMapPosition(mapImage, dungeonMapPan, dungeonMapZoom);
        mapImage.localScale = new Vector3(dungeonMapZoom, dungeonMapZoom, 1f);
        mapImage.anchoredPosition = dungeonMapPan;
    }

    private bool DungeonMapPinsVisible()
    {
        return dungeonMapZoomed && dungeonMapZoom >= DungeonMapPinRevealZoom;
    }

    private void ResetDungeonMapView()
    {
        dungeonMapZoomed = false;
        dungeonMapZoom = DungeonMapMinZoom;
        dungeonMapPan = Vector2.zero;
    }

    private void FocusDungeonMapRegion(DungeonMapRegion region, RectTransform mapImage)
    {
        if (region == null)
        {
            return;
        }

        selectedDungeonRegionKey = region.key;
        dungeonMapZoomed = true;
        dungeonMapZoom = DungeonMapRegionZoom;
        dungeonMapPan = DungeonMapPanForPercent(mapImage, region.center, dungeonMapZoom);
    }

    private Vector2 DungeonMapPanForPercent(RectTransform mapImage, Vector2 percent, float zoom)
    {
        return -DungeonMapLocalPoint(mapImage, percent) * zoom;
    }

    private Vector2 DungeonMapPercentFromLocalPoint(RectTransform mapImage, Vector2 localPoint)
    {
        var contentSize = DungeonMapContentSize(mapImage);
        var width = Mathf.Max(1f, contentSize.x);
        var height = Mathf.Max(1f, contentSize.y);
        return new Vector2(
            Mathf.Clamp((localPoint.x / width + 0.5f) * 100f, 0f, 100f),
            Mathf.Clamp((0.5f - localPoint.y / height) * 100f, 0f, 100f));
    }

    private string ClosestDungeonRegionKeyToPercent(Vector2 percent)
    {
        var bestKey = string.IsNullOrEmpty(selectedDungeonRegionKey) ? "green" : selectedDungeonRegionKey;
        var bestDistance = float.MaxValue;
        foreach (var region in DungeonMapRegions())
        {
            var distance = (region.center - percent).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestKey = region.key;
            }
        }

        return bestKey;
    }

    private bool TryDungeonMapLocalPointer(RectTransform viewport, PointerEventData eventData, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (viewport == null || eventData == null)
        {
            return false;
        }

        var camera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, camera, out localPoint);
    }

    private void OnDungeonMapDrag(RectTransform viewport, RectTransform mapImage, PointerEventData eventData, ref Vector2 lastLocalPointer)
    {
        if (currentScreen != AetheriaScreen.DungeonSelect || mapImage == null || dungeonMapZoom <= DungeonMapMinZoom + 0.001f)
        {
            return;
        }

        if (!TryDungeonMapLocalPointer(viewport, eventData, out var localPoint))
        {
            return;
        }

        var delta = localPoint - lastLocalPointer;
        lastLocalPointer = localPoint;
        dungeonMapPan = ClampDungeonMapPosition(mapImage, dungeonMapPan + delta, dungeonMapZoom);
        selectedDungeonRegionKey = ClosestDungeonRegionKeyToPercent(DungeonMapPercentFromLocalPoint(mapImage, -dungeonMapPan / dungeonMapZoom));
        ClearDungeonInfoPopups(mapImage);
        ApplyDungeonMapView(mapImage);
    }

    private void OnDungeonMapScroll(RectTransform viewport, RectTransform mapImage, PointerEventData eventData)
    {
        if (currentScreen != AetheriaScreen.DungeonSelect || mapImage == null)
        {
            return;
        }

        if (!TryDungeonMapLocalPointer(viewport, eventData, out var localPoint))
        {
            return;
        }

        var scroll = eventData.scrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        var oldZoom = Mathf.Max(DungeonMapMinZoom, dungeonMapZoom);
        var oldZoomed = dungeonMapZoomed;
        var oldPinsVisible = DungeonMapPinsVisible();
        var mapPointBeforeZoom = (localPoint - dungeonMapPan) / oldZoom;
        var direction = scroll > 0f ? 1f : -1f;
        var newZoom = Mathf.Clamp(oldZoom + DungeonMapWheelStep * direction, DungeonMapMinZoom, DungeonMapMaxZoom);
        if (direction > 0f && oldZoom < DungeonMapRegionZoom && newZoom > DungeonMapRegionZoom)
        {
            newZoom = DungeonMapRegionZoom;
        }
        else if (direction < 0f && oldZoom > DungeonMapRegionZoom && newZoom < DungeonMapRegionZoom)
        {
            newZoom = DungeonMapRegionZoom;
        }

        if (newZoom <= DungeonMapMinZoom + 0.001f)
        {
            ResetDungeonMapView();
        }
        else
        {
            dungeonMapZoom = newZoom;
            dungeonMapZoomed = true;
            dungeonMapPan = ClampDungeonMapPosition(mapImage, localPoint - mapPointBeforeZoom * newZoom, dungeonMapZoom);
            selectedDungeonRegionKey = ClosestDungeonRegionKeyToPercent(DungeonMapPercentFromLocalPoint(mapImage, mapPointBeforeZoom));
        }

        ClearDungeonInfoPopups(mapImage);
        if (oldZoomed != dungeonMapZoomed || oldPinsVisible != DungeonMapPinsVisible())
        {
            ShowDungeonSelect();
            return;
        }

        ApplyDungeonMapView(mapImage);
    }

    private void AddDungeonPins(RectTransform mapParent, List<DungeonData> dungeons)
    {
        if (mapParent == null || dungeons == null)
        {
            return;
        }

        var recommended = NextRecommendedDungeon(dungeons);
        AddDungeonRouteNetwork(mapParent, dungeons, recommended);
        foreach (var region in DungeonMapRegions())
        {
            if (region.points == null || region.points.Length == 0)
            {
                continue;
            }

            var regionDungeons = DungeonsInRegion(dungeons, region.key);
            for (var i = 0; i < regionDungeons.Count; i++)
            {
                var dungeon = regionDungeons[i];
                var unlocked = IsDungeonUnlocked(dungeon);
                var bossCleared = IsBossCleared(dungeon);
                var isCurrent = dungeon.number == Mathf.Clamp(player.stage, 1, Mathf.Max(1, dungeons.Count));
                var isRecommended = recommended != null && dungeon.number == recommended.number;
                var point = DungeonMapPointForDungeon(region, dungeon, i);
                var color = bossCleared ? goodColor : unlocked ? manaColor : mutedColor;
                var localDungeon = dungeon;
                var localPoint = point;
                var button = AddDungeonPinButton(mapParent, dungeon, point, color, unlocked, bossCleared, isCurrent, isRecommended, () =>
                {
                    ShowDungeonInfoPopup(mapParent, localDungeon, localPoint);
                });
                button.interactable = HasDungeonEncounter(dungeon);
            }
        }

    }

    private void AddDungeonRouteNetwork(RectTransform mapParent, List<DungeonData> dungeons, DungeonData recommended)
    {
        foreach (var region in DungeonMapRegions())
        {
            var regionDungeons = DungeonsInRegion(dungeons, region.key);
            for (var index = 1; index < regionDungeons.Count; index++)
            {
                var from = regionDungeons[index - 1];
                var to = regionDungeons[index];
                var fromPoint = DungeonMapPointForDungeon(region, from, index - 1);
                var toPoint = DungeonMapPointForDungeon(region, to, index);
                var routeColor = IsBossCleared(to)
                    ? goodColor
                    : IsDungeonUnlocked(to) ? goldColor : Rgb(118, 132, 145);
                var emphasized = (recommended != null && (from.number == recommended.number || to.number == recommended.number))
                    || to.number == player.stage;
                AddDungeonRouteSegment(mapParent, fromPoint, toPoint, routeColor, emphasized);
            }
        }
    }

    private void AddDungeonRouteSegment(RectTransform mapParent, Vector2 fromPercent, Vector2 toPercent, Color color, bool emphasized)
    {
        var from = DungeonMapLocalPoint(mapParent, fromPercent);
        var to = DungeonMapLocalPoint(mapParent, toPercent);
        var delta = to - from;
        var distance = delta.magnitude;
        if (distance <= 1f)
        {
            return;
        }

        var counterScale = DungeonMapCounterScale(mapParent);
        var route = AddFlatPanel("Dungeon Route", mapParent, new Color(color.r, color.g, color.b, emphasized ? 0.72f : 0.48f));
        route.anchorMin = new Vector2(0.5f, 0.5f);
        route.anchorMax = route.anchorMin;
        route.pivot = new Vector2(0.5f, 0.5f);
        route.anchoredPosition = (from + to) * 0.5f;
        route.sizeDelta = new Vector2(distance, (emphasized ? 5f : 3f) * counterScale);
        route.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        route.GetComponent<Image>().raycastTarget = false;
        route.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        var zoom = Mathf.Max(DungeonMapMinZoom, mapParent.localScale.x);
        var visualDistance = distance * zoom;
        var dotCount = Mathf.Max(2, Mathf.FloorToInt(visualDistance / 36f));
        for (var dotIndex = 1; dotIndex < dotCount; dotIndex++)
        {
            var dot = AddFlatPanel("Dungeon Route Step", mapParent, new Color(color.r, color.g, color.b, emphasized ? 0.98f : 0.76f));
            dot.anchorMin = new Vector2(0.5f, 0.5f);
            dot.anchorMax = dot.anchorMin;
            dot.pivot = new Vector2(0.5f, 0.5f);
            dot.anchoredPosition = Vector2.Lerp(from, to, dotIndex / (float)dotCount);
            dot.sizeDelta = Vector2.one * (emphasized ? 10f : 7f) * counterScale;
            var dotImage = dot.GetComponent<Image>();
            dotImage.sprite = MapCircleSprite();
            dotImage.raycastTarget = false;
            dot.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }
    }

    private Vector2 DungeonMapPointForDungeon(DungeonMapRegion region, DungeonData dungeon, int index)
    {
        if (dungeon != null)
        {
            switch (dungeon.number)
            {
                case 1: return new Vector2(42f, 24f);
                case 13: return new Vector2(49f, 16f);
                case 14: return new Vector2(56f, 24f);
                case 23: return new Vector2(44f, 33f);
                case 45: return new Vector2(58f, 32f);
                case 2: return new Vector2(42f, 48f);
                case 18: return new Vector2(50f, 42f);
                case 20: return new Vector2(58f, 51f);
                case 24: return new Vector2(45f, 62f);
                case 30: return new Vector2(60f, 59f);
                case 3: return new Vector2(18f, 75f);
                case 10: return new Vector2(24f, 69f);
                case 16: return new Vector2(32f, 76f);
                case 17: return new Vector2(21f, 84f);
                case 43: return new Vector2(35f, 82f);
                case 4: return new Vector2(66f, 24f);
                case 7: return new Vector2(74f, 16f);
                case 11: return new Vector2(82f, 24f);
                case 19: return new Vector2(69f, 33f);
                case 42: return new Vector2(84f, 32f);
                case 5: return new Vector2(63f, 69f);
                case 25: return new Vector2(77f, 67f);
                case 28: return new Vector2(83f, 66f);
                case 32: return new Vector2(63f, 79f);
                case 35: return new Vector2(76f, 78f);
                case 6: return new Vector2(17f, 24f);
                case 12: return new Vector2(24f, 17f);
                case 15: return new Vector2(32f, 24f);
                case 26: return new Vector2(20f, 32f);
                case 40: return new Vector2(35f, 31f);
                case 8: return new Vector2(16f, 50f);
                case 21: return new Vector2(23f, 42f);
                case 31: return new Vector2(32f, 49f);
                case 36: return new Vector2(20f, 60f);
                case 41: return new Vector2(35f, 58f);
                case 9: return new Vector2(42f, 73f);
                case 22: return new Vector2(49f, 69f);
                case 29: return new Vector2(56f, 76f);
                case 33: return new Vector2(46f, 86f);
                case 38: return new Vector2(59f, 83f);
                case 27: return new Vector2(65f, 49f);
                case 34: return new Vector2(74f, 42f);
                case 37: return new Vector2(83f, 50f);
                case 39: return new Vector2(66f, 58f);
                case 44: return new Vector2(81f, 56f);
            }
        }

        if (region == null || region.points == null || region.points.Length == 0)
        {
            return new Vector2(50f, 50f);
        }

        var basePoint = region.points[Mathf.Abs(index) % region.points.Length];
        var cycle = Mathf.Max(0, index / region.points.Length);
        if (cycle <= 0)
        {
            return basePoint;
        }

        var spreadX = (cycle % 2 == 0 ? 2.4f : -2.4f) * cycle;
        var spreadY = 1.8f * cycle;
        return new Vector2(Mathf.Clamp(basePoint.x + spreadX, 6f, 94f), Mathf.Clamp(basePoint.y + spreadY, 6f, 94f));
    }

    private float DungeonMapCounterScale(RectTransform mapParent)
    {
        var zoom = mapParent != null ? Mathf.Max(DungeonMapMinZoom, mapParent.localScale.x) : DungeonMapMinZoom;
        return 1f / zoom;
    }

    private void ApplyDungeonMapCounterScale(RectTransform rect, RectTransform mapParent)
    {
        if (rect == null)
        {
            return;
        }

        var scale = DungeonMapCounterScale(mapParent);
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    private void ShowDungeonInfoPopup(RectTransform mapParent, DungeonData dungeon, Vector2 percent)
    {
        if (mapParent == null || dungeon == null)
        {
            return;
        }

        ClearDungeonInfoPopups(mapParent);

        var unlocked = IsDungeonUnlocked(dungeon);
        var bossCleared = IsBossCleared(dungeon);
        var underLevel = player.level < dungeon.recommendedLevel;
        var current = dungeon.number == player.stage;
        var recommendedDungeon = NextRecommendedDungeon(Dungeons());
        var recommended = recommendedDungeon != null && recommendedDungeon.number == dungeon.number;
        var region = FindDungeonMapRegion(DungeonRegionKey(dungeon));
        var statusColor = bossCleared ? goodColor : recommended ? manaColor : current ? goldColor : unlocked ? manaColor : mutedColor;
        var statusText = bossCleared ? "토벌 완료" : current && recommended ? "현재 · 추천" : current ? "현재 경로" : recommended ? "추천 경로" : unlocked ? "진입 가능" : "잠금";
        if (underLevel && unlocked)
        {
            statusText += " · 위험";
            statusColor = goldColor;
        }

        var popupParent = mapParent.parent as RectTransform;
        if (popupParent == null)
        {
            return;
        }

        SetDungeonMapHudVisible(mapParent, false);

        var viewportRect = popupParent.rect;
        var availableWidth = viewportRect.width > 36f ? viewportRect.width - 36f : viewportRect.width;
        var availableHeight = viewportRect.height > 36f ? viewportRect.height - 36f : viewportRect.height;
        var popupSize = new Vector2(
            Mathf.Min(1060f, Mathf.Max(1f, availableWidth)),
            Mathf.Min(900f, Mathf.Max(1f, availableHeight)));
        var dimmer = AddFlatPanel("Dungeon Info Modal Dimmer", popupParent, new Color(7f / 255f, 18f / 255f, 30f / 255f, 0.52f));
        Stretch(dimmer, 0f, 0f, 0f, 0f);
        dimmer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var dimmerImage = dimmer.GetComponent<Image>();
        dimmerImage.raycastTarget = true;
        var dimmerButton = dimmer.gameObject.AddComponent<Button>();
        dimmerButton.targetGraphic = dimmerImage;
        var dimmerNavigation = dimmerButton.navigation;
        dimmerNavigation.mode = Navigation.Mode.None;
        dimmerButton.navigation = dimmerNavigation;
        dimmer.SetAsLastSibling();

        var window = AddFlatPanel("Dungeon Info Popup", popupParent, new Color(12f / 255f, 33f / 255f, 45f / 255f, 0.94f));
        var windowImage = window.GetComponent<Image>();
        windowImage.sprite = MapRoundedRectSprite();
        windowImage.type = Image.Type.Sliced;
        windowImage.raycastTarget = true;
        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = window.anchorMin;
        window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = popupSize;
        window.localScale = Vector3.one;
        window.anchoredPosition = Vector2.zero;
        window.SetAsLastSibling();
        var popupLayout = window.GetComponent<LayoutElement>();
        if (popupLayout != null)
        {
            popupLayout.ignoreLayout = true;
        }
        ApplyCharacterThemePanelFrame(window, null, true);

        var safeHorizontal = Mathf.Min(78f, popupSize.x * 0.075f);
        var safeVertical = Mathf.Min(58f, popupSize.y * 0.065f);
        var safeContent = AddFlatPanel("Dungeon Modal Safe Content", window, Color.clear);
        safeContent.GetComponent<Image>().raycastTarget = false;
        Stretch(safeContent, safeHorizontal, safeVertical, safeHorizontal, safeVertical);
        safeContent.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        AddVertical(safeContent, 10, TextAnchor.UpperCenter, new RectOffset(0, 0, 0, 0));

        var header = AddDungeonPopupGlassPanel(safeContent, "Expedition Modal Header Surface", 76f, statusColor);
        var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 14f;
        headerLayout.padding = new RectOffset(22, 22, 8, 8);
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        AddDungeonRegionArtwork(header, region, "Dungeon Popup Region Emblem", 58f);
        var titleText = AddText(header, dungeon.number + ". " + dungeon.name, 30, FontStyle.Bold, unlocked ? goldColor : mutedColor, TextAnchor.MiddleLeft, 58);
        AddLayoutSize(titleText.GetComponent<RectTransform>(), -1, 58);
        var statePlate = AddFlatPanel("Dungeon State Badge", header, new Color(statusColor.r, statusColor.g, statusColor.b, DungeonMapPlateAlpha));
        var statePlateImage = statePlate.GetComponent<Image>();
        statePlateImage.sprite = MapRoundedRectSprite();
        statePlateImage.type = Image.Type.Sliced;
        statePlateImage.raycastTarget = false;
        ConstrainLayoutSize(statePlate, 184f, 48f);
        var stateText = AddText(statePlate, statusText, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 48);
        Stretch(stateText.GetComponent<RectTransform>(), 10, 3, 10, 3);

        var facts = AddDungeonPopupGlassPanel(safeContent, "Expedition Modal Facts Surface", 64f, region != null ? region.color : manaColor);
        var factsLayout = facts.gameObject.AddComponent<HorizontalLayoutGroup>();
        factsLayout.spacing = 12;
        factsLayout.padding = new RectOffset(12, 12, 4, 4);
        factsLayout.childAlignment = TextAnchor.MiddleLeft;
        factsLayout.childControlWidth = true;
        factsLayout.childControlHeight = true;
        factsLayout.childForceExpandWidth = true;
        factsLayout.childForceExpandHeight = true;
        AddDungeonPopupFact(facts, DungeonRegionGlyph(region != null ? region.key : ""), "지역", region != null ? region.name : "미지의 대륙", region != null ? region.color : manaColor);
        AddDungeonPopupFact(facts, "Lv", "권장", "Lv." + dungeon.recommendedLevel, underLevel ? goldColor : goodColor);
        AddDungeonPopupFact(facts, "층", "구역", DungeonFloorCount(dungeon) + "개", manaColor);

        var routeSurface = AddDungeonPopupGlassPanel(safeContent, "Expedition Modal Route Surface", 64f, statusColor);
        AddVertical(routeSurface, 0, TextAnchor.MiddleCenter, new RectOffset(8, 8, 5, 5));
        AddDungeonRoutePreview(routeSurface, DungeonFloorCount(dungeon), statusColor, bossCleared);

        var descriptionSurface = AddDungeonPopupGlassPanel(safeContent, "Expedition Modal Description Surface", 70f, statusColor);
        var descriptionText = AddText(descriptionSurface, dungeon.description, 19, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft, 54);
        Stretch(descriptionText.GetComponent<RectTransform>(), 18, 8, 18, 8);

        var details = AddRow("Dungeon Popup Details", safeContent, 14, TextAnchor.UpperCenter);
        AddLayoutSize(details, -1, -1);
        var detailsLayout = details.GetComponent<HorizontalLayoutGroup>();
        detailsLayout.childForceExpandWidth = true;
        detailsLayout.childForceExpandHeight = true;

        var preparation = AddDungeonPopupGlassPanel(details, "Expedition Modal Preparation Surface", -1f, statusColor);
        AddVertical(preparation, 6, TextAnchor.UpperLeft, new RectOffset(18, 18, 12, 12));
        AddText(preparation, "원정 준비", 22, FontStyle.Bold, statusColor, TextAnchor.MiddleLeft, 30);
        AddText(preparation, DungeonRecommendedPowerText(dungeon), 18, FontStyle.Bold, underLevel ? goldColor : Color.white, TextAnchor.UpperLeft, 76);
        AddText(preparation, "적 정보", 20, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 28);
        AddText(
            preparation,
            "일반  " + DungeonMonsterSummary(dungeon) + "\n보스  " + DungeonBossName(dungeon),
            18,
            FontStyle.Bold,
            Color.white,
            TextAnchor.UpperLeft,
            88);

        var rewards = AddDungeonPopupGlassPanel(details, "Expedition Modal Rewards Surface", -1f, RarityColor(MaxRarityForDungeon(dungeon.number)));
        AddVertical(rewards, 6, TextAnchor.UpperLeft, new RectOffset(18, 18, 12, 12));
        AddText(rewards, "예상 전리품", 22, FontStyle.Bold, RarityColor(MaxRarityForDungeon(dungeon.number)), TextAnchor.MiddleLeft, 30);
        AddText(rewards, DungeonLootRarityText(dungeon), 18, FontStyle.Bold, Color.white, TextAnchor.UpperLeft, 82);
        AddText(rewards, DungeonRewardAndDangerText(dungeon), 18, FontStyle.Normal, Color.white, TextAnchor.UpperLeft, 112);
        AddText(rewards, "현재 영웅", 20, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 28);
        AddText(
            rewards,
            "Lv." + player.level + "  HP " + player.hp + "/" + MaxHp() + "  MP " + player.mp + "/" + MaxMp()
                + "\n공격 " + Attack() + "  ·  마력 " + Magic() + "  ·  방어 " + Defense() + "  ·  속도 " + Speed(),
            18,
            FontStyle.Bold,
            Color.white,
            TextAnchor.UpperLeft,
            66);

        var buttons = AddDungeonPopupGlassPanel(safeContent, "Expedition Modal Footer Surface", 78f, statusColor);
        var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 14f;
        buttonsLayout.padding = new RectOffset(22, 22, 10, 10);
        buttonsLayout.childAlignment = TextAnchor.MiddleRight;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = false;
        Action closePopup = () =>
        {
            SetDungeonMapHudVisible(mapParent, true);
            SelectFirstDungeonMapButton(mapParent, dungeon.number);
            Destroy(window.gameObject);
            Destroy(dimmer.gameObject);
        };
        dimmerButton.onClick.AddListener(() => closePopup());
        var closeButton = AddObjectActionButton(buttons, "닫기", () => closePopup(), panelAltColor, VisualActionRole.Back, "back", 190, 58);
        var enterButton = AddObjectActionButton(buttons, unlocked ? "전투 시작" : "경로 잠금", () => StartCombat(dungeon, false), goodColor, VisualActionRole.Portal, "portal", 280, 58);
        enterButton.interactable = unlocked && HasDungeonEncounter(dungeon);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                (enterButton.interactable ? enterButton : closeButton).gameObject);
        }
    }

    private RectTransform AddDungeonPopupGlassPanel(Transform parent, string name, float preferredHeight, Color accent)
    {
        var baseSurface = new Color(7f / 255f, 30f / 255f, 43f / 255f, 1f);
        var accentSurface = new Color(accent.r, accent.g, accent.b, 1f);
        var tint = Color.Lerp(baseSurface, accentSurface, 0.10f);
        var panel = AddFlatPanel(name, parent, new Color(tint.r, tint.g, tint.b, 0.86f));
        var image = panel.GetComponent<Image>();
        image.sprite = MapRoundedRectSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        AddLayoutSize(panel, -1f, preferredHeight > 0f ? preferredHeight : -1f);
        return panel;
    }

    private void SelectFirstDungeonMapButton(RectTransform mapParent, int preferredDungeonNumber)
    {
        if (mapParent == null || EventSystem.current == null)
        {
            return;
        }

        var preferredTransform = mapParent.Find("Dungeon Pin " + preferredDungeonNumber);
        var preferredButton = preferredTransform != null ? preferredTransform.GetComponent<Button>() : null;
        if (preferredButton != null
            && preferredButton.gameObject.activeInHierarchy
            && preferredButton.interactable)
        {
            EventSystem.current.SetSelectedGameObject(preferredButton.gameObject);
            return;
        }

        var buttons = mapParent.GetComponentsInChildren<Button>(true);
        for (var index = 0; index < buttons.Length; index++)
        {
            var button = buttons[index];
            var buttonName = button != null ? button.gameObject.name ?? "" : "";
            if (button != null
                && button.gameObject.activeInHierarchy
                && button.interactable
                && !buttonName.Contains(" Hit"))
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                return;
            }
        }
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void AddDungeonPopupFact(Transform parent, string symbol, string label, string value, Color accent, Sprite artwork = null)
    {
        var fact = AddFlatPanel("Dungeon Fact " + label, parent, Color.clear);
        fact.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(fact, -1, 56);
        var layout = fact.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var icon = AddFlatPanel("Dungeon Fact Icon " + label, fact, new Color(accent.r, accent.g, accent.b, 0.94f));
        var iconImage = icon.GetComponent<Image>();
        iconImage.sprite = artwork != null ? artwork : MapCircleSprite();
        iconImage.color = artwork != null ? Color.white : new Color(accent.r, accent.g, accent.b, 0.94f);
        iconImage.preserveAspect = artwork != null;
        iconImage.raycastTarget = false;
        AddLayoutSize(icon, 44, 44);
        if (artwork == null)
        {
            var iconText = AddText(icon, symbol, 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 44);
            Stretch(iconText.GetComponent<RectTransform>(), 0, 0, 0, 0);
        }

        var text = AddText(fact, label + "  " + value, 18, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 50);
        AddLayoutSize(text.GetComponent<RectTransform>(), -1, 50);
    }

    private void ClearDungeonInfoPopups(RectTransform mapParent)
    {
        DestroyDungeonInfoPopupsUnder(mapParent);
        DestroyDungeonInfoPopupsUnder(mapParent != null ? mapParent.parent as RectTransform : null);
        SetDungeonMapHudVisible(mapParent, true);
    }

    private void DestroyDungeonInfoPopupsUnder(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child != null
                && (child.name == "Dungeon Info Popup" || child.name == "Dungeon Info Modal Dimmer"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static bool DungeonInfoModalOpen(RectTransform viewport)
    {
        return viewport != null
            && (viewport.Find("Dungeon Info Popup") != null
                || viewport.Find("Dungeon Info Modal Dimmer") != null);
    }

    private void SetDungeonMapHudVisible(RectTransform mapParent, bool visible)
    {
        var portalPage = mapParent != null && mapParent.parent != null ? mapParent.parent.parent : null;
        if (portalPage == null)
        {
            return;
        }

        var hiddenNames = new[] { "Dungeon Map Top Overlay", "Dungeon Map Legend", "Dungeon Map Status Overlay", "Dungeon Quick Start" };
        for (var i = 0; i < hiddenNames.Length; i++)
        {
            var child = portalPage.Find(hiddenNames[i]);
            if (child != null)
            {
                child.gameObject.SetActive(visible);
            }
        }

        var mapButtons = mapParent.GetComponentsInChildren<Button>(true);
        for (var i = 0; i < mapButtons.Length; i++)
        {
            mapButtons[i].gameObject.SetActive(visible);
        }

        // Pin glows are siblings of the pin buttons, so hiding only Button
        // objects left marker light behind the transparent information popup.
        for (var i = 0; i < mapParent.childCount; i++)
        {
            var child = mapParent.GetChild(i);
            if (child != null
                && (child.name.StartsWith("Dungeon Pin Glow ", StringComparison.Ordinal)
                    || child.name.StartsWith("Dungeon Route", StringComparison.Ordinal)
                    || child.name.StartsWith("Region Light ", StringComparison.Ordinal)))
            {
                child.gameObject.SetActive(visible);
            }
        }
    }

    private void AddDungeonMapDetail(Transform parent, List<DungeonData> dungeons)
    {
        var region = FindDungeonMapRegion(selectedDungeonRegionKey) ?? DungeonMapRegions()[0];
        var regionDungeons = DungeonsInRegion(dungeons, region.key);
        AddText(parent, DungeonMapBuildLabel, 18, FontStyle.Bold, goodColor, TextAnchor.MiddleLeft, 28);

        if (!dungeonMapZoomed)
        {
            AddText(parent, "지도 탐색", 24, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 36);
            AddText(parent, "처음 지도에 들어오면 던전 위치는 숨겨집니다.\n대륙을 선택하면 해당 대륙으로 확대되고, 확대 상태에서만 던전 핀과 진입 버튼이 나타납니다.", 21, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 120);
            AddText(parent, "대륙별 던전은 이름과 분위기에 맞게 5개씩 배치했습니다.\n예: 숲/정원/뿌리는 그린우드, 얼음/오로라는 서리 대지, 화산/용광로는 용암 고원입니다.", 19, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 150);
            AddText(parent, "표시 조건: 대륙 선택 또는 확대 상태", 22, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 44);
            return;
        }

        AddText(parent, region.name, 31, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 46);
        AddText(parent, "이 대륙의 던전 " + regionDungeons.Count + "개", 21, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 32);

        foreach (var dungeon in regionDungeons)
        {
            AddDungeonMapDetailRow(parent, dungeon);
        }
    }

    private void AddDungeonMapDetailRow(Transform parent, DungeonData dungeon)
    {
        var unlocked = IsDungeonUnlocked(dungeon);
        var bossCleared = IsBossCleared(dungeon);
        var underLevel = player.level < dungeon.recommendedLevel;
        var rowColor = bossCleared ? Rgba(229, 247, 238, 248) : unlocked ? panelColor : Rgba(232, 236, 241, 246);
        var row = AddPanel("Map Dungeon " + dungeon.number, parent, rowColor);
        AddLayoutSize(row, -1, 118);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var info = AddPanel("Map Dungeon Info", row, new Color(0, 0, 0, 0));
        AddLayoutSize(info, -1, -1);
        AddVertical(info, 3, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
        AddText(info, dungeon.number + ". " + dungeon.name, 20, FontStyle.Bold, unlocked ? goldColor : mutedColor, TextAnchor.MiddleLeft, 28);
        AddText(info, "권장 Lv." + dungeon.recommendedLevel + "  구역 " + DungeonFloorCount(dungeon) + (underLevel ? "  위험" : "") + (bossCleared ? "  토벌 완료" : ""), 15, FontStyle.Normal, bossCleared ? goodColor : (underLevel ? goldColor : mutedColor), TextAnchor.MiddleLeft, 24);
        AddText(info, DungeonBossName(dungeon), 15, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 22);

        var enter = AddButton(row, "진입", () => StartCombat(dungeon, false), goodColor);
        AddLayoutSize(enter.GetComponent<RectTransform>(), 104, 58);
        enter.interactable = unlocked && HasDungeonEncounter(dungeon);
    }

    private Button AddMapButton(RectTransform parent, string label, Vector2 percent, Vector2 size, Color color, Action onClick)
    {
        var button = AddButton(parent, label, onClick, color);
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = DungeonMapLocalPoint(parent, percent);
        rect.sizeDelta = size;

        var layout = rect.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.ignoreLayout = true;
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
        }

        return button;
    }

    private Button AddMapRegionButton(RectTransform parent, DungeonMapRegion region, int dungeonCount, bool selected, bool containsRecommended, Action onClick)
    {
        var hit = CreateMapButtonObject("Region Hit " + region.key, parent, new Vector2(region.center.x, region.center.y), new Vector2(DungeonMapBaseWidth * 0.18f, DungeonMapBaseHeight * 0.15f), onClick);
        hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        hit.interactable = dungeonCount > 0;

        var emphasized = selected || containsRecommended;
        AddMapGlow(parent, "Region Emblem Glow " + region.key, region.center, emphasized ? goldColor : region.color, emphasized ? 0.34f : 0.18f, emphasized ? 142f : 118f);
        var emblemSize = emphasized ? 98f : 84f;
        var button = CreateMapButtonObject("Region " + region.key, parent, new Vector2(region.center.x, region.center.y), new Vector2(emblemSize, emblemSize), onClick);
        var image = button.GetComponent<Image>();
        image.sprite = MapCircleSprite();
        var regionSprite = DungeonRegionVisualSprite(region.key);
        if (regionSprite != null)
        {
            image.color = Color.clear;
            var art = AddFlatPanel("Region Emblem Art " + region.key, button.transform, Color.white);
            art.anchorMin = new Vector2(0.5f, 0.5f);
            art.anchorMax = art.anchorMin;
            art.pivot = new Vector2(0.5f, 0.5f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = Vector2.one * emblemSize * 1.32f;
            art.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var artImage = art.GetComponent<Image>();
            artImage.sprite = regionSprite;
            artImage.preserveAspect = true;
            artImage.raycastTarget = false;
        }
        else
        {
            var surface = Color.Lerp(Rgb(246, 250, 251), new Color(region.color.r, region.color.g, region.color.b, 1f), 0.36f);
            image.color = new Color(surface.r, surface.g, surface.b, 0.98f);
            var outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = emphasized ? goldColor : region.color;
            outline.effectDistance = emphasized ? new Vector2(4.2f, -4.2f) : new Vector2(2.8f, -2.8f);

            var shine = AddFlatPanel("Region Emblem Inner " + region.key, button.transform, new Color(region.color.r, region.color.g, region.color.b, 0.30f));
            shine.GetComponent<Image>().sprite = MapCircleSprite();
            shine.GetComponent<Image>().raycastTarget = false;
            Stretch(shine, 11, 11, 11, 11);

            var glyph = AddText(button.transform, DungeonRegionGlyph(region.key), 28, FontStyle.Bold, Rgb(28, 48, 61), TextAnchor.MiddleCenter, emblemSize);
            glyph.raycastTarget = false;
            Stretch(glyph.GetComponent<RectTransform>(), 0, 0, 0, 0);
        }

        var label = AddText(button.transform, (containsRecommended ? "추천 · " : "") + region.name, 21, FontStyle.Bold, emphasized ? goldColor : textColor, TextAnchor.MiddleCenter, 42);
        label.raycastTarget = false;
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = labelRect.anchorMin;
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -10f);
        labelRect.sizeDelta = new Vector2(250f, 42f);

        var countBadge = AddFlatPanel("Region Count " + region.key, button.transform, new Color(goldColor.r, goldColor.g, goldColor.b, 0.96f));
        countBadge.GetComponent<Image>().sprite = MapCircleSprite();
        countBadge.GetComponent<Image>().raycastTarget = false;
        var countRect = countBadge;
        countRect.anchorMin = new Vector2(1f, 1f);
        countRect.anchorMax = new Vector2(1f, 1f);
        countRect.pivot = new Vector2(0.5f, 0.5f);
        countRect.anchoredPosition = new Vector2(-5f, -5f);
        countRect.sizeDelta = new Vector2(34f, 34f);
        countRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var count = AddText(countBadge, dungeonCount.ToString(), 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 34);
        Stretch(count.GetComponent<RectTransform>(), 0, 0, 0, 0);
        button.interactable = dungeonCount > 0;
        return button;
    }

    private string DungeonRegionGlyph(string key)
    {
        switch (key)
        {
            case "frost": return "설";
            case "green": return "숲";
            case "lava": return "화";
            case "elesia": return "엘";
            case "arcadia": return "성";
            case "desert": return "사";
            case "shadow": return "흑";
            case "isles": return "섬";
            case "wind": return "풍";
            default: return "문";
        }
    }

    private Button AddDungeonPinButton(RectTransform parent, DungeonData dungeon, Vector2 percent, Color color, bool unlocked, bool bossCleared, bool isCurrent, bool isRecommended, Action onClick)
    {
        var stateColor = bossCleared ? goodColor : isCurrent ? goldColor : isRecommended ? manaColor : unlocked ? color : mutedColor;
        var emphasized = isCurrent || isRecommended;
        AddMapGlow(parent, "Dungeon Pin Glow " + dungeon.number, percent, stateColor, unlocked ? (emphasized ? 0.42f : 0.24f) : 0.10f, emphasized ? 94f : 72f);
        var hit = CreateMapButtonObject("Dungeon Pin Hit " + dungeon.number, parent, percent, new Vector2(112f, 112f), onClick);
        ApplyDungeonMapCounterScale(hit.GetComponent<RectTransform>(), parent);
        hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        var nodeSize = emphasized ? 68f : 58f;
        var button = CreateMapButtonObject("Dungeon Pin " + dungeon.number, parent, percent, new Vector2(nodeSize, nodeSize), onClick);
        ApplyDungeonMapCounterScale(button.GetComponent<RectTransform>(), parent);
        var image = button.GetComponent<Image>();
        image.sprite = MapCircleSprite();
        var nodeSurface = Color.Lerp(Rgb(244, 249, 251), new Color(stateColor.r, stateColor.g, stateColor.b, 1f), unlocked ? 0.32f : 0.58f);
        image.color = new Color(nodeSurface.r, nodeSurface.g, nodeSurface.b, 0.98f);
        var outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = stateColor;
        outline.effectDistance = emphasized ? new Vector2(4.2f, -4.2f) : new Vector2(2.8f, -2.8f);

        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = Color.Lerp(stateColor, Color.white, 0.34f);
        colors.pressedColor = Color.Lerp(stateColor, Color.black, 0.12f);
        colors.disabledColor = new Color(0.54f, 0.58f, 0.61f, 0.90f);
        button.colors = colors;

        var inner = AddFlatPanel("Pin Inner Light " + dungeon.number, button.transform, new Color(stateColor.r, stateColor.g, stateColor.b, unlocked ? 0.38f : 0.16f));
        inner.GetComponent<Image>().sprite = MapCircleSprite();
        inner.GetComponent<Image>().raycastTarget = false;
        Stretch(inner, 9, 9, 9, 9);

        var number = AddText(button.transform, dungeon.number.ToString(), 20, FontStyle.Bold, unlocked ? Rgb(22, 42, 57) : Rgb(232, 237, 240), TextAnchor.MiddleCenter, nodeSize);
        number.raycastTarget = false;
        Stretch(number.GetComponent<RectTransform>(), 0, 0, 0, 0);

        var stateBadge = AddFlatPanel("Pin State " + dungeon.number, button.transform, new Color(stateColor.r, stateColor.g, stateColor.b, 0.98f));
        stateBadge.GetComponent<Image>().sprite = MapCircleSprite();
        stateBadge.GetComponent<Image>().raycastTarget = false;
        stateBadge.anchorMin = new Vector2(1f, 1f);
        stateBadge.anchorMax = stateBadge.anchorMin;
        stateBadge.pivot = new Vector2(0.5f, 0.5f);
        stateBadge.anchoredPosition = new Vector2(-2f, -2f);
        stateBadge.sizeDelta = new Vector2(28f, 28f);
        stateBadge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var stateText = AddText(stateBadge, DungeonPinStateSymbol(unlocked, bossCleared, isCurrent, isRecommended), 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 28);
        Stretch(stateText.GetComponent<RectTransform>(), 0, 0, 0, 0);

        var label = AddFlatPanel("Pin Label " + dungeon.number, button.transform, new Color(0.96f, 0.98f, 1f, DungeonMapPlateAlpha));
        ConfigureDungeonPinLabel(label, percent);
        var labelImage = label.GetComponent<Image>();
        labelImage.sprite = MapRoundedRectSprite();
        labelImage.raycastTarget = false;
        var labelText = AddText(label, DungeonPinStateLabel(unlocked, bossCleared, isCurrent, isRecommended) + "  " + dungeon.number + ". " + dungeon.name, 18, FontStyle.Bold, unlocked ? textColor : mutedColor, TextAnchor.MiddleCenter, 48);
        labelText.raycastTarget = false;
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 16;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Truncate;
        Stretch(labelText.GetComponent<RectTransform>(), 10, 0, 10, 0);
        return button;
    }

    private string DungeonPinStateSymbol(bool unlocked, bool bossCleared, bool isCurrent, bool isRecommended)
    {
        if (bossCleared) return "완";
        if (isCurrent) return "현";
        if (isRecommended) return "추";
        return unlocked ? "진" : "잠";
    }

    private string DungeonPinStateLabel(bool unlocked, bool bossCleared, bool isCurrent, bool isRecommended)
    {
        if (bossCleared) return "완료";
        if (isCurrent && isRecommended) return "현재·추천";
        if (isCurrent) return "현재";
        if (isRecommended) return "추천";
        return unlocked ? "진입 가능" : "잠금";
    }

    private void ConfigureDungeonPinLabel(RectTransform label, Vector2 percent)
    {
        label.sizeDelta = new Vector2(258f, 50f);

        if (percent.x > 84f)
        {
            label.anchorMin = new Vector2(0f, 0.5f);
            label.anchorMax = label.anchorMin;
            label.pivot = new Vector2(1f, 0.5f);
            label.anchoredPosition = new Vector2(-8f, 0f);
            return;
        }

        if (percent.x < 16f)
        {
            label.anchorMin = new Vector2(1f, 0.5f);
            label.anchorMax = label.anchorMin;
            label.pivot = new Vector2(0f, 0.5f);
            label.anchoredPosition = new Vector2(8f, 0f);
            return;
        }

        if (percent.y > 62f)
        {
            label.anchorMin = new Vector2(0.5f, 1f);
            label.anchorMax = label.anchorMin;
            label.pivot = new Vector2(0.5f, 0f);
            label.anchoredPosition = new Vector2(0f, 8f);
            return;
        }

        label.anchorMin = new Vector2(0.5f, 0f);
        label.anchorMax = label.anchorMin;
        label.pivot = new Vector2(0.5f, 1f);
        label.anchoredPosition = new Vector2(0f, -8f);
    }

    private Button CreateMapButtonObject(string name, RectTransform parent, Vector2 percent, Vector2 size, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = DungeonMapLocalPoint(parent, percent);
        rect.sizeDelta = size;

        var layout = go.GetComponent<LayoutElement>();
        layout.ignoreLayout = true;
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        var button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        button.onClick.AddListener(() => onClick());
        return button;
    }

    private void AddMapGlow(RectTransform parent, string name, Vector2 percent, Color color, float alpha, float size)
    {
        var glowObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        glowObject.transform.SetParent(parent, false);
        var glow = glowObject.GetComponent<RectTransform>();
        glow.anchorMin = new Vector2(0.5f, 0.5f);
        glow.anchorMax = glow.anchorMin;
        glow.pivot = new Vector2(0.5f, 0.5f);
        glow.anchoredPosition = DungeonMapLocalPoint(parent, percent);
        glow.sizeDelta = new Vector2(size, size);
        ApplyDungeonMapCounterScale(glow, parent);
        var image = glowObject.GetComponent<Image>();
        image.sprite = MapCircleSprite();
        image.color = new Color(color.r, color.g, color.b, alpha);
        image.raycastTarget = false;
        glowObject.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    private Sprite MapCircleSprite()
    {
        if (mapCircleSprite != null)
        {
            return mapCircleSprite;
        }

        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        var center = (size - 1) * 0.5f;
        var radius = center - 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01(radius - distance + 1.8f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        mapCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return mapCircleSprite;
    }

    private Sprite MapRoundedRectSprite()
    {
        if (mapRoundedRectSprite != null)
        {
            return mapRoundedRectSprite;
        }

        const int width = 160;
        const int height = 64;
        const float radius = 24f;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        var half = new Vector2(width * 0.5f, height * 0.5f);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var p = new Vector2(Mathf.Abs(x + 0.5f - half.x), Mathf.Abs(y + 0.5f - half.y));
                var q = p - new Vector2(half.x - radius, half.y - radius);
                var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                var distance = outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                var alpha = Mathf.Clamp01(1.8f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        mapRoundedRectSprite = Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        return mapRoundedRectSprite;
    }

    private void TryApplyStreamingSprite(RectTransform target, string relativePath)
    {
        var image = target.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        var path = Path.Combine(Application.streamingAssetsPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return;
        }

        if (!streamingSpriteCache.TryGetValue(path, out var sprite) || sprite == null)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                Destroy(texture);
                return;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = Path.GetFileNameWithoutExtension(path);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = texture.name;
            streamingSpriteCache[path] = sprite;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    private DungeonData NextRecommendedDungeon(List<DungeonData> dungeons)
    {
        foreach (var dungeon in dungeons)
        {
            if (IsDungeonUnlocked(dungeon) && !IsBossCleared(dungeon))
            {
                return dungeon;
            }
        }

        return dungeons.Count > 0 ? dungeons[Mathf.Clamp(player.stage - 1, 0, dungeons.Count - 1)] : null;
    }

    private List<DungeonData> DungeonsInRegion(List<DungeonData> dungeons, string regionKey)
    {
        var result = new List<DungeonData>();
        if (dungeons == null)
        {
            return result;
        }

        foreach (var dungeon in dungeons)
        {
            if (dungeon != null && DungeonRegionKey(dungeon) == regionKey)
            {
                result.Add(dungeon);
            }
        }

        result.Sort((left, right) => left.number.CompareTo(right.number));
        return result;
    }

    private DungeonMapRegion FindDungeonMapRegion(string key)
    {
        foreach (var region in DungeonMapRegions())
        {
            if (region.key == key)
            {
                return region;
            }
        }

        return null;
    }

    private string DungeonRegionKey(DungeonData dungeon)
    {
        if (dungeon == null)
        {
            return "green";
        }

        switch (dungeon.number)
        {
            case 1:
            case 13:
            case 14:
            case 23:
            case 45:
                return "green";
            case 2:
            case 18:
            case 20:
            case 24:
            case 30:
                return "arcadia";
            case 3:
            case 10:
            case 16:
            case 17:
            case 43:
                return "shadow";
            case 4:
            case 7:
            case 11:
            case 19:
            case 42:
                return "lava";
            case 5:
            case 25:
            case 28:
            case 32:
            case 35:
                return "wind";
            case 6:
            case 12:
            case 15:
            case 26:
            case 40:
                return "frost";
            case 8:
            case 21:
            case 31:
            case 36:
            case 41:
                return "elesia";
            case 9:
            case 22:
            case 29:
            case 33:
            case 38:
                return "isles";
            case 27:
            case 34:
            case 37:
            case 39:
            case 44:
                return "desert";
            default:
                return "green";
        }
    }

    private List<DungeonMapRegion> DungeonMapRegions()
    {
        if (cachedDungeonMapRegions != null)
        {
            return cachedDungeonMapRegions;
        }

        cachedDungeonMapRegions = new List<DungeonMapRegion>
        {
            new DungeonMapRegion("frost", "서리 대지", new Vector2(24, 24), new[] { new Vector2(18, 24), new Vector2(24, 18), new Vector2(31, 24), new Vector2(21, 31), new Vector2(34, 32) }, Rgba(148, 197, 255, 232)),
            new DungeonMapRegion("green", "그린우드", new Vector2(49, 24), new[] { new Vector2(42, 24), new Vector2(49, 17), new Vector2(55, 23), new Vector2(45, 31), new Vector2(57, 32) }, Rgba(34, 197, 94, 232)),
            new DungeonMapRegion("lava", "용암 고원", new Vector2(74, 24), new[] { new Vector2(67, 24), new Vector2(74, 17), new Vector2(81, 24), new Vector2(70, 32), new Vector2(83, 33) }, Rgba(239, 68, 68, 232)),
            new DungeonMapRegion("elesia", "엘레시아", new Vector2(24, 51), new[] { new Vector2(17, 50), new Vector2(23, 43), new Vector2(31, 49), new Vector2(21, 59), new Vector2(34, 58) }, Rgba(236, 72, 153, 232)),
            new DungeonMapRegion("arcadia", "아르카디아", new Vector2(50, 52), new[] { new Vector2(43, 48), new Vector2(50, 43), new Vector2(57, 51), new Vector2(46, 61), new Vector2(59, 60) }, Rgba(245, 158, 11, 232)),
            new DungeonMapRegion("desert", "황금 사막", new Vector2(74, 53), new[] { new Vector2(65, 49), new Vector2(74, 43), new Vector2(82, 50), new Vector2(69, 60), new Vector2(84, 60) }, Rgba(251, 191, 36, 232)),
            new DungeonMapRegion("shadow", "그림자 땅", new Vector2(25, 77), new[] { new Vector2(19, 75), new Vector2(24, 70), new Vector2(31, 76), new Vector2(22, 83), new Vector2(34, 82) }, Rgba(124, 58, 237, 232)),
            new DungeonMapRegion("isles", "푸른 군도", new Vector2(49, 78), new[] { new Vector2(43, 73), new Vector2(49, 70), new Vector2(55, 76), new Vector2(47, 85), new Vector2(58, 83) }, Rgba(6, 182, 212, 232)),
            new DungeonMapRegion("wind", "바람의 땅", new Vector2(72, 70), new[] { new Vector2(65, 72), new Vector2(72, 66), new Vector2(80, 70), new Vector2(64, 77), new Vector2(75, 68) }, Rgba(96, 165, 250, 232))
        };
        return cachedDungeonMapRegions;
    }

    private int DungeonFloorCount(DungeonData dungeon)
    {
        return dungeon == null ? 1 : Mathf.Max(1, dungeon.floors);
    }

    private string DungeonBossName(DungeonData dungeon)
    {
        return dungeon != null && dungeon.boss != null && !string.IsNullOrEmpty(dungeon.boss.name)
            ? dungeon.boss.name
            : "알 수 없는 보스";
    }

    private string DungeonMonsterSummary(DungeonData dungeon)
    {
        if (dungeon == null || dungeon.monsters == null || dungeon.monsters.Count == 0)
        {
            return "알 수 없는 적";
        }

        var shown = Mathf.Min(2, dungeon.monsters.Count);
        var names = new List<string>();
        for (var i = 0; i < shown; i++)
        {
            if (dungeon.monsters[i] != null && !string.IsNullOrEmpty(dungeon.monsters[i].name))
            {
                names.Add(dungeon.monsters[i].name);
            }
        }

        return names.Count > 0 ? string.Join(", ", names.ToArray()) : "알 수 없는 적";
    }

    private string DungeonLootRarityText(DungeonData dungeon)
    {
        var dungeonNumber = dungeon != null ? dungeon.number : Mathf.Max(1, player != null ? player.stage : 1);
        var normalChance = Mathf.Min(0.85f, 0.35f + ItemFind());
        var namedName = NamedBossItemName(dungeonNumber);
        var namedText = string.IsNullOrEmpty(namedName) ? "" : " / 네임드 10%: " + namedName;
        return "아이템 획득 희귀도: 일반 " + RarityRangeLabel(NormalRarityPool(dungeonNumber), dungeonNumber)
            + " / 보스 " + RarityRangeLabel(BossRarityPool(), dungeonNumber) + namedText
            + "\n획득 확률: 일반 전투 " + RoundToGameInt(normalChance * 100f) + "% / 보스 100%";
    }

    private string DungeonRecommendedPowerText(DungeonData dungeon)
    {
        var recommended = RecommendedCombatPower(dungeon);
        var current = PlayerCombatPowerEstimate();
        var gap = current - recommended;
        var gapLabel = gap >= 0 ? "여유 " + SignedInt(gap) : "부족 " + Mathf.Abs(gap);
        return "추천 전투력 " + recommended + " / 현재 " + current + " (" + gapLabel + ")"
            + "\n추천 준비: Lv." + dungeon.recommendedLevel + " 이상, 장비 강화와 상태 대응 확인";
    }

    private int RecommendedCombatPower(DungeonData dungeon)
    {
        if (dungeon == null)
        {
            return 1;
        }

        var enemyScore = 0f;
        if (dungeon.monsters != null)
        {
            foreach (var monster in dungeon.monsters)
            {
                enemyScore += EnemyThreatScore(monster, dungeon.recommendedLevel);
            }
        }

        if (dungeon.monsters != null && dungeon.monsters.Count > 0)
        {
            enemyScore /= dungeon.monsters.Count;
        }

        var bossScore = EnemyThreatScore(dungeon.boss, dungeon.recommendedLevel) * 1.18f;
        var floorScore = DungeonFloorCount(dungeon) * 45f;
        var stageScore = dungeon.number * 28f + dungeon.recommendedLevel * 34f;
        return Mathf.Max(1, RoundToGameInt(Mathf.Max(enemyScore, bossScore * 0.72f) + floorScore + stageScore));
    }

    private float EnemyThreatScore(EnemyTemplate enemy, int recommendedLevel)
    {
        if (enemy == null)
        {
            return recommendedLevel * 25f;
        }

        return enemy.hp * 0.08f
            + Mathf.Max(enemy.attack, enemy.magic) * 7f
            + enemy.defense * 8f
            + enemy.speed * 6f
            + enemy.crit * 120f
            + recommendedLevel * 18f
            + EnemyStatusThreat(enemy) * 35f;
    }

    private float EnemyStatusThreat(EnemyTemplate enemy)
    {
        if (enemy == null || enemy.skills == null)
        {
            return 0f;
        }

        var score = 0f;
        foreach (var skill in enemy.skills)
        {
            score += skill.stunChance + skill.freezeChance + skill.silenceChance;
            score += (skill.burnChance + skill.poisonChance + skill.bleedChance + skill.shockChance + skill.manaBurnChance) * 0.65f;
            score += (skill.blindChance + skill.weakenChance + skill.vulnerableChance) * 0.55f;
        }

        return score;
    }

    private int PlayerCombatPowerEstimate()
    {
        if (player == null)
        {
            return 1;
        }

        var score = Attack() + Magic() + Defense() * 2f + MaxHp() * 0.2f + MaxMp() * 0.25f + Speed() * 4f;
        score += (CritChance() + CritDamage() + Evasion() + DamageReduction() + LifeSteal() + ManaRegen() + StatusPower() + ItemFind()) * 100f;
        return Mathf.Max(1, RoundToGameInt(score));
    }

    private string DungeonRewardAndDangerText(DungeonData dungeon)
    {
        var namedName = dungeon != null ? NamedBossItemName(dungeon.number) : "";
        var namedText = string.IsNullOrEmpty(namedName) ? "" : "\n네임드 보스 드랍: " + namedName;
        return "주요 드랍: 무기 / 방어구 / 장신구, 보스는 높은 희귀도 확정" + namedText
            + "\n지역 성향: " + DungeonDropThemeText(dungeon)
            + "\n위험 상태이상: " + DungeonDangerStatusText(dungeon);
    }

    private string DungeonDropThemeText(DungeonData dungeon)
    {
        switch (DungeonRegionKey(dungeon))
        {
            case "frost":
                return "빙결 저항 준비, 방어구와 마나재생 장신구 선호";
            case "green":
                return "회복/중독 대응, 마력 장비와 상태 피해 옵션 선호";
            case "lava":
                return "화상 대응, 공격 장비와 흡혈/피해감소 장신구 선호";
            case "elesia":
                return "독/출혈 대응, 아이템발견과 생존 장신구 선호";
            case "arcadia":
                return "균형형 보상, 무기/방어구 강화 재료 확보에 유리";
            case "desert":
                return "실명/약화 대응, 치명과 회피 장신구 선호";
            case "shadow":
                return "침묵/취약 대응, 속도와 마나 장신구 선호";
            case "isles":
                return "감전/마나 연소 대응, 마나재생과 마력 장비 선호";
            case "wind":
                return "속도전 중심, 속도/회피 장신구와 치명 장비 선호";
            default:
                return "범용 장비와 강화 재료";
        }
    }

    private string DungeonDangerStatusText(DungeonData dungeon)
    {
        var types = new List<string>();
        if (dungeon != null)
        {
            AddEnemyDangerStatuses(types, dungeon.boss);
            if (dungeon.monsters != null)
            {
                foreach (var monster in dungeon.monsters)
                {
                    AddEnemyDangerStatuses(types, monster);
                }
            }
        }

        return types.Count == 0 ? "특이 상태이상 없음" : string.Join(" / ", types.ToArray());
    }

    private void AddEnemyDangerStatuses(List<string> types, EnemyTemplate enemy)
    {
        if (enemy == null || enemy.skills == null)
        {
            return;
        }

        foreach (var skill in enemy.skills)
        {
            AddDangerStatus(types, "stun", skill.stunChance);
            AddDangerStatus(types, "burn", skill.burnChance);
            AddDangerStatus(types, "poison", skill.poisonChance);
            AddDangerStatus(types, "bleed", skill.bleedChance);
            AddDangerStatus(types, "shock", skill.shockChance);
            AddDangerStatus(types, "freeze", skill.freezeChance);
            AddDangerStatus(types, "blind", skill.blindChance);
            AddDangerStatus(types, "weaken", skill.weakenChance);
            AddDangerStatus(types, "vulnerable", skill.vulnerableChance);
            AddDangerStatus(types, "silence", skill.silenceChance);
            AddDangerStatus(types, "mana_burn", skill.manaBurnChance);
        }
    }

    private void AddDangerStatus(List<string> types, string type, float chance)
    {
        if (chance <= 0f)
        {
            return;
        }

        var label = StatusName(type);
        if (!types.Contains(label))
        {
            types.Add(label);
        }
    }

    private string RarityRangeLabel(int[] pool, int dungeonNumber)
    {
        var maxAllowed = MaxRarityForDungeon(dungeonNumber);
        var min = int.MaxValue;
        var max = int.MinValue;
        for (var i = 0; i < pool.Length; i++)
        {
            if (pool[i] > maxAllowed)
            {
                continue;
            }

            min = Mathf.Min(min, pool[i]);
            max = Mathf.Max(max, pool[i]);
        }

        if (min == int.MaxValue)
        {
            min = Mathf.Clamp(maxAllowed, 0, NamedRarity);
            max = min;
        }

        return min == max ? RarityLabel(min) : RarityLabel(min) + "~" + RarityLabel(max);
    }

    private int[] BossRarityPool()
    {
        return new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    }

    private string NamedBossItemName(int stage)
    {
        if (stage == 5) return "제피로스의 폭풍핵";
        if (stage == 10) return "아트라스 균열검";
        if (stage == 15) return "루미나 오로라 성배";
        if (stage == 20) return "제네시온 왕좌갑";
        if (stage == 25) return "이그라스 잿폭풍 인장";
        if (stage == 30) return "아르카이온 심장 파편";
        if (stage == 35) return "월식 군주의 결속구";
        if (stage == 40) return "무한성좌의 예복";
        if (stage == 45) return "근원의 왕관";
        return "";
    }

    private void AddDungeonPageRow(Transform parent, DungeonData dungeon)
    {
        var unlocked = IsDungeonUnlocked(dungeon);
        var bossCleared = IsBossCleared(dungeon);
        var underLevel = player.level < dungeon.recommendedLevel;
        var row = AddPanel("Dungeon Row " + dungeon.number, parent, bossCleared ? Rgba(229, 247, 238, 248) : (unlocked ? panelColor : Rgba(232, 236, 241, 246)));
        AddLayoutSize(row, -1, 122);
        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 14;
        rowLayout.padding = new RectOffset(16, 16, 10, 10);
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        var info = AddPanel("Dungeon Info", row, new Color(0, 0, 0, 0));
        AddLayoutSize(info, -1, -1);
        AddVertical(info, 4, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
        AddText(info, dungeon.number + ". " + dungeon.name, 23, FontStyle.Bold, unlocked ? goldColor : mutedColor, TextAnchor.MiddleLeft, 30);
        AddText(info, "권장 Lv." + dungeon.recommendedLevel + "  구역 " + DungeonFloorCount(dungeon) + (underLevel ? "  위험" : "") + "  보스: " + DungeonBossName(dungeon) + (bossCleared ? "  토벌 완료" : ""), 17, FontStyle.Normal, bossCleared ? goodColor : (underLevel ? goldColor : mutedColor), TextAnchor.MiddleLeft, 24);
        AddText(info, dungeon.description, 16, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 42);

        var actions = AddPanel("Dungeon Actions", row, new Color(0, 0, 0, 0));
        AddLayoutSize(actions, 190, -1);
        var actionLayout = actions.gameObject.AddComponent<GridLayoutGroup>();
        actionLayout.cellSize = new Vector2(180, 46);
        actionLayout.spacing = new Vector2(10, 8);
        actionLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        actionLayout.constraintCount = 1;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;

        var enter = AddButton(actions, "전투 시작", () => StartCombat(dungeon, false), goodColor);
        enter.interactable = unlocked && HasDungeonEncounter(dungeon);
    }

    private void AddDungeonCard(Transform parent, DungeonData dungeon)
    {
        var unlocked = IsDungeonUnlocked(dungeon);
        var underLevel = player.level < dungeon.recommendedLevel;
        var card = AddPanel("Dungeon Card " + dungeon.name, parent, unlocked ? panelColor : Rgba(232, 236, 241, 246));
        AddVertical(card, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));

        AddText(card, dungeon.name, 25, FontStyle.Bold, unlocked ? goldColor : mutedColor, TextAnchor.MiddleLeft, 34);
        AddText(card, "권장 Lv." + dungeon.recommendedLevel + "  구역 " + DungeonFloorCount(dungeon) + (underLevel ? "  위험" : "") + "\n" + dungeon.description, 18, FontStyle.Normal, underLevel ? goldColor : mutedColor, TextAnchor.UpperLeft, 86);
        AddText(card, "일반 적: " + DungeonMonsterSummary(dungeon) + "\n보스: " + DungeonBossName(dungeon), 17, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 54);

        var buttons = AddRow("Dungeon Buttons", card, 10, TextAnchor.MiddleLeft);
        AddLayoutSize(buttons, -1, 56);
        var enter = AddButton(buttons, "전투 시작", () => StartCombat(dungeon, false), goodColor);
        enter.interactable = unlocked && HasDungeonEncounter(dungeon);
    }

    private void StartCombat(DungeonData dungeon, bool bossBattle)
    {
        if (player == null)
        {
            ResetCombatState(true);
            ShowMainMenu();
            return;
        }

        EnsurePlayerData();
        if (currentEnemy != null)
        {
            return;
        }

        if (dungeon == null)
        {
            AddGeneralLog("던전 데이터가 없어 전투를 시작하지 못했습니다.");
            ShowDungeonSelect(true);
            return;
        }

        if (!IsDungeonUnlocked(dungeon))
        {
            AddGeneralLog(dungeon.name + "은(는) 아직 해금되지 않았습니다.");
            ShowDungeonSelect(true);
            return;
        }

        if (bossBattle && !IsBossUnlocked(dungeon))
        {
            AddGeneralLog(dungeon.name + " 보스전은 아직 열리지 않았습니다.");
            ShowDungeonSelect(true);
            return;
        }

        if (!HasDungeonEncounter(dungeon))
        {
            AddGeneralLog("던전 데이터가 부족해 전투를 시작하지 못했습니다.");
            ShowDungeonSelect(true);
            return;
        }

        currentDungeon = dungeon;
        actionLocked = false;
        var floorCount = DungeonFloorCount(dungeon);
        currentDungeonFloor = bossBattle ? floorCount : 1;
        currentEnemyIsBoss = bossBattle || currentDungeonFloor >= floorCount;
        currentEnemy = CreateEnemy(dungeon, currentEnemyIsBoss);
        playerStatusEffects.Clear();
        enemyStatusEffects.Clear();
        battleLog.Clear();
        enemyManaRegenTurnCounter = 0;
        battleLog.Add(dungeon.name + " " + currentDungeonFloor + "층에서 " + currentEnemy.name + "이(가) 나타났습니다.");
        BeginCombatTurnOrder();
    }

    private void BeginCombatTurnOrder()
    {
        var playerSpeed = Speed();
        var enemySpeed = currentEnemy != null ? currentEnemy.speed : 0;
        actionLocked = currentEnemy != null && enemySpeed > playerSpeed;
        combatPresentationPhase = actionLocked
            ? CombatPresentationPhase.EnemyResolving
            : CombatPresentationPhase.PlayerChoice;
        battleLog.Add(actionLocked
            ? "기습! 적의 속도(" + enemySpeed + ")가 플레이어(" + playerSpeed + ")보다 빠릅니다."
            : "선공 획득! 플레이어의 속도(" + playerSpeed + ")가 적(" + enemySpeed + ")보다 빠릅니다.");
        PlayEncounterSound();
        ShowCombat();
        PlayTurnSound(actionLocked);
        if (actionLocked)
        {
            StartEnemyOnlyPresentation();
        }
    }

    private void ShowCombat(bool resultBackdrop = false)
    {
        if (player == null)
        {
            ShowMainMenu();
            return;
        }

        EnsurePlayerData();
        if (currentEnemy == null)
        {
            ShowTown("전투 대상이 없어 마을로 돌아왔습니다.");
            return;
        }

        if (!resultBackdrop)
        {
            currentScreen = AetheriaScreen.Combat;
        }
        ClearRoot();
        BuildCombatScreen(resultBackdrop);
    }

    private string CombatLogText()
    {
        var lines = RecentCombatLogLines();
        return lines.Count == 0 ? "전투 기록이 없습니다." : string.Join("\n", lines.ToArray());
    }

    private List<string> RecentCombatLogLines()
    {
        TrimBattleLog();
        var lines = new List<string>();
        if (battleLog.Count == 0)
        {
            return lines;
        }

        var start = Mathf.Max(0, battleLog.Count - MaxVisibleCombatLogLines);
        for (var i = start; i < battleLog.Count; i++)
        {
            lines.Add(battleLog[i]);
        }
        return lines;
    }

    private void AddCombatLogList(Transform parent)
    {
        var list = AddScrollList("Combat Log Lines", parent, Rgba(241, 247, 251, 235), -1, -1, 4, new RectOffset(12, 20, 10, 10));
        var scroll = list != null && list.parent != null ? list.parent.GetComponent<ScrollRect>() : null;
        var lines = RecentCombatLogLines();
        if (lines.Count == 0)
        {
            AddText(list, "전투 기록이 없습니다.", 19, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 34);
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 0f;
            }
            return;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var newest = i == lines.Count - 1;
            AddText(list, lines[i], newest ? 19 : 18, newest ? FontStyle.Bold : FontStyle.Normal, newest ? textColor : mutedColor, TextAnchor.MiddleLeft, 44);
        }

        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }

    private void TrimBattleLog()
    {
        if (battleLog.Count <= MaxStoredBattleLogLines)
        {
            return;
        }

        battleLog.RemoveRange(0, battleLog.Count - MaxStoredBattleLogLines);
    }

    private void PlayerAttack(string actionName, float multiplier, int mpCost, bool usesMagic)
    {
        PlayerAttack(new SkillState(actionName, mpCost, multiplier, usesMagic));
    }

    private void ExitDungeon()
    {
        if (actionLocked)
        {
            return;
        }

        CancelCombatPresentation();
        var dungeonName = currentDungeon != null ? currentDungeon.name : "던전";
        currentEnemy = null;
        currentDungeon = null;
        currentDungeonFloor = 0;
        currentEnemyIsBoss = false;
        enemyManaRegenTurnCounter = 0;
        playerStatusEffects.Clear();
        enemyStatusEffects.Clear();
        AddGeneralLog(dungeonName + " 탐험을 중단하고 마을로 귀환했습니다.");
        SaveGame();
        ShowTown("던전 탐험을 중단하고 마을로 돌아왔습니다.");
    }

    private void PlayerAttack(SkillState skill)
    {
        if (actionLocked || currentEnemy == null)
        {
            return;
        }

        actionLocked = true;
        var enemySnapshot = currentEnemy;
        var playerHpAtTurnStart = player.hp;
        var playerTurnStatusCue = CombatStatusFxCue(playerStatusEffects);
        var playerTurnStatusRawDamage = CombatTurnStartRawDamage(playerStatusEffects);
        if (ProcessTurnStart(playerStatusEffects, "플레이어", true))
        {
            if (currentScreen != AetheriaScreen.Combat || currentEnemy == null || currentEnemy != enemySnapshot)
            {
                return;
            }

            ExpireActionStatusEffects(playerStatusEffects);
            StartPlayerResolvedPresentation(new CombatPresentationEvent
            {
                actionName = "상태 이상 · 행동 불가",
                attackerIsPlayer = true,
                targetIsPlayer = true,
                statusOnly = true,
                damage = Mathf.Max(0, playerHpAtTurnStart - player.hp),
                absorbedDamage = Mathf.Max(0, playerTurnStatusRawDamage - Mathf.Max(0, playerHpAtTurnStart - player.hp)),
                oldTargetHp = playerHpAtTurnStart,
                newTargetHp = Mathf.Max(0, player.hp),
                statusType = playerTurnStatusCue,
                targetDefeated = player.hp <= 0
            });
            return;
        }

        if (HasStatus(playerStatusEffects, "silence") && skill.mpCost > 0)
        {
            actionLocked = false;
            battleLog.Add("침묵 상태라 스킬을 사용할 수 없습니다.");
            ShowCombat();
            RejectCombatInput("침묵 상태라 스킬을 사용할 수 없습니다.");
            return;
        }

        if (player.mp < skill.mpCost)
        {
            actionLocked = false;
            battleLog.Add("마나가 부족합니다.");
            ShowCombat();
            RejectCombatInput("MP가 부족합니다. 기본 공격을 사용하세요.");
            return;
        }

        player.mp -= skill.mpCost;
        var basePower = skill.magic ? Magic() : Attack();
        if (skill.noDamage)
        {
            var hpBeforeSkill = player.hp;
            ApplySelfStatus(skill, playerStatusEffects, "플레이어", basePower);
            battleLog.Add(skill.name + "을(를) 사용했습니다.");
            ExpireActionStatusEffects(playerStatusEffects);
            StartPlayerResolvedPresentation(new CombatPresentationEvent
            {
                actionName = skill.name,
                attackerIsPlayer = true,
                targetIsPlayer = true,
                magic = true,
                buff = true,
                healAmount = Mathf.Max(0, player.hp - hpBeforeSkill),
                oldTargetHp = hpBeforeSkill,
                newTargetHp = player.hp,
                supportType = CombatSupportFxCue(skill)
            });
            return;
        }

        if (TryBlindMiss(playerStatusEffects, "플레이어"))
        {
            ExpireActionStatusEffects(playerStatusEffects);
            StartPlayerResolvedPresentation(new CombatPresentationEvent
            {
                actionName = skill.name,
                attackerIsPlayer = true,
                targetIsPlayer = false,
                magic = skill.magic,
                missed = true,
                oldTargetHp = currentEnemy.hp,
                newTargetHp = currentEnemy.hp
            });
            return;
        }

        var enemyHpBeforeAttack = currentEnemy.hp;
        var playerHpBeforeLeech = player.hp;
        var critical = skill.forceCrit || UnityEngine.Random.value < Mathf.Clamp01(CritChance() + skill.critBonus);
        var rawDamage = CalculateDamage(basePower, skill.multiplier, currentEnemy.defense, playerStatusEffects, enemyStatusEffects, critical ? 1.5f + CritDamage() : 1f, 0f);
        var damage = AbsorbShield(enemyStatusEffects, rawDamage, "적");
        currentEnemy.hp -= damage;
        if (damage > 0 && skill.lifeStealRatio > 0f)
        {
            var leeched = Mathf.Max(1, RoundToGameInt(damage * skill.lifeStealRatio));
            player.hp = Mathf.Min(MaxHp(), player.hp + leeched);
            battleLog.Add("기술 흡혈로 체력을 " + leeched + " 회복했습니다.");
        }

        var gearLifeSteal = LifeSteal();
        if (damage > 0 && gearLifeSteal > 0f)
        {
            var leeched = Mathf.Max(1, RoundToGameInt(damage * gearLifeSteal));
            player.hp = Mathf.Min(MaxHp(), player.hp + leeched);
            battleLog.Add("흡혈로 체력을 " + leeched + " 회복했습니다.");
        }
        battleLog.Add(skill.name + "으로 " + damage + (critical ? "의 치명타" : "") + " 피해를 주었습니다.");
        ApplySkillStatus(skill, enemyStatusEffects, "적", Attack(), Magic(), 1f + StatusPower());
        ApplySelfStatus(skill, playerStatusEffects, "플레이어", basePower);
        ExpireActionStatusEffects(playerStatusEffects);
        StartPlayerResolvedPresentation(new CombatPresentationEvent
        {
            actionName = skill.name,
            attackerIsPlayer = true,
            targetIsPlayer = false,
            magic = skill.magic,
            critical = critical,
            damage = damage,
            absorbedDamage = Mathf.Max(0, rawDamage - damage),
            healAmount = Mathf.Max(0, player.hp - playerHpBeforeLeech),
            oldTargetHp = enemyHpBeforeAttack,
            newTargetHp = Mathf.Max(0, currentEnemy.hp),
            supportType = CombatSupportFxCue(skill),
            targetDefeated = currentEnemy.hp <= 0
        });
    }

    private IEnumerator EnemyTurn()
    {
        yield return new WaitForSecondsRealtime(0.42f);
        if (currentEnemy == null)
        {
            yield break;
        }

        var enemySnapshot = currentEnemy;
        var enemyHpBeforeTurnStatus = currentEnemy.hp;
        var enemyTurnStatusCue = CombatStatusFxCue(enemyStatusEffects);
        var enemyTurnStatusRawDamage = CombatTurnStartRawDamage(enemyStatusEffects);
        var enemyActionBlocked = ProcessTurnStart(enemyStatusEffects, currentEnemy.name, false);
        var enemyTurnStatusDamage = Mathf.Max(0, enemyHpBeforeTurnStatus - currentEnemy.hp);
        var enemyTurnStatusAbsorbed = Mathf.Max(0, enemyTurnStatusRawDamage - enemyTurnStatusDamage);
        if (currentEnemy.hp <= 0)
        {
            yield return ShowEnemyResolvedPresentation(new CombatPresentationEvent
            {
                actionName = "지속 피해",
                attackerIsPlayer = false,
                targetIsPlayer = false,
                statusOnly = true,
                damage = enemyTurnStatusDamage,
                absorbedDamage = enemyTurnStatusAbsorbed,
                oldTargetHp = enemyHpBeforeTurnStatus,
                newTargetHp = Mathf.Max(0, currentEnemy.hp),
                statusType = enemyTurnStatusCue,
                targetDefeated = true
            });
            if (!IsSameCombat(enemySnapshot))
            {
                yield break;
            }
            WinCombat();
            yield break;
        }

        if (enemyTurnStatusDamage > 0 || enemyTurnStatusAbsorbed > 0)
        {
            yield return ShowEnemyResolvedPresentation(new CombatPresentationEvent
            {
                actionName = "지속 피해",
                attackerIsPlayer = false,
                targetIsPlayer = false,
                statusOnly = true,
                damage = enemyTurnStatusDamage,
                absorbedDamage = enemyTurnStatusAbsorbed,
                oldTargetHp = enemyHpBeforeTurnStatus,
                newTargetHp = Mathf.Max(0, currentEnemy.hp),
                statusType = enemyTurnStatusCue
            });
            if (!IsSameCombat(enemySnapshot))
            {
                yield break;
            }
        }

        if (enemyActionBlocked)
        {
            ExpireActionStatusEffects(enemyStatusEffects);
            yield return ShowEnemyResolvedPresentation(new CombatPresentationEvent
            {
                actionName = "상태 이상 · 행동 불가",
                attackerIsPlayer = false,
                targetIsPlayer = false,
                statusOnly = true,
                damage = 0,
                oldTargetHp = currentEnemy.hp,
                newTargetHp = Mathf.Max(0, currentEnemy.hp),
                statusType = enemyTurnStatusCue
            });
            if (IsSameCombat(enemySnapshot))
            {
                CompleteEnemyTurnPresentation();
            }
            yield break;
        }

        RegenerateEnemyManaOnSchedule();

        SkillState skill = null;
        if (HasStatus(enemyStatusEffects, "silence"))
        {
            if (currentEnemy.skills != null && currentEnemy.skills.Count > 0)
            {
                battleLog.Add(currentEnemy.name + "은(는) 침묵 상태라 스킬을 사용할 수 없습니다.");
            }
        }
        else
        {
            skill = PickEnemySkill();
        }

        var usesMagic = skill != null && skill.magic;
        var attackName = skill != null ? skill.name : currentEnemy.name + "의 공격";
        var multiplier = skill != null ? skill.multiplier : 1.0f;
        var basePower = usesMagic ? currentEnemy.magic : currentEnemy.attack;

        if (skill != null)
        {
            currentEnemy.mp = Mathf.Max(0, currentEnemy.mp - EnemySkillCost(skill));
        }

        if (skill != null && skill.noDamage)
        {
            var hpBeforeSkill = currentEnemy.hp;
            ApplySelfStatus(skill, enemyStatusEffects, currentEnemy.name, basePower);
            battleLog.Add(attackName + "을(를) 사용했습니다.");
            ExpireActionStatusEffects(enemyStatusEffects);
            yield return ShowEnemyResolvedPresentation(new CombatPresentationEvent
            {
                actionName = attackName,
                attackerIsPlayer = false,
                targetIsPlayer = false,
                magic = true,
                buff = true,
                healAmount = Mathf.Max(0, currentEnemy.hp - hpBeforeSkill),
                oldTargetHp = hpBeforeSkill,
                newTargetHp = currentEnemy.hp,
                supportType = CombatSupportFxCue(skill)
            });
            if (IsSameCombat(enemySnapshot))
            {
                CompleteEnemyTurnPresentation();
            }
            yield break;
        }

        if (TryBlindMiss(enemyStatusEffects, currentEnemy.name))
        {
            ExpireActionStatusEffects(enemyStatusEffects);
            yield return ShowEnemyResolvedPresentation(new CombatPresentationEvent
            {
                actionName = attackName,
                attackerIsPlayer = false,
                targetIsPlayer = true,
                magic = usesMagic,
                missed = true,
                oldTargetHp = player.hp,
                newTargetHp = player.hp
            });
            if (IsSameCombat(enemySnapshot))
            {
                CompleteEnemyTurnPresentation();
            }
            yield break;
        }

        var effectiveEvasion = EffectiveEvasion();
        if (effectiveEvasion > 0f && UnityEngine.Random.value < effectiveEvasion)
        {
            battleLog.Add("회피율 효과로 공격을 피했습니다.");
            ExpireActionStatusEffects(enemyStatusEffects);
            yield return ShowEnemyResolvedPresentation(new CombatPresentationEvent
            {
                actionName = attackName,
                attackerIsPlayer = false,
                targetIsPlayer = true,
                magic = usesMagic,
                missed = true,
                oldTargetHp = player.hp,
                newTargetHp = player.hp
            });
            if (IsSameCombat(enemySnapshot))
            {
                CompleteEnemyTurnPresentation();
            }
            yield break;
        }

        var playerHpBeforeAttack = player.hp;
        var critical = (skill != null && skill.forceCrit) || UnityEngine.Random.value < Mathf.Clamp01(currentEnemy.crit + (skill != null ? skill.critBonus : 0f));
        var rawDamage = CalculateDamage(basePower, multiplier, Defense(), enemyStatusEffects, playerStatusEffects, critical ? 1.5f : 1f, DamageReduction());
        var damage = AbsorbShield(playerStatusEffects, rawDamage, "플레이어");
        player.hp -= damage;
        battleLog.Add(attackName + (critical ? " 치명타! " : "으로 ") + damage + " 피해를 받았습니다.");
        if (skill != null)
        {
            ApplySkillStatus(skill, playerStatusEffects, "플레이어", currentEnemy.attack, currentEnemy.magic, 1f);
            ApplySelfStatus(skill, enemyStatusEffects, currentEnemy.name, basePower);
        }
        ExpireActionStatusEffects(enemyStatusEffects);
        yield return ShowEnemyResolvedPresentation(new CombatPresentationEvent
        {
            actionName = attackName,
            attackerIsPlayer = false,
            targetIsPlayer = true,
            magic = usesMagic,
            critical = critical,
            damage = damage,
            absorbedDamage = Mathf.Max(0, rawDamage - damage),
            oldTargetHp = playerHpBeforeAttack,
            newTargetHp = Mathf.Max(0, player.hp),
            supportType = CombatSupportFxCue(skill),
            targetDefeated = player.hp <= 0
        });

        if (!IsSameCombat(enemySnapshot))
        {
            yield break;
        }
        if (player.hp <= 0)
        {
            yield return new WaitForSecondsRealtime(0.20f);
            LoseCombat();
            yield break;
        }

        CompleteEnemyTurnPresentation();
    }

    private void WinCombat()
    {
        ClearPendingCombatPresentation();
        actionLocked = false;
        combatPresentationPhase = CombatPresentationPhase.Result;
        if (currentEnemy == null)
        {
            ShowTown("전투 대상이 사라져 마을로 돌아왔습니다.");
            return;
        }

        var enemy = currentEnemy;
        enemyStatusEffects.Clear();
        playerStatusEffects.Clear();
        player.gold += enemy.gold;
        player.xp += enemy.xp;

        var reward = "";
        var lootChance = currentEnemyIsBoss ? 1f : Mathf.Min(0.85f, 0.35f + ItemFind());
        var lootDropped = UnityEngine.Random.value < lootChance;
        if (lootDropped)
        {
            var dungeonNumber = currentDungeon != null ? currentDungeon.number : player.stage;
            var item = currentEnemyIsBoss ? CreateBossItem(dungeonNumber) : CreateRandomItem(dungeonNumber);
            reward = AddItemToInventoryOrConvert(item);
        }

        while (player.xp >= XpToNext())
        {
            player.xp -= XpToNext();
            player.level++;
            player.hp = MaxHp();
            player.mp = MaxMp();
            reward += " 레벨 업!";
        }

        if (currentDungeon != null && !currentEnemyIsBoss && currentDungeonFloor < DungeonFloorCount(currentDungeon))
        {
            var clearedFloor = currentDungeonFloor;
            var nextFloor = currentDungeonFloor + 1;
            SaveGame();
            ShowVictoryResult("전투 승리", currentDungeon.name + " " + clearedFloor + "층 돌파\n경험치 +" + enemy.xp + " / 골드 +" + enemy.gold + reward, "다음 층", () =>
            {
                currentDungeonFloor = nextFloor;
                currentEnemyIsBoss = currentDungeonFloor >= DungeonFloorCount(currentDungeon);
                currentEnemy = CreateEnemy(currentDungeon, currentEnemyIsBoss);
                battleLog.Clear();
                enemyManaRegenTurnCounter = 0;
                battleLog.Add(clearedFloor + "층 돌파: " + enemy.gold + "G 획득." + reward);
                battleLog.Add(currentDungeon.name + " " + currentDungeonFloor + "층에서 " + currentEnemy.name + "이(가) 나타났습니다.");
                BeginCombatTurnOrder();
            });
            return;
        }

        if (currentDungeon != null)
        {
            if (currentEnemyIsBoss)
            {
                player.stage = Mathf.Max(player.stage, currentDungeon.number + 1);
                SetDungeonProgress(currentDungeon.number, true, true);
                AddGeneralLog(currentDungeon.name + " 보스 토벌 완료");
            }
            else
            {
                SetDungeonProgress(currentDungeon.number, true, false);
                AddGeneralLog(currentDungeon.name + " 모든 층 돌파");
            }
        }
        else
        {
            player.stage++;
        }

        SaveGame();
        var clearMessage = currentDungeon == null
            ? "전투에서 승리했습니다."
            : (currentEnemyIsBoss
                ? currentDungeon.name + " 보스를 쓰러뜨리고 다음 던전을 열었습니다."
                : currentDungeon.name + " 구역을 돌파했습니다.");
        if (currentEnemyIsBoss && currentDungeon != null && currentDungeon.number >= Dungeons().Count)
        {
            SaveGame();
            ShowVictoryResult("최종 보스 토벌", clearMessage + "\n경험치 +" + enemy.xp + " / 골드 +" + enemy.gold + reward, "엔딩 보기", () => ShowGameClear(clearMessage + " " + enemy.gold + "G를 획득했습니다." + reward));
            return;
        }

        ShowVictoryResult("전투 승리", clearMessage + "\n경험치 +" + enemy.xp + " / 골드 +" + enemy.gold + reward, "마을로", () => ShowTown(clearMessage + " " + enemy.gold + "G를 획득했습니다." + reward));
    }

    private void ShowVictoryResult(string title, string message, string buttonLabel, Action nextAction)
    {
        EnsurePlayerData();
        currentScreen = AetheriaScreen.Victory;
        ClearRoot();
        PlayVictorySound();
        message = message ?? "";

        var page = AddPanel("Victory Result", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 12, TextAnchor.UpperCenter, new RectOffset(64, 64, 30, 30));

        var heading = AddRow("Victory Heading", page, 16, TextAnchor.MiddleLeft);
        AddLayoutSize(heading, -1, 92);
        heading.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        AddText(heading, title, 52, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 82);
        AddResultChip(heading, "WIN", "승리 기록", player.heroClass, ActiveCharacterAccent(goodColor), 300, 68);

        var stage = AddRow("Victory Stage", page, 24, TextAnchor.UpperCenter);
        AddLayoutSize(stage, -1, -1);
        stage.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var heroColumn = AddPanel("Victory Character Showcase", stage, Color.clear);
        AddLayoutSize(heroColumn, 650, -1);
        AddVertical(heroColumn, 8, TextAnchor.UpperCenter, new RectOffset(16, 16, 6, 6));
        AddCharacterArt(heroColumn, player.portraitName, "victory", 600, 760);
        AddText(heroColumn, player.heroName + "  ·  " + player.heroClass, 24, FontStyle.Bold, ActiveCharacterAccent(goldColor), TextAnchor.MiddleCenter, 42);

        var summary = AddPanel("Victory Reward Summary", stage, Color.clear);
        AddLayoutSize(summary, -1, -1);
        AddVertical(summary, 10, TextAnchor.UpperLeft, new RectOffset(16, 16, 10, 10));

        var messagePlate = AddPanel("Victory Message Read Plate", summary, Color.clear);
        AddLayoutSize(messagePlate, -1, 126);
        AddResultReadPlate(messagePlate, Rgb(235, 248, 240));
        AddVertical(messagePlate, 4, TextAnchor.MiddleLeft, new RectOffset(18, 18, 12, 12));
        AddText(messagePlate, "전투 결과", 21, FontStyle.Bold, goodColor, TextAnchor.MiddleLeft, 30);
        AddText(messagePlate, message, 20, FontStyle.Bold, textColor, TextAnchor.UpperLeft, 68);

        AddText(summary, "획득 보상", 26, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 36);
        var rewardGrid = AddPanel("Victory Reward Icons", summary, Color.clear);
        AddLayoutSize(rewardGrid, -1, 194);
        var rewardLayout = rewardGrid.gameObject.AddComponent<GridLayoutGroup>();
        rewardLayout.cellSize = new Vector2(430, 88);
        rewardLayout.spacing = new Vector2(12, 12);
        rewardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rewardLayout.constraintCount = 2;
        rewardLayout.childAlignment = TextAnchor.UpperLeft;
        var xpGain = ResultGainFromMessage(message, "경험치 +");
        var goldGain = ResultGainFromMessage(message, "골드 +");
        var lootEarned = message.Contains("발견했습니다") || message.Contains("전환했습니다") || message.Contains("[");
        var leveledUp = message.Contains("레벨 업");
        AddResultChip(rewardGrid, "XP", "경험치", xpGain > 0 ? "+" + xpGain : "획득", manaColor, 430, 88);
        AddResultChip(rewardGrid, "G", "골드", goldGain > 0 ? "+" + goldGain : "획득", goldColor, 430, 88);
        AddResultChip(rewardGrid, "ITEM", "전리품", lootEarned ? "장비 획득" : "추가 없음", lootEarned ? neonPurple : mutedColor, 430, 88);
        AddResultChip(rewardGrid, "LV", "성장", leveledUp ? "레벨 상승" : "Lv." + player.level, leveledUp ? goodColor : mutedColor, 430, 88);

        AddText(summary, "현재 원정 상태", 23, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 32);
        var statusGrid = AddPanel("Victory Status Chips", summary, Color.clear);
        AddLayoutSize(statusGrid, -1, 84);
        var statusLayout = statusGrid.gameObject.AddComponent<GridLayoutGroup>();
        statusLayout.cellSize = new Vector2(280, 78);
        statusLayout.spacing = new Vector2(12, 0);
        statusLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        statusLayout.constraintCount = 3;
        statusLayout.childAlignment = TextAnchor.UpperLeft;
        AddResultChip(statusGrid, "HP", "체력", player.hp + " / " + MaxHp(), goodColor, 280, 78);
        AddResultChip(statusGrid, "MP", "마나", player.mp + " / " + MaxMp(), manaColor, 280, 78);
        AddResultChip(statusGrid, "BAG", "가방", InventoryCountLabel(), neonPurple, 280, 78);

        AddResultObjectButton(summary, ResultActionIcon(buttonLabel), buttonLabel, ResultActionHint(buttonLabel), () =>
        {
            currentEnemy = null;
            if (nextAction != null)
            {
                nextAction();
            }
            else
            {
                ShowTown("전투 결과를 확인하고 마을로 돌아왔습니다.");
            }
        }, goodColor, -1, 100);
    }

    private void LoseCombat()
    {
        ClearPendingCombatPresentation();
        actionLocked = false;
        combatPresentationPhase = CombatPresentationPhase.Result;
        var lostGold = Mathf.Min(player.gold, RoundToGameInt(player.gold * 0.15f));
        player.gold -= lostGold;
        player.hp = RoundToGameInt(MaxHp() * 0.3f);
        player.mp = RoundToGameInt(MaxMp() * 0.3f);
        enemyStatusEffects.Clear();
        playerStatusEffects.Clear();
        SaveGame();
        ShowDefeatResult(lostGold);
    }

    private void ShowDefeatResult(int lostGold)
    {
        EnsurePlayerData();
        currentScreen = AetheriaScreen.Defeat;
        ClearRoot();
        PlayDefeatSound();

        var page = AddPanel("Defeat Result", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        var veil = AddFlatPanel("Defeat Desaturation Veil", page, new Color(0.80f, 0.84f, 0.88f, 0.12f));
        Stretch(veil, 0, 0, 0, 0);
        veil.GetComponent<Image>().raycastTarget = false;
        veil.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        AddVertical(page, 12, TextAnchor.UpperCenter, new RectOffset(64, 64, 30, 30));

        var heading = AddRow("Defeat Heading", page, 16, TextAnchor.MiddleLeft);
        AddLayoutSize(heading, -1, 92);
        heading.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        AddText(heading, "전투 패배", 52, FontStyle.Bold, dangerColor, TextAnchor.MiddleLeft, 82);
        AddResultChip(heading, "SAFE", "구조 완료", "장비 보존", mutedColor, 300, 68);

        var stage = AddRow("Defeat Stage", page, 24, TextAnchor.UpperCenter);
        AddLayoutSize(stage, -1, -1);
        stage.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var heroColumn = AddPanel("Defeat Character Showcase", stage, Color.clear);
        AddLayoutSize(heroColumn, 650, -1);
        AddVertical(heroColumn, 8, TextAnchor.UpperCenter, new RectOffset(16, 16, 6, 6));
        AddCharacterArt(heroColumn, player.portraitName, "defeat", 600, 760);
        AddText(heroColumn, player.heroName + "  ·  " + player.heroClass, 24, FontStyle.Bold, ActiveCharacterAccent(mutedColor), TextAnchor.MiddleCenter, 42);

        var summary = AddPanel("Defeat Rescue Summary", stage, Color.clear);
        AddLayoutSize(summary, -1, -1);
        AddVertical(summary, 10, TextAnchor.UpperLeft, new RectOffset(16, 16, 10, 10));

        var messagePlate = AddPanel("Defeat Message Read Plate", summary, Color.clear);
        AddLayoutSize(messagePlate, -1, 136);
        AddResultReadPlate(messagePlate, Rgb(248, 236, 238));
        AddVertical(messagePlate, 4, TextAnchor.MiddleLeft, new RectOffset(18, 18, 12, 12));
        AddText(messagePlate, "차원 구조대가 귀환시켰습니다", 22, FontStyle.Bold, dangerColor, TextAnchor.MiddleLeft, 34);
        AddText(messagePlate, "치료비와 탈출 비용으로 " + lostGold + "G를 잃었습니다.\n장비와 진행 기록은 안전하게 보존되었습니다.", 20, FontStyle.Bold, textColor, TextAnchor.UpperLeft, 76);

        AddText(summary, "손실과 구조 상태", 26, FontStyle.Bold, dangerColor, TextAnchor.MiddleLeft, 36);
        var lossGrid = AddPanel("Defeat Loss Icons", summary, Color.clear);
        AddLayoutSize(lossGrid, -1, 194);
        var lossLayout = lossGrid.gameObject.AddComponent<GridLayoutGroup>();
        lossLayout.cellSize = new Vector2(430, 88);
        lossLayout.spacing = new Vector2(12, 12);
        lossLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        lossLayout.constraintCount = 2;
        lossLayout.childAlignment = TextAnchor.UpperLeft;
        AddResultChip(lossGrid, "G", "잃은 골드", "-" + lostGold, dangerColor, 430, 88);
        AddResultChip(lossGrid, "HP", "구조 후 체력", player.hp + " / " + MaxHp(), goodColor, 430, 88);
        AddResultChip(lossGrid, "MP", "구조 후 마나", player.mp + " / " + MaxMp(), manaColor, 430, 88);
        AddResultChip(lossGrid, "GEAR", "장비 상태", "모두 보존", goldColor, 430, 88);

        AddText(summary, "다음 행동", 23, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 32);
        AddResultObjectButton(summary, "GATE", "마을로 귀환", "구조 포털을 열고 회복 거점으로 이동", () =>
        {
            currentEnemy = null;
            ShowTown("패배했습니다. 마을 치유사들이 당신을 구조했습니다. " + lostGold + "G를 잃었습니다.");
        }, panelAltColor, -1, 100);
    }

    private void ShowGameClear(string message)
    {
        EnsurePlayerData();
        currentScreen = AetheriaScreen.GameClear;
        ClearRoot();
        message = message ?? "";

        var page = AddPanel("Game Clear", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 12, TextAnchor.UpperCenter, new RectOffset(56, 56, 24, 24));

        var heading = AddRow("Game Clear Heading", page, 16, TextAnchor.MiddleLeft);
        AddLayoutSize(heading, -1, 104);
        heading.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        heading.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
        AddAetheriaCrestIcon(heading, "Aetheria Conquest Crest", 86f, 86f, 1f);
        AddText(heading, "AETHERIA 정복 완료", 58, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 92);
        AddResultChip(heading, "★", "정복자", player.heroName, ActiveCharacterAccent(goldColor), 330, 72);

        var stage = AddRow("Game Clear Stage", page, 24, TextAnchor.UpperCenter);
        AddLayoutSize(stage, -1, -1);
        stage.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var heroColumn = AddPanel("Game Clear Character Showcase", stage, Color.clear);
        AddLayoutSize(heroColumn, 640, -1);
        AddVertical(heroColumn, 8, TextAnchor.UpperCenter, new RectOffset(12, 12, 4, 4));
        AddCharacterArt(heroColumn, player.portraitName, "victory", 590, 750);
        AddText(heroColumn, player.heroName + "  ·  " + player.heroClass + "  ·  Lv." + player.level, 24, FontStyle.Bold, ActiveCharacterAccent(goldColor), TextAnchor.MiddleCenter, 42);

        var summary = AddPanel("Game Clear Conquest Summary", stage, Color.clear);
        AddLayoutSize(summary, -1, -1);
        AddVertical(summary, 8, TextAnchor.UpperLeft, new RectOffset(14, 14, 6, 6));
        ApplyCharacterThemePanelFrame(summary, null, true);

        var messagePlate = AddPanel("Game Clear Message Read Plate", summary, Color.clear);
        AddLayoutSize(messagePlate, -1, 102);
        AddResultReadPlate(messagePlate, Rgb(251, 246, 229));
        AddVertical(messagePlate, 3, TextAnchor.MiddleLeft, new RectOffset(18, 18, 10, 10));
        AddText(messagePlate, "최종 균열 봉인", 22, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 28);
        AddText(messagePlate, message, 20, FontStyle.Bold, textColor, TextAnchor.UpperLeft, 50);

        AddText(summary, "9개 차원 정복 기록", 25, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 34);
        var regionGrid = AddPanel("Game Clear Region Seals", summary, Color.clear);
        AddLayoutSize(regionGrid, -1, 258);
        var regionLayout = regionGrid.gameObject.AddComponent<GridLayoutGroup>();
        regionLayout.cellSize = new Vector2(280, 78);
        regionLayout.spacing = new Vector2(12, 12);
        regionLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        regionLayout.constraintCount = 3;
        regionLayout.childAlignment = TextAnchor.UpperLeft;
        AddGameClearSealNetwork(regionGrid);
        var conqueredRegions = DungeonMapRegions();
        for (var i = 0; i < conqueredRegions.Count; i++)
        {
            AddGameClearRegionSeal(regionGrid, conqueredRegions[i], i + 1);
        }

        var finalStatus = AddPanel("Game Clear Status Chips", summary, Color.clear);
        AddLayoutSize(finalStatus, -1, 84);
        var finalStatusLayout = finalStatus.gameObject.AddComponent<GridLayoutGroup>();
        finalStatusLayout.cellSize = new Vector2(280, 78);
        finalStatusLayout.spacing = new Vector2(12, 0);
        finalStatusLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        finalStatusLayout.constraintCount = 3;
        finalStatusLayout.childAlignment = TextAnchor.UpperLeft;
        AddResultChip(finalStatus, "LV", "최종 레벨", player.level.ToString(), goodColor, 280, 78);
        AddResultChip(finalStatus, "G", "보유 골드", player.gold + " G", goldColor, 280, 78);
        AddResultChip(finalStatus, "BAG", "전리품", InventoryCountLabel(), neonPurple, 280, 78);

        AddText(summary, "정복 이후에도 마을에서 초월 세팅을 완성할 수 있습니다.", 18, FontStyle.Bold, mutedColor, TextAnchor.MiddleLeft, 30);
        var buttons = AddRow("Game Clear Object Buttons", summary, 14, TextAnchor.MiddleLeft);
        AddLayoutSize(buttons, -1, 100);
        buttons.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        buttons.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
        AddResultObjectButton(buttons, "GATE", "마을로", "정복 기록을 들고 귀환", () => ShowTown("정복 기록을 가지고 마을로 돌아왔습니다."), goodColor, 430, 96);
        AddResultObjectButton(buttons, "A", "메인 메뉴", "새로운 원정 기록 선택", ShowMainMenu, panelAltColor, 430, 96);
    }

    private void AddResultReadPlate(RectTransform target, Color tint)
    {
        if (target == null)
        {
            return;
        }

        var plate = AddFlatPanel(target.name + " Backplate", target, new Color(tint.r, tint.g, tint.b, 0.30f));
        plate.SetAsFirstSibling();
        Stretch(plate, 0, 0, 0, 0);
        plate.GetComponent<Image>().raycastTarget = false;
        plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private RectTransform AddResultChip(Transform parent, string icon, string label, string value, Color accent, float width, float height)
    {
        var surface = Color.Lerp(Rgb(247, 250, 252), new Color(accent.r, accent.g, accent.b, 1f), 0.24f);
        surface.a = 0.30f;
        var chip = AddFlatPanel("Result Chip " + label, parent, surface);
        chip.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(chip, width, height);

        var layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 12, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var emblem = AddFlatPanel("Result Chip Emblem", chip, new Color(accent.r, accent.g, accent.b, 0.30f));
        emblem.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(emblem, 64, height - 16f);
        var emblemText = AddText(emblem, icon, icon.Length > 3 ? 16 : 22, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, height - 16f);
        Stretch(emblemText.GetComponent<RectTransform>(), 3, 2, 3, 2);

        var copy = AddPanel("Result Chip Copy", chip, Color.clear);
        AddLayoutSize(copy, -1, -1);
        AddVertical(copy, 1, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
        AddText(copy, label, 18, FontStyle.Bold, mutedColor, TextAnchor.MiddleLeft, 23);
        AddText(copy, value, 20, FontStyle.Bold, accent, TextAnchor.MiddleLeft, 27);
        return chip;
    }

    private Button AddResultObjectButton(Transform parent, string icon, string label, string hint, Action onClick, Color accent, float width, float height)
    {
        var isMenu = (!string.IsNullOrEmpty(label) && label.Contains("메인 메뉴")) || icon == "A";
        var isTown = !string.IsNullOrEmpty(label) && label.Contains("마을");
        var role = isMenu ? VisualActionRole.Menu : isTown ? VisualActionRole.Inn : VisualActionRole.Portal;
        var visualKey = isMenu ? "menu" : isTown ? "inn" : "portal";
        return AddObjectActionButton(
            parent,
            label + "\n" + hint,
            onClick,
            accent,
            role,
            visualKey,
            width,
            height);
    }

    private int ResultGainFromMessage(string message, string marker)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(marker))
        {
            return 0;
        }

        var markerIndex = message.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return 0;
        }

        var index = markerIndex + marker.Length;
        var value = 0;
        var foundDigit = false;
        while (index < message.Length && char.IsDigit(message[index]))
        {
            foundDigit = true;
            value = value * 10 + (message[index] - '0');
            index++;
        }
        return foundDigit ? value : 0;
    }

    private string ResultActionIcon(string buttonLabel)
    {
        if (!string.IsNullOrEmpty(buttonLabel) && buttonLabel.Contains("엔딩")) return "★";
        if (!string.IsNullOrEmpty(buttonLabel) && buttonLabel.Contains("다음")) return "UP";
        return "GATE";
    }

    private string ResultActionHint(string buttonLabel)
    {
        if (!string.IsNullOrEmpty(buttonLabel) && buttonLabel.Contains("엔딩")) return "정복의 결말 확인";
        if (!string.IsNullOrEmpty(buttonLabel) && buttonLabel.Contains("다음")) return "열린 계단으로 계속 전진";
        return "포털을 통해 마을로 귀환";
    }

    private void AddGameClearRegionSeal(Transform parent, DungeonMapRegion region, int sequence)
    {
        if (region == null)
        {
            return;
        }

        var chip = AddResultChip(parent, sequence.ToString("00"), region.name, "정복 완료", region.color, 280, 78);
        var chipImage = chip != null ? chip.GetComponent<Image>() : null;
        if (chipImage != null)
        {
            chipImage.color = Color.clear;
        }
        if (chip != null)
        {
            var sealRule = AddFlatPanel("Conquest Seal Accent", chip, new Color(region.color.r, region.color.g, region.color.b, 0.62f));
            sealRule.anchorMin = new Vector2(0.16f, 0f);
            sealRule.anchorMax = new Vector2(0.84f, 0f);
            sealRule.pivot = new Vector2(0.5f, 0f);
            sealRule.anchoredPosition = new Vector2(0f, 4f);
            sealRule.sizeDelta = new Vector2(0f, 3f);
            sealRule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            sealRule.GetComponent<Image>().raycastTarget = false;
        }
        var emblem = chip != null ? chip.Find("Result Chip Emblem") : null;
        var regionSprite = DungeonRegionVisualSprite(region.key);
        if (emblem != null && regionSprite != null)
        {
            var emblemImage = emblem.GetComponent<Image>();
            emblemImage.sprite = regionSprite;
            emblemImage.color = Color.white;
            emblemImage.preserveAspect = true;
            var emblemLabel = emblem.GetComponentInChildren<Text>();
            if (emblemLabel != null)
            {
                emblemLabel.gameObject.SetActive(false);
            }
        }
    }

    private void Rest()
    {
        EnsurePlayerData();
        if (player.hp >= MaxHp() && player.mp >= MaxMp())
        {
            ShowTown("체력과 마나가 이미 가득 차 있습니다.");
            return;
        }

        if (player.gold < 25)
        {
            ShowTown("휴식할 골드가 부족합니다.");
            return;
        }

        player.gold -= 25;
        player.hp = MaxHp();
        player.mp = MaxMp();
        SaveGame();
        ShowTown("체력과 마나를 모두 회복했습니다.");
    }

    private void EquipItem(int index)
    {
        EnsurePlayerData();
        EquipItemToSlot(index, 0);
    }

    private void EquipItemToSlot(int index, int accessorySlot)
    {
        EnsurePlayerData();
        if (index < 0 || index >= player.inventory.Count)
        {
            return;
        }

        var item = player.inventory[index];
        if (item == null)
        {
            player.inventory.RemoveAt(index);
            selectedInventoryIndex = -1;
            SaveGame();
            ShowInventory("비정상 아이템을 정리했습니다.");
            return;
        }

        NormalizeItemStats(item);
        player.inventory.RemoveAt(index);
        ItemState replaced = null;

        if (item.type == "Weapon")
        {
            replaced = player.weapon;
            player.weapon = item;
        }
        else if (item.type == "Armor")
        {
            replaced = player.armor;
            player.armor = item;
        }
        else
        {
            if (accessorySlot == 1)
            {
                replaced = player.charm;
                player.charm = item;
            }
            else if (accessorySlot == 2)
            {
                replaced = player.charm2;
                player.charm2 = item;
            }
            else if (accessorySlot == 3)
            {
                replaced = player.charm3;
                player.charm3 = item;
            }
            else if (accessorySlot == 4)
            {
                replaced = player.charm4;
                player.charm4 = item;
            }
            else if (player.charm == null)
            {
                player.charm = item;
            }
            else if (player.charm2 == null)
            {
                player.charm2 = item;
            }
            else if (player.charm3 == null)
            {
                player.charm3 = item;
            }
            else if (player.charm4 == null)
            {
                player.charm4 = item;
            }
            else
            {
                replaced = player.charm;
                player.charm = item;
            }
        }

        if (replaced != null)
        {
            NormalizeItemStats(replaced);
            player.inventory.Add(replaced);
        }

        selectedInventoryIndex = -1;
        SaveGame();
        ShowInventory(item.name + "을(를) 착용했습니다.");
    }

    private void UnequipItem(string slotType)
    {
        EnsurePlayerData();
        ItemState item = null;
        if (slotType == "Weapon")
        {
            item = player.weapon;
            if (item != null && !CanAddInventoryItem())
            {
                ShowInventory("가방이 가득 차 장비를 해제할 수 없습니다.");
                return;
            }
            player.weapon = null;
        }
        else if (slotType == "Armor")
        {
            item = player.armor;
            if (item != null && !CanAddInventoryItem())
            {
                ShowInventory("가방이 가득 차 장비를 해제할 수 없습니다.");
                return;
            }
            player.armor = null;
        }
        else if (slotType == "Charm1")
        {
            item = player.charm;
            if (item != null && !CanAddInventoryItem())
            {
                ShowInventory("가방이 가득 차 장비를 해제할 수 없습니다.");
                return;
            }
            player.charm = null;
        }
        else if (slotType == "Charm2")
        {
            item = player.charm2;
            if (item != null && !CanAddInventoryItem())
            {
                ShowInventory("가방이 가득 차 장비를 해제할 수 없습니다.");
                return;
            }
            player.charm2 = null;
        }
        else if (slotType == "Charm3")
        {
            item = player.charm3;
            if (item != null && !CanAddInventoryItem())
            {
                ShowInventory("가방이 가득 차 장비를 해제할 수 없습니다.");
                return;
            }
            player.charm3 = null;
        }
        else if (slotType == "Charm4")
        {
            item = player.charm4;
            if (item != null && !CanAddInventoryItem())
            {
                ShowInventory("가방이 가득 차 장비를 해제할 수 없습니다.");
                return;
            }
            player.charm4 = null;
        }
        else
        {
            item = player.charm4 ?? player.charm3 ?? player.charm2 ?? player.charm;
            if (item != null && !CanAddInventoryItem())
            {
                ShowInventory("가방이 가득 차 장비를 해제할 수 없습니다.");
                return;
            }
            if (player.charm4 != null) player.charm4 = null;
            else if (player.charm3 != null) player.charm3 = null;
            else if (player.charm2 != null) player.charm2 = null;
            else if (player.charm != null) player.charm = null;
        }

        if (item == null)
        {
            ShowInventory("해제할 장비가 없습니다.");
            return;
        }

        player.inventory.Add(item);
        SaveGame();
        ShowInventory(item.name + "을(를) 해제했습니다.");
    }

    private void EnhanceEquippedItem(string slotLabel, ItemState item)
    {
        EnsurePlayerData();
        if (item == null)
        {
            ShowEnhancement(slotLabel + "에 장착된 장비가 없습니다.");
            return;
        }

        if (item.level >= MaxGearEnhancementLevel)
        {
            ShowEnhancement(slotLabel + "은(는) 이미 최대 강화입니다.");
            return;
        }

        var cost = EnhancementCost(item);
        if (player.gold < cost)
        {
            ShowEnhancement(slotLabel + " 강화에 필요한 골드가 부족합니다.");
            return;
        }

        player.gold -= cost;
        if (UnityEngine.Random.value < GearEnhancementSuccessChance)
        {
            item.level = Mathf.Min(MaxGearEnhancementLevel, item.level + 1);
            UpgradeItemStats(item);
            item.power = GearPowerFromStats(item);
            SaveGame();
            AddGeneralLog(slotLabel + " 강화 성공: " + ItemLabel(item));
            ShowEnhancement(slotLabel + " 강화에 성공했습니다.");
        }
        else
        {
            SaveGame();
            AddGeneralLog(slotLabel + " 강화 실패");
            ShowEnhancement(slotLabel + " 강화에 실패했습니다. 장비는 유지됩니다.");
        }
    }

    private int PreviewEnhancedPower(ItemState item)
    {
        if (item == null)
        {
            return 0;
        }

        if (item.level >= MaxGearEnhancementLevel)
        {
            return item.power;
        }

        var preview = CopyItemForEnhancement(item);
        UpgradeItemStats(preview);
        return GearPowerFromStats(preview);
    }

    private ItemState CopyItemForEnhancement(ItemState item)
    {
        return new ItemState
        {
            type = item.type,
            rarity = item.rarity,
            maxHp = item.maxHp,
            maxMp = item.maxMp,
            attack = item.attack,
            magic = item.magic,
            defense = item.defense,
            speed = item.speed,
            critRate = item.critRate,
            critDamage = item.critDamage,
            evasion = item.evasion,
            damageReduction = item.damageReduction,
            lifeSteal = item.lifeSteal,
            manaRegen = item.manaRegen,
            statusPower = item.statusPower,
            itemFind = item.itemFind
        };
    }

    private void UpgradeItemStats(ItemState item)
    {
        if (item == null)
        {
            return;
        }

        item.maxHp = GrowFlatStat(item.maxHp);
        item.maxMp = GrowFlatStat(item.maxMp);
        item.attack = GrowFlatStat(item.attack);
        item.magic = GrowFlatStat(item.magic);
        item.defense = GrowFlatStat(item.defense);
        item.speed = GrowFlatStat(item.speed);
        item.critRate = GrowRateStat(item.critRate, "critRate");
        item.critDamage = GrowRateStat(item.critDamage, "critDamage");
        item.evasion = GrowRateStat(item.evasion, "evasion");
        item.damageReduction = GrowRateStat(item.damageReduction, "damageReduction");
        item.lifeSteal = GrowRateStat(item.lifeSteal, "lifeSteal");
        item.manaRegen = GrowRateStat(item.manaRegen, "manaRegen");
        item.statusPower = GrowRateStat(item.statusPower, "statusPower");
        item.itemFind = GrowRateStat(item.itemFind, "itemFind");
        ClampItemRateStats(item);
    }

    private int GrowFlatStat(int value)
    {
        return value <= 0 ? value : RoundToGameInt(value * (1f + GearEnhancementGrowth));
    }

    private float GrowRateStat(float value, string stat)
    {
        if (value <= 0f)
        {
            return value;
        }

        var grown = RoundToGameHundredth(value * (1f + GearEnhancementGrowth));
        if (stat == "critRate") return Mathf.Min(0.95f, grown);
        if (stat == "evasion") return Mathf.Min(0.8f, grown);
        if (stat == "damageReduction") return Mathf.Min(0.5f, grown);
        if (stat == "lifeSteal") return Mathf.Min(0.35f, grown);
        if (stat == "manaRegen") return Mathf.Min(ManaRegenCap, grown);
        if (stat == "statusPower") return Mathf.Min(0.75f, grown);
        if (stat == "itemFind") return Mathf.Min(0.5f, grown);
        return grown;
    }

    private string SellItem(int index)
    {
        EnsurePlayerData();
        if (index < 0 || index >= player.inventory.Count)
        {
            return "판매할 아이템이 없습니다.";
        }

        var item = player.inventory[index];
        if (item == null)
        {
            player.inventory.RemoveAt(index);
            SaveGame();
            return "비정상 아이템을 정리했습니다.";
        }

        NormalizeItemStats(item);
        var value = SellValue(item);
        player.gold += value;
        player.inventory.RemoveAt(index);
        SaveGame();
        var message = ItemLabel(item) + "을(를) " + value + "G에 판매했습니다.";
        AddGeneralLog(message);
        return message;
    }

    private bool IsNamedItem(ItemState item)
    {
        return item != null && item.rarity >= NamedRarity;
    }

    private int CountCraftIngredients(int rarity)
    {
        EnsurePlayerData();
        var count = 0;
        foreach (var item in player.inventory)
        {
            if (item != null && item.rarity == rarity && !IsNamedItem(item))
            {
                count++;
            }
        }
        return count;
    }

    private int RecipeCraftCost(int rarity)
    {
        if (rarity >= 0 && rarity < CraftingRecipeCosts.Length)
        {
            return CraftingRecipeCosts[rarity];
        }

        return CraftingRecipeCosts[CraftingRecipeCosts.Length - 1];
    }

    private void CraftByRecipe(int rarity)
    {
        EnsurePlayerData();
        if (rarity < 0 || rarity > MaxRecipeIngredientRarity)
        {
            ShowCrafting("이 등급은 레시피 조합 대상이 아닙니다.");
            return;
        }

        var cost = RecipeCraftCost(rarity);
        if (player.gold < cost)
        {
            ShowCrafting("레시피 조합에 필요한 골드가 부족합니다.");
            return;
        }

        var ingredientIndexes = new List<int>();
        for (var i = 0; i < player.inventory.Count && ingredientIndexes.Count < 3; i++)
        {
            var ingredient = player.inventory[i];
            if (ingredient != null && ingredient.rarity == rarity && !IsNamedItem(ingredient))
            {
                ingredientIndexes.Add(i);
            }
        }

        if (ingredientIndexes.Count < 3)
        {
            ShowCrafting("같은 등급 장비 3개가 필요합니다.");
            return;
        }

        ingredientIndexes.Sort((a, b) => b.CompareTo(a));
        foreach (var index in ingredientIndexes)
        {
            player.inventory.RemoveAt(index);
        }

        player.gold -= cost;
        var resultRarity = rarity + 1;
        var dungeonCount = Mathf.Max(1, Dungeons().Count);
        var craftStage = Mathf.Clamp(player.stage, 1, dungeonCount);
        var result = CreateRandomItem(craftStage, resultRarity);
        player.inventory.Add(result);
        SaveGame();
        var message = RarityLabel(rarity) + " 장비 3개를 조합해 " + ItemLabel(result) + "을(를) 만들었습니다.";
        AddGeneralLog(message);
        ShowCrafting(message);
    }

    private bool ProcessTurnStart(List<StatusEffect> effects, string ownerName, bool ownerIsPlayer)
    {
        if (effects == null)
        {
            return false;
        }

        var skipAction = false;
        var poisonDamage = 0;
        var burnDamage = 0;
        var bleedDamage = 0;
        var shockDamage = 0;
        var totalHeal = 0;
        var manaBurn = 0;

        for (var i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            if (effect.type == "poison")
            {
                poisonDamage += Mathf.Max(1, RoundToGameInt(effect.value));
            }
            else if (effect.type == "burn")
            {
                burnDamage += Mathf.Max(1, RoundToGameInt(effect.value));
            }
            else if (effect.type == "bleed")
            {
                bleedDamage += Mathf.Max(1, RoundToGameInt(effect.value));
            }
            else if (effect.type == "shock")
            {
                shockDamage += Mathf.Max(1, RoundToGameInt(effect.value));
            }
            else if (effect.type == "regen")
            {
                totalHeal += Mathf.Max(1, RoundToGameInt(effect.value));
            }
            else if (effect.type == "mana_burn")
            {
                manaBurn += Mathf.Max(1, RoundToGameInt(effect.value));
            }
            else if (effect.type == "stun" || effect.type == "freeze")
            {
                skipAction = true;
            }

            effect.duration--;
            if (effect.duration <= 0 && !IsActionStatus(effect.type))
            {
                effects.RemoveAt(i);
            }
        }

        var poisonApplied = ApplyTurnStartDamage(ownerName, ownerIsPlayer, "poison", poisonDamage);
        if (!ownerIsPlayer && poisonApplied > 0 && player != null && GearClassId(player.heroClass) == "rogue")
        {
            var healed = Mathf.Min(MaxHp() - player.hp, poisonApplied);
            if (healed > 0)
            {
                player.hp += healed;
                battleLog.Add("독 피해를 흡수해 체력을 " + healed + " 회복했습니다.");
            }
        }

        ApplyTurnStartDamage(ownerName, ownerIsPlayer, "burn", burnDamage);
        ApplyTurnStartDamage(ownerName, ownerIsPlayer, "bleed", bleedDamage);
        ApplyTurnStartDamage(ownerName, ownerIsPlayer, "shock", shockDamage);

        if (totalHeal > 0)
        {
            if (ownerIsPlayer)
            {
                player.hp = Mathf.Min(MaxHp(), player.hp + totalHeal);
            }
            else if (currentEnemy != null)
            {
                currentEnemy.hp = Mathf.Min(currentEnemy.maxHp, currentEnemy.hp + totalHeal);
            }
            battleLog.Add(ownerName + "이(가) 재생으로 " + totalHeal + " 회복했습니다.");
        }

        if (manaBurn > 0 && ownerIsPlayer)
        {
            player.mp = Mathf.Max(0, player.mp - manaBurn);
            battleLog.Add("마나 연소로 마나가 " + manaBurn + " 감소했습니다.");
        }
        else if (manaBurn > 0 && currentEnemy != null)
        {
            currentEnemy.mp = Mathf.Max(0, currentEnemy.mp - manaBurn);
            battleLog.Add(ownerName + "의 마나가 마나 연소로 " + manaBurn + " 감소했습니다.");
        }

        if (ownerIsPlayer && player.hp <= 0)
        {
            return true;
        }

        if (!ownerIsPlayer && currentEnemy != null && currentEnemy.hp <= 0)
        {
            return true;
        }

        if (skipAction)
        {
            battleLog.Add(ownerName + "은(는) 행동할 수 없습니다.");
        }

        return skipAction;
    }

    private int ApplyTurnStartDamage(string ownerName, bool ownerIsPlayer, string effectType, int damage)
    {
        if (damage <= 0)
        {
            return 0;
        }

        var appliedDamage = damage;
        if (ownerIsPlayer)
        {
            appliedDamage = AbsorbShield(playerStatusEffects, damage, "플레이어");
            player.hp -= appliedDamage;
        }
        else if (currentEnemy != null)
        {
            appliedDamage = AbsorbShield(enemyStatusEffects, damage, ownerName);
            currentEnemy.hp -= appliedDamage;
        }

        battleLog.Add(ownerName + "이(가) " + StatusName(effectType) + " 상태로 " + appliedDamage + " 피해를 받았습니다.");
        return appliedDamage;
    }

    private void ExpireActionStatusEffects(List<StatusEffect> effects)
    {
        for (var i = effects.Count - 1; i >= 0; i--)
        {
            if (effects[i].duration <= 0)
            {
                effects.RemoveAt(i);
            }
        }
    }

    private bool IsActionStatus(string type)
    {
        return type == "stun"
            || type == "freeze"
            || type == "blind"
            || type == "weaken"
            || type == "vulnerable"
            || type == "silence"
            || type == "evasion_boost"
            || type == "damage_boost";
    }

    private bool TryBlindMiss(List<StatusEffect> effects, string ownerName)
    {
        var blind = StatusValue(effects, "blind", 0f);
        if (blind > 0f && UnityEngine.Random.value < blind)
        {
            battleLog.Add(ownerName + "의 공격이 실명 때문에 빗나갔습니다.");
            return true;
        }

        return false;
    }

    private float DamageMultiplier(List<StatusEffect> attackerEffects, List<StatusEffect> defenderEffects)
    {
        var multiplier = 1f;
        multiplier *= 1f - StatusValue(attackerEffects, "weaken", 0f);
        multiplier *= 1f + StatusValue(attackerEffects, "damage_boost", 0f);
        multiplier *= 1f + StatusValue(defenderEffects, "vulnerable", 0f);
        if (HasStatus(defenderEffects, "burn"))
        {
            multiplier *= 1.25f;
        }
        if (HasStatus(defenderEffects, "freeze"))
        {
            multiplier *= 1.15f;
        }
        return Mathf.Max(0.25f, multiplier);
    }

    private int CalculateDamage(int basePower, float skillMultiplier, int defenderDefense, List<StatusEffect> attackerEffects, List<StatusEffect> defenderEffects, float criticalMultiplier, float damageReduction)
    {
        var damage = Mathf.Max(0f, basePower) * Mathf.Max(0f, skillMultiplier);
        damage *= DefenseDampening(defenderDefense);
        damage *= DamageMultiplier(attackerEffects, defenderEffects);
        damage *= Mathf.Max(1f, criticalMultiplier);
        damage *= 1f - Mathf.Clamp(damageReduction, 0f, 0.6f);
        return Mathf.Max(1, RoundToGameInt(damage));
    }

    private float DefenseDampening(int defense)
    {
        return 100f / (100f + Mathf.Max(0, defense));
    }

    private void ApplySkillStatus(SkillState skill, List<StatusEffect> targetEffects, string targetName, int physicalPower, int magicPower, float statusScale)
    {
        TryApplyStatus(targetEffects, targetName, "stun", skill.stunChance, 1, 0);
        TryApplyStatus(targetEffects, targetName, "burn", skill.burnChance, 3, Mathf.Max(1, RoundToGameInt(magicPower * 0.4f * statusScale)));
        TryApplyStatus(targetEffects, targetName, "poison", skill.poisonChance, 3, Mathf.Max(1, RoundToGameInt(physicalPower * 0.3f * statusScale)));
        TryApplyStatus(targetEffects, targetName, "bleed", skill.bleedChance, 3, Mathf.Max(1, RoundToGameInt(physicalPower * 0.28f * statusScale)));
        TryApplyStatus(targetEffects, targetName, "shock", skill.shockChance, 2, Mathf.Max(1, RoundToGameInt(magicPower * 0.32f * statusScale)));
        TryApplyStatus(targetEffects, targetName, "freeze", skill.freezeChance, 1, 0);
        TryApplyStatus(targetEffects, targetName, "blind", skill.blindChance, 2, 0.25f);
        TryApplyStatus(targetEffects, targetName, "weaken", skill.weakenChance, 2, 0.25f);
        TryApplyStatus(targetEffects, targetName, "vulnerable", skill.vulnerableChance, 2, 0.2f);
        TryApplyStatus(targetEffects, targetName, "silence", skill.silenceChance, 1, 0);
        TryApplyStatus(targetEffects, targetName, "mana_burn", skill.manaBurnChance, 2, Mathf.Max(1, RoundToGameInt(magicPower * 0.25f * statusScale)));
    }

    private void ApplySelfStatus(SkillState skill, List<StatusEffect> targetEffects, string ownerName, int basePower)
    {
        if (!skill.grantsShield && !skill.healsSelf && string.IsNullOrEmpty(skill.selfStatusType))
        {
            return;
        }

        if (skill.healsSelf)
        {
            var heal = Mathf.Max(1, RoundToGameInt(basePower * Mathf.Max(0.1f, skill.multiplier)));
            if (ownerName == "플레이어")
            {
                player.hp = Mathf.Min(MaxHp(), player.hp + heal);
            }
            else if (currentEnemy != null)
            {
                currentEnemy.hp = Mathf.Min(currentEnemy.maxHp, currentEnemy.hp + heal);
            }
            battleLog.Add(ownerName + "이(가) " + heal + " 회복했습니다.");
        }

        if (skill.grantsShield || skill.selfStatusType == "shield")
        {
            var shieldValue = ShieldValueFor(ownerName, skill);
            AddStatus(targetEffects, "shield", int.MaxValue, shieldValue);
            battleLog.Add(ownerName + "이(가) 보호막 효과를 얻었습니다.");
        }

        if (!string.IsNullOrEmpty(skill.selfStatusType) && skill.selfStatusType != "shield")
        {
            var value = skill.selfStatusValue;
            if (value < 0f && skill.selfStatusType == "regen")
            {
                value = Mathf.Max(1, RoundToGameInt(basePower * -value));
            }
            else if (value <= 0f && skill.selfStatusType == "regen")
            {
                value = Mathf.Max(1, RoundToGameInt(basePower * 0.25f));
            }

            AddStatus(targetEffects, skill.selfStatusType, Mathf.Max(1, skill.selfStatusDuration), value);
            battleLog.Add(ownerName + "이(가) " + StatusName(skill.selfStatusType) + " 효과를 얻었습니다.");
        }
    }

    private int ShieldValueFor(string ownerName, SkillState skill)
    {
        if (ownerName != "플레이어" && currentEnemy != null && currentDungeon != null)
        {
            var rate = currentDungeon.number <= 6 ? 0.2f : 0.05f;
            var shield = currentEnemy.maxHp * rate;
            return Mathf.Max(1, RoundToGameInt(currentDungeon.number >= 10 ? Mathf.Min(shield, 20000f) : shield));
        }

        var ratio = skill.grantsShield ? skill.multiplier : (skill.selfStatusValue > 0f ? skill.selfStatusValue : skill.multiplier);
        return Mathf.Max(1, RoundToGameInt(MaxHp() * Mathf.Max(0.2f, ratio)));
    }

    private int AbsorbShield(List<StatusEffect> effects, int damage, string ownerName)
    {
        var shield = effects.Find(effect => effect.type == "shield");
        if (shield == null || damage <= 0)
        {
            return damage;
        }

        var blocked = Mathf.Min(damage, RoundToGameInt(shield.value));
        shield.value -= blocked;
        damage -= blocked;
        battleLog.Add(ownerName + "의 보호막이 " + blocked + " 피해를 막았습니다.");
        if (shield.value <= 0f)
        {
            effects.Remove(shield);
        }
        return Mathf.Max(0, damage);
    }

    private void TryApplyStatus(List<StatusEffect> effects, string targetName, string type, float chance, int duration, float value)
    {
        if (effects == null || string.IsNullOrEmpty(type))
        {
            return;
        }

        chance = Mathf.Clamp01(chance);
        if (chance <= 0f)
        {
            return;
        }

        if (UnityEngine.Random.value >= chance)
        {
            if (LogStatusFailure)
            {
                battleLog.Add(targetName + "에게 " + StatusName(type) + " 적용 실패.");
            }
            return;
        }

        AddStatus(effects, type, duration, value);
        battleLog.Add(targetName + "에게 " + StatusName(type) + " 상태가 적용되었습니다.");
    }

    private void AddStatus(List<StatusEffect> effects, string type, int duration, float value)
    {
        if (effects == null || string.IsNullOrEmpty(type))
        {
            return;
        }

        duration = Mathf.Max(1, duration);
        if (type == "shield")
        {
            value = Mathf.Max(0f, value);
        }

        var existing = effects.Find(effect => effect.type == type);
        if (existing != null)
        {
            existing.duration = Mathf.Max(existing.duration, duration);
            existing.value = type == "shield" ? existing.value + value : Mathf.Max(existing.value, value);
            return;
        }

        effects.Add(new StatusEffect { type = type, duration = duration, value = value });
    }

    private bool HasStatus(List<StatusEffect> effects, string type)
    {
        if (effects == null || string.IsNullOrEmpty(type))
        {
            return false;
        }

        return effects.Exists(effect => effect.type == type);
    }

    private float StatusValue(List<StatusEffect> effects, string type, float fallback)
    {
        if (effects == null || string.IsNullOrEmpty(type))
        {
            return fallback;
        }

        var effect = effects.Find(entry => entry.type == type);
        return effect != null ? Mathf.Max(effect.value, fallback) : fallback;
    }

    private string StatusLine(List<StatusEffect> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            return "상태이상 없음";
        }

        var labels = new List<string>();
        foreach (var effect in effects)
        {
            if (effect.type == "shield")
            {
                labels.Add(StatusName(effect.type) + " " + RoundToGameInt(effect.value));
            }
            else
            {
                labels.Add(StatusName(effect.type) + " " + effect.duration + "턴");
            }
        }
        if (labels.Count > 4)
        {
            var compact = new List<string>();
            for (var i = 0; i < 4; i++)
            {
                compact.Add(labels[i]);
            }
            compact.Add("외 " + (labels.Count - 4));
            return string.Join(" / ", compact.ToArray());
        }
        return string.Join(" / ", labels.ToArray());
    }

    private string StatusName(string type)
    {
        if (type == "poison") return "중독";
        if (type == "burn") return "화상";
        if (type == "stun") return "기절";
        if (type == "bleed") return "출혈";
        if (type == "shock") return "감전";
        if (type == "freeze") return "빙결";
        if (type == "blind") return "실명";
        if (type == "weaken") return "약화";
        if (type == "vulnerable") return "취약";
        if (type == "silence") return "침묵";
        if (type == "mana_burn") return "마나 연소";
        if (type == "regen") return "재생";
        if (type == "shield") return "보호막";
        if (type == "evasion_boost") return "회피 상승";
        if (type == "damage_boost") return "피해 증가";
        return type;
    }

    private PlayerState CreatePlayer(HeroClass heroClass)
    {
        var state = new PlayerState
        {
            heroName = "에테리안",
            heroClass = heroClass.name,
            portraitName = heroClass.portraitName,
            saveVersion = CurrentSaveVersion,
            level = 1,
            xp = 0,
            stage = 1,
            gold = 100,
            potions = 0,
            baseHp = heroClass.hp,
            baseMp = heroClass.mp,
            baseAttack = heroClass.attack,
            baseMagic = heroClass.magic,
            baseDefense = heroClass.defense,
            baseCrit = heroClass.crit,
            inventory = new List<ItemState>(),
            skillLevels = new List<SkillLevelState>(),
            dungeonProgress = new List<DungeonProgressState>(),
            generalLogs = new List<string>()
        };
        var startingSkills = SkillsForClass(state.heroClass);
        if (startingSkills.Count == 0 && heroClass.skills.Count > 0)
        {
            startingSkills = heroClass.skills;
        }
        foreach (var skill in startingSkills)
        {
            state.skillLevels.Add(new SkillLevelState { skillName = skill.name, level = 1 });
        }
        state.hp = MaxHp(state);
        state.mp = MaxMp(state);
        return state;
    }

    private EnemyState CreateEnemy(DungeonData dungeon, bool bossBattle)
    {
        var source = dungeon == null
            ? null
            : (bossBattle || dungeon.monsters == null || dungeon.monsters.Count == 0
                ? dungeon.boss
                : dungeon.monsters[UnityEngine.Random.Range(0, dungeon.monsters.Count)]);
        if (source == null)
        {
            source = Enemy("차원 잔재", "전투 데이터가 부족해 생성된 임시 적입니다.", 10, 1, 0, 0, 0f, 0, 0);
        }

        var floorScale = 1f + Mathf.Max(0, currentDungeonFloor - 1) * 0.12f;
        var maxMp = EnemyMaxMp(dungeon, source, floorScale);
        return new EnemyState
        {
            name = source.name,
            description = source.description,
            spriteKey = source.spriteKey,
            maxHp = Mathf.Max(1, RoundToGameInt(source.hp * floorScale)),
            hp = Mathf.Max(1, RoundToGameInt(source.hp * floorScale)),
            maxMp = maxMp,
            mp = maxMp,
            attack = Mathf.Max(0, RoundToGameInt(source.attack * floorScale)),
            magic = Mathf.Max(0, RoundToGameInt(source.magic * floorScale)),
            defense = Mathf.Max(0, RoundToGameInt(source.defense * floorScale)),
            speed = source.speed > 0 ? RoundToGameInt(source.speed * floorScale) : EstimateEnemySpeed(dungeon, source, bossBattle, floorScale),
            crit = source.crit,
            gold = Mathf.Max(0, RoundToGameInt(source.gold * (bossBattle ? 1f : floorScale))),
            xp = ScaledEnemyXpReward(dungeon, source, bossBattle, floorScale),
            skills = source.skills != null ? new List<SkillState>(source.skills) : new List<SkillState>()
        };
    }

    private int ScaledEnemyXpReward(DungeonData dungeon, EnemyTemplate source, bool bossBattle, float floorScale)
    {
        if (source == null || source.xp <= 0)
        {
            return 0;
        }

        var baseXp = source.xp * (bossBattle ? 1f : floorScale);
        var multiplier = BaseExperienceRewardMultiplier * (bossBattle ? BossExperienceRewardMultiplier : 1f);
        if (dungeon != null && player != null)
        {
            var levelGap = Mathf.Max(0, dungeon.recommendedLevel - Mathf.Max(1, player.level));
            var catchUpBonus = Mathf.Min(UnderRecommendedLevelXpBonusCap, levelGap * UnderRecommendedLevelXpBonusPerLevel);
            multiplier *= 1f + catchUpBonus;
        }

        return Mathf.Max(0, RoundToGameInt(baseXp * multiplier));
    }

    private int EnemyMaxMp(DungeonData dungeon, EnemyTemplate source, float floorScale)
    {
        if (source == null)
        {
            return 0;
        }

        if (source.maxMp > 0)
        {
            return Mathf.Max(0, RoundToGameInt(source.maxMp * floorScale));
        }

        if (source.skills == null || source.skills.Count == 0)
        {
            return 0;
        }

        var level = dungeon != null ? dungeon.recommendedLevel : 1;
        return Mathf.Max(30, RoundToGameInt((source.magic + source.attack * 0.35f + level * 4) * floorScale));
    }

    private int EstimateEnemySpeed(DungeonData dungeon, EnemyTemplate source, bool bossBattle, float floorScale)
    {
        if (source == null)
        {
            return 1;
        }

        var powerHint = Mathf.Sqrt(Mathf.Max(source.attack, source.magic) + source.defense * 0.5f);
        var dungeonNumber = dungeon != null ? dungeon.number : 1;
        var stageSpeed = 4f + dungeonNumber * 1.35f + powerHint * 0.45f;
        if (bossBattle)
        {
            stageSpeed += 2.5f;
        }
        else
        {
            stageSpeed += currentDungeonFloor * 0.6f;
        }

        return Mathf.Max(1, RoundToGameInt(stageSpeed * Mathf.Lerp(1f, floorScale, 0.35f)));
    }

    private bool IsDungeonUnlocked(DungeonData dungeon)
    {
        EnsurePlayerData();
        return dungeon != null && dungeon.number > 0 && dungeon.number <= player.stage;
    }

    private bool IsBossUnlocked(DungeonData dungeon)
    {
        if (dungeon == null)
        {
            return false;
        }

        EnsurePlayerData();
        if (player.stage > dungeon.number)
        {
            return true;
        }

        var progress = DungeonProgress(dungeon.number);
        return progress != null && progress.floorCleared;
    }

    private bool IsBossCleared(DungeonData dungeon)
    {
        if (dungeon == null)
        {
            return false;
        }

        EnsurePlayerData();
        if (player.stage > dungeon.number)
        {
            return true;
        }

        var progress = DungeonProgress(dungeon.number);
        return progress != null && progress.bossCleared;
    }

    private DungeonProgressState DungeonProgress(int dungeonNumber)
    {
        EnsurePlayerData();
        if (dungeonNumber <= 0)
        {
            return null;
        }

        return player.dungeonProgress.Find(progress => progress.dungeonNumber == dungeonNumber);
    }

    private void SetDungeonProgress(int dungeonNumber, bool floorCleared, bool bossCleared)
    {
        EnsurePlayerData();
        if (dungeonNumber <= 0)
        {
            return;
        }

        var progress = player.dungeonProgress.Find(entry => entry.dungeonNumber == dungeonNumber);
        if (progress == null)
        {
            progress = new DungeonProgressState { dungeonNumber = dungeonNumber };
            player.dungeonProgress.Add(progress);
        }

        progress.floorCleared = progress.floorCleared || floorCleared;
        progress.bossCleared = progress.bossCleared || bossCleared;
    }

    private SkillState PickEnemySkill()
    {
        if (currentEnemy == null || currentEnemy.skills == null || currentEnemy.skills.Count == 0 || UnityEngine.Random.value > 0.6f)
        {
            return null;
        }

        var available = new List<SkillState>();
        foreach (var skill in currentEnemy.skills)
        {
            if (skill != null && currentEnemy.mp >= EnemySkillCost(skill))
            {
                available.Add(skill);
            }
        }

        if (available.Count == 0)
        {
            return null;
        }

        return available[UnityEngine.Random.Range(0, available.Count)];
    }

    private int EnemySkillCost(SkillState skill)
    {
        return skill == null ? 0 : skill.mpCost;
    }

    private void RegenerateEnemyManaOnSchedule()
    {
        if (currentEnemy == null || currentEnemy.maxMp <= 0)
        {
            return;
        }

        enemyManaRegenTurnCounter++;
        if (enemyManaRegenTurnCounter < 3)
        {
            return;
        }

        enemyManaRegenTurnCounter = 0;
        var restored = Mathf.Min(currentEnemy.maxMp - currentEnemy.mp, RoundToGameInt(currentEnemy.maxMp * MonsterTurnManaRegen));
        if (restored <= 0)
        {
            return;
        }

        currentEnemy.mp += restored;
        battleLog.Add(currentEnemy.name + "이(가) 마나를 " + restored + " 회복했습니다.");
    }

    private bool BasicAttackUsesMagic()
    {
        var heroClass = HeroClasses().Find(entry => entry.name == player.heroClass);
        if (heroClass != null)
        {
            return heroClass.basicAttackUsesMagic;
        }

        return player.heroClass == "원소술사" || player.heroClass == "빛의 사제" || player.heroClass == "폭렬술사" || player.heroClass == "정령술사";
    }

    private float BaseSpeedForClass(string heroClass)
    {
        if (heroClass == "성기사") return 8f;
        if (heroClass == "원소술사") return 10f;
        if (heroClass == "그림자 자객" || heroClass == "그림자 궁수" || heroClass == "Ranger") return 16f;
        if (heroClass == "빛의 사제") return 10f;
        if (heroClass == "폭렬술사") return 9f;
        if (heroClass == "정령술사") return 12f;
        if (heroClass == "바람 궁수") return 15f;
        if (heroClass == "무투가") return 14f;
        return 8f;
    }

    private float SpeedGrowthForClass(string heroClass)
    {
        return ClassStatGrowth(heroClass, "speed");
    }

    private int LevelGrowth(PlayerState state, string stat)
    {
        return RoundToGameInt(LevelGrowthFloat(state, stat));
    }

    private float LevelGrowthFloat(PlayerState state, string stat)
    {
        if (state == null)
        {
            return 0f;
        }

        return Mathf.Max(0, state.level - 1) * ClassStatGrowth(state.heroClass, stat);
    }

    private float ClassStatGrowth(string heroClass, string stat)
    {
        if (heroClass == "성기사")
        {
            if (stat == "maxHp") return 20f;
            if (stat == "maxMp") return 5f;
            if (stat == "attack") return 3f;
            if (stat == "magic") return 1f;
            if (stat == "defense") return 2f;
            if (stat == "speed") return 0.8f;
            if (stat == "critRate") return 0.005f;
        }
        else if (heroClass == "원소술사")
        {
            if (stat == "maxHp") return 10f;
            if (stat == "maxMp") return 19f;
            if (stat == "attack") return 1f;
            if (stat == "magic") return 5f;
            if (stat == "defense") return 0.8f;
            if (stat == "speed") return 1.2f;
            if (stat == "critRate") return 0.01f;
        }
        else if (heroClass == "그림자 자객" || heroClass == "그림자 궁수" || heroClass == "Ranger")
        {
            if (stat == "maxHp") return 14f;
            if (stat == "maxMp") return 8f;
            if (stat == "attack") return 4f;
            if (stat == "magic") return 1.5f;
            if (stat == "defense") return 1.2f;
            if (stat == "speed") return 1.8f;
            if (stat == "critRate") return 0.02f;
        }
        else if (heroClass == "빛의 사제")
        {
            if (stat == "maxHp") return 14f;
            if (stat == "maxMp") return 16f;
            if (stat == "attack") return 1f;
            if (stat == "magic") return 4.2f;
            if (stat == "defense") return 1.2f;
            if (stat == "speed") return 1f;
            if (stat == "critRate") return 0.008f;
        }
        else if (heroClass == "폭렬술사")
        {
            if (stat == "maxHp") return 11f;
            if (stat == "maxMp") return 17f;
            if (stat == "attack") return 0.8f;
            if (stat == "magic") return 5.3f;
            if (stat == "defense") return 0.8f;
            if (stat == "speed") return 0.9f;
            if (stat == "critRate") return 0.012f;
        }
        else if (heroClass == "정령술사")
        {
            if (stat == "maxHp") return 12f;
            if (stat == "maxMp") return 21f;
            if (stat == "attack") return 0f;
            if (stat == "magic") return 4.6f;
            if (stat == "defense") return 1f;
            if (stat == "speed") return 1.3f;
            if (stat == "critRate") return 0.008f;
        }
        else if (heroClass == "바람 궁수")
        {
            if (stat == "maxHp") return 13f;
            if (stat == "maxMp") return 9f;
            if (stat == "attack") return 4.2f;
            if (stat == "magic") return 1f;
            if (stat == "defense") return 1.1f;
            if (stat == "speed") return 1.7f;
            if (stat == "critRate") return 0.018f;
        }
        else if (heroClass == "무투가")
        {
            if (stat == "maxHp") return 15f;
            if (stat == "maxMp") return 8f;
            if (stat == "attack") return 4.4f;
            if (stat == "magic") return 0.8f;
            if (stat == "defense") return 1.4f;
            if (stat == "speed") return 1.6f;
            if (stat == "critRate") return 0.018f;
        }

        if (stat == "maxHp") return 20f;
        if (stat == "maxMp") return 5f;
        if (stat == "attack") return 3f;
        if (stat == "magic") return 1f;
        if (stat == "defense") return 2f;
        if (stat == "speed") return 0.8f;
        if (stat == "critRate") return 0.005f;
        return 0f;
    }

    private List<DungeonData> Dungeons()
    {
        if (cachedDungeons != null)
        {
            return cachedDungeons;
        }

        var defaultDungeons = DefaultDungeons();
        var databaseDungeons = DungeonsFromDatabase();
        if (databaseDungeons.Count > 0)
        {
            cachedDungeons = MergeDungeons(defaultDungeons, databaseDungeons);
            return cachedDungeons;
        }

        cachedDungeons = defaultDungeons;
        return cachedDungeons;
    }

    private List<DungeonData> MergeDungeons(List<DungeonData> defaults, List<DungeonData> overrides)
    {
        var result = new List<DungeonData>(defaults ?? new List<DungeonData>());
        if (overrides == null)
        {
            return result;
        }

        foreach (var dungeon in overrides)
        {
            if (dungeon == null || dungeon.number <= 0)
            {
                continue;
            }

            var index = result.FindIndex(entry => entry != null && entry.number == dungeon.number);
            if (index >= 0)
            {
                result[index] = MergeDungeon(result[index], dungeon);
            }
            else if (HasDungeonEncounter(dungeon))
            {
                result.Add(NormalizeNewDungeon(dungeon));
            }
        }

        result.Sort((left, right) => left.number.CompareTo(right.number));
        return result;
    }

    private DungeonData MergeDungeon(DungeonData fallback, DungeonData dungeon)
    {
        if (fallback == null)
        {
            return NormalizeNewDungeon(dungeon);
        }

        if (dungeon == null)
        {
            return fallback;
        }

        var monsters = dungeon.monsters != null && dungeon.monsters.Count > 0 ? dungeon.monsters : fallback.monsters;
        var boss = dungeon.boss ?? fallback.boss;
        return new DungeonData(
            dungeon.number > 0 ? dungeon.number : fallback.number,
            string.IsNullOrEmpty(dungeon.name) ? fallback.name : dungeon.name,
            dungeon.recommendedLevel > 0 ? dungeon.recommendedLevel : fallback.recommendedLevel,
            dungeon.floors > 0 ? Mathf.Clamp(dungeon.floors, 1, MaxDungeonFloors) : fallback.floors,
            string.IsNullOrEmpty(dungeon.description) ? fallback.description : dungeon.description,
            monsters,
            boss);
    }

    private DungeonData NormalizeNewDungeon(DungeonData dungeon)
    {
        if (dungeon == null)
        {
            return null;
        }

        return new DungeonData(
            dungeon.number,
            string.IsNullOrEmpty(dungeon.name) ? "던전 " + dungeon.number : dungeon.name,
            Mathf.Max(1, dungeon.recommendedLevel),
            Mathf.Clamp(dungeon.floors <= 0 ? 1 : dungeon.floors, 1, MaxDungeonFloors),
            string.IsNullOrEmpty(dungeon.description) ? "새 던전입니다." : dungeon.description,
            dungeon.monsters ?? new List<EnemyTemplate>(),
            dungeon.boss);
    }

    private static List<DungeonData> DefaultDungeons()
    {
        return new List<DungeonData>
        {
            new DungeonData(1, "속삭이는 숲", 1, 3, "빛줄기조차 희미한 고요한 숲입니다. 야생 동물들과 고블린 무리가 어슬렁거리며 여행자를 습격합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("초록 슬라임", "초록 슬라임입니다.", 45, 8, 0, 2, 0.02f, 10, 25),
                    Enemy("고블린 순찰병", "고블린 순찰병입니다.", 60, 11, 0, 4, 0.05f, 15, 35, Skill("독침 투척", 14, 0.8f, false, poison: 0.5f)),
                    Enemy("사나운 숲늑대", "사나운 숲늑대입니다.", 55, 13, 0, 3, 0.1f, 12, 40, Skill("물어뜯기", 0, 1.2f, false))
                },
                Enemy("대왕 고블린 족장", "대왕 고블린 족장입니다.", 150, 18, 0, 8, 0.08f, 60, 150, Skill("족장의 분노", 27, 1.4f, false), Skill("대지 격타", 20, 1.1f, false, stun: 0.17f))),

            new DungeonData(2, "잊혀진 지하묘지", 4, 3, "오래전 봉인된 차가운 석실입니다. 원혼에 사로잡힌 해골 해골들과 언데드 생명체가 침입자를 기다립니다.",
                new List<EnemyTemplate>
                {
                    Enemy("부패한 좀비", "부패한 좀비입니다.", 110, 16, 0, 5, 0.02f, 25, 65),
                    Enemy("해골 검사", "해골 검사입니다.", 130, 22, 0, 12, 0.06f, 30, 80, Skill("연속 베기", 20, 1.3f, false)),
                    Enemy("무덤의 유령", "무덤의 유령입니다.", 90, 8, 26, 4, 0.12f, 35, 90, Skill("공포의 손짓", 27, 1.4f, true, stun: 0.11f))
                },
                Enemy("어둠의 리치 공작", "어둠의 리치 공작입니다.", 320, 12, 38, 15, 0.1f, 150, 300, Skill("죽음의 고리", 41, 1.6f, true, poison: 0.5f), Skill("암흑 방벽", 34, 0.3f, false))),

            new DungeonData(3, "공허의 성채", 7, 4, "시공간이 일그러진 공허의 중심부에 위치한 탑입니다. 이계의 지배자와 마주하게 되는 마지막 격전지입니다.",
                new List<EnemyTemplate>
                {
                    Enemy("공허 사냥개", "공허 사냥개입니다.", 210, 32, 12, 14, 0.15f, 50, 160, Skill("위동 물어뜯기", 20, 1.3f, false, poison: 0.5f)),
                    Enemy("심연의 차원 감시자", "심연의 차원 감시자입니다.", 190, 15, 45, 10, 0.12f, 60, 180, Skill("붕괴 레이저", 34, 1.5f, true, burn: 0.4f)),
                    Enemy("공허의 집행기사", "공허의 집행기사입니다.", 270, 38, 0, 25, 0.08f, 65, 210, Skill("파쇄 참격", 27, 1.4f, false))
                },
                Enemy("공허의 파괴 군주 바알", "공허의 파괴 군주 바알입니다.", 650, 48, 55, 30, 0.15f, 500, 1000, Skill("멸망의 전조", 54, 1.8f, true, burn: 0.8f), Skill("공허의 파동", 41, 1.4f, false, stun: 0.22f))),

            new DungeonData(4, "붉은 수정 광산", 10, 4, "붉게 빛나는 수정맥이 지하 깊은 곳에서 맥동합니다. 광산을 점거한 괴물들이 수정의 힘에 취해 난폭해졌습니다.",
                new List<EnemyTemplate>
                {
                    Enemy("수정 광부 좀비", "수정 광부 좀비입니다.", 340, 52, 8, 34, 0.08f, 85, 280, Skill("곡괭이 강타", 20, 1.35f, false, stun: 0.11f)),
                    Enemy("혈정 박쥐", "혈정 박쥐입니다.", 260, 48, 22, 18, 0.18f, 90, 300, Skill("흡혈 급습", 27, 1.45f, false)),
                    Enemy("수정 골렘", "수정 골렘입니다.", 430, 55, 18, 46, 0.05f, 110, 340, Skill("수정 파편", 27, 1.35f, true, burn: 0.25f))
                },
                Enemy("혈정 거인 그라논", "혈정 거인 그라논입니다.", 980, 72, 32, 55, 0.1f, 700, 1600, Skill("붉은 붕괴", 47, 1.65f, false, stun: 0.19f), Skill("수정 갑피", 41, 0.35f, false))),

            new DungeonData(5, "폭풍 첨탑", 13, 4, "벼락이 멈추지 않는 하늘의 탑입니다. 바람 정령과 번개 괴수들이 침입자의 균형을 무너뜨립니다.",
                new List<EnemyTemplate>
                {
                    Enemy("돌풍 정령", "돌풍 정령입니다.", 390, 38, 66, 26, 0.14f, 125, 430, Skill("칼바람", 34, 1.4f, true)),
                    Enemy("번개 와이번", "번개 와이번입니다.", 470, 64, 58, 32, 0.16f, 145, 470, Skill("뇌격 숨결", 41, 1.45f, true, stun: 0.14f)),
                    Enemy("먹구름 사제", "먹구름 사제입니다.", 410, 22, 78, 28, 0.1f, 155, 490, Skill("폭풍 의식", 47, 1.55f, true, burn: 0.35f))
                },
                Enemy("천둥 군주 라이에스", "천둥 군주 라이에스입니다.", 1250, 78, 94, 42, 0.16f, 950, 2300, Skill("천벌 낙뢰", 61, 1.75f, true, stun: 0.22f), Skill("폭풍 장막", 47, 0.38f, false))),

            new DungeonData(6, "얼어붙은 왕궁", 16, 4, "시간마저 얼어붙은 폐궁입니다. 차가운 기사단과 얼음 마법이 발걸음을 늦춥니다.",
                new List<EnemyTemplate>
                {
                    Enemy("서리 창병", "서리 창병입니다.", 560, 82, 34, 54, 0.09f, 180, 620, Skill("빙결 찌르기", 34, 1.45f, false, stun: 0.14f)),
                    Enemy("눈보라 마녀", "눈보라 마녀입니다.", 480, 28, 98, 36, 0.12f, 205, 660, Skill("서리 파도", 54, 1.65f, true)),
                    Enemy("빙벽 수호자", "빙벽 수호자입니다.", 680, 76, 20, 70, 0.06f, 220, 700, Skill("얼음 방패", 34, 0.25f, false))
                },
                Enemy("빙관의 여왕 세레나", "빙관의 여왕 세레나입니다.", 1680, 70, 128, 64, 0.14f, 1250, 3200, Skill("영원의 동결", 74, 1.8f, true, stun: 0.25f), Skill("서리 왕관", 61, 0.5f, false))),

            new DungeonData(7, "잿빛 황무지", 20, 5, "불타고 남은 대지 위로 독기와 재가 떠돕니다. 생존한 괴수들은 상처 입은 세계처럼 거칠고 집요합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("잿불 약탈자", "잿불 약탈자입니다.", 760, 104, 46, 66, 0.14f, 260, 900, Skill("불타는 도끼", 41, 1.55f, false, burn: 0.35f)),
                    Enemy("독안개 괴수", "독안개 괴수입니다.", 840, 92, 60, 58, 0.08f, 275, 940, Skill("맹독 구름", 47, 1.45f, true, poison: 0.7f)),
                    Enemy("검은 재의 기사", "검은 재의 기사입니다.", 900, 116, 24, 78, 0.11f, 290, 980, Skill("회색 참수", 47, 1.6f, false))
                },
                Enemy("화산 심장 모르칸", "화산 심장 모르칸입니다.", 2400, 138, 116, 86, 0.15f, 1700, 4600, Skill("용암 폭주", 81, 1.9f, true, burn: 0.75f), Skill("지각 분쇄", 68, 1.75f, false, stun: 0.19f))),

            new DungeonData(8, "월광 미궁", 24, 5, "달빛을 따라 길이 바뀌는 미궁입니다. 환영과 암살자들이 방향감각을 흔듭니다.",
                new List<EnemyTemplate>
                {
                    Enemy("달그림자 추적자", "달그림자 추적자입니다.", 980, 138, 72, 72, 0.22f, 350, 1250, Skill("월광 급습", 54, 1.7f, false)),
                    Enemy("거울 환영술사", "거울 환영술사입니다.", 900, 44, 150, 60, 0.16f, 375, 1300, Skill("환영 붕괴", 68, 1.65f, true, stun: 0.17f)),
                    Enemy("미궁 살수", "미궁 살수입니다.", 940, 154, 50, 64, 0.24f, 390, 1340, Skill("독월 베기", 61, 1.6f, false, poison: 0.65f))
                },
                Enemy("미궁의 은월 아리아", "미궁의 은월 아리아입니다.", 3100, 158, 170, 82, 0.22f, 2200, 6200, Skill("은월 심판", 95, 1.9f, true), Skill("그림자 결박", 74, 1.65f, false, stun: 0.22f))),

            new DungeonData(9, "가라앉은 기록보관소", 28, 5, "고대 지식이 바닷물 아래 잠든 장소입니다. 잊힌 문서와 수중 괴물들이 금지된 주문을 지킵니다.",
                new List<EnemyTemplate>
                {
                    Enemy("심해 필경사", "심해 필경사입니다.", 1180, 62, 188, 82, 0.15f, 470, 1650, Skill("금서 낭독", 81, 1.7f, true, poison: 0.5f)),
                    Enemy("해구 파수꾼", "해구 파수꾼입니다.", 1380, 172, 82, 100, 0.12f, 500, 1720, Skill("촉수 압박", 74, 1.75f, false, stun: 0.17f)),
                    Enemy("침수된 골렘", "침수된 골렘입니다.", 1580, 166, 44, 126, 0.07f, 520, 1780, Skill("수압 강타", 68, 1.8f, false))
                },
                Enemy("심해 기록관 노틸루스", "심해 기록관 노틸루스입니다.", 4200, 178, 220, 124, 0.16f, 2850, 8200, Skill("금단의 조류", 108, 1.95f, true, poison: 0.65f), Skill("해저 봉인", 95, 1.65f, true, stun: 0.25f))),

            new DungeonData(10, "영원의 균열", 38, 6, "모든 차원의 끝과 시작이 겹쳐진 마지막 균열입니다. 현실의 법칙을 벗어난 존재들이 영웅의 한계를 시험합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("시간 파편수", "시간 파편수입니다.", 2100, 260, 210, 160, 0.18f, 820, 2900, Skill("시간 절단", 122, 1.9f, false, stun: 0.19f)),
                    Enemy("차원 심판관", "차원 심판관입니다.", 2300, 220, 280, 170, 0.16f, 860, 3050, Skill("차원 판결", 128, 1.95f, true, stun: 0.17f)),
                    Enemy("무한의 포식자", "무한의 포식자입니다.", 2600, 300, 180, 150, 0.22f, 900, 3200, Skill("공허 포식", 115, 2f, false, poison: 0.55f))
                },
                Enemy("영겁의 핵 아스트라온", "영겁의 핵 아스트라온입니다.", 7800, 320, 360, 190, 0.24f, 5000, 15000, Skill("영겁 붕괴", 162, 2.2f, true), Skill("무한 낙인", 142, 2f, false, poison: 0.7f))),

            new DungeonData(11, "흑요석 성소", 44, 6, "검은 성석이 끝없이 자라나는 성소입니다. 공허를 숭배하는 수호자들이 침입자를 제물로 삼으려 합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("흑요석 파수병", "흑요석 파수병입니다.", 3000, 350, 170, 220, 0.16f, 1050, 3900, Skill("검은 파쇄", 128, 1.95f, false, stun: 0.14f)),
                    Enemy("성소 주술사", "성소 주술사입니다.", 2700, 160, 390, 165, 0.18f, 1120, 4100, Skill("저주 성화", 149, 2f, true, poison: 0.58f))
                },
                Enemy("흑요 대사제 카르복스", "흑요 대사제 카르복스입니다.", 9800, 390, 430, 245, 0.2f, 6100, 19000, Skill("성소 붕괴", 182, 2.25f, true, burn: 0.6f), Skill("흑요 감옥", 155, 2f, false, stun: 0.17f))),

            new DungeonData(12, "별추락 요새", 50, 6, "추락한 별의 파편으로 지어진 요새입니다. 별빛에 뒤틀린 전사들이 강렬한 일격을 퍼붓습니다.",
                new List<EnemyTemplate>
                {
                    Enemy("성흔 기사", "성흔 기사입니다.", 3600, 420, 260, 240, 0.22f, 1260, 4700, Skill("유성 절단", 155, 2.1f, false)),
                    Enemy("별빛 포격수", "별빛 포격수입니다.", 3200, 190, 470, 190, 0.17f, 1320, 4900, Skill("낙성 포화", 169, 2.15f, true, burn: 0.62f))
                },
                Enemy("추락성 아르카엘", "추락성 아르카엘입니다.", 12200, 470, 520, 275, 0.24f, 7200, 24000, Skill("별의 심판", 209, 2.35f, true), Skill("중력 붕괴", 176, 2.05f, true, stun: 0.18f))),

            new DungeonData(13, "몽환 늪지", 56, 6, "꿈과 독기가 뒤섞인 늪입니다. 몬스터의 형체가 흐릿하게 흔들리며 장기전을 강요합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("몽독 수렁괴물", "몽독 수렁괴물입니다.", 4200, 430, 310, 285, 0.14f, 1450, 5700, Skill("환각 독무", 169, 1.95f, true, poison: 0.68f)),
                    Enemy("잠식된 몽령", "잠식된 몽령입니다.", 3600, 160, 560, 210, 0.2f, 1520, 5900, Skill("꿈결 속박", 182, 1.95f, true, stun: 0.16f))
                },
                Enemy("잠든 여왕 모르페나", "잠든 여왕 모르페나입니다.", 14800, 390, 620, 310, 0.22f, 8300, 30000, Skill("영면의 독안개", 230, 2.25f, true, poison: 0.72f), Skill("몽환 장막", 189, 0.48f, false))),

            new DungeonData(14, "강철나무 거목림", 62, 7, "나무껍질이 금속처럼 단단한 숲입니다. 느리지만 무거운 공격과 높은 방어력이 특징입니다.",
                new List<EnemyTemplate>
                {
                    Enemy("강철가지 파괴자", "강철가지 파괴자입니다.", 5200, 560, 120, 360, 0.12f, 1680, 6800, Skill("철목 강타", 169, 2.15f, false, stun: 0.15f)),
                    Enemy("녹슨 수액 정령", "녹슨 수액 정령입니다.", 4700, 300, 500, 320, 0.13f, 1740, 7050, Skill("부식 수액", 182, 2.05f, true, poison: 0.62f))
                },
                Enemy("강철뿌리 오르다곤", "강철뿌리 오르다곤입니다.", 19000, 650, 430, 430, 0.16f, 9600, 37000, Skill("대지 고정", 209, 2.25f, false, stun: 0.18f), Skill("철목 재생", 203, 0.42f, false))),

            new DungeonData(15, "오로라 감옥", 68, 7, "빛의 결계가 수감자를 가두는 감옥입니다. 눈부신 마법과 빙결의 압박이 이어집니다.",
                new List<EnemyTemplate>
                {
                    Enemy("오로라 간수", "오로라 간수입니다.", 5600, 360, 620, 340, 0.18f, 1900, 7900, Skill("광휘 족쇄", 196, 2.1f, true, stun: 0.17f)),
                    Enemy("빙광 죄수", "빙광 죄수입니다.", 6200, 610, 300, 380, 0.15f, 1980, 8150, Skill("얼어붙은 난동", 182, 2.25f, false))
                },
                Enemy("빛감옥장 루미라", "빛감옥장 루미라입니다.", 22500, 520, 760, 430, 0.2f, 10800, 45000, Skill("극광 처형", 257, 2.45f, true), Skill("봉인 광선", 216, 2.05f, true, stun: 0.18f))),

            new DungeonData(16, "심연 관측소", 74, 7, "공허 너머를 관측하던 탑입니다. 별 사이의 괴물들이 계산된 마법으로 압박합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("심연 점성술사", "심연 점성술사입니다.", 6500, 240, 780, 370, 0.2f, 2200, 9200, Skill("별자리 붕괴", 223, 2.35f, true, burn: 0.55f)),
                    Enemy("망원경 포식체", "망원경 포식체입니다.", 7200, 720, 420, 410, 0.22f, 2280, 9500, Skill("시야 절단", 209, 2.35f, false))
                },
                Enemy("무저성 관측자 엘드라", "무저성 관측자 엘드라입니다.", 26000, 620, 880, 470, 0.22f, 12400, 54000, Skill("무저점 관측", 284, 2.35f, true, stun: 0.17f), Skill("공허 좌표", 257, 2.25f, true, poison: 0.65f))),

            new DungeonData(17, "핏달 대성당", 80, 7, "붉은 달빛이 스테인드글라스를 타고 쏟아지는 대성당입니다. 흡혈과 치명타 공격이 위협적입니다.",
                new List<EnemyTemplate>
                {
                    Enemy("핏달 사제", "핏달 사제입니다.", 7600, 420, 760, 420, 0.24f, 2500, 10600, Skill("혈월 저주", 230, 2.2f, true, poison: 0.7f)),
                    Enemy("적월 성기사", "적월 성기사입니다.", 8400, 820, 260, 500, 0.2f, 2580, 10900, Skill("피의 참회", 216, 2.35f, false, stun: 0.16f))
                },
                Enemy("붉은 대주교 베르나크", "붉은 대주교 베르나크입니다.", 30500, 780, 940, 540, 0.26f, 14500, 65000, Skill("핏달 강림", 311, 2.55f, true), Skill("성혈 속박", 257, 2.2f, true, stun: 0.18f))),

            new DungeonData(18, "수정 시간로", 86, 8, "수정 속에 시간이 접힌 길입니다. 빠른 적들이 턴을 흔들고 강한 지속 피해를 남깁니다.",
                new List<EnemyTemplate>
                {
                    Enemy("시간 유리검사", "시간 유리검사입니다.", 8800, 920, 360, 520, 0.25f, 2850, 12300, Skill("초침 난무", 236, 2.45f, false)),
                    Enemy("수정 시계마녀", "수정 시계마녀입니다.", 8000, 300, 980, 460, 0.21f, 2940, 12600, Skill("균열 초침", 257, 2.3f, true, poison: 0.68f))
                },
                Enemy("시간결정 크로노스핀", "시간결정 크로노스핀입니다.", 35000, 900, 1080, 600, 0.26f, 16600, 78000, Skill("영겁 회전", 338, 2.45f, true, stun: 0.17f), Skill("수정 역류", 297, 0.46f, false))),

            new DungeonData(19, "공허 대장간 핵", 92, 8, "차원을 벼려 무기로 만드는 핵심 대장간입니다. 무겁고 뜨거운 공격이 방어를 시험합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("공허 제련수", "공허 제련수입니다.", 9800, 1040, 520, 640, 0.2f, 3200, 14000, Skill("차원 망치", 257, 2.55f, false, stun: 0.16f)),
                    Enemy("용광로 악령", "용광로 악령입니다.", 9100, 480, 1120, 560, 0.22f, 3300, 14400, Skill("검은 용암", 284, 2.5f, true, burn: 0.75f))
                },
                Enemy("차원 대장장이 모르둠", "차원 대장장이 모르둠입니다.", 41000, 1180, 980, 700, 0.24f, 18800, 92000, Skill("세계 단조", 365, 2.7f, false, stun: 0.18f), Skill("공허 담금질", 324, 2.45f, true, burn: 0.7f))),

            new DungeonData(20, "태초의 왕좌", 100, 9, "모든 균열의 첫 숨이 남아 있는 왕좌입니다. 마지막 공허가 영웅의 모든 성장을 시험합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("태초의 파편", "태초의 파편입니다.", 11200, 980, 1220, 680, 0.24f, 3600, 16500, Skill("기원 붕괴", 311, 2.55f, true, poison: 0.72f)),
                    Enemy("왕좌의 집행자", "왕좌의 집행자입니다.", 12500, 1280, 640, 760, 0.23f, 3800, 17000, Skill("창세 참격", 297, 2.65f, false))
                },
                Enemy("태초의 공허 제네시온", "태초의 공허 제네시온입니다.", 52000, 1380, 1450, 820, 0.28f, 25000, 120000, Skill("태초 붕괴", 432, 2.85f, true), Skill("왕좌 단죄", 392, 2.65f, false, stun: 0.18f))),

            new DungeonData(21, "찬란한 성가당", 108, 9, "빛이 지나치게 응축되어 눈을 멀게 하는 예배당입니다. 성가의 잔향이 적과 아군을 동시에 시험합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("눈부신 순례자", "눈부신 순례자입니다.", 14200, 920, 1540, 860, 0.25f, 4300, 19500, Skill("섬광 찬가", 351, 2.75f, true, blind: 0.45f)),
                    Enemy("성역 파수병", "성역 파수병입니다.", 16800, 1480, 920, 980, 0.22f, 4500, 20200, Skill("눈부신 철퇴", 324, 2.75f, false, stun: 0.16f))
                },
                Enemy("찬란한 심판관 루멘", "찬란한 심판관 루멘입니다.", 65000, 1520, 1780, 980, 0.28f, 30000, 150000, Skill("백광 판결", 486, 3f, true, blind: 0.55f), Skill("심판의 종", 432, 2.8f, true, silence: 0.35f))),

            new DungeonData(22, "천둥 공동", 116, 9, "번개가 바위 속을 흐르는 거대한 공동입니다. 감전 피해와 기습적인 기절을 조심해야 합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("전류 포식자", "전류 포식자입니다.", 17000, 1120, 1680, 900, 0.27f, 4900, 22500, Skill("전류 물어뜯기", 378, 2.9f, true, shock: 0.62f)),
                    Enemy("낙뢰 기사", "낙뢰 기사입니다.", 18600, 1650, 1180, 1040, 0.26f, 5100, 23200, Skill("낙뢰 돌격", 365, 2.95f, false, stun: 0.17f, shock: 0.35f))
                },
                Enemy("공동의 천둥룡 브론테", "공동의 천둥룡 브론테입니다.", 76000, 1780, 1980, 1120, 0.3f, 34500, 176000, Skill("천둥 포효", 527, 3.15f, true, shock: 0.7f), Skill("전하 붕괴", 486, 3f, true, stun: 0.18f))),

            new DungeonData(23, "역병 정원", 124, 10, "아름다운 꽃향기에 독과 출혈이 숨어 있는 정원입니다. 오래 끌수록 지속 피해가 쌓입니다.",
                new List<EnemyTemplate>
                {
                    Enemy("가시 만드라고라", "가시 만드라고라입니다.", 20400, 1760, 980, 1080, 0.24f, 5600, 26000, Skill("가시 분출", 405, 3.05f, false, bleed: 0.65f)),
                    Enemy("역병 향기술사", "역병 향기술사입니다.", 18800, 980, 1880, 980, 0.25f, 5800, 26800, Skill("녹색 향무", 432, 3f, true, poison: 0.78f, weaken: 0.35f))
                },
                Enemy("만개의 역병왕 베르단트", "만개의 역병왕 베르단트입니다.", 89000, 1850, 2120, 1260, 0.29f, 39000, 210000, Skill("검은 개화", 581, 3.25f, true, poison: 0.85f, bleed: 0.45f), Skill("왕의 재생", 513, 0.55f, false))),

            new DungeonData(24, "거울 보루", 132, 10, "거울벽이 공격의 방향감각을 빼앗는 보루입니다. 실명과 취약, 침묵이 전투 흐름을 흔듭니다.",
                new List<EnemyTemplate>
                {
                    Enemy("반사 검객", "반사 검객입니다.", 22600, 2050, 1100, 1320, 0.31f, 6400, 30000, Skill("반사 베기", 446, 3.1f, false, vulnerable: 0.4f)),
                    Enemy("침묵의 거울마녀", "침묵의 거울마녀입니다.", 21000, 980, 2280, 1180, 0.27f, 6600, 31200, Skill("무음 반사", 486, 3.12f, true, blind: 0.35f, silence: 0.38f))
                },
                Enemy("거울 군주 스페큘라", "거울 군주 스페큘라입니다.", 104000, 2150, 2460, 1440, 0.33f, 44000, 250000, Skill("만상 반전", 635, 3.35f, true, blind: 0.55f, vulnerable: 0.45f), Skill("거울 속 침묵", 567, 3.1f, true, silence: 0.42f))),

            new DungeonData(25, "잿폭풍 전선", 140, 10, "타오른 전장이 끝없이 재가 되어 흩날립니다. 화상과 약화가 동시에 밀려옵니다.",
                new List<EnemyTemplate>
                {
                    Enemy("잿바람 포병", "잿바람 포병입니다.", 25200, 1600, 2520, 1380, 0.29f, 7200, 34800, Skill("소이 포격", 527, 3.35f, true, burn: 0.82f, weaken: 0.35f)),
                    Enemy("재의 돌격병", "재의 돌격병입니다.", 27800, 2380, 920, 1560, 0.26f, 7400, 35600, Skill("그을린 방패벽", 459, 3.22f, false, weaken: 0.5f))
                },
                Enemy("잿폭풍 장군 이그라스", "잿폭풍 장군 이그라스입니다.", 122000, 2500, 2680, 1620, 0.32f, 50000, 296000, Skill("전선 초토화", 702, 3.55f, true, burn: 0.88f), Skill("잿빛 압박", 621, 3.35f, false, weaken: 0.45f, vulnerable: 0.55f))),

            new DungeonData(26, "얼어붙은 별낙하", 148, 11, "추락한 별이 얼음 속에서 맥동하는 장소입니다. 빙결과 마나 연소가 전투 리듬을 끊습니다.",
                new List<EnemyTemplate>
                {
                    Enemy("서리 별조각", "서리 별조각입니다.", 30000, 1400, 2920, 1640, 0.3f, 8200, 40500, Skill("빙성 파편", 581, 3.5f, true, freeze: 0.28f, manaBurn: 0.35f)),
                    Enemy("오한 관측자", "오한 관측자입니다.", 28400, 1280, 3060, 1520, 0.31f, 8400, 41800, Skill("절대영도 시선", 621, 3.45f, true, freeze: 0.32f, blind: 0.4f))
                },
                Enemy("얼어붙은 혜성 이스카론", "얼어붙은 혜성 이스카론입니다.", 146000, 2350, 3240, 1800, 0.34f, 57000, 350000, Skill("혜성 정지", 756, 3.75f, true, freeze: 0.38f), Skill("별빛 연소", 675, 3.45f, true, manaBurn: 0.55f))),

            new DungeonData(27, "뱀문자 기록원", 156, 11, "살아 움직이는 문자가 독과 침묵의 주문을 새기는 기록원입니다.",
                new List<EnemyTemplate>
                {
                    Enemy("문자 독사", "문자 독사입니다.", 33800, 2600, 1900, 1720, 0.34f, 9200, 46200, Skill("독문 각인", 621, 3.55f, false, poison: 0.85f, silence: 0.28f)),
                    Enemy("봉인 서기관", "봉인 서기관입니다.", 32000, 1320, 3420, 1680, 0.3f, 9400, 47500, Skill("봉인 문장", 675, 3.62f, true, vulnerable: 0.35f, silence: 0.48f))
                },
                Enemy("고문서의 뱀왕 세르펜", "고문서의 뱀왕 세르펜입니다.", 172000, 2950, 3560, 1980, 0.35f, 65000, 420000, Skill("왕의 봉인문", 837, 3.85f, true, silence: 0.52f), Skill("독사 성문", 783, 3.75f, false, poison: 0.9f, bleed: 0.45f))),

            new DungeonData(28, "일식 첨탑", 164, 11, "태양과 달이 겹친 그림자 첨탑입니다. 실명, 취약, 흡혈성 공격이 이어집니다.",
                new List<EnemyTemplate>
                {
                    Enemy("일식 추적자", "일식 추적자입니다.", 36800, 3200, 1980, 1960, 0.38f, 10400, 52500, Skill("그림자 추적", 702, 3.72f, false, blind: 0.45f)),
                    Enemy("흑일 마도사", "흑일 마도사입니다.", 35000, 1420, 3860, 1840, 0.34f, 10600, 53800, Skill("흑태양 낙인", 756, 3.82f, true, blind: 0.35f, vulnerable: 0.55f))
                },
                Enemy("일식의 성녀 노크티아", "일식의 성녀 노크티아입니다.", 205000, 3250, 4100, 2200, 0.38f, 74000, 500000, Skill("개기일식", 918, 4.05f, true, blind: 0.65f, vulnerable: 0.5f), Skill("어둠의 성가", 824, 0.6f, false))),

            new DungeonData(29, "태초 소용돌이", 172, 12, "사원소가 한꺼번에 뒤엉킨 원초의 소용돌이입니다. 모든 상태이상이 복합적으로 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("사원소 포식체", "사원소 포식체입니다.", 42000, 2800, 4300, 2240, 0.36f, 12200, 62000, Skill("원소 난류", 864, 4f, true, burn: 0.55f, shock: 0.55f, blind: 0.35f)),
                    Enemy("원초의 파수핵", "원초의 파수핵입니다.", 46000, 3900, 2800, 2520, 0.34f, 12600, 63800, Skill("핵심 압괴", 837, 4f, false, bleed: 0.45f, weaken: 0.55f))
                },
                Enemy("원초폭풍 칼라미타스", "원초폭풍 칼라미타스입니다.", 245000, 4100, 4650, 2600, 0.4f, 86000, 610000, Skill("사원소 붕괴", 1026, 4.3f, true, burn: 0.65f, shock: 0.65f, freeze: 0.25f), Skill("태초의 압력", 945, 4.15f, false, weaken: 0.55f, vulnerable: 0.55f))),

            new DungeonData(30, "에테리아 심장부", 180, 12, "모든 던전과 차원을 움직이는 심장입니다. 최종 수호자가 영웅의 완성도를 시험합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("심장부 맥동체", "심장부 맥동체입니다.", 50000, 3600, 5100, 2700, 0.39f, 14500, 75000, Skill("에테르 맥동", 1026, 4.35f, true, shock: 0.5f, manaBurn: 0.55f)),
                    Enemy("완성의 집행자", "완성의 집행자입니다.", 56000, 5000, 3200, 3000, 0.38f, 15000, 78000, Skill("완성의 참격", 972, 4.35f, false, bleed: 0.6f, vulnerable: 0.45f))
                },
                Enemy("에테리아의 심장 아르카이온", "에테리아의 심장 아르카이온입니다.", 320000, 5200, 5600, 3300, 0.42f, 120000, 800000, Skill("세계 심장 박동", 1215, 4.7f, true, shock: 0.7f, manaBurn: 0.55f), Skill("종언의 궤도", 1134, 4.55f, false, bleed: 0.7f, vulnerable: 0.55f))),

            new DungeonData(31, "은월 기록성", 188, 12, "실명, 침묵, 마나 연소 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("은월 기록성 파수체 1", "은월 기록성 파수체 1입니다.", 77500, 4785, 6380, 4160, 0.36f, 19900, 108000, Skill("차원 봉인", 1207, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("은월 기록성 파수체 2", "은월 기록성 파수체 2입니다.", 88319, 6592, 4747, 4297, 0.38f, 20422, 111240, Skill("근원 절단", 1224, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("은월 기록성 파수체 3", "은월 기록성 파수체 3입니다.", 99696, 5104, 6805, 4434, 0.4f, 20944, 114480, Skill("차원 봉인", 1243, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("은월 사서 셀레네", "은월 사서 셀레네입니다.", 458000, 7550, 7920, 4900, 0.42f, 182000, 1160000, Skill("왕권 붕괴", 1451, 4.75f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 1539, 4.9f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(32, "잿빛 시계탑", 196, 12, "감전, 출혈, 취약 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("잿빛 시계탑 파수체 1", "잿빛 시계탑 파수체 1입니다.", 93000, 5670, 7560, 4920, 0.36f, 22800, 126000, Skill("차원 봉인", 1307, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("잿빛 시계탑 파수체 2", "잿빛 시계탑 파수체 2입니다.", 105369, 7772, 5596, 5057, 0.38f, 23322, 129240, Skill("근원 절단", 1324, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("잿빛 시계탑 파수체 3", "잿빛 시계탑 파수체 3입니다.", 118296, 5989, 7985, 5194, 0.4f, 23844, 132480, Skill("차원 봉인", 1343, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("잿빛 시계공 오르로크", "잿빛 시계공 오르로크입니다.", 556000, 9200, 9640, 6000, 0.42f, 224000, 1420000, Skill("왕권 붕괴", 1580, 4.8f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 1674, 4.96f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(33, "수정 심연", 204, 12, "빙결, 취약, 마법 피해 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("수정 심연 파수체 1", "수정 심연 파수체 1입니다.", 108500, 6555, 8740, 5680, 0.36f, 25700, 144000, Skill("차원 봉인", 1407, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("수정 심연 파수체 2", "수정 심연 파수체 2입니다.", 122419, 8952, 6446, 5817, 0.38f, 26222, 147240, Skill("근원 절단", 1424, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("수정 심연 파수체 3", "수정 심연 파수체 3입니다.", 136896, 6874, 9165, 5954, 0.4f, 26744, 150480, Skill("차원 봉인", 1443, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("심연 수정체 네레이드", "심연 수정체 네레이드입니다.", 654000, 10850, 11360, 7100, 0.42f, 266000, 1680000, Skill("왕권 붕괴", 1708, 4.85f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 1809, 5.01f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(34, "무음 태양사원", 212, 12, "침묵, 실명, 약화 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("무음 태양사원 파수체 1", "무음 태양사원 파수체 1입니다.", 124000, 7440, 9920, 6440, 0.36f, 28600, 162000, Skill("차원 봉인", 1507, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("무음 태양사원 파수체 2", "무음 태양사원 파수체 2입니다.", 139469, 10132, 7295, 6577, 0.38f, 29122, 165240, Skill("근원 절단", 1524, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("무음 태양사원 파수체 3", "무음 태양사원 파수체 3입니다.", 155496, 7759, 10345, 6714, 0.4f, 29644, 168480, Skill("차원 봉인", 1543, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("무음의 태양 라하르", "무음의 태양 라하르입니다.", 752000, 12500, 13080, 8200, 0.42f, 308000, 1940000, Skill("왕권 붕괴", 1836, 4.9f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 1944, 5.07f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(35, "월식 투기장", 220, 12, "치명타, 기절, 출혈 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("월식 투기장 파수체 1", "월식 투기장 파수체 1입니다.", 139500, 8325, 11100, 7200, 0.36f, 31500, 180000, Skill("차원 봉인", 1607, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("월식 투기장 파수체 2", "월식 투기장 파수체 2입니다.", 156519, 11312, 8145, 7337, 0.38f, 32022, 183240, Skill("근원 절단", 1624, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("월식 투기장 파수체 3", "월식 투기장 파수체 3입니다.", 174096, 8644, 11525, 7474, 0.4f, 32544, 186480, Skill("차원 봉인", 1643, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("월식 군주 발테온", "월식 군주 발테온입니다.", 850000, 14150, 14800, 9300, 0.42f, 350000, 2200000, Skill("왕권 붕괴", 1964, 4.95f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 2079, 5.13f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(36, "천사 잔해지", 230, 13, "실명, 재생, 취약 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("천사 잔해지 파수체 1", "천사 잔해지 파수체 1입니다.", 155000, 9210, 12280, 7960, 0.36f, 34400, 198000, Skill("차원 봉인", 1706, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("천사 잔해지 파수체 2", "천사 잔해지 파수체 2입니다.", 173569, 12492, 8995, 8097, 0.38f, 34922, 201240, Skill("근원 절단", 1724, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("천사 잔해지 파수체 3", "천사 잔해지 파수체 3입니다.", 192696, 9529, 12705, 8234, 0.4f, 35444, 204480, Skill("차원 봉인", 1743, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("부서진 세라프 아제르", "부서진 세라프 아제르입니다.", 948000, 15800, 16520, 10400, 0.42f, 392000, 2460000, Skill("왕권 붕괴", 2093, 5f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 2214, 5.18f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(37, "천독 저장고", 240, 13, "중독, 출혈, 마나 연소 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("천독 저장고 파수체 1", "천독 저장고 파수체 1입니다.", 170500, 10095, 13460, 8720, 0.36f, 37300, 216000, Skill("차원 봉인", 1806, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("천독 저장고 파수체 2", "천독 저장고 파수체 2입니다.", 190619, 13672, 9844, 8857, 0.38f, 37822, 219240, Skill("근원 절단", 1824, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("천독 저장고 파수체 3", "천독 저장고 파수체 3입니다.", 211296, 10414, 13885, 8994, 0.4f, 38344, 222480, Skill("차원 봉인", 1843, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("천독 군주 모르바인", "천독 군주 모르바인입니다.", 1046000, 17450, 18240, 11500, 0.42f, 434000, 2720000, Skill("왕권 붕괴", 2221, 5.05f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 2349, 5.23f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(38, "폭풍유리 해역", 250, 13, "감전, 빙결, 침묵 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("폭풍유리 해역 파수체 1", "폭풍유리 해역 파수체 1입니다.", 186000, 10980, 14640, 9480, 0.36f, 40200, 234000, Skill("차원 봉인", 1906, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("폭풍유리 해역 파수체 2", "폭풍유리 해역 파수체 2입니다.", 207669, 14852, 10694, 9617, 0.38f, 40722, 237240, Skill("근원 절단", 1924, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("폭풍유리 해역 파수체 3", "폭풍유리 해역 파수체 3입니다.", 229896, 11299, 15065, 9754, 0.4f, 41244, 240480, Skill("차원 봉인", 1943, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("유리폭풍 리바이아", "유리폭풍 리바이아입니다.", 1144000, 19100, 19960, 12600, 0.42f, 476000, 2980000, Skill("왕권 붕괴", 2349, 5.1f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 2484, 5.29f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(39, "붉은 거울미궁", 260, 13, "실명, 취약, 치명타 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("붉은 거울미궁 파수체 1", "붉은 거울미궁 파수체 1입니다.", 201500, 11865, 15820, 10240, 0.36f, 43100, 252000, Skill("차원 봉인", 2006, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("붉은 거울미궁 파수체 2", "붉은 거울미궁 파수체 2입니다.", 224719, 16032, 11543, 10377, 0.38f, 43622, 255240, Skill("근원 절단", 2024, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("붉은 거울미궁 파수체 3", "붉은 거울미궁 파수체 3입니다.", 248496, 12184, 16245, 10514, 0.4f, 44144, 258480, Skill("차원 봉인", 2043, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("붉은 거울왕 비트라", "붉은 거울왕 비트라입니다.", 1242000, 20750, 21680, 13700, 0.42f, 518000, 3240000, Skill("왕권 붕괴", 2477, 5.15f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 2619, 5.34f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(40, "무한성좌", 270, 13, "복합 상태이상, 고마나전 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("무한성좌 파수체 1", "무한성좌 파수체 1입니다.", 217000, 12750, 17000, 11000, 0.36f, 46000, 270000, Skill("차원 봉인", 2106, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("무한성좌 파수체 2", "무한성좌 파수체 2입니다.", 241769, 17212, 12393, 11137, 0.38f, 46522, 273240, Skill("근원 절단", 2124, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("무한성좌 파수체 3", "무한성좌 파수체 3입니다.", 267096, 13069, 17425, 11274, 0.4f, 47044, 276480, Skill("차원 봉인", 2142, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("무한성좌 아스트라", "무한성좌 아스트라입니다.", 1340000, 22400, 23400, 14800, 0.42f, 560000, 3500000, Skill("왕권 붕괴", 2606, 5.2f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 2754, 5.4f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(41, "흑련 성역", 282, 14, "침묵, 약화, 보호막 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("흑련 성역 파수체 1", "흑련 성역 파수체 1입니다.", 232500, 13635, 18180, 11760, 0.36f, 48900, 288000, Skill("차원 봉인", 2206, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("흑련 성역 파수체 2", "흑련 성역 파수체 2입니다.", 258819, 18392, 13243, 11897, 0.38f, 49422, 291240, Skill("근원 절단", 2223, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("흑련 성역 파수체 3", "흑련 성역 파수체 3입니다.", 285696, 13954, 18605, 12034, 0.4f, 49944, 294480, Skill("차원 봉인", 2242, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("흑련의 성자 나르키온", "흑련의 성자 나르키온입니다.", 1438000, 24050, 25120, 15900, 0.42f, 602000, 3760000, Skill("왕권 붕괴", 2734, 5.25f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 2889, 5.46f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(42, "태고 용광로", 294, 14, "화상, 감전, 취약 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("태고 용광로 파수체 1", "태고 용광로 파수체 1입니다.", 248000, 14520, 19360, 12520, 0.36f, 51800, 306000, Skill("차원 봉인", 2306, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("태고 용광로 파수체 2", "태고 용광로 파수체 2입니다.", 275869, 19572, 14092, 12657, 0.38f, 52322, 309240, Skill("근원 절단", 2323, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("태고 용광로 파수체 3", "태고 용광로 파수체 3입니다.", 304296, 14839, 19785, 12794, 0.4f, 52844, 312480, Skill("차원 봉인", 2342, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("태고 대장장이 브루칸", "태고 대장장이 브루칸입니다.", 1536000, 25700, 26840, 17000, 0.42f, 644000, 4020000, Skill("왕권 붕괴", 2862, 5.3f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 3024, 5.51f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(43, "공허 관현당", 306, 14, "실명, 침묵, 마나 연소 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("공허 관현당 파수체 1", "공허 관현당 파수체 1입니다.", 263500, 15405, 20540, 13280, 0.36f, 54700, 324000, Skill("차원 봉인", 2406, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("공허 관현당 파수체 2", "공허 관현당 파수체 2입니다.", 292919, 20752, 14942, 13417, 0.38f, 55222, 327240, Skill("근원 절단", 2423, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("공허 관현당 파수체 3", "공허 관현당 파수체 3입니다.", 322896, 15724, 20965, 13554, 0.4f, 55744, 330480, Skill("차원 봉인", 2442, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("공허 지휘자 오르페온", "공허 지휘자 오르페온입니다.", 1634000, 27350, 28560, 18100, 0.42f, 686000, 4280000, Skill("왕권 붕괴", 2990, 5.35f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 3159, 5.56f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(44, "무관의 왕좌", 318, 14, "기절, 출혈, 약화 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("무관의 왕좌 파수체 1", "무관의 왕좌 파수체 1입니다.", 279000, 16290, 21720, 14040, 0.36f, 57600, 342000, Skill("차원 봉인", 2506, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("무관의 왕좌 파수체 2", "무관의 왕좌 파수체 2입니다.", 309969, 21932, 15791, 14177, 0.38f, 58122, 345240, Skill("근원 절단", 2523, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("무관의 왕좌 파수체 3", "무관의 왕좌 파수체 3입니다.", 341496, 16609, 22145, 14314, 0.4f, 58644, 348480, Skill("차원 봉인", 2542, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("무관왕 레갈리온", "무관왕 레갈리온입니다.", 1732000, 29000, 30280, 19200, 0.42f, 728000, 4540000, Skill("왕권 붕괴", 3119, 5.4f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 3294, 5.62f, true, blind: 0.5f, silence: 0.42f))),

            new DungeonData(45, "근원의 뿌리", 330, 14, "최종 복합 상태이상 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.",
                new List<EnemyTemplate>
                {
                    Enemy("근원의 뿌리 파수체 1", "근원의 뿌리 파수체 1입니다.", 294500, 17175, 22900, 14800, 0.36f, 60500, 360000, Skill("차원 봉인", 2606, 4.4f, true, blind: 0.32f, silence: 0.38f)),
                    Enemy("근원의 뿌리 파수체 2", "근원의 뿌리 파수체 2입니다.", 327019, 23112, 16641, 14937, 0.38f, 61022, 363240, Skill("근원 절단", 2623, 4.49f, false, bleed: 0.58f, vulnerable: 0.38f)),
                    Enemy("근원의 뿌리 파수체 3", "근원의 뿌리 파수체 3입니다.", 360096, 17494, 23325, 15074, 0.4f, 61544, 366480, Skill("차원 봉인", 2642, 4.64f, true, blind: 0.32f, silence: 0.38f))
                },
                Enemy("근원의 왕 엘라드리온", "근원의 왕 엘라드리온입니다.", 1830000, 30650, 32000, 20300, 0.42f, 770000, 4800000, Skill("왕권 붕괴", 3247, 5.45f, false, stun: 0.22f, vulnerable: 0.52f), Skill("근원 주문", 3429, 5.67f, true, blind: 0.5f, silence: 0.42f)))

        };
    }

    private static EnemyTemplate Enemy(string name, string description, int hp, int attack, int magic, int defense, float crit, int gold, int xp, params SkillState[] skills)
    {
        return new EnemyTemplate(name, description, hp, 0, attack, magic, defense, 0, crit, gold, xp, new List<SkillState>(skills));
    }

    private static SkillState Skill(string name, int mpCost, float multiplier, bool magic, float stun = 0f, float burn = 0f, float poison = 0f, float bleed = 0f, float shock = 0f, float freeze = 0f, float blind = 0f, float weaken = 0f, float vulnerable = 0f, float silence = 0f, float manaBurn = 0f)
    {
        return new SkillState(name, mpCost, multiplier, magic)
        {
            stunChance = stun,
            burnChance = burn,
            poisonChance = poison,
            bleedChance = bleed,
            shockChance = shock,
            freezeChance = freeze,
            blindChance = blind,
            weakenChance = weaken,
            vulnerableChance = vulnerable,
            silenceChance = silence,
            manaBurnChance = manaBurn
        };
    }

    private ItemState CreateRandomItem(int stage)
    {
        var safeStage = Mathf.Max(1, stage);
        return CreateRandomItem(safeStage, PickRarity(safeStage, false));
    }

    private ItemState CreateRandomItem(int stage, int rarity)
    {
        stage = Mathf.Max(1, stage);
        rarity = Mathf.Clamp(rarity, 0, NamedRarity - 1);
        var heroClass = player != null ? player.heroClass : "성기사";
        var level = player != null ? Mathf.Max(1, player.level) : 1;
        return BuildGeneratedGearItem(PickRandomGearType(), rarity, heroClass, level, stage);
    }

    private ItemState CreateBossItem(int stage)
    {
        stage = Mathf.Max(1, stage);
        var named = TryCreateNamedBossItem(stage);
        if (named != null && UnityEngine.Random.value < 0.1f)
        {
            return named;
        }

        return CreateRandomItem(stage, PickRarity(stage, true));
    }

    private ItemState TryCreateNamedBossItem(int stage)
    {
        if (stage == 5) return BuildNamedBossItem(stage, "제피로스의 폭풍핵", "Charm");
        if (stage == 10) return BuildNamedBossItem(stage, "아트라스 균열검", "Weapon");
        if (stage == 15) return BuildNamedBossItem(stage, "루미나 오로라 성배", "Charm");
        if (stage == 20) return BuildNamedBossItem(stage, "제네시온 왕좌갑", "Armor");
        if (stage == 25) return BuildNamedBossItem(stage, "이그라스 잿폭풍 인장", "Charm");
        if (stage == 30) return BuildNamedBossItem(stage, "아르카이온 심장 파편", "Charm");
        if (stage == 35) return BuildNamedBossItem(stage, "월식 군주의 결속구", "Weapon");
        if (stage == 40) return BuildNamedBossItem(stage, "무한성좌의 예복", "Armor");
        if (stage == 45) return BuildNamedBossItem(stage, "근원의 왕관", "Charm");
        return null;
    }

    private ItemState BuildGeneratedGearItem(string type, int rarity, string heroClass, int level, int stage)
    {
        if (type != "Weapon" && type != "Armor" && type != "Charm")
        {
            type = "Charm";
        }

        rarity = Mathf.Clamp(rarity, 0, NamedRarity - 1);
        level = Mathf.Max(1, level);
        stage = Mathf.Max(1, stage);
        string[] accessoryBaseStats = null;
        var suffix = PickItemSuffix(type, heroClass);
        if (type == "Charm")
        {
            accessoryBaseStats = PickAccessoryBaseStats(heroClass, out suffix);
        }

        var item = new ItemState
        {
            id = Guid.NewGuid().ToString(),
            name = PickItemPrefix(type, rarity) + " " + suffix,
            type = type,
            rarity = rarity,
            level = 0
        };

        ApplyGeneratedGearStats(item, heroClass, level, stage, accessoryBaseStats);
        ClampItemRateStats(item);
        item.power = GearPowerFromStats(item);
        return item;
    }

    private string PickRandomGearType()
    {
        var roll = UnityEngine.Random.value;
        if (roll < 0.34f) return "Weapon";
        if (roll < 0.67f) return "Armor";
        return "Charm";
    }

    private void ApplyGeneratedGearStats(ItemState item, string heroClass, int level, int stage, string[] accessoryBaseStats = null)
    {
        if (item == null)
        {
            return;
        }

        if (item.type != "Weapon" && item.type != "Armor" && item.type != "Charm")
        {
            item.type = "Charm";
        }

        item.rarity = Mathf.Clamp(item.rarity, 0, NamedRarity);
        level = Mathf.Max(1, level);
        stage = Mathf.Max(1, stage);
        ResetGearStats(item);
        var rarityMult = RarityMultiplier(item.rarity);
        var dungeonMult = DungeonGearMultiplier(stage);
        var statMult = rarityMult * GearLevelMultiplier(level) * dungeonMult;

        if (item.type == "Weapon")
        {
            if (UsesMagicWeaponGear(heroClass))
            {
                item.magic = RoundToGameInt(14f * statMult);
                item.maxMp = RoundToGameInt(14f * statMult);
            }
            else
            {
                item.attack = RoundToGameInt(8f * statMult);
                if (GearClassId(heroClass) == "rogue")
                {
                    item.statusPower = RoundRate((0.06f + rarityMult * 0.025f) * dungeonMult);
                }
            }
        }
        else if (item.type == "Armor")
        {
            item.defense = RoundToGameInt(4f * statMult);
            item.maxHp = RoundToGameInt(25f * statMult);
        }
        else
        {
            var baseStats = accessoryBaseStats;
            if (baseStats == null)
            {
                string unusedName;
                baseStats = PickAccessoryBaseStats(heroClass, out unusedName);
            }

            for (var i = 0; i < baseStats.Length; i++)
            {
                AddAccessoryStat(item, baseStats[i], statMult, rarityMult, dungeonMult);
            }

            var bonusCount = RarityBonusOptionCount(item.rarity);
            for (var i = 0; i < bonusCount; i++)
            {
                var bonusStat = PickAccessoryBonusStat(item, heroClass);
                if (string.IsNullOrEmpty(bonusStat))
                {
                    break;
                }

                AddAccessoryStat(item, bonusStat, statMult, rarityMult, dungeonMult);
            }
        }
    }

    private ItemState BuildNamedBossItem(int stage, string name, string type)
    {
        stage = Mathf.Max(1, stage);
        if (type != "Weapon" && type != "Armor" && type != "Charm")
        {
            type = "Charm";
        }

        var item = new ItemState
        {
            id = "named_" + stage + "_" + Guid.NewGuid(),
            name = name,
            type = type,
            rarity = NamedRarity,
            level = 0
        };

        var level = player != null ? player.level : 1;
        var flatMult = GearLevelMultiplier(level) * DungeonGearMultiplier(stage);
        var rateMult = DungeonGearMultiplier(stage);
        if (stage == 5) { item.speed = NamedFlat(4f, flatMult); item.critRate = NamedRate(0.05f, rateMult); item.evasion = NamedRate(0.04f, rateMult); }
        else if (stage == 10) { item.attack = NamedFlat(12f, flatMult); item.magic = NamedFlat(12f, flatMult); item.statusPower = NamedRate(0.04f, rateMult); }
        else if (stage == 15) { item.maxMp = NamedFlat(25f, flatMult); item.manaRegen = NamedRate(0.04f, rateMult, true); item.damageReduction = NamedRate(0.04f, rateMult); }
        else if (stage == 20) { item.defense = NamedFlat(7f, flatMult); item.maxHp = NamedFlat(45f, flatMult); }
        else if (stage == 25) { item.critDamage = NamedRate(0.12f, rateMult); item.damageReduction = NamedRate(0.05f, rateMult); item.lifeSteal = NamedRate(0.04f, rateMult); }
        else if (stage == 30) { item.maxMp = NamedFlat(45f, flatMult); item.statusPower = NamedRate(0.1f, rateMult); item.itemFind = NamedRate(0.08f, rateMult); }
        else if (stage == 35) { item.attack = NamedFlat(18f, flatMult); item.magic = NamedFlat(18f, flatMult); item.statusPower = NamedRate(0.08f, rateMult); }
        else if (stage == 40) { item.defense = NamedFlat(10f, flatMult); item.maxHp = NamedFlat(60f, flatMult); }
        else if (stage == 45) { item.critRate = NamedRate(0.1f, rateMult); item.critDamage = NamedRate(0.25f, rateMult); item.statusPower = NamedRate(0.15f, rateMult); item.maxMp = NamedFlat(80f, flatMult); }

        ClampItemRateStats(item);
        item.power = GearPowerFromStats(item);
        return item;
    }

    private void ResetGearStats(ItemState item)
    {
        item.maxHp = 0;
        item.maxMp = 0;
        item.attack = 0;
        item.magic = 0;
        item.defense = 0;
        item.critRate = 0f;
        item.damageReduction = 0f;
        item.lifeSteal = 0f;
        item.manaRegen = 0f;
        item.statusPower = 0f;
        item.itemFind = 0f;
        item.critDamage = 0f;
        item.evasion = 0f;
        item.speed = 0;
    }

    private float GearLevelMultiplier(int level)
    {
        return 1f + Mathf.Max(0, level - 1) * GearEnhancementGrowth;
    }

    private string GearClassId(string heroClass)
    {
        if (heroClass == "원소술사") return "mage";
        if (heroClass == "그림자 자객" || heroClass == "그림자 궁수" || heroClass == "Ranger") return "rogue";
        if (heroClass == "빛의 사제") return "priest";
        if (heroClass == "폭렬술사") return "bomber";
        if (heroClass == "정령술사") return "spirit";
        if (heroClass == "바람 궁수") return "archer";
        if (heroClass == "무투가") return "monk";
        return "knight";
    }

    private bool UsesMagicWeaponGear(string heroClass)
    {
        var classId = GearClassId(heroClass);
        return classId == "mage" || classId == "priest" || classId == "bomber" || classId == "spirit";
    }

    private string[] PreferredAccessoryStats(string heroClass)
    {
        var classId = GearClassId(heroClass);
        if (classId == "mage" || classId == "spirit") return new[] { "manaRegen", "maxMp" };
        if (classId == "rogue") return new[] { "evasion", "statusPower" };
        if (classId == "priest") return new[] { "manaRegen", "lifeSteal" };
        if (classId == "bomber") return new[] { "critDamage", "statusPower" };
        if (classId == "archer" || classId == "monk") return new[] { "critRate", "critDamage" };
        return new[] { "lifeSteal", "damageReduction" };
    }

    private string[] PickAccessoryBaseStats(string heroClass, out string accessoryName)
    {
        var names = new[]
        {
            "마력 반지",
            "현자의 목걸이",
            "칼날 귀걸이",
            "수호 부적",
            "탐험가 문장",
            "흡혈 팔찌",
            "바람 허리띠",
            "저주 룬석",
            "전술 브로치",
            "비전 성배",
            "밤안개 망토 장식",
            "차원 시계"
        };
        var bases = new[]
        {
            new[] { "maxMp", "critRate" },
            new[] { "maxMp", "manaRegen" },
            new[] { "critRate", "critDamage" },
            new[] { "damageReduction", "evasion" },
            new[] { "itemFind", "speed" },
            new[] { "lifeSteal", "critDamage" },
            new[] { "speed", "evasion" },
            new[] { "statusPower", "maxMp" },
            new[] { "critRate", "damageReduction" },
            new[] { "manaRegen", "statusPower" },
            new[] { "evasion", "lifeSteal" },
            new[] { "speed", "critDamage" }
        };

        var preferred = PreferredAccessoryStats(heroClass);
        var total = 0f;
        var weights = new float[bases.Length];
        for (var i = 0; i < bases.Length; i++)
        {
            var matches = 0;
            for (var statIndex = 0; statIndex < bases[i].Length; statIndex++)
            {
                if (IsPreferredAccessoryStat(bases[i][statIndex], preferred))
                {
                    matches++;
                }
            }

            weights[i] = 1f + matches * 4f;
            total += weights[i];
        }

        var roll = UnityEngine.Random.value * total;
        for (var i = 0; i < bases.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
            {
                accessoryName = names[i];
                return bases[i];
            }
        }

        accessoryName = names[names.Length - 1];
        return bases[bases.Length - 1];
    }

    private string PickAccessoryBonusStat(ItemState item, string heroClass)
    {
        var pool = new[] { "maxMp", "speed", "critRate", "critDamage", "evasion", "damageReduction", "lifeSteal", "manaRegen", "statusPower", "itemFind" };
        var preferred = PreferredAccessoryStats(heroClass);
        var total = 0f;
        var weights = new float[pool.Length];
        for (var i = 0; i < pool.Length; i++)
        {
            if (HasGearStat(item, pool[i]))
            {
                continue;
            }

            weights[i] = IsPreferredAccessoryStat(pool[i], preferred) ? 5f : 1f;
            total += weights[i];
        }

        if (total <= 0f)
        {
            return "";
        }

        var roll = UnityEngine.Random.value * total;
        for (var i = 0; i < pool.Length; i++)
        {
            if (weights[i] <= 0f)
            {
                continue;
            }

            roll -= weights[i];
            if (roll <= 0f)
            {
                return pool[i];
            }
        }

        return "";
    }

    private bool IsPreferredAccessoryStat(string statName, string[] preferred)
    {
        for (var i = 0; i < preferred.Length; i++)
        {
            if (preferred[i] == statName)
            {
                return true;
            }
        }

        return false;
    }

    private void AddAccessoryStat(ItemState item, string statName, float statMult, float rarityMult, float dungeonMult)
    {
        if (item == null || string.IsNullOrEmpty(statName))
        {
            return;
        }

        if (statName == "maxMp") item.maxMp += RoundToGameInt(18f * statMult);
        else if (statName == "speed") item.speed += Mathf.Max(1, RoundToGameInt(2f * statMult));
        else if (statName == "critRate") item.critRate += RoundRate((0.02f + rarityMult * 0.012f) * dungeonMult);
        else if (statName == "critDamage") item.critDamage += RoundRate((0.08f + rarityMult * 0.03f) * dungeonMult);
        else if (statName == "evasion") item.evasion += RoundRate((0.03f + rarityMult * 0.01f) * dungeonMult);
        else if (statName == "damageReduction") item.damageReduction += RoundRate((0.03f + rarityMult * 0.01f) * dungeonMult);
        else if (statName == "lifeSteal") item.lifeSteal += RoundRate((0.03f + rarityMult * 0.008f) * dungeonMult);
        else if (statName == "manaRegen") item.manaRegen = Mathf.Min(ManaRegenCap, item.manaRegen + RoundRate((0.015f + rarityMult * 0.004f) * dungeonMult));
        else if (statName == "statusPower") item.statusPower += RoundRate((0.08f + rarityMult * 0.03f) * dungeonMult);
        else if (statName == "itemFind") item.itemFind += RoundRate((0.05f + rarityMult * 0.02f) * dungeonMult);
    }

    private bool HasGearStat(ItemState item, string statName)
    {
        if (item == null || string.IsNullOrEmpty(statName))
        {
            return false;
        }

        if (statName == "maxMp") return item.maxMp != 0;
        if (statName == "speed") return item.speed != 0;
        if (statName == "critRate") return item.critRate != 0f;
        if (statName == "critDamage") return item.critDamage != 0f;
        if (statName == "evasion") return item.evasion != 0f;
        if (statName == "damageReduction") return item.damageReduction != 0f;
        if (statName == "lifeSteal") return item.lifeSteal != 0f;
        if (statName == "manaRegen") return item.manaRegen != 0f;
        if (statName == "statusPower") return item.statusPower != 0f;
        if (statName == "itemFind") return item.itemFind != 0f;
        return false;
    }

    private int RarityBonusOptionCount(int rarity)
    {
        if (rarity >= 2 && rarity <= 5) return 1;
        if (rarity >= 6 && rarity <= 8) return 2;
        if (rarity >= 9) return 3;
        return 0;
    }

    private float RoundRate(float value)
    {
        return RoundToGameHundredth(value);
    }

    private int NamedFlat(float value, float multiplier)
    {
        return RoundToGameInt(value * multiplier);
    }

    private float NamedRate(float value, float multiplier, bool manaRegen = false)
    {
        var result = RoundRate(value * multiplier);
        return manaRegen ? Mathf.Min(ManaRegenCap, result) : result;
    }

    private int GearPowerFromStats(ItemState item)
    {
        if (item == null)
        {
            return 0;
        }

        var score = Mathf.Max(0, item.attack) + Mathf.Max(0, item.magic) + Mathf.Max(0, item.defense) * 2f + Mathf.Max(0, item.maxHp) * 0.2f + Mathf.Max(0, item.maxMp) * 0.25f + Mathf.Max(0, item.speed) * 4f;
        score += (Mathf.Max(0f, item.critRate) + Mathf.Max(0f, item.critDamage) + Mathf.Max(0f, item.evasion) + Mathf.Max(0f, item.damageReduction) + Mathf.Max(0f, item.lifeSteal) + Mathf.Max(0f, item.manaRegen) + Mathf.Max(0f, item.statusPower) + Mathf.Max(0f, item.itemFind)) * 100f;
        return Mathf.Max(1, RoundToGameInt(score));
    }

    private int PickRarity(int dungeonNumber, bool boss)
    {
        dungeonNumber = Mathf.Max(1, dungeonNumber);
        var max = MaxRarityForDungeon(dungeonNumber);
        var allowed = boss ? new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 } : NormalRarityPool(dungeonNumber);
        var weights = boss
            ? new[] { 35f, 30f, 18f, 10f, 5f, 1.5f, 0.9f, 0.55f, 0.35f, 0.2f }
            : NormalRarityWeights(allowed);
        var total = 0f;
        for (var i = 0; i < allowed.Length; i++)
        {
            if (allowed[i] <= max)
            {
                total += weights[i];
            }
        }

        var roll = UnityEngine.Random.value * Mathf.Max(total, 1f);
        for (var i = 0; i < allowed.Length; i++)
        {
            if (allowed[i] > max)
            {
                continue;
            }

            roll -= weights[i];
            if (roll <= 0f)
            {
                return allowed[i];
            }
        }
        return Mathf.Min(max, allowed[0]);
    }

    private int[] NormalRarityPool(int dungeonNumber)
    {
        if (dungeonNumber <= 5) return new[] { 0, 1, 2, 3 };
        if (dungeonNumber <= 10) return new[] { 2, 3, 4 };
        if (dungeonNumber <= 15) return new[] { 3, 4, 5 };
        if (dungeonNumber <= 20) return new[] { 4, 5, 6 };
        if (dungeonNumber <= 25) return new[] { 5, 6, 7 };
        if (dungeonNumber <= 30) return new[] { 6, 7, 8 };
        if (dungeonNumber <= 35) return new[] { 7, 8, 9 };
        if (dungeonNumber <= 40) return new[] { 8, 9, 10 };
        return new[] { 9, 10, 11 };
    }

    private float[] NormalRarityWeights(int[] pool)
    {
        var weights = new float[pool.Length];
        for (var i = 0; i < pool.Length; i++)
        {
            weights[i] = NormalRarityWeight(pool[i]);
        }
        return weights;
    }

    private float NormalRarityWeight(int rarity)
    {
        if (TryGetRarityData(rarity, out var rarityData) && rarityData.dropWeight > 0f)
        {
            return rarityData.dropWeight;
        }

        var weights = new[] { 45f, 25f, 15f, 8f, 4f, 2f, 0.7f, 0.25f, 0.12f, 0.06f, 0.03f, 0.012f };
        return weights[Mathf.Clamp(rarity, 0, weights.Length - 1)];
    }

    private int MaxRarityForDungeon(int dungeonNumber)
    {
        if (dungeonNumber <= 5) return 4;
        if (dungeonNumber <= 10) return 5;
        if (dungeonNumber <= 15) return 6;
        if (dungeonNumber <= 20) return 7;
        if (dungeonNumber <= 25) return 8;
        if (dungeonNumber <= 30) return 9;
        if (dungeonNumber <= 35) return 10;
        return 11;
    }

    private float DungeonGearMultiplier(int dungeonNumber)
    {
        if (dungeonNumber <= 5) return 1f;
        if (dungeonNumber <= 10) return 1.15f;
        if (dungeonNumber <= 15) return 1.35f;
        if (dungeonNumber <= 20) return 1.6f;
        if (dungeonNumber <= 25) return 1.9f;
        if (dungeonNumber <= 30) return 2.25f;
        if (dungeonNumber <= 35) return 2.65f;
        if (dungeonNumber <= 40) return 3.1f;
        return 3.6f;
    }

    private float RarityMultiplier(int rarity)
    {
        if (TryGetRarityData(rarity, out var rarityData) && rarityData.statMultiplier > 0f)
        {
            return rarityData.statMultiplier;
        }

        var mults = new[] { 1f, 1.12f, 1.25f, 1.6f, 2.1f, 2.75f, 3.5f, 4.4f, 5.2f, 6.4f, 7.8f, 9.2f, 10.5f };
        return mults[Mathf.Clamp(rarity, 0, mults.Length - 1)];
    }

    private string PickItemPrefix(string type, int rarity)
    {
        var weapon = new[] { "연습용", "강화된", "철제", "공허의", "성스러운", "학살자의", "심연의" };
        var armor = new[] { "낡은", "가죽", "철제", "사슬", "미스릴", "아다만티움", "공허 가죽" };
        var charm = new[] { "낡은", "빛나는", "룬이 새겨진", "별빛의", "공허의", "축복받은", "심연의" };
        var table = type == "Weapon" ? weapon : (type == "Armor" ? armor : charm);
        return table[UnityEngine.Random.Range(0, table.Length)];
    }

    private string PickItemSuffix(string type, string heroClass)
    {
        var classId = GearClassId(heroClass);
        if (type == "Armor")
        {
            string[] armors;
            if (classId == "mage") armors = new[] { "천 옷", "마법사 로브", "룬 로브", "현자의 예복" };
            else if (classId == "rogue") armors = new[] { "가죽 조끼", "그림자 슈트", "어둠의 망토", "밤의 예복" };
            else if (classId == "priest") armors = new[] { "사제 예복", "빛의 로브", "성직자 망토", "축복의 성의" };
            else if (classId == "bomber") armors = new[] { "화염 로브", "폭발 방호복", "용암 망토", "기폭술사 코트" };
            else if (classId == "spirit") armors = new[] { "정령 예복", "사원소 망토", "바람결 로브", "대지 의복" };
            else if (classId == "archer") armors = new[] { "사냥꾼 조끼", "궁수 튜닉", "바람 망토", "매의 갑옷" };
            else if (classId == "monk") armors = new[] { "수련복", "무투 도복", "금강 조끼", "선승의 법의" };
            else armors = new[] { "판금 갑옷", "기사 갑옷", "강철 중갑", "성기사의 흉갑" };
            return armors[UnityEngine.Random.Range(0, armors.Length)];
        }
        if (type == "Charm")
        {
            var charms = new[] { "마력 반지", "현자의 목걸이", "칼날 귀걸이", "수호 부적", "탐험가 문장", "흡혈 팔찌", "바람 허리띠", "저주 룬석", "전술 브로치", "비전 성배", "밤안개 망토 장식", "차원 시계" };
            return charms[UnityEngine.Random.Range(0, charms.Length)];
        }

        string[] weapons;
        if (classId == "mage") weapons = new[] { "나뭇가지", "지팡이", "원소 스태프", "룬 완드", "마도서" };
        else if (classId == "rogue") weapons = new[] { "녹슨 단검", "쌍단검", "비수", "그림자 펜촉", "암살검" };
        else if (classId == "priest") weapons = new[] { "성서", "빛 지팡이", "축복의 홀", "태양 성물", "성광 완드" };
        else if (classId == "bomber") weapons = new[] { "폭발 촉매", "화약 지팡이", "기폭 완드", "용암 마도서", "섬광 구체" };
        else if (classId == "spirit") weapons = new[] { "정령 구슬", "사원소 토템", "바람 부적", "대지 지팡이", "원초의 가지" };
        else if (classId == "archer") weapons = new[] { "단궁", "장궁", "합성궁", "바람 활", "관통 쇠뇌" };
        else if (classId == "monk") weapons = new[] { "수련 장갑", "철권갑", "염주 너클", "용문 권갑", "금강 장갑" };
        else weapons = new[] { "단검", "검", "기사검", "대검", "전투도끼" };
        return weapons[UnityEngine.Random.Range(0, weapons.Length)];
    }

    private List<SkillState> ScaledSkillsForPlayer()
    {
        EnsurePlayerData();
        var scaled = new List<SkillState>();
        foreach (var skill in SkillsForClass(player.heroClass))
        {
            scaled.Add(ScaleSkill(skill, SkillLevelAfterEnsure(skill.name)));
        }
        return scaled;
    }

    private SkillState ScaleSkill(SkillState baseSkill, int level)
    {
        var bonus = Mathf.Max(0, level - 1);
        var multiplier = RoundToGameHundredth(baseSkill.multiplier * (1f + bonus * 0.12f));
        return new SkillState(baseSkill.name, ScaledSkillMpCost(baseSkill, bonus), multiplier, baseSkill.magic)
        {
            stunChance = ScaleChance(baseSkill, "stun", baseSkill.stunChance, bonus),
            burnChance = ScaleChance(baseSkill, "burn", baseSkill.burnChance, bonus),
            poisonChance = ScaleChance(baseSkill, "poison", baseSkill.poisonChance, bonus),
            bleedChance = ScaleChance(baseSkill, "bleed", baseSkill.bleedChance, bonus),
            shockChance = ScaleChance(baseSkill, "shock", baseSkill.shockChance, bonus),
            freezeChance = ScaleChance(baseSkill, "freeze", baseSkill.freezeChance, bonus),
            blindChance = ScaleChance(baseSkill, "blind", baseSkill.blindChance, bonus),
            weakenChance = ScaleChance(baseSkill, "weaken", baseSkill.weakenChance, bonus),
            vulnerableChance = ScaleChance(baseSkill, "vulnerable", baseSkill.vulnerableChance, bonus),
            silenceChance = ScaleChance(baseSkill, "silence", baseSkill.silenceChance, bonus),
            manaBurnChance = ScaleChance(baseSkill, "mana_burn", baseSkill.manaBurnChance, bonus),
            selfStatusType = baseSkill.selfStatusType,
            selfStatusDuration = baseSkill.selfStatusDuration,
            selfStatusValue = ScaleSelfStatusValue(baseSkill.selfStatusValue, bonus),
            critBonus = ScaleRatioEffect(baseSkill.critBonus, bonus, 0.75f),
            lifeStealRatio = ScaleRatioEffect(baseSkill.lifeStealRatio, bonus, 0.6f),
            forceCrit = baseSkill.forceCrit,
            noDamage = baseSkill.noDamage,
            grantsShield = baseSkill.grantsShield,
            healsSelf = baseSkill.healsSelf
        };
    }

    private int ScaledSkillMpCost(SkillState baseSkill, int bonusLevels)
    {
        if (baseSkill == null || baseSkill.mpCost <= 0)
        {
            return 0;
        }

        bonusLevels = Mathf.Max(0, bonusLevels);
        var flatGrowth = bonusLevels * (baseSkill.magic ? MagicSkillMpCostPerLevel : PhysicalSkillMpCostPerLevel);
        var baseGrowth = baseSkill.mpCost * bonusLevels * (baseSkill.magic ? MagicSkillMpCostBaseGrowth : PhysicalSkillMpCostBaseGrowth);
        var accessoryGrowth = AccessoryMaxMpSkillCost(baseSkill, bonusLevels);
        return Mathf.Max(baseSkill.mpCost, RoundToGameInt(baseSkill.mpCost + flatGrowth + baseGrowth + accessoryGrowth));
    }

    private float AccessoryMaxMpSkillCost(SkillState baseSkill, int bonusLevels)
    {
        if (baseSkill == null || player == null)
        {
            return 0f;
        }

        var accessoryMaxMp = EquippedAccessoryMaxMp(player);
        if (accessoryMaxMp <= 0)
        {
            return 0f;
        }

        var rate = baseSkill.magic ? MagicAccessoryMaxMpSkillCostRate : PhysicalAccessoryMaxMpSkillCostRate;
        var levelFactor = 1f + Mathf.Max(0, bonusLevels) * AccessoryMaxMpSkillCostLevelGrowth;
        return accessoryMaxMp * rate * levelFactor;
    }

    private int EquippedAccessoryMaxMp(PlayerState state)
    {
        if (state == null)
        {
            return 0;
        }

        return AccessoryMaxMp(state.charm)
            + AccessoryMaxMp(state.charm2)
            + AccessoryMaxMp(state.charm3)
            + AccessoryMaxMp(state.charm4);
    }

    private int AccessoryMaxMp(ItemState item)
    {
        return item != null && item.type == "Charm" ? Mathf.Max(0, item.maxMp) : 0;
    }

    private float ScaleSelfStatusValue(float value, int bonusLevels)
    {
        if (value == 0f)
        {
            return value;
        }

        var sign = Mathf.Sign(value);
        return sign * RoundToGameHundredth(Mathf.Abs(value) * (1f + bonusLevels * 0.06f));
    }

    private float ScaleRatioEffect(float value, int bonusLevels, float cap)
    {
        if (value <= 0f)
        {
            return value;
        }

        return Mathf.Min(cap, RoundToGameHundredth(value * (1f + bonusLevels * 0.04f)));
    }

    private float ScaleChance(SkillState skill, string chanceType, float chance, int bonusLevels)
    {
        if (chance <= 0f)
        {
            return 0f;
        }

        var cap = SkillChanceCap(skill.name, chanceType);
        return Mathf.Min(cap, RoundToGameHundredth(chance + bonusLevels * 0.08f));
    }

    private float SkillChanceCap(string skillName, string chanceType)
    {
        if ((skillName == "방패 후려치기" || skillName == "성광 분쇄") && chanceType == "stun") return 1f;
        if (skillName == "화염구" && chanceType == "burn") return 0.8f;
        if (skillName == "번개 폭풍" && chanceType == "stun") return 0.7f;
        if ((skillName == "독 묻은 단검" || skillName == "황혼 난무") && chanceType == "poison") return 1f;
        if (skillName == "찬란한 징벌" && chanceType == "stun") return 0.75f;
        if (skillName == "구원의 광선" && chanceType == "vulnerable") return 0.35f;
        if (skillName == "폭염 불꽃" && chanceType == "burn") return 0.7f;
        if (skillName == "파쇄 폭탄" && chanceType == "vulnerable") return 0.55f;
        if (skillName == "연쇄 기폭" && chanceType == "shock") return 0.65f;
        if (skillName == "연쇄 기폭" && chanceType == "stun") return 0.25f;
        if (skillName == "화염 정령" && chanceType == "burn") return 0.75f;
        if (skillName == "질풍 정령" && chanceType == "blind") return 0.75f;
        if (skillName == "대지 정령" && (chanceType == "weaken" || chanceType == "vulnerable")) return 0.6f;
        if (skillName == "속박 화살" && chanceType == "stun") return 0.7f;
        if (skillName == "관통 연사" && chanceType == "bleed") return 0.65f;
        if (skillName == "관통 연사" && chanceType == "vulnerable") return 0.35f;
        if (skillName == "급소 봉쇄" && chanceType == "stun") return 0.7f;
        return 1f;
    }

    private int SkillLevel(string skillName)
    {
        EnsurePlayerData();
        return SkillLevelAfterEnsure(skillName);
    }

    private int SkillLevelAfterEnsure(string skillName)
    {
        if (player == null || player.skillLevels == null)
        {
            return 1;
        }

        var entry = player.skillLevels.Find(level => level.skillName == skillName);
        return entry != null ? Mathf.Clamp(entry.level, 1, MaxSkillLevel) : 1;
    }

    private int SkillUpgradeCost(int skillLevel)
    {
        return RoundToGameInt(80f * Mathf.Pow(1.55f, Mathf.Max(1, skillLevel) - 1));
    }

    private void UpgradeSkill(string skillName)
    {
        EnsurePlayerData();
        var level = SkillLevelAfterEnsure(skillName);
        var cost = SkillUpgradeCost(level);
        if (level >= MaxSkillLevel)
        {
            ShowSkillTraining(skillName + "은(는) 이미 최대 레벨입니다.");
            return;
        }

        if (player.gold < cost)
        {
            ShowSkillTraining("골드가 부족합니다.");
            return;
        }

        player.gold -= cost;
        var entry = player.skillLevels.Find(item => item.skillName == skillName);
        if (entry == null)
        {
            entry = new SkillLevelState { skillName = skillName, level = 1 };
            player.skillLevels.Add(entry);
        }
        entry.level = Mathf.Min(MaxSkillLevel, entry.level + 1);
        SaveGame();
        ShowSkillTraining(skillName + " Lv." + entry.level + " 강화 완료.");
    }

    private string SkillEffectLine(SkillState skill)
    {
        var parts = new List<string>();
        AddChanceLine(parts, "기절", skill.stunChance);
        AddChanceLine(parts, "화상", skill.burnChance);
        AddChanceLine(parts, "중독", skill.poisonChance);
        AddChanceLine(parts, "출혈", skill.bleedChance);
        AddChanceLine(parts, "감전", skill.shockChance);
        AddChanceLine(parts, "빙결", skill.freezeChance);
        AddChanceLine(parts, "실명", skill.blindChance);
        AddChanceLine(parts, "약화", skill.weakenChance);
        AddChanceLine(parts, "취약", skill.vulnerableChance);
        AddChanceLine(parts, "침묵", skill.silenceChance);
        AddChanceLine(parts, "마나 연소", skill.manaBurnChance);
        if (skill.grantsShield || skill.selfStatusType == "shield")
        {
            parts.Add("자신 보호막");
        }
        if (skill.healsSelf)
        {
            parts.Add("자신 회복");
        }
        if (!string.IsNullOrEmpty(skill.selfStatusType) && skill.selfStatusType != "shield")
        {
            parts.Add("자신 " + StatusName(skill.selfStatusType));
        }
        if (skill.critBonus > 0f) parts.Add("치명 +" + RoundToGameInt(skill.critBonus * 100f) + "%");
        if (skill.forceCrit) parts.Add("확정 치명");
        if (skill.lifeStealRatio > 0f) parts.Add("흡혈 " + RoundToGameInt(skill.lifeStealRatio * 100f) + "%");
        return parts.Count == 0 ? "추가 효과 없음" : string.Join(" / ", parts.ToArray());
    }

    private string SkillTrainingEffectLine(SkillState current, SkillState next)
    {
        var lines = new List<string>();
        var chances = new List<string>();
        AddChanceCompareLine(chances, "기절", current.stunChance, next.stunChance);
        AddChanceCompareLine(chances, "화상", current.burnChance, next.burnChance);
        AddChanceCompareLine(chances, "중독", current.poisonChance, next.poisonChance);
        AddChanceCompareLine(chances, "출혈", current.bleedChance, next.bleedChance);
        AddChanceCompareLine(chances, "감전", current.shockChance, next.shockChance);
        AddChanceCompareLine(chances, "빙결", current.freezeChance, next.freezeChance);
        AddChanceCompareLine(chances, "실명", current.blindChance, next.blindChance);
        AddChanceCompareLine(chances, "약화", current.weakenChance, next.weakenChance);
        AddChanceCompareLine(chances, "취약", current.vulnerableChance, next.vulnerableChance);
        AddChanceCompareLine(chances, "침묵", current.silenceChance, next.silenceChance);
        AddChanceCompareLine(chances, "마나 연소", current.manaBurnChance, next.manaBurnChance);

        if (chances.Count > 0)
        {
            lines.Add("상태 확률: " + string.Join(" / ", chances.ToArray()));
        }

        var effects = new List<string>();
        if (current.grantsShield || next.grantsShield || current.selfStatusType == "shield" || next.selfStatusType == "shield")
        {
            AddRatioCompareLine(effects, "보호막", current.multiplier, next.multiplier);
        }
        if (current.healsSelf || next.healsSelf)
        {
            AddRatioCompareLine(effects, "회복", current.multiplier, next.multiplier);
        }
        AddChanceCompareLine(effects, "치명", current.critBonus, next.critBonus);
        AddChanceCompareLine(effects, "흡혈", current.lifeStealRatio, next.lifeStealRatio);
        if (current.forceCrit || next.forceCrit)
        {
            effects.Add("확정 치명 유지");
        }
        if (!string.IsNullOrEmpty(current.selfStatusType) && current.selfStatusType != "shield")
        {
            effects.Add("자신 " + StatusName(current.selfStatusType) + SelfStatusCompareText(current, next));
        }
        else if (!string.IsNullOrEmpty(next.selfStatusType) && next.selfStatusType != "shield")
        {
            effects.Add("자신 " + StatusName(next.selfStatusType) + SelfStatusCompareText(current, next));
        }

        if (effects.Count > 0)
        {
            lines.Add("핵심 효과: " + string.Join(" / ", effects.ToArray()));
        }

        lines.Add("소모 MP: " + current.mpCost + " → " + next.mpCost);
        return lines.Count == 1 ? "효과: " + SkillEffectLine(current) + "\n다음: " + SkillEffectLine(next) : string.Join("\n", lines.ToArray());
    }

    private string SkillCombatButtonLabel(string key, SkillState skill)
    {
        var effect = SkillCombatEffectSummary(skill);
        return key + skill.name + "  MP " + skill.mpCost + "\n" + effect;
    }

    private string SkillCombatEffectSummary(SkillState skill)
    {
        var parts = new List<string>();
        AddLimitedChanceLine(parts, "기절", skill.stunChance, 2);
        AddLimitedChanceLine(parts, "화상", skill.burnChance, 2);
        AddLimitedChanceLine(parts, "중독", skill.poisonChance, 2);
        AddLimitedChanceLine(parts, "출혈", skill.bleedChance, 2);
        AddLimitedChanceLine(parts, "감전", skill.shockChance, 2);
        AddLimitedChanceLine(parts, "빙결", skill.freezeChance, 2);
        AddLimitedChanceLine(parts, "실명", skill.blindChance, 2);
        AddLimitedChanceLine(parts, "약화", skill.weakenChance, 2);
        AddLimitedChanceLine(parts, "취약", skill.vulnerableChance, 2);
        AddLimitedChanceLine(parts, "침묵", skill.silenceChance, 2);
        AddLimitedChanceLine(parts, "마나연소", skill.manaBurnChance, 2);

        if (parts.Count > 0)
        {
            return string.Join(" / ", parts.ToArray());
        }
        if (skill.grantsShield || skill.selfStatusType == "shield")
        {
            return "자신 보호막";
        }
        if (!string.IsNullOrEmpty(skill.selfStatusType))
        {
            return "자신 " + StatusName(skill.selfStatusType);
        }
        if (skill.forceCrit)
        {
            return "확정 치명";
        }
        if (skill.critBonus > 0f)
        {
            return "치명 +" + RoundToGameInt(skill.critBonus * 100f) + "%";
        }
        if (skill.lifeStealRatio > 0f)
        {
            return "흡혈 " + RoundToGameInt(skill.lifeStealRatio * 100f) + "%";
        }

        return "위력 " + RoundToGameInt(skill.multiplier * 100f) + "%";
    }

    private void AddLimitedChanceLine(List<string> parts, string label, float chance, int maxCount)
    {
        if (chance > 0f && parts.Count < maxCount)
        {
            parts.Add(label + " " + RoundToGameInt(chance * 100f) + "%");
        }
    }

    private void AddChanceLine(List<string> parts, string label, float chance)
    {
        if (chance > 0f)
        {
            parts.Add(label + " " + RoundToGameInt(chance * 100f) + "%");
        }
    }

    private void AddChanceCompareLine(List<string> parts, string label, float current, float next)
    {
        if (current > 0f || next > 0f)
        {
            parts.Add(label + " " + RoundToGameInt(current * 100f) + "% → " + RoundToGameInt(next * 100f) + "%");
        }
    }

    private void AddRatioCompareLine(List<string> parts, string label, float current, float next)
    {
        parts.Add(label + " " + RoundToGameInt(current * 100f) + "% → " + RoundToGameInt(next * 100f) + "%");
    }

    private string SelfStatusCompareText(SkillState current, SkillState next)
    {
        if (current == null || next == null || current.selfStatusValue == 0f && next.selfStatusValue == 0f)
        {
            return "";
        }

        return " " + RoundToGameInt(Mathf.Abs(current.selfStatusValue) * 100f) + "% → " + RoundToGameInt(Mathf.Abs(next.selfStatusValue) * 100f) + "%";
    }

    private string HeroDescriptionForClass(string heroClass, string fallback)
    {
        if (heroClass == "성기사")
        {
            return "높은 방어력과 생명력으로 전선의 앞을 지키는 든든한 강철 방패입니다. 물리 기술을 사용해 아군을 보호하고 적을 분쇄합니다.";
        }
        if (heroClass == "원소술사")
        {
            return "원소의 신비한 힘을 부려 적에게 파괴적인 마법을 선사합니다. 낮은 생명력을 가졌으나 최강의 화력을 발휘합니다.";
        }
        if (heroClass == "그림자 자객" || heroClass == "그림자 궁수" || heroClass == "Ranger")
        {
            return "어둠 속에 몸을 숨긴 채 치명적인 급소를 노립니다. 극도로 민첩하며, 빠르고 날카로운 일격을 연달아 날립니다.";
        }
        if (heroClass == "빛의 사제")
        {
            return "빛의 권능으로 회복과 공격을 동시에 수행하는 힐러형 딜러입니다. 실명과 재생으로 긴 전투에 강합니다.";
        }
        if (heroClass == "폭렬술사")
        {
            return "폭발 마법으로 짧은 시간에 큰 피해를 쏟아붓는 마법 딜러입니다. 화상, 취약, 충격 효과를 활용합니다.";
        }
        if (heroClass == "정령술사")
        {
            return "기본 공격과 스킬을 함께 쓰는 사원소 전문 캐릭터입니다. 불, 물, 바람, 땅 정령을 번갈아 운용합니다.";
        }
        if (heroClass == "바람 궁수")
        {
            return "원거리에서 치명타와 상태이상 화살을 쏘는 민첩한 딜러입니다. 출혈, 기절, 회피 운용에 능합니다.";
        }
        if (heroClass == "무투가")
        {
            return "수도승처럼 단련된 육체와 빠른 연격으로 적을 제압하는 물리 치명타 중심 캐릭터입니다.";
        }

        return string.IsNullOrEmpty(fallback) ? "직업마다 능력치와 전투 기술이 다릅니다." : fallback;
    }

    private Color ClassAccentColor(string heroClass)
    {
        var theme = CharacterThemeForKey(heroClass);
        return theme == null ? Rgb(148, 163, 184) : theme.accent;
    }

    private List<HeroClass> HeroClasses()
    {
        if (cachedHeroClasses != null)
        {
            return cachedHeroClasses;
        }

        var defaultClasses = DefaultHeroClasses();
        var databaseClasses = HeroClassesFromDatabase();
        if (databaseClasses.Count > 0)
        {
            cachedHeroClasses = MergeHeroClasses(defaultClasses, databaseClasses);
            return cachedHeroClasses;
        }

        cachedHeroClasses = defaultClasses;
        return cachedHeroClasses;
    }

    private List<HeroClass> MergeHeroClasses(List<HeroClass> defaults, List<HeroClass> overrides)
    {
        var result = new List<HeroClass>(defaults ?? new List<HeroClass>());
        if (overrides == null)
        {
            return result;
        }

        foreach (var heroClass in overrides)
        {
            if (heroClass == null || string.IsNullOrEmpty(heroClass.name))
            {
                continue;
            }

            var normalizedName = NormalizeHeroClassName(heroClass.name);
            var index = result.FindIndex(entry => entry != null && NormalizeHeroClassName(entry.name) == normalizedName);
            if (index >= 0)
            {
                result[index] = MergeHeroClass(result[index], heroClass);
            }
            else
            {
                result.Add(heroClass);
            }
        }

        return result;
    }

    private HeroClass MergeHeroClass(HeroClass fallback, HeroClass heroClass)
    {
        if (fallback == null)
        {
            return heroClass;
        }

        if (heroClass == null)
        {
            return fallback;
        }

        return new HeroClass(
            string.IsNullOrEmpty(heroClass.name) ? fallback.name : NormalizeHeroClassName(heroClass.name),
            string.IsNullOrEmpty(heroClass.description) ? fallback.description : heroClass.description,
            string.IsNullOrEmpty(heroClass.portraitName) ? fallback.portraitName : heroClass.portraitName,
            heroClass.hp > 1 ? heroClass.hp : fallback.hp,
            heroClass.mp > 0 ? heroClass.mp : fallback.mp,
            heroClass.attack > 0 ? heroClass.attack : fallback.attack,
            heroClass.magic > 0 ? heroClass.magic : fallback.magic,
            heroClass.defense > 0 ? heroClass.defense : fallback.defense,
            heroClass.crit > 0f ? heroClass.crit : fallback.crit,
            heroClass.basicAttackUsesMagic || fallback.basicAttackUsesMagic,
            MergeSkills(fallback.skills, heroClass.skills));
    }

    private List<HeroClass> DefaultHeroClasses()
    {
        return new List<HeroClass>
        {
            new HeroClass("성기사", HeroDescriptionForClass("성기사", ""), "knight", 160, 50, 18, 5, 12, 0.05f),
            new HeroClass("원소술사", HeroDescriptionForClass("원소술사", ""), "mage", 90, 150, 6, 24, 4, 0.08f, true),
            new HeroClass("그림자 자객", HeroDescriptionForClass("그림자 자객", ""), "rogue", 110, 70, 15, 8, 6, 0.20f),
            new HeroClass("빛의 사제", HeroDescriptionForClass("빛의 사제", ""), "priest", 120, 130, 7, 21, 7, 0.07f, true),
            new HeroClass("폭렬술사", HeroDescriptionForClass("폭렬술사", ""), "bomber", 95, 135, 5, 27, 4, 0.10f, true),
            new HeroClass("정령술사", HeroDescriptionForClass("정령술사", ""), "spirit", 105, 170, 1, 22, 5, 0.06f, true),
            new HeroClass("바람 궁수", HeroDescriptionForClass("바람 궁수", ""), "archer", 115, 85, 17, 6, 6, 0.16f),
            new HeroClass("무투가", HeroDescriptionForClass("무투가", ""), "monk", 130, 75, 19, 4, 8, 0.18f)
        };
    }

    private List<SkillState> SkillsForClass(string heroClass)
    {
        var normalizedHeroClass = NormalizeHeroClassName(heroClass);
        if (skillCacheByClass.TryGetValue(normalizedHeroClass, out var cachedSkills))
        {
            return cachedSkills;
        }

        var defaultSkills = DefaultSkillsForClass(normalizedHeroClass);
        var databaseSkills = SkillsForClassFromDatabase(normalizedHeroClass);
        if (databaseSkills.Count > 0)
        {
            cachedSkills = MergeSkills(defaultSkills, databaseSkills);
            skillCacheByClass[normalizedHeroClass] = cachedSkills;
            return cachedSkills;
        }

        skillCacheByClass[normalizedHeroClass] = defaultSkills;
        return defaultSkills;
    }

    private List<SkillState> MergeSkills(List<SkillState> defaults, List<SkillState> overrides)
    {
        var result = new List<SkillState>(defaults ?? new List<SkillState>());
        if (overrides == null)
        {
            return result;
        }

        foreach (var skill in overrides)
        {
            if (skill == null || string.IsNullOrEmpty(skill.name))
            {
                continue;
            }

            var index = result.FindIndex(entry => entry != null && entry.name == skill.name);
            if (index >= 0)
            {
                result[index] = skill;
            }
            else
            {
                result.Add(skill);
            }
        }

        return result;
    }

    private List<SkillState> DefaultSkillsForClass(string heroClass)
    {
        if (heroClass == "원소술사" || heroClass == "Elementalist")
        {
            return new List<SkillState>
            {
                new SkillState("화염구", 25, 1.8f, true) { burnChance = 0.64f },
                new SkillState("냉기 장벽", 20, 0.55f, false) { noDamage = true, grantsShield = true },
                new SkillState("번개 폭풍", 35, 1.95f, true) { stunChance = 0.35f, critBonus = 0.4f }
            };
        }
        if (heroClass == "그림자 자객" || heroClass == "그림자 궁수" || heroClass == "Ranger")
        {
            return new List<SkillState>
            {
                new SkillState("그림자 습격", 20, 1.82f, false) { critBonus = 0.4f, lifeStealRatio = 0.25f },
                new SkillState("독 묻은 단검", 25, 1.17f, false) { poisonChance = 1.0f },
                new SkillState("황혼 난무", 40, 2.02f, false) { poisonChance = 1.0f, critBonus = 0.4f, forceCrit = true }
            };
        }
        if (heroClass == "빛의 사제" || heroClass == "Cleric")
        {
            return new List<SkillState>
            {
                new SkillState("찬란한 징벌", 22, 1.74f, true) { stunChance = 0.5f },
                new SkillState("성역", 28, 0.6f, false) { noDamage = true, grantsShield = true, selfStatusType = "damage_boost", selfStatusDuration = 2, selfStatusValue = 0.2f },
                new SkillState("구원의 광선", 36, 1.75f, true) { vulnerableChance = 0.19f, lifeStealRatio = 0.2f }
            };
        }
        if (heroClass == "폭렬술사")
        {
            return new List<SkillState>
            {
                new SkillState("폭염 불꽃", 26, 1.85f, true) { burnChance = 0.54f },
                new SkillState("파쇄 폭탄", 34, 2.05f, true) { vulnerableChance = 0.39f },
                new SkillState("연쇄 기폭", 48, 2.35f, true) { shockChance = 0.49f, stunChance = 0.09f, critBonus = 0.4f }
            };
        }
        if (heroClass == "정령술사")
        {
            return new List<SkillState>
            {
                new SkillState("화염 정령", 30, 1.85f, true) { burnChance = 0.59f },
                new SkillState("물결 정령", 30, 1.85f, true) { selfStatusType = "shield", selfStatusValue = 0.25f },
                new SkillState("질풍 정령", 30, 1.85f, true) { blindChance = 0.5f, critBonus = 0.4f },
                new SkillState("대지 정령", 30, 1.85f, true) { weakenChance = 0.44f, vulnerableChance = 0.44f }
            };
        }
        if (heroClass == "바람 궁수")
        {
            return new List<SkillState>
            {
                new SkillState("정조준 사격", 18, 1.65f, false) { critBonus = 0.4f },
                new SkillState("속박 화살", 24, 1.35f, false) { stunChance = 0.54f },
                new SkillState("관통 연사", 34, 1.95f, false) { bleedChance = 0.49f, vulnerableChance = 0.19f },
                new SkillState("바람걸음 사격", 30, 1.55f, false) { critBonus = 0.4f, selfStatusType = "evasion_boost", selfStatusDuration = 2, selfStatusValue = 0.18f }
            };
        }
        if (heroClass == "무투가")
        {
            return new List<SkillState>
            {
                new SkillState("철권 연타", 20, 1.65f, false) { critBonus = 0.4f },
                new SkillState("급소 봉쇄", 28, 1.55f, false) { stunChance = 0.45f },
                new SkillState("내공 폭발", 36, 2.05f, false) { critBonus = 0.4f, selfStatusType = "damage_boost", selfStatusDuration = 2, selfStatusValue = 0.18f }
            };
        }
        if (string.IsNullOrEmpty(heroClass) || heroClass == "성기사" || heroClass == "Paladin")
        {
            return new List<SkillState>
            {
                new SkillState("방패 후려치기", 15, 1.3f, false) { stunChance = 0.5f },
                new SkillState("철벽 방어", 20, 0.6f, false) { noDamage = true, grantsShield = true },
                new SkillState("성광 분쇄", 25, 1.6f, false) { stunChance = 0.5f }
            };
        }

        return new List<SkillState>();
    }

    private int MaxHp()
    {
        return MaxHp(player);
    }

    private int MaxHp(PlayerState state)
    {
        if (state == null)
        {
            return 1;
        }

        return Mathf.Max(1, state.baseHp + LevelGrowth(state, "maxHp") + ItemStatInt(state, "maxHp"));
    }

    private int MaxMp()
    {
        return MaxMp(player);
    }

    private int MaxMp(PlayerState state)
    {
        if (state == null)
        {
            return 0;
        }

        return Mathf.Max(0, state.baseMp + LevelGrowth(state, "maxMp") + ItemStatInt(state, "maxMp"));
    }

    private int Attack()
    {
        return player == null ? 0 : Mathf.Max(0, player.baseAttack + LevelGrowth(player, "attack") + ItemStatInt(player, "attack"));
    }

    private int Magic()
    {
        return player == null ? 0 : Mathf.Max(0, player.baseMagic + LevelGrowth(player, "magic") + ItemStatInt(player, "magic"));
    }

    private int Defense()
    {
        return player == null ? 0 : Mathf.Max(0, player.baseDefense + LevelGrowth(player, "defense") + ItemStatInt(player, "defense"));
    }

    private int Speed()
    {
        if (player == null)
        {
            return 1;
        }

        return Mathf.Max(1, RoundToGameInt(BaseSpeedForClass(player.heroClass) + Mathf.Max(0, player.level - 1) * SpeedGrowthForClass(player.heroClass)) + ItemStatInt(player, "speed"));
    }

    private float CritChance()
    {
        if (player == null)
        {
            return 0f;
        }

        return Mathf.Clamp(player.baseCrit + LevelGrowthFloat(player, "critRate") + ItemStatFloat(player, "critRate"), 0.04f, 0.95f);
    }

    private float DamageReduction()
    {
        return Mathf.Clamp(ItemStatFloat(player, "damageReduction"), 0f, 0.5f);
    }

    private float CritDamage()
    {
        return Mathf.Max(0f, ItemStatFloat(player, "critDamage"));
    }

    private float Evasion()
    {
        return Mathf.Clamp(ItemStatFloat(player, "evasion"), 0f, 0.8f);
    }

    private float EffectiveEvasion()
    {
        return Mathf.Clamp(Evasion() + StatusValue(playerStatusEffects, "evasion_boost", 0f), 0f, 0.8f);
    }

    private float LifeSteal()
    {
        return Mathf.Clamp(ItemStatFloat(player, "lifeSteal"), 0f, 0.35f);
    }

    private float ManaRegen()
    {
        return BaseTurnManaRegen + Mathf.Clamp(ItemStatFloat(player, "manaRegen"), 0f, ManaRegenCap);
    }

    private int PlayerTurnManaRegenAmount()
    {
        return Mathf.Max(0, RoundToGameInt(MaxMp() * ManaRegen()));
    }

    private float StatusPower()
    {
        return Mathf.Clamp(ItemStatFloat(player, "statusPower"), 0f, 0.75f);
    }

    private float ItemFind()
    {
        return Mathf.Clamp(ItemStatFloat(player, "itemFind"), 0f, 0.5f);
    }

    private int ItemStatInt(PlayerState state, string stat)
    {
        if (state == null)
        {
            return 0;
        }

        return ItemStatInt(state.weapon, stat)
            + ItemStatInt(state.armor, stat)
            + ItemStatInt(state.charm, stat)
            + ItemStatInt(state.charm2, stat)
            + ItemStatInt(state.charm3, stat)
            + ItemStatInt(state.charm4, stat);
    }

    private int ItemStatInt(ItemState item, string stat)
    {
        if (item == null)
        {
            return 0;
        }

        if (stat == "maxHp") return Mathf.Max(0, item.maxHp);
        if (stat == "maxMp") return Mathf.Max(0, item.maxMp);
        if (stat == "attack") return Mathf.Max(0, item.attack);
        if (stat == "magic") return Mathf.Max(0, item.magic);
        if (stat == "defense") return Mathf.Max(0, item.defense);
        if (stat == "speed") return Mathf.Max(0, item.speed);
        return 0;
    }

    private float ItemStatFloat(PlayerState state, string stat)
    {
        if (state == null)
        {
            return 0f;
        }

        return ItemStatFloat(state.weapon, stat)
            + ItemStatFloat(state.armor, stat)
            + ItemStatFloat(state.charm, stat)
            + ItemStatFloat(state.charm2, stat)
            + ItemStatFloat(state.charm3, stat)
            + ItemStatFloat(state.charm4, stat);
    }

    private float ItemStatFloat(ItemState item, string stat)
    {
        if (item == null)
        {
            return 0f;
        }

        if (stat == "critRate") return Mathf.Max(0f, item.critRate);
        if (stat == "critDamage") return Mathf.Max(0f, item.critDamage);
        if (stat == "evasion") return Mathf.Max(0f, item.evasion);
        if (stat == "damageReduction") return Mathf.Max(0f, item.damageReduction);
        if (stat == "lifeSteal") return Mathf.Max(0f, item.lifeSteal);
        if (stat == "manaRegen") return Mathf.Max(0f, item.manaRegen);
        if (stat == "statusPower") return Mathf.Max(0f, item.statusPower);
        if (stat == "itemFind") return Mathf.Max(0f, item.itemFind);
        return 0f;
    }

    private int EquippedPowerTotal(PlayerState state)
    {
        if (state == null)
        {
            return 0;
        }

        return ItemPower(state.weapon)
            + ItemPower(state.armor)
            + ItemPower(state.charm)
            + ItemPower(state.charm2)
            + ItemPower(state.charm3)
            + ItemPower(state.charm4);
    }

    private int ItemPower(ItemState item)
    {
        return item == null ? 0 : Mathf.Max(0, item.power);
    }

    private int XpToNext()
    {
        return RoundToGameInt(100f * Mathf.Pow(1.3f, Mathf.Max(0, player.level - 1)));
    }

    private int SellValue(ItemState item)
    {
        if (item == null)
        {
            return 0;
        }

        return RaritySellValues[Mathf.Clamp(item.rarity, 0, RaritySellValues.Length - 1)];
    }

    private int EnhancementCost(ItemState item)
    {
        if (item == null)
        {
            return 0;
        }

        return Mathf.Max(1, RoundToGameInt(25f * Mathf.Pow(1.6f, Mathf.Max(0, item.level)) * RarityCostMultiplier(item.rarity)));
    }

    private float RarityCostMultiplier(int rarity)
    {
        return RarityCostMultipliers[Mathf.Clamp(rarity, 0, RarityCostMultipliers.Length - 1)];
    }

    private string StatLine()
    {
        return "공격 " + Attack() + "  마력 " + Magic() + "  방어 " + Defense() + "  속도 " + Speed() + "  치명 " + RoundToGameInt(CritChance() * 100) + "%  치피 " + RoundToGameInt(CritDamage() * 100) + "%\n회피 " + RoundToGameInt(Evasion() * 100) + "%  피해감소 " + RoundToGameInt(DamageReduction() * 100) + "%  상태 피해 " + RoundToGameInt(StatusPower() * 100) + "%  발견 " + RoundToGameInt(ItemFind() * 100) + "%  경험치 " + player.xp + "/" + XpToNext();
    }

    private string BuildStatusText()
    {
        return "현재 목표: 추천 던전을 선택해 보스까지 돌파하세요.\n상태이상은 턴 시작에 적용되며, 기절과 빙결은 행동을 막습니다.";
    }

    private void AddGeneralLog(string message)
    {
        EnsurePlayerData();
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (player.generalLogs.Count > 0 && player.generalLogs[player.generalLogs.Count - 1] == message)
        {
            return;
        }

        player.generalLogs.Add(message);
        while (player.generalLogs.Count > MaxGeneralLogLines)
        {
            player.generalLogs.RemoveAt(0);
        }
    }

    private string RecentLogText()
    {
        EnsurePlayerData();
        if (player.generalLogs.Count == 0)
        {
            return "아직 기록된 소식이 없습니다.";
        }

        var lines = new List<string>();
        for (var i = player.generalLogs.Count - 1; i >= 0 && lines.Count < 4; i--)
        {
            lines.Add("- " + player.generalLogs[i]);
        }
        return string.Join("\n", lines.ToArray());
    }

    private string EnemyNameForStage(int stage)
    {
        var names = new[] { "공허의 위습", "텅 빈 경비병", "잿빛 약탈자", "수정 감시자", "균열 기사", "심연의 전령" };
        return names[Mathf.Clamp((stage - 1) / 2, 0, names.Length - 1)];
    }

    private string EquipmentLine(string label, ItemState item)
    {
        return label + ": " + (item == null ? "없음" : ItemLabel(item));
    }

    private string InventoryCountLabel()
    {
        EnsurePlayerData();
        return player.inventory.Count + "/" + MaxInventoryItems;
    }

    private bool CanAddInventoryItem()
    {
        EnsurePlayerData();
        return player.inventory.Count < MaxInventoryItems;
    }

    private string AddItemToInventoryOrConvert(ItemState item)
    {
        if (item == null)
        {
            return "";
        }

        if (CanAddInventoryItem())
        {
            player.inventory.Add(item);
            return " [" + RarityLabel(item.rarity) + "] " + item.name + "을(를) 발견했습니다.";
        }

        var value = SellValue(item);
        player.gold += value;
        return " 가방이 가득 차 [" + RarityLabel(item.rarity) + "] " + item.name + "을(를) " + value + "G로 전환했습니다.";
    }

    private void ClampSelectedInventoryIndex()
    {
        if (player == null || player.inventory == null || player.inventory.Count == 0)
        {
            selectedInventoryIndex = -1;
            return;
        }

        if (selectedInventoryIndex >= player.inventory.Count)
        {
            selectedInventoryIndex = player.inventory.Count - 1;
        }
    }

    private string SelectedItemDetailText(ItemState item)
    {
        if (item == null)
        {
            return "선택된 아이템이 없습니다.";
        }

        return ItemLabel(item)
            + "\n전투력 +" + item.power
            + "\n" + GearStatsDescription(item)
            + "\n" + GearStatDeltaText(item, EquippedItemForReplacement(item.type, 0))
            + GearOptionHelpText(item)
            + "\n판매가 " + SellValue(item) + "G";
    }

    private string ReplacementTargetDetailText(ItemState item)
    {
        if (item == null)
        {
            return "선택된 아이템이 없습니다.";
        }

        if (item.type == "Charm")
        {
            var lines = new List<string>();
            lines.Add("장신구 슬롯별 현재 착용");
            lines.Add(AccessorySlotDetailLine(1, player.charm, item));
            lines.Add(AccessorySlotDetailLine(2, player.charm2, item));
            lines.Add(AccessorySlotDetailLine(3, player.charm3, item));
            lines.Add(AccessorySlotDetailLine(4, player.charm4, item));
            return string.Join("\n", lines.ToArray());
        }

        var equipped = EquippedItemForReplacement(item.type, 0);
        return TypeLabel(item.type) + " 슬롯"
            + "\n" + GearPowerComparisonLine(item, equipped)
            + "\n" + EquippedItemDetailText(equipped);
    }

    private string AccessorySlotDetailLine(int slot, ItemState equipped, ItemState replacement)
    {
        return slot + "번 " + GearPowerDeltaText(replacement, equipped) + ": " + CompactItemDetailText(equipped)
            + " / " + GearStatDeltaText(replacement, equipped);
    }

    private string EquippedItemDetailText(ItemState item)
    {
        if (item == null)
        {
            return "현재 착용: 비어 있음\n능력치 없음";
        }

        return "현재 착용: " + ItemLabel(item)
            + "\n전투력 +" + item.power
            + "\n" + GearStatsDescription(item)
            + GearOptionHelpText(item);
    }

    private string CompactItemDetailText(ItemState item)
    {
        if (item == null)
        {
            return "비어 있음";
        }

        return ItemLabel(item) + " / 전투력 +" + item.power + " / " + CompactGearStatsDescription(item);
    }

    private ItemState EquippedItemForReplacement(string type, int accessorySlot)
    {
        if (type == "Weapon")
        {
            return player.weapon;
        }
        if (type == "Armor")
        {
            return player.armor;
        }
        if (accessorySlot == 1)
        {
            return player.charm;
        }
        if (accessorySlot == 2)
        {
            return player.charm2;
        }
        if (accessorySlot == 3)
        {
            return player.charm3;
        }
        if (accessorySlot == 4)
        {
            return player.charm4;
        }
        return null;
    }

    private string AccessorySlotButtonLabel(ItemState replacement, int slot, ItemState equipped)
    {
        var current = equipped == null ? "빈칸" : "현재 +" + equipped.power;
        return slot + "번 교체 " + GearPowerDeltaText(replacement, equipped) + "\n" + current;
    }

    private string GearPowerComparisonLine(ItemState replacement, ItemState equipped)
    {
        var replacementPower = replacement == null ? 0 : replacement.power;
        var equippedPower = equipped == null ? 0 : equipped.power;
        return "전투력 새 +" + replacementPower + " / 현재 +" + equippedPower + " / 차이 " + SignedInt(replacementPower - equippedPower);
    }

    private string GearStatDeltaText(ItemState replacement, ItemState equipped)
    {
        if (replacement == null)
        {
            return "능력치 차이 없음";
        }

        var parts = new List<string>();
        AddStatDelta(parts, "체력", replacement.maxHp, equipped != null ? equipped.maxHp : 0);
        AddStatDelta(parts, "마나", replacement.maxMp, equipped != null ? equipped.maxMp : 0);
        AddStatDelta(parts, "공격", replacement.attack, equipped != null ? equipped.attack : 0);
        AddStatDelta(parts, "마력", replacement.magic, equipped != null ? equipped.magic : 0);
        AddStatDelta(parts, "방어", replacement.defense, equipped != null ? equipped.defense : 0);
        AddStatDelta(parts, "속도", replacement.speed, equipped != null ? equipped.speed : 0);
        AddRateDelta(parts, "치명", replacement.critRate, equipped != null ? equipped.critRate : 0f);
        AddRateDelta(parts, "치피", replacement.critDamage, equipped != null ? equipped.critDamage : 0f);
        AddRateDelta(parts, "회피", replacement.evasion, equipped != null ? equipped.evasion : 0f);
        AddRateDelta(parts, "피감", replacement.damageReduction, equipped != null ? equipped.damageReduction : 0f);
        AddRateDelta(parts, "흡혈", replacement.lifeSteal, equipped != null ? equipped.lifeSteal : 0f);
        AddRateDelta(parts, "마나재생", replacement.manaRegen, equipped != null ? equipped.manaRegen : 0f);
        AddRateDelta(parts, "상태 피해", replacement.statusPower, equipped != null ? equipped.statusPower : 0f);
        AddRateDelta(parts, "발견", replacement.itemFind, equipped != null ? equipped.itemFind : 0f);
        return parts.Count == 0 ? "능력치 차이 없음" : "능력치 차이: " + string.Join(" / ", parts.ToArray());
    }

    private void AddStatDelta(List<string> parts, string label, int replacement, int equipped)
    {
        var delta = replacement - equipped;
        if (delta != 0)
        {
            parts.Add(label + " " + SignedInt(delta));
        }
    }

    private void AddRateDelta(List<string> parts, string label, float replacement, float equipped)
    {
        var delta = RoundToGameInt((replacement - equipped) * 100f);
        if (delta != 0)
        {
            parts.Add(label + " " + SignedInt(delta) + "%");
        }
    }

    private string GearPowerDeltaText(ItemState replacement, ItemState equipped)
    {
        if (replacement == null)
        {
            return "";
        }
        if (equipped == null)
        {
            return "(빈칸)";
        }
        return "(" + SignedInt(replacement.power - equipped.power) + ")";
    }

    private string SignedInt(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }

    private string ItemLabel(ItemState item)
    {
        if (item == null)
        {
            return "알 수 없는 아이템";
        }

        var enhanceLabel = item.level > 0 ? " +" + item.level : "";
        return item.name + enhanceLabel + " [" + TypeLabel(item.type) + "] " + RarityLabel(item.rarity);
    }

    private string ItemListButtonLabel(ItemState item)
    {
        if (item == null)
        {
            return "비정상 아이템";
        }

        return ItemLabel(item) + "\n전투력 +" + item.power + " / " + CompactGearStatsDescription(item);
    }

    private string GearStatsDescription(ItemState item)
    {
        var parts = new List<string>();
        if (item.maxHp != 0) parts.Add("체력 +" + item.maxHp);
        if (item.maxMp != 0) parts.Add("마나 +" + item.maxMp);
        if (item.attack != 0) parts.Add("공격 +" + item.attack);
        if (item.magic != 0) parts.Add("마력 +" + item.magic);
        if (item.defense != 0) parts.Add("방어 +" + item.defense);
        if (item.critRate > 0f) parts.Add("치명 +" + RoundToGameInt(item.critRate * 100f) + "%");
        if (item.critDamage > 0f) parts.Add("치명피해 +" + RoundToGameInt(item.critDamage * 100f) + "%");
        if (item.evasion > 0f) parts.Add("회피 +" + RoundToGameInt(item.evasion * 100f) + "%");
        if (item.damageReduction > 0f) parts.Add("피해감소 +" + RoundToGameInt(item.damageReduction * 100f) + "%");
        if (item.lifeSteal > 0f) parts.Add("흡혈 +" + RoundToGameInt(item.lifeSteal * 100f) + "%");
        if (item.manaRegen > 0f) parts.Add("마나재생 +" + RoundToGameInt(item.manaRegen * 100f) + "%");
        if (item.statusPower > 0f) parts.Add("상태 피해 +" + RoundToGameInt(item.statusPower * 100f) + "%");
        if (item.itemFind > 0f) parts.Add("아이템발견 +" + RoundToGameInt(item.itemFind * 100f) + "%");
        if (item.speed > 0) parts.Add("속도 +" + item.speed);
        return parts.Count == 0 ? "전투력 +" + item.power : string.Join(" / ", parts.ToArray());
    }

    private string CompactGearStatsDescription(ItemState item)
    {
        if (item == null)
        {
            return "능력치 없음";
        }

        var parts = new List<string>();
        if (item.attack != 0) parts.Add("공격 +" + item.attack);
        if (item.magic != 0) parts.Add("마력 +" + item.magic);
        if (item.defense != 0) parts.Add("방어 +" + item.defense);
        if (item.maxHp != 0) parts.Add("체력 +" + item.maxHp);
        if (item.maxMp != 0) parts.Add("마나 +" + item.maxMp);
        if (item.speed > 0) parts.Add("속도 +" + item.speed);
        if (item.critRate > 0f) parts.Add("치명 +" + RoundToGameInt(item.critRate * 100f) + "%");
        if (item.evasion > 0f) parts.Add("회피 +" + RoundToGameInt(item.evasion * 100f) + "%");
        if (item.damageReduction > 0f) parts.Add("피감 +" + RoundToGameInt(item.damageReduction * 100f) + "%");
        if (item.statusPower > 0f) parts.Add("상태 피해 +" + RoundToGameInt(item.statusPower * 100f) + "%");
        if (parts.Count == 0)
        {
            return "전투력 +" + item.power;
        }

        var max = Mathf.Min(3, parts.Count);
        var compact = new List<string>();
        for (var i = 0; i < max; i++)
        {
            compact.Add(parts[i]);
        }
        if (parts.Count > max)
        {
            compact.Add("외 " + (parts.Count - max));
        }
        return string.Join(" / ", compact.ToArray());
    }

    private string GearOptionHelpText(ItemState item)
    {
        if (item == null)
        {
            return "";
        }

        var parts = new List<string>();
        if (item.statusPower > 0f) parts.Add("상태 피해: 화상/중독/출혈/감전/마나 연소 피해 증가");
        if (item.itemFind > 0f) parts.Add("아이템발견: 전투 보상 장비 획득 기대치 증가");
        if (item.manaRegen > 0f) parts.Add("마나재생: 턴마다 회복되는 MP 증가");
        if (item.damageReduction > 0f) parts.Add("피해감소: 받는 최종 피해 감소");
        if (item.evasion > 0f) parts.Add("회피: 공격을 완전히 피할 확률");
        if (item.lifeSteal > 0f) parts.Add("흡혈: 준 피해 일부를 체력으로 회복");
        return parts.Count == 0 ? "" : "\n설명: " + string.Join(" / ", parts.ToArray());
    }

    private string TypeLabel(string type)
    {
        if (type == "Weapon")
        {
            return "무기";
        }
        if (type == "Armor")
        {
            return "방어구";
        }
        return "장신구";
    }

    private string RarityLabel(int rarity)
    {
        if (TryGetRarityData(rarity, out var rarityData) && !string.IsNullOrEmpty(rarityData.rarityName))
        {
            return rarityData.rarityName;
        }

        var names = new[] { "일반", "고급", "희귀", "영웅", "전설", "신화", "고대", "불멸", "성물", "성좌", "근원", "초월", "네임드" };
        return names[Mathf.Clamp(rarity, 0, names.Length - 1)];
    }

    private Color RarityColor(int rarity)
    {
        if (TryGetRarityData(rarity, out var rarityData))
        {
            return rarityData.color;
        }

        var colors = new[]
        {
            Rgb(156, 163, 175),
            Rgb(34, 197, 94),
            Rgb(59, 130, 246),
            Rgb(168, 85, 247),
            Rgb(245, 158, 11),
            Rgb(236, 72, 153),
            Rgb(20, 184, 166),
            Rgb(239, 68, 68),
            Rgb(132, 204, 22),
            Rgb(96, 165, 250),
            Rgb(251, 113, 133),
            Rgb(248, 250, 252),
            Rgb(250, 204, 21)
        };
        return colors[Mathf.Clamp(rarity, 0, colors.Length - 1)];
    }

    private void ClearRoot()
    {
        if (root == null)
        {
            return;
        }

        InvalidateCombatView();
        for (var i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }

        AddGeneratedScreenBackdrop();
        QueueVisualPolishRefresh();
        QueueScreenMotionEntrance();
    }

    private RectTransform AddPanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        var requestedColor = color;
        var isScreenPage = IsGeneratedScreenPage(name, parent);
        image.color = ResolvePanelSurfaceColor(name, requestedColor, isScreenPage, parent);
        image.raycastTarget = ShouldPanelReceiveRaycasts(name, image.color, isScreenPage);
        var rect = go.GetComponent<RectTransform>();
        return rect;
    }

    private Color ResolvePanelSurfaceColor(string name, Color requestedColor, bool isScreenPage, Transform parent)
    {
        if (isScreenPage || requestedColor.a <= 0.05f)
        {
            return new Color(requestedColor.r, requestedColor.g, requestedColor.b, 0f);
        }

        if (ShouldPreservePanelGraphic(name))
        {
            return requestedColor;
        }

        // Every layout/content surface is deliberately transparent. Buttons,
        // bars, generated artwork and decorative frames remain visible, but no
        // generic rectangular UX fill is allowed to appear on any screen.
        return new Color(requestedColor.r, requestedColor.g, requestedColor.b, 0f);
    }

    private bool ShouldPanelReceiveRaycasts(string name, Color displayedColor, bool isScreenPage)
    {
        if (isScreenPage || name == "Root")
        {
            return false;
        }

        return displayedColor.a > 0.05f
            || name == "Dungeon Info Popup"
            || name == "Dungeon World Map Panel"
            || name.EndsWith(" Viewport", StringComparison.Ordinal);
    }

    private static bool ShouldPreservePanelGraphic(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name == "Fill"
            || name.EndsWith(" Bar", StringComparison.Ordinal)
            || name.EndsWith(" Image", StringComparison.Ordinal)
            || name.Contains("World Map Image")
            || name.Contains("Opening World Map")
            || name.Contains("Closed Dungeon Scroll");
    }

    private RectTransform AddFlatPanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return go.GetComponent<RectTransform>();
    }

    private bool ShouldAddOrnateFrame(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return true;
        }

        return name != "Root"
            && !name.EndsWith(" Bar")
            && !name.Contains("World Map Image")
            && !name.Contains("Opening World Map")
            && !name.Contains("Closed Dungeon Scroll")
            && !name.Contains("Scroll Leaf")
            && !name.Contains("Map Shadow")
            && !name.Contains("Map Gold")
            && !name.Contains("Status Overlay")
            && !name.Contains("Top Overlay")
            && !name.Contains("Region Button Shine")
            && !name.Contains("Pin Inner Light")
            && !name.Contains("Dungeon Pin Glow")
            && !name.Contains("Dungeon Pin Hit")
            && !name.Contains("Pin Label")
            && !name.Contains("Scrollbar")
            && !name.Contains("Panel Depth")
            && !name.Contains("Button Shine")
            && !name.Contains("Button Shade")
            && !name.Contains("Portrait");
    }

    private bool CompactFrameForPanel(string name)
    {
        return name.Contains("Bar")
            || name.Contains("HUD")
            || name.Contains("Row")
            || name.Contains("Actions")
            || name.Contains("Buttons")
            || name.Contains("Viewport")
            || name.Contains("Content")
            || name.Contains("Status Overlay")
            || name.Contains("Top Overlay");
    }

    private bool ShouldAddPanelDepth(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return true;
        }

        return name != "Root"
            && name != "Town"
            && name != "Combat"
            && name != "Victory Result"
            && name != "Defeat Result"
            && name != "Game Clear"
            && !name.Contains("Dungeon Select Full Map")
            && !name.Contains("World Map")
            && !name.Contains("Opening World Map")
            && !name.Contains("Closed Dungeon Scroll")
            && !name.Contains("Scroll Leaf")
            && !name.Contains("Map Shadow")
            && !name.Contains("Map Gold")
            && !name.Contains("Scrollbar")
            && !name.Contains("Panel Depth")
            && !name.Contains("Button Shine")
            && !name.Contains("Button Shade")
            && !name.Contains("Fill")
            && !name.Contains("Portrait");
    }

    private void AddPanelDepth(RectTransform target, bool compact)
    {
        if (target == null || target.GetComponent<UiFramePresenter>() != null)
        {
            return;
        }

        var top = AddFlatPanel("Panel Depth Top", target, Rgba(255, 255, 255, compact ? (byte)8 : (byte)12));
        top.GetComponent<Image>().raycastTarget = false;
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = new Vector2(0f, -5f);
        top.sizeDelta = new Vector2(-8f, compact ? 4f : 6f);
        top.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        var bottom = AddFlatPanel("Panel Depth Bottom", target, Rgba(0, 0, 0, compact ? (byte)22 : (byte)34));
        bottom.GetComponent<Image>().raycastTarget = false;
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.anchoredPosition = new Vector2(0f, 5f);
        bottom.sizeDelta = new Vector2(-8f, compact ? 5f : 8f);
        bottom.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private void AddSideAccent(RectTransform target, Color accent)
    {
        if (target == null)
        {
            return;
        }

        var stripe = AddFlatPanel("Panel Side Accent", target, new Color(accent.r, accent.g, accent.b, 0.76f));
        stripe.GetComponent<Image>().raycastTarget = false;
        stripe.anchorMin = new Vector2(0f, 0f);
        stripe.anchorMax = new Vector2(0f, 1f);
        stripe.pivot = new Vector2(0f, 0.5f);
        stripe.anchoredPosition = new Vector2(3f, 0f);
        stripe.sizeDelta = new Vector2(4f, -12f);
        stripe.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private Color UiFrameAccentColor(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (name.Contains("Dungeon") || name.Contains("Map") || currentScreen == AetheriaScreen.DungeonSelect)
            {
                return Rgb(245, 158, 11);
            }
            if (name.Contains("Inventory") || name.Contains("Bag") || currentScreen == AetheriaScreen.Inventory)
            {
                return Rgb(178, 148, 102);
            }
            if (name.Contains("Enhancement") || name.Contains("Forge") || currentScreen == AetheriaScreen.Enhancement)
            {
                return Rgb(245, 158, 11);
            }
            if (name.Contains("Skill") || currentScreen == AetheriaScreen.SkillTraining)
            {
                return Rgb(59, 130, 246);
            }
            if (name.Contains("Craft") || currentScreen == AetheriaScreen.Crafting)
            {
                return Rgb(168, 85, 247);
            }
            if (name.Contains("Combat") || currentScreen == AetheriaScreen.Combat)
            {
                return Rgb(239, 68, 68);
            }
        }

        switch (currentScreen)
        {
            case AetheriaScreen.Inventory:
                return Rgb(178, 148, 102);
            case AetheriaScreen.Enhancement:
                return Rgb(245, 158, 11);
            case AetheriaScreen.SkillTraining:
                return Rgb(59, 130, 246);
            case AetheriaScreen.Crafting:
                return Rgb(168, 85, 247);
            case AetheriaScreen.DungeonSelect:
                return Rgb(245, 158, 11);
            case AetheriaScreen.Combat:
                return Rgb(239, 68, 68);
            case AetheriaScreen.Victory:
                return Rgb(16, 185, 129);
            case AetheriaScreen.Defeat:
                return Rgb(239, 68, 68);
            default:
                return Rgb(190, 190, 184);
        }
    }

    private void AddOrnateFrame(RectTransform target, Color accent, bool compact)
    {
        if (target == null)
        {
            return;
        }

        var presenter = target.gameObject.AddComponent<UiFramePresenter>();
        var baseLine = Rgba(196, 196, 188, compact ? (byte)120 : (byte)150);
        var accentLine = new Color(accent.r, accent.g, accent.b, compact ? 0.42f : 0.66f);
        var darkLine = Rgba(0, 0, 0, compact ? (byte)72 : (byte)98);

        AddUiFramePart(target, presenter, "Frame Outer Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1f), new Vector2(0f, 2f), baseLine, 0f);
        AddUiFramePart(target, presenter, "Frame Outer Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f), baseLine, 0f);
        AddUiFramePart(target, presenter, "Frame Outer Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(2f, 0f), baseLine, 0f);
        AddUiFramePart(target, presenter, "Frame Outer Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-1f, 0f), new Vector2(2f, 0f), baseLine, 0f);

        AddUiFramePart(target, presenter, "Frame Inner Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(0f, 1.4f), darkLine, 0f);
        AddUiFramePart(target, presenter, "Frame Inner Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(0f, 1.4f), darkLine, 0f);

        var corner = compact ? 12f : 22f;
        var thickness = compact ? 2f : 3f;
        AddCornerFrame(target, presenter, corner, thickness, baseLine);

        if (!compact)
        {
            AddUiFramePart(target, presenter, "Frame Accent Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(76f, 3f), accentLine, 0f);
            AddUiFramePart(target, presenter, "Frame Accent Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(76f, 3f), accentLine, 0f);
            AddUiFramePart(target, presenter, "Frame Top Gem", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(13f, 13f), accentLine, 45f);
            AddUiFramePart(target, presenter, "Frame Bottom Gem", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(13f, 13f), accentLine, 45f);
        }
    }

    private void AddCornerFrame(RectTransform target, UiFramePresenter presenter, float corner, float thickness, Color color)
    {
        AddUiFramePart(target, presenter, "Frame TL Horizontal", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -4f), new Vector2(corner, thickness), color, 0f);
        AddUiFramePart(target, presenter, "Frame TL Vertical", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -4f), new Vector2(thickness, corner), color, 0f);
        AddUiFramePart(target, presenter, "Frame TR Horizontal", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -4f), new Vector2(corner, thickness), color, 0f);
        AddUiFramePart(target, presenter, "Frame TR Vertical", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -4f), new Vector2(thickness, corner), color, 0f);
        AddUiFramePart(target, presenter, "Frame BL Horizontal", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(4f, 4f), new Vector2(corner, thickness), color, 0f);
        AddUiFramePart(target, presenter, "Frame BL Vertical", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(4f, 4f), new Vector2(thickness, corner), color, 0f);
        AddUiFramePart(target, presenter, "Frame BR Horizontal", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-4f, 4f), new Vector2(corner, thickness), color, 0f);
        AddUiFramePart(target, presenter, "Frame BR Vertical", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-4f, 4f), new Vector2(thickness, corner), color, 0f);
    }

    private RectTransform AddUiFramePart(RectTransform target, UiFramePresenter presenter, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, float rotation)
    {
        var part = AddFlatPanel(name, target, color);
        var image = part.GetComponent<Image>();
        image.raycastTarget = false;
        part.anchorMin = anchorMin;
        part.anchorMax = anchorMax;
        part.pivot = pivot;
        part.anchoredPosition = anchoredPosition;
        part.sizeDelta = sizeDelta;
        part.localRotation = Quaternion.Euler(0f, 0f, rotation);
        var layout = part.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        presenter.Add(part);
        return part;
    }

    private RectTransform AddRow(string name, Transform parent, int spacing, TextAnchor alignment)
    {
        var row = AddPanel(name, parent, new Color(0, 0, 0, 0));
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return row;
    }

    private RectTransform AddScrollList(string name, Transform parent, Color color, float preferredWidth, float preferredHeight, int spacing, RectOffset padding)
    {
        var viewportColor = color.a > 0.05f ? Rgba(248, 251, 253, 236) : color;
        var viewport = AddPanel(name + " Viewport", parent, viewportColor);
        AddLayoutSize(viewport, preferredWidth, preferredHeight);
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = AddPanel(name + " Content", viewport, new Color(0, 0, 0, 0));
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.offsetMin = Vector2.zero;
        content.offsetMax = new Vector2(-(UiScrollbarWidth + 8f), 0f);
        AddVertical(content, spacing, TextAnchor.UpperLeft, padding);

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32f;
        scroll.inertia = true;
        AddVerticalScrollbar(viewport, scroll);
        return content;
    }

    private void AddVerticalScrollbar(RectTransform viewport, ScrollRect scroll)
    {
        if (viewport == null || scroll == null)
        {
            return;
        }

        var track = AddFlatPanel("Scrollbar Track", viewport, new Color(174f / 255f, 194f / 255f, 209f / 255f, 0f));
        track.anchorMin = new Vector2(1f, 0f);
        track.anchorMax = new Vector2(1f, 1f);
        track.pivot = new Vector2(1f, 0.5f);
        track.anchoredPosition = new Vector2(-4f, 0f);
        track.sizeDelta = new Vector2(UiScrollbarWidth, -12f);
        track.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        track.GetComponent<Image>().raycastTarget = true;

        var handle = AddFlatPanel("Scrollbar Handle", track, Rgba(19, 128, 160, 220));
        Stretch(handle, 2f, 2f, 2f, 2f);
        handle.GetComponent<Image>().raycastTarget = true;

        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.handleRect = handle;
        scrollbar.size = 0.22f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scroll.verticalScrollbarSpacing = -UiScrollbarWidth;
    }

    private void AddVertical(RectTransform target, int spacing, TextAnchor alignment, RectOffset padding)
    {
        var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = padding;
    }

    private void AddMessageBanner(Transform parent, string message, Color accent, float preferredHeight)
    {
        var bannerColor = Color.Lerp(Rgb(250, 252, 249), new Color(accent.r, accent.g, accent.b, 1f), 0.08f);
        var banner = AddPanel("Screen Message Banner", parent, bannerColor);
        AddLayoutSize(banner, -1, preferredHeight);
        var layout = banner.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.padding = new RectOffset(14, 16, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var stripe = AddFlatPanel("Message Accent", banner, accent);
        AddLayoutSize(stripe, 5, -1);
        stripe.GetComponent<Image>().raycastTarget = false;

        var text = AddText(banner, string.IsNullOrEmpty(message) ? " " : message, 19, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, preferredHeight - 16f);
        AddLayoutSize(text.GetComponent<RectTransform>(), -1, preferredHeight - 16f);
    }

    private void AddDivider(Transform parent, Color color)
    {
        var divider = AddFlatPanel("Divider", parent, color);
        AddLayoutSize(divider, -1, 2);
        divider.GetComponent<Image>().raycastTarget = false;
    }

    private Text AddText(Transform parent, string value, int size, FontStyle style, Color color, TextAnchor alignment, float preferredHeight)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = value ?? "";
        text.font = uiFont;
        var parentName = parent != null ? parent.name : "";
        var compactMapLabel = currentScreen == AetheriaScreen.DungeonSelect
            && size <= 16
            && (parentName.Contains("Pin Label") || parentName.StartsWith("Region "));
        var readableSize = compactMapLabel ? Mathf.Max(18, size) : Mathf.Max(18, size);
        text.fontSize = readableSize;
        text.fontStyle = style == FontStyle.Normal ? FontStyle.Bold : style;
        color = EnsureHighContrastTextColor(ResolveCharacterThemeTextColor(color, size, style));
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Min(16, readableSize);
        text.resizeTextMaxSize = readableSize;
        text.lineSpacing = value != null && value.Contains("\n") ? 1.18f : 1f;

        // All generic UX boxes are invisible, so use one unambiguous treatment:
        // light glyphs, a near-black outline, and a short dark shadow. Varying the
        // stroke by text size prevents small Korean glyph counters from filling in.
        var stroke = readableSize >= 32 ? 1.8f : readableSize >= 22 ? 1.55f : 1.25f;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.01f, 0.02f, 0.035f, 0.96f);
        outline.effectDistance = new Vector2(stroke, -stroke);

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.38f);
        shadow.effectDistance = new Vector2(0.9f, -0.9f);

        AddLayoutSize(go.GetComponent<RectTransform>(), -1, preferredHeight);
        return text;
    }

    private static Color EnsureHighContrastTextColor(Color color)
    {
        var opaque = new Color(color.r, color.g, color.b, 1f);
        var luminance = opaque.r * 0.2126f + opaque.g * 0.7152f + opaque.b * 0.0722f;
        if (luminance >= 0.82f)
        {
            return opaque;
        }

        var whiteBlend = Mathf.Clamp01((0.82f - luminance) / Mathf.Max(0.01f, 1f - luminance));
        return Color.Lerp(opaque, Color.white, whiteBlend);
    }

    private int DynamicTextShrink(int size, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 8;
        }

        var longest = 0;
        var lines = value.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            longest = Mathf.Max(longest, lines[i].Length);
        }

        if (longest > 42 || value.Length > 120) return Mathf.Max(12, size - 9);
        if (longest > 28 || value.Length > 70) return Mathf.Max(10, size - 10);
        return 8;
    }

    private Button AddButton(Transform parent, string label, Action onClick, Color color)
    {
        label = label ?? "";
        var go = new GameObject(string.IsNullOrEmpty(label) ? "Button" : label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        var rawAccent = new Color(color.r, color.g, color.b, 1f);
        var accent = color.grayscale > 0.72f ? Rgb(48, 91, 122) : rawAccent;
        var surface = Color.Lerp(Rgb(248, 251, 253), accent, 0.14f);
        image.color = surface;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
        {
            button.onClick.AddListener(() =>
            {
                PlayUiClickSound();
                onClick();
            });
        }

        var hasCharacterThemeSkin = ApplyCharacterThemeButtonFrame(button);
        var hasIllustratedSkin = hasCharacterThemeSkin;
        if (!hasIllustratedSkin)
        {
            hasIllustratedSkin = ApplyGeneratedButtonSkin(image, accent);
        }

        if (hasCharacterThemeSkin)
        {
            image.color = Color.Lerp(Color.white, accent, 0.16f);
        }

        if (!hasIllustratedSkin)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(Color.white, accent, 0.08f);
        colors.pressedColor = Color.Lerp(Color.white, accent, 0.20f);
        colors.disabledColor = Rgb(218, 226, 232);
        button.colors = colors;

        var rect = go.GetComponent<RectTransform>();
        if (!hasIllustratedSkin)
        {
            AddSideAccent(rect, accent);
        }
        AddLayoutSize(rect, 260, 62);

        var textSize = ButtonTextSize(label);
        var resolvedButtonTextColor = hasCharacterThemeSkin
            ? CharacterThemeButtonText(ScreenCharacterThemeKey(), Color.white)
            : buttonTextColor;
        var resolvedDisabledTextColor = hasCharacterThemeSkin
            ? Rgb(196, 205, 216)
            : disabledButtonTextColor;
        var labelText = AddText(go.transform, label, textSize, FontStyle.Bold, resolvedButtonTextColor, TextAnchor.MiddleCenter, 62);
        labelText.resizeTextMinSize = 17;
        Stretch(labelText.GetComponent<RectTransform>(), 34, 4, 34, 4);
        var labelOutline = labelText.GetComponent<Outline>();
        if (labelOutline != null)
        {
            labelOutline.effectColor = hasCharacterThemeSkin
                ? new Color(1f, 1f, 1f, 0.94f)
                : new Color(1f, 1f, 1f, 0.78f);
            labelOutline.effectDistance = hasCharacterThemeSkin
                ? new Vector2(1.1f, -1.1f)
                : new Vector2(0.55f, -0.55f);
        }
        var labelShadow = labelText.GetComponent<Shadow>();
        if (labelShadow != null)
        {
            labelShadow.effectColor = Color.clear;
            labelShadow.effectDistance = Vector2.zero;
        }
        var textPresenter = go.AddComponent<UiButtonTextPresenter>();
        textPresenter.Configure(button, labelText, resolvedButtonTextColor, resolvedDisabledTextColor);
        return button;
    }

    private int ButtonTextSize(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return UiButtonDefaultTextSize;
        }

        if (label.Contains("\n")) return 21;
        if (label.Length > 18) return 20;
        if (label.Length > 10) return 22;
        return UiButtonDefaultTextSize;
    }

    private void AddButtonDepth(RectTransform rect, Color color)
    {
        if (rect == null)
        {
            return;
        }

        var shine = AddFlatPanel("Button Shine", rect, Rgba(255, 255, 255, 22));
        shine.GetComponent<Image>().raycastTarget = false;
        shine.anchorMin = new Vector2(0f, 0.56f);
        shine.anchorMax = new Vector2(1f, 1f);
        shine.offsetMin = new Vector2(7f, 0f);
        shine.offsetMax = new Vector2(-7f, -5f);
        shine.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        var shade = AddFlatPanel("Button Shade", rect, new Color(0f, 0f, 0f, Mathf.Clamp01(color.a * 0.2f)));
        shade.GetComponent<Image>().raycastTarget = false;
        shade.anchorMin = new Vector2(0f, 0f);
        shade.anchorMax = new Vector2(1f, 0.34f);
        shade.offsetMin = new Vector2(7f, 5f);
        shade.offsetMax = new Vector2(-7f, 0f);
        shade.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private Button AddFlexibleButton(Transform parent, string label, Action onClick, Color color, float preferredHeight)
    {
        var button = AddButton(parent, label, onClick, color);
        AddLayoutSize(button.GetComponent<RectTransform>(), -1, preferredHeight);
        return button;
    }

    private void AddPortrait(Transform parent, string portraitName, float size)
    {
        AddCharacterArt(parent, portraitName, "idle", size, size);
    }

    private void AddBar(Transform parent, int value, int max, Color color, string label)
    {
        var frame = AddPanel(label + " Bar", parent, Rgb(12, 15, 23));
        frame.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(frame, -1, 34);

        var safeMax = Mathf.Max(0, max);
        var fill = AddPanel("Fill", frame, color);
        fill.SetAsFirstSibling();
        fill.GetComponent<Image>().raycastTarget = false;
        fill.anchorMin = new Vector2(0, 0);
        fill.anchorMax = new Vector2(Mathf.Clamp01(safeMax <= 0 ? 0 : (float)Mathf.Max(0, value) / safeMax), 1);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        var text = AddText(frame, label + " " + Mathf.Max(0, value) + "/" + safeMax, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 34);
        Stretch(text.GetComponent<RectTransform>(), 0, 0, 0, 0);
    }

    private void AddLayoutSize(RectTransform rect, float preferredWidth, float preferredHeight)
    {
        var layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = rect.gameObject.AddComponent<LayoutElement>();
        }

        if (preferredWidth > 0)
        {
            layout.preferredWidth = preferredWidth;
        }
        if (preferredHeight > 0)
        {
            layout.preferredHeight = preferredHeight;
        }
        if (preferredWidth < 0)
        {
            layout.preferredWidth = -1;
            layout.flexibleWidth = 1;
        }
        if (preferredHeight < 0)
        {
            layout.preferredHeight = -1;
            layout.flexibleHeight = 1;
        }
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static Color Rgb(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private static Color Rgba(byte r, byte g, byte b, byte a)
    {
        return new Color32(r, g, b, a);
    }

    private sealed class UiFramePresenter : MonoBehaviour
    {
        private readonly List<RectTransform> parts = new List<RectTransform>();

        public void Add(RectTransform part)
        {
            if (part != null)
            {
                parts.Add(part);
            }
        }

        private void LateUpdate()
        {
            for (var i = parts.Count - 1; i >= 0; i--)
            {
                var part = parts[i];
                if (part == null)
                {
                    parts.RemoveAt(i);
                    continue;
                }

                part.SetAsLastSibling();
            }
        }
    }

    private sealed class UiButtonTextPresenter : MonoBehaviour
    {
        private Button button;
        private Text label;
        private Color enabledColor;
        private Color disabledColor;

        public void Configure(Button sourceButton, Text sourceLabel, Color sourceEnabledColor, Color sourceDisabledColor)
        {
            button = sourceButton;
            label = sourceLabel;
            enabledColor = sourceEnabledColor;
            disabledColor = sourceDisabledColor;
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (button == null || label == null)
            {
                return;
            }

            label.color = button.interactable ? enabledColor : disabledColor;
        }
    }

    private enum AetheriaScreen
    {
        MainMenu,
        ClassSelect,
        Guide,
        Town,
        Inventory,
        Enhancement,
        SkillTraining,
        Crafting,
        DungeonSelect,
        Combat,
        Victory,
        Defeat,
        GameClear
    }

    private enum InventorySortMode
    {
        Power,
        Rarity,
        Type
    }

    private struct InventoryEntry
    {
        public int index;
        public ItemState item;
    }

    [Serializable]
    private sealed class PlayerState
    {
        public int saveVersion;
        public string heroName;
        public string heroClass;
        public string portraitName;
        public int level;
        public int xp;
        public int stage;
        public int gold;
        public int potions;
        public int hp;
        public int mp;
        public int baseHp;
        public int baseMp;
        public int baseAttack;
        public int baseMagic;
        public int baseDefense;
        public float baseCrit;
        public ItemState weapon;
        public ItemState armor;
        public ItemState charm;
        public ItemState charm2;
        public ItemState charm3;
        public ItemState charm4;
        public List<ItemState> inventory = new List<ItemState>();
        public List<SkillLevelState> skillLevels = new List<SkillLevelState>();
        public List<DungeonProgressState> dungeonProgress = new List<DungeonProgressState>();
        public List<string> generalLogs = new List<string>();
        public bool hasSeenGuide;
    }

    [Serializable]
    private sealed class ItemState
    {
        public string id;
        public string name;
        public string type;
        public int rarity;
        public int power;
        public int level;
        public int maxHp;
        public int maxMp;
        public int attack;
        public int magic;
        public int defense;
        public int speed;
        public float critRate;
        public float critDamage;
        public float evasion;
        public float damageReduction;
        public float lifeSteal;
        public float manaRegen;
        public float statusPower;
        public float itemFind;
    }

    [Serializable]
    private sealed class SkillLevelState
    {
        public string skillName;
        public int level;
    }

    [Serializable]
    private sealed class DungeonProgressState
    {
        public int dungeonNumber;
        public bool floorCleared;
        public bool bossCleared;
    }

    private sealed class EnemyState
    {
        public string name;
        public string description;
        public string spriteKey;
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int attack;
        public int magic;
        public int defense;
        public int speed;
        public float crit;
        public int gold;
        public int xp;
        public List<SkillState> skills = new List<SkillState>();
    }

    private sealed class StatusEffect
    {
        public string type;
        public int duration;
        public float value;
    }

    private sealed class DungeonData
    {
        public readonly int number;
        public readonly string name;
        public readonly int recommendedLevel;
        public readonly int floors;
        public readonly string description;
        public readonly List<EnemyTemplate> monsters;
        public readonly EnemyTemplate boss;

        public DungeonData(int number, string name, int recommendedLevel, int floors, string description, List<EnemyTemplate> monsters, EnemyTemplate boss)
        {
            this.number = number;
            this.name = name;
            this.recommendedLevel = recommendedLevel;
            this.floors = floors;
            this.description = description;
            this.monsters = monsters;
            this.boss = boss;
        }
    }

    private sealed class DungeonMapInputHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        private AetheriaGame owner;
        private RectTransform viewport;
        private RectTransform mapImage;
        private Vector2 lastLocalPointer;
        private bool hasLastLocalPointer;

        public void Configure(AetheriaGame owner, RectTransform viewport, RectTransform mapImage)
        {
            this.owner = owner;
            this.viewport = viewport;
            this.mapImage = mapImage;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner == null || viewport == null || DungeonInfoModalOpen(viewport))
            {
                hasLastLocalPointer = false;
                return;
            }

            hasLastLocalPointer = owner.TryDungeonMapLocalPointer(viewport, eventData, out lastLocalPointer);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner == null || viewport == null || DungeonInfoModalOpen(viewport))
            {
                hasLastLocalPointer = false;
                return;
            }

            if (!hasLastLocalPointer)
            {
                hasLastLocalPointer = owner.TryDungeonMapLocalPointer(viewport, eventData, out lastLocalPointer);
                return;
            }

            owner.OnDungeonMapDrag(viewport, mapImage, eventData, ref lastLocalPointer);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (owner == null || DungeonInfoModalOpen(viewport))
            {
                return;
            }

            owner.OnDungeonMapScroll(viewport, mapImage, eventData);
        }
    }

    private sealed class DungeonMapRegion
    {
        public readonly string key;
        public readonly string name;
        public readonly Vector2 center;
        public readonly Vector2[] points;
        public readonly Color color;

        public DungeonMapRegion(string key, string name, Vector2 center, Vector2[] points, Color color)
        {
            this.key = key;
            this.name = name;
            this.center = center;
            this.points = points;
            this.color = color;
        }
    }

    private sealed class EnemyTemplate
    {
        public readonly string name;
        public readonly string description;
        public readonly string spriteKey;
        public readonly int hp;
        public readonly int maxMp;
        public readonly int attack;
        public readonly int magic;
        public readonly int defense;
        public readonly int speed;
        public readonly float crit;
        public readonly int gold;
        public readonly int xp;
        public readonly List<SkillState> skills;

        public EnemyTemplate(string name, string description, int hp, int maxMp, int attack, int magic, int defense, int speed, float crit, int gold, int xp, List<SkillState> skills, string spriteKey = null)
        {
            this.name = name;
            this.description = description;
            this.spriteKey = spriteKey;
            this.hp = hp;
            this.maxMp = maxMp;
            this.attack = attack;
            this.magic = magic;
            this.defense = defense;
            this.speed = speed;
            this.crit = crit;
            this.gold = gold;
            this.xp = xp;
            this.skills = skills;
        }
    }

    private sealed class HeroClass
    {
        public readonly string name;
        public readonly string description;
        public readonly string portraitName;
        public readonly int hp;
        public readonly int mp;
        public readonly int attack;
        public readonly int magic;
        public readonly int defense;
        public readonly float crit;
        public readonly bool basicAttackUsesMagic;
        public readonly List<SkillState> skills;

        public HeroClass(string name, string description, string portraitName, int hp, int mp, int attack, int magic, int defense, float crit, bool basicAttackUsesMagic = false, List<SkillState> skills = null)
        {
            this.name = name;
            this.description = description;
            this.portraitName = portraitName;
            this.hp = hp;
            this.mp = mp;
            this.attack = attack;
            this.magic = magic;
            this.defense = defense;
            this.crit = crit;
            this.basicAttackUsesMagic = basicAttackUsesMagic;
            this.skills = skills ?? new List<SkillState>();
        }
    }

    private sealed class SkillState
    {
        public readonly string name;
        public readonly int mpCost;
        public readonly float multiplier;
        public readonly bool magic;
        public float stunChance;
        public float burnChance;
        public float poisonChance;
        public float bleedChance;
        public float shockChance;
        public float freezeChance;
        public float blindChance;
        public float weakenChance;
        public float vulnerableChance;
        public float silenceChance;
        public float manaBurnChance;
        public string selfStatusType;
        public int selfStatusDuration;
        public float selfStatusValue;
        public float critBonus;
        public float lifeStealRatio;
        public bool forceCrit;
        public bool noDamage;
        public bool grantsShield;
        public bool healsSelf;

        public SkillState(string name, int mpCost, float multiplier, bool magic)
        {
            this.name = name;
            this.mpCost = mpCost;
            this.multiplier = multiplier;
            this.magic = magic;
        }
    }
}
