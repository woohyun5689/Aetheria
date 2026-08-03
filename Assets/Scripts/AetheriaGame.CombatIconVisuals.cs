using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private const string CombatIconVisualRoot = "UI/VisualRefresh/Combat/";

    private void BuildEnemyIntentVisual(RectTransform intent, CombatPresentationPhase phase, Color accent)
    {
        if (intent == null)
        {
            return;
        }

        var row = intent.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 10f;
        row.padding = new RectOffset(12, 14, 8, 8);
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;

        var iconHolder = AddFlatPanel("Enemy Intent Icon Holder", intent, Color.clear);
        iconHolder.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(iconHolder, 76f, 92f);

        var icon = AddFlatPanel("Enemy Intent Generated Icon", iconHolder, Color.clear);
        icon.anchorMin = new Vector2(0.5f, 0.5f);
        icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(68f, 68f);
        icon.anchoredPosition = Vector2.zero;
        icon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var iconImage = icon.GetComponent<Image>();
        combatEnemyIntentIcon = icon;
        combatEnemyIntentIconImage = iconImage;
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.sprite = LoadGeneratedSprite(EnemyIntentIconPath(phase), Vector4.zero);
        iconImage.color = iconImage.sprite != null ? Color.white : new Color(accent.r, accent.g, accent.b, 0.30f);
        ConfigureEnemyIntentPulse(icon, phase == CombatPresentationPhase.EnemyResolving);

        var copy = AddFlatPanel("Enemy Intent Copy", intent, Color.clear);
        copy.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(copy, -1f, 92f);
        var copyLayout = copy.GetComponent<LayoutElement>();
        copyLayout.minWidth = 190f;
        copyLayout.flexibleWidth = 1f;
        AddVertical(copy, 0, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));

        AddText(copy, EnemyIntentTitle(phase), 20, FontStyle.Bold, accent, TextAnchor.MiddleLeft, 29f);
        AddText(copy, EnemyIntentDetail(phase), 17, FontStyle.Normal, mutedColor, TextAnchor.MiddleLeft, 26f);
        AddText(copy, EnemyIntentForecast(phase), 24, FontStyle.Bold, EnemyIntentForecastColor(phase, accent), TextAnchor.MiddleLeft, 35f);
    }

    private string EnemyIntentIconPath(CombatPresentationPhase phase)
    {
        if (phase == CombatPresentationPhase.Result
            || HasStatus(enemyStatusEffects, "stun")
            || HasStatus(enemyStatusEffects, "freeze"))
        {
            return CombatIconVisualRoot + "intent_disabled";
        }

        if (pendingCombatPresentation != null && !pendingCombatPresentation.attackerIsPlayer)
        {
            if (pendingCombatPresentation.buff || pendingCombatPresentation.healAmount > 0)
            {
                return CombatIconVisualRoot + "intent_skill";
            }
            return CombatIconVisualRoot + (pendingCombatPresentation.magic ? "intent_magic" : "intent_attack");
        }

        if (EnemyHasAffordableSkill())
        {
            return CombatIconVisualRoot + "intent_skill";
        }

        return CombatIconVisualRoot + (currentEnemy != null && currentEnemy.magic > currentEnemy.attack * 1.2f
            ? "intent_magic"
            : "intent_attack");
    }

    private string EnemyIntentForecast(CombatPresentationPhase phase)
    {
        if (currentEnemy == null || phase == CombatPresentationPhase.Result)
        {
            return "행동 종료";
        }

        if (pendingCombatPresentation != null && !pendingCombatPresentation.attackerIsPlayer)
        {
            if (pendingCombatPresentation.missed)
            {
                return "피해 없음 · 회피 성공";
            }
            if (pendingCombatPresentation.healAmount > 0)
            {
                return "회복 " + pendingCombatPresentation.healAmount;
            }
            if (pendingCombatPresentation.buff)
            {
                return "강화 행동";
            }
            if (pendingCombatPresentation.damage > 0)
            {
                return "확정 피해 " + pendingCombatPresentation.damage;
            }
        }

        if (HasStatus(enemyStatusEffects, "stun") || HasStatus(enemyStatusEffects, "freeze"))
        {
            return "예상 피해 0";
        }

        var minDamage = PreviewEnemyDamage(currentEnemy.attack, 1f, false);
        var criticalPossible = currentEnemy.crit > 0f;
        var maxDamage = criticalPossible
            ? Mathf.Max(minDamage, PreviewEnemyDamage(currentEnemy.attack, 1f, true))
            : minDamage;
        var supportActionPossible = false;
        var previewEnemyMp = EnemyPreviewMpForNextAction();
        if (!HasStatus(enemyStatusEffects, "silence") && currentEnemy.skills != null)
        {
            for (var index = 0; index < currentEnemy.skills.Count; index++)
            {
                var skill = currentEnemy.skills[index];
                if (skill == null || previewEnemyMp < EnemySkillCost(skill))
                {
                    continue;
                }
                if (skill.noDamage)
                {
                    supportActionPossible = true;
                    continue;
                }

                var power = skill.magic ? currentEnemy.magic : currentEnemy.attack;
                var skillCanCrit = skill.forceCrit || currentEnemy.crit + skill.critBonus > 0f;
                criticalPossible = criticalPossible || skillCanCrit;
                var preview = PreviewEnemyDamage(power, skill.multiplier, skillCanCrit);
                maxDamage = Mathf.Max(maxDamage, preview);
            }
        }

        if (supportActionPossible)
        {
            minDamage = 0;
        }

        var damageText = minDamage == maxDamage
            ? "예상 " + minDamage
            : "예상 " + minDamage + "–" + maxDamage;
        if (criticalPossible)
        {
            damageText += " · 치명";
        }
        if (EffectiveEvasion() > 0f || HasStatus(enemyStatusEffects, "blind"))
        {
            damageText += " · 회피";
        }
        return damageText;
    }

    private int PreviewEnemyDamage(int basePower, float multiplier, bool forcedCritical)
    {
        var damage = CalculateDamage(
            Mathf.Max(0, basePower),
            Mathf.Max(0f, multiplier),
            Defense(),
            enemyStatusEffects,
            playerStatusEffects,
            forcedCritical ? 1.5f : 1f,
            DamageReduction());
        var shield = Mathf.Max(0, RoundToGameInt(StatusValue(playerStatusEffects, "shield", 0f)));
        return Mathf.Max(0, damage - shield);
    }

    private Color EnemyIntentForecastColor(CombatPresentationPhase phase, Color accent)
    {
        if (phase == CombatPresentationPhase.Result
            || HasStatus(enemyStatusEffects, "stun")
            || HasStatus(enemyStatusEffects, "freeze"))
        {
            return goodColor;
        }
        return Color.Lerp(accent, dangerColor, 0.34f);
    }

    private bool TryAddGeneratedCombatStatusSummary(Transform parent, List<StatusEffect> effects, Color fallbackAccent)
    {
        if (parent == null || effects == null || effects.Count == 0)
        {
            return false;
        }

        if (LoadGeneratedSprite(CombatIconVisualRoot + "status_damage", Vector4.zero) == null)
        {
            return false;
        }

        var row = AddFlatPanel("Combat Status Icon Row", parent, Color.clear);
        row.GetComponent<Image>().raycastTarget = false;
        AddLayoutSize(row, -1f, 34f);
        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6f;
        rowLayout.padding = new RectOffset(2, 2, 1, 1);
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        var orderedEffects = new List<StatusEffect>(effects.Count);
        for (var effectIndex = 0; effectIndex < effects.Count; effectIndex++)
        {
            if (effects[effectIndex] != null)
            {
                orderedEffects.Add(effects[effectIndex]);
            }
        }
        orderedEffects.Sort((left, right) =>
            CombatStatusDisplayPriority(left.type).CompareTo(CombatStatusDisplayPriority(right.type)));

        var visibleCount = Mathf.Min(4, orderedEffects.Count);
        for (var index = 0; index < visibleCount; index++)
        {
            var effect = orderedEffects[index];

            var tint = CombatStatusTint(effect.type, fallbackAccent);
            var chip = AddFlatPanel("Combat Status " + effect.type, row, Color.clear);
            chip.GetComponent<Image>().raycastTarget = false;
            AddLayoutSize(chip, 112f, 32f);
            var chipLayout = chip.GetComponent<LayoutElement>();
            chipLayout.minWidth = 112f;
            chipLayout.flexibleWidth = 1f;

            var icon = AddFlatPanel("Combat Status Icon", chip, Color.clear);
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(24f, 24f);
            icon.anchoredPosition = new Vector2(4f, 0f);
            icon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var iconImage = icon.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.sprite = LoadGeneratedSprite(CombatStatusIconPath(effect.type), Vector4.zero);
            iconImage.color = iconImage.sprite != null ? Color.white : tint;

            var value = effect.type == "shield"
                ? RoundToGameInt(effect.value).ToString()
                : Mathf.Max(0, effect.duration) + "턴";
            var label = AddText(chip, StatusName(effect.type) + " " + value, 15, FontStyle.Bold, tint, TextAnchor.MiddleLeft, 32f);
            Stretch(label.GetComponent<RectTransform>(), 31f, 0f, 4f, 0f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 15;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        if (orderedEffects.Count > visibleCount)
        {
            var extra = AddText(row, "+" + (orderedEffects.Count - visibleCount), 16, FontStyle.Bold, fallbackAccent, TextAnchor.MiddleCenter, 32f);
            AddLayoutSize(extra.GetComponent<RectTransform>(), 38f, 32f);
        }
        return true;
    }

    private static int CombatStatusDisplayPriority(string statusType)
    {
        switch (statusType)
        {
            case "stun":
            case "freeze":
            case "silence":
            case "blind":
                return 0;
            case "burn":
            case "poison":
            case "bleed":
            case "mana_burn":
            case "shock":
            case "weaken":
            case "vulnerable":
                return 1;
            case "shield":
            case "regen":
                return 2;
            default:
                return 3;
        }
    }

    private string CombatStatusIconPath(string statusType)
    {
        switch (statusType)
        {
            case "burn":
            case "poison":
            case "bleed":
            case "mana_burn":
                return CombatIconVisualRoot + "status_damage";
            case "stun":
            case "freeze":
            case "blind":
            case "silence":
                return CombatIconVisualRoot + "status_control";
            case "shock":
            case "weaken":
            case "vulnerable":
                return CombatIconVisualRoot + "status_weaken";
            case "shield":
                return CombatIconVisualRoot + "status_shield";
            case "regen":
                return CombatIconVisualRoot + "status_health";
            case "evasion_boost":
            case "damage_boost":
                return CombatIconVisualRoot + "status_buff";
            default:
                return CombatIconVisualRoot + "status_weaken";
        }
    }

    private Color CombatStatusTint(string statusType, Color fallback)
    {
        switch (statusType)
        {
            case "burn": return Rgb(248, 90, 48);
            case "poison": return Rgb(70, 205, 103);
            case "bleed": return Rgb(225, 58, 74);
            case "mana_burn": return Rgb(131, 95, 238);
            case "stun": return Rgb(250, 193, 58);
            case "freeze": return Rgb(84, 190, 245);
            case "blind": return Rgb(116, 107, 147);
            case "silence": return Rgb(155, 105, 220);
            case "shock": return Rgb(88, 173, 255);
            case "weaken": return Rgb(180, 91, 198);
            case "vulnerable": return Rgb(239, 85, 113);
            case "shield": return Rgb(74, 185, 211);
            case "regen": return Rgb(63, 194, 109);
            case "evasion_boost": return Rgb(62, 205, 180);
            case "damage_boost": return Rgb(246, 159, 52);
            default: return fallback;
        }
    }
}
