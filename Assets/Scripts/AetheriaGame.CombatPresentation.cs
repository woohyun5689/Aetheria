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
        public int healAmount;
        public int oldTargetHp;
        public int newTargetHp;
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

    private void BuildCombatScreen(bool resultBackdrop)
    {
        var phase = resultBackdrop ? CombatPresentationPhase.Result : combatPresentationPhase;
        var canChooseAction = phase == CombatPresentationPhase.PlayerChoice && !actionLocked && !resultBackdrop;
        var page = AddPanel("Combat", root, pageColor);
        Stretch(page, 0, 0, 0, 0);
        AddVertical(page, 12, TextAnchor.UpperCenter, new RectOffset(28, 28, 18, 18));

        var header = AddPanel("Combat Header", page, Rgba(255, 252, 244, 248));
        AddLayoutSize(header, -1, 64);
        var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerRow.spacing = 14;
        headerRow.padding = new RectOffset(18, 18, 8, 8);
        headerRow.childAlignment = TextAnchor.MiddleCenter;
        headerRow.childControlWidth = true;
        headerRow.childControlHeight = true;
        headerRow.childForceExpandWidth = false;
        AddText(
            header,
            (currentDungeon != null ? currentDungeon.name + "  " + currentDungeonFloor + "/" + DungeonFloorCount(currentDungeon) + "층" : "차원 전투")
                + (currentEnemyIsBoss ? "  ·  보스전" : "  ·  일반 전투"),
            28,
            FontStyle.Bold,
            textColor,
            TextAnchor.MiddleLeft,
            46);

        var turnPill = AddPanel("Turn Banner", header, CombatPhaseBackground(phase));
        combatTurnPillImage = turnPill.GetComponent<Image>();
        AddLayoutSize(turnPill, 254, 46);
        combatTurnText = AddText(
            turnPill,
            CombatPhaseTitle(phase),
            21,
            FontStyle.Bold,
            CombatPhaseAccent(phase),
            TextAnchor.MiddleCenter,
            46);
        Stretch(combatTurnText.GetComponent<RectTransform>(), 8, 0, 8, 0);
        var retreat = AddButton(header, "마을로 후퇴", ExitDungeon, dangerColor);
        AddLayoutSize(retreat.GetComponent<RectTransform>(), 210, 48);
        retreat.interactable = canChooseAction;

        var arena = AddPanel("Combat Arena", page, Rgba(238, 247, 252, 222));
        AddLayoutSize(arena, -1, 520);
        var arenaImage = arena.GetComponent<Image>();
        arenaImage.raycastTarget = false;

        combatStageContent = AddFlatPanel("Combat Stage Content", arena, new Color(0f, 0f, 0f, 0f));
        Stretch(combatStageContent, 18, 12, 18, 12);
        combatStageContent.GetComponent<Image>().raycastTarget = false;
        var arenaRow = combatStageContent.gameObject.AddComponent<HorizontalLayoutGroup>();
        arenaRow.spacing = 18;
        arenaRow.padding = new RectOffset(8, 8, 8, 8);
        arenaRow.childAlignment = TextAnchor.MiddleCenter;
        arenaRow.childControlWidth = true;
        arenaRow.childControlHeight = true;
        arenaRow.childForceExpandWidth = true;
        arenaRow.childForceExpandHeight = true;

        combatHeroPanel = AddPanel("Player Combatant", combatStageContent, Rgba(231, 247, 252, 239));
        AddSideAccent(combatHeroPanel, ActiveCharacterAccent(manaColor));
        AddLayoutSize(combatHeroPanel, 690, -1);
        AddVertical(combatHeroPanel, 5, TextAnchor.UpperCenter, new RectOffset(16, 16, 10, 10));
        AddHeroCombatArt(combatHeroPanel);
        AddText(combatHeroPanel, player.heroName + "  Lv." + player.level + "  " + player.heroClass, 26, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 34);
        var displayedHeroHp = DisplayedCombatHp(true, player.hp);
        combatHeroHpBar = AddCombatBar(combatHeroPanel, displayedHeroHp, MaxHp(), dangerColor, "HP");
        if (MaxMp() > 0)
        {
            AddCombatBar(combatHeroPanel, player.mp, MaxMp(), manaColor, "MP");
        }
        AddCombatStatusSummary(combatHeroPanel, playerStatusEffects, manaColor);
        AddText(combatHeroPanel, "공격 " + Attack() + "  마력 " + Magic() + "  방어 " + Defense() + "  속도 " + Speed(), 18, FontStyle.Bold, mutedColor, TextAnchor.MiddleCenter, 27);

        var versus = AddPanel("Combat Center", combatStageContent, Rgba(250, 247, 238, 242));
        AddLayoutSize(versus, 190, -1);
        AddVertical(versus, 9, TextAnchor.MiddleCenter, new RectOffset(10, 10, 16, 16));
        AddText(versus, currentEnemyIsBoss ? "BOSS" : "BATTLE", 20, FontStyle.Bold, currentEnemyIsBoss ? goldColor : neonPurple, TextAnchor.MiddleCenter, 34);
        AddText(versus, "VS", 46, FontStyle.Bold, neonPurple, TextAnchor.MiddleCenter, 68);
        combatActionText = AddText(
            versus,
            pendingCombatPresentation != null
                ? pendingCombatPresentation.actionName
                : CombatPhaseActionPrompt(phase),
            20,
            FontStyle.Bold,
            CombatPhaseAccent(phase),
            TextAnchor.MiddleCenter,
            86);
        AddDivider(versus, Rgba(116, 137, 158, 120));
        AddText(versus, "턴제 전투\nHP 0 = 패배", 16, FontStyle.Bold, mutedColor, TextAnchor.MiddleCenter, 76);

        combatEnemyPanel = AddPanel("Enemy Combatant", combatStageContent, Rgba(255, 238, 240, 239));
        AddSideAccent(combatEnemyPanel, currentEnemyIsBoss ? goldColor : dangerColor);
        AddLayoutSize(combatEnemyPanel, 690, -1);
        AddVertical(combatEnemyPanel, 5, TextAnchor.UpperCenter, new RectOffset(16, 16, 10, 10));
        AddEnemyCombatArt(combatEnemyPanel);
        AddText(combatEnemyPanel, currentEnemyIsBoss ? "BOSS  ·  " + currentEnemy.name : currentEnemy.name, 27, FontStyle.Bold, currentEnemyIsBoss ? goldColor : dangerColor, TextAnchor.MiddleCenter, 34);
        var displayedEnemyHp = DisplayedCombatHp(false, currentEnemy.hp);
        combatEnemyHpBar = AddCombatBar(combatEnemyPanel, displayedEnemyHp, currentEnemy.maxHp, currentEnemyIsBoss ? goldColor : dangerColor, "HP");
        if (currentEnemy.maxMp > 0)
        {
            AddCombatBar(combatEnemyPanel, currentEnemy.mp, currentEnemy.maxMp, manaColor, "MP");
        }
        AddCombatStatusSummary(combatEnemyPanel, enemyStatusEffects, goldColor);
        AddText(combatEnemyPanel, EnemyCombatHint(), 18, FontStyle.Bold, mutedColor, TextAnchor.MiddleCenter, 27);

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

        var commandArea = AddRow("Combat Console", page, 14, TextAnchor.UpperCenter);
        AddLayoutSize(commandArea, -1, 392);

        var commands = AddPanel("Action Deck", commandArea, Rgba(255, 252, 244, 248));
        AddLayoutSize(commands, 1080, -1);
        AddVertical(commands, 7, TextAnchor.UpperCenter, new RectOffset(16, 16, 12, 12));
        combatCommandText = AddText(
            commands,
            CombatPhaseCommandPrompt(phase),
            23,
            FontStyle.Bold,
            CombatPhaseAccent(phase),
            TextAnchor.MiddleCenter,
            32);
        AddText(commands, "Q 기본 공격 · W/E/R/T 스킬 · 스킬은 MP 소모 · ESC 후퇴", 17, FontStyle.Bold, mutedColor, TextAnchor.MiddleCenter, 27);

        var commandButtons = AddPanel("Action Button Grid", commands, new Color(0f, 0f, 0f, 0f));
        AddLayoutSize(commandButtons, -1, -1);
        var commandGrid = commandButtons.gameObject.AddComponent<GridLayoutGroup>();
        commandGrid.cellSize = new Vector2(510, 72);
        commandGrid.spacing = new Vector2(12, 10);
        commandGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        commandGrid.constraintCount = 2;
        commandGrid.childAlignment = TextAnchor.UpperCenter;
        commandGrid.padding = new RectOffset(0, 0, 2, 0);

        var basicAttack = AddButton(commandButtons, "Q  기본 공격\nMP 0 · 안정적인 기본 행동", () => PlayerAttack("기본 공격", 1.0f, 0, BasicAttackUsesMagic()), goodColor);
        basicAttack.interactable = canChooseAction;
        var playerSilenced = HasStatus(playerStatusEffects, "silence");
        var skillIndex = 0;
        foreach (var skill in ScaledSkillsForPlayer())
        {
            var localSkill = skill;
            var key = skillIndex == 0 ? "W " : (skillIndex == 1 ? "E " : (skillIndex == 2 ? "R " : "T "));
            var unavailable = playerSilenced || player.mp < localSkill.mpCost;
            var reason = playerSilenced ? " · 침묵" : (player.mp < localSkill.mpCost ? " · MP 부족" : "");
            var skillButton = AddButton(commandButtons, SkillCombatButtonLabel(key, localSkill) + reason, () => PlayerAttack(localSkill), unavailable ? panelAltColor : manaColor);
            skillButton.interactable = canChooseAction;
            skillIndex++;
        }
        var exit = AddButton(commandButtons, "ESC  마을로 후퇴\n현재 전투를 종료합니다", ExitDungeon, dangerColor);
        exit.interactable = canChooseAction;

        if (!canChooseAction && !resultBackdrop)
        {
            AddCombatInputLockOverlay(commandButtons);
        }

        var logPanel = AddPanel("Combat Log", commandArea, Rgba(255, 252, 244, 248));
        AddLayoutSize(logPanel, -1, -1);
        AddVertical(logPanel, 7, TextAnchor.UpperLeft, new RectOffset(18, 18, 14, 14));
        AddText(logPanel, "전투 기록 · 최근 행동", 24, FontStyle.Bold, manaColor, TextAnchor.MiddleLeft, 36);
        AddCombatLogList(logPanel);
    }

    private void AddHeroCombatArt(Transform parent)
    {
        var holder = AddFlatPanel("Hero Combat Art Slot", parent, new Color(0f, 0f, 0f, 0f));
        AddLayoutSize(holder, 330, 285);
        holder.GetComponent<Image>().raycastTarget = false;
        var heroGlowAlpha = combatPresentationPhase == CombatPresentationPhase.EnemyResolving ? 0.07f : 0.28f;
        var heroTheme = ActiveCharacterAccent(manaColor);
        AddCharacterThemeHalo(holder, player.portraitName, 0.20f);
        combatHeroGlow = AddCombatGlow(holder, new Color(heroTheme.r, heroTheme.g, heroTheme.b, heroGlowAlpha));
        combatHeroArt = AddFlatPanel("Hero Combat Art", holder, Color.clear);
        Stretch(combatHeroArt, 0, 0, 0, 0);
        combatHeroArt.GetComponent<Image>().raycastTarget = false;
        var visual = AddFlatPanel("Hero Sprite Visual", combatHeroArt, Color.white);
        Stretch(visual, 0, 0, 0, 0);
        combatHeroImage = visual.GetComponent<Image>();
        combatHeroImage.raycastTarget = false;
        combatHeroImage.preserveAspect = true;
        SetHeroCombatSprite("combat");
    }

    private void AddEnemyCombatArt(Transform parent)
    {
        var holder = AddFlatPanel("Enemy Combat Art Slot", parent, new Color(0f, 0f, 0f, 0f));
        AddLayoutSize(holder, 330, 285);
        holder.GetComponent<Image>().raycastTarget = false;
        var theme = currentEnemyIsBoss ? goldColor : EnemyThemeColor();
        var enemyTurn = combatPresentationPhase == CombatPresentationPhase.EnemyResolving;
        var glowAlpha = currentEnemyIsBoss ? (enemyTurn ? 0.38f : 0.20f) : (enemyTurn ? 0.27f : 0.07f);
        combatEnemyGlow = AddCombatGlow(holder, new Color(theme.r, theme.g, theme.b, glowAlpha));
        combatEnemyArt = AddFlatPanel("Enemy Combat Art", holder, Color.clear);
        Stretch(combatEnemyArt, 0, 0, 0, 0);
        combatEnemyArt.GetComponent<Image>().raycastTarget = false;
        var visual = AddFlatPanel("Enemy Sprite Visual", combatEnemyArt, Color.white);
        Stretch(visual, 0, 0, 0, 0);
        combatEnemyImage = visual.GetComponent<Image>();
        combatEnemyImage.raycastTarget = false;
        combatEnemyImage.preserveAspect = true;
        var sprite = LoadCurrentEnemySprite();
        ApplyCombatSprite(combatEnemyImage, sprite, currentEnemyIsBoss ? 0.96f : 0.90f, currentEnemyIsBoss ? 1.68f : 1.56f);
        if (combatEnemyImage.sprite == null)
        {
            var fallback = AddText(holder, currentEnemyIsBoss ? "BOSS" : "ENEMY", 30, FontStyle.Bold, dangerColor, TextAnchor.MiddleCenter, 285);
            Stretch(fallback.GetComponent<RectTransform>(), 0, 0, 0, 0);
        }
    }

    private void SetHeroCombatSprite(string state)
    {
        if (combatHeroImage == null)
        {
            return;
        }

        ApplyCombatSprite(combatHeroImage, LoadCharacterStateSprite(player.portraitName, state), 0.90f, 1.46f);
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

        var status = AddPanel("Combat Status Chips", parent, new Color(accent.r, accent.g, accent.b, 0.11f));
        AddLayoutSize(status, -1, 28);
        var label = StatusLine(effects);
        var text = AddText(status, label, 15, FontStyle.Bold, accent, TextAnchor.MiddleCenter, 28);
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
            return new Color(1f, 231f / 255f, 234f / 255f, 0f);
        }
        if (phase == CombatPresentationPhase.Result)
        {
            return new Color(1f, 247f / 255f, 218f / 255f, 0f);
        }
        return new Color(224f / 255f, 245f / 255f, 252f / 255f, 0f);
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
                yield return AnimateCombatImpact(presentation, target, targetStart, targetBar, generation);
            }
            else
            {
                yield return AnimateFloatingOnly(presentation.targetIsPlayer, "행동 불가", dangerColor, generation);
            }
        }
        else if (presentation.buff)
        {
            PlayCombatActionSound(true);
            if (presentation.attackerIsPlayer && combatHeroImage != null)
            {
                SetHeroCombatSprite("skill");
            }
            yield return PulseCombatant(attacker, attackerGlow, glowStart, generation);
            yield return AnimateFloatingOnly(presentation.targetIsPlayer, "강화", goodColor, generation);
        }
        else
        {
            if (presentation.attackerIsPlayer && combatHeroImage != null)
            {
                SetHeroCombatSprite(presentation.magic || presentation.actionName != "기본 공격" ? "skill" : "combat");
            }

            PlayCombatActionSound(presentation.magic);
            yield return PrepareCombatant(attacker, attackerGlow, glowStart, presentation.magic, generation);
            var travel = presentation.magic ? 72f : 168f;
            var direction = presentation.attackerIsPlayer ? 1f : -1f;
            yield return MoveCombatant(attacker, attackerStart, attackerStart + new Vector2(travel * direction, presentation.magic ? 10f : 0f), 0.17f, generation);

            if (presentation.missed)
            {
                PlayCombatMissSound();
                yield return AnimateFloatingOnly(presentation.targetIsPlayer, "회피!", manaColor, generation);
            }
            else
            {
                PlayCombatHitSound(presentation.critical);
                yield return WaitForCombatSeconds(0.065f);
                yield return AnimateCombatImpact(presentation, target, targetStart, targetBar, generation);
            }
            yield return MoveCombatant(attacker, attacker.anchoredPosition, attackerStart, 0.18f, generation);
        }

        if (presentation.healAmount > 0 && CombatViewIsValid(generation))
        {
            PlayCombatHealSound();
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
        var floating = CreateCombatFloatingText(
            presentation.targetIsPlayer,
            presentation.critical ? "치명타!\n-" + presentation.damage : "-" + presentation.damage,
            presentation.critical ? goldColor : dangerColor);
        var floatingStart = floating != null ? floating.rectTransform.anchoredPosition : Vector2.zero;
        var stageStart = combatStageContent != null ? combatStageContent.anchoredPosition : Vector2.zero;
        var flashColor = presentation.critical ? new Color(1f, 0.82f, 0.28f, 0.54f) : (presentation.magic ? new Color(0.38f, 0.82f, 1f, 0.38f) : new Color(1f, 1f, 1f, 0.42f));
        const float duration = 0.34f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var strength = (presentation.critical ? 22f : 13f) * (1f - progress);
            target.anchoredPosition = targetStart + new Vector2(Mathf.Sin(progress * 91f) * strength, Mathf.Cos(progress * 73f) * strength * 0.45f);
            if (combatStageContent != null)
            {
                var cameraStrength = (presentation.critical ? 9f : 5f) * (1f - progress);
                combatStageContent.anchoredPosition = stageStart + new Vector2(Mathf.Sin(progress * 67f) * cameraStrength, Mathf.Cos(progress * 59f) * cameraStrength);
            }
            if (combatImpactFlash != null)
            {
                combatImpactFlash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * (1f - progress));
            }
            if (floating != null)
            {
                floating.rectTransform.anchoredPosition = floatingStart + new Vector2(0f, progress * 76f);
                var textColorNow = floating.color;
                textColorNow.a = Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.58f) / 0.42f);
                floating.color = textColorNow;
                floating.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(progress * Mathf.PI) * (presentation.critical ? 0.26f : 0.12f));
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
            yield return WaitForCombatSeconds(0.10f);
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
        var anchor = targetIsPlayer ? new Vector2(0.27f, 0.60f) : new Vector2(0.73f, 0.60f);
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
        const float duration = 0.48f;
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
        InvalidateCombatView();
    }

    private void ClearPendingCombatPresentation()
    {
        pendingCombatPresentation = null;
    }

    private void ReleaseCombatPresentationResources()
    {
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
