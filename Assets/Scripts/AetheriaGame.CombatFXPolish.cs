using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private const string CombatFxVisualRoot = "UI/VisualRefresh/CombatFX/";
    private const string CombatFxSlashPath = CombatFxVisualRoot + "fx_slash_arc";
    private const string CombatFxImpactPath = CombatFxVisualRoot + "fx_impact_burst";
    private const string CombatFxSigilPath = CombatFxVisualRoot + "fx_arcane_sigil";
    private const string CombatFxWardPath = CombatFxVisualRoot + "fx_ward_bloom";
    private const string CombatFxClassVisualRoot = CombatFxVisualRoot + "Classes/";
    private const string CombatFxSkillVisualRoot = CombatFxVisualRoot + "Skills/";

    private Image combatHeroRune;
    private Image combatEnemyRune;
    private RectTransform combatEnemyIntentIcon;
    private Image combatEnemyIntentIconImage;

    private Image AddCombatFxSprite(
        Transform parent,
        string name,
        string resourcePath,
        Vector2 position,
        Vector2 size,
        float rotation,
        Color color,
        float outlineAlpha = 0f)
    {
        var sprite = LoadGeneratedSprite(resourcePath, Vector4.zero);
        if (parent == null || sprite == null)
        {
            return null;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = color;
        if (outlineAlpha > 0f)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.015f, 0.028f, 0.055f, outlineAlpha);
            outline.effectDistance = new Vector2(2.2f, -2.2f);
            outline.useGraphicAlpha = true;
        }
        return image;
    }

    private static string CombatFxClassKey(CombatFxStyle style)
    {
        switch (style)
        {
            case CombatFxStyle.Paladin: return "knight";
            case CombatFxStyle.Elementalist: return "mage";
            case CombatFxStyle.Rogue: return "rogue";
            case CombatFxStyle.Priest: return "priest";
            case CombatFxStyle.Bomber: return "bomber";
            case CombatFxStyle.Spirit: return "spirit";
            case CombatFxStyle.Archer: return "archer";
            case CombatFxStyle.Monk: return "monk";
            default: return null;
        }
    }

    private string CombatFxClassPath(CombatFxLook look, string slot)
    {
        var key = CombatFxClassKey(look.style);
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(slot))
        {
            return null;
        }

        var path = CombatFxClassVisualRoot + key + "/fx_" + slot;
        return LoadGeneratedSprite(path, Vector4.zero) != null ? path : null;
    }

    private string CombatFxSkillKey(CombatPresentationEvent presentation)
    {
        if (presentation == null
            || !presentation.attackerIsPlayer
            || string.IsNullOrEmpty(presentation.actionName))
        {
            return null;
        }

        string key;
        return SkillIconResourceByName.TryGetValue(presentation.actionName, out key)
            ? key
            : null;
    }

    private string CombatFxSkillPath(CombatPresentationEvent presentation, string slot)
    {
        var key = CombatFxSkillKey(presentation);
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(slot))
        {
            return null;
        }

        var path = CombatFxSkillVisualRoot + key + "/fx_" + slot;
        return LoadGeneratedSprite(path, Vector4.zero) != null ? path : null;
    }

    private static Vector2 CombatFxClassAttackSize(CombatFxStyle style)
    {
        switch (style)
        {
            case CombatFxStyle.Archer:
            case CombatFxStyle.Monk:
                return new Vector2(334f, 206f);
            default:
                return new Vector2(318f, 212f);
        }
    }

    private void AddGeneratedCombatAttackAccents(RectTransform rootFx, CombatFxLook look, float direction)
    {
        if (rootFx == null)
        {
            return;
        }

        switch (look.element)
        {
            case CombatFxElement.Fire:
                AddCombatFxPart(rootFx, "Fire Core Accent", new Vector2(8f * direction, 0f), new Vector2(58f, 58f), 0f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.82f), true);
                AddCombatFxPart(rootFx, "Fire Ember Accent A", new Vector2(-62f * direction, 24f), new Vector2(18f, 18f), 0f, look.secondary, true);
                AddCombatFxPart(rootFx, "Fire Ember Accent B", new Vector2(-78f * direction, -20f), new Vector2(12f, 12f), 0f, look.primary, true);
                break;
            case CombatFxElement.Ice:
                AddCombatFxPart(rootFx, "Ice Shard Accent A", new Vector2(-28f * direction, 18f), new Vector2(10f, 52f), direction > 0f ? -58f : 58f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.88f), false);
                AddCombatFxPart(rootFx, "Ice Shard Accent B", new Vector2(-54f * direction, -24f), new Vector2(7f, 34f), direction > 0f ? -76f : 76f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.70f), false);
                break;
            case CombatFxElement.Lightning:
                AddCombatFxPart(rootFx, "Lightning Accent A", new Vector2(-44f * direction, 14f), new Vector2(52f, 5f), direction > 0f ? -24f : 24f, look.primary, false);
                AddCombatFxPart(rootFx, "Lightning Accent B", new Vector2(0f, -2f), new Vector2(48f, 5f), direction > 0f ? 28f : -28f, look.secondary, false);
                AddCombatFxPart(rootFx, "Lightning Accent C", new Vector2(42f * direction, -18f), new Vector2(48f, 5f), direction > 0f ? -24f : 24f, new Color(1f, 1f, 1f, 0.86f), false);
                break;
            case CombatFxElement.Poison:
                AddCombatFxPart(rootFx, "Poison Mote Accent A", new Vector2(-54f * direction, 30f), new Vector2(19f, 19f), 0f, new Color(look.accent.r, look.accent.g, look.accent.b, 0.86f), true);
                AddCombatFxPart(rootFx, "Poison Mote Accent B", new Vector2(-72f * direction, -24f), new Vector2(12f, 12f), 0f, look.primary, true);
                break;
            case CombatFxElement.Shadow:
                AddCombatFxPart(rootFx, "Shadow Wake Accent", new Vector2(-38f * direction, 0f), new Vector2(104f, 72f), 0f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.40f), true);
                break;
            case CombatFxElement.Spirit:
                AddCombatFxPart(rootFx, "Spirit Accent Fire", new Vector2(-48f, 0f), new Vector2(22f, 22f), 0f, Rgb(245, 105, 55), true);
                AddCombatFxPart(rootFx, "Spirit Accent Water", new Vector2(0f, 44f), new Vector2(22f, 22f), 0f, look.secondary, true);
                AddCombatFxPart(rootFx, "Spirit Accent Wind", new Vector2(48f, 0f), new Vector2(22f, 22f), 0f, look.primary, true);
                AddCombatFxPart(rootFx, "Spirit Accent Earth", new Vector2(0f, -44f), new Vector2(22f, 22f), 0f, look.accent, true);
                break;
            case CombatFxElement.Explosion:
                AddCombatFxPart(rootFx, "Bomb Core Accent", Vector2.zero, new Vector2(48f, 48f), 0f, new Color(0.08f, 0.07f, 0.12f, 0.86f), true);
                AddCombatFxPart(rootFx, "Bomb Fuse Accent", new Vector2(-34f * direction, 34f), new Vector2(44f, 5f), direction > 0f ? -38f : 38f, look.secondary, false);
                AddCombatFxPart(rootFx, "Bomb Spark Accent", new Vector2(-49f * direction, 51f), new Vector2(19f, 19f), 0f, look.accent, true);
                break;
        }
    }

    private Vector2 CombatFxAnchorFor(bool playerSide, float verticalOffset = 0f)
    {
        var fallback = playerSide ? new Vector2(0.27f, 0.59f) : new Vector2(0.73f, 0.59f);
        var art = playerSide ? combatHeroArt : combatEnemyArt;
        if (combatFxLayer == null || art == null || combatFxLayer.rect.width <= 1f || combatFxLayer.rect.height <= 1f)
        {
            fallback.y += verticalOffset;
            return fallback;
        }

        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(combatFxLayer, art);
        var layerRect = combatFxLayer.rect;
        var anchor = new Vector2(
            Mathf.InverseLerp(layerRect.xMin, layerRect.xMax, bounds.center.x),
            Mathf.InverseLerp(layerRect.yMin, layerRect.yMax, bounds.center.y) + verticalOffset);
        anchor.x = Mathf.Clamp(anchor.x, 0.08f, 0.92f);
        anchor.y = Mathf.Clamp(anchor.y, 0.18f, 0.86f);
        return anchor;
    }

    private IEnumerator PlayCombatTelegraphFx(CombatPresentationEvent presentation, int generation)
    {
        if (presentation == null || !CombatViewIsValid(generation) || combatFxLayer == null)
        {
            yield break;
        }

        var look = ResolveCombatFxLook(presentation);
        var attackerAnchor = CombatFxAnchorFor(presentation.attackerIsPlayer, 0.035f);
        var targetAnchor = CombatFxAnchorFor(presentation.targetIsPlayer, -0.015f);
        var charge = CreateCombatFxRoot("Attack Telegraph Charge", attackerAnchor, new Vector2(210f, 210f));
        var reticle = CreateCombatFxRoot("Attack Telegraph Target", targetAnchor, new Vector2(178f, 178f));
        if (charge == null || reticle == null)
        {
            if (charge != null) Destroy(charge.gameObject);
            if (reticle != null) Destroy(reticle.gameObject);
            yield break;
        }

        var chargeGroup = charge.gameObject.AddComponent<CanvasGroup>();
        var reticleGroup = reticle.gameObject.AddComponent<CanvasGroup>();
        AddCombatFxPart(charge, "Charge Backplate", Vector2.zero, new Vector2(188f, 188f), 0f, new Color(0.01f, 0.025f, 0.055f, 0.30f), true);
        var skillChargePath = CombatFxSkillPath(presentation, "impact");
        var classChargePath = CombatFxClassPath(look, "impact");
        var chargePath = !string.IsNullOrEmpty(skillChargePath)
            ? skillChargePath
            : classChargePath;
        AddCombatFxSprite(
            charge,
            string.IsNullOrEmpty(chargePath)
                ? "Generated Casting Sigil"
                : (!string.IsNullOrEmpty(skillChargePath)
                    ? "Generated Skill Telegraph " + CombatFxSkillKey(presentation)
                    : "Generated Class Telegraph " + CombatFxClassKey(look.style)),
            string.IsNullOrEmpty(chargePath) ? CombatFxSigilPath : chargePath,
            Vector2.zero,
            string.IsNullOrEmpty(chargePath)
                ? (presentation.magic ? new Vector2(184f, 184f) : new Vector2(142f, 142f))
                : new Vector2(158f, 158f),
            0f,
            string.IsNullOrEmpty(chargePath)
                ? new Color(look.primary.r, look.primary.g, look.primary.b, presentation.magic ? 0.86f : 0.58f)
                : new Color(1f, 1f, 1f, 0.88f),
            0.42f);
        AddCombatFxPart(reticle, "Target Local Contrast", Vector2.zero, new Vector2(160f, 160f), 0f, new Color(0.01f, 0.025f, 0.045f, 0.24f), true);
        AddCombatFxSprite(
            reticle,
            "Generated Target Reticle",
            CombatFxSigilPath,
            Vector2.zero,
            new Vector2(154f, 154f),
            0f,
            new Color(look.accent.r, look.accent.g, look.accent.b, 0.62f),
            0.34f);

        var duration = presentation.attackerIsPlayer ? 0.17f : 0.22f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && charge != null && reticle != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var reveal = Mathf.Sin(progress * Mathf.PI);
            charge.localScale = Vector3.one * Mathf.Lerp(0.58f, 1.04f, 1f - Mathf.Pow(1f - progress, 3f));
            charge.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-22f, 2f, progress));
            reticle.localScale = Vector3.one * Mathf.Lerp(1.18f, 0.94f, progress);
            reticle.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(16f, -6f, progress));
            chargeGroup.alpha = reveal;
            reticleGroup.alpha = reveal * 0.86f;
            yield return null;
        }

        if (charge != null) Destroy(charge.gameObject);
        if (reticle != null) Destroy(reticle.gameObject);
    }

    private bool AddGeneratedCombatAttackSprite(
        RectTransform rootFx,
        CombatFxLook look,
        float direction,
        CombatPresentationEvent presentation)
    {
        if (rootFx == null)
        {
            return false;
        }

        var skillPath = CombatFxSkillPath(presentation, "action");
        if (!string.IsNullOrEmpty(skillPath))
        {
            var skillImage = AddCombatFxSprite(
                rootFx,
                "Generated Skill Action " + CombatFxSkillKey(presentation),
                skillPath,
                new Vector2(8f * direction, 0f),
                CombatFxClassAttackSize(look.style),
                0f,
                new Color(1f, 1f, 1f, 0.99f),
                0.24f);
            if (skillImage != null && direction < 0f)
            {
                skillImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            return skillImage != null;
        }

        var classPath = CombatFxClassPath(look, "attack");
        if (!string.IsNullOrEmpty(classPath))
        {
            var classImage = AddCombatFxSprite(
                rootFx,
                "Generated Class Attack " + CombatFxClassKey(look.style),
                classPath,
                new Vector2(8f * direction, 0f),
                CombatFxClassAttackSize(look.style),
                0f,
                new Color(1f, 1f, 1f, 0.98f),
                0.24f);
            if (classImage != null && direction < 0f)
            {
                classImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            return classImage != null;
        }

        var tint = Color.Lerp(Color.white, look.primary, 0.28f);
        tint.a = 0.96f;
        var physical = look.element == CombatFxElement.Slash
            || look.element == CombatFxElement.Poison
            || look.element == CombatFxElement.Shadow
            || look.element == CombatFxElement.Wind
            || look.element == CombatFxElement.Fist
            || look.element == CombatFxElement.EnemyPhysical;
        if (physical)
        {
            var size = look.element == CombatFxElement.Fist ? new Vector2(242f, 160f) : new Vector2(310f, 204f);
            return AddCombatFxSprite(
                rootFx,
                "Generated Slash Arc",
                CombatFxSlashPath,
                new Vector2(8f * direction, 0f),
                size,
                direction > 0f ? -4f : 176f,
                tint,
                0.30f) != null;
        }

        var sigilTint = Color.Lerp(Color.white, look.primary, 0.38f);
        sigilTint.a = look.element == CombatFxElement.Explosion ? 0.68f : 0.84f;
        return AddCombatFxSprite(
            rootFx,
            "Generated Element Sigil",
            CombatFxSigilPath,
            Vector2.zero,
            look.element == CombatFxElement.Explosion ? new Vector2(136f, 136f) : new Vector2(154f, 154f),
            0f,
            sigilTint,
            0.40f) != null;
    }

    private bool AddGeneratedCombatImpactSprite(
        RectTransform rootFx,
        CombatFxLook look,
        bool critical,
        CombatPresentationEvent presentation)
    {
        if (rootFx == null)
        {
            return false;
        }

        var skillPath = CombatFxSkillPath(presentation, "impact");
        if (!string.IsNullOrEmpty(skillPath))
        {
            return AddCombatFxSprite(
                rootFx,
                "Generated Skill Impact " + CombatFxSkillKey(presentation),
                skillPath,
                Vector2.zero,
                critical ? new Vector2(364f, 364f) : new Vector2(304f, 304f),
                critical ? -5f : 0f,
                new Color(1f, 1f, 1f, critical ? 1f : 0.98f),
                critical ? 0.34f : 0.24f) != null;
        }

        var classPath = CombatFxClassPath(look, "impact");
        if (!string.IsNullOrEmpty(classPath))
        {
            return AddCombatFxSprite(
                rootFx,
                "Generated Class Impact " + CombatFxClassKey(look.style),
                classPath,
                Vector2.zero,
                critical ? new Vector2(364f, 364f) : new Vector2(304f, 304f),
                critical ? -5f : 0f,
                new Color(1f, 1f, 1f, critical ? 1f : 0.96f),
                critical ? 0.34f : 0.24f) != null;
        }

        var tint = Color.Lerp(Color.white, look.primary, 0.22f);
        tint.a = critical ? 1f : 0.94f;
        var impactImage = AddCombatFxSprite(
            rootFx,
            critical ? "Generated Critical Burst" : "Generated Impact Burst",
            CombatFxImpactPath,
            Vector2.zero,
            critical ? new Vector2(350f, 350f) : new Vector2(286f, 286f),
            0f,
            tint,
            critical ? 0.38f : 0.28f);
        if (impactImage == null)
        {
            return false;
        }

        if (look.element == CombatFxElement.Slash
            || look.element == CombatFxElement.Shadow
            || look.element == CombatFxElement.Poison
            || look.element == CombatFxElement.Wind)
        {
            var slashTint = Color.Lerp(Color.white, look.secondary, 0.30f);
            slashTint.a = 0.82f;
            AddCombatFxSprite(
                rootFx,
                "Generated Contact Slash",
                CombatFxSlashPath,
                Vector2.zero,
                new Vector2(270f, 180f),
                -20f,
                slashTint,
                0.24f);
        }
        return impactImage != null;
    }

    private bool AddGeneratedCombatSupportSprite(
        RectTransform rootFx,
        CombatFxLook look,
        bool healing,
        CombatPresentationEvent presentation)
    {
        if (rootFx == null)
        {
            return false;
        }

        var skillPath = CombatFxSkillPath(presentation, "support");
        if (!string.IsNullOrEmpty(skillPath))
        {
            return AddCombatFxSprite(
                rootFx,
                "Generated Skill Support " + CombatFxSkillKey(presentation),
                skillPath,
                new Vector2(0f, 4f),
                new Vector2(318f, 382f),
                0f,
                new Color(1f, 1f, 1f, healing ? 0.99f : 0.97f),
                0.26f) != null;
        }

        var classPath = CombatFxClassPath(look, "support");
        if (!string.IsNullOrEmpty(classPath))
        {
            return AddCombatFxSprite(
                rootFx,
                "Generated Class Support " + CombatFxClassKey(look.style),
                classPath,
                new Vector2(0f, 4f),
                new Vector2(318f, 382f),
                0f,
                new Color(1f, 1f, 1f, healing ? 0.98f : 0.96f),
                0.26f) != null;
        }

        var supportColor = healing ? Rgb(45, 196, 139) : Rgb(66, 159, 224);
        supportColor = Color.Lerp(supportColor, look.primary, 0.18f);
        var wardTint = Color.Lerp(Color.white, supportColor, 0.58f);
        wardTint.a = healing ? 0.96f : 0.98f;
        var sigilTint = Color.Lerp(Color.white, supportColor, 0.66f);
        sigilTint.a = 0.64f;
        var wardImage = AddCombatFxSprite(
            rootFx,
            healing ? "Generated Healing Bloom" : "Generated Guard Bloom",
            CombatFxWardPath,
            new Vector2(0f, 6f),
            new Vector2(316f, 374f),
            0f,
            wardTint,
            0.34f);
        if (wardImage == null)
        {
            return false;
        }
        AddCombatFxSprite(
            rootFx,
            "Generated Support Sigil",
            CombatFxSigilPath,
            new Vector2(0f, -68f),
            new Vector2(246f, 246f),
            0f,
            sigilTint,
            0.20f);
        return true;
    }

    private IEnumerator PlayCombatStatusFx(CombatPresentationEvent presentation, int generation)
    {
        if (presentation == null || !CombatViewIsValid(generation) || combatFxLayer == null)
        {
            yield break;
        }

        var cue = string.IsNullOrEmpty(presentation.statusType) ? "stun" : presentation.statusType;
        var color = CombatStatusFxColor(cue);
        var anchor = CombatFxAnchorFor(presentation.targetIsPlayer, 0.01f);
        var rootFx = CreateCombatFxRoot("Status FX - " + cue, anchor, new Vector2(300f, 300f));
        if (rootFx == null)
        {
            yield break;
        }

        var group = rootFx.gameObject.AddComponent<CanvasGroup>();
        var damageCue = cue == "burn" || cue == "poison" || cue == "bleed" || cue == "shock" || cue == "mana_burn";
        AddCombatFxPart(rootFx, "Status Local Contrast", Vector2.zero, new Vector2(230f, 230f), 0f, new Color(0.01f, 0.02f, 0.04f, 0.24f), true);
        var statusTint = Color.Lerp(Color.white, color, 0.46f);
        AddCombatFxSprite(
            rootFx,
            damageCue ? "Generated Status Burst" : "Generated Control Sigil",
            damageCue ? CombatFxImpactPath : CombatFxSigilPath,
            Vector2.zero,
            damageCue ? new Vector2(244f, 244f) : new Vector2(218f, 218f),
            0f,
            new Color(statusTint.r, statusTint.g, statusTint.b, 0.90f),
            0.26f);

        for (var index = 0; index < 7; index++)
        {
            var angle = (index * 360f / 7f + 18f) * Mathf.Deg2Rad;
            var distance = 78f + (index % 2) * 22f;
            var point = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
            AddCombatFxPart(
                rootFx,
                "Status Mote " + index,
                point,
                new Vector2(index % 2 == 0 ? 18f : 11f, index % 2 == 0 ? 18f : 11f),
                index * 31f,
                new Color(color.r, color.g, color.b, 0.82f),
                true);
        }

        const float duration = 0.42f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && rootFx != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var snap = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress * 3.2f), 3f);
            rootFx.localScale = Vector3.one * Mathf.Lerp(0.42f, 1.22f, snap);
            rootFx.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-14f, 16f, progress));
            group.alpha = Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.50f) / 0.50f);
            yield return null;
        }
        if (rootFx != null) Destroy(rootFx.gameObject);
    }

    private IEnumerator PlayCombatShieldImpactFx(CombatPresentationEvent presentation, int generation)
    {
        if (presentation == null || !CombatViewIsValid(generation) || combatFxLayer == null)
        {
            yield break;
        }

        var anchor = CombatFxAnchorFor(presentation.targetIsPlayer, 0.01f);
        var rootFx = CreateCombatFxRoot("Shield Absorb FX", anchor, new Vector2(290f, 330f));
        if (rootFx == null)
        {
            yield break;
        }
        var group = rootFx.gameObject.AddComponent<CanvasGroup>();
        AddCombatFxPart(rootFx, "Shield Local Contrast", Vector2.zero, new Vector2(240f, 260f), 0f, new Color(0.01f, 0.03f, 0.06f, 0.30f), true);
        AddCombatFxSprite(
            rootFx,
            "Generated Shield Absorb",
            CombatFxWardPath,
            Vector2.zero,
            new Vector2(272f, 318f),
            0f,
            new Color(0.58f, 0.86f, 1f, 0.96f),
            0.62f);
        AddCombatFxSprite(
            rootFx,
            "Generated Shield Ring",
            CombatFxSigilPath,
            new Vector2(0f, -42f),
            new Vector2(230f, 230f),
            0f,
            new Color(0.40f, 0.77f, 1f, 0.58f),
            0.30f);

        const float duration = 0.36f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && rootFx != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var snap = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress * 3.6f), 3f);
            rootFx.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.13f, snap);
            group.alpha = Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.58f) / 0.42f);
            yield return null;
        }
        if (rootFx != null) Destroy(rootFx.gameObject);
    }

    private IEnumerator PlayCombatVictoryFx(bool playerDefeated, int generation)
    {
        if (!CombatViewIsValid(generation) || combatFxLayer == null)
        {
            yield break;
        }

        var targetAnchor = CombatFxAnchorFor(playerDefeated, 0.01f);
        var targetFx = CreateCombatFxRoot(playerDefeated ? "Defeat Collapse FX" : "Victory Finisher FX", targetAnchor, new Vector2(380f, 380f));
        RectTransform heroFx = null;
        if (!playerDefeated)
        {
            heroFx = CreateCombatFxRoot("Victory Hero Sigil", CombatFxAnchorFor(true, -0.08f), new Vector2(300f, 300f));
        }
        if (targetFx == null)
        {
            if (heroFx != null) Destroy(heroFx.gameObject);
            yield break;
        }

        var targetGroup = targetFx.gameObject.AddComponent<CanvasGroup>();
        var targetColor = playerDefeated ? new Color(0.53f, 0.30f, 0.72f, 0.88f) : new Color(1f, 0.82f, 0.32f, 1f);
        AddCombatFxSprite(targetFx, "Generated Finisher Burst", CombatFxImpactPath, Vector2.zero, new Vector2(370f, 370f), 0f, targetColor, 0.62f);
        CanvasGroup heroGroup = null;
        if (heroFx != null)
        {
            heroGroup = heroFx.gameObject.AddComponent<CanvasGroup>();
            AddCombatFxSprite(heroFx, "Generated Victory Sigil", CombatFxSigilPath, Vector2.zero, new Vector2(286f, 286f), 0f, new Color(1f, 0.88f, 0.48f, 0.82f), 0.42f);
        }

        const float duration = 0.58f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && targetFx != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var snap = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress * 3f), 3f);
            targetFx.localScale = Vector3.one * Mathf.Lerp(0.35f, playerDefeated ? 0.92f : 1.38f, snap);
            targetFx.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-12f, 28f, progress));
            targetGroup.alpha = Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.55f) / 0.45f);
            if (heroFx != null && heroGroup != null)
            {
                heroFx.localScale = Vector3.one * Mathf.Lerp(0.62f, 1.10f, snap);
                heroFx.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-18f, 10f, progress));
                heroGroup.alpha = Mathf.Sin(progress * Mathf.PI) * 0.92f;
            }
            yield return null;
        }

        if (targetFx != null) Destroy(targetFx.gameObject);
        if (heroFx != null) Destroy(heroFx.gameObject);
    }

    private void AddCombatPersistentStatusAura(Transform parent, List<StatusEffect> effects, Color fallback)
    {
        var cue = DominantCombatStatusType(effects);
        if (parent == null || string.IsNullOrEmpty(cue))
        {
            return;
        }

        var shield = cue == "shield";
        var color = CombatStatusFxColor(cue);
        var aura = AddFlatPanel("Persistent Status Aura - " + cue, parent, Color.clear);
        aura.anchorMin = shield ? new Vector2(0.16f, 0.03f) : new Vector2(0.20f, 0.04f);
        aura.anchorMax = shield ? new Vector2(0.84f, 0.96f) : new Vector2(0.80f, 0.78f);
        aura.offsetMin = Vector2.zero;
        aura.offsetMax = Vector2.zero;
        aura.GetComponent<Image>().raycastTarget = false;
        aura.GetComponent<Image>().sprite = LoadGeneratedSprite(shield ? CombatFxWardPath : CombatFxSigilPath, Vector4.zero);
        aura.GetComponent<Image>().preserveAspect = true;
        var tint = Color.Lerp(Color.white, color, 0.62f);
        var baseAlpha = shield ? 0.22f : 0.18f;
        aura.GetComponent<Image>().color = new Color(tint.r, tint.g, tint.b, baseAlpha);
        var presenter = aura.gameObject.AddComponent<UiCombatPersistentAuraPresenter>();
        presenter.Configure(aura, aura.GetComponent<Image>(), tint, baseAlpha, shield ? 0f : 5.5f, shield ? 0.018f : 0.028f);

        for (var index = 0; index < 5; index++)
        {
            var mote = AddFlatPanel("Persistent Status Mote " + index, parent, new Color(color.r, color.g, color.b, 0.30f));
            var x = 0.20f + index * 0.15f;
            var y = index % 2 == 0 ? 0.28f : 0.54f;
            mote.anchorMin = new Vector2(x, y);
            mote.anchorMax = mote.anchorMin;
            mote.pivot = new Vector2(0.5f, 0.5f);
            mote.sizeDelta = new Vector2(index % 2 == 0 ? 14f : 9f, index % 2 == 0 ? 14f : 9f);
            mote.anchoredPosition = Vector2.zero;
            var moteImage = mote.GetComponent<Image>();
            moteImage.sprite = CombatGlowSprite();
            moteImage.raycastTarget = false;
        }
    }

    private static string DominantCombatStatusType(List<StatusEffect> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            return "";
        }
        var priorities = new[] { "freeze", "stun", "shock", "burn", "poison", "bleed", "mana_burn", "shield", "regen", "vulnerable", "weaken", "silence", "blind", "damage_boost", "evasion_boost" };
        for (var priority = 0; priority < priorities.Length; priority++)
        {
            for (var index = 0; index < effects.Count; index++)
            {
                if (effects[index] != null && effects[index].type == priorities[priority])
                {
                    return priorities[priority];
                }
            }
        }
        return effects[0] != null ? effects[0].type : "";
    }

    private static string CombatStatusFxCue(List<StatusEffect> effects)
    {
        return DominantCombatStatusType(effects);
    }

    private static int CombatTurnStartRawDamage(List<StatusEffect> effects)
    {
        if (effects == null)
        {
            return 0;
        }
        var total = 0;
        for (var index = 0; index < effects.Count; index++)
        {
            var effect = effects[index];
            if (effect == null)
            {
                continue;
            }
            if (effect.type == "poison" || effect.type == "burn" || effect.type == "bleed" || effect.type == "shock")
            {
                total += Mathf.Max(1, RoundToGameInt(effect.value));
            }
        }
        return total;
    }

    private static Color CombatStatusFxColor(string cue)
    {
        switch (cue)
        {
            case "burn": return new Color32(244, 82, 42, 255);
            case "poison": return new Color32(126, 206, 62, 255);
            case "bleed": return new Color32(211, 52, 76, 255);
            case "shock": return new Color32(255, 218, 61, 255);
            case "freeze": return new Color32(73, 201, 242, 255);
            case "stun": return new Color32(154, 100, 224, 255);
            case "mana_burn": return new Color32(82, 125, 231, 255);
            case "shield": return new Color32(70, 174, 234, 255);
            case "regen": return new Color32(48, 193, 131, 255);
            case "vulnerable": return new Color32(223, 83, 144, 255);
            case "weaken": return new Color32(167, 104, 209, 255);
            case "silence": return new Color32(121, 93, 178, 255);
            case "blind": return new Color32(92, 119, 152, 255);
            case "damage_boost": return new Color32(244, 146, 46, 255);
            case "evasion_boost": return new Color32(69, 195, 181, 255);
            default: return new Color32(131, 174, 222, 255);
        }
    }

    private static string CombatSupportFxCue(SkillState skill)
    {
        if (skill == null)
        {
            return "";
        }
        if (skill.healsSelf) return "heal";
        if (skill.grantsShield || skill.selfStatusType == "shield") return "shield";
        if (!string.IsNullOrEmpty(skill.selfStatusType)) return skill.selfStatusType;
        return skill.noDamage ? "buff" : "";
    }

    private static string CombatSupportLabel(CombatPresentationEvent presentation)
    {
        if (presentation == null)
        {
            return "강화";
        }
        if (presentation.healAmount > 0 || presentation.supportType == "heal") return "회복";
        switch (presentation.supportType)
        {
            case "shield": return "보호막";
            case "regen": return "재생";
            case "damage_boost": return "공격 강화";
            case "evasion_boost": return "회피 상승";
            default: return "강화";
        }
    }

    private Color CombatSupportFxColor(CombatPresentationEvent presentation)
    {
        if (presentation != null && (presentation.healAmount > 0 || presentation.supportType == "heal"))
        {
            return Rgb(45, 196, 139);
        }
        if (presentation != null && !string.IsNullOrEmpty(presentation.supportType))
        {
            return CombatStatusFxColor(presentation.supportType);
        }
        return goodColor;
    }

    private void RefreshCombatTurnFxPhase(CombatPresentationPhase phase)
    {
        if (combatHeroRune != null)
        {
            var tint = Color.Lerp(Color.white, ActiveCharacterAccent(manaColor), 0.20f);
            var presenter = combatHeroRune.GetComponent<UiCombatGroundRunePresenter>();
            if (presenter != null)
            {
                presenter.Configure(combatHeroRune, tint, phase == CombatPresentationPhase.PlayerChoice || phase == CombatPresentationPhase.PlayerResolving, 0f);
            }
            var outline = combatHeroRune.GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(0.015f, 0.05f, 0.08f, phase == CombatPresentationPhase.EnemyResolving ? 0.32f : 0.52f);
        }

        if (combatEnemyRune != null)
        {
            var enemyTintSource = currentEnemyIsBoss ? goldColor : EnemyThemeColor();
            var tint = Color.Lerp(Color.white, enemyTintSource, 0.20f);
            var presenter = combatEnemyRune.GetComponent<UiCombatGroundRunePresenter>();
            if (presenter != null)
            {
                presenter.Configure(combatEnemyRune, tint, phase == CombatPresentationPhase.EnemyResolving, Mathf.PI);
            }
            var outline = combatEnemyRune.GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(0.015f, 0.05f, 0.08f, phase == CombatPresentationPhase.EnemyResolving ? 0.52f : 0.32f);
        }

        if (combatEnemyIntentIcon != null)
        {
            ConfigureEnemyIntentPulse(combatEnemyIntentIcon, phase == CombatPresentationPhase.EnemyResolving);
        }
        if (combatEnemyIntentIconImage != null)
        {
            combatEnemyIntentIconImage.sprite = LoadGeneratedSprite(EnemyIntentIconPath(phase), Vector4.zero);
        }
    }

    private sealed class UiCombatPersistentAuraPresenter : MonoBehaviour
    {
        private RectTransform rect;
        private Image image;
        private Color tint;
        private float baseAlpha;
        private float rotationSpeed;
        private float pulseAmount;
        private float phase;

        public void Configure(RectTransform sourceRect, Image sourceImage, Color sourceTint, float sourceAlpha, float sourceRotationSpeed, float sourcePulseAmount)
        {
            rect = sourceRect;
            image = sourceImage;
            tint = sourceTint;
            baseAlpha = sourceAlpha;
            rotationSpeed = sourceRotationSpeed;
            pulseAmount = sourcePulseAmount;
            phase = sourceRect != null && sourceRect.name.Contains("Enemy") ? Mathf.PI : 0f;
        }

        private void Update()
        {
            if (rect == null || image == null)
            {
                return;
            }
            var wave = (Mathf.Sin(Time.unscaledTime * 2.25f + phase) + 1f) * 0.5f;
            image.color = new Color(tint.r, tint.g, tint.b, baseAlpha * Mathf.Lerp(0.78f, 1.16f, wave));
            var scale = 1f + wave * pulseAmount;
            rect.localScale = new Vector3(scale, scale, 1f);
            if (Mathf.Abs(rotationSpeed) > 0.01f)
            {
                rect.localEulerAngles = new Vector3(0f, 0f, Time.unscaledTime * rotationSpeed);
            }
        }
    }
}
