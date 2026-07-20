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
    private const string DungeonMapBuildLabel = "UNITY 지도형 던전 UX v18";
    private const string DungeonMapSpritePath = "WebVersion/assets/world-map.png";
    private const string DungeonClosedScrollSpritePath = "WebVersion/assets/closed-bound-scroll.png";
    private const float DungeonMapBaseWidth = 1920f;
    private const float DungeonMapBaseHeight = 1080f;
    private const float DungeonMapRegionZoom = 2.35f;
    private const float DungeonMapMinZoom = 1f;
    private const float DungeonMapMaxZoom = 4.5f;
    private const float DungeonMapPinRevealZoom = DungeonMapRegionZoom;
    private const float DungeonMapWheelStep = 0.22f;
    private const float DungeonMapOpenAnimationSeconds = 1.15f;
    private const byte DungeonInfoPopupAlpha = 204;
    private const int SaveSlotCount = 7;
    private const int CurrentSaveVersion = 1;
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
    private const int UiButtonDefaultTextSize = 21;
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

    private readonly Color pageColor = Rgb(6, 6, 10);
    private readonly Color panelColor = Rgba(15, 15, 25, 218);
    private readonly Color panelAltColor = Rgba(41, 24, 64, 225);
    private readonly Color goldColor = Rgb(245, 158, 11);
    private readonly Color goodColor = Rgb(16, 185, 129);
    private readonly Color dangerColor = Rgb(239, 68, 68);
    private readonly Color manaColor = Rgb(6, 182, 212);
    private readonly Color textColor = Rgb(243, 244, 246);
    private readonly Color mutedColor = Rgb(156, 163, 175);
    private readonly Color neonPurple = Rgb(168, 85, 247);
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
        LoadGameDatabase();
        uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 18);
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        CreateCanvas();
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
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
        scaler.matchWidthOrHeight = 0.5f;

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
        AddVertical(frame, 20, TextAnchor.MiddleCenter, new RectOffset(160, 160, 56, 56));

        AddText(frame, "AETHERIA", 86, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 104);
        AddText(frame, "공허의 연대기", 32, FontStyle.Bold, manaColor, TextAnchor.MiddleCenter, 44);

        var slotPanel = AddPanel("Save Slots", frame, panelColor);
        AddLayoutSize(slotPanel, -1, 420);
        AddVertical(slotPanel, 18, TextAnchor.UpperCenter, new RectOffset(28, 28, 26, 26));
        AddText(slotPanel, "모험 기록", 30, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 44);

        var slots = AddPanel("Slot Grid", slotPanel, new Color(0, 0, 0, 0));
        AddLayoutSize(slots, -1, 306);
        var slotGrid = slots.gameObject.AddComponent<GridLayoutGroup>();
        slotGrid.cellSize = new Vector2(360, 126);
        slotGrid.spacing = new Vector2(14, 14);
        slotGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        slotGrid.constraintCount = 4;
        slotGrid.childAlignment = TextAnchor.UpperCenter;

        for (var i = 0; i < SaveSlotCount; i++)
        {
            var slot = i;
            var selected = slot == activeSlot;
            var label = HasSave(slot) ? SavePreview(slot) : "빈 슬롯 " + (slot + 1);
            var button = AddButton(slots, label, () =>
            {
                activeSlot = slot;
                ShowMainMenu();
            }, selected ? goldColor : panelAltColor);
            AddLayoutSize(button.GetComponent<RectTransform>(), 360, 126);
        }

        var buttons = AddRow("Menu Buttons", frame, 16, TextAnchor.MiddleCenter);
        AddLayoutSize(buttons, -1, 76);
        var loadButton = AddButton(buttons, HasSave(activeSlot) ? "선택 슬롯 불러오기" : "불러올 저장 없음", LoadSelectedSaveSlot, goodColor);
        loadButton.interactable = HasSave(activeSlot);
        var canStartAdventure = !HasSave(activeSlot) || HasEmptySaveSlot();
        var newAdventureLabel = !HasSave(activeSlot) ? "선택 슬롯 새 모험" : (HasEmptySaveSlot() ? "빈 슬롯 새 모험" : "빈 슬롯 없음");
        var newAdventureButton = AddButton(buttons, newAdventureLabel, () =>
        {
            ShowClassSelect(HasSave(activeSlot) ? FirstEmptySaveSlot() : activeSlot);
        }, manaColor);
        newAdventureButton.interactable = canStartAdventure;
        var resetButton = AddButton(buttons, "선택 슬롯 삭제 " + (activeSlot + 1), () =>
        {
            ShowDeleteSaveConfirm(activeSlot);
        }, dangerColor);
        resetButton.interactable = HasSave(activeSlot);

        AddText(frame, "균열을 넘어 아에테리아의 기록을 되찾으세요.", 20, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 40);
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
            ShowTown("슬롯 " + (activeSlot + 1) + "의 기록을 불러왔습니다.");
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
        ClearRoot();
        var heroClasses = HeroClasses();

        var page = AddPanel("Class Select", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 12, TextAnchor.UpperCenter, new RectOffset(50, 50, 24, 24));

        AddText(page, "영웅 선택", 46, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 54);
        AddText(page, "공허의 원정에 나설 영웅을 선택하세요.", 20, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 28);

        var grid = AddPanel("Class Grid", page, new Color(0, 0, 0, 0));
        AddLayoutSize(grid, -1, 808);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(410, 398);
        layout.spacing = new Vector2(18, 12);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.childAlignment = TextAnchor.MiddleCenter;

        foreach (var heroClass in heroClasses)
        {
            var localHeroClass = heroClass;
            var accent = ClassAccentColor(heroClass.name);
            var selected = selectedHeroClassName == heroClass.name;
            var card = AddPanel(heroClass.name, grid, selected ? Rgba(22, 30, 40, 238) : panelColor);
            AddSideAccent(card, selected ? goodColor : accent);
            AddVertical(card, 6, TextAnchor.UpperCenter, new RectOffset(10, 10, 8, 8));
            AddCharacterArt(card, heroClass.portraitName, "idle", 174, 174);
            AddText(card, heroClass.name, 24, FontStyle.Bold, accent, TextAnchor.MiddleCenter, 34);
            AddText(card, heroClass.description, 15, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 54);
            AddText(card, "HP " + heroClass.hp + "  MP " + heroClass.mp + "  ATK " + heroClass.attack + "  MAG " + heroClass.magic + "\nDEF " + heroClass.defense + "  SPD " + RoundToGameInt(BaseSpeedForClass(heroClass.name)) + "  CRIT " + RoundToGameInt(heroClass.crit * 100f) + "%", 14, FontStyle.Normal, textColor, TextAnchor.MiddleCenter, 44);
            var selectButton = AddButton(card, selected ? "선택됨" : "선택", () =>
            {
                selectedHeroClassName = localHeroClass.name;
                ShowClassSelect(activeSlot);
            }, selected ? goodColor : panelAltColor);
            AddLayoutSize(selectButton.GetComponent<RectTransform>(), -1, 52);
        }

        var selectedHeroClass = heroClasses.Find(entry => entry.name == selectedHeroClassName);
        var buttons = AddRow("Class Select Actions", page, 16, TextAnchor.MiddleCenter);
        AddLayoutSize(buttons, -1, 62);
        var startButton = AddButton(buttons, selectedHeroClass != null ? selectedHeroClass.name + "으로 시작" : "영웅을 먼저 선택하세요", StartSelectedHeroClass, goodColor);
        startButton.interactable = selectedHeroClass != null;
        AddButton(buttons, "뒤로", ShowMainMenu, panelAltColor);
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
        ShowTown("모험이 시작되었습니다.");
    }

    private void ShowDeleteSaveConfirm(int slot)
    {
        slot = Mathf.Clamp(slot, 0, SaveSlotCount - 1);
        activeSlot = slot;
        currentScreen = AetheriaScreen.MainMenu;
        ClearRoot();

        var page = AddPanel("Delete Save Confirm", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 22, TextAnchor.MiddleCenter, new RectOffset(90, 90, 80, 80));

        var panel = AddPanel("Delete Save Confirm Panel", page, Rgba(24, 10, 14, 235));
        AddSideAccent(panel, dangerColor);
        AddLayoutSize(panel, 920, 430);
        AddVertical(panel, 18, TextAnchor.MiddleCenter, new RectOffset(44, 44, 36, 36));

        AddText(panel, "저장 슬롯 삭제", 44, FontStyle.Bold, dangerColor, TextAnchor.MiddleCenter, 64);
        AddMessageBanner(panel, "슬롯 " + (slot + 1) + "의 저장 데이터를 삭제합니다.\n삭제하면 되돌릴 수 없습니다.", dangerColor, 108);
        AddText(panel, HasSave(slot) ? SavePreview(slot) : "선택한 슬롯에는 저장 데이터가 없습니다.", 20, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 86);

        var buttons = AddRow("Delete Save Buttons", panel, 14, TextAnchor.MiddleCenter);
        AddLayoutSize(buttons, -1, 68);
        var deleteButton = AddButton(buttons, "삭제", () =>
        {
            DeleteSave(slot);
            ShowMainMenu();
        }, dangerColor);
        deleteButton.interactable = HasSave(slot);
        AddButton(buttons, "취소", ShowMainMenu, panelAltColor);
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
        AddVertical(page, 14, TextAnchor.UpperCenter, new RectOffset(28, 28, 20, 20));

        var hud = AddPanel("Town HUD", page, panelColor);
        AddLayoutSize(hud, -1, 150);
        var hudLayout = hud.gameObject.AddComponent<HorizontalLayoutGroup>();
        hudLayout.spacing = 18;
        hudLayout.padding = new RectOffset(18, 18, 12, 12);
        hudLayout.childAlignment = TextAnchor.MiddleLeft;
        hudLayout.childControlWidth = true;
        hudLayout.childControlHeight = true;
        hudLayout.childForceExpandWidth = false;
        hudLayout.childForceExpandHeight = true;

        AddCharacterArt(hud, player.portraitName, townCharacterState, 120, 120);

        var hudStats = AddPanel("HUD Stats", hud, new Color(0, 0, 0, 0));
        AddLayoutSize(hudStats, 760, -1);
        AddVertical(hudStats, 4, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
        AddText(hudStats, "Lv." + player.level + "  " + player.heroName + "  /  " + player.heroClass, 28, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 34);
        AddText(hudStats, "원정 " + unlockedDungeonLabel + "   골드 " + player.gold + " G   전투력 " + EquippedPowerTotal(player), 18, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 24);
        AddBar(hudStats, player.hp, MaxHp(), dangerColor, "HP");
        AddBar(hudStats, player.mp, MaxMp(), manaColor, "MP");
        AddBar(hudStats, player.xp, XpToNext(), goodColor, "XP");

        var topActions = AddRow("Town Quick Actions", hud, 10, TextAnchor.MiddleRight);
        AddLayoutSize(topActions, -1, 64);
        AddButton(topActions, "기록 저장", () =>
        {
            SaveGame();
            ShowTown("슬롯 " + (activeSlot + 1) + "에 저장했습니다.");
        }, neonPurple);
        AddButton(topActions, "여관 25 G", Rest, manaColor);
        AddButton(topActions, "메인 메뉴", () =>
        {
            SaveGame();
            ShowMainMenu();
        }, panelAltColor);

        var body = AddRow("Town Command Deck", page, 18, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 860);

        var heroShowcase = AddPanel("Hero Showcase", body, Rgba(4, 8, 18, 230));
        AddLayoutSize(heroShowcase, 380, -1);
        AddVertical(heroShowcase, 10, TextAnchor.UpperCenter, new RectOffset(18, 18, 18, 18));
        AddText(heroShowcase, townCharacterState == "rest" ? "여관" : "원정대장", 24, FontStyle.Bold, manaColor, TextAnchor.MiddleCenter, 38);
        AddCharacterArt(heroShowcase, player.portraitName, townCharacterState, 330, 500);
        AddText(heroShowcase, player.heroName + "\n" + player.heroClass, 29, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 66);
        AddText(heroShowcase, "HP " + player.hp + "/" + MaxHp() + "   MP " + player.mp + "/" + MaxMp() + "\n" + (BasicAttackUsesMagic() ? "마법 공격형" : "물리 공격형"), 18, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 70);

        var commandBoard = AddPanel("Adventure Command Board", body, panelColor);
        AddLayoutSize(commandBoard, 870, -1);
        AddVertical(commandBoard, 12, TextAnchor.UpperCenter, new RectOffset(20, 20, 20, 20));
        AddText(commandBoard, "아에테리아 원정본부", 36, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 48);
        AddText(commandBoard, "다음 행동을 선택하세요.", 19, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 36);
        AddDivider(commandBoard, Rgba(202, 168, 92, 155));
        AddMessageBanner(commandBoard, message, manaColor, 72);

        var actionGrid = AddPanel("Town Action Grid", commandBoard, new Color(0, 0, 0, 0));
        AddLayoutSize(actionGrid, -1, 430);
        var actionLayout = actionGrid.gameObject.AddComponent<GridLayoutGroup>();
        actionLayout.cellSize = new Vector2(400, 126);
        actionLayout.spacing = new Vector2(14, 14);
        actionLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        actionLayout.constraintCount = 2;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;

        AddButton(actionGrid, "던전 탐험\n차원 지도", EnterDungeonSelectFromTown, goodColor);
        AddButton(actionGrid, "가방 · 장비\n전투 세팅", () => ShowInventory("가방과 착용 장비를 정비합니다."), panelAltColor);
        AddButton(actionGrid, "대장간\n장비 강화", () => ShowEnhancement("착용 장비를 부위별로 강화합니다."), goldColor);
        AddButton(actionGrid, "스킬 수련\n기술 강화", () => ShowSkillTraining("골드로 직업 스킬을 강화합니다."), manaColor);
        AddButton(actionGrid, "장비 조합\n상위 등급 제작", () => ShowCrafting("같은 등급 장비 3개를 다음 등급으로 조합합니다."), neonPurple);
        AddButton(actionGrid, "여관 휴식\nHP · MP 회복", Rest, dangerColor);

        AddDivider(commandBoard, Rgba(202, 168, 92, 110));
        AddText(commandBoard, StatLine() + "\n가방 " + InventoryCountLabel() + "   최근 기록 " + player.generalLogs.Count, 17, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 116);

        var fieldJournal = AddPanel("Field Journal", body, Rgba(6, 10, 20, 232));
        AddLayoutSize(fieldJournal, -1, -1);
        AddVertical(fieldJournal, 12, TextAnchor.UpperLeft, new RectOffset(20, 20, 20, 20));
        AddText(fieldJournal, "원정 기록", 30, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 42);
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
            16, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 190);
        AddDivider(fieldJournal, Rgba(111, 211, 255, 95));
        AddText(fieldJournal, "최근 소식", 22, FontStyle.Bold, neonPurple, TextAnchor.MiddleLeft, 34);
        AddText(fieldJournal, RecentLogText(), 16, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 190);
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

        var portraitPanel = AddPanel("HUD Avatar", hud, Rgba(4, 8, 18, 210));
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

        var sidebar = AddPanel("Town Sidebar", body, Rgba(4, 8, 18, 220));
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
        AddText(content, "에테리아 마을", 34, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 46);
        AddText(content, "마을에서 영웅 정보, 착용 장비, 스킬, 가방을 정비하고 차원 포탈로 진입합니다.", 20, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 34);

        var statusAndGear = AddRow("Status And Gear", content, 14, TextAnchor.UpperCenter);
        AddLayoutSize(statusAndGear, -1, 350);

        var stats = AddPanel("Hero Stats Card", statusAndGear, Rgba(10, 14, 24, 225));
        AddLayoutSize(stats, 520, -1);
        AddVertical(stats, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));
        AddText(stats, "영웅 능력치", 25, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 34);
        AddText(stats, StatLine(), 19, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 52);
        AddText(stats, "체력 " + player.hp + "/" + MaxHp() + "\n마나 " + player.mp + "/" + MaxMp() + "\n기본 공격 타입 " + (BasicAttackUsesMagic() ? "마법" : "물리") + "\n상태 피해는 지속 피해와 마나 연소량에 반영됩니다.", 20, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 160);

        var equipment = AddPanel("Equipment Card", statusAndGear, Rgba(41, 24, 64, 225));
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

        var skills = AddPanel("Skill Section", lower, Rgba(10, 14, 24, 225));
        AddLayoutSize(skills, 560, -1);
        AddVertical(skills, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));
        AddText(skills, "스킬 강화", 24, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 34);
        foreach (var skill in ScaledSkillsForPlayer())
        {
            AddText(skills, skill.name + "  MP " + skill.mpCost + "  위력 " + RoundToGameInt(skill.multiplier * 100) + "%", 18, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 27);
        }

        var log = AddPanel("Town Event Log", lower, Rgba(4, 8, 18, 220));
        AddLayoutSize(log, -1, -1);
        AddVertical(log, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));
        AddText(log, "영웅 소식", 24, FontStyle.Bold, neonPurple, TextAnchor.MiddleLeft, 34);
        AddGeneralLog(message);
        SaveGame();
        AddText(log, message + "\n\n" + RecentLogText() + "\n\n" + BuildStatusText(), 18, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 200);
    }

    private void ResetCombatState(bool clearDungeon)
    {
        actionLocked = false;
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
        AddVertical(page, 18, TextAnchor.UpperCenter, new RectOffset(44, 44, 34, 34));

        var top = AddRow("Inventory Top", page, 16, TextAnchor.MiddleLeft);
        AddLayoutSize(top, -1, 78);
        AddText(top, "가방", 42, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 420);
        AddText(top, "골드 " + player.gold + "  아이템 " + InventoryCountLabel(), 24, FontStyle.Normal, goldColor, TextAnchor.MiddleLeft, 300);
        AddButton(top, "마을로", () => ShowTown("마을로 돌아왔습니다."), panelAltColor);

        var sortRow = AddRow("Inventory Sort Row", page, 10, TextAnchor.MiddleLeft);
        AddLayoutSize(sortRow, -1, 56);
        AddText(sortRow, "정렬", 20, FontStyle.Bold, mutedColor, TextAnchor.MiddleLeft, 92);
        AddInventorySortButton(sortRow, "전투력", InventorySortMode.Power);
        AddInventorySortButton(sortRow, "희귀도", InventorySortMode.Rarity);
        AddInventorySortButton(sortRow, "부위", InventorySortMode.Type);

        var body = AddRow("Inventory Body", page, 18, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 800);

        var list = AddScrollList("Item List", body, panelColor, 930, -1, 10, new RectOffset(18, 18, 18, 18));

        if (player.inventory.Count == 0)
        {
            AddText(list, "아직 아이템이 없습니다. 던전을 클리어해 장비를 획득하세요.", 24, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 80);
        }
        else
        {
            var orderedItems = SortedInventoryEntries();
            for (var i = 0; i < orderedItems.Count; i++)
            {
                var index = orderedItems[i].index;
                var item = orderedItems[i].item;
                var button = AddButton(list, ItemListButtonLabel(item), () =>
                {
                    selectedInventoryIndex = index;
                    ShowInventory(item.name + "을(를) 선택했습니다.");
                }, selectedInventoryIndex == index ? goldColor : panelAltColor);
                AddLayoutSize(button.GetComponent<RectTransform>(), -1, 72);
            }
        }

        var detail = AddPanel("Item Detail", body, panelColor);
        AddLayoutSize(detail, -1, -1);
        AddVertical(detail, 12, TextAnchor.UpperLeft, new RectOffset(24, 24, 24, 24));
        AddText(detail, "상세 정보", 32, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 48);
        AddMessageBanner(detail, message, Rgb(178, 148, 102), 60);

        if (selectedInventoryIndex >= 0 && selectedInventoryIndex < player.inventory.Count)
        {
            var item = player.inventory[selectedInventoryIndex];
            var comparison = AddRow("Selected Item Comparison", detail, 12, TextAnchor.UpperCenter);
            AddLayoutSize(comparison, -1, 320);
            AddItemComparisonPanel(comparison, "선택 아이템", SelectedItemDetailText(item), RarityColor(item.rarity));
            AddItemComparisonPanel(comparison, "교체 대상", ReplacementTargetDetailText(item), textColor);
            AddColoredGearDeltaPanel(detail, item, EquippedItemForReplacement(item.type, item.type == "Charm" ? 1 : 0));

            if (item.type == "Charm")
            {
                var slotRow = AddRow("Accessory Equip Slots", detail, 10, TextAnchor.MiddleLeft);
                AddLayoutSize(slotRow, -1, 68);
                var slot1 = AddButton(slotRow, AccessorySlotButtonLabel(item, 1, player.charm), () => EquipItemToSlot(selectedInventoryIndex, 1), goodColor);
                var slot2 = AddButton(slotRow, AccessorySlotButtonLabel(item, 2, player.charm2), () => EquipItemToSlot(selectedInventoryIndex, 2), goodColor);
                var slot3 = AddButton(slotRow, AccessorySlotButtonLabel(item, 3, player.charm3), () => EquipItemToSlot(selectedInventoryIndex, 3), goodColor);
                var slot4 = AddButton(slotRow, AccessorySlotButtonLabel(item, 4, player.charm4), () => EquipItemToSlot(selectedInventoryIndex, 4), goodColor);
                AddLayoutSize(slot1.GetComponent<RectTransform>(), -1, 68);
                AddLayoutSize(slot2.GetComponent<RectTransform>(), -1, 68);
                AddLayoutSize(slot3.GetComponent<RectTransform>(), -1, 68);
                AddLayoutSize(slot4.GetComponent<RectTransform>(), -1, 68);
            }
            else
            {
                var equipButton = AddButton(detail, "착용 " + GearPowerDeltaText(item, EquippedItemForReplacement(item.type, 0)), () => EquipItem(selectedInventoryIndex), goodColor);
                AddLayoutSize(equipButton.GetComponent<RectTransform>(), -1, 58);
            }

            var actionRow = AddRow("Selected Item Actions", detail, 12, TextAnchor.MiddleLeft);
            AddLayoutSize(actionRow, -1, 58);
            AddButton(actionRow, "강화소로", () =>
            {
                ShowEnhancement("착용 중인 장비만 부위별로 강화할 수 있습니다.");
            }, goldColor);
            AddButton(actionRow, "판매", () =>
            {
                var sellMessage = SellItem(selectedInventoryIndex);
                selectedInventoryIndex = -1;
                ShowInventory(sellMessage);
            }, dangerColor);
        }
        else
        {
            AddText(detail, "왼쪽 가방 목록에서 아이템을 선택하면 현재 착용 장비와 비교됩니다.", 21, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 72);
        }

        var equipped = AddPanel("Equipped Detail", detail, panelAltColor);
        AddLayoutSize(equipped, -1, 240);
        AddVertical(equipped, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 18, 18));
        AddText(equipped, "전체 착용 요약", 25, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 36);
        AddText(equipped, EquipmentLine("무기", player.weapon), 20, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 30);
        AddText(equipped, EquipmentLine("방어구", player.armor), 20, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 30);
        AddText(equipped, EquipmentLine("장신구 1", player.charm), 19, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 28);
        AddText(equipped, EquipmentLine("장신구 2", player.charm2), 19, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 28);
        AddText(equipped, EquipmentLine("장신구 3", player.charm3), 19, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 28);
        AddText(equipped, EquipmentLine("장신구 4", player.charm4), 19, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 28);
        var unequipRow = AddRow("Unequip Row", equipped, 8, TextAnchor.MiddleLeft);
        AddLayoutSize(unequipRow, -1, 48);
        AddFlexibleButton(unequipRow, "무기 해제", () => UnequipItem("Weapon"), panelColor, 48);
        AddFlexibleButton(unequipRow, "방어구 해제", () => UnequipItem("Armor"), panelColor, 48);
        AddFlexibleButton(unequipRow, "장신구 1", () => UnequipItem("Charm1"), panelColor, 48);
        AddFlexibleButton(unequipRow, "장신구 2", () => UnequipItem("Charm2"), panelColor, 48);
        AddFlexibleButton(unequipRow, "장신구 3", () => UnequipItem("Charm3"), panelColor, 48);
        AddFlexibleButton(unequipRow, "장신구 4", () => UnequipItem("Charm4"), panelColor, 48);
    }

    private void AddItemComparisonPanel(Transform parent, string title, string value, Color titleColor)
    {
        var panel = AddPanel("Item Comparison " + title, parent, Rgba(4, 8, 18, 210));
        AddSideAccent(panel, titleColor);
        AddLayoutSize(panel, -1, -1);
        AddVertical(panel, 8, TextAnchor.UpperLeft, new RectOffset(16, 16, 14, 14));
        AddText(panel, title, 22, FontStyle.Bold, titleColor, TextAnchor.MiddleLeft, 30);
        AddText(panel, value, 16, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 270);
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

        var panel = AddPanel("Colored Gear Delta", parent, Rgba(4, 8, 18, 186));
        AddLayoutSize(panel, -1, 74);
        var layout = panel.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(116, 30);
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
        AddSideAccent(chip, delta > 0 ? goodColor : dangerColor);
        AddText(chip, label + " " + SignedInt(delta) + (percent ? "%" : ""), 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 28);
    }

    private void ShowEnhancement(string message)
    {
        currentScreen = AetheriaScreen.Enhancement;
        EnsurePlayerData();
        SaveGame();
        ClearRoot();

        var page = AddPanel("Enhancement", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 18, TextAnchor.UpperCenter, new RectOffset(44, 44, 34, 34));

        var top = AddRow("Enhancement Top", page, 16, TextAnchor.MiddleLeft);
        AddLayoutSize(top, -1, 82);
        AddText(top, "심연의 대장간", 42, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 380);
        AddText(top, "골드 " + player.gold + "  등급과 강화 단계에 따라 비용이 증가합니다.", 23, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 620);
        AddButton(top, "마을로", () => ShowTown("강화소를 나왔습니다."), panelAltColor);

        AddMessageBanner(page, message, goldColor, 54);

        var grid = AddPanel("Enhancement Grid", page, pageColor);
        AddLayoutSize(grid, -1, 790);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(560, 285);
        layout.spacing = new Vector2(18, 18);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.childAlignment = TextAnchor.UpperCenter;

        AddEnhancementCard(grid, "무기", player.weapon, "공격 성장");
        AddEnhancementCard(grid, "방어구", player.armor, "체력/방어 성장");
        AddEnhancementCard(grid, "장신구 1", player.charm, "마나/마력 성장");
        AddEnhancementCard(grid, "장신구 2", player.charm2, "마나/마력 성장");
        AddEnhancementCard(grid, "장신구 3", player.charm3, "마나/마력 성장");
        AddEnhancementCard(grid, "장신구 4", player.charm4, "마나/마력 성장");
    }

    private void ShowSkillTraining(string message)
    {
        currentScreen = AetheriaScreen.SkillTraining;
        EnsurePlayerData();
        SaveGame();
        ClearRoot();

        var page = AddPanel("Skill Training", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 14, TextAnchor.UpperCenter, new RectOffset(44, 44, 24, 24));

        var top = AddPanel("Skill Training Top", page, panelColor);
        AddLayoutSize(top, -1, 118);
        var topLayout = top.gameObject.AddComponent<HorizontalLayoutGroup>();
        topLayout.spacing = 16;
        topLayout.padding = new RectOffset(16, 16, 10, 10);
        topLayout.childAlignment = TextAnchor.MiddleLeft;
        topLayout.childControlWidth = true;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = true;
        AddCharacterArt(top, player.portraitName, "skill", 96, 96);
        var trainingTitle = AddPanel("Skill Training Heading", top, new Color(0, 0, 0, 0));
        AddLayoutSize(trainingTitle, -1, -1);
        AddVertical(trainingTitle, 3, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
        AddText(trainingTitle, "스킬 강화", 38, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 48);
        AddText(trainingTitle, "골드 " + player.gold + " G   직업 스킬의 위력과 효과를 수련합니다.", 20, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 34);
        AddButton(top, "마을로", () => ShowTown("훈련장을 나왔습니다."), panelAltColor);

        AddMessageBanner(page, message, manaColor, 54);

        var grid = AddPanel("Skill Training Grid", page, new Color(0, 0, 0, 0));
        AddLayoutSize(grid, -1, 784);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(560, 295);
        layout.spacing = new Vector2(18, 18);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.childAlignment = TextAnchor.UpperCenter;

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
        var card = AddPanel("Skill " + baseSkill.name, parent, panelColor);
        AddSideAccent(card, manaColor);
        AddVertical(card, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));

        AddText(card, baseSkill.name + "  Lv." + level, 25, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 34);
        AddText(card, "MP " + skill.mpCost + "  위력 " + RoundToGameInt(skill.multiplier * 100) + "%  →  " + RoundToGameInt(nextSkill.multiplier * 100) + "%", 19, FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 34);
        AddText(card, SkillTrainingEffectLine(skill, nextSkill), 17, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 98);
        var button = AddButton(card, capped ? "최대 레벨" : "강화 " + cost + "G", () => UpgradeSkill(baseSkill.name), goldColor);
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
        AddVertical(page, 18, TextAnchor.UpperCenter, new RectOffset(44, 44, 34, 34));

        var top = AddRow("Crafting Top", page, 16, TextAnchor.MiddleLeft);
        AddLayoutSize(top, -1, 82);
        AddText(top, "아이템 조합", 42, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 380);
        AddText(top, "골드 " + player.gold + "  같은 등급 장비 3개를 다음 등급으로 조합합니다.", 23, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 620);
        AddButton(top, "마을로", () => ShowTown("조합소를 나왔습니다."), panelAltColor);

        var body = AddRow("Crafting Body", page, 18, TextAnchor.UpperCenter);
        AddLayoutSize(body, -1, 840);

        var list = AddScrollList("Crafting Inventory", body, panelColor, 980, -1, 10, new RectOffset(18, 18, 18, 18));
        AddText(list, "가방", 28, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 40);

        if (player.inventory.Count == 0)
        {
            AddText(list, "조합할 장비가 없습니다. 던전에서 장비를 획득하세요.", 22, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 80);
        }
        else
        {
            for (var i = 0; i < player.inventory.Count; i++)
            {
                var item = player.inventory[i];
                AddText(list, ItemLabel(item), 18, FontStyle.Normal, RarityColor(item.rarity), TextAnchor.MiddleLeft, 32);
            }
        }

        var detail = AddPanel("Crafting Detail", body, panelColor);
        AddLayoutSize(detail, -1, -1);
        AddVertical(detail, 16, TextAnchor.UpperLeft, new RectOffset(24, 24, 24, 24));
        AddText(detail, "등급 조합 레시피", 32, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 48);
        AddMessageBanner(detail, message, neonPurple, 64);
        AddText(detail, "네임드가 아닌 같은 등급 장비 3개를 소모해 다음 등급 장비 1개를 만듭니다.", 20, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 48);
        var recipeGrid = AddPanel("Crafting Recipe Grid", detail, new Color(0, 0, 0, 0));
        AddLayoutSize(recipeGrid, -1, 360);
        var recipeLayout = recipeGrid.gameObject.AddComponent<GridLayoutGroup>();
        recipeLayout.cellSize = new Vector2(210, 58);
        recipeLayout.spacing = new Vector2(10, 10);
        recipeLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        recipeLayout.constraintCount = 3;
        recipeLayout.childAlignment = TextAnchor.UpperLeft;
        for (var rarity = 0; rarity <= MaxRecipeIngredientRarity; rarity++)
        {
            var localRarity = rarity;
            var count = CountCraftIngredients(localRarity);
            var cost = RecipeCraftCost(localRarity);
            var button = AddButton(recipeGrid, RarityLabel(localRarity) + " " + count + "/3\n" + cost + "G", () => CraftByRecipe(localRarity), RarityColor(localRarity + 1));
            button.interactable = count >= 3 && player.gold >= cost;
        }
    }

    private void AddEnhancementCard(Transform parent, string slotLabel, ItemState item, string role)
    {
        var card = AddPanel(slotLabel, parent, panelColor);
        AddSideAccent(card, item == null ? mutedColor : RarityColor(item.rarity));
        AddVertical(card, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));

        AddText(card, slotLabel, 25, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 34);
        AddText(card, item == null ? "장착된 장비 없음" : ItemLabel(item), 20, FontStyle.Bold, item == null ? mutedColor : RarityColor(item.rarity), TextAnchor.MiddleLeft, 42);
        var cost = item == null ? 0 : EnhancementCost(item);
        var nextPower = item == null ? 0 : PreviewEnhancedPower(item);
        var detail = item == null
            ? "가방에서 해당 부위 장비를 먼저 착용하세요."
            : role + "\n현재 전투력 +" + item.power + (item.level >= MaxGearEnhancementLevel ? " / 최대 강화" : " -> 강화 후 +" + nextPower) + "\n" + GearStatsDescription(item) + "\n" + (item.level >= MaxGearEnhancementLevel ? "더 이상 강화할 수 없습니다." : "비용 " + cost + "G / 성공률 " + RoundToGameInt(GearEnhancementSuccessChance * 100f) + "%");
        AddText(card, detail, 17, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 92);

        var button = AddButton(card, item != null && item.level >= MaxGearEnhancementLevel ? "최대 강화" : "이 부위 강화", () => EnhanceEquippedItem(slotLabel, item), goldColor);
        button.interactable = item != null && item.level < MaxGearEnhancementLevel && player.gold >= cost;
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

        var mapPanel = AddPanel("Dungeon World Map Panel", portalPage, Rgba(2, 6, 23, 245));
        Stretch(mapPanel, 0, 0, 0, 0);
        mapPanel.gameObject.AddComponent<RectMask2D>();

        var mapImage = AddPanel("Dungeon World Map Image", mapPanel, Rgb(3, 9, 18));
        ConfigureDungeonMapImage(mapImage);
        TryApplyStreamingSprite(mapImage, DungeonMapSpritePath);
        ApplyDungeonMapView(mapImage);
        ConfigureDungeonMapInput(mapPanel, mapImage);

        AddDungeonRegionButtons(mapImage, portalDungeons);
        if (DungeonMapPinsVisible())
        {
            AddDungeonPins(mapImage, portalDungeons);
        }

        AddDungeonMapChrome(portalPage);
        AddDungeonMapTopOverlay(portalPage, currentDungeonLabel, true);
        var pinsVisible = DungeonMapPinsVisible();
        var statusTitle = pinsVisible ? "던전 핀 선택: 정보창 표시" : dungeonMapZoomed ? "지도 확대 중: 휠로 더 확대하면 던전 표시" : "대륙 선택: 확대 후 던전 표시";
        var statusDetail = pinsVisible ? (FindDungeonMapRegion(selectedDungeonRegionKey)?.name ?? "미지의 대륙") : dungeonMapZoomed ? "마우스 드래그로 이동 / 휠로 확대, 축소" : "지도 버튼 위 UI는 클릭을 통과합니다.";
        AddDungeonMapStatusOverlay(portalPage, statusTitle, statusDetail);
    }

    private void EnterDungeonSelectFromTown()
    {
        ResetDungeonMapView();
        dungeonMapScrollOpened = false;
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
        AddMapEdgeBand(portalPage, "Map Shadow Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 126f), Rgba(0, 0, 0, 106));
        AddMapEdgeBand(portalPage, "Map Shadow Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 126f), Rgba(0, 0, 0, 112));
        AddMapEdgeBand(portalPage, "Map Shadow Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(110f, 0f), Rgba(0, 0, 0, 88));
        AddMapEdgeBand(portalPage, "Map Shadow Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(110f, 0f), Rgba(0, 0, 0, 88));

        AddMapEdgeBand(portalPage, "Map Gold Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(0f, 3f), Rgba(245, 158, 11, 90));
        AddMapEdgeBand(portalPage, "Map Gold Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(0f, 3f), Rgba(245, 158, 11, 82));
        AddMapEdgeBand(portalPage, "Map Gold Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(3f, 0f), Rgba(245, 158, 11, 70));
        AddMapEdgeBand(portalPage, "Map Gold Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-7f, 0f), new Vector2(3f, 0f), Rgba(245, 158, 11, 70));
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
        portalTop.GetComponent<Image>().raycastTarget = false;
        portalTop.anchorMin = new Vector2(1f, 1f);
        portalTop.anchorMax = new Vector2(1f, 1f);
        portalTop.pivot = new Vector2(1f, 1f);
        portalTop.anchoredPosition = new Vector2(-14f, -14f);
        portalTop.sizeDelta = new Vector2(includeResetButton ? 430f : 330f, 56f);
        var portalTopLayout = portalTop.GetComponent<HorizontalLayoutGroup>();
        portalTopLayout.padding = new RectOffset(10, 10, 7, 7);
        portalTopLayout.childForceExpandWidth = false;
        portalTopLayout.childForceExpandHeight = true;
        AddText(portalTop, "지도 v18", 25, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 42);
        AddText(portalTop, currentDungeonLabel, 16, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 42);

        if (includeResetButton)
        {
            var resetButton = AddButton(portalTop, "100%", () =>
            {
                ResetDungeonMapView();
                ShowDungeonSelect();
            }, panelAltColor);
            AddLayoutSize(resetButton.GetComponent<RectTransform>(), 86, 44);
        }

        var townButton = AddButton(portalTop, "마을로", () => ShowTown("마을로 돌아왔습니다."), panelAltColor);
        AddLayoutSize(townButton.GetComponent<RectTransform>(), 112, 44);
    }

    private void AddDungeonMapStatusOverlay(RectTransform portalPage, string title, string detail)
    {
        var statusPanel = AddPanel("Dungeon Map Status Overlay", portalPage, Rgba(4, 8, 18, 205));
        statusPanel.GetComponent<Image>().raycastTarget = false;
        statusPanel.anchorMin = new Vector2(0f, 0f);
        statusPanel.anchorMax = new Vector2(0f, 0f);
        statusPanel.pivot = new Vector2(0f, 0f);
        statusPanel.anchoredPosition = new Vector2(14f, 14f);
        statusPanel.sizeDelta = new Vector2(520f, 50f);
        AddVertical(statusPanel, 1, TextAnchor.MiddleCenter, new RectOffset(14, 14, 7, 7));
        AddText(statusPanel, title, 18, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 26);
        AddText(statusPanel, detail, 14, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 18);
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

        foreach (var region in DungeonMapRegions())
        {
            var localRegion = region;
            var regionDungeons = DungeonsInRegion(dungeons, localRegion.key);
            var button = AddMapRegionButton(mapParent, localRegion, regionDungeons.Count, () =>
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
                var point = DungeonMapPointForDungeon(region, dungeon, i);
                var color = bossCleared ? goodColor : unlocked ? manaColor : mutedColor;
                var localDungeon = dungeon;
                var localPoint = point;
                var button = AddDungeonPinButton(mapParent, dungeon, point, color, unlocked, bossCleared, () =>
                {
                    ShowDungeonInfoPopup(mapParent, localDungeon, localPoint);
                });
                button.interactable = HasDungeonEncounter(dungeon);
            }
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
        var region = FindDungeonMapRegion(DungeonRegionKey(dungeon));
        var statusColor = bossCleared ? goodColor : unlocked ? manaColor : mutedColor;
        var statusText = bossCleared ? "토벌 완료" : unlocked ? "진입 가능" : "잠금";
        if (underLevel && unlocked)
        {
            statusText += " / 위험";
            statusColor = goldColor;
        }

        var showRight = percent.x < 58f;
        var verticalPivot = percent.y > 72f ? 0f : percent.y < 28f ? 1f : 0.5f;
        var verticalOffset = percent.y > 72f ? 30f : percent.y < 28f ? -30f : 0f;
        var zoom = Mathf.Max(0.01f, mapParent.localScale.x);
        var window = AddPanel("Dungeon Info Popup", mapParent, Rgba(10, 14, 24, DungeonInfoPopupAlpha));
        AddSideAccent(window, statusColor);
        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = window.anchorMin;
        window.pivot = new Vector2(showRight ? 0f : 1f, verticalPivot);
        window.anchoredPosition = DungeonMapLocalPoint(mapParent, percent) + new Vector2((showRight ? 42f : -42f) / zoom, verticalOffset / zoom);
        window.sizeDelta = new Vector2(620f, 680f);
        window.localScale = new Vector3(1f / zoom, 1f / zoom, 1f);
        window.anchoredPosition = ClampDungeonInfoPopupPosition(mapParent, window.anchoredPosition, window.sizeDelta, window.pivot, zoom);
        var popupLayout = window.GetComponent<LayoutElement>();
        if (popupLayout != null)
        {
            popupLayout.ignoreLayout = true;
        }
        AddVertical(window, 9, TextAnchor.UpperLeft, new RectOffset(22, 22, 18, 18));

        var titleRow = AddRow("Dungeon Popup Title", window, 14, TextAnchor.MiddleLeft);
        AddLayoutSize(titleRow, -1, 54);
        AddText(titleRow, dungeon.number + ". " + dungeon.name, 26, FontStyle.Bold, unlocked ? goldColor : mutedColor, TextAnchor.MiddleLeft, 46);
        AddText(titleRow, statusText, 18, FontStyle.Bold, statusColor, TextAnchor.MiddleRight, 46);

        AddText(window, (region != null ? region.name : "미지의 대륙") + " / 권장 Lv." + dungeon.recommendedLevel + " / 구역 " + DungeonFloorCount(dungeon), 18, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 32);
        AddText(window, DungeonRecommendedPowerText(dungeon), 16, FontStyle.Bold, underLevel ? goldColor : textColor, TextAnchor.UpperLeft, 50);
        AddText(window, dungeon.description, 17, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 68);
        AddDivider(window, statusColor);
        AddText(window, DungeonLootRarityText(dungeon), 16, FontStyle.Bold, RarityColor(MaxRarityForDungeon(dungeon.number)), TextAnchor.UpperLeft, 72);
        AddText(window, DungeonRewardAndDangerText(dungeon), 16, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 106);
        AddDivider(window, Rgba(148, 163, 184, 150));
        AddText(window, "일반 적: " + DungeonMonsterSummary(dungeon) + "\n보스: " + DungeonBossName(dungeon), 16, FontStyle.Normal, mutedColor, TextAnchor.UpperLeft, 52);
        AddText(window, "영웅 Lv." + player.level + "  HP " + player.hp + "/" + MaxHp() + "  MP " + player.mp + "/" + MaxMp() + "\n공격 " + Attack() + " / 마력 " + Magic() + " / 방어 " + Defense() + " / 속도 " + Speed(), 15, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 54);

        var buttons = AddRow("Dungeon Popup Buttons", window, 16, TextAnchor.MiddleRight);
        AddLayoutSize(buttons, -1, 56);
        var closeButton = AddButton(buttons, "닫기", () => Destroy(window.gameObject), panelAltColor);
        AddLayoutSize(closeButton.GetComponent<RectTransform>(), 118, 48);
        var enterButton = AddButton(buttons, "던전 진입", () => StartCombat(dungeon, false), goodColor);
        AddLayoutSize(enterButton.GetComponent<RectTransform>(), 150, 48);
        enterButton.interactable = unlocked && HasDungeonEncounter(dungeon);
    }

    private Vector2 ClampDungeonInfoPopupPosition(RectTransform mapParent, Vector2 position, Vector2 popupSize, Vector2 pivot, float zoom)
    {
        var contentSize = DungeonMapContentSize(mapParent);
        var effectiveZoom = Mathf.Max(0.01f, zoom);
        var margin = 20f / effectiveZoom;
        var effectiveSize = popupSize / effectiveZoom;
        var minX = -contentSize.x * 0.5f + margin + effectiveSize.x * pivot.x;
        var maxX = contentSize.x * 0.5f - margin - effectiveSize.x * (1f - pivot.x);
        var minY = -contentSize.y * 0.5f + margin + effectiveSize.y * pivot.y;
        var maxY = contentSize.y * 0.5f - margin - effectiveSize.y * (1f - pivot.y);

        if (minX > maxX)
        {
            position.x = 0f;
        }
        else
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }

        if (minY > maxY)
        {
            position.y = 0f;
        }
        else
        {
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        return position;
    }

    private void ClearDungeonInfoPopups(RectTransform mapParent)
    {
        for (var i = mapParent.childCount - 1; i >= 0; i--)
        {
            var child = mapParent.GetChild(i);
            if (child != null && child.name == "Dungeon Info Popup")
            {
                Destroy(child.gameObject);
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
        var rowColor = bossCleared ? Rgba(18, 45, 35, 230) : unlocked ? panelColor : Rgba(28, 28, 36, 230);
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

    private Button AddMapRegionButton(RectTransform parent, DungeonMapRegion region, int dungeonCount, Action onClick)
    {
        var hit = CreateMapButtonObject("Region Hit " + region.key, parent, new Vector2(region.center.x, region.center.y), new Vector2(DungeonMapBaseWidth * 0.18f, DungeonMapBaseHeight * 0.15f), onClick);
        hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);
        hit.interactable = dungeonCount > 0;

        var button = CreateMapButtonObject("Region " + region.key, parent, new Vector2(region.center.x, region.center.y), new Vector2(190f, 52f), onClick);
        var image = button.GetComponent<Image>();
        image.sprite = MapRoundedRectSprite();
        image.color = Rgba(48, 27, 8, 194);
        var outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = Rgba(245, 190, 92, 210);
        outline.effectDistance = new Vector2(1.9f, -1.9f);

        var shine = AddFlatPanel("Region Button Shine", button.transform, Rgba(255, 230, 150, 34));
        shine.GetComponent<Image>().raycastTarget = false;
        shine.anchorMin = new Vector2(0f, 0.54f);
        shine.anchorMax = new Vector2(1f, 1f);
        shine.offsetMin = new Vector2(10f, 0f);
        shine.offsetMax = new Vector2(-10f, -5f);

        var text = AddText(button.transform, region.name, 20, FontStyle.Bold, Rgb(255, 236, 179), TextAnchor.MiddleCenter, 46);
        text.raycastTarget = false;
        Stretch(text.GetComponent<RectTransform>(), 10, 0, 10, 0);

        var count = AddText(button.transform, dungeonCount + "개", 12, FontStyle.Bold, Rgb(250, 204, 21), TextAnchor.UpperRight, 18);
        count.raycastTarget = false;
        var countRect = count.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1f, 1f);
        countRect.anchorMax = new Vector2(1f, 1f);
        countRect.pivot = new Vector2(1f, 1f);
        countRect.anchoredPosition = new Vector2(-8f, -4f);
        countRect.sizeDelta = new Vector2(44f, 18f);
        button.interactable = dungeonCount > 0;
        return button;
    }

    private Button AddDungeonPinButton(RectTransform parent, DungeonData dungeon, Vector2 percent, Color color, bool unlocked, bool bossCleared, Action onClick)
    {
        AddMapGlow(parent, "Dungeon Pin Glow " + dungeon.number, percent, bossCleared ? goodColor : unlocked ? goldColor : mutedColor, unlocked ? 0.22f : 0.08f);
        var hit = CreateMapButtonObject("Dungeon Pin Hit " + dungeon.number, parent, percent, new Vector2(98f, 98f), onClick);
        ApplyDungeonMapCounterScale(hit.GetComponent<RectTransform>(), parent);
        hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

        var button = CreateMapButtonObject("Dungeon Pin " + dungeon.number, parent, percent, new Vector2(50f, 50f), onClick);
        ApplyDungeonMapCounterScale(button.GetComponent<RectTransform>(), parent);
        var image = button.GetComponent<Image>();
        image.sprite = MapCircleSprite();
        image.color = unlocked ? Rgba(12, 8, 5, 236) : Rgba(24, 22, 20, 178);
        var outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = bossCleared ? goodColor : unlocked ? Rgba(245, 190, 92, 236) : color;
        outline.effectDistance = new Vector2(2.6f, -2.6f);

        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.disabledColor = Rgba(30, 41, 59, 180);
        button.colors = colors;

        var inner = AddFlatPanel("Pin Inner Light " + dungeon.number, button.transform, new Color(color.r, color.g, color.b, unlocked ? 0.34f : 0.12f));
        inner.GetComponent<Image>().sprite = MapCircleSprite();
        inner.GetComponent<Image>().raycastTarget = false;
        Stretch(inner, 8, 8, 8, 8);

        var number = AddText(button.transform, dungeon.number.ToString(), 18, FontStyle.Bold, unlocked ? Rgb(255, 247, 210) : Rgb(203, 213, 225), TextAnchor.MiddleCenter, 44);
        number.raycastTarget = false;
        Stretch(number.GetComponent<RectTransform>(), 0, 0, 0, 0);

        var label = AddPanel("Pin Label " + dungeon.number, button.transform, Rgba(20, 12, 5, 226));
        ConfigureDungeonPinLabel(label, percent);
        var labelImage = label.GetComponent<Image>();
        labelImage.sprite = MapRoundedRectSprite();
        labelImage.raycastTarget = false;
        var labelOutline = label.gameObject.AddComponent<Outline>();
        labelOutline.effectColor = unlocked ? Rgba(245, 190, 92, 170) : Rgba(148, 163, 184, 100);
        labelOutline.effectDistance = new Vector2(1.2f, -1.2f);
        var labelText = AddText(label, dungeon.number + ". " + dungeon.name, 14, FontStyle.Bold, unlocked ? Rgb(255, 247, 210) : mutedColor, TextAnchor.MiddleCenter, 40);
        labelText.raycastTarget = false;
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 10;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Truncate;
        Stretch(labelText.GetComponent<RectTransform>(), 10, 0, 10, 0);
        return button;
    }

    private void ConfigureDungeonPinLabel(RectTransform label, Vector2 percent)
    {
        label.sizeDelta = new Vector2(248f, 42f);

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

    private void AddMapGlow(RectTransform parent, string name, Vector2 percent, Color color, float alpha)
    {
        var glowObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        glowObject.transform.SetParent(parent, false);
        var glow = glowObject.GetComponent<RectTransform>();
        glow.anchorMin = new Vector2(0.5f, 0.5f);
        glow.anchorMax = glow.anchorMin;
        glow.pivot = new Vector2(0.5f, 0.5f);
        glow.anchoredPosition = DungeonMapLocalPoint(parent, percent);
        glow.sizeDelta = new Vector2(58f, 58f);
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
        mapRoundedRectSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
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
        var row = AddPanel("Dungeon Row " + dungeon.number, parent, bossCleared ? Rgba(18, 45, 35, 230) : (unlocked ? panelColor : Rgba(28, 28, 36, 230)));
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

        var enter = AddButton(actions, "던전 진입", () => StartCombat(dungeon, false), goodColor);
        enter.interactable = unlocked && HasDungeonEncounter(dungeon);
    }

    private void AddDungeonCard(Transform parent, DungeonData dungeon)
    {
        var unlocked = IsDungeonUnlocked(dungeon);
        var underLevel = player.level < dungeon.recommendedLevel;
        var card = AddPanel(dungeon.name, parent, unlocked ? panelColor : Rgba(28, 28, 36, 210));
        AddVertical(card, 8, TextAnchor.UpperLeft, new RectOffset(18, 18, 16, 16));

        AddText(card, dungeon.name, 25, FontStyle.Bold, unlocked ? goldColor : mutedColor, TextAnchor.MiddleLeft, 34);
        AddText(card, "권장 Lv." + dungeon.recommendedLevel + "  구역 " + DungeonFloorCount(dungeon) + (underLevel ? "  위험" : "") + "\n" + dungeon.description, 18, FontStyle.Normal, underLevel ? goldColor : mutedColor, TextAnchor.UpperLeft, 86);
        AddText(card, "일반 적: " + DungeonMonsterSummary(dungeon) + "\n보스: " + DungeonBossName(dungeon), 17, FontStyle.Normal, textColor, TextAnchor.UpperLeft, 54);

        var buttons = AddRow("Dungeon Buttons", card, 10, TextAnchor.MiddleLeft);
        AddLayoutSize(buttons, -1, 56);
        var enter = AddButton(buttons, "던전 진입", () => StartCombat(dungeon, false), goodColor);
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
        battleLog.Add(actionLocked
            ? "기습! 적의 속도(" + enemySpeed + ")가 플레이어(" + playerSpeed + ")보다 빠릅니다."
            : "선공 획득! 플레이어의 속도(" + playerSpeed + ")가 적(" + enemySpeed + ")보다 빠릅니다.");
        ShowCombat();
        if (actionLocked)
        {
            StartCoroutine(EnemyTurn());
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

        var page = AddPanel("Combat", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 14, TextAnchor.UpperCenter, new RectOffset(34, 34, 24, 24));

        var header = AddPanel("Combat Header", page, panelColor);
        AddLayoutSize(header, -1, 72);
        var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerRow.spacing = 18;
        headerRow.padding = new RectOffset(18, 18, 10, 10);
        headerRow.childAlignment = TextAnchor.MiddleCenter;
        headerRow.childControlWidth = true;
        headerRow.childControlHeight = true;
        headerRow.childForceExpandWidth = false;
        AddText(header, (currentDungeon != null ? currentDungeon.name + " " + currentDungeonFloor + "/" + DungeonFloorCount(currentDungeon) + "층" : "던전") + " - " + (currentEnemyIsBoss ? "보스전" : "일반 전투"), 30, FontStyle.Bold, goldColor, TextAnchor.MiddleLeft, 52);
        AddText(header, actionLocked ? "적 턴" : "아군 턴", 24, FontStyle.Bold, actionLocked ? dangerColor : manaColor, TextAnchor.MiddleCenter, 52);
        AddButton(header, "던전 나가기", ExitDungeon, dangerColor).interactable = !actionLocked && !resultBackdrop;

        var arena = AddPanel("Combat Arena", page, Rgba(4, 8, 18, 215));
        AddLayoutSize(arena, -1, 475);
        var arenaRow = arena.gameObject.AddComponent<HorizontalLayoutGroup>();
        arenaRow.spacing = 24;
        arenaRow.padding = new RectOffset(28, 28, 26, 26);
        arenaRow.childAlignment = TextAnchor.MiddleCenter;
        arenaRow.childControlWidth = true;
        arenaRow.childControlHeight = true;
        arenaRow.childForceExpandWidth = true;
        arenaRow.childForceExpandHeight = true;

        var hero = AddPanel("Player Fighter Box", arena, Rgba(15, 15, 25, 235));
        AddSideAccent(hero, manaColor);
        AddLayoutSize(hero, 670, -1);
        AddVertical(hero, 10, TextAnchor.UpperCenter, new RectOffset(24, 24, 20, 20));
        AddCharacterArt(hero, player.portraitName, "combat", 180, 180);
        AddText(hero, player.heroName + "  Lv." + player.level + " " + player.heroClass, 28, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 40);
        AddBar(hero, player.hp, MaxHp(), dangerColor, "HP");
        AddBar(hero, player.mp, MaxMp(), manaColor, "MP");
        AddText(hero, StatusLine(playerStatusEffects), 18, FontStyle.Bold, manaColor, TextAnchor.MiddleCenter, 32);
        AddText(hero, StatLine(), 17, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 58);

        var versus = AddPanel("Versus", arena, new Color(0, 0, 0, 0));
        AddLayoutSize(versus, 120, -1);
        AddVertical(versus, 12, TextAnchor.MiddleCenter, new RectOffset(0, 0, 0, 0));
        AddText(versus, "VS", 42, FontStyle.Bold, neonPurple, TextAnchor.MiddleCenter, 80);
        AddText(versus, currentEnemyIsBoss ? "BOSS" : "ENCOUNTER", 18, FontStyle.Bold, mutedColor, TextAnchor.MiddleCenter, 40);

        var enemy = AddPanel("Enemy Fighter Box", arena, Rgba(24, 10, 14, 235));
        AddSideAccent(enemy, dangerColor);
        AddLayoutSize(enemy, 670, -1);
        AddVertical(enemy, 6, TextAnchor.UpperCenter, new RectOffset(24, 24, 20, 20));
        AddEnemyArt(enemy, currentEnemyIsBoss ? 185 : 170);
        AddText(enemy, currentEnemy.name, 28, FontStyle.Bold, dangerColor, TextAnchor.MiddleCenter, 38);
        AddText(enemy, currentEnemy.description, 17, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 36);
        AddBar(enemy, currentEnemy.hp, currentEnemy.maxHp, dangerColor, "HP");
        AddBar(enemy, currentEnemy.mp, currentEnemy.maxMp, manaColor, "MP");
        AddText(enemy, StatusLine(enemyStatusEffects), 17, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 28);
        AddText(enemy, "공격 " + currentEnemy.attack + "  마력 " + currentEnemy.magic + "  방어 " + currentEnemy.defense + "  속도 " + currentEnemy.speed + "  보상 " + currentEnemy.gold + "G", 17, FontStyle.Normal, textColor, TextAnchor.MiddleCenter, 30);

        var commandArea = AddRow("Combat Console", page, 16, TextAnchor.UpperCenter);
        AddLayoutSize(commandArea, -1, 425);

        var commands = AddPanel("Action Deck", commandArea, panelColor);
        AddLayoutSize(commands, 760, -1);
        var commandGrid = commands.gameObject.AddComponent<GridLayoutGroup>();
        commandGrid.cellSize = new Vector2(350, 74);
        commandGrid.spacing = new Vector2(14, 12);
        commandGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        commandGrid.constraintCount = 2;
        commandGrid.childAlignment = TextAnchor.UpperCenter;
        commandGrid.padding = new RectOffset(20, 20, 20, 20);

        AddButton(commands, "Q 기본 공격", () => PlayerAttack("기본 공격", 1.0f, 0, BasicAttackUsesMagic()), panelAltColor).interactable = !actionLocked && !resultBackdrop;
        var playerSilenced = HasStatus(playerStatusEffects, "silence");
        var skillIndex = 0;
        foreach (var skill in ScaledSkillsForPlayer())
        {
            var localSkill = skill;
            var key = skillIndex == 0 ? "W " : (skillIndex == 1 ? "E " : (skillIndex == 2 ? "R " : "T "));
            var skillButton = AddButton(commands, SkillCombatButtonLabel(key, localSkill), () => PlayerAttack(localSkill), manaColor);
            skillButton.interactable = !actionLocked && !resultBackdrop && !playerSilenced && player.mp >= localSkill.mpCost;
            skillIndex++;
        }
        AddButton(commands, "ESC 후퇴", ExitDungeon, dangerColor).interactable = !actionLocked && !resultBackdrop;

        var logPanel = AddPanel("Combat Log", commandArea, Rgba(4, 8, 18, 220));
        AddLayoutSize(logPanel, -1, -1);
        AddVertical(logPanel, 8, TextAnchor.UpperLeft, new RectOffset(22, 22, 20, 20));
        AddText(logPanel, "전투 기록", 28, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 42);
        AddCombatLogList(logPanel);
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
        var list = AddScrollList("Combat Log Lines", parent, Rgba(2, 6, 18, 160), -1, -1, 4, new RectOffset(12, 20, 10, 10));
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
            AddText(list, lines[i], newest ? 19 : 18, newest ? FontStyle.Bold : FontStyle.Normal, newest ? textColor : mutedColor, TextAnchor.MiddleLeft, 32);
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

        if (ProcessTurnStart(playerStatusEffects, "플레이어", true))
        {
            if (currentEnemy == null || player.hp <= 0)
            {
                return;
            }

            ExpireActionStatusEffects(playerStatusEffects);
            actionLocked = true;
            ShowCombat();
            StartCoroutine(EnemyTurn());
            return;
        }

        if (HasStatus(playerStatusEffects, "silence") && skill.mpCost > 0)
        {
            battleLog.Add("침묵 상태라 스킬을 사용할 수 없습니다.");
            ShowCombat();
            return;
        }

        if (player.mp < skill.mpCost)
        {
            battleLog.Add("마나가 부족합니다.");
            ShowCombat();
            return;
        }

        player.mp -= skill.mpCost;
        var basePower = skill.magic ? Magic() : Attack();
        if (skill.noDamage)
        {
            ApplySelfStatus(skill, playerStatusEffects, "플레이어", basePower);
            battleLog.Add(skill.name + "을(를) 사용했습니다.");
            ExpireActionStatusEffects(playerStatusEffects);
            actionLocked = true;
            ShowCombat();
            StartCoroutine(EnemyTurn());
            return;
        }

        if (TryBlindMiss(playerStatusEffects, "플레이어"))
        {
            ExpireActionStatusEffects(playerStatusEffects);
            actionLocked = true;
            ShowCombat();
            StartCoroutine(EnemyTurn());
            return;
        }

        var critical = skill.forceCrit || UnityEngine.Random.value < Mathf.Clamp01(CritChance() + skill.critBonus);
        var damage = CalculateDamage(basePower, skill.multiplier, currentEnemy.defense, playerStatusEffects, enemyStatusEffects, critical ? 1.5f + CritDamage() : 1f, 0f);

        damage = AbsorbShield(enemyStatusEffects, damage, "적");
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

        if (currentEnemy.hp <= 0)
        {
            WinCombat();
            return;
        }

        actionLocked = true;
        ShowCombat();
        StartCoroutine(EnemyTurn());
    }

    private IEnumerator EnemyTurn()
    {
        yield return new WaitForSeconds(0.55f);
        if (currentEnemy == null)
        {
            yield break;
        }

        if (ProcessTurnStart(enemyStatusEffects, currentEnemy.name, false))
        {
            actionLocked = false;
            if (currentEnemy.hp <= 0)
            {
                WinCombat();
                yield break;
            }

            ExpireActionStatusEffects(enemyStatusEffects);
            player.mp = Mathf.Min(MaxMp(), player.mp + PlayerTurnManaRegenAmount());
            ShowCombat();
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
            ApplySelfStatus(skill, enemyStatusEffects, currentEnemy.name, basePower);
            battleLog.Add(attackName + "을(를) 사용했습니다.");
            actionLocked = false;
            ExpireActionStatusEffects(enemyStatusEffects);
            player.mp = Mathf.Min(MaxMp(), player.mp + PlayerTurnManaRegenAmount());
            ShowCombat();
            yield break;
        }

        if (TryBlindMiss(enemyStatusEffects, currentEnemy.name))
        {
            actionLocked = false;
            ExpireActionStatusEffects(enemyStatusEffects);
            player.mp = Mathf.Min(MaxMp(), player.mp + PlayerTurnManaRegenAmount());
            ShowCombat();
            yield break;
        }

        var effectiveEvasion = EffectiveEvasion();
        if (effectiveEvasion > 0f && UnityEngine.Random.value < effectiveEvasion)
        {
            battleLog.Add("회피율 효과로 공격을 피했습니다.");
            actionLocked = false;
            ExpireActionStatusEffects(enemyStatusEffects);
            player.mp = Mathf.Min(MaxMp(), player.mp + PlayerTurnManaRegenAmount());
            ShowCombat();
            yield break;
        }

        var critical = (skill != null && skill.forceCrit) || UnityEngine.Random.value < Mathf.Clamp01(currentEnemy.crit + (skill != null ? skill.critBonus : 0f));
        var damage = CalculateDamage(basePower, multiplier, Defense(), enemyStatusEffects, playerStatusEffects, critical ? 1.5f : 1f, DamageReduction());
        damage = AbsorbShield(playerStatusEffects, damage, "플레이어");
        player.hp -= damage;
        battleLog.Add(attackName + (critical ? " 치명타! " : "으로 ") + damage + " 피해를 받았습니다.");
        if (skill != null)
        {
            ApplySkillStatus(skill, playerStatusEffects, "플레이어", currentEnemy.attack, currentEnemy.magic, 1f);
            ApplySelfStatus(skill, enemyStatusEffects, currentEnemy.name, basePower);
        }
        ExpireActionStatusEffects(enemyStatusEffects);
        actionLocked = false;

        if (player.hp <= 0)
        {
            LoseCombat();
            yield break;
        }

        player.mp = Mathf.Min(MaxMp(), player.mp + PlayerTurnManaRegenAmount());
        ShowCombat();
    }

    private void WinCombat()
    {
        actionLocked = false;
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
        ShowCombat(true);
        currentScreen = AetheriaScreen.Victory;

        var page = AddFlatPanel("Victory Result", root, new Color(0f, 0f, 0f, 0f));
        Stretch(page, 0, 0, 0, 0);
        page.GetComponent<Image>().raycastTarget = true;
        AddVertical(page, 14, TextAnchor.MiddleCenter, new RectOffset(70, 70, 54, 54));

        var panel = AddPanel("Victory Panel", page, Rgba(4, 8, 18, 235));
        AddSideAccent(panel, goodColor);
        AddLayoutSize(panel, 700, 600);
        AddVertical(panel, 10, TextAnchor.MiddleCenter, new RectOffset(28, 28, 24, 24));

        AddCharacterArt(panel, player.portraitName, "victory", 180, 180);
        AddText(panel, title, 34, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 50);
        AddMessageBanner(panel, message, goodColor, 96);
        AddDivider(panel, Rgba(148, 163, 184, 150));
        AddText(panel, "현재 HP " + player.hp + "/" + MaxHp() + "  MP " + player.mp + "/" + MaxMp() + "\n가방 " + InventoryCountLabel() + "  골드 " + player.gold + "G", 17, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 54);
        AddButton(panel, buttonLabel, () =>
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
        }, goodColor);
    }

    private void LoseCombat()
    {
        actionLocked = false;
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
        ShowCombat(true);
        currentScreen = AetheriaScreen.Defeat;

        var page = AddFlatPanel("Defeat Result", root, new Color(0f, 0f, 0f, 0f));
        Stretch(page, 0, 0, 0, 0);
        page.GetComponent<Image>().raycastTarget = true;
        AddVertical(page, 14, TextAnchor.MiddleCenter, new RectOffset(70, 70, 54, 54));

        var panel = AddPanel("Defeat Panel", page, Rgba(24, 10, 14, 235));
        AddSideAccent(panel, dangerColor);
        AddLayoutSize(panel, 700, 570);
        AddVertical(panel, 10, TextAnchor.MiddleCenter, new RectOffset(28, 28, 24, 24));

        AddCharacterArt(panel, player.portraitName, "defeat", 170, 170);
        AddText(panel, "전투 패배", 34, FontStyle.Bold, dangerColor, TextAnchor.MiddleCenter, 50);
        AddMessageBanner(panel, "차원 균열에서 구조되었습니다.\n치료비와 탈출 비용으로 " + lostGold + "G를 잃었습니다.", dangerColor, 86);
        AddDivider(panel, Rgba(148, 163, 184, 130));
        AddText(panel, "현재 HP " + player.hp + "/" + MaxHp() + "  MP " + player.mp + "/" + MaxMp() + "\n골드 " + player.gold + "G", 17, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 50);
        AddButton(panel, "마을로", () =>
        {
            currentEnemy = null;
            ShowTown("패배했습니다. 마을 치유사들이 당신을 구조했습니다. " + lostGold + "G를 잃었습니다.");
        }, panelAltColor);
    }

    private void ShowGameClear(string message)
    {
        EnsurePlayerData();
        currentScreen = AetheriaScreen.GameClear;
        ClearRoot();

        var page = AddPanel("Game Clear", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 24, TextAnchor.MiddleCenter, new RectOffset(90, 90, 70, 70));

        var panel = AddPanel("Game Clear Panel", page, Rgba(4, 8, 18, 235));
        AddSideAccent(panel, goldColor);
        AddLayoutSize(panel, 1080, 780);
        AddVertical(panel, 18, TextAnchor.MiddleCenter, new RectOffset(48, 48, 42, 42));

        AddCharacterArt(panel, player.portraitName, "victory", 200, 200);
        AddText(panel, "AETHERIA 정복 완료", 58, FontStyle.Bold, goldColor, TextAnchor.MiddleCenter, 86);
        AddMessageBanner(panel, message, goldColor, 110);
        AddText(panel, "모든 차원 포탈의 지배자를 토벌했습니다.\n장비 조합, 강화, 직업 스킬을 더 올려 초월 세팅을 완성할 수 있습니다.", 22, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, 95);

        var buttons = AddRow("Game Clear Buttons", panel, 16, TextAnchor.MiddleCenter);
        AddLayoutSize(buttons, -1, 74);
        AddButton(buttons, "마을로", () => ShowTown("정복 기록을 가지고 마을로 돌아왔습니다."), goodColor);
        AddButton(buttons, "메인 메뉴", ShowMainMenu, panelAltColor);
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
            LoseCombat();
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
        var classId = GearClassId(heroClass);
        if (classId == "mage") return Rgb(59, 130, 246);
        if (classId == "rogue") return Rgb(168, 85, 247);
        if (classId == "priest") return Rgb(250, 204, 21);
        if (classId == "bomber") return Rgb(239, 68, 68);
        if (classId == "spirit") return Rgb(20, 184, 166);
        if (classId == "archer") return Rgb(34, 197, 94);
        if (classId == "monk") return Rgb(245, 158, 11);
        return Rgb(148, 163, 184);
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
        return "현재 목표: 추천 레벨에 맞는 던전을 선택해 보스까지 돌파.\n상태이상은 턴 시작에 적용되고, 기절/빙결은 행동을 막습니다.\n작업 메모: D:\\수정 목록M.K 1.txt / 백업: D:\\Unity_Backups";
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

        for (var i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }

        AddGeneratedScreenBackdrop();
    }

    private RectTransform AddPanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        if (IsGeneratedScreenPage(name, parent))
        {
            color = new Color(0, 0, 0, 0);
        }
        image.color = color;
        image.raycastTarget = color.a > 0.05f;
        var rect = go.GetComponent<RectTransform>();
        if (color.a > 0.05f && name != "Fill")
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.Lerp(UiFrameAccentColor(name), Rgba(220, 220, 210, 255), 0.2f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            var usesGeneratedSkin = ApplyGeneratedPanelSkin(name, image, color);
            if (!usesGeneratedSkin && ShouldAddPanelDepth(name))
            {
                AddPanelDepth(rect, CompactFrameForPanel(name));
            }
            if (!usesGeneratedSkin && ShouldAddOrnateFrame(name))
            {
                AddOrnateFrame(rect, UiFrameAccentColor(name), CompactFrameForPanel(name));
            }
        }
        return rect;
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
        var viewport = AddPanel(name + " Viewport", parent, color);
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

        var track = AddFlatPanel("Scrollbar Track", viewport, Rgba(0, 0, 0, 96));
        track.anchorMin = new Vector2(1f, 0f);
        track.anchorMax = new Vector2(1f, 1f);
        track.pivot = new Vector2(1f, 0.5f);
        track.anchoredPosition = new Vector2(-4f, 0f);
        track.sizeDelta = new Vector2(UiScrollbarWidth, -12f);
        track.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        track.GetComponent<Image>().raycastTarget = true;

        var handle = AddFlatPanel("Scrollbar Handle", track, Rgba(245, 190, 92, 190));
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
        var banner = AddPanel("Screen Message Banner", parent, Rgba(6, 10, 20, 218));
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
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(9, size - DynamicTextShrink(size, value));
        text.resizeTextMaxSize = size;
        text.lineSpacing = value != null && value.Contains("\n") ? 0.92f : 1f;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(1.4f, -1.4f);

        AddLayoutSize(go.GetComponent<RectTransform>(), -1, preferredHeight);
        return text;
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
        image.color = color;
        var usesGeneratedSkin = ApplyGeneratedButtonSkin(image, color);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.Lerp(color, Color.white, 0.22f);
        outline.effectDistance = new Vector2(1.4f, -1.4f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        var colors = button.colors;
        colors.normalColor = usesGeneratedSkin ? Color.Lerp(Color.white, color, 0.16f) : color;
        colors.highlightedColor = usesGeneratedSkin ? Color.white : Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = usesGeneratedSkin ? Rgb(190, 194, 202) : Color.Lerp(color, Color.black, 0.18f);
        colors.disabledColor = Rgb(66, 72, 86);
        button.colors = colors;

        var rect = go.GetComponent<RectTransform>();
        if (!usesGeneratedSkin)
        {
            AddOrnateFrame(rect, Color.Lerp(color, UiFrameAccentColor(label), 0.35f), true);
            AddButtonDepth(rect, color);
        }
        AddLayoutSize(rect, 260, 62);
        var textSize = ButtonTextSize(label);
        var labelText = AddText(go.transform, label, textSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 62);
        labelText.resizeTextMinSize = Mathf.Max(10, textSize - 9);
        Stretch(labelText.GetComponent<RectTransform>(), 12, 0, 12, 0);
        var textPresenter = go.AddComponent<UiButtonTextPresenter>();
        textPresenter.Configure(button, labelText, Color.white, Rgb(180, 186, 198));
        return button;
    }

    private int ButtonTextSize(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return UiButtonDefaultTextSize;
        }

        if (label.Contains("\n")) return 18;
        if (label.Length > 18) return 18;
        if (label.Length > 10) return 20;
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
            if (owner == null || viewport == null)
            {
                return;
            }

            hasLastLocalPointer = owner.TryDungeonMapLocalPointer(viewport, eventData, out lastLocalPointer);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner == null || viewport == null)
            {
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
            if (owner == null)
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

        public EnemyTemplate(string name, string description, int hp, int maxMp, int attack, int magic, int defense, int speed, float crit, int gold, int xp, List<SkillState> skills)
        {
            this.name = name;
            this.description = description;
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
