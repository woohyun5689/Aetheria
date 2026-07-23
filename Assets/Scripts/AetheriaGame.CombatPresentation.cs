using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private enum CombatPresentationPhase
    {
        PlayerChoice,
        PlayerResolving,
        EnemyResolving,
        Result
    }

    private sealed class CombatBarView
    {
        public RectTransform primaryFill;
        public RectTransform delayedFill;
        public Text valueText;
        public int maxValue;
    }

    private sealed class CombatPresentationEvent
    {
        public string actionName;
        public bool attackerIsPlayer;
        public bool targetIsPlayer;
        public bool magic;
        public bool critical;
        public bool missed;
        public bool buff;
        public bool statusOnly;
        public bool targetDefeated;
        public int damage;
        public int absorbedDamage;
        public int healAmount;
        public int oldTargetHp;
        public int newTargetHp;
        public string statusType;
        public string supportType;
    }

    private RectTransform combatStageContent;
    private RectTransform combatFxLayer;
    private RectTransform combatHeroArt;
    private RectTransform combatEnemyArt;
    private RectTransform combatHeroPanel;
    private RectTransform combatEnemyPanel;
    private Image combatHeroImage;
    private Image combatEnemyImage;
    private Image combatHeroGlow;
    private Image combatEnemyGlow;
    private Image combatImpactFlash;
    private Image combatTurnPillImage;
    private Text combatTurnText;
    private Text combatActionText;
    private Text combatCommandText;
    private CombatBarView combatHeroHpBar;
    private CombatBarView combatEnemyHpBar;
    private CombatPresentationEvent pendingCombatPresentation;
    private Coroutine combatSequence;
    private int combatSequenceVersion;
    private int combatUiGeneration;
    private Sprite combatGlowSprite;
    private Texture2D combatGlowTexture;
    private CombatPresentationPhase combatPresentationPhase = CombatPresentationPhase.PlayerChoice;
    private float nextCombatRejectTime;
    private bool combatLogExpanded;

    private void BuildCombatScreen(bool resultBackdrop)
    {
        var phase = resultBackdrop ? CombatPresentationPhase.Result : combatPresentationPhase;
        var canChooseAction = phase == CombatPresentationPhase.PlayerChoice && !actionLocked && !resultBackdrop;
        var page = AddPanel("Combat", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 8, TextAnchor.UpperCenter, new RectOffset(24, 24, 14, 14));

        // The battlefield background is already supplied by the generated/character
        // backdrop loader. Only a light readability strip is kept over it here.
        var header = AddFlatPanel("Combat Header Plate", page, new Color(0.94f, 0.98f, 1f, 0.30f));
        header.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(header, -1, 64);
        var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerRow.spacing = 12;
        headerRow.padding = new RectOffset(16, 16, 5, 5);
        headerRow.childAlignment = TextAnchor.MiddleCenter;
        headerRow.childControlWidth = true;
        headerRow.childControlHeight = true;
        headerRow.childForceExpandWidth = false;
        AddText(
            header,
            (currentDungeon != null ? currentDungeon.name + "  " + currentDungeonFloor + "/" + DungeonFloorCount(currentDungeon) + "층" : "차원 전투")
                + (currentEnemyIsBoss ? "  ·  보스전" : "  ·  일반 전투"),
            25,
            FontStyle.Bold,
            textColor,
            TextAnchor.MiddleLeft,
            48);

        var turnPill = AddFlatPanel("Turn Banner", header, CombatPhaseBackground(phase));
        combatTurnPillImage = turnPill.GetComponent<Image>();
        combatTurnPillImage.raycastTarget = false;
        AddLayoutSize(turnPill, 238, 46);
        combatTurnText = AddText(
            turnPill,
            CombatPhaseTitle(phase),
            20,
            FontStyle.Bold,
            CombatPhaseAccent(phase),
            TextAnchor.MiddleCenter,
            46);
        Stretch(combatTurnText.GetComponent<RectTransform>(), 8, 0, 8, 0);

        var actionRibbon = AddFlatPanel("Current Action Ribbon", header, new Color(0.97f, 0.99f, 1f, 0.30f));
        actionRibbon.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(actionRibbon, 410, 46);
        combatActionText = AddText(
            actionRibbon,
            pendingCombatPresentation != null
                ? pendingCombatPresentation.actionName
                : CombatPhaseActionPrompt(phase),
            19,
            FontStyle.Bold,
            CombatPhaseAccent(phase),
            TextAnchor.MiddleCenter,
            46);
        Stretch(combatActionText.GetComponent<RectTransform>(), 10, 0, 10, 0);

        var retreat = AddButton(header, "마을로 후퇴", ExitDungeon, dangerColor);
        AddLayoutSize(retreat.GetComponent<RectTransform>(), 178, 56);
        var retreatLabel = retreat.GetComponentInChildren<Text>();
        if (retreatLabel != null)
        {
            retreatLabel.fontSize = 19;
            retreatLabel.resizeTextMinSize = 16;
            retreatLabel.resizeTextMaxSize = 19;
        }
        retreat.interactable = canChooseAction;

        // Open stage: no VS column, no vertical separator, no opaque combatant boxes.
        var arena = AddFlatPanel("Combat Arena", page, new Color(0.90f, 0.96f, 1f, 0.04f));
        AddLayoutSize(arena, -1, 590);
        var arenaImage = arena.GetComponent<Image>();
        arenaImage.raycastTarget = false;

        combatStageContent = AddFlatPanel("Combat Stage Content", arena, new Color(0f, 0f, 0f, 0f));
        Stretch(combatStageContent, 18, 4, 18, 4);
        combatStageContent.GetComponent<Image>().raycastTarget = false;
        AddCombatStageAtmosphere(combatStageContent);

        combatHeroPanel = AddFlatPanel("Player Combatant", combatStageContent, Color.clear);
        SetCombatStageRect(combatHeroPanel, new Vector2(0.01f, 0.01f), new Vector2(0.405f, 0.99f), 8f, 0f, -10f, 0f);
        combatHeroPanel.GetComponent<Image>().raycastTarget = false;
        AddVertical(combatHeroPanel, 4, TextAnchor.UpperCenter, new RectOffset(12, 26, 4, 4));
        AddHeroCombatArt(combatHeroPanel);
        AddText(combatHeroPanel, player.heroName + "  Lv." + player.level + "  " + player.heroClass, 28, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 34);
        var displayedHeroHp = DisplayedCombatHp(true, player.hp);
        combatHeroHpBar = AddCombatBar(combatHeroPanel, displayedHeroHp, MaxHp(), dangerColor, "HP");
        if (MaxMp() > 0)
        {
            AddCombatBar(combatHeroPanel, player.mp, MaxMp(), manaColor, "MP");
        }
        AddCombatStatusSummary(combatHeroPanel, playerStatusEffects, manaColor);
        AddText(combatHeroPanel, "공격 " + Attack() + "  ·  마력 " + Magic() + "  ·  방어 " + Defense() + "  ·  속도 " + Speed(), 18, FontStyle.Bold, mutedColor, TextAnchor.MiddleCenter, 24);

        combatEnemyPanel = AddFlatPanel("Enemy Combatant", combatStageContent, Color.clear);
        SetCombatStageRect(combatEnemyPanel, new Vector2(0.595f, 0.01f), new Vector2(0.99f, 0.99f), 10f, 0f, -8f, 0f);
        combatEnemyPanel.GetComponent<Image>().raycastTarget = false;
        AddVertical(combatEnemyPanel, 4, TextAnchor.UpperCenter, new RectOffset(26, 12, 4, 4));
        AddEnemyCombatArt(combatEnemyPanel);
        AddText(combatEnemyPanel, currentEnemyIsBoss ? "BOSS  ·  " + currentEnemy.name : currentEnemy.name, 29, FontStyle.Bold, currentEnemyIsBoss ? goldColor : dangerColor, TextAnchor.MiddleCenter, 34);
        var displayedEnemyHp = DisplayedCombatHp(false, currentEnemy.hp);
        combatEnemyHpBar = AddCombatBar(combatEnemyPanel, displayedEnemyHp, currentEnemy.maxHp, currentEnemyIsBoss ? goldColor : dangerColor, "HP");
        if (currentEnemy.maxMp > 0)
        {
            AddCombatBar(combatEnemyPanel, currentEnemy.mp, currentEnemy.maxMp, manaColor, "MP");
        }
        AddCombatStatusSummary(combatEnemyPanel, enemyStatusEffects, goldColor);
        AddText(combatEnemyPanel, EnemyCombatHint(), 18, FontStyle.Bold, mutedColor, TextAnchor.MiddleCenter, 24);

        AddEnemyIntentWidget(combatStageContent, phase);

        combatFxLayer = AddFlatPanel("Combat FX Layer", arena, new Color(0f, 0f, 0f, 0f));
        Stretch(combatFxLayer, 0, 0, 0, 0);
        combatFxLayer.GetComponent<Image>().raycastTarget = false;
        combatFxLayer.SetAsLastSibling();

        var flash = AddFlatPanel("Combat Impact Flash", arena, new Color(1f, 1f, 1f, 0f));
        Stretch(flash, 0, 0, 0, 0);
        flash.GetComponent<Image>().raycastTarget = false;
        flash.SetAsLastSibling();
        combatImpactFlash = flash.GetComponent<Image>();
        combatFxLayer.SetAsLastSibling();

        // These are the player's five fixed actions presented as cards. There is
        // intentionally no draw pile, random hand, or change to combat mechanics.
        var commands = AddFlatPanel("Fixed Action Deck", page, new Color(0.95f, 0.98f, 1f, 0.30f));
        commands.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(commands, -1, 366);
        AddVertical(commands, 8, TextAnchor.UpperCenter, new RectOffset(14, 14, 10, 10));

        var commandHeader = AddFlatPanel("Action Deck Header", commands, Color.clear);
        commandHeader.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(commandHeader, -1, 100);
        var commandHeaderRow = commandHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
        commandHeaderRow.spacing = 12;
        commandHeaderRow.padding = new RectOffset(4, 4, 0, 0);
        commandHeaderRow.childAlignment = TextAnchor.MiddleCenter;
        commandHeaderRow.childControlWidth = true;
        commandHeaderRow.childControlHeight = true;
        commandHeaderRow.childForceExpandWidth = false;

        var commandPrompt = AddFlatPanel("Action Prompt", commandHeader, Color.clear);
        commandPrompt.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(commandPrompt, -1, 100);
        AddVertical(commandPrompt, 1, TextAnchor.MiddleLeft, new RectOffset(8, 8, 2, 2));
        combatCommandText = AddText(
            commandPrompt,
            CombatPhaseCommandPrompt(phase),
            22,
            FontStyle.Bold,
            CombatPhaseAccent(phase),
            TextAnchor.MiddleLeft,
            40);
        AddText(commandPrompt, "카드를 선택해 적을 공격하세요  ·  마우스 또는 패드 조작  ·  ESC 후퇴", 17, FontStyle.Bold, mutedColor, TextAnchor.MiddleLeft, 25);

        var logToggle = AddCombatLogToggle(commandHeader, canChooseAction);
        AddLayoutSize(logToggle.GetComponent<RectTransform>(), 720, 94);

        var commandButtons = AddFlatPanel("Fixed Action Cards", commands, Color.clear);
        commandButtons.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(commandButtons, -1, 238);
        var commandRow = commandButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
        commandRow.spacing = 16;
        commandRow.padding = new RectOffset(8, 8, 2, 2);
        commandRow.childAlignment = TextAnchor.MiddleCenter;
        commandRow.childControlWidth = true;
        commandRow.childControlHeight = true;
        commandRow.childForceExpandWidth = false;
        commandRow.childForceExpandHeight = false;

        var combatSkills = ScaledSkillsForPlayer();
        var totalActionCards = Mathf.Clamp(combatSkills.Count + 1, 1, 5);
        var basicAttack = AddCombatActionCard(
            commandButtons,
            0,
            totalActionCards,
            "Q",
            "기본 공격",
            "MP 0",
            BasicAttackUsesMagic() ? "마력 기반의 안정적인 공격" : "공격력 기반의 안정적인 공격",
            () => PlayerAttack("기본 공격", 1.0f, 0, BasicAttackUsesMagic()),
            CombatBasicCardAccent(),
            canChooseAction,
            "공격");
        basicAttack.interactable = canChooseAction;
        var playerSilenced = HasStatus(playerStatusEffects, "silence");
        var skillIndex = 0;
        foreach (var skill in combatSkills)
        {
            var localSkill = skill;
            var key = skillIndex == 0 ? "W" : (skillIndex == 1 ? "E" : (skillIndex == 2 ? "R" : "T"));
            var unavailable = playerSilenced || player.mp < localSkill.mpCost;
            var reason = playerSilenced ? "침묵으로 사용 불가" : (player.mp < localSkill.mpCost ? "MP 부족" : SkillCombatEffectSummary(localSkill));
            var roleAccent = CombatCardRoleAccent(localSkill);
            var skillButton = AddCombatActionCard(
                commandButtons,
                skillIndex + 1,
                totalActionCards,
                key,
                localSkill.name,
                "MP " + localSkill.mpCost,
                reason,
                () => PlayerAttack(localSkill),
                unavailable ? panelAltColor : roleAccent,
                canChooseAction && !unavailable,
                CombatCardRoleLabel(localSkill));
            skillButton.interactable = canChooseAction && !unavailable;
            skillIndex++;
        }

        if (!canChooseAction && !resultBackdrop)
        {
            AddCombatInputLockOverlay(commandButtons);
        }
    }

    private static void SetCombatStageRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float left, float top, float right, float bottom)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, -top);
    }

    private void AddCombatStageAtmosphere(RectTransform parent)
    {
        var horizon = AddFlatPanel("Combat Ground Horizon", parent, new Color(0.24f, 0.43f, 0.57f, 0.20f));
        horizon.anchorMin = new Vector2(0.08f, 0.205f);
        horizon.anchorMax = new Vector2(0.92f, 0.205f);
        horizon.sizeDelta = new Vector2(0f, 2f);
        horizon.anchoredPosition = Vector2.zero;
        horizon.GetComponent<Image>().raycastTarget = false;

        var stageLight = AddFlatPanel("Combat Stage Light", parent, new Color(0.85f, 0.96f, 1f, 0.13f));
        stageLight.anchorMin = new Vector2(0.22f, 0.02f);
        stageLight.anchorMax = new Vector2(0.78f, 0.62f);
        stageLight.offsetMin = Vector2.zero;
        stageLight.offsetMax = Vector2.zero;
        var stageLightImage = stageLight.GetComponent<Image>();
        stageLightImage.sprite = CombatGlowSprite();
        stageLightImage.raycastTarget = false;
    }

    private void AddEnemyIntentWidget(RectTransform parent, CombatPresentationPhase phase)
    {
        if (currentEnemy == null)
        {
            return;
        }

        var accent = currentEnemyIsBoss ? goldColor : EnemyThemeColor();
        var intent = AddFlatPanel("Enemy Intent", parent, new Color(0.96f, 0.98f, 1f, 0.30f));
        intent.anchorMin = new Vector2(0.635f, 0.790f);
        intent.anchorMax = new Vector2(0.845f, 0.985f);
        intent.offsetMin = Vector2.zero;
        intent.offsetMax = Vector2.zero;
        intent.GetComponent<Image>().raycastTarget = false;

        var marker = AddFlatPanel("Enemy Intent Accent", intent, new Color(accent.r, accent.g, accent.b, 0.92f));
        marker.anchorMin = new Vector2(0f, 0.18f);
        marker.anchorMax = new Vector2(0f, 0.82f);
        marker.pivot = new Vector2(0f, 0.5f);
        marker.sizeDelta = new Vector2(5f, 0f);
        marker.anchoredPosition = new Vector2(5f, 0f);
        marker.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        marker.GetComponent<Image>().raycastTarget = false;

        AddEnemyIntentPointer(intent, accent);
        BuildEnemyIntentVisual(intent, phase, accent);
    }

    private string EnemyIntentTitle(CombatPresentationPhase phase)
    {
        if (phase == CombatPresentationPhase.Result)
        {
            return "행동 종료";
        }
        if (pendingCombatPresentation != null && !pendingCombatPresentation.attackerIsPlayer)
        {
            return pendingCombatPresentation.actionName;
        }
        if (HasStatus(enemyStatusEffects, "stun") || HasStatus(enemyStatusEffects, "freeze"))
        {
            return "행동 불가 예고";
        }
        if (phase == CombatPresentationPhase.EnemyResolving)
        {
            return "행동 준비 중";
        }

        var baseIntent = currentEnemy.magic > currentEnemy.attack * 1.2f
            ? "마법 공격 경계"
            : (currentEnemy.attack > currentEnemy.magic * 1.2f ? "물리 공격 경계" : "혼합 공격 경계");
        if (HasStatus(enemyStatusEffects, "silence"))
        {
            return "기본 공격 예상";
        }
        return EnemyHasAffordableSkill() ? baseIntent + " · 스킬 가능" : baseIntent;
    }

    private string EnemyIntentDetail(CombatPresentationPhase phase)
    {
        if (pendingCombatPresentation != null && !pendingCombatPresentation.attackerIsPlayer)
        {
            return "현재 실행 중인 행동입니다";
        }
        if (HasStatus(enemyStatusEffects, "stun") || HasStatus(enemyStatusEffects, "freeze"))
        {
            return "다음 턴 행동 불가";
        }
        if (phase == CombatPresentationPhase.Result)
        {
            return "전투 결과를 확인하세요";
        }
        if (HasStatus(enemyStatusEffects, "silence"))
        {
            return "침묵 상태 · 스킬 사용 불가";
        }
        if (EnemyManaRegenExpected() && EnemyHasAffordableSkill())
        {
            return "MP 회복 예정 · 스킬 가능";
        }
        return EnemyHasAffordableSkill()
            ? "MP 충분 · 스킬 가능"
            : "스킬 불가 · 기본 공격 예상";
    }

    private bool EnemyHasAffordableSkill()
    {
        if (currentEnemy == null || currentEnemy.skills == null || HasStatus(enemyStatusEffects, "silence"))
        {
            return false;
        }

        for (var index = 0; index < currentEnemy.skills.Count; index++)
        {
            var skill = currentEnemy.skills[index];
            if (skill != null && EnemyPreviewMpForNextAction() >= EnemySkillCost(skill))
            {
                return true;
            }
        }
        return false;
    }

    private bool EnemyManaRegenExpected()
    {
        return currentEnemy != null
            && currentEnemy.maxMp > 0
            && currentEnemy.mp < currentEnemy.maxMp
            && enemyManaRegenTurnCounter >= 2;
    }

    private int EnemyPreviewMpForNextAction()
    {
        if (currentEnemy == null)
        {
            return 0;
        }
        if (!EnemyManaRegenExpected())
        {
            return currentEnemy.mp;
        }

        var restored = Mathf.Min(
            currentEnemy.maxMp - currentEnemy.mp,
            RoundToGameInt(currentEnemy.maxMp * MonsterTurnManaRegen));
        return Mathf.Min(currentEnemy.maxMp, currentEnemy.mp + Mathf.Max(0, restored));
    }

    private Button AddCombatActionCard(
        Transform parent,
        int cardIndex,
        int cardCount,
        string key,
        string title,
        string resource,
        string description,
        System.Action onClick,
        Color accent,
        bool canChooseAction,
        string roleLabel)
    {
        var safeDescription = string.IsNullOrEmpty(description) ? "직업 고유 행동" : description;
        var card = AddButton(parent, title + "\n" + resource + "\n" + safeDescription, onClick, accent);
        var rect = card.GetComponent<RectTransform>();
        var preferredCardWidth = cardCount <= 3 ? 300f : cardCount == 4 ? 270f : 244f;
        AddLayoutSize(rect, preferredCardWidth, 226);
        var layout = rect.GetComponent<LayoutElement>();
        layout.minWidth = preferredCardWidth;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        var label = card.GetComponentInChildren<Text>();
        var hasSkillIcon = cardIndex == 0
            ? AddBasicAttackIconToActionCard(rect, canChooseAction, accent)
            : AddSkillIconToActionCard(rect, title, canChooseAction);
        if (label != null)
        {
            label.fontSize = 21;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 21;
            label.lineSpacing = 1.12f;
            label.alignment = hasSkillIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            Stretch(label.rectTransform, hasSkillIcon ? 140f : 18f, 12f, 18f, 14f);
        }

        var accentRail = AddFlatPanel("Action Card Accent", rect, new Color(accent.r, accent.g, accent.b, 0.88f));
        accentRail.anchorMin = new Vector2(0.16f, 0f);
        accentRail.anchorMax = new Vector2(0.84f, 0f);
        accentRail.pivot = new Vector2(0.5f, 0f);
        accentRail.sizeDelta = new Vector2(0f, 5f);
        accentRail.anchoredPosition = new Vector2(0f, 7f);
        accentRail.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        accentRail.GetComponent<Image>().raycastTarget = false;
        AddCombatCardRoleBadge(rect, roleLabel, accent);
        card.interactable = canChooseAction;
        ConfigureCombatActionCardMotion(card, cardIndex, cardCount, key, accent);
        return card;
    }

    private Button AddCombatLogToggle(Transform parent, bool canToggle)
    {
        var go = new GameObject("Compact Combat Log", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = new Color(0.92f, 0.97f, 1f, 0.30f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.interactable = canToggle;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.98f, 1f, 1f);
        colors.pressedColor = new Color(0.84f, 0.93f, 1f, 1f);
        colors.disabledColor = new Color(0.85f, 0.88f, 0.90f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            if (combatPresentationPhase != CombatPresentationPhase.PlayerChoice || actionLocked)
            {
                RejectCombatInput(CombatLockedMessage());
                return;
            }

            PlayUiClickSound();
            combatLogExpanded = !combatLogExpanded;
            ShowCombat();
        });

        var content = AddFlatPanel("Compact Combat Log Content", go.transform, Color.clear);
        content.GetComponent<Image>().raycastTarget = false;
        Stretch(content, 12f, 4f, 12f, 4f);
        AddVertical(content, 0, TextAnchor.UpperLeft, new RectOffset(2, 2, 0, 0));

        var heading = AddText(content, combatLogExpanded ? "전투 기록  ▴" : "전투 기록  ▾", 17, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 22);
        ConfigureCompactCombatLogText(heading, 17, 22);
        var previewLines = CombatLogPreviewLines();
        for (var index = 0; index < previewLines.Count; index++)
        {
            var line = AddText(content, previewLines[index], 16, index == previewLines.Count - 1 ? FontStyle.Bold : FontStyle.Normal, textColor, TextAnchor.MiddleLeft, 21);
            ConfigureCompactCombatLogText(line, 16, 21);
        }
        return button;
    }

    private void ConfigureCompactCombatLogText(Text text, int fontSize, float height)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.94f;
        AddLayoutSize(text.rectTransform, -1, height);
    }

    private System.Collections.Generic.List<string> CombatLogPreviewLines()
    {
        var lines = RecentCombatLogLines();
        var visible = new System.Collections.Generic.List<string>();
        if (lines.Count == 0)
        {
            visible.Add("아직 기록된 행동이 없습니다");
            return visible;
        }

        var visibleCount = combatLogExpanded ? Mathf.Min(3, lines.Count) : 1;
        var start = lines.Count - visibleCount;
        for (var index = start; index < lines.Count; index++)
        {
            var compact = (lines[index] ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            if (compact.Length > 40)
            {
                compact = compact.Substring(0, 39) + "…";
            }
            visible.Add(string.IsNullOrEmpty(compact) ? "기록 없음" : compact);
        }
        return visible;
    }

    private void AddHeroCombatArt(Transform parent)
    {
        var holder = AddFlatPanel("Hero Combat Art Slot", parent, new Color(0f, 0f, 0f, 0f));
        AddLayoutSize(holder, 480, 395);
        holder.GetComponent<Image>().raycastTarget = false;
        var heroGlowAlpha = combatPresentationPhase == CombatPresentationPhase.EnemyResolving ? 0.07f : 0.28f;
        var heroTheme = ActiveCharacterAccent(manaColor);
        AddCombatGroundShadow(holder, heroTheme);
        combatHeroRune = AddCombatGroundRune(
            holder,
            heroTheme,
            combatPresentationPhase == CombatPresentationPhase.PlayerChoice
                || combatPresentationPhase == CombatPresentationPhase.PlayerResolving,
            true);
        AddCharacterThemeHalo(holder, player.portraitName, 0.16f);
        combatHeroGlow = AddCombatGlow(holder, new Color(heroTheme.r, heroTheme.g, heroTheme.b, heroGlowAlpha));
        AddCombatPersistentStatusAura(holder, playerStatusEffects, heroTheme);
        combatHeroArt = AddFlatPanel("Hero Combat Art", holder, Color.clear);
        Stretch(combatHeroArt, 0, 0, 0, 0);
        combatHeroArt.GetComponent<Image>().raycastTarget = false;
        var visual = AddFlatPanel("Hero Sprite Visual", combatHeroArt, Color.white);
        Stretch(visual, 0, 0, 0, 0);
        combatHeroImage = visual.GetComponent<Image>();
        combatHeroImage.raycastTarget = false;
        combatHeroImage.preserveAspect = true;
        SetHeroCombatSprite("combat");
        ApplyCombatRimLight(combatHeroImage, heroTheme, true);
    }

    private void AddEnemyCombatArt(Transform parent)
    {
        var holder = AddFlatPanel("Enemy Combat Art Slot", parent, new Color(0f, 0f, 0f, 0f));
        AddLayoutSize(holder, 480, 395);
        holder.GetComponent<Image>().raycastTarget = false;
        var theme = currentEnemyIsBoss ? goldColor : EnemyThemeColor();
        var enemyTurn = combatPresentationPhase == CombatPresentationPhase.EnemyResolving;
        var glowAlpha = currentEnemyIsBoss ? (enemyTurn ? 0.38f : 0.20f) : (enemyTurn ? 0.27f : 0.07f);
        AddCombatGroundShadow(holder, theme);
        combatEnemyRune = AddCombatGroundRune(holder, theme, enemyTurn, false);
        combatEnemyGlow = AddCombatGlow(holder, new Color(theme.r, theme.g, theme.b, glowAlpha));
        AddCombatPersistentStatusAura(holder, enemyStatusEffects, theme);
        combatEnemyArt = AddFlatPanel("Enemy Combat Art", holder, Color.clear);
        Stretch(combatEnemyArt, 0, 0, 0, 0);
        combatEnemyArt.GetComponent<Image>().raycastTarget = false;
        var visual = AddFlatPanel("Enemy Sprite Visual", combatEnemyArt, Color.white);
        Stretch(visual, 0, 0, 0, 0);
        combatEnemyImage = visual.GetComponent<Image>();
        combatEnemyImage.raycastTarget = false;
        combatEnemyImage.preserveAspect = true;
        var sprite = LoadCurrentEnemySprite();
        ApplyCombatSprite(combatEnemyImage, sprite, currentEnemyIsBoss ? 1.15f : 1.10f, currentEnemyIsBoss ? 2.02f : 1.92f);
        ApplyCombatRimLight(combatEnemyImage, theme, false);
        if (combatEnemyImage.sprite == null)
        {
            var fallback = AddText(holder, currentEnemyIsBoss ? "BOSS" : "ENEMY", 30, FontStyle.Bold, dangerColor, TextAnchor.MiddleCenter, 395);
            Stretch(fallback.GetComponent<RectTransform>(), 0, 0, 0, 0);
        }
    }

    private void AddCombatGroundShadow(Transform parent, Color accent)
    {
        var shadow = AddFlatPanel("Combat Ground Shadow", parent, new Color(0.03f, 0.07f, 0.10f, 0.20f));
        shadow.anchorMin = new Vector2(0.14f, 0.015f);
        shadow.anchorMax = new Vector2(0.86f, 0.235f);
        shadow.offsetMin = Vector2.zero;
        shadow.offsetMax = Vector2.zero;
        var shadowImage = shadow.GetComponent<Image>();
        shadowImage.sprite = CombatGlowSprite();
        shadowImage.raycastTarget = false;

        var contact = AddFlatPanel("Combat Ground Contact", parent, new Color(accent.r, accent.g, accent.b, 0.18f));
        contact.anchorMin = new Vector2(0.28f, 0.04f);
        contact.anchorMax = new Vector2(0.72f, 0.16f);
        contact.offsetMin = Vector2.zero;
        contact.offsetMax = Vector2.zero;
        var contactImage = contact.GetComponent<Image>();
        contactImage.sprite = CombatGlowSprite();
        contactImage.raycastTarget = false;
        AddCombatGroundShadowDetail(parent, accent);
    }

    private void SetHeroCombatSprite(string state)
    {
        if (combatHeroImage == null)
        {
            return;
        }

        ApplyCombatSprite(combatHeroImage, LoadCharacterStateSprite(player.portraitName, state), 1.10f, 1.84f);
    }

    private static void ApplyCombatSprite(Image image, Sprite sprite, float targetVisibleHeight, float maxScale)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.color = sprite == null ? new Color(0f, 0f, 0f, 0f) : Color.white;
        image.rectTransform.localScale = Vector3.one;
        if (sprite == null || sprite.pixelsPerUnit <= 0f)
        {
            return;
        }

        var visibleHeight = sprite.bounds.size.y * sprite.pixelsPerUnit;
        if (visibleHeight <= 0f)
        {
            return;
        }

        var scale = Mathf.Clamp(sprite.rect.height * targetVisibleHeight / visibleHeight, 1f, maxScale);
        image.rectTransform.localScale = Vector3.one * scale;
    }

    private Image AddCombatGlow(Transform parent, Color color)
    {
        var glow = AddFlatPanel("Active Turn Glow", parent, color);
        Stretch(glow, 22, 18, 22, 12);
        var image = glow.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = CombatGlowSprite();
        image.preserveAspect = false;
        return image;
    }

    private Sprite CombatGlowSprite()
    {
        if (combatGlowSprite != null)
        {
            return combatGlowSprite;
        }

        const int size = 96;
        combatGlowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        combatGlowTexture.name = "Combat Radial Glow";
        var pixels = new Color32[size * size];
        var center = (size - 1) * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - center) / center;
                var dy = (y - center) / center;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                var alpha = (byte)Mathf.RoundToInt(Mathf.Pow(Mathf.Clamp01(1f - distance), 1.7f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        combatGlowTexture.SetPixels32(pixels);
        combatGlowTexture.Apply(false, true);
        combatGlowSprite = Sprite.Create(combatGlowTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return combatGlowSprite;
    }

    private CombatBarView AddCombatBar(Transform parent, int value, int max, Color color, string label)
    {
        var frame = AddPanel(label + " Combat Bar", parent, Rgb(25, 37, 50));
        frame.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(frame, -1, 30);
        var safeMax = Mathf.Max(0, max);
        var ratio = safeMax <= 0 ? 0f : Mathf.Clamp01(Mathf.Max(0, value) / (float)safeMax);

        var delayed = AddFlatPanel("Delayed Fill", frame, Color.Lerp(color, Color.white, 0.52f));
        delayed.GetComponent<Image>().raycastTarget = false;
        delayed.anchorMin = Vector2.zero;
        delayed.anchorMax = new Vector2(ratio, 1f);
        delayed.offsetMin = Vector2.zero;
        delayed.offsetMax = Vector2.zero;

        var primary = AddFlatPanel("Primary Fill", frame, color);
        primary.GetComponent<Image>().raycastTarget = false;
        primary.anchorMin = Vector2.zero;
        primary.anchorMax = new Vector2(ratio, 1f);
        primary.offsetMin = Vector2.zero;
        primary.offsetMax = Vector2.zero;

        var text = AddText(frame, label + "  " + Mathf.Max(0, value) + " / " + safeMax, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 30);
        Stretch(text.GetComponent<RectTransform>(), 0, 0, 0, 0);
        return new CombatBarView
        {
            primaryFill = primary,
            delayedFill = delayed,
            valueText = text,
            maxValue = safeMax
        };
    }

    private void AddCombatStatusSummary(Transform parent, System.Collections.Generic.List<StatusEffect> effects, Color accent)
    {
        if (effects == null || effects.Count == 0)
        {
            return;
        }

        if (TryAddGeneratedCombatStatusSummary(parent, effects, accent))
        {
            return;
        }

        var status = AddFlatPanel("Combat Status Chips", parent, new Color(accent.r, accent.g, accent.b, 0.30f));
        var statusImage = status.GetComponent<Image>();
        statusImage.sprite = MapRoundedRectSprite();
        statusImage.raycastTarget = false;
        AddLayoutSize(status, -1, 30);
        var label = StatusLine(effects);
        var text = AddText(status, label, 17, FontStyle.Bold, accent, TextAnchor.MiddleCenter, 30);
        Stretch(text.GetComponent<RectTransform>(), 8, 0, 8, 0);
    }

    private void AddCombatInputLockOverlay(RectTransform parent)
    {
        var overlayObject = new GameObject("Combat Input Lock", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        overlayObject.transform.SetParent(parent, false);
        var overlay = overlayObject.GetComponent<RectTransform>();
        Stretch(overlay, 0, 0, 0, 0);
        overlayObject.GetComponent<LayoutElement>().ignoreLayout = true;
        var image = overlayObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
        var button = overlayObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.onClick.AddListener(() => RejectCombatInput(CombatLockedMessage()));
        overlay.SetAsLastSibling();
    }

    private string EnemyCombatHint()
    {
        var style = currentEnemy.magic > currentEnemy.attack * 1.2f ? "마법형" : (currentEnemy.attack > currentEnemy.magic * 1.2f ? "물리형" : "혼합형");
        return style + "  ·  공격 " + currentEnemy.attack + "  마력 " + currentEnemy.magic + "  방어 " + currentEnemy.defense + "  속도 " + currentEnemy.speed;
    }

    private string EnemySpriteArchetype()
    {
        var name = currentEnemy != null ? currentEnemy.name : "";
        if (name.Contains("파수체 1")) return "elemental";
        if (name.Contains("파수체 2")) return "melee";
        if (name.Contains("파수체 3")) return "caster";
        if (ContainsEnemyKeyword(name, "고블린", "순찰병", "약탈자")) return "raider";
        if (ContainsEnemyKeyword(name, "좀비", "해골", "유령", "리치", "악령", "몽령")) return "undead";
        if (ContainsEnemyKeyword(name, "슬라임", "점액", "젤리")) return "slime_ooze";
        if (ContainsEnemyKeyword(name, "수렁", "만드라고라", "수액", "가시", "역병", "뿌리", "천독", "흑련")) return "plant_ooze";
        if (ContainsEnemyKeyword(name, "와이번", "천둥룡", "거인", "세라프", "리바이아", "뱀왕", "파괴 군주")) return "beast_colossus";
        if (ContainsEnemyKeyword(name, "늑대", "사냥개", "박쥐", "독사", "포식자", "괴수", "파편수")) return "beast_ground";
        if (ContainsEnemyKeyword(name, "골렘", "수호자", "파수핵", "수정체", "시계공", "대장장이", "제련수")) return "construct";
        if (ContainsEnemyKeyword(name, "사제", "마녀", "술사", "마도사", "서기관", "필경사", "사서", "관측자", "성녀", "여왕", "심판관", "기록관", "지휘자")) return "caster";
        if (ContainsEnemyKeyword(name, "정령", "파편", "핵", "심장", "폭풍", "소용돌이", "혜성", "별조각", "맥동체", "성좌", "태양", "잔재")) return "elemental";
        if (ContainsEnemyKeyword(name, "기사", "검사", "창병", "살수", "추적자", "병사", "집행자", "장군", "족장", "간수", "군주")) return "melee";
        return currentEnemy != null && currentEnemy.magic > currentEnemy.attack * 1.2f ? "caster" : "melee";
    }

    private Sprite LoadCurrentEnemySprite()
    {
        if (currentEnemy != null && !string.IsNullOrEmpty(currentEnemy.spriteKey))
        {
            var uniqueSprite = LoadGeneratedSprite("EnemiesV3/" + currentEnemy.spriteKey, Vector4.zero);
            if (uniqueSprite != null)
            {
                return uniqueSprite;
            }
        }

        return LoadGeneratedSprite("EnemiesV2/" + EnemySpriteArchetype(), Vector4.zero);
    }

    private static bool ContainsEnemyKeyword(string source, params string[] keywords)
    {
        for (var index = 0; index < keywords.Length; index++)
        {
            if (source.Contains(keywords[index]))
            {
                return true;
            }
        }
        return false;
    }

    private Color EnemyThemeColor()
    {
        var source = (currentDungeon != null ? currentDungeon.name : "") + " " + (currentEnemy != null ? currentEnemy.name : "");
        if (ContainsEnemyKeyword(source, "서리", "얼음", "빙", "오로라")) return Rgb(44, 139, 184);
        if (ContainsEnemyKeyword(source, "잿", "화산", "용광로", "붉은", "핏달")) return Rgb(214, 91, 56);
        if (ContainsEnemyKeyword(source, "독", "역병", "가시", "뿌리", "수액")) return Rgb(83, 139, 54);
        if (ContainsEnemyKeyword(source, "폭풍", "번개", "낙뢰", "천둥", "전류")) return Rgb(45, 117, 194);
        if (ContainsEnemyKeyword(source, "빛", "별", "성흔", "태양", "성좌")) return Rgb(190, 133, 36);
        if (ContainsEnemyKeyword(source, "공허", "심연", "흑", "어둠", "일식", "월식")) return Rgb(105, 73, 166);
        return dangerColor;
    }

    private int DisplayedCombatHp(bool playerSide, int currentHp)
    {
        if (pendingCombatPresentation == null)
        {
            return currentHp;
        }

        if (pendingCombatPresentation.targetIsPlayer == playerSide)
        {
            return pendingCombatPresentation.oldTargetHp;
        }

        if (pendingCombatPresentation.healAmount > 0 && pendingCombatPresentation.attackerIsPlayer == playerSide)
        {
            return Mathf.Max(0, currentHp - pendingCombatPresentation.healAmount);
        }

        return currentHp;
    }

    private string CombatPhaseTitle(CombatPresentationPhase phase)
    {
        switch (phase)
        {
            case CombatPresentationPhase.PlayerResolving: return "내 행동 · 연출 중";
            case CombatPresentationPhase.EnemyResolving: return "적 턴 · 행동 중";
            case CombatPresentationPhase.Result: return "전투 종료";
            default: return "내 턴 · 행동 선택";
        }
    }

    private string CombatPhaseActionPrompt(CombatPresentationPhase phase)
    {
        switch (phase)
        {
            case CombatPresentationPhase.PlayerResolving: return "선택한 행동을 처리하고 있습니다";
            case CombatPresentationPhase.EnemyResolving: return "적의 행동을 지켜보세요";
            case CombatPresentationPhase.Result: return "전투 결과를 확인하세요";
            default: return "공격 또는 스킬을 선택하세요";
        }
    }

    private string CombatPhaseCommandPrompt(CombatPresentationPhase phase)
    {
        switch (phase)
        {
            case CombatPresentationPhase.PlayerResolving: return "내 행동 처리 중 — 연출이 끝나면 적이 반격합니다";
            case CombatPresentationPhase.EnemyResolving: return "적 행동 중 — 연출이 끝나면 자동으로 내 턴이 됩니다";
            case CombatPresentationPhase.Result: return "전투가 종료되었습니다";
            default: return "행동 선택 — 적의 HP를 먼저 0으로 만들면 승리합니다";
        }
    }

    private string CombatLockedMessage()
    {
        return combatPresentationPhase == CombatPresentationPhase.PlayerResolving
            ? "내 행동을 처리하고 있습니다. 연출이 끝날 때까지 기다려 주세요."
            : "적이 행동하고 있습니다. 내 턴이 될 때까지 기다려 주세요.";
    }

    private Color CombatPhaseAccent(CombatPresentationPhase phase)
    {
        if (phase == CombatPresentationPhase.EnemyResolving)
        {
            return dangerColor;
        }
        if (phase == CombatPresentationPhase.Result)
        {
            return goldColor;
        }
        return phase == CombatPresentationPhase.PlayerResolving ? manaColor : goodColor;
    }

    private Color CombatPhaseBackground(CombatPresentationPhase phase)
    {
        if (phase == CombatPresentationPhase.EnemyResolving)
        {
            return new Color(1f, 231f / 255f, 234f / 255f, 0.30f);
        }
        if (phase == CombatPresentationPhase.Result)
        {
            return new Color(1f, 247f / 255f, 218f / 255f, 0.30f);
        }
        return new Color(224f / 255f, 245f / 255f, 252f / 255f, 0.30f);
    }

    private void StartPlayerResolvedPresentation(CombatPresentationEvent presentation)
    {
        SetCombatPresentationPhase(CombatPresentationPhase.PlayerResolving, presentation.actionName);
        pendingCombatPresentation = presentation;
        ShowCombat();
        StartCombatSequence(PlayerPresentationThenEnemy(presentation, currentEnemy));
    }

    private IEnumerator PlayerPresentationThenEnemy(CombatPresentationEvent presentation, EnemyState enemySnapshot)
    {
        yield return PlayCombatPresentation(presentation);
        if (!IsSameCombat(enemySnapshot))
        {
            yield break;
        }

        pendingCombatPresentation = null;
        if (presentation.targetDefeated)
        {
            yield return WaitForCombatSeconds(0.22f);
            if (presentation.targetIsPlayer)
            {
                LoseCombat();
            }
            else
            {
                WinCombat();
            }
            yield break;
        }

        SetCombatPresentationPhase(CombatPresentationPhase.EnemyResolving, "적 턴 · 반격 준비");
        PlayTurnSound(true);
        yield return EnemyTurn();
    }

    private void StartEnemyOnlyPresentation()
    {
        SetCombatPresentationPhase(CombatPresentationPhase.EnemyResolving, "적의 선제 공격");
        StartCombatSequence(EnemyTurn());
    }

    private void StartCombatSequence(IEnumerator sequence)
    {
        if (combatSequence != null)
        {
            StopCoroutine(combatSequence);
        }
        var version = ++combatSequenceVersion;
        combatSequence = StartCoroutine(CombatSequenceWrapper(sequence, version));
    }

    private IEnumerator CombatSequenceWrapper(IEnumerator sequence, int version)
    {
        try
        {
            yield return sequence;
        }
        finally
        {
            if (version == combatSequenceVersion)
            {
                combatSequence = null;
            }
        }
    }

    private IEnumerator ShowEnemyResolvedPresentation(CombatPresentationEvent presentation)
    {
        SetCombatPresentationPhase(CombatPresentationPhase.EnemyResolving, presentation.actionName);
        pendingCombatPresentation = presentation;
        ShowCombat();
        yield return PlayCombatPresentation(presentation);
        pendingCombatPresentation = null;
    }

    private void CompleteEnemyTurnPresentation()
    {
        pendingCombatPresentation = null;
        actionLocked = false;
        SetCombatPresentationPhase(CombatPresentationPhase.PlayerChoice, "공격 또는 스킬을 선택하세요");
        player.mp = Mathf.Min(MaxMp(), player.mp + PlayerTurnManaRegenAmount());
        ShowCombat();
        PlayTurnSound(false);
    }

    private IEnumerator PlayCombatPresentation(CombatPresentationEvent presentation)
    {
        var generation = combatUiGeneration;
        if (!CombatViewIsValid(generation))
        {
            yield break;
        }

        SetCombatPresentationPhase(
            presentation.attackerIsPlayer ? CombatPresentationPhase.PlayerResolving : CombatPresentationPhase.EnemyResolving,
            presentation.actionName);
        var attacker = presentation.attackerIsPlayer ? combatHeroArt : combatEnemyArt;
        var target = presentation.targetIsPlayer ? combatHeroArt : combatEnemyArt;
        var targetBar = presentation.targetIsPlayer ? combatHeroHpBar : combatEnemyHpBar;
        var attackerGlow = presentation.attackerIsPlayer ? combatHeroGlow : combatEnemyGlow;
        if (attacker == null || target == null)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        var attackerStart = attacker.anchoredPosition;
        var targetStart = target.anchoredPosition;
        var glowStart = attackerGlow != null ? attackerGlow.color : Color.clear;

        if (presentation.statusOnly)
        {
            if (presentation.damage > 0)
            {
                PlayCombatHitSound(false);
                StartCoroutine(PlayCombatStatusFx(presentation, generation));
                yield return AnimateCombatImpact(presentation, target, targetStart, targetBar, generation);
            }
            else if (presentation.absorbedDamage > 0)
            {
                PlayCombatHitSound(false);
                StartCoroutine(PlayCombatShieldImpactFx(presentation, generation));
                yield return AnimateCombatImpact(presentation, target, targetStart, targetBar, generation);
            }
            else
            {
                yield return PlayCombatStatusFx(presentation, generation);
                yield return AnimateFloatingOnly(presentation.targetIsPlayer, "행동 불가", CombatStatusFxColor(presentation.statusType), generation);
            }
        }
        else if (presentation.buff)
        {
            PlayCombatActionSound(true);
            if (presentation.attackerIsPlayer && combatHeroImage != null)
            {
                SetHeroCombatSprite("skill");
            }
            yield return PlayClassCombatSupportFx(presentation, presentation.healAmount > 0, generation);
            yield return PulseCombatant(attacker, attackerGlow, glowStart, generation);
            yield return AnimateFloatingOnly(presentation.attackerIsPlayer, CombatSupportLabel(presentation), CombatSupportFxColor(presentation), generation);
        }
        else
        {
            if (presentation.attackerIsPlayer && combatHeroImage != null)
            {
                SetHeroCombatSprite(presentation.magic || presentation.actionName != "기본 공격" ? "skill" : "combat");
            }

            PlayCombatActionSound(presentation.magic);
            yield return PlayCombatTelegraphFx(presentation, generation);
            yield return PrepareCombatant(attacker, attackerGlow, glowStart, presentation.magic, generation);
            var travel = presentation.magic ? 72f : 168f;
            var direction = presentation.attackerIsPlayer ? 1f : -1f;
            yield return MoveCombatant(attacker, attackerStart, attackerStart + new Vector2(travel * direction, presentation.magic ? 10f : 0f), 0.17f, generation);
            yield return PlayClassCombatAttackFx(presentation, generation);

            if (presentation.missed)
            {
                PlayCombatMissSound();
                yield return AnimateFloatingOnly(presentation.targetIsPlayer, "회피!", manaColor, generation);
            }
            else
            {
                PlayCombatHitSound(presentation.critical);
                if (presentation.damage <= 0 && presentation.absorbedDamage > 0)
                {
                    StartCoroutine(PlayCombatShieldImpactFx(presentation, generation));
                }
                else
                {
                    StartCoroutine(PlayClassCombatImpactFx(presentation, generation));
                }
                yield return WaitForCombatSeconds(presentation.critical ? 0.105f : 0.072f);
                yield return AnimateCombatImpact(presentation, target, targetStart, targetBar, generation);
            }
            yield return MoveCombatant(attacker, attacker.anchoredPosition, attackerStart, 0.18f, generation);
        }

        if (presentation.healAmount > 0 && CombatViewIsValid(generation))
        {
            PlayCombatHealSound();
            if (!presentation.buff)
            {
                yield return PlayClassCombatSupportFx(presentation, true, generation);
            }
            yield return AnimateFloatingOnly(presentation.attackerIsPlayer, "+" + presentation.healAmount, goodColor, generation);
            var healingBar = presentation.attackerIsPlayer ? combatHeroHpBar : combatEnemyHpBar;
            var healedHp = presentation.attackerIsPlayer
                ? (player != null ? player.hp : presentation.healAmount)
                : (currentEnemy != null ? currentEnemy.hp : presentation.healAmount);
            yield return TweenCombatBarValue(
                healingBar,
                Mathf.Max(0, healedHp - presentation.healAmount),
                healedHp,
                0.28f,
                generation);
        }

        if (presentation.healAmount <= 0
            && !presentation.buff
            && !string.IsNullOrEmpty(presentation.supportType)
            && CombatViewIsValid(generation))
        {
            yield return PlayClassCombatSupportFx(presentation, false, generation);
            yield return AnimateFloatingOnly(
                presentation.attackerIsPlayer,
                CombatSupportLabel(presentation),
                CombatSupportFxColor(presentation),
                generation);
        }

        if (CombatViewIsValid(generation) && attacker != null)
        {
            attacker.anchoredPosition = attackerStart;
            attacker.localScale = Vector3.one;
        }
        if (CombatViewIsValid(generation) && attackerGlow != null)
        {
            attackerGlow.color = glowStart;
        }
        if (presentation.attackerIsPlayer && combatHeroImage != null && !presentation.targetIsPlayer)
        {
            SetHeroCombatSprite("combat");
        }

        if (presentation.targetDefeated && CombatViewIsValid(generation))
        {
            yield return AnimateCombatDefeat(presentation.targetIsPlayer, generation);
        }
    }

    private IEnumerator PrepareCombatant(RectTransform art, Image glow, Color baseGlow, bool magic, int generation)
    {
        const float duration = 0.18f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && art != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var pulse = Mathf.Sin(progress * Mathf.PI);
            art.localScale = Vector3.one * (1f + pulse * (magic ? 0.045f : 0.035f));
            if (glow != null)
            {
                glow.color = new Color(baseGlow.r, baseGlow.g, baseGlow.b, Mathf.Clamp01(baseGlow.a + pulse * 0.34f));
            }
            yield return null;
        }
    }

    private IEnumerator PulseCombatant(RectTransform art, Image glow, Color baseGlow, int generation)
    {
        const float duration = 0.42f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && art != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var pulse = Mathf.Sin(progress * Mathf.PI * 2f) * (1f - progress);
            art.localScale = Vector3.one * (1f + pulse * 0.045f);
            if (glow != null)
            {
                glow.color = new Color(baseGlow.r, baseGlow.g, baseGlow.b, Mathf.Clamp01(baseGlow.a + Mathf.Abs(pulse) * 0.48f));
            }
            yield return null;
        }
        if (CombatViewIsValid(generation) && art != null) art.localScale = Vector3.one;
        if (CombatViewIsValid(generation) && glow != null) glow.color = baseGlow;
    }

    private IEnumerator MoveCombatant(RectTransform art, Vector2 from, Vector2 to, float duration, int generation)
    {
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && art != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            progress = 1f - Mathf.Pow(1f - progress, 3f);
            art.anchoredPosition = Vector2.LerpUnclamped(from, to, progress);
            yield return null;
        }
        if (CombatViewIsValid(generation) && art != null) art.anchoredPosition = to;
    }

    private IEnumerator AnimateCombatImpact(CombatPresentationEvent presentation, RectTransform target, Vector2 targetStart, CombatBarView bar, int generation)
    {
        var fullyBlocked = presentation.damage <= 0 && presentation.absorbedDamage > 0;
        var floating = CreateCombatFloatingText(
            presentation.targetIsPlayer,
            fullyBlocked
                ? "막음\n" + presentation.absorbedDamage
                : (presentation.critical ? "치명타!\n-" + presentation.damage : "-" + presentation.damage),
            fullyBlocked ? manaColor : (presentation.critical ? goldColor : dangerColor));
        var floatingStart = floating != null ? floating.rectTransform.anchoredPosition : Vector2.zero;
        var stageStart = combatStageContent != null ? combatStageContent.anchoredPosition : Vector2.zero;
        var flashColor = CombatFxImpactFlashColor(presentation);
        var shakeMultiplier = CombatFxShakeMultiplier(presentation);
        var duration = presentation.critical ? 0.54f : (fullyBlocked ? 0.42f : 0.47f);
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var impactProgress = Mathf.Clamp01((progress - (presentation.critical ? 0.12f : 0.08f)) / (presentation.critical ? 0.88f : 0.92f));
            var kickCurve = Mathf.Sin(impactProgress * Mathf.PI) * (1f - impactProgress * 0.42f);
            var side = presentation.targetIsPlayer ? -1f : 1f;
            var strength = (fullyBlocked ? 5f : (presentation.critical ? 20f : 12f)) * shakeMultiplier;
            target.anchoredPosition = targetStart + new Vector2(side * strength * kickCurve, Mathf.Sin(impactProgress * Mathf.PI * 2f) * strength * 0.16f * (1f - impactProgress));
            if (combatStageContent != null)
            {
                var cameraStrength = (fullyBlocked ? 1.5f : (presentation.critical ? 8f : 4f)) * shakeMultiplier;
                combatStageContent.anchoredPosition = stageStart + new Vector2(-side * cameraStrength * kickCurve, 0f);
            }
            if (combatImpactFlash != null)
            {
                var flashFade = Mathf.Clamp01(1f - progress * (fullyBlocked ? 3.4f : 2.6f));
                combatImpactFlash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * flashFade * (fullyBlocked ? 0.52f : 1f));
            }
            if (floating != null)
            {
                floating.rectTransform.anchoredPosition = floatingStart + new Vector2(side * progress * 18f, progress * 88f);
                var textColorNow = floating.color;
                textColorNow.a = Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.58f) / 0.42f);
                floating.color = textColorNow;
                var textPop = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress * 4f), 3f);
                floating.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.66f, presentation.critical ? 1.32f : 1.10f, textPop);
            }
            SetCombatBarPrimary(bar, Mathf.RoundToInt(Mathf.Lerp(presentation.oldTargetHp, presentation.newTargetHp, Mathf.Clamp01(progress * 1.8f))));
            yield return null;
        }

        if (CombatViewIsValid(generation))
        {
            if (target != null) target.anchoredPosition = targetStart;
            if (combatStageContent != null) combatStageContent.anchoredPosition = stageStart;
            if (combatImpactFlash != null) combatImpactFlash.color = new Color(1f, 1f, 1f, 0f);
            SetCombatBarPrimary(bar, presentation.newTargetHp);
            yield return WaitForCombatSeconds(presentation.critical ? 0.14f : 0.10f);
            yield return TweenCombatBarDelayed(bar, presentation.oldTargetHp, presentation.newTargetHp, 0.34f, generation);
        }
        if (floating != null) Destroy(floating.gameObject);
    }

    private IEnumerator TweenCombatBarDelayed(CombatBarView bar, int from, int to, float duration, int generation)
    {
        if (bar == null || bar.delayedFill == null)
        {
            yield break;
        }
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && bar.delayedFill != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);
            SetCombatBarFill(bar.delayedFill, Mathf.RoundToInt(Mathf.Lerp(from, to, progress)), bar.maxValue);
            yield return null;
        }
        if (CombatViewIsValid(generation) && bar.delayedFill != null)
        {
            SetCombatBarFill(bar.delayedFill, to, bar.maxValue);
        }
    }

    private IEnumerator TweenCombatBarValue(CombatBarView bar, int from, int to, float duration, int generation)
    {
        if (bar == null)
        {
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation))
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);
            var value = Mathf.RoundToInt(Mathf.Lerp(from, to, progress));
            SetCombatBarPrimary(bar, value);
            SetCombatBarFill(bar.delayedFill, value, bar.maxValue);
            yield return null;
        }

        if (CombatViewIsValid(generation))
        {
            SetCombatBarPrimary(bar, to);
            SetCombatBarFill(bar.delayedFill, to, bar.maxValue);
        }
    }

    private void SetCombatBarPrimary(CombatBarView bar, int value)
    {
        if (bar == null)
        {
            return;
        }
        SetCombatBarFill(bar.primaryFill, value, bar.maxValue);
        if (bar.valueText != null)
        {
            bar.valueText.text = "HP  " + Mathf.Max(0, value) + " / " + bar.maxValue;
        }
    }

    private static void SetCombatBarFill(RectTransform fill, int value, int max)
    {
        if (fill == null)
        {
            return;
        }
        var ratio = max <= 0 ? 0f : Mathf.Clamp01(Mathf.Max(0, value) / (float)max);
        fill.anchorMax = new Vector2(ratio, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
    }

    private Text CreateCombatFloatingText(bool targetIsPlayer, string value, Color color)
    {
        if (combatFxLayer == null)
        {
            return null;
        }
        var go = new GameObject("Combat Floating Text", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
        go.transform.SetParent(combatFxLayer, false);
        var rect = go.GetComponent<RectTransform>();
        var anchor = CombatFxAnchorFor(targetIsPlayer, 0.035f);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(330, 120);
        rect.anchoredPosition = Vector2.zero;
        var text = go.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = EnsureHighContrastTextColor(color);
        text.text = value;
        text.raycastTarget = false;
        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.01f, 0.02f, 0.035f, 0.98f);
        outline.effectDistance = new Vector2(2f, -2f);
        var shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private IEnumerator AnimateFloatingOnly(bool targetIsPlayer, string value, Color color, int generation)
    {
        var floating = CreateCombatFloatingText(targetIsPlayer, value, color);
        if (floating == null)
        {
            yield break;
        }
        var start = floating.rectTransform.anchoredPosition;
        const float duration = 0.62f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && floating != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            floating.rectTransform.anchoredPosition = start + new Vector2(0f, progress * 66f);
            floating.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(progress * Mathf.PI) * 0.16f);
            var current = floating.color;
            current.a = Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.62f) / 0.38f);
            floating.color = current;
            yield return null;
        }
        if (floating != null) Destroy(floating.gameObject);
    }

    private IEnumerator AnimateCombatDefeat(bool playerDefeated, int generation)
    {
        StartCoroutine(PlayCombatVictoryFx(playerDefeated, generation));
        if (playerDefeated && combatHeroImage != null)
        {
            SetHeroCombatSprite("defeat");
            SetCombatPresentationPhase(CombatPresentationPhase.Result, "쓰러졌습니다");
        }
        else if (!playerDefeated && combatHeroImage != null)
        {
            SetHeroCombatSprite("victory");
            SetCombatPresentationPhase(CombatPresentationPhase.Result, "승리!");
        }

        var defeatedArt = playerDefeated ? combatHeroArt : combatEnemyArt;
        var defeatedImage = playerDefeated ? combatHeroImage : combatEnemyImage;
        if (defeatedArt == null || defeatedImage == null)
        {
            yield break;
        }
        var original = defeatedImage.color;
        const float duration = 0.42f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && defeatedArt != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            defeatedArt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * (playerDefeated ? 0.92f : 0.80f), progress);
            if (!playerDefeated)
            {
                defeatedImage.color = new Color(original.r, original.g, original.b, Mathf.Lerp(1f, 0.18f, progress));
            }
            yield return null;
        }
    }

    private IEnumerator WaitForCombatSeconds(float duration)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetCombatPresentationPhase(CombatPresentationPhase phase, string action)
    {
        combatPresentationPhase = phase;
        if (combatTurnText != null)
        {
            combatTurnText.text = CombatPhaseTitle(phase);
            combatTurnText.color = EnsureHighContrastTextColor(CombatPhaseAccent(phase));
        }
        if (combatTurnPillImage != null)
        {
            combatTurnPillImage.color = CombatPhaseBackground(phase);
        }
        if (combatActionText != null && !string.IsNullOrEmpty(action))
        {
            combatActionText.text = action;
            combatActionText.color = EnsureHighContrastTextColor(CombatPhaseAccent(phase));
        }
        if (combatCommandText != null)
        {
            combatCommandText.text = CombatPhaseCommandPrompt(phase);
            combatCommandText.color = EnsureHighContrastTextColor(CombatPhaseAccent(phase));
        }
        if (combatHeroGlow != null)
        {
            var alpha = phase == CombatPresentationPhase.EnemyResolving ? 0.07f : (phase == CombatPresentationPhase.Result ? 0.16f : 0.28f);
            var heroTheme = ActiveCharacterAccent(manaColor);
            combatHeroGlow.color = new Color(heroTheme.r, heroTheme.g, heroTheme.b, alpha);
        }
        if (combatEnemyGlow != null)
        {
            var theme = currentEnemyIsBoss ? goldColor : EnemyThemeColor();
            var alpha = currentEnemyIsBoss
                ? (phase == CombatPresentationPhase.EnemyResolving ? 0.38f : 0.20f)
                : (phase == CombatPresentationPhase.EnemyResolving ? 0.27f : 0.07f);
            combatEnemyGlow.color = new Color(theme.r, theme.g, theme.b, alpha);
        }
        RefreshCombatTurnFxPhase(phase);
    }

    private void RejectCombatInput(string message)
    {
        if (Time.unscaledTime >= nextCombatRejectTime)
        {
            nextCombatRejectTime = Time.unscaledTime + 0.12f;
            PlayRejectSound();
        }
        if (combatActionText != null)
        {
            combatActionText.text = message;
            combatActionText.color = EnsureHighContrastTextColor(dangerColor);
        }
    }

    private bool IsSameCombat(EnemyState enemySnapshot)
    {
        return currentScreen == AetheriaScreen.Combat && currentEnemy != null && currentEnemy == enemySnapshot;
    }

    private bool CombatViewIsValid(int generation)
    {
        return generation == combatUiGeneration
            && currentScreen == AetheriaScreen.Combat
            && currentEnemy != null
            && combatStageContent != null
            && combatFxLayer != null;
    }

    private void InvalidateCombatView()
    {
        combatUiGeneration++;
        combatStageContent = null;
        combatFxLayer = null;
        combatHeroArt = null;
        combatEnemyArt = null;
        combatHeroPanel = null;
        combatEnemyPanel = null;
        combatHeroImage = null;
        combatEnemyImage = null;
        combatHeroGlow = null;
        combatEnemyGlow = null;
        combatImpactFlash = null;
        combatTurnPillImage = null;
        combatTurnText = null;
        combatActionText = null;
        combatCommandText = null;
        combatHeroHpBar = null;
        combatEnemyHpBar = null;
        combatHeroRune = null;
        combatEnemyRune = null;
        combatEnemyIntentIcon = null;
        combatEnemyIntentIconImage = null;
        InvalidateCombatCardMotionView();
    }

    private void CancelCombatPresentation()
    {
        combatSequenceVersion++;
        if (combatSequence != null)
        {
            StopCoroutine(combatSequence);
            combatSequence = null;
        }
        pendingCombatPresentation = null;
        combatPresentationPhase = CombatPresentationPhase.PlayerChoice;
        nextCombatRejectTime = 0f;
        combatLogExpanded = false;
        InvalidateCombatView();
    }

    private void ClearPendingCombatPresentation()
    {
        pendingCombatPresentation = null;
    }

    private void ReleaseCombatPresentationResources()
    {
        ReleaseCombatCardMotionResources();

        if (combatGlowSprite != null)
        {
            Destroy(combatGlowSprite);
            combatGlowSprite = null;
        }

        if (combatGlowTexture != null)
        {
            Destroy(combatGlowTexture);
            combatGlowTexture = null;
        }
    }
}
