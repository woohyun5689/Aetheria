#if UNITY_EDITOR
using System;
using UnityEngine;

public sealed partial class AetheriaGame
{
    private bool hudEditorPreviewMode;
    private bool hudEditorDungeonInfoPreview;

    public RectTransform EditorHudRoot
    {
        get { return root; }
    }

    public RectTransform EditorCurrentHudPage
    {
        get
        {
            if (root == null)
            {
                return null;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var candidate = root.GetChild(i) as RectTransform;
                if (candidate != null && candidate.gameObject.activeInHierarchy
                    && candidate.name != "Generated Screen Backdrop")
                {
                    return candidate;
                }
            }
            return null;
        }
    }

    public string EditorCurrentHudScreenKey
    {
        get
        {
            var page = EditorCurrentHudPage;
            if (page != null && page.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Title";
            }

            switch (currentScreen)
            {
                case AetheriaScreen.MainMenu: return "MainMenu";
                case AetheriaScreen.ClassSelect: return "ClassSelect";
                case AetheriaScreen.Guide: return "Guide";
                case AetheriaScreen.Town: return "Town";
                case AetheriaScreen.Inventory: return "Inventory";
                case AetheriaScreen.Enhancement: return "Enhancement";
                case AetheriaScreen.SkillTraining: return "SkillTraining";
                case AetheriaScreen.Crafting: return "Crafting";
                case AetheriaScreen.DungeonSelect:
                    return hudEditorDungeonInfoPreview && EditorDungeonInfoPopupOpen(page)
                        ? "DungeonInfo"
                        : "DungeonSelect";
                case AetheriaScreen.Combat: return "Combat";
                case AetheriaScreen.Victory: return "Victory";
                case AetheriaScreen.Defeat: return "Defeat";
                case AetheriaScreen.GameClear: return "GameClear";
                default: return "MainMenu";
            }
        }
    }

    public bool EditorIsHudScreenPageName(string objectName)
    {
        return root != null && IsGeneratedScreenPage(objectName, root);
    }

    public void EditorOpenHudPreview(string screenName)
    {
        hudEditorPreviewMode = true;
        hudEditorDungeonInfoPreview = false;
        switch (screenName)
        {
            case "Title":
                ShowTitleScreen();
                break;
            case "MainMenu":
                ShowMainMenu();
                break;
            case "ClassSelect":
                ShowClassSelect(activeSlot);
                break;
            case "Guide":
                ShowHowToPlay(true);
                break;
            case "Town":
                EnsureHudEditorPlayer(false);
                ShowTown("HUD 편집 미리보기");
                break;
            case "Inventory":
                EnsureHudEditorPlayer(true);
                ShowInventory("HUD 편집 미리보기");
                break;
            case "Enhancement":
                EnsureHudEditorPlayer(false);
                ShowEnhancement("HUD 편집 미리보기");
                break;
            case "SkillTraining":
                EnsureHudEditorPlayer(false);
                ShowSkillTraining("HUD 편집 미리보기");
                break;
            case "Crafting":
                EnsureHudEditorPlayer(false);
                ShowCrafting("HUD 편집 미리보기");
                break;
            case "DungeonSelect":
                EnsureHudEditorPlayer(false);
                ShowDungeonSelect(false);
                break;
            case "DungeonInfo":
                EnsureHudEditorPlayer(false);
                hudEditorDungeonInfoPreview = true;
                OpenHudEditorDungeonInfoPreview();
                break;
            case "Combat":
                EnsureHudEditorPlayer(false);
                OpenHudEditorCombatPreview();
                break;
            case "Victory":
                EnsureHudEditorPlayer(false);
                ShowVictoryResult("전투 승리", "HUD 편집 미리보기", "확인", delegate { });
                break;
            case "Defeat":
                EnsureHudEditorPlayer(false);
                ShowDefeatResult(0);
                break;
            case "GameClear":
                EnsureHudEditorPlayer(false);
                ShowGameClear("HUD 편집 미리보기");
                break;
            default:
                ShowMainMenu();
                break;
        }

        StabilizeHudEditorScreen();
    }

    public void EditorEndHudPreview()
    {
        hudEditorPreviewMode = false;
        hudEditorDungeonInfoPreview = false;
    }

    private void StabilizeHudEditorScreen()
    {
        // The regular screen entrance starts with the root CanvasGroup at zero
        // alpha. A HUD clone captured in that short interval would stay invisible
        // forever because preview animation behaviours are deliberately disabled.
        screenMotionVersion++;
        if (screenMotionCoroutine != null)
        {
            StopCoroutine(screenMotionCoroutine);
            screenMotionCoroutine = null;
        }
        RestoreScreenMotionState();
        Canvas.ForceUpdateCanvases();
    }

    private void EnsureHudEditorPlayer(bool showSelectedInventoryState)
    {
        if (player == null)
        {
            var bestSlot = FindHudEditorPreviewSlot();
            if (bestSlot >= 0)
            {
                LoadGame(bestSlot);
            }
        }

        if (player == null)
        {
            var heroClasses = HeroClasses();
            if (heroClasses.Count > 0)
            {
                player = CreatePlayer(heroClasses[0]);
            }
        }

        if (player == null)
        {
            return;
        }

        EnsurePlayerData();
        EnsureHudEditorInventorySamples();
        if (showSelectedInventoryState && player.inventory != null && player.inventory.Count > 0)
        {
            selectedInventoryIndex = Mathf.Clamp(selectedInventoryIndex, 0, player.inventory.Count - 1);
        }
    }

    private int FindHudEditorPreviewSlot()
    {
        var bestSlot = -1;
        var bestScore = int.MinValue;
        for (var slot = 0; slot < SaveSlotCount; slot++)
        {
            PlayerState state;
            if (!TryReadSaveState(slot, out state) || state == null)
            {
                continue;
            }

            var inventoryCount = state.inventory != null ? state.inventory.Count : 0;
            var equippedCount = (state.weapon != null ? 1 : 0)
                + (state.armor != null ? 1 : 0)
                + (state.charm != null ? 1 : 0)
                + (state.charm2 != null ? 1 : 0)
                + (state.charm3 != null ? 1 : 0)
                + (state.charm4 != null ? 1 : 0);
            var score = inventoryCount * 100000
                + equippedCount * 10000
                + Mathf.Max(0, state.stage) * 100
                + Mathf.Max(0, state.level);
            if (score > bestScore || (score == bestScore && slot == activeSlot))
            {
                bestScore = score;
                bestSlot = slot;
            }
        }
        return bestSlot;
    }

    private void EnsureHudEditorInventorySamples()
    {
        if (player == null || (player.inventory != null && player.inventory.Count > 0))
        {
            return;
        }

        if (player.inventory == null)
        {
            player.inventory = new System.Collections.Generic.List<ItemState>();
        }

        var types = new[] { "Weapon", "Armor", "Charm", "Charm" };
        for (var i = 0; i < types.Length; i++)
        {
            var item = BuildGeneratedGearItem(
                types[i],
                Mathf.Clamp(i, 0, 3),
                player.heroClass,
                Mathf.Max(1, player.level),
                Mathf.Max(1, player.stage));
            if (item != null)
            {
                item.name = "HUD 미리보기 장비 " + (i + 1);
                player.inventory.Add(item);
            }
        }
    }

    private void OpenHudEditorCombatPreview()
    {
        ResetCombatState(true);
        var dungeons = Dungeons();
        if (dungeons.Count == 0)
        {
            ShowTown("전투 미리보기 데이터가 없습니다.");
            return;
        }
        StartCombat(dungeons[0], false);
    }

    private void OpenHudEditorDungeonInfoPreview()
    {
        var dungeons = Dungeons();
        if (dungeons.Count == 0)
        {
            ShowDungeonSelect(false);
            return;
        }

        var dungeon = NextRecommendedDungeon(dungeons) ?? dungeons[0];
        selectedDungeonRegionKey = DungeonRegionKey(dungeon);
        dungeonMapScrollOpened = true;
        dungeonMapScrollOpening = false;
        dungeonMapZoomed = true;
        dungeonMapZoom = DungeonMapRegionZoom;
        dungeonMapPan = Vector2.zero;
        ShowDungeonSelect(false);

        var page = EditorCurrentHudPage;
        var mapImage = page != null
            ? page.Find("Dungeon World Map Panel/Dungeon World Map Image") as RectTransform
            : null;
        if (mapImage == null)
        {
            return;
        }

        var region = FindDungeonMapRegion(selectedDungeonRegionKey);
        if (region != null)
        {
            FocusDungeonMapRegion(region, mapImage);
            ApplyDungeonMapView(mapImage);
        }
        ShowDungeonInfoPopup(mapImage, dungeon, new Vector2(50f, 50f));
    }

    private static bool EditorDungeonInfoPopupOpen(RectTransform page)
    {
        return page != null
            && page.Find("Dungeon World Map Panel/Dungeon Info Popup") != null;
    }
}
#endif
